using System;
using System.Collections.Generic;
using GoldenPress.Core;

namespace GoldenPress.Gameplay
{
    public sealed class GameSession
    {
        public GameBalanceConfig Balance { get; }
        public ISaveService SaveService { get; }
        public SaveData State => SaveService.Current;

        public EconomyService Economy { get; }
        public InventoryService Inventory { get; }
        public OrderService Orders { get; }
        public ProgressionService Progression { get; }
        public TutorialService Tutorial { get; }
        public ProductionService Production { get; }
        public UpgradeService Upgrades { get; }

        public event Action StateChanged;

        public GameSession(GameBalanceConfig balance, ISaveService saveService)
        {
            Balance = balance ?? GameBalanceConfig.CreateDefault();
            SaveService = saveService ?? new SaveService();
            SaveService.LoadOrCreate(Balance);

            Economy = new EconomyService(this);
            Inventory = new InventoryService(this);
            Orders = new OrderService(this);
            Progression = new ProgressionService(this);
            Tutorial = new TutorialService(this);
            Production = new ProductionService(this);
            Upgrades = new UpgradeService(this);
        }

        public void NotifyChanged(bool persist = true)
        {
            if (persist)
            {
                SaveService.Save();
            }

            StateChanged?.Invoke();
        }

        public void ResetProgress()
        {
            SaveService.ResetToDefault(Balance);
            NotifyChanged(false);
            StateChanged?.Invoke();
        }
    }

    public sealed class EconomyService
    {
        private readonly GameSession _session;

        public EconomyService(GameSession session)
        {
            _session = session;
        }

        public bool CanAfford(int amount) => _session.State.money >= amount;

        public TransactionResult TrySpend(int amount, string reason)
        {
            if (amount < 0)
            {
                return TransactionResult.Fail("Invalid spend amount.");
            }

            if (!CanAfford(amount))
            {
                return TransactionResult.Fail($"Not enough money. Need {amount} coins (have {_session.State.money}).");
            }

            _session.State.money -= amount;
            _session.NotifyChanged();
            return TransactionResult.Ok(reason);
        }

        public TransactionResult TryAdd(int amount, string reason)
        {
            if (amount < 0)
            {
                return TransactionResult.Fail("Invalid add amount.");
            }

            _session.State.money += amount;
            _session.NotifyChanged();
            return TransactionResult.Ok(reason);
        }

        public float GetRewardBonusMultiplier()
        {
            var tier = _session.State.GetUpgradeTier(UpgradeType.RewardBonus);
            var def = _session.Balance.GetUpgrade(UpgradeType.RewardBonus);
            var perTier = def != null ? def.valuePerTier : _session.Balance.rewardBonusPerUpgradeTier;
            return 1f + tier * perTier;
        }
    }

    public sealed class InventoryService
    {
        private readonly GameSession _session;

        public InventoryService(GameSession session)
        {
            _session = session;
        }

        public float GetOilLiters(string oilId) => _session.State.GetOilLiters(oilId);

        public float RemainingCapacity =>
            Math.Max(0f, _session.State.storageCapacityLiters - _session.State.GetTotalStoredLiters());

        public TransactionResult TryAddOil(string oilId, float liters)
        {
            if (liters <= 0f)
            {
                return TransactionResult.Fail("Nothing to store.");
            }

            if (liters > RemainingCapacity + 0.001f)
            {
                return TransactionResult.Fail("Storage tank is full.");
            }

            var current = _session.State.GetOilLiters(oilId);
            _session.State.SetOilLiters(oilId, current + liters);
            _session.NotifyChanged();
            return TransactionResult.Ok("Oil stored.");
        }

        public TransactionResult TryRemoveOil(string oilId, float liters)
        {
            if (liters <= 0f)
            {
                return TransactionResult.Fail("Invalid oil amount.");
            }

            var current = _session.State.GetOilLiters(oilId);
            if (current + 0.001f < liters)
            {
                return TransactionResult.Fail("Not enough oil in storage.");
            }

            _session.State.SetOilLiters(oilId, current - liters);
            _session.NotifyChanged();
            return TransactionResult.Ok("Oil removed.");
        }

