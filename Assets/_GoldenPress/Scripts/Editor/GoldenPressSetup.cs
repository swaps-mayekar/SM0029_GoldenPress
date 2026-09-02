using System.IO;
using GoldenPress.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace GoldenPress.EditorTools
{
    public static class GoldenPressSetup
    {
        private const string ScenesFolder = "Assets/Scenes";

        [MenuItem("Golden Press/Setup Scenes And Build Settings")]
        public static void SetupScenesAndBuildSettings()
        {
            if (!Directory.Exists(ScenesFolder))
            {
                Directory.CreateDirectory(ScenesFolder);
            }

            CreateOrUpdateScene("0_SplashScene", SceneEntryPoint.SceneKind.Splash);
            CreateOrUpdateScene("1_MainMillScene", SceneEntryPoint.SceneKind.MainMill);
            CreateOrUpdateScene("2_ProductionScene", SceneEntryPoint.SceneKind.Production);

            var scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/0_SplashScene.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/1_MainMillScene.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/2_ProductionScene.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
            Debug.Log("Golden Press scenes and build settings are ready.");
        }

        [MenuItem("Golden Press/Create Default Balance Asset")]
        public static void CreateBalanceAsset()
        {
            var folder = "Assets/_GoldenPress/Data";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var path = $"{folder}/DefaultGameBalance.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GoldenPress.Core.GameBalanceConfigAsset>(path);
            if (existing == null)
            {
                var asset = ScriptableObject.CreateInstance<GoldenPress.Core.GameBalanceConfigAsset>();
                asset.balance = GoldenPress.Core.GameBalanceConfig.CreateDefault();
                AssetDatabase.CreateAsset(asset, path);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Balance asset ready at {path}");
        }

        private static void CreateOrUpdateScene(string sceneName, SceneEntryPoint.SceneKind kind)
        {
            var path = $"{ScenesFolder}/{sceneName}.unity";
            Scene scene;
            if (File.Exists(path))
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.96f, 0.90f, 0.78f);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var rootGo = new GameObject("SceneRoot");
            var entry = rootGo.AddComponent<SceneEntryPoint>();
            entry.Configure(kind);

            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
