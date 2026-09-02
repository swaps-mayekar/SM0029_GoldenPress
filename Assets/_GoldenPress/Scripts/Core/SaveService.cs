using System;
using System.IO;
using UnityEngine;

namespace GoldenPress.Core
{
    public interface ISaveService
    {
        SaveData Current { get; }
        bool HasSave { get; }
        void LoadOrCreate(GameBalanceConfig balance);
        void Save();
        void ResetToDefault(GameBalanceConfig balance);
        string GetSavePath();
    }

    public class SaveService : ISaveService
    {
        private readonly string _filePath;
        private SaveData _current;

        public SaveData Current => _current;
        public bool HasSave => File.Exists(_filePath);

        public SaveService(string filePath = null)
        {
            _filePath = string.IsNullOrEmpty(filePath)
                ? Path.Combine(Application.persistentDataPath, "golden_press_save.json")
                : filePath;
        }

        public string GetSavePath() => _filePath;

        public void LoadOrCreate(GameBalanceConfig balance)
        {
            if (!HasSave)
            {
                _current = SaveData.CreateDefault(balance);
                Save();
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonUtility.FromJson<SaveData>(json);
                if (loaded == null || loaded.version <= 0)
                {
                    throw new InvalidDataException("Invalid save payload.");
                }

                _current = Migrate(loaded, balance);
                EnsureIntegrity(_current, balance);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Golden Press save recovery: {ex.Message}");
                BackupCorruptSave();
                _current = SaveData.CreateDefault(balance);
                Save();
            }
        }

        public void Save()
        {
            if (_current == null)
            {
                return;
            }

            _current.version = SaveData.CurrentVersion;
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonUtility.ToJson(_current, true);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            File.Move(tempPath, _filePath);
        }

        public void ResetToDefault(GameBalanceConfig balance)
        {
            _current = SaveData.CreateDefault(balance);
            Save();
        }

        private static SaveData Migrate(SaveData data, GameBalanceConfig balance)
        {
            if (data.version < 1)
            {
                data.version = 1;
            }

            if (data.unlockedOilIds == null || data.unlockedOilIds.Count == 0)
            {
                data.unlockedOilIds = new System.Collections.Generic.List<string> { OilIds.Groundnut };
            }

            if (data.oilStock == null)
            {
                data.oilStock = new System.Collections.Generic.List<OilStockEntry>();
            }

            if (data.upgrades == null || data.upgrades.Count == 0)
            {
                data.upgrades = SaveData.CreateDefault(balance).upgrades;
            }

            if (data.productionSession == null)
            {
                data.productionSession = new ProductionSession();
            }

            if (data.currentOrder == null)
            {
                data.currentOrder = SaveData.CreateDefault(balance).currentOrder;
            }

            if (data.storageCapacityLiters <= 0f)
            {
                data.storageCapacityLiters = balance.startingStorageLiters;
            }

            data.version = SaveData.CurrentVersion;
            return data;
        }

        private static void EnsureIntegrity(SaveData data, GameBalanceConfig balance)
        {
            if (data.money < 0)
            {
                data.money = 0;
            }

            if (!data.IsOilUnlocked(OilIds.Groundnut))
            {
                data.unlockedOilIds.Add(OilIds.Groundnut);
            }

            if (data.tutorialStep == TutorialStep.None)
            {
                data.tutorialStep = TutorialStep.InspectOrder;
            }
        }

        private void BackupCorruptSave()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var backup = _filePath + ".corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    File.Copy(_filePath, backup, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not backup corrupt save: {ex.Message}");
            }
        }
    }
}
