using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoldenPress.EditorTools
{
    /// <summary>
    /// Persists authored TextMeshPro layout (RectTransform + fontSize) across UI rebakes.
    /// Keys entries by path relative to the canvas root (e.g. SafeRoot/Card/Title).
    /// </summary>
    public static class UiLayoutOverlay
    {
        public const string OverrideAssetPath = "Assets/_GoldenPress/Data/UiLayoutOverrides.json";

        [Serializable]
        public sealed class Store
        {
            public List<CanvasLayouts> canvases = new List<CanvasLayouts>();
        }

        [Serializable]
        public sealed class CanvasLayouts
        {
            public string canvasName;
            public List<TextLayout> texts = new List<TextLayout>();
        }

        [Serializable]
        public sealed class TextLayout
        {
            public string path;
            public float anchorMinX, anchorMinY, anchorMaxX, anchorMaxY;
            public float pivotX = 0.5f, pivotY = 0.5f;
            public float anchoredPosX, anchoredPosY;
            public float sizeDeltaX, sizeDeltaY;
            public int fontSize;
            public bool hasFontSize;
        }

        [MenuItem("Golden Press/Capture UI Text Layout Overrides")]
        public static void CaptureFromOpenScenesMenu()
        {
            CaptureFromSceneFile("Assets/Scenes/0_SplashScene.unity", "SplashCanvas");
            CaptureFromSceneFile("Assets/Scenes/1_MainMillScene.unity", "MillCanvas");
            CaptureFromSceneFile("Assets/Scenes/2_ProductionScene.unity", "ProductionCanvas");
            Debug.Log($"Golden Press: captured text layout overrides → {OverrideAssetPath}");
        }

        public static void CaptureAndMerge(Transform canvasRoot, string canvasName)
        {
            if (canvasRoot == null)
            {
                return;
            }

            var captured = Capture(canvasRoot);
            // Skip empty captures so a migration bake (legacy UI.Text → TMP) does not wipe overrides.
            if (captured.Count == 0)
            {
                return;
            }

            var store = Load();
            UpsertCanvas(store, canvasName, captured);
            Save(store);
        }

        public static void Apply(Transform canvasRoot, string canvasName)
        {
            if (canvasRoot == null)
            {
                return;
            }

            var store = Load();
            var canvas = FindCanvas(store, canvasName);
            if (canvas == null || canvas.texts == null || canvas.texts.Count == 0)
            {
                return;
            }

            int applied = 0;
            foreach (var entry in canvas.texts)
            {
                var target = canvasRoot.Find(entry.path);
                if (target == null)
                {
                    continue;
                }

                var rt = target as RectTransform ?? target.GetComponent<RectTransform>();
                if (rt == null)
                {
                    continue;
                }

                rt.anchorMin = new Vector2(entry.anchorMinX, entry.anchorMinY);
                rt.anchorMax = new Vector2(entry.anchorMaxX, entry.anchorMaxY);
                rt.pivot = new Vector2(entry.pivotX, entry.pivotY);
                rt.anchoredPosition = new Vector2(entry.anchoredPosX, entry.anchoredPosY);
                rt.sizeDelta = new Vector2(entry.sizeDeltaX, entry.sizeDeltaY);

                if (entry.hasFontSize)
                {
                    var text = target.GetComponent<TMP_Text>();
                    if (text != null)
                    {
                        text.fontSize = entry.fontSize;
                    }
                }

                applied++;
            }

            if (applied > 0)
            {
                Debug.Log($"Golden Press: reapplied {applied} text layout override(s) on {canvasName}.");
            }
        }

        public static List<TextLayout> Capture(Transform canvasRoot)
        {
            var list = new List<TextLayout>();
            var texts = canvasRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                var rt = text.rectTransform;
                var path = GetRelativePath(canvasRoot, rt);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                list.Add(new TextLayout
                {
                    path = path,
                    anchorMinX = rt.anchorMin.x,
                    anchorMinY = rt.anchorMin.y,
                    anchorMaxX = rt.anchorMax.x,
                    anchorMaxY = rt.anchorMax.y,
                    pivotX = rt.pivot.x,
                    pivotY = rt.pivot.y,
                    anchoredPosX = rt.anchoredPosition.x,
                    anchoredPosY = rt.anchoredPosition.y,
                    sizeDeltaX = rt.sizeDelta.x,
                    sizeDeltaY = rt.sizeDelta.y,
                    fontSize = Mathf.RoundToInt(text.fontSize),
                    hasFontSize = true
                });
            }

            return list;
        }

        private static void CaptureFromSceneFile(string scenePath, string canvasName)
        {
            if (!File.Exists(scenePath))
            {
                return;
            }

            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            Transform canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                canvas = root.transform.Find(canvasName);
                if (canvas == null && root.name == canvasName)
                {
                    canvas = root.transform;
                }

                if (canvas == null)
                {
                    var nested = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in nested)
                    {
                        if (t.name == canvasName)
                        {
                            canvas = t;
                            break;
                        }
                    }
                }

                if (canvas != null)
                {
                    break;
                }
            }

            if (canvas != null)
            {
                CaptureAndMerge(canvas, canvasName);
            }
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (target == root)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            if (current != root)
            {
                return null;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        public static Store Load()
        {
            if (!File.Exists(OverrideAssetPath))
            {
                return new Store();
            }

            try
            {
                var json = File.ReadAllText(OverrideAssetPath);
                var store = JsonUtility.FromJson<Store>(json);
                return store ?? new Store();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Golden Press: failed to read layout overrides ({ex.Message}). Starting fresh.");
                return new Store();
            }
        }

        public static void Save(Store store)
        {
            var folder = Path.GetDirectoryName(OverrideAssetPath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var json = JsonUtility.ToJson(store, true);
            File.WriteAllText(OverrideAssetPath, json);
            AssetDatabase.ImportAsset(OverrideAssetPath);
        }

        private static void UpsertCanvas(Store store, string canvasName, List<TextLayout> texts)
        {
            var canvas = FindCanvas(store, canvasName);
            if (canvas == null)
            {
                canvas = new CanvasLayouts { canvasName = canvasName };
                store.canvases.Add(canvas);
            }

            canvas.texts = texts;
        }

        private static CanvasLayouts FindCanvas(Store store, string canvasName)
        {
            if (store?.canvases == null)
            {
                return null;
            }

            foreach (var c in store.canvases)
            {
                if (c != null && c.canvasName == canvasName)
                {
                    return c;
                }
            }

            return null;
        }
    }
}
