using System.Collections.Generic;
using GoldenPress.Core;
using GoldenPress.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GoldenPress.UI
{
    public sealed class MillHubController : MonoBehaviour
    {
        private GameSession _session;
        private Text _moneyText;
        private Text _orderText;
        private Text _storageText;
        private Text _tutorialText;
        private Text _statusText;
        private Text _unlockText;
        private Button _inspectButton;
        private Button _produceButton;
        private Button _fulfillButton;
        private Button _upgradesButton;
        private GameObject _orderPanel;
        private GameObject _upgradePanel;
        private GameObject _tutorialBlocker;
        private readonly List<Button> _upgradeButtons = new List<Button>();
        private readonly List<Text> _upgradeLabels = new List<Text>();

        private void Start()
        {
            EnsureEventSystem();
            if (GameContext.Instance == null || !GameContext.Instance.IsReady)
            {
                Debug.LogError("GameContext missing.");
                return;
            }

            _session = GameContext.Instance.Session;
            _session.StateChanged += Refresh;
            if (_session.State.productionSession != null && _session.State.productionSession.committed)
            {
                _session.Production.ClearCommittedSession();
            }
            if (GetComponent<AudioService>() == null)
            {
                gameObject.AddComponent<AudioService>();
            }

            AudioService.Instance.Muted = _session.State.muted;
            BuildUi();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.StateChanged -= Refresh;
            }
        }

        private void BuildUi()
        {
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = GameTheme.Background;
            }

            var canvas = UiFactory.CreateCanvas("MillCanvas", transform);
            var root = UiFactory.CreatePanel(canvas.transform, "SafeRoot", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.ApplySafeArea(root);

            BuildBackdrop(root);
            BuildTopBar(root);
            BuildCenterStage(root);
            BuildOrderCard(root);
            BuildActions(root);
            BuildTutorialBanner(root);
            BuildOrderPanel(root);
            BuildUpgradePanel(root);
            BuildTutorialBlocker(root);
        }

        private void BuildBackdrop(RectTransform root)
        {
            var mill = UiFactory.CreatePanel(root, "MillSilhouette", GameTheme.Wood,
                new Vector2(0.08f, 0.18f), new Vector2(0.42f, 0.72f), Vector2.zero, Vector2.zero);
            UiFactory.CreateText(mill, "MillLabel", "Wooden Oil Mill", 28, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);

            var press = UiFactory.CreatePanel(mill, "Press", GameTheme.WoodDark,
                new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.55f), Vector2.zero, Vector2.zero);
            UiFactory.CreateText(press, "PressLabel", "Father's Press", 22, GameTheme.AccentSoft, TextAnchor.MiddleCenter, FontStyle.Bold);

            var tank = UiFactory.CreatePanel(root, "Tank", new Color(0.75f, 0.82f, 0.88f),
                new Vector2(0.45f, 0.22f), new Vector2(0.62f, 0.58f), Vector2.zero, Vector2.zero);
            UiFactory.CreateText(tank, "TankLabel", "Oil Tank", 22, GameTheme.TextDark, TextAnchor.UpperCenter, FontStyle.Bold);

            var shop = UiFactory.CreatePanel(root, "Shop", GameTheme.PanelDark,
                new Vector2(0.66f, 0.22f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);
            UiFactory.CreateText(shop, "ShopLabel", "Family Shop", 24, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
        }

        private void BuildTopBar(RectTransform root)
        {
            var top = UiFactory.CreatePanel(root, "TopBar", GameTheme.Panel,
                new Vector2(0.02f, 0.86f), new Vector2(0.98f, 0.97f), Vector2.zero, Vector2.zero);
            _moneyText = UiFactory.CreateText(top, "Money", "Coins: 0", 30, GameTheme.TextDark, TextAnchor.MiddleLeft, FontStyle.Bold);
            _moneyText.rectTransform.anchorMin = new Vector2(0.02f, 0f);
            _moneyText.rectTransform.anchorMax = new Vector2(0.35f, 1f);

            _storageText = UiFactory.CreateText(top, "Storage", "Storage: 0/20 L", 26, GameTheme.TextMuted, TextAnchor.MiddleCenter);
            _storageText.rectTransform.anchorMin = new Vector2(0.35f, 0f);
            _storageText.rectTransform.anchorMax = new Vector2(0.7f, 1f);

            var mute = UiFactory.CreateButton(top, "MuteButton", "Mute", GameTheme.AccentSoft,
                new Vector2(0.78f, 0.15f), new Vector2(0.97f, 0.85f), Vector2.zero, Vector2.zero);
            mute.onClick.AddListener(() =>
            {
                _session.State.muted = !_session.State.muted;
                AudioService.Instance.Muted = _session.State.muted;
                _session.NotifyChanged();
                Refresh();
            });
        }

        private void BuildCenterStage(RectTransform root)
        {
            _unlockText = UiFactory.CreateText(root, "UnlockProgress", "", 22, GameTheme.TextMuted, TextAnchor.MiddleLeft);
            _unlockText.rectTransform.anchorMin = new Vector2(0.08f, 0.12f);
            _unlockText.rectTransform.anchorMax = new Vector2(0.55f, 0.2f);

            _statusText = UiFactory.CreateText(root, "Status", "", 24, GameTheme.Success, TextAnchor.MiddleRight);
            _statusText.rectTransform.anchorMin = new Vector2(0.55f, 0.12f);
            _statusText.rectTransform.anchorMax = new Vector2(0.92f, 0.2f);
        }

        private void BuildOrderCard(RectTransform root)
        {
            var card = UiFactory.CreatePanel(root, "OrderCard", GameTheme.Panel,
                new Vector2(0.45f, 0.62f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);
            UiFactory.CreateText(card, "OrderTitle", "Current Order", 24, GameTheme.WoodDark, TextAnchor.UpperLeft, FontStyle.Bold)
                .rectTransform.offsetMin = new Vector2(18, -8);
            _orderText = UiFactory.CreateText(card, "OrderBody", "", 24, GameTheme.TextDark, TextAnchor.MiddleLeft);
            _orderText.rectTransform.offsetMin = new Vector2(18, 8);
            _orderText.rectTransform.offsetMax = new Vector2(-18, -36);
        }

        private void BuildActions(RectTransform root)
        {
            _inspectButton = UiFactory.CreateButton(root, "InspectButton", "View Order", GameTheme.Accent,
                new Vector2(0.05f, 0.03f), new Vector2(0.26f, 0.11f), Vector2.zero, Vector2.zero);
            _inspectButton.onClick.AddListener(OnInspectOrder);

            _produceButton = UiFactory.CreateButton(root, "ProduceButton", "Make Oil", GameTheme.Wood,
                new Vector2(0.28f, 0.03f), new Vector2(0.49f, 0.11f), Vector2.zero, Vector2.zero);
            _produceButton.onClick.AddListener(OnProduce);

            _fulfillButton = UiFactory.CreateButton(root, "FulfillButton", "Fulfill Order", GameTheme.Success,
                new Vector2(0.51f, 0.03f), new Vector2(0.72f, 0.11f), Vector2.zero, Vector2.zero);
            _fulfillButton.onClick.AddListener(OnFulfill);

            _upgradesButton = UiFactory.CreateButton(root, "UpgradesButton", "Upgrades", GameTheme.PanelDark,
                new Vector2(0.74f, 0.03f), new Vector2(0.95f, 0.11f), Vector2.zero, Vector2.zero);
            _upgradesButton.onClick.AddListener(() =>
            {
                if (_session.Tutorial.IsActive && !_session.State.hasSeenPostTutorialReveal)
                {
                    _statusText.text = "Finish the first order first.";
                    return;
                }

                _upgradePanel.SetActive(true);
                RefreshUpgrades();
            });
        }

        private void BuildTutorialBanner(RectTransform root)
        {
            var banner = UiFactory.CreatePanel(root, "TutorialBanner", new Color(1f, 0.96f, 0.82f, 0.95f),
                new Vector2(0.08f, 0.74f), new Vector2(0.42f, 0.84f), Vector2.zero, Vector2.zero);
            _tutorialText = UiFactory.CreateText(banner, "TutorialText", "", 22, GameTheme.TextDark, TextAnchor.MiddleCenter);
            _tutorialText.rectTransform.offsetMin = new Vector2(12, 8);
            _tutorialText.rectTransform.offsetMax = new Vector2(-12, -8);
        }

        private void BuildOrderPanel(RectTransform root)
        {
            _orderPanel = UiFactory.CreatePanel(root, "OrderPanel", GameTheme.Overlay,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var card = UiFactory.CreatePanel(_orderPanel.transform, "OrderDetail", GameTheme.Panel,
                new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f), Vector2.zero, Vector2.zero);
            UiFactory.CreateText(card, "Title", "Pending Customer Order", 32, GameTheme.WoodDark, TextAnchor.UpperCenter, FontStyle.Bold)
                .rectTransform.offsetMin = new Vector2(20, -20);
            var body = UiFactory.CreateText(card, "Body", "", 26, GameTheme.TextDark, TextAnchor.MiddleCenter);
            body.rectTransform.offsetMin = new Vector2(30, 70);
            body.rectTransform.offsetMax = new Vector2(-30, -70);
            body.name = "OrderDetailBody";

            var close = UiFactory.CreateButton(card, "Close", "Got it", GameTheme.Accent,
                new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.22f), Vector2.zero, Vector2.zero);
            close.onClick.AddListener(() =>
            {
                _orderPanel.SetActive(false);
                if (_session.Tutorial.CurrentStep == TutorialStep.InspectOrder)
                {
                    _session.Tutorial.AdvanceTo(TutorialStep.PurchaseMaterials);
                }
            });
            _orderPanel.SetActive(false);
        }

        private void BuildUpgradePanel(RectTransform root)
        {
            _upgradePanel = UiFactory.CreatePanel(root, "UpgradePanel", GameTheme.Overlay,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var card = UiFactory.CreatePanel(_upgradePanel.transform, "UpgradeCard", GameTheme.Panel,
                new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
            UiFactory.CreateText(card, "Title", "Mill Upgrades", 34, GameTheme.WoodDark, TextAnchor.UpperCenter, FontStyle.Bold);

            float y = 0.62f;
            foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
            {
                var button = UiFactory.CreateButton(card, type + "Upgrade", "Upgrade", GameTheme.AccentSoft,
                    new Vector2(0.1f, y - 0.12f), new Vector2(0.9f, y), Vector2.zero, Vector2.zero);
                var captured = type;
                button.onClick.AddListener(() =>
                {
                    var result = _session.Upgrades.TryPurchase(captured);
                    _statusText.text = result.Message;
                    Refresh();
                    RefreshUpgrades();
                });
                _upgradeButtons.Add(button);
                _upgradeLabels.Add(button.GetComponentInChildren<Text>());
                y -= 0.16f;
            }

            var close = UiFactory.CreateButton(card, "CloseUpgrades", "Close", GameTheme.Wood,
                new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.14f), Vector2.zero, Vector2.zero);
            close.onClick.AddListener(() => _upgradePanel.SetActive(false));
            _upgradePanel.SetActive(false);
        }

        private void BuildTutorialBlocker(RectTransform root)
        {
            _tutorialBlocker = UiFactory.CreatePanel(root, "TutorialBlocker", new Color(0, 0, 0, 0.01f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            _tutorialBlocker.GetComponent<Image>().raycastTarget = true;
            _tutorialBlocker.SetActive(false);
        }

        private void OnInspectOrder()
        {
            if (_session.Tutorial.IsActive && !_session.Tutorial.IsStepAllowed(TutorialStep.InspectOrder)
                && _session.Tutorial.CurrentStep != TutorialStep.PurchaseMaterials
                && _session.Tutorial.CurrentStep != TutorialStep.FulfillOrder
                && _session.Tutorial.CurrentStep != TutorialStep.StartProduction)
            {
                // Allow viewing order freely after first inspect, but not before required step when on later exclusive steps.
            }

            var order = _session.Orders.Current;
            var oil = _session.Balance.GetOil(order.oilId);
            var body = _orderPanel.transform.Find("OrderDetail/OrderDetailBody")?.GetComponent<Text>();
            if (body != null)
            {
                body.text = $"{order.customerName}\n\nNeeds {order.litersRequired:0} L of {oil.displayName}\nReward: {order.payout} coins";
            }

            _orderPanel.SetActive(true);
            if (_session.Tutorial.CurrentStep == TutorialStep.InspectOrder)
            {
                // Closed via Got it advances tutorial.
            }
        }

        private void OnProduce()
        {
            if (_session.Tutorial.IsActive &&
                _session.Tutorial.CurrentStep != TutorialStep.PurchaseMaterials &&
                _session.Tutorial.CurrentStep != TutorialStep.StartProduction &&
                _session.Tutorial.CurrentStep != TutorialStep.CompleteSorting)
            {
                _statusText.text = "Follow the tutorial step first.";
                return;
            }

            if (_session.Production.HasActiveSession)
            {
                SceneManager.LoadScene(SceneNames.Production);
                return;
            }

            var result = _session.Production.TryBeginForCurrentOrder();
            _statusText.text = result.Message;
            if (!result.Success)
            {
                Refresh();
                return;
            }

            SceneManager.LoadScene(SceneNames.Production);
        }

        private void OnFulfill()
        {
            if (_session.Tutorial.IsActive && _session.Tutorial.CurrentStep != TutorialStep.FulfillOrder)
            {
                _statusText.text = "Produce the oil before delivering.";
                return;
            }

            var result = _session.Orders.TryFulfillCurrent();
            _statusText.text = result.Message;
            if (result.Success)
            {
                AudioService.Instance?.PlayCoin();
                if (!_session.State.hasSeenPostTutorialReveal && _session.Tutorial.IsCompleted)
                {
                    _statusText.text = result.Message + " Upgrades and new oils will unlock as you grow.";
                    _session.Tutorial.MarkPostTutorialRevealSeen();
                }
            }

            Refresh();
        }

        private void Refresh()
        {
            if (_session == null)
            {
                return;
            }

            var order = _session.Orders.Current;
            var oil = order != null ? _session.Balance.GetOil(order.oilId) : null;
            _moneyText.text = $"Coins: {_session.State.money}";
            _storageText.text = $"Storage: {_session.State.GetTotalStoredLiters():0.#}/{_session.State.storageCapacityLiters:0} L";
            if (order != null && oil != null)
            {
                _orderText.text = $"{order.customerName}\n{order.litersRequired:0} L {oil.displayName}\nPay: {order.payout}  |  Owned: {_session.Inventory.GetOilLiters(order.oilId):0.#} L";
            }

            _tutorialText.text = _session.Tutorial.IsActive ? _session.Tutorial.GetPrompt() : "Keep Father's mill humming. New orders arrive endlessly.";
            _tutorialText.transform.parent.gameObject.SetActive(true);

            var next = _session.Progression.GetNextUnlockCandidate();
            if (next != null)
            {
                var remaining = Mathf.Max(0, next.unlockAfterOrders - _session.State.completedOrderCount);
                _unlockText.text = remaining == 0
                    ? $"{next.displayName} unlocked!"
                    : $"Next oil: {next.displayName} in {remaining} order(s)";
            }
            else
            {
                _unlockText.text = "All starter oils unlocked.";
            }

            bool tutorial = _session.Tutorial.IsActive;
            _inspectButton.interactable = !tutorial || _session.Tutorial.CurrentStep == TutorialStep.InspectOrder || _session.Tutorial.CurrentStep >= TutorialStep.PurchaseMaterials;
            _produceButton.interactable = !tutorial || _session.Tutorial.CurrentStep == TutorialStep.PurchaseMaterials || _session.Tutorial.CurrentStep == TutorialStep.StartProduction || _session.Production.HasActiveSession;
            _fulfillButton.interactable = (!tutorial || _session.Tutorial.CurrentStep == TutorialStep.FulfillOrder) && _session.Orders.CanFulfillCurrent();
            _upgradesButton.interactable = !tutorial || _session.State.hasSeenPostTutorialReveal || _session.Tutorial.IsCompleted;

            var muteLabel = _moneyText.transform.parent.Find("MuteButton/Label")?.GetComponent<Text>();
            if (muteLabel != null)
            {
                muteLabel.text = _session.State.muted ? "Unmute" : "Mute";
            }
        }

        private void RefreshUpgrades()
        {
            var types = (UpgradeType[])System.Enum.GetValues(typeof(UpgradeType));
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var def = _session.Balance.GetUpgrade(type);
                var tier = _session.State.GetUpgradeTier(type);
                var cost = _session.Upgrades.GetCost(type);
                var costLabel = cost < 0 ? "MAX" : $"{cost} coins";
                _upgradeLabels[i].text = $"{def.displayName}  (Tier {tier}/{def.maxTier})\n{def.description}\n{costLabel}";
                _upgradeButtons[i].interactable = _session.Upgrades.CanPurchase(type);
            }
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
