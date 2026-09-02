using System;
using System.Collections.Generic;

namespace GoldenPress.Core
{
    public static class OilIds
    {
        public const string Groundnut = "groundnut";
        public const string Sunflower = "sunflower";
        public const string Mustard = "mustard";
        public const string Sesame = "sesame";
        public const string Coconut = "coconut";
        public const string Soybean = "soybean";
    }

    public static class SceneNames
    {
        public const string Splash = "0_SplashScene";
        public const string MainMill = "1_MainMillScene";
        public const string Production = "2_ProductionScene";
    }

    public enum TutorialStep
    {
        None = 0,
        InspectOrder = 1,
        PurchaseMaterials = 2,
        StartProduction = 3,
        CompleteSorting = 4,
        CompleteProcessing = 5,
        CompleteBottling = 6,
        FulfillOrder = 7,
        CollectReward = 8,
        Completed = 9
    }

    public enum ProductionStage
    {
        None = 0,
        Sorting = 1,
        Processing = 2,
        Bottling = 3,
        Finished = 4
    }

    public enum UpgradeType
    {
        StorageCapacity = 0,
        ProductionForgiveness = 1,
        RewardBonus = 2
    }

    [Serializable]
    public class OilStockEntry
    {
        public string oilId;
        public float liters;
    }

    [Serializable]
    public class UpgradeTierEntry
    {
        public UpgradeType upgradeType;
        public int tier;
    }

    [Serializable]
    public class CustomerOrder
    {
        public string orderId;
        public string customerId;
        public string customerName;
        public string oilId;
        public float litersRequired;
        public int payout;
        public bool isTutorialOrder;
        public bool isFulfilled;
    }

    [Serializable]
    public class ProductionSession
    {
        public bool isActive;
        public string oilId;
        public float batchLiters;
        public int processingFeePaid;
        public ProductionStage currentStage;
        public float sortingScore;
        public float processingScore;
        public float bottlingScore;
        public float resultingLiters;
        public float qualityMultiplier;
        public bool committed;
    }

    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int money;
        public float storageCapacityLiters;
        public List<OilStockEntry> oilStock = new List<OilStockEntry>();
        public List<string> unlockedOilIds = new List<string>();
        public List<UpgradeTierEntry> upgrades = new List<UpgradeTierEntry>();
        public int completedOrderCount;
        public TutorialStep tutorialStep = TutorialStep.InspectOrder;
        public CustomerOrder currentOrder;
        public ProductionSession productionSession = new ProductionSession();
        public int orderSeed = 1;
        public bool muted;
        public bool hasSeenPostTutorialReveal;

        public static SaveData CreateDefault(GameBalanceConfig balance)
        {
            var data = new SaveData
            {
                version = CurrentVersion,
                money = balance.startingMoney,
                storageCapacityLiters = balance.startingStorageLiters,
                completedOrderCount = 0,
                tutorialStep = TutorialStep.InspectOrder,
                orderSeed = 1,
                muted = false,
                hasSeenPostTutorialReveal = false,
                unlockedOilIds = new List<string> { OilIds.Groundnut },
                oilStock = new List<OilStockEntry>(),
                upgrades = new List<UpgradeTierEntry>
                {
                    new UpgradeTierEntry { upgradeType = UpgradeType.StorageCapacity, tier = 0 },
                    new UpgradeTierEntry { upgradeType = UpgradeType.ProductionForgiveness, tier = 0 },
                    new UpgradeTierEntry { upgradeType = UpgradeType.RewardBonus, tier = 0 }
                },
                productionSession = new ProductionSession(),
                currentOrder = new CustomerOrder
                {
                    orderId = "order_tutorial_001",
                    customerId = "general_store",
                    customerName = "General Store",
                    oilId = OilIds.Groundnut,
                    litersRequired = balance.tutorialOrderLiters,
                    payout = balance.tutorialOrderPayout,
                    isTutorialOrder = true,
                    isFulfilled = false
                }
            };

            return data;
        }

        public float GetOilLiters(string oilId)
        {
            for (int i = 0; i < oilStock.Count; i++)
            {
                if (oilStock[i].oilId == oilId)
                {
                    return oilStock[i].liters;
                }
            }

            return 0f;
        }

        public void SetOilLiters(string oilId, float liters)
        {
            for (int i = 0; i < oilStock.Count; i++)
            {
                if (oilStock[i].oilId == oilId)
                {
                    oilStock[i].liters = Math.Max(0f, liters);
                    return;
                }
            }

            oilStock.Add(new OilStockEntry { oilId = oilId, liters = Math.Max(0f, liters) });
        }

        public int GetUpgradeTier(UpgradeType type)
        {
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i].upgradeType == type)
                {
                    return upgrades[i].tier;
                }
            }

            return 0;
        }

        public void SetUpgradeTier(UpgradeType type, int tier)
        {
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i].upgradeType == type)
                {
                    upgrades[i].tier = Math.Max(0, tier);
                    return;
                }
            }

            upgrades.Add(new UpgradeTierEntry { upgradeType = type, tier = Math.Max(0, tier) });
        }

        public bool IsOilUnlocked(string oilId)
        {
            return unlockedOilIds != null && unlockedOilIds.Contains(oilId);
        }

        public float GetTotalStoredLiters()
        {
            float total = 0f;
            for (int i = 0; i < oilStock.Count; i++)
            {
                total += oilStock[i].liters;
            }

            return total;
        }
    }
}
