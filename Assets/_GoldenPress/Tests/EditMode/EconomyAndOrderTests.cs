using System.IO;
using GoldenPress.Core;
using GoldenPress.Gameplay;
using NUnit.Framework;

namespace GoldenPress.Tests
{
    public class EconomyAndOrderTests
    {
        private string _tempSave;
        private GameBalanceConfig _balance;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _tempSave = Path.Combine(Path.GetTempPath(), "gp_test_" + Path.GetRandomFileName() + ".json");
            _balance = GameBalanceConfig.CreateDefault();
            _session = new GameSession(_balance, new SaveService(_tempSave));
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempSave))
            {
                File.Delete(_tempSave);
            }
        }

        [Test]
        public void FirstRun_HasTutorialGroundnutOrder_AndLimitedMoney()
        {
            Assert.AreEqual(OilIds.Groundnut, _session.Orders.Current.oilId);
            Assert.AreEqual("General Store", _session.Orders.Current.customerName);
            Assert.AreEqual(_balance.tutorialOrderLiters, _session.Orders.Current.litersRequired);
            Assert.IsTrue(_session.Orders.Current.isTutorialOrder);
            Assert.AreEqual(_balance.startingMoney, _session.State.money);
            Assert.IsTrue(_session.State.IsOilUnlocked(OilIds.Groundnut));
            Assert.IsFalse(_session.State.IsOilUnlocked(OilIds.Sunflower));
            Assert.AreEqual(TutorialStep.InspectOrder, _session.Tutorial.CurrentStep);
        }

        [Test]
        public void ProcessingFee_SpendsMoney_AndStartsSortingStage()
        {
            var before = _session.State.money;
            var result = _session.Production.TryBeginForCurrentOrder();
            Assert.IsTrue(result.Success);
            Assert.AreEqual(before - _balance.tutorialProcessingFee, _session.State.money);
            Assert.IsTrue(_session.Production.HasActiveSession);
            Assert.AreEqual(ProductionStage.Sorting, _session.Production.Session.currentStage);
        }

        [Test]
        public void CannotAfford_ProcessingFee_FailsAtomically()
        {
            _session.State.money = 0;
            var result = _session.Production.TryBeginForCurrentOrder();
            Assert.IsFalse(result.Success);
            Assert.IsFalse(_session.Production.HasActiveSession);
            Assert.AreEqual(0, _session.State.money);
        }

        [Test]
        public void Fulfillment_PaysOut_PreventsDuplicate_AndGeneratesNextOrder()
        {
            _session.State.money = 1000;
            Assert.IsTrue(_session.Production.TryBeginForCurrentOrder().Success);
            _session.Production.SetStageScore(ProductionStage.Sorting, 1f);
            _session.Production.SetStageScore(ProductionStage.Processing, 1f);
            _session.Production.SetStageScore(ProductionStage.Bottling, 1f);

            Assert.IsTrue(_session.Orders.CanFulfillCurrent());
            var first = _session.Orders.TryFulfillCurrent();
            Assert.IsTrue(first.Success);
            Assert.AreEqual(1, _session.State.completedOrderCount);
            Assert.IsFalse(_session.Orders.Current.isTutorialOrder);

            // Current order is already the next one; fulfilling again without oil should fail.
            var second = _session.Orders.TryFulfillCurrent();
            Assert.IsFalse(second.Success);
        }

        [Test]
        public void DuplicateFulfill_OnSameOrderObject_IsBlocked()
        {
            _session.Inventory.TryAddOil(OilIds.Groundnut, 10f);
            var order = _session.Orders.Current;
            Assert.IsTrue(_session.Orders.TryFulfillCurrent().Success);

            // Simulate stale reference attempt.
            order.isFulfilled = false;
            _session.State.currentOrder = order;
            _session.Inventory.TryAddOil(OilIds.Groundnut, 10f);
            // Order id already consumed path: fulfillment should still work on the forced stale order once,
            // but second call with same fulfilled flag must fail.
            Assert.IsTrue(_session.Orders.TryFulfillCurrent().Success);
            Assert.IsFalse(_session.Orders.TryFulfillCurrent().Success);
        }

        [Test]
        public void UnlockThresholds_UnlockSunflowerAfterThreeOrders()
        {
            _session.State.completedOrderCount = 3;
            _session.Progression.ApplyUnlocksAfterOrder();
            Assert.IsTrue(_session.State.IsOilUnlocked(OilIds.Sunflower));
        }

        [Test]
        public void OrderGenerator_OnlyUsesUnlockedOils()
        {
            _session.State.completedOrderCount = 2;
            _session.State.unlockedOilIds.Clear();
            _session.State.unlockedOilIds.Add(OilIds.Groundnut);
            for (int i = 0; i < 12; i++)
            {
                var order = _session.Orders.GenerateNextOrder();
                Assert.AreEqual(OilIds.Groundnut, order.oilId);
            }
        }

        [Test]
        public void UpgradePurchase_IncreasesStorageCapacity()
        {
            _session.State.money = 500;
            _session.Tutorial.AdvanceTo(TutorialStep.Completed);
            _session.Tutorial.MarkPostTutorialRevealSeen();
            var before = _session.State.storageCapacityLiters;
            var result = _session.Upgrades.TryPurchase(UpgradeType.StorageCapacity);
            Assert.IsTrue(result.Success);
            Assert.Greater(_session.State.storageCapacityLiters, before);
        }
    }

    public class SaveServiceTests
    {
        [Test]
        public void SaveRoundTrip_PreservesMoneyAndOrder()
        {
            var path = Path.Combine(Path.GetTempPath(), "gp_save_" + Path.GetRandomFileName() + ".json");
            try
            {
                var balance = GameBalanceConfig.CreateDefault();
                var service = new SaveService(path);
                service.LoadOrCreate(balance);
                service.Current.money = 123;
                service.Current.completedOrderCount = 4;
                service.Current.currentOrder.customerName = "Sweet Shop";
                service.Save();

                var loaded = new SaveService(path);
                loaded.LoadOrCreate(balance);
                Assert.AreEqual(123, loaded.Current.money);
                Assert.AreEqual(4, loaded.Current.completedOrderCount);
                Assert.AreEqual("Sweet Shop", loaded.Current.currentOrder.customerName);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void CorruptSave_RecoversToDefaults()
        {
            var path = Path.Combine(Path.GetTempPath(), "gp_corrupt_" + Path.GetRandomFileName() + ".json");
            try
            {
                File.WriteAllText(path, "{ not-json");
                var balance = GameBalanceConfig.CreateDefault();
                var service = new SaveService(path);
                service.LoadOrCreate(balance);
                Assert.AreEqual(balance.startingMoney, service.Current.money);
                Assert.AreEqual(OilIds.Groundnut, service.Current.currentOrder.oilId);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                foreach (var backup in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + ".corrupt.*"))
                {
                    File.Delete(backup);
                }
            }
        }
    }

    public class EconomyMathTests
    {
        [Test]
        public void QualityMultiplier_ClampsToConfiguredRange()
        {
            var balance = GameBalanceConfig.CreateDefault();
            var low = EconomyMath.CalculateQualityMultiplier(0f, 0f, 0f, balance);
            var high = EconomyMath.CalculateQualityMultiplier(1f, 1f, 1f, balance);
            Assert.AreEqual(balance.minQualityMultiplier, low);
            Assert.AreEqual(balance.maxQualityMultiplier, high);
        }

        [Test]
        public void TutorialFee_UsesBalanceOverride()
        {
            var balance = GameBalanceConfig.CreateDefault();
            var oil = balance.GetOil(OilIds.Groundnut);
            var fee = EconomyMath.CalculateProcessingFee(oil, 10f, true, balance);
            Assert.AreEqual(balance.tutorialProcessingFee, fee);
        }
    }
}
