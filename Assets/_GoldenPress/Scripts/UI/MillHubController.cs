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
        [Header("Authored UI")]
        [SerializeField] private Text moneyText;
        [SerializeField] private Text orderText;
        [SerializeField] private Text storageText;
        [SerializeField] private Text tutorialText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text unlockText;
        [SerializeField] private Text orderDetailBody;
        [SerializeField] private Button inspectButton;
        [SerializeField] private Button produceButton;
        [SerializeField] private Button fulfillButton;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private Button orderCloseButton;
        [SerializeField] private Button upgradesCloseButton;
        [SerializeField] private GameObject orderPanel;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private GameObject tutorialBlocker;
        [SerializeField] private List<Button> upgradeButtons = new List<Button>();
        [SerializeField] private List<Text> upgradeLabels = new List<Text>();
        [SerializeField] private RectTransform punchTarget;

        private GameSession _session;

        private void Start()
        {
            EnsureEventSystem();
            if (GameContext.Instance == null || !GameContext.Instance.IsReady)
            {
                Debug.LogError("GameContext missing.");
                return;
            }

            if (moneyText == null || produceButton == null)
            {
                Debug.LogError("MillHub UI refs missing. Run Golden Press → Bake Authored UI Into Scenes.");
                return;
            }

            _session = GameContext.Instance.Session;
            _session.StateChanged += Refresh;
            if (_session.State.productionSession != null && _session.State.productionSession.committed)
            {
                _session.Production.ClearCommittedSession();
            }

            ArtCatalog.Warm();
            WireUi();
            Refresh();

            if (punchTarget != null)
            {
                StartCoroutine(SimpleTween.ScalePunch(punchTarget, 1.02f, 0.4f));
            }
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.StateChanged -= Refresh;
            }
        }

        private void WireUi()
        {
            inspectButton.onClick.RemoveAllListeners();
            inspectButton.onClick.AddListener(OnInspectOrder);

            produceButton.onClick.RemoveAllListeners();
            produceButton.onClick.AddListener(OnProduce);

            fulfillButton.onClick.RemoveAllListeners();
            fulfillButton.onClick.AddListener(OnFulfill);

            upgradesButton.onClick.RemoveAllListeners();
            upgradesButton.onClick.AddListener(OnOpenUpgrades);

            if (orderCloseButton != null)
            {
                orderCloseButton.onClick.RemoveAllListeners();
                orderCloseButton.onClick.AddListener(OnCloseOrderPanel);
            }

            if (upgradesCloseButton != null)
            {
                upgradesCloseButton.onClick.RemoveAllListeners();
                upgradesCloseButton.onClick.AddListener(() => upgradePanel.SetActive(false));
            }

            var types = (UpgradeType[])System.Enum.GetValues(typeof(UpgradeType));
            for (int i = 0; i < upgradeButtons.Count && i < types.Length; i++)
            {
                var captured = types[i];
                var button = upgradeButtons[i];
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    var result = _session.Upgrades.TryPurchase(captured);
                    SetStatus(result.Message, result.Success);
                    Refresh();
                    RefreshUpgrades();
                });
            }
        }

        private void OnOpenUpgrades()
        {
            if (_session.Tutorial.IsActive && !_session.State.hasSeenPostTutorialReveal)
            {
                SetStatus("Finish the first order first.", false);
                return;
            }

            upgradePanel.SetActive(true);
            RefreshUpgrades();
        }

        private void OnCloseOrderPanel()
        {
            orderPanel.SetActive(false);
            if (_session.Tutorial.CurrentStep == TutorialStep.InspectOrder)
            {
                _session.Tutorial.AdvanceTo(TutorialStep.PurchaseMaterials);
            }
        }

        private void OnInspectOrder()
        {
            var order = _session.Orders.Current;
            var oil = _session.Balance.GetOil(order.oilId);
            if (orderDetailBody != null)
            {
                orderDetailBody.text = $"{order.customerName}\n\nNeeds {order.litersRequired:0} L of {oil.displayName}\nReward: {order.payout} coins";
            }

            orderPanel.SetActive(true);
        }

        private void OnProduce()
        {
            if (_session.Tutorial.IsActive &&
                _session.Tutorial.CurrentStep != TutorialStep.PurchaseMaterials &&
                _session.Tutorial.CurrentStep != TutorialStep.StartProduction &&
                _session.Tutorial.CurrentStep != TutorialStep.CompleteSorting)
            {
                SetStatus("Follow the tutorial step first.", false);
                return;
            }

            if (_session.Production.HasActiveSession)
            {
                SceneManager.LoadScene(SceneNames.Production);
                return;
            }

            var moneyBefore = _session.State.money;
            var fee = GetCurrentProcessingFee();
            var result = _session.Production.TryBeginForCurrentOrder();
            if (result.Success && moneyBefore < fee)
            {
                SetStatus("Father's savings covered materials. Production started.", true);
            }
            else
            {
                SetStatus(result.Message, result.Success);
            }

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
                SetStatus("Produce the oil before delivering.", false);
                return;
            }

            var result = _session.Orders.TryFulfillCurrent();
            SetStatus(result.Message, result.Success);
            if (result.Success)
            {
                if (!_session.State.hasSeenPostTutorialReveal && _session.Tutorial.IsCompleted)
                {
                    SetStatus(result.Message + " Upgrades and new oils will unlock as you grow.", true);
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
            moneyText.text = $"Coins: {_session.State.money}";
            storageText.text = $"Storage: {_session.State.GetTotalStoredLiters():0.#}/{_session.State.storageCapacityLiters:0} L";
            if (order != null && oil != null)
            {
                orderText.text = $"{order.customerName}\n{order.litersRequired:0} L {oil.displayName}\nPay: {order.payout}  |  Owned: {_session.Inventory.GetOilLiters(order.oilId):0.#} L";
            }

            tutorialText.text = _session.Tutorial.IsActive ? _session.Tutorial.GetPrompt() : "Keep Father's mill humming. New orders arrive endlessly.";
            if (tutorialText.transform.parent != null)
            {
                tutorialText.transform.parent.gameObject.SetActive(true);
            }

            var next = _session.Progression.GetNextUnlockCandidate();
            if (next != null)
            {
                var remaining = Mathf.Max(0, next.unlockAfterOrders - _session.State.completedOrderCount);
                unlockText.text = remaining == 0
                    ? $"{next.displayName} unlocked!"
                    : $"Next oil: {next.displayName} in {remaining} order(s)";
            }
            else
            {
                unlockText.text = "All starter oils unlocked.";
            }

            bool tutorial = _session.Tutorial.IsActive;
            var fee = GetCurrentProcessingFee();
            var produceLabel = produceButton.GetComponentInChildren<Text>();
            if (produceLabel != null)
            {
                produceLabel.text = _session.Production.HasActiveSession
                    ? "Continue Oil"
                    : fee > 0 ? $"Make Oil ({fee})" : "Make Oil";
            }

            bool tutorialAllowsProduce = !tutorial
                || _session.Tutorial.CurrentStep == TutorialStep.PurchaseMaterials
                || _session.Tutorial.CurrentStep == TutorialStep.StartProduction
                || _session.Tutorial.CurrentStep == TutorialStep.CompleteSorting
                || _session.Production.HasActiveSession;
            bool canAffordOrResume = _session.Production.HasActiveSession
                || fee <= 0
                || _session.Economy.CanAfford(fee)
                || !_session.Orders.CanFulfillCurrent();
            inspectButton.interactable = !tutorial || _session.Tutorial.CurrentStep == TutorialStep.InspectOrder || _session.Tutorial.CurrentStep >= TutorialStep.PurchaseMaterials;
            produceButton.interactable = tutorialAllowsProduce && canAffordOrResume;
            fulfillButton.interactable = (!tutorial || _session.Tutorial.CurrentStep == TutorialStep.FulfillOrder) && _session.Orders.CanFulfillCurrent();
            upgradesButton.interactable = !tutorial || _session.State.hasSeenPostTutorialReveal || _session.Tutorial.IsCompleted;

            if (!_session.Production.HasActiveSession && fee > 0 && !_session.Economy.CanAfford(fee) && !_session.Orders.CanFulfillCurrent())
            {
                SetStatus($"Need {fee} coins to make oil (have {_session.State.money}). Father's savings will cover the rest.", false);
            }
        }

        private void RefreshUpgrades()
        {
            var types = (UpgradeType[])System.Enum.GetValues(typeof(UpgradeType));
            for (int i = 0; i < types.Length && i < upgradeLabels.Count; i++)
            {
                var type = types[i];
                var def = _session.Balance.GetUpgrade(type);
                var tier = _session.State.GetUpgradeTier(type);
                var cost = _session.Upgrades.GetCost(type);
                var costLabel = cost < 0 ? "MAX" : $"{cost} coins";
                upgradeLabels[i].text = $"{def.displayName}  (Tier {tier}/{def.maxTier})\n{def.description}\n{costLabel}";
                upgradeButtons[i].interactable = _session.Upgrades.CanPurchase(type);
            }
        }

        private int GetCurrentProcessingFee()
        {
            var order = _session.Orders.Current;
            if (order == null || order.isFulfilled)
            {
                return 0;
            }

            var oil = _session.Balance.GetOil(order.oilId);
            if (oil == null)
            {
                return 0;
            }

            return EconomyMath.CalculateProcessingFee(oil, order.litersRequired, order.isTutorialOrder, _session.Balance);
        }

        private void SetStatus(string message, bool success)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message ?? string.Empty;
            statusText.color = success ? GameTheme.Success : GameTheme.Danger;
        }

