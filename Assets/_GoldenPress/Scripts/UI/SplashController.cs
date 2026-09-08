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
        private bool _loading;
        private Text _status;
        private float _autoLoadAt = float.PositiveInfinity;

        private void Start()
        {
            EnsureEventSystem();
            ArtCatalog.Warm();
            BuildUi();

            // Returning players can skip ahead; first-time players should read the story.
            var returning = GameContext.Instance != null
                && GameContext.Instance.IsReady
                && GameContext.Instance.Session.Tutorial.IsCompleted;
            if (returning)
            {
                _autoLoadAt = Time.unscaledTime + 2.4f;
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
                new Vector2(0.16f, 0.12f), new Vector2(0.84f, 0.88f), Vector2.zero, Vector2.zero);

            UiFactory.CreateArtImage(card, "Logo", ArtCatalog.LogoMark,
                new Vector2(0.38f, 0.78f), new Vector2(0.62f, 0.94f), Vector2.zero, Vector2.zero);

            var title = UiFactory.CreateText(card, "Title", "Golden Press", 58, GameTheme.WoodDark, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.64f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.78f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var hook = UiFactory.CreateText(card, "Hook", "Father's mill is yours now.", 30, GameTheme.WoodDark, TextAnchor.MiddleCenter, FontStyle.Bold);
            hook.rectTransform.anchorMin = new Vector2(0.08f, 0.52f);
            hook.rectTransform.anchorMax = new Vector2(0.92f, 0.64f);
            hook.rectTransform.offsetMin = Vector2.zero;
            hook.rectTransform.offsetMax = Vector2.zero;

            var story = UiFactory.CreateText(card, "Story",
                "After Father suddenly passes away, you take over his small traditional wooden oil mill in town.\n\nOne customer order is already waiting.",
                24, GameTheme.TextMuted, TextAnchor.UpperCenter);
            story.rectTransform.anchorMin = new Vector2(0.1f, 0.22f);
            story.rectTransform.anchorMax = new Vector2(0.9f, 0.52f);
            story.rectTransform.offsetMin = Vector2.zero;
            story.rectTransform.offsetMax = Vector2.zero;
            story.horizontalOverflow = HorizontalWrapMode.Wrap;
            story.verticalOverflow = VerticalWrapMode.Overflow;

            _status = UiFactory.CreateText(root, "Status", "Tap below when you are ready.", 24, Color.white, TextAnchor.LowerCenter);
            _status.rectTransform.anchorMin = new Vector2(0.15f, 0.14f);
            _status.rectTransform.anchorMax = new Vector2(0.85f, 0.22f);

            var begin = UiFactory.CreateButton(root, "BeginButton", "Enter the Mill", GameTheme.Accent,
                new Vector2(0.32f, 0.04f), new Vector2(0.68f, 0.12f), Vector2.zero, Vector2.zero);
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
