using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Newtonsoft.Json;
using ValheimGuide.Data;

namespace ValheimGuide.Data
{
    public static class GuideDataLoader
    {
        private static ManualLogSource _log;

        private static readonly List<Stage> _allStages = new List<Stage>();
        public static IReadOnlyList<Stage> AllStages => _allStages;
        public static HashSet<string> InstalledMods { get; set; } = new HashSet<string>();

        public static void Load(string dataFolderPath, ManualLogSource log)
        {
            _log = log;
            _allStages.Clear();

            if (!Directory.Exists(dataFolderPath))
            {
                _log.LogError($"[GuideDataLoader] Data folder not found: {dataFolderPath}");
                return;
            }

            string[] guideFiles = Directory.GetFiles(dataFolderPath, "*.guide");

            if (guideFiles.Length == 0)
            {
                _log.LogWarning("[GuideDataLoader] No .guide files found in data folder.");
                return;
            }

            foreach (string filePath in guideFiles)
            {
                LoadFile(filePath);
            }

            _allStages.Sort((a, b) => a.Order.CompareTo(b.Order));

            _log.LogInfo($"[GuideDataLoader] Loaded {_allStages.Count} stages from {guideFiles.Length} files.");
        }

        private static void LoadFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            try
            {
                string raw = File.ReadAllText(filePath);
                GuideData data = JsonConvert.DeserializeObject<GuideData>(raw);

                if (data == null || data.Stages == null || data.Stages.Count == 0)
                {
                    _log.LogWarning($"[GuideDataLoader] {fileName} deserialized empty — skipping.");
                    return;
                }

                int accepted = 0;

                foreach (Stage stage in data.Stages)
                {
                    if (!ValidateStage(stage, fileName))
                        continue;

                    if (!string.IsNullOrEmpty(stage.ModRequired) &&
                        !InstalledMods.Contains(stage.ModRequired))
                    {
                        continue;
                    }

                    stage.Gear = FilterByMod(stage.Gear, g => g.ModRequired);
                    stage.Drops = FilterByMod(stage.Drops, d => d.ModRequired);
                    stage.Recipes = FilterByMod(stage.Recipes, r => r.ModRequired);

                    _allStages.Add(stage);
                    accepted++;
                }

                _log.LogInfo($"[GuideDataLoader] {fileName} → {accepted} stage(s) accepted.");
            }
            catch (JsonException ex)
            {
                _log.LogError($"[GuideDataLoader] Parse error in {fileName}: {ex.Message}");
            }
            catch (IOException ex)
            {
                _log.LogError($"[GuideDataLoader] File read error for {fileName}: {ex.Message}");
            }
        }

        private static List<T> FilterByMod<T>(List<T> list, Func<T, string> getModRequired)
        {
            if (list == null) return new List<T>();

            return list
                .Where(item =>
                {
                    string mod = getModRequired(item);
                    if (string.IsNullOrEmpty(mod)) return true;
                    return InstalledMods.Contains(mod);
                })
                .ToList();
        }

        private static bool ValidateStage(Stage stage, string fileName)
        {
            if (string.IsNullOrEmpty(stage.Id)) return false;
            if (string.IsNullOrEmpty(stage.Label)) return false;

            if (stage.UnlockTrigger == null) return false;

            return true;
        }

        public static Stage GetStageById(string id)
        {
            return _allStages.FirstOrDefault(s => s.Id == id);
        }

        public static Stage GetNextStage(string currentId)
        {
            int index = _allStages.FindIndex(s => s.Id == currentId);
            if (index < 0 || index >= _allStages.Count - 1) return null;
            return _allStages[index + 1];
        }

        public static DropEntry GetDropEntry(string itemId)
        {
            foreach (Stage stage in _allStages)
            {
                DropEntry entry = stage.Drops.FirstOrDefault(d => d.ItemId == itemId);
                if (entry != null) return entry;
            }
            return null;
        }
    }
}