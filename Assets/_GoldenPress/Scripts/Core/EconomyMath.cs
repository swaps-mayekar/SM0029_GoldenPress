using System;

namespace GoldenPress.Core
{
    public readonly struct TransactionResult
    {
        public bool Success { get; }
        public string Message { get; }

        public TransactionResult(bool success, string message = "")
        {
            Success = success;
            Message = message ?? string.Empty;
        }

        public static TransactionResult Ok(string message = "") => new TransactionResult(true, message);
        public static TransactionResult Fail(string message) => new TransactionResult(false, message);
    }

    public static class EconomyMath
    {
        public static int CalculateProcessingFee(OilDefinition oil, float liters, bool isTutorial, GameBalanceConfig balance)
        {
            if (isTutorial)
            {
                return balance.tutorialProcessingFee;
            }

            return Math.Max(1, (int)Math.Round(oil.baseProcessingFeePerLiter * (double)liters));
        }

        public static int CalculatePayout(CustomerOrder order, float rewardBonusMultiplier)
        {
            return Math.Max(1, (int)Math.Round(order.payout * (double)rewardBonusMultiplier));
        }

        public static int CalculateUpgradeCost(UpgradeDefinition definition, int currentTier)
        {
            if (currentTier >= definition.maxTier)
            {
                return -1;
            }

            return Math.Max(1, (int)Math.Round(definition.baseCost * Math.Pow(definition.costGrowth, currentTier)));
        }

        public static float CalculateQualityMultiplier(float sorting, float processing, float bottling, GameBalanceConfig balance)
        {
            var average = (Clamp01(sorting) + Clamp01(processing) + Clamp01(bottling)) / 3f;
            return Lerp(balance.minQualityMultiplier, balance.maxQualityMultiplier, average);
        }

        public static float CalculateResultingLiters(float batchLiters, float qualityMultiplier)
        {
            return Math.Max(0.1f, (float)Math.Round(batchLiters * (double)qualityMultiplier, 2));
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }
    }
}
