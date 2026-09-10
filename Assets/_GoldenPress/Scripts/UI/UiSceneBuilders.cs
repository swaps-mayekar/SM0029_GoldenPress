using System.Collections.Generic;
using GoldenPress.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenPress.UI
{
    /// <summary>
    /// Builds static UI hierarchies for scene authoring. Controllers bind to the resulting refs
    /// and only refresh text / wire clicks at runtime. Minigame stage content stays dynamic.
    /// Text position/size polish is persisted across rebakes via UiLayoutOverrides.json
    /// (see GoldenPress.EditorTools.UiLayoutOverlay).
    /// </summary>
    public static class UiSceneBuilders
    {
        public sealed class SplashRefs
        {
            public TextMeshProUGUI Status;
            public Button BeginButton;
            public RectTransform PunchTarget;
        }

        public sealed class MillHubRefs
        {
            public TextMeshProUGUI MoneyText;
            public TextMeshProUGUI OrderText;
            public TextMeshProUGUI StorageText;
            public TextMeshProUGUI TutorialText;
            public TextMeshProUGUI StatusText;
            public TextMeshProUGUI UnlockText;
            public TextMeshProUGUI OrderDetailBody;
            public Button InspectButton;
            public Button ProduceButton;
            public Button FulfillButton;
            public Button UpgradesButton;
            public GameObject OrderPanel;
            public GameObject UpgradePanel;
            public GameObject TutorialBlocker;
            public readonly List<Button> UpgradeButtons = new List<Button>();
            public readonly List<TextMeshProUGUI> UpgradeLabels = new List<TextMeshProUGUI>();
            public RectTransform PunchTarget;
        }

        public sealed class ProductionRefs
        {
            public RectTransform Root;
            public TextMeshProUGUI HeaderText;
            public TextMeshProUGUI HintText;
            public TextMeshProUGUI ScoreText;
            public GameObject StageRoot;
            public Button ContinueButton;
            public Button RetryButton;
            public Button ExitButton;
        }

        public static SplashRefs BuildSplash(Transform host)
        {
            ArtCatalog.Warm();
            ClearExistingCanvas(host, "SplashCanvas");

            var canvas = UiFactory.CreateCanvas("SplashCanvas", host);
            var root = UiFactory.CreatePanel(canvas.transform, "SafeRoot", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UiFactory.CreateFullscreenBackground(root, ArtCatalog.SplashHero, GameTheme.Background);
            UiFactory.CreatePanel(root, "Veil", new Color(0.18f, 0.10f, 0.05f, 0.28f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var card = UiFactory.CreateFramedPanel(root, "Card",
                new Vector2(0.16f, 0.12f), new Vector2(0.84f, 0.88f), Vector2.zero, Vector2.zero);

            UiFactory.CreateArtImage(card, "Logo", ArtCatalog.LogoMark,
                new Vector2(0.38f, 0.78f), new Vector2(0.62f, 0.94f), Vector2.zero, Vector2.zero);

            var title = UiFactory.CreateText(card, "Title", "Golden Press", 100, GameTheme.WoodDark, TextAnchor.MiddleCenter, FontStyle.Normal, UiFontRole.Title);
            UiFactory.Place(title.rectTransform, 0.08f, 0.64f, 0.92f, 0.78f);

            var hook = UiFactory.CreateText(card, "Hook", "Father's mill is yours now.", 50, GameTheme.WoodDark, TextAnchor.MiddleCenter, FontStyle.Normal, UiFontRole.Title);
            UiFactory.Place(hook.rectTransform, 0.08f, 0.52f, 0.92f, 0.64f);

            var story = UiFactory.CreateText(card, "Story",
                "After Father suddenly passes away, you take over his small traditional wooden oil mill in town.\n\nOne customer order is already waiting.",
                40, GameTheme.TextMuted, TextAnchor.UpperCenter, FontStyle.Normal, UiFontRole.Body);
            // Authored layout (also persisted in UiLayoutOverrides.json for rebakes).
            story.rectTransform.anchorMin = Vector2.zero;
            story.rectTransform.anchorMax = Vector2.one;
            story.rectTransform.anchoredPosition = new Vector2(0f, -96.5f);
            story.rectTransform.sizeDelta = new Vector2(-400f, -521f);

            var status = UiFactory.CreateText(root, "Status", "Tap below when you are ready.", 35, Color.white, TextAnchor.LowerCenter);
            UiFactory.Place(status.rectTransform, 0.15f, 0.14f, 0.85f, 0.22f);
            status.rectTransform.anchoredPosition = new Vector2(0f, -12.620087f);
            status.rectTransform.sizeDelta = new Vector2(0f, -25.2401f);

            var begin = UiFactory.CreateButton(root, "BeginButton", "Enter the Mill", GameTheme.Accent,
                new Vector2(0.32f, 0.04f), new Vector2(0.68f, 0.12f), Vector2.zero, Vector2.zero);
            begin.GetComponentInChildren<TextMeshProUGUI>().fontSize = 50;

            return new SplashRefs
            {
                Status = status,
                BeginButton = begin,
                PunchTarget = card
            };
        }

        public static MillHubRefs BuildMillHub(Transform host)
        {
            ArtCatalog.Warm();
            ClearExistingCanvas(host, "MillCanvas");

            var refs = new MillHubRefs();
            var canvas = UiFactory.CreateCanvas("MillCanvas", host);
            var root = UiFactory.CreatePanel(canvas.transform, "SafeRoot", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Backdrop
            UiFactory.CreateFullscreenBackground(root, ArtCatalog.HubBackground, GameTheme.Background);
            UiFactory.CreatePanel(root, "SoftVeil", new Color(1f, 0.95f, 0.85f, 0.16f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var mill = UiFactory.CreateFramedPanel(root, "MillSilhouette",
                new Vector2(0.05f, 0.18f), new Vector2(0.40f, 0.72f), Vector2.zero, Vector2.zero);
            UiFactory.CreateArtImage(mill, "PressArt", ArtCatalog.WoodenPress,
                new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.82f), Vector2.zero, Vector2.zero);
            PlaceLabel(mill, "MillLabel", "Father's Wooden Press", 26, GameTheme.WoodDark, 0.84f, 0.98f);
            refs.PunchTarget = mill;

            var tank = UiFactory.CreateFramedPanel(root, "Tank",
                new Vector2(0.42f, 0.20f), new Vector2(0.62f, 0.56f), Vector2.zero, Vector2.zero);
            UiFactory.CreateArtImage(tank, "TankArt", ArtCatalog.OilTank,
                new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.78f), Vector2.zero, Vector2.zero);
            PlaceLabel(tank, "TankLabel", "Oil Tank", 24, GameTheme.TextDark, 0.82f, 0.98f);

            var shop = UiFactory.CreateFramedPanel(root, "Shop",
                new Vector2(0.64f, 0.20f), new Vector2(0.95f, 0.60f), Vector2.zero, Vector2.zero);
            UiFactory.CreateArtImage(shop, "ShopArt", ArtCatalog.FamilyShop,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);
            PlaceLabel(shop, "ShopLabel", "Family Shop", 24, GameTheme.WoodDark, 0.82f, 0.98f);

            // Top bar — split money / storage so they don't collide
            var top = UiFactory.CreateFramedPanel(root, "TopBar",
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.97f), Vector2.zero, Vector2.zero);
            UiFactory.CreateArtImage(top, "CoinIcon", ArtCatalog.Coin,
                new Vector2(0.01f, 0.18f), new Vector2(0.07f, 0.82f), Vector2.zero, Vector2.zero);
            refs.MoneyText = UiFactory.CreateText(top, "Money", "Coins: 0", 32, GameTheme.TextDark, TextAnchor.MiddleLeft);
            UiFactory.Place(refs.MoneyText.rectTransform, 0.08f, 0f, 0.42f, 1f);
            refs.StorageText = UiFactory.CreateText(top, "Storage", "Storage: 0/20 L", 28, GameTheme.TextMuted, TextAnchor.MiddleRight);
            UiFactory.Place(refs.StorageText.rectTransform, 0.44f, 0f, 0.97f, 1f);

            // Center status row
            refs.UnlockText = UiFactory.CreateText(root, "UnlockProgress", "", 24, GameTheme.TextMuted, TextAnchor.MiddleLeft);
            UiFactory.Place(refs.UnlockText.rectTransform, 0.05f, 0.12f, 0.48f, 0.18f);
            refs.StatusText = UiFactory.CreateText(root, "Status", "", 24, GameTheme.Success, TextAnchor.MiddleRight);
            UiFactory.Place(refs.StatusText.rectTransform, 0.50f, 0.12f, 0.95f, 0.18f);

            // Order card — kept clear of top bar and tutorial banner
            var card = UiFactory.CreateFramedPanel(root, "OrderCard",
                new Vector2(0.42f, 0.60f), new Vector2(0.95f, 0.86f), Vector2.zero, Vector2.zero);
            PlaceLabel(card, "OrderTitle", "Current Order", 26, GameTheme.WoodDark, 0.74f, 0.96f, TextAnchor.UpperLeft);
            UiFactory.CreateArtImage(card, "BottleIcon", ArtCatalog.OilBottle,
                new Vector2(0.78f, 0.18f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
            refs.OrderText = UiFactory.CreateText(card, "OrderBody", "", 26, GameTheme.TextDark, TextAnchor.MiddleLeft);
            UiFactory.Place(refs.OrderText.rectTransform, 0.05f, 0.08f, 0.76f, 0.70f);

            // Actions — hub button labels authored at 40 (overrides also in UiLayoutOverrides.json)
            refs.InspectButton = UiFactory.CreateButton(root, "InspectButton", "View Order", GameTheme.Accent,
                new Vector2(0.05f, 0.03f), new Vector2(0.26f, 0.11f), Vector2.zero, Vector2.zero);
            refs.ProduceButton = UiFactory.CreateButton(root, "ProduceButton", "Make Oil", GameTheme.Wood,
                new Vector2(0.28f, 0.03f), new Vector2(0.49f, 0.11f), Vector2.zero, Vector2.zero);
            refs.FulfillButton = UiFactory.CreateButton(root, "FulfillButton", "Fulfill Order", GameTheme.Success,
                new Vector2(0.51f, 0.03f), new Vector2(0.72f, 0.11f), Vector2.zero, Vector2.zero);
            refs.UpgradesButton = UiFactory.CreateButton(root, "UpgradesButton", "Upgrades", GameTheme.PanelDark,
                new Vector2(0.74f, 0.03f), new Vector2(0.95f, 0.11f), Vector2.zero, Vector2.zero);
            SetButtonLabelSize(refs.InspectButton, 40);
            SetButtonLabelSize(refs.ProduceButton, 40);
            SetButtonLabelSize(refs.FulfillButton, 40);
            SetButtonLabelSize(refs.UpgradesButton, 40);

            // Tutorial banner — left column above mill art
            var banner = UiFactory.CreateFramedPanel(root, "TutorialBanner",
                new Vector2(0.05f, 0.74f), new Vector2(0.40f, 0.86f), Vector2.zero, Vector2.zero);
            refs.TutorialText = UiFactory.CreateText(banner, "TutorialText", "", 24, GameTheme.TextDark, TextAnchor.MiddleCenter);
            refs.TutorialText.rectTransform.offsetMin = new Vector2(18, 12);
            refs.TutorialText.rectTransform.offsetMax = new Vector2(-18, -12);

            // Order overlay
            refs.OrderPanel = UiFactory.CreatePanel(root, "OrderPanel", GameTheme.Overlay,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var orderDetail = UiFactory.CreateFramedPanel(refs.OrderPanel.transform, "OrderDetail",
                new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.78f), Vector2.zero, Vector2.zero);
            UiFactory.CreateArtImage(orderDetail, "Logo", ArtCatalog.LogoMark,
                new Vector2(0.42f, 0.72f), new Vector2(0.58f, 0.92f), Vector2.zero, Vector2.zero);
            PlaceLabel(orderDetail, "Title", "Pending Customer Order", 34, GameTheme.WoodDark, 0.58f, 0.72f);
            refs.OrderDetailBody = UiFactory.CreateText(orderDetail, "OrderDetailBody", "", 28, GameTheme.TextDark, TextAnchor.MiddleCenter);
            UiFactory.Place(refs.OrderDetailBody.rectTransform, 0.1f, 0.28f, 0.9f, 0.58f);
            var closeOrder = UiFactory.CreateButton(orderDetail, "Close", "Got it", GameTheme.Accent,
                new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.22f), Vector2.zero, Vector2.zero);
            SetButtonLabelSize(closeOrder, 40);
            refs.OrderPanel.SetActive(false);

            // Upgrade overlay
            refs.UpgradePanel = UiFactory.CreatePanel(root, "UpgradePanel", GameTheme.Overlay,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var upgradeCard = UiFactory.CreateFramedPanel(refs.UpgradePanel.transform, "UpgradeCard",
                new Vector2(0.16f, 0.14f), new Vector2(0.84f, 0.86f), Vector2.zero, Vector2.zero);
            PlaceLabel(upgradeCard, "Title", "Mill Upgrades", 36, GameTheme.WoodDark, 0.88f, 0.98f);

            float y = 0.72f;
            foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
            {
                var button = UiFactory.CreateButton(upgradeCard, type + "Upgrade", "Upgrade", GameTheme.AccentSoft,
                    new Vector2(0.1f, y - 0.14f), new Vector2(0.9f, y), Vector2.zero, Vector2.zero);
                refs.UpgradeButtons.Add(button);
                refs.UpgradeLabels.Add(button.GetComponentInChildren<TextMeshProUGUI>());
                y -= 0.18f;
            }

            UiFactory.CreateButton(upgradeCard, "CloseUpgrades", "Close", GameTheme.Wood,
                new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.14f), Vector2.zero, Vector2.zero);
            refs.UpgradePanel.SetActive(false);

            refs.TutorialBlocker = UiFactory.CreatePanel(root, "TutorialBlocker", new Color(0, 0, 0, 0.01f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            refs.TutorialBlocker.GetComponent<Image>().raycastTarget = true;
            refs.TutorialBlocker.SetActive(false);

            return refs;
        }

        public static ProductionRefs BuildProduction(Transform host)
        {
            ArtCatalog.Warm();
            ClearExistingCanvas(host, "ProductionCanvas");

            var canvas = UiFactory.CreateCanvas("ProductionCanvas", host);
            var root = UiFactory.CreatePanel(canvas.transform, "SafeRoot", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UiFactory.CreateFullscreenBackground(root, ArtCatalog.ProductionBackground, GameTheme.Background);
            UiFactory.CreatePanel(root, "SoftVeil", new Color(1f, 0.94f, 0.84f, 0.22f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var top = UiFactory.CreateFramedPanel(root, "Top",
                new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);
            var header = UiFactory.CreateText(top, "Header", "", 32, GameTheme.TextDark, TextAnchor.MiddleLeft, FontStyle.Normal, UiFontRole.Title);
            header.rectTransform.offsetMin = new Vector2(20, 0);
            header.rectTransform.offsetMax = new Vector2(-280, 0);
            var score = UiFactory.CreateText(top, "Score", "", 28, GameTheme.TextMuted, TextAnchor.MiddleRight);
            score.rectTransform.offsetMin = new Vector2(0, 0);
            score.rectTransform.offsetMax = new Vector2(-20, 0);
            UiFactory.Place(score.rectTransform, 0.55f, 0f, 1f, 1f);
            UiFactory.Place(header.rectTransform, 0f, 0f, 0.55f, 1f);
            header.rectTransform.offsetMin = new Vector2(20, 0);
            score.rectTransform.offsetMax = new Vector2(-20, 0);

            var hint = UiFactory.CreateText(root, "Hint", "", 40, GameTheme.TextDark, TextAnchor.MiddleCenter);
            UiFactory.Place(hint.rectTransform, 0.1f, 0.78f, 0.9f, 0.86f);

            var stageRoot = UiFactory.CreatePanel(root, "StageRoot", Color.clear,
                new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.76f), Vector2.zero, Vector2.zero).gameObject;

            var continueButton = UiFactory.CreateButton(root, "Continue", "Continue", GameTheme.Success,
                new Vector2(0.55f, 0.03f), new Vector2(0.8f, 0.12f), Vector2.zero, Vector2.zero);
            continueButton.gameObject.SetActive(false);

            var retryButton = UiFactory.CreateButton(root, "Retry", "Retry Stage", GameTheme.Accent,
                new Vector2(0.2f, 0.03f), new Vector2(0.45f, 0.12f), Vector2.zero, Vector2.zero);
            retryButton.gameObject.SetActive(false);

            var exitButton = UiFactory.CreateButton(root, "Exit", "Back", GameTheme.Wood,
                new Vector2(0.03f, 0.03f), new Vector2(0.15f, 0.12f), Vector2.zero, Vector2.zero);
            SetButtonLabelSize(continueButton, 40);
            SetButtonLabelSize(retryButton, 40);
            SetButtonLabelSize(exitButton, 40);

            return new ProductionRefs
            {
                Root = root,
                HeaderText = header,
                HintText = hint,
                ScoreText = score,
                StageRoot = stageRoot,
                ContinueButton = continueButton,
                RetryButton = retryButton,
                ExitButton = exitButton
            };
        }

        private static void PlaceLabel(Transform parent, string name, string content, int size, Color color, float minY, float maxY, TextAnchor anchor = TextAnchor.UpperCenter)
        {
            var text = UiFactory.CreateText(parent, name, content, size, color, anchor, FontStyle.Normal, UiFontRole.Title);
            UiFactory.Place(text.rectTransform, 0.05f, minY, 0.95f, maxY);
        }

        private static void SetButtonLabelSize(Button button, int fontSize)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.fontSize = fontSize;
            }
        }

        private static void ClearExistingCanvas(Transform host, string canvasName)
        {
            var existing = host.Find(canvasName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }
    }
}
