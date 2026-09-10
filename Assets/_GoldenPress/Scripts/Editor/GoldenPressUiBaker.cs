using System.IO;
using GoldenPress.Bootstrap;
using GoldenPress.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GoldenPress.EditorTools
{
    /// <summary>
    /// One-shot bake of static UI into scenes so layout can be edited in the Hierarchy.
    /// Re-running replaces existing SplashCanvas / MillCanvas / ProductionCanvas hierarchies,
    /// then reapplies persisted text layout overrides (see UiLayoutOverlay).
    /// </summary>
    public static class GoldenPressUiBaker
    {
        private const string ScenesFolder = "Assets/Scenes";

        [MenuItem("Golden Press/Bake Authored UI Into Scenes")]
        public static void BakeAuthoredUiIntoScenes()
        {
            EnsureWhiteSpriteAsset();
            EnsureTmpFontsInResources();
            AssetDatabase.Refresh();
            ArtCatalog.Warm();

            BakeScene("0_SplashScene", SceneEntryPoint.SceneKind.Splash);
            BakeScene("1_MainMillScene", SceneEntryPoint.SceneKind.MainMill);
            BakeScene("2_ProductionScene", SceneEntryPoint.SceneKind.Production);

            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Golden Press: authored UI baked into Splash, Mill, and Production scenes. Text position/size overrides were preserved via UiLayoutOverrides.json.");
        }

        [MenuItem("Golden Press/Apply UI Fonts (Cinzel + Liberation)")]
        public static void ApplyUiFontsToScenes()
        {
            EnsureTmpFontsInResources();
            AssetDatabase.Refresh();

            var title = LoadTitleFont();
            var body = LoadBodyFont();
            if (title == null || body == null)
            {
                Debug.LogError("Golden Press: missing Cinzel-ExtraBold SDF or LiberationSans SDF font assets.");
                return;
            }

            ApplyFontsInScene("0_SplashScene", title, body);
            ApplyFontsInScene("1_MainMillScene", title, body);
            ApplyFontsInScene("2_ProductionScene", title, body);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Golden Press: applied Cinzel Extra Bold SDF to titles/headings/buttons and Liberation Sans SDF to body text.");
        }

        private static TMP_FontAsset LoadTitleFont()
        {
            var fromResources = Resources.Load<TMP_FontAsset>("Fonts/Cinzel-ExtraBold SDF");
            if (fromResources != null) return fromResources;
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Font/Cinzel-ExtraBold SDF.asset");
        }

        private static TMP_FontAsset LoadBodyFont()
        {
            var bold = Resources.Load<TMP_FontAsset>("Fonts/LiberationSans-Bold SDF");
            if (bold != null) return bold;
            bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Font/LiberationSans-Bold SDF.asset");
            if (bold != null) return bold;
            var regular = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (regular != null) return regular;
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        private static void ApplyFontsInScene(string sceneName, TMP_FontAsset title, TMP_FontAsset body)
        {
            var path = $"{ScenesFolder}/{sceneName}.unity";
            if (!File.Exists(path))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int changed = 0;
            foreach (var text in texts)
            {
                var role = ResolveFontRole(text);
                var next = role == UiFontRole.Title ? title : body;
                if (text.font != next)
                {
                    text.font = next;
                    EditorUtility.SetDirty(text);
                    changed++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Golden Press: updated {changed} TMP text components in {sceneName}.");
        }

        private static UiFontRole ResolveFontRole(TextMeshProUGUI text)
        {
            if (text.GetComponentInParent<Button>(true) != null)
            {
                return UiFontRole.Title;
            }

            switch (text.gameObject.name)
            {
                case "Title":
                case "Hook":
                case "Header":
                case "Label":
                case "MillLabel":
                case "TankLabel":
                case "ShopLabel":
                case "OrderTitle":
                    return UiFontRole.Title;
            }

            if (text.gameObject.name.EndsWith("Label"))
            {
                return UiFontRole.Title;
            }

            return UiFontRole.Body;
        }

        private static void EnsureTmpFontsInResources()
        {
            EnsureResourceCopy(
                "Assets/Art/Font/Cinzel-ExtraBold SDF.asset",
                "Assets/_GoldenPress/Resources/Fonts/Cinzel-ExtraBold SDF.asset");
        }

        private static void EnsureResourceCopy(string src, string dest)
        {
            if (!File.Exists(src))
            {
                Debug.LogWarning($"Golden Press: missing font asset at {src}");
                return;
            }

            if (File.Exists(dest))
            {
                return;
            }

            var folder = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            AssetDatabase.CopyAsset(src, dest);
        }

        private static void EnsureCinzelInResources()
        {
            EnsureTmpFontsInResources();
        }

        /// <summary>Batch-mode entry: Unity -batchmode -executeMethod GoldenPress.EditorTools.GoldenPressUiBaker.BakeAuthoredUiIntoScenesBatch</summary>
        public static void BakeAuthoredUiIntoScenesBatch()
        {
            BakeAuthoredUiIntoScenes();
            EditorApplication.Exit(0);
        }

        private static void BakeScene(string sceneName, SceneEntryPoint.SceneKind kind)
        {
            var path = $"{ScenesFolder}/{sceneName}.unity";
            if (!File.Exists(path))
            {
                Debug.LogError($"Missing scene: {path}. Run Golden Press/Setup Scenes And Build Settings first.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var root = EnsureSceneShell(scene, kind);
            var canvasName = CanvasNameFor(kind);

            // Capture current Hierarchy text layout before the canvas is destroyed,
            // then merge into the persisted override file.
            var existingCanvas = FindCanvas(root.transform, canvasName);
            if (existingCanvas != null)
            {
                UiLayoutOverlay.CaptureAndMerge(existingCanvas, canvasName);
            }

            switch (kind)
            {
                case SceneEntryPoint.SceneKind.Splash:
                {
                    var controller = root.GetComponent<SplashController>() ?? root.AddComponent<SplashController>();
                    var refs = UiSceneBuilders.BuildSplash(root.transform);
                    AttachSafeArea(root.transform, canvasName);
                    UiLayoutOverlay.Apply(FindCanvas(root.transform, canvasName), canvasName);
                    controller.ApplyAuthoredRefs(refs);
                    EditorUtility.SetDirty(controller);
                    break;
                }
                case SceneEntryPoint.SceneKind.MainMill:
                {
                    var controller = root.GetComponent<MillHubController>() ?? root.AddComponent<MillHubController>();
                    var refs = UiSceneBuilders.BuildMillHub(root.transform);
                    AttachSafeArea(root.transform, canvasName);
                    UiLayoutOverlay.Apply(FindCanvas(root.transform, canvasName), canvasName);
                    controller.ApplyAuthoredRefs(refs);
                    EditorUtility.SetDirty(controller);
                    break;
                }
                case SceneEntryPoint.SceneKind.Production:
                {
                    var controller = root.GetComponent<ProductionController>() ?? root.AddComponent<ProductionController>();
                    var refs = UiSceneBuilders.BuildProduction(root.transform);
                    AttachSafeArea(root.transform, canvasName);
                    UiLayoutOverlay.Apply(FindCanvas(root.transform, canvasName), canvasName);
                    controller.ApplyAuthoredRefs(refs);
                    EditorUtility.SetDirty(controller);
                    break;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static string CanvasNameFor(SceneEntryPoint.SceneKind kind)
        {
            switch (kind)
            {
                case SceneEntryPoint.SceneKind.Splash: return "SplashCanvas";
                case SceneEntryPoint.SceneKind.MainMill: return "MillCanvas";
                case SceneEntryPoint.SceneKind.Production: return "ProductionCanvas";
                default: return null;
            }
        }

        private static Transform FindCanvas(Transform host, string canvasName)
        {
            if (host == null || string.IsNullOrEmpty(canvasName))
            {
                return null;
            }

            return host.Find(canvasName);
        }

        private static void AttachSafeArea(Transform host, string canvasName)
        {
            var safe = host.Find($"{canvasName}/SafeRoot");
            if (safe == null)
            {
                return;
            }

            if (safe.GetComponent<SafeAreaWatcher>() == null)
            {
                safe.gameObject.AddComponent<SafeAreaWatcher>();
            }
        }

        private static GameObject EnsureSceneShell(Scene scene, SceneEntryPoint.SceneKind kind)
        {
            GameObject camGo = null;
            GameObject esGo = null;
            GameObject rootGo = null;

            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.GetComponent<Camera>() != null && camGo == null) camGo = go;
                if (go.GetComponent<EventSystem>() != null && esGo == null) esGo = go;
                if (go.GetComponent<SceneEntryPoint>() != null && rootGo == null) rootGo = go;
            }

            if (camGo == null)
            {
                camGo = new GameObject("Main Camera");
                var cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.96f, 0.90f, 0.78f);
                camGo.AddComponent<AudioListener>();
                camGo.transform.position = new Vector3(0f, 0f, -10f);
            }

            if (esGo == null)
            {
                esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            if (rootGo == null)
            {
                rootGo = new GameObject("SceneRoot");
            }

            var entry = rootGo.GetComponent<SceneEntryPoint>() ?? rootGo.AddComponent<SceneEntryPoint>();
            entry.Configure(kind);
            EditorUtility.SetDirty(entry);
            return rootGo;
        }

        private static void EnsureWhiteSpriteAsset()
        {
            var folder = "Assets/_GoldenPress/Resources/Art";
            var path = $"{folder}/ui_white.png";
            if (File.Exists(path))
            {
                return;
            }

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
    }
}
