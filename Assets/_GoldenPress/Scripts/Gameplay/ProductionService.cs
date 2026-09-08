using System;
using GoldenPress.Core;

namespace GoldenPress.Gameplay
{
    public sealed class ProductionService
    {
        private readonly GameSession _session;

        public ProductionService(GameSession session)
        {
            _session = session;
        }

        public ProductionSession Session => _session.State.productionSession;

        public bool HasActiveSession => Session != null && Session.isActive && !Session.committed;

        public TransactionResult TryBeginForCurrentOrder()
        {
            var order = _session.Orders.Current;
            if (order == null || order.isFulfilled)
            {
                return TransactionResult.Fail("No order to produce for.");
            }

            if (!_session.State.IsOilUnlocked(order.oilId))
            {
                return TransactionResult.Fail("Oil type is locked.");
            }

            if (HasActiveSession)
            {
                return TransactionResult.Ok("Production already in progress.");
            }

            var oil = _session.Balance.GetOil(order.oilId);
            if (oil == null)
            {
                return TransactionResult.Fail("Unknown oil type.");
            }

            var isTutorial = order.isTutorialOrder;
            var fee = EconomyMath.CalculateProcessingFee(oil, order.litersRequired, isTutorial, _session.Balance);

            // Soft-lock escape: if the player cannot afford materials and also cannot
            // fulfill the current order, Father's leftover savings cover the shortfall.
            if (!_session.Economy.CanAfford(fee) && !_session.Orders.CanFulfillCurrent())
            {
                var shortfall = fee - _session.State.money;
                if (shortfall > 0)
                {
                    _session.Economy.TryAdd(shortfall, "Father's savings");
                }
            }

            var spend = _session.Economy.TrySpend(fee, "Processing fee");
            if (!spend.Success)
            {
                return spend;
            }

            Session.isActive = true;
            Session.oilId = order.oilId;
            Session.batchLiters = order.litersRequired;
            Session.processingFeePaid = fee;
            Session.currentStage = ProductionStage.Sorting;
            Session.sortingScore = 0f;
            Session.processingScore = 0f;
            Session.bottlingScore = 0f;
            Session.resultingLiters = 0f;
            Session.qualityMultiplier = 1f;
            Session.committed = false;

            _session.Tutorial.AdvanceTo(TutorialStep.StartProduction);
            _session.Tutorial.AdvanceTo(TutorialStep.CompleteSorting);
            _session.NotifyChanged();
            return TransactionResult.Ok("Production started.");
        }

        public void SetStageScore(ProductionStage stage, float score01)
        {
            if (!HasActiveSession)
            {
                return;
            }

            score01 = Math.Max(0f, Math.Min(1f, score01));
            switch (stage)
            {
                case ProductionStage.Sorting:
                    Session.sortingScore = score01;
                    Session.currentStage = ProductionStage.Processing;
                    _session.Tutorial.AdvanceTo(TutorialStep.CompleteProcessing);
                    break;
                case ProductionStage.Processing:
                    Session.processingScore = score01;
                    Session.currentStage = ProductionStage.Bottling;
                    _session.Tutorial.AdvanceTo(TutorialStep.CompleteBottling);
                    break;
                case ProductionStage.Bottling:
                    Session.bottlingScore = score01;
                    Session.currentStage = ProductionStage.Finished;
                    FinalizeBatch();
                    break;
            }

            _session.NotifyChanged();
        }

        public void RetryCurrentStage()
        {
            // No fee re-charge; player may replay the current unfinished stage.
            _session.NotifyChanged(false);
        }

        private void FinalizeBatch()
        {
            Session.qualityMultiplier = EconomyMath.CalculateQualityMultiplier(
                Session.sortingScore,
                Session.processingScore,
                Session.bottlingScore,
                _session.Balance);

            // Tutorial batches never produce less than required liters.
            var liters = EconomyMath.CalculateResultingLiters(Session.batchLiters, Session.qualityMultiplier);
            var order = _session.Orders.Current;
            if (order != null && order.isTutorialOrder)
            {
                liters = Math.Max(liters, order.litersRequired);
            }

            // Clamp to remaining storage; excess is discarded softly but never soft-locks tutorial.
            var capacity = _session.Inventory.RemainingCapacity;
            if (liters > capacity)
            {
                liters = capacity;
            }

            if (order != null && order.isTutorialOrder && liters < order.litersRequired)
            {
                // Free just enough capacity by not exceeding; if tank somehow full, force enough for tutorial.
                _session.State.SetOilLiters(Session.oilId, 0f);
                liters = order.litersRequired;
            }

            Session.resultingLiters = liters;
            _session.Inventory.TryAddOil(Session.oilId, liters);
            Session.committed = true;
            Session.isActive = false;
            _session.Tutorial.AdvanceTo(TutorialStep.FulfillOrder);
        }

        public void ClearCommittedSession()
        {
            if (Session == null)
            {
                return;
            }

            Session.isActive = false;
            Session.committed = false;
            Session.currentStage = ProductionStage.None;
            _session.NotifyChanged();
        }
    }
}
