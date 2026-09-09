using GoldenPress.Core;
using GoldenPress.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GoldenPress.UI
{
    public sealed class SplashController : MonoBehaviour
    {
        [Header("Authored UI")]
        [SerializeField] private Text status;
        [SerializeField] private Button beginButton;
        [SerializeField] private RectTransform punchTarget;

        private bool _loading;
        private float _autoLoadAt = float.PositiveInfinity;

        private void Start()
        {
            EnsureEventSystem();
            ArtCatalog.Warm();
            WireUi();

            var returning = GameContext.Instance != null
                && GameContext.Instance.IsReady
                && GameContext.Instance.Session.Tutorial.IsCompleted;
            if (returning)
            {
                _autoLoadAt = Time.unscaledTime + 2.4f;
            }

            if (punchTarget != null)
            {
                StartCoroutine(SimpleTween.ScalePunch(punchTarget, 1.03f, 0.35f));
            }
        }

        private void WireUi()
        {
            if (beginButton != null)
            {
                beginButton.onClick.RemoveListener(BeginGame);
                beginButton.onClick.AddListener(BeginGame);
            }
        }

        private void Update()
        {
            if (!_loading && Time.unscaledTime >= _autoLoadAt)
            {
                BeginGame();
            }
        }

        private void BeginGame()
        {
            if (_loading)
            {
                return;
            }

            _loading = true;
            if (status != null)
            {
                status.text = "Opening the mill...";
            }

            SceneManager.LoadScene(SceneNames.MainMill);
        }

#if UNITY_EDITOR
        public void ApplyAuthoredRefs(UiSceneBuilders.SplashRefs refs)
        {
            status = refs.Status;
            beginButton = refs.BeginButton;
            punchTarget = refs.PunchTarget;
        }
#endif

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }
    }
}
