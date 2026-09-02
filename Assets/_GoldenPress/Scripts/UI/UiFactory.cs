using UnityEngine;
using UnityEngine.UI;

namespace GoldenPress.UI
{
    public static class GameTheme
    {
        public static readonly Color Background = new Color(0.96f, 0.90f, 0.78f);
        public static readonly Color Panel = new Color(0.99f, 0.95f, 0.86f, 0.96f);
        public static readonly Color PanelDark = new Color(0.78f, 0.55f, 0.30f);
        public static readonly Color Wood = new Color(0.62f, 0.40f, 0.22f);
        public static readonly Color WoodDark = new Color(0.42f, 0.26f, 0.14f);
        public static readonly Color Accent = new Color(0.86f, 0.62f, 0.18f);
        public static readonly Color AccentSoft = new Color(0.95f, 0.80f, 0.42f);
        public static readonly Color TextDark = new Color(0.27f, 0.18f, 0.10f);
        public static readonly Color TextMuted = new Color(0.45f, 0.33f, 0.22f);
        public static readonly Color Success = new Color(0.35f, 0.62f, 0.34f);
        public static readonly Color Danger = new Color(0.75f, 0.28f, 0.22f);
        public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.45f);

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                {
                    return _whiteSprite;
                }

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                tex.Apply();
                tex.filterMode = FilterMode.Bilinear;
                _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
                return _whiteSprite;
            }
        }

        private static Sprite _whiteSprite;

        public static Font DefaultFont
        {
            get
            {
                if (_defaultFont != null)
                {
                    return _defaultFont;
                }

                _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_defaultFont == null)
                {
                    _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return _defaultFont;
            }
        }

        private static Font _defaultFont;
    }

    public static class UiFactory
    {
        public static Canvas CreateCanvas(string name, Transform parent = null)
        {
            var canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null)
            {
                canvasGo.transform.SetParent(parent, false);
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var image = go.GetComponent<Image>();
            image.sprite = GameTheme.WhiteSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return rt;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            Stretch(rt);

            var text = go.GetComponent<Text>();
            text.font = GameTheme.DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var image = go.GetComponent<Image>();
            image.sprite = GameTheme.WhiteSprite;
            image.color = color;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.4f);
            button.colors = colors;

            var luminance = color.r * 0.3f + color.g * 0.59f + color.b * 0.11f;
            var labelColor = luminance < 0.55f ? Color.white : GameTheme.TextDark;
            var text = CreateText(rt, "Label", label, 28, labelColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            var textRt = text.GetComponent<RectTransform>();
            textRt.offsetMin = new Vector2(12, 8);
            textRt.offsetMax = new Vector2(-12, -8);
            return button;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void ApplySafeArea(RectTransform rt)
        {
            var safe = Screen.safeArea;
            var width = Screen.width > 0 ? Screen.width : 1;
            var height = Screen.height > 0 ? Screen.height : 1;
            var min = safe.position;
            var max = safe.position + safe.size;
            min.x /= width;
            min.y /= height;
            max.x /= width;
            max.y /= height;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
