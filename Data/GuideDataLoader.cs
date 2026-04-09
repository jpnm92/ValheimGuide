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

        // All stages merged and sorted from every loaded JSON file
        public static List<Stage> AllStages { get; private set; } = new List<Stage>();

        // Detected mod IDs — set by Plugin.cs before Load() is called
        public static HashSet<string> InstalledMods { get; set; } = new HashSet<string>();

        // ─────────────────────────────────────────
        //  ENTRY POINT
        // ─────────────────────────────────────────

        public static void Load(string dataFolderPath, ManualLogSource log)
        {
            _log = log;
            AllStages.Clear();

            if (!Directory.Exists(dataFolderPath))
            {
                _log.LogError($"[GuideDataLoader] Data folder not found: {dataFolderPath}");
                return;
            }

            string[] jsonFiles = Directory.GetFiles(dataFolderPath, "*.json");

            if (jsonFiles.Length == 0)
            {
                _log.LogWarning("[GuideDataLoader] No JSON files found in data folder.");
                return;
            }

            foreach (string filePath in jsonFiles)
            {
                LoadFile(filePath);
            }

            // Sort all stages across all files by their Order field
            AllStages = AllStages
                .OrderBy(s => s.Order)
                .ToList();

            _log.LogInfo($"[GuideDataLoader] Loaded {AllStages.Count} stages from {jsonFiles.Length} files.");
        }

        // ─────────────────────────────────────────
        //  PER FILE
        // ─────────────────────────────────────────

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

                    // Drop the whole stage if it requires a mod that isn't installed
                    if (!string.IsNullOrEmpty(stage.ModRequired) &&
                        !InstalledMods.Contains(stage.ModRequired))
                    {
                        _log.LogInfo($"[GuideDataLoader] Skipping stage '{stage.Id}' — mod '{stage.ModRequired}' not installed.");
                        continue;
                    }

                    // Filter mod-gated entries within the stage
                    stage.Gear = FilterByMod(stage.Gear, g => g.ModRequired);
                    stage.Drops = FilterByMod(stage.Drops, d => d.ModRequired);
                    stage.Recipes = FilterByMod(stage.Recipes, r => r.ModRequired);

                    AllStages.Add(stage);
                    accepted++;
                }

                _log.LogInfo($"[GuideDataLoader] {fileName} → {accepted} stage(s) accepted.");
            }
            catch (JsonException ex)
            {
                _log.LogError($"[GuideDataLoader] JSON parse error in {fileName}: {ex.Message}");
            }
            catch (IOException ex)
            {
                _log.LogError($"[GuideDataLoader] File read error for {fileName}: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────
        //  MOD FILTER
        // ─────────────────────────────────────────

        private static List<T> FilterByMod<T>(List<T> list, Func<T, string> getModRequired)
        {
            if (list == null) return new List<T>();

            return list
                .Where(item =>
                {
                    string mod = getModRequired(item);
                    if (string.IsNullOrEmpty(mod)) return true;           // vanilla entry, always keep
                    return InstalledMods.Contains(mod);                   // keep only if mod present
                })
                .ToList();
        }

        // ─────────────────────────────────────────
        //  VALIDATION
        // ─────────────────────────────────────────

        private static bool ValidateStage(Stage stage, string fileName)
        {
            if (string.IsNullOrEmpty(stage.Id))
            {
                _log.LogWarning($"[GuideDataLoader] Stage in {fileName} is missing Id — skipping.");
                return false;
            }

            if (string.IsNullOrEmpty(stage.Label))
            {
                _log.LogWarning($"[GuideDataLoader] Stage '{stage.Id}' in {fileName} is missing Label — skipping.");
                return false;
            }

            if (stage.UnlockTrigger == null)
            {
                _log.LogWarning($"[GuideDataLoader] Stage '{stage.Id}' in {fileName} has no UnlockTrigger — skipping.");
                return false;
            }

            return true;
        }

        // ─────────────────────────────────────────
        //  HELPERS  (used by ProgressionTracker)
        // ─────────────────────────────────────────

        public static Stage GetStageById(string id)
        {
            return AllStages.FirstOrDefault(s => s.Id == id);
        }

        public static Stage GetNextStage(string currentId)
        {
            int index = AllStages.FindIndex(s => s.Id == currentId);
            if (index < 0 || index >= AllStages.Count - 1) return null;
            return AllStages[index + 1];
        }

        public static DropEntry GetDropEntry(string itemId)
        {
            foreach (Stage stage in AllStages)
            {
                DropEntry entry = stage.Drops.FirstOrDefault(d => d.ItemId == itemId);
                if (entry != null) return entry;
            }
            return null;
        }
    }
}