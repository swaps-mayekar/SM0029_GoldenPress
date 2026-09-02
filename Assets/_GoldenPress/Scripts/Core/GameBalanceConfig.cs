using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoldenPress.Core
{
    [Serializable]
    public class OilDefinition
    {
        public string id;
        public string displayName;
        public string rawMaterialName;
        public Color oilColor = Color.yellow;
        public Color seedColor = new Color(0.72f, 0.53f, 0.28f);
        public int unlockAfterOrders;
        public int baseProcessingFeePerLiter = 4;
        public int basePayoutPerLiter = 12;
        public float sortingSpeed = 1f;
        public float processingWindow = 0.28f;
        public float bottlingWindow = 0.22f;
    }

    [Serializable]
    public class CustomerDefinition
    {
        public string id;
        public string displayName;
    }

    [Serializable]
    public class UpgradeDefinition
    {
        public UpgradeType upgradeType;
        public string displayName;
        public string description;
        public int maxTier = 5;
        public int baseCost = 40;
        public float costGrowth = 1.55f;
        public float valuePerTier = 1f;
    }

    [Serializable]
    public class GameBalanceConfig
    {
        public int startingMoney = 80;
        public float startingStorageLiters = 20f;
        public float tutorialOrderLiters = 10f;
        public int tutorialOrderPayout = 150;
        public int tutorialProcessingFee = 40;
        public float minQualityMultiplier = 0.85f;
        public float maxQualityMultiplier = 1.15f;
        public float storagePerUpgradeTier = 10f;
        public float forgivenessPerUpgradeTier = 0.04f;
        public float rewardBonusPerUpgradeTier = 0.05f;
        public int baseOrderLitersMin = 8;
        public int baseOrderLitersMax = 14;
        public float orderScalePerCompleted = 0.08f;
        public List<OilDefinition> oils = new List<OilDefinition>();
        public List<CustomerDefinition> customers = new List<CustomerDefinition>();
        public List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();

        public static GameBalanceConfig CreateDefault()
        {
            var config = new GameBalanceConfig();

            config.oils = new List<OilDefinition>
            {
                new OilDefinition
                {
                    id = OilIds.Groundnut,
                    displayName = "Groundnut Oil",
                    rawMaterialName = "Groundnuts",
                    oilColor = new Color(0.95f, 0.78f, 0.28f),
                    seedColor = new Color(0.72f, 0.53f, 0.28f),
                    unlockAfterOrders = 0,
                    baseProcessingFeePerLiter = 4,
                    basePayoutPerLiter = 12,
                    sortingSpeed = 1f,
                    processingWindow = 0.30f,
                    bottlingWindow = 0.24f
                },
                new OilDefinition
                {
                    id = OilIds.Sunflower,
                    displayName = "Sunflower Oil",
                    rawMaterialName = "Sunflower Seeds",
                    oilColor = new Color(1f, 0.88f, 0.35f),
                    seedColor = new Color(0.45f, 0.32f, 0.12f),
                    unlockAfterOrders = 3,
                    baseProcessingFeePerLiter = 5,
                    basePayoutPerLiter = 14,
                    sortingSpeed = 1.05f,
                    processingWindow = 0.28f,
                    bottlingWindow = 0.22f
                },
                new OilDefinition
                {
                    id = OilIds.Mustard,
                    displayName = "Mustard Oil",
                    rawMaterialName = "Mustard Seeds",
                    oilColor = new Color(0.92f, 0.72f, 0.18f),
                    seedColor = new Color(0.55f, 0.42f, 0.08f),
                    unlockAfterOrders = 6,
                    baseProcessingFeePerLiter = 5,
                    basePayoutPerLiter = 15,
                    sortingSpeed = 1.1f,
                    processingWindow = 0.26f,
                    bottlingWindow = 0.21f
                },
                new OilDefinition
                {
                    id = OilIds.Sesame,
                    displayName = "Sesame Oil",
                    rawMaterialName = "Sesame Seeds",
                    oilColor = new Color(0.86f, 0.66f, 0.28f),
                    seedColor = new Color(0.82f, 0.74f, 0.55f),
                    unlockAfterOrders = 9,
                    baseProcessingFeePerLiter = 6,
                    basePayoutPerLiter = 17,
                    sortingSpeed = 1.12f,
                    processingWindow = 0.25f,
                    bottlingWindow = 0.20f
                },
                new OilDefinition
                {
                    id = OilIds.Coconut,
                    displayName = "Coconut Oil",
                    rawMaterialName = "Dried Coconut",
                    oilColor = new Color(0.98f, 0.95f, 0.88f),
                    seedColor = new Color(0.55f, 0.35f, 0.18f),
                    unlockAfterOrders = 12,
                    baseProcessingFeePerLiter = 6,
                    basePayoutPerLiter = 18,
                    sortingSpeed = 1.0f,
                    processingWindow = 0.27f,
                    bottlingWindow = 0.22f
                },
                new OilDefinition
                {
                    id = OilIds.Soybean,
                    displayName = "Soybean Oil",
                    rawMaterialName = "Soybeans",
                    oilColor = new Color(0.94f, 0.84f, 0.42f),
                    seedColor = new Color(0.62f, 0.52f, 0.22f),
                    unlockAfterOrders = 15,
                    baseProcessingFeePerLiter = 5,
                    basePayoutPerLiter = 13,
                    sortingSpeed = 1.08f,
                    processingWindow = 0.27f,
                    bottlingWindow = 0.21f
                }
            };

            config.customers = new List<CustomerDefinition>
            {
                new CustomerDefinition { id = "general_store", displayName = "General Store" },
                new CustomerDefinition { id = "family_kitchen", displayName = "Family Kitchen" },
                new CustomerDefinition { id = "temple_kitchen", displayName = "Temple Kitchen" },
                new CustomerDefinition { id = "snack_stall", displayName = "Snack Stall" },
                new CustomerDefinition { id = "sweet_shop", displayName = "Sweet Shop" },
                new CustomerDefinition { id = "village_cafe", displayName = "Village Cafe" },
                new CustomerDefinition { id = "neighbor_home", displayName = "Neighbor Home" },
                new CustomerDefinition { id = "market_vendor", displayName = "Market Vendor" }
            };

            config.upgrades = new List<UpgradeDefinition>
            {
                new UpgradeDefinition
                {
                    upgradeType = UpgradeType.StorageCapacity,
                    displayName = "Larger Storage Tank",
                    description = "Store more oil between orders.",
                    maxTier = 5,
                    baseCost = 50,
                    costGrowth = 1.6f,
                    valuePerTier = 10f
                },
                new UpgradeDefinition
                {
                    upgradeType = UpgradeType.ProductionForgiveness,
                    displayName = "Steady Press Arm",
                    description = "Slightly wider timing windows in production.",
                    maxTier = 5,
                    baseCost = 60,
                    costGrowth = 1.55f,
                    valuePerTier = 0.04f
                },
                new UpgradeDefinition
                {
                    upgradeType = UpgradeType.RewardBonus,
                    displayName = "Shopfront Charm",
                    description = "Customers pay a little more for your oil.",
                    maxTier = 5,
                    baseCost = 70,
                    costGrowth = 1.65f,
                    valuePerTier = 0.05f
                }
            };

            return config;
        }

        public OilDefinition GetOil(string oilId)
        {
            for (int i = 0; i < oils.Count; i++)
            {
                if (oils[i].id == oilId)
                {
                    return oils[i];
                }
            }

            return null;
        }

        public CustomerDefinition GetCustomer(string customerId)
        {
            for (int i = 0; i < customers.Count; i++)
            {
                if (customers[i].id == customerId)
                {
                    return customers[i];
                }
            }

            return null;
        }

        public UpgradeDefinition GetUpgrade(UpgradeType type)
        {
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i].upgradeType == type)
                {
                    return upgrades[i];
                }
            }

            return null;
        }

        public IEnumerable<OilDefinition> GetUnlockableOils()
        {
            for (int i = 0; i < oils.Count; i++)
            {
                if (oils[i].unlockAfterOrders > 0)
                {
                    yield return oils[i];
                }
            }
        }
    }
}
