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
            ArtCatalog.Warm();
            BuildUi();
            _autoLoadAt = Time.unscaledTime + 2.4f;
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

            UiFactory.CreateFullscreenBackground(root, ArtCatalog.SplashHero, GameTheme.Background);
            UiFactory.CreatePanel(root, "Veil", new Color(0.18f, 0.10f, 0.05f, 0.28f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var card = UiFactory.CreateFramedPanel(root, "Card",
                new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.82f), Vector2.zero, Vector2.zero);

            UiFactory.CreateArtImage(card, "Logo", ArtCatalog.LogoMark,
                new Vector2(0.38f, 0.62f), new Vector2(0.62f, 0.88f), Vector2.zero, Vector2.zero);

            var title = UiFactory.CreateText(card, "Title", "Golden Press", 64, GameTheme.WoodDark, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.40f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.58f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var subtitle = UiFactory.CreateText(card, "Subtitle", "Continue Father's legacy", 30, GameTheme.TextMuted, TextAnchor.MiddleCenter);
            subtitle.rectTransform.anchorMin = new Vector2(0.1f, 0.28f);
            subtitle.rectTransform.anchorMax = new Vector2(0.9f, 0.40f);
            subtitle.rectTransform.offsetMin = Vector2.zero;
            subtitle.rectTransform.offsetMax = Vector2.zero;

            var line = UiFactory.CreateText(card, "Subtitle2", "A cozy oil mill for quiet evenings", 24, GameTheme.TextMuted, TextAnchor.MiddleCenter);
            line.rectTransform.anchorMin = new Vector2(0.1f, 0.18f);
            line.rectTransform.anchorMax = new Vector2(0.9f, 0.28f);
            line.rectTransform.offsetMin = Vector2.zero;
            line.rectTransform.offsetMax = Vector2.zero;

            _status = UiFactory.CreateText(root, "Status", "Loading your family mill...", 26, Color.white, TextAnchor.LowerCenter);
            _status.rectTransform.anchorMin = new Vector2(0.2f, 0.14f);
            _status.rectTransform.anchorMax = new Vector2(0.8f, 0.22f);

            var begin = UiFactory.CreateButton(root, "BeginButton", "Enter the Mill", GameTheme.Accent,
                new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.13f), Vector2.zero, Vector2.zero);
            begin.onClick.AddListener(BeginGame);

            StartCoroutine(SimpleTween.ScalePunch(card, 1.03f, 0.35f));
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
