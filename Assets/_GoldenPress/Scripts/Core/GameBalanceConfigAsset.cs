using GoldenPress.Core;
using UnityEngine;

namespace GoldenPress.Core
{
    [CreateAssetMenu(menuName = "Golden Press/Game Balance Config Asset", fileName = "GameBalanceConfigAsset")]
    public sealed class GameBalanceConfigAsset : ScriptableObject
    {
        public GameBalanceConfig balance = GameBalanceConfig.CreateDefault();

        public GameBalanceConfig GetBalanceOrDefault()
        {
            return balance ?? GameBalanceConfig.CreateDefault();
        }
    }
}
