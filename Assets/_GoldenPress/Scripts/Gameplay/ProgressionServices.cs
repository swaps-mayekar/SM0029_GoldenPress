using System.Collections.Generic;
using GoldenPress.Core;

namespace GoldenPress.Gameplay
{
    public sealed class ProgressionService
    {
        private readonly GameSession _session;

        public ProgressionService(GameSession session)
        {
            _session = session;
        }

        public IReadOnlyList<OilDefinition> NewlyUnlockedOils { get; private set; } = new List<OilDefinition>();

        public void ApplyUnlocksAfterOrder()
        {
            var unlockedNow = new List<OilDefinition>();
            var completed = _session.State.completedOrderCount;

            for (int i = 0; i < _session.Balance.oils.Count; i++)
            {
                var oil = _session.Balance.oils[i];
                if (oil.unlockAfterOrders <= 0)
                {
                    continue;
                }

                if (completed >= oil.unlockAfterOrders && !_session.State.IsOilUnlocked(oil.id))
                {
                    _session.State.unlockedOilIds.Add(oil.id);
                    unlockedNow.Add(oil);
                }
            }

            NewlyUnlockedOils = unlockedNow;
        }

        public OilDefinition GetNextUnlockCandidate()
        {
            OilDefinition next = null;
            for (int i = 0; i < _session.Balance.oils.Count; i++)
            {
                var oil = _session.Balance.oils[i];
                if (_session.State.IsOilUnlocked(oil.id))
                {
                    continue;
                }

                if (next == null || oil.unlockAfterOrders < next.unlockAfterOrders)
                {
                    next = oil;
                }
            }

            return next;
        }

        public float GetForgivenessBonus()
        {
            var tier = _session.State.GetUpgradeTier(UpgradeType.ProductionForgiveness);
            var def = _session.Balance.GetUpgrade(UpgradeType.ProductionForgiveness);
            var perTier = def != null ? def.valuePerTier : _session.Balance.forgivenessPerUpgradeTier;
            return tier * perTier;
        }
    }

    public sealed class UpgradeService
    {
        private readonly GameSession _session;

        public UpgradeService(GameSession session)
        {
            _session = session;
        }

        public int GetCost(UpgradeType type)
        {
            var def = _session.Balance.GetUpgrade(type);
            if (def == null)
            {
                return -1;
            }

            return EconomyMath.CalculateUpgradeCost(def, _session.State.GetUpgradeTier(type));
        }

        public bool CanPurchase(UpgradeType type)
        {
            var cost = GetCost(type);
            return cost > 0 && _session.Economy.CanAfford(cost);
        }

        public TransactionResult TryPurchase(UpgradeType type)
        {
            if (_session.Tutorial.IsActive && !_session.State.hasSeenPostTutorialReveal)
            {
                return TransactionResult.Fail("Finish the first order before upgrading.");
            }

            var def = _session.Balance.GetUpgrade(type);
            if (def == null)
            {
                return TransactionResult.Fail("Unknown upgrade.");
            }

            var tier = _session.State.GetUpgradeTier(type);
            if (tier >= def.maxTier)
            {
                return TransactionResult.Fail("Upgrade already maxed.");
            }

            var cost = EconomyMath.CalculateUpgradeCost(def, tier);
            var spend = _session.Economy.TrySpend(cost, "Upgrade purchased");
            if (!spend.Success)
            {
                return spend;
            }

            _session.State.SetUpgradeTier(type, tier + 1);
            if (type == UpgradeType.StorageCapacity)
            {
                _session.Inventory.RecalculateStorageCapacity();
            }

            _session.NotifyChanged();
            return TransactionResult.Ok($"{def.displayName} upgraded.");
        }
    }

    public sealed class TutorialService
    {
        private readonly GameSession _session;

        public TutorialService(GameSession session)
        {
            _session = session;
        }

        public TutorialStep CurrentStep => _session.State.tutorialStep;
        public bool IsActive => CurrentStep != TutorialStep.Completed && CurrentStep != TutorialStep.None;
        public bool IsCompleted => CurrentStep == TutorialStep.Completed;

        public bool IsStepAllowed(TutorialStep required)
        {
            if (!IsActive)
            {
                return true;
            }

            return CurrentStep == required;
        }

        public void AdvanceTo(TutorialStep step)
        {
            if (IsCompleted)
            {
                return;
            }

            if ((int)step >= (int)CurrentStep)
            {
                _session.State.tutorialStep = step;
                if (step == TutorialStep.CollectReward)
                {
                    _session.State.tutorialStep = TutorialStep.Completed;
                    _session.State.hasSeenPostTutorialReveal = false;
                }

                _session.NotifyChanged();
            }
        }

        public void MarkPostTutorialRevealSeen()
        {
            _session.State.hasSeenPostTutorialReveal = true;
            _session.NotifyChanged();
        }

        public string GetPrompt()
        {
            switch (CurrentStep)
            {
                case TutorialStep.InspectOrder:
                    return "Father left one pending order. Open it and see what the General Store needs.";
                case TutorialStep.PurchaseMaterials:
                    return "Pay the processing fee to buy groundnuts for this batch.";
                case TutorialStep.StartProduction:
                    return "Start production and make the oil on Father's wooden press.";
                case TutorialStep.CompleteSorting:
                    return "Tap the good groundnuts as they fall. Leave the debris alone.";
                case TutorialStep.CompleteProcessing:
                    return "Keep the press pressure in the golden zone.";
                case TutorialStep.CompleteBottling:
                    return "Fill each bottle inside the target band.";
                case TutorialStep.FulfillOrder:
                    return "Deliver the oil and keep Father's mill running.";
                case TutorialStep.CollectReward:
                case TutorialStep.Completed:
                    return "Well done. Father's mill lives on through you. New orders will keep coming.";
                default:
                    return string.Empty;
            }
        }
    }
}
