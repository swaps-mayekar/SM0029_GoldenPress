using GoldenPress.Core;
using UnityEngine;

namespace GoldenPress.Gameplay
{
    public sealed class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }

        public GameBalanceConfig Balance { get; private set; }
        public GameSession Session { get; private set; }
        public bool IsReady { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
            {
                return;
            }

            var go = new GameObject("GameContext");
            DontDestroyOnLoad(go);
            go.AddComponent<GameContext>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            Balance = GameBalanceConfig.CreateDefault();
            Session = new GameSession(Balance, new SaveService());
            Session.Orders.EnsureCurrentOrder();
            Session.Inventory.RecalculateStorageCapacity();
            IsReady = true;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && Session != null)
            {
                Session.SaveService.Save();
            }
        }

        private void OnApplicationQuit()
        {
            if (Session != null)
            {
                Session.SaveService.Save();
            }
        }

        public void ResetForTests(string savePath)
        {
            Balance = GameBalanceConfig.CreateDefault();
            Session = new GameSession(Balance, new SaveService(savePath));
            Session.Orders.EnsureCurrentOrder();
            IsReady = true;
        }
    }
}