#if UNITY_EDITOR
        public void ApplyAuthoredRefs(UiSceneBuilders.MillHubRefs refs)
        {
            moneyText = refs.MoneyText;
            orderText = refs.OrderText;
            storageText = refs.StorageText;
            tutorialText = refs.TutorialText;
            statusText = refs.StatusText;
            unlockText = refs.UnlockText;
            orderDetailBody = refs.OrderDetailBody;
            inspectButton = refs.InspectButton;
            produceButton = refs.ProduceButton;
            fulfillButton = refs.FulfillButton;
            upgradesButton = refs.UpgradesButton;
            orderPanel = refs.OrderPanel;
            upgradePanel = refs.UpgradePanel;
            tutorialBlocker = refs.TutorialBlocker;
            punchTarget = refs.PunchTarget;
            upgradeButtons = new List<Button>(refs.UpgradeButtons);
            upgradeLabels = new List<Text>(refs.UpgradeLabels);

            var close = refs.OrderPanel != null
                ? refs.OrderPanel.transform.Find("OrderDetail/Close")?.GetComponent<Button>()
                : null;
            orderCloseButton = close;

            var upClose = refs.UpgradePanel != null
                ? refs.UpgradePanel.transform.Find("UpgradeCard/CloseUpgrades")?.GetComponent<Button>()
                : null;
            upgradesCloseButton = upClose;
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
