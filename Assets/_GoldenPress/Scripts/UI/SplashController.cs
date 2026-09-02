using GoldenPress.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GoldenPress.UI
{
    public sealed class SplashController : MonoBehaviour
    {
        private bool _loading;
        private Text _status;
        private float _autoLoadAt;

        private void Start()
        {
            EnsureEventSystem();
            BuildUi();
            _autoLoadAt = Time.unscaledTime + 2f;
        }

        private void Update()
        {
            if (_loading)
            {
                return;
            }

            if (Time.unscaledTime >= _autoLoadAt)
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
            if (_status != null)
            {
                _status.text = "Opening the mill...";
            }

            SceneManager.LoadScene(SceneNames.MainMill);
        }

        private void BuildUi()
        {
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = GameTheme.Background;
            }

            var canvas = UiFactory.CreateCanvas("SplashCanvas", transform);
            var root = UiFactory.CreatePanel(canvas.transform, "SafeRoot", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.ApplySafeArea(root);

            var card = UiFactory.CreatePanel(root, "Card", GameTheme.Panel,
                new Vector2(0.2f, 0.22f), new Vector2(0.8f, 0.78f), Vector2.zero, Vector2.zero);

            UiFactory.CreateText(card, "Title", "Golden Press", 64, GameTheme.WoodDark, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.offsetMax = new Vector2(-20, -40);
            UiFactory.CreateText(card, "Subtitle", "Continue Father's legacy", 30, GameTheme.TextMuted, TextAnchor.MiddleCenter)
                .rectTransform.offsetMin = new Vector2(20, 80);
            UiFactory.CreateText(card, "Subtitle2", "A cozy oil mill for quiet evenings", 24, GameTheme.TextMuted, TextAnchor.MiddleCenter)
                .rectTransform.offsetMin = new Vector2(20, 30);

            _status = UiFactory.CreateText(root, "Status", "Loading your family mill...", 26, GameTheme.TextDark, TextAnchor.LowerCenter);
            _status.rectTransform.anchorMin = new Vector2(0.2f, 0.16f);
            _status.rectTransform.anchorMax = new Vector2(0.8f, 0.24f);

            var begin = UiFactory.CreateButton(root, "BeginButton", "Enter the Mill", GameTheme.Accent,
                new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.14f), Vector2.zero, Vector2.zero);
            begin.onClick.AddListener(BeginGame);
        }

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