        public void RecalculateStorageCapacity()
        {
            var tier = _session.State.GetUpgradeTier(UpgradeType.StorageCapacity);
            var def = _session.Balance.GetUpgrade(UpgradeType.StorageCapacity);
            var perTier = def != null ? def.valuePerTier : _session.Balance.storagePerUpgradeTier;
            _session.State.storageCapacityLiters = _session.Balance.startingStorageLiters + tier * perTier;
        }
    }

    public sealed class OrderService
    {
        private readonly GameSession _session;
        private readonly System.Random _rng;

        public OrderService(GameSession session)
        {
            _session = session;
            _rng = new System.Random(session.State.orderSeed);
        }

        public CustomerOrder Current => _session.State.currentOrder;

        public bool CanFulfillCurrent()
        {
            var order = Current;
            if (order == null || order.isFulfilled)
            {
                return false;
            }

            return _session.Inventory.GetOilLiters(order.oilId) + 0.001f >= order.litersRequired;
        }

        public TransactionResult TryFulfillCurrent()
        {
            var order = Current;
            if (order == null)
            {
                return TransactionResult.Fail("No active order.");
            }

            if (order.isFulfilled)
            {
                return TransactionResult.Fail("Order already fulfilled.");
            }

            var remove = _session.Inventory.TryRemoveOil(order.oilId, order.litersRequired);
            if (!remove.Success)
            {
                return remove;
            }

            var payout = EconomyMath.CalculatePayout(order, _session.Economy.GetRewardBonusMultiplier());
            order.isFulfilled = true;
            _session.State.completedOrderCount += 1;

            var pay = _session.Economy.TryAdd(payout, "Order fulfilled");
            if (!pay.Success)
            {
                // Extremely unlikely; restore oil to avoid soft-lock.
                _session.Inventory.TryAddOil(order.oilId, order.litersRequired);
                order.isFulfilled = false;
                _session.State.completedOrderCount -= 1;
                return pay;
            }

            _session.Progression.ApplyUnlocksAfterOrder();
            _session.Tutorial.AdvanceTo(TutorialStep.CollectReward);
            GenerateNextOrder();
            _session.NotifyChanged();
            return TransactionResult.Ok($"Earned {payout}");
        }

        public void EnsureCurrentOrder()
        {
            if (_session.State.currentOrder == null || string.IsNullOrEmpty(_session.State.currentOrder.orderId))
            {
                if (_session.State.completedOrderCount == 0)
                {
                    _session.State.currentOrder = SaveData.CreateDefault(_session.Balance).currentOrder;
                }
                else
                {
                    GenerateNextOrder();
                }

                _session.NotifyChanged();
            }
        }

        public CustomerOrder GenerateNextOrder()
        {
            _session.State.orderSeed += 1;
            var localRng = new System.Random(_session.State.orderSeed);

            var unlocked = new List<OilDefinition>();
            for (int i = 0; i < _session.Balance.oils.Count; i++)
            {
                var oil = _session.Balance.oils[i];
                if (_session.State.IsOilUnlocked(oil.id))
                {
                    unlocked.Add(oil);
                }
            }

            if (unlocked.Count == 0)
            {
                unlocked.Add(_session.Balance.GetOil(OilIds.Groundnut));
            }

            var oilChoice = unlocked[localRng.Next(unlocked.Count)];
            var customers = _session.Balance.customers;
            var customer = customers[localRng.Next(customers.Count)];

            var completed = _session.State.completedOrderCount;
            var scale = 1f + completed * _session.Balance.orderScalePerCompleted;
            var minL = (int)Math.Round((double)_session.Balance.baseOrderLitersMin * scale);
            var maxL = (int)Math.Round((double)_session.Balance.baseOrderLitersMax * scale);
            if (maxL < minL) maxL = minL;
            var liters = localRng.Next(minL, maxL + 1);

            var payout = Math.Max(1, (int)Math.Round((double)oilChoice.basePayoutPerLiter * liters));

            var order = new CustomerOrder
            {
                orderId = $"order_{completed + 1:000}_{_session.State.orderSeed}",
                customerId = customer.id,
                customerName = customer.displayName,
                oilId = oilChoice.id,
                litersRequired = liters,
                payout = payout,
                isTutorialOrder = false,
                isFulfilled = false
            };

            _session.State.currentOrder = order;
            return order;
        }
    }
}
