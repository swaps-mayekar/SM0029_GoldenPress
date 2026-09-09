using GoldenPress.UI;
using UnityEngine;

namespace GoldenPress.Bootstrap
{
    /// <summary>
    /// Scene root bootstrap. Controllers expect authored canvas UI (see Golden Press → Bake Authored UI).
    /// </summary>
    public sealed class SceneEntryPoint : MonoBehaviour
    {
        public enum SceneKind
        {
            Splash,
            MainMill,
            Production
        }

        [SerializeField] private SceneKind sceneKind = SceneKind.Splash;

        private void Awake()
        {
            EnsureCamera();
            switch (sceneKind)
            {
                case SceneKind.Splash:
                    if (GetComponent<SplashController>() == null) gameObject.AddComponent<SplashController>();
                    break;
                case SceneKind.MainMill:
                    if (GetComponent<MillHubController>() == null) gameObject.AddComponent<MillHubController>();
                    break;
                case SceneKind.Production:
                    if (GetComponent<ProductionController>() == null) gameObject.AddComponent<ProductionController>();
                    break;
            }
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GameTheme.Background;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        public void Configure(SceneKind kind)
        {
            sceneKind = kind;
        }
    }
}
