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

        // Playstyles loaded separately from playstyles.json
        private static readonly List<PlaystyleDefinition> _playstyles = new List<PlaystyleDefinition>();
        public static IReadOnlyList<PlaystyleDefinition> Playstyles => _playstyles;

        public static void Load(string dataFolderPath, ManualLogSource log)
        {
            _log = log;
            _allStages.Clear();
            _playstyles.Clear();

            if (!Directory.Exists(dataFolderPath))
            {
                _log.LogError($"[GuideDataLoader] Data folder not found: {dataFolderPath}");
                return;
            }

            // Load playstyles first — needed before stages so UI can reference them
            LoadPlaystyles(dataFolderPath);

            string[] guideFiles = Directory.GetFiles(dataFolderPath, "*.guide");

            if (guideFiles.Length == 0)
            {
                _log.LogWarning("[GuideDataLoader] No .guide files found in data folder.");
                return;
            }

            foreach (string filePath in guideFiles)
                LoadFile(filePath);

            _allStages.Sort((a, b) => a.Order.CompareTo(b.Order));
            _log.LogInfo($"[GuideDataLoader] Loaded {_allStages.Count} stages, {_playstyles.Count} playstyles.");
        }

        private static void LoadPlaystyles(string dataFolderPath)
        {
            string path = Path.Combine(dataFolderPath, "playstyles.json");

            if (!File.Exists(path))
            {
                _log.LogWarning("[GuideDataLoader] playstyles.json not found — playstyle features disabled.");
                return;
            }

            try
            {
                string raw = File.ReadAllText(path);
                PlaystyleData data = JsonConvert.DeserializeObject<PlaystyleData>(raw);

                if (data?.Playstyles == null || data.Playstyles.Count == 0)
                {
                    _log.LogWarning("[GuideDataLoader] playstyles.json deserialized empty.");
                    return;
                }

                _playstyles.AddRange(data.Playstyles);
                _log.LogInfo($"[GuideDataLoader] Loaded {_playstyles.Count} playstyles.");
            }
            catch (JsonException ex)
            {
                _log.LogError($"[GuideDataLoader] Parse error in playstyles.json: {ex.Message}");
            }
            catch (IOException ex)
            {
                _log.LogError($"[GuideDataLoader] File read error for playstyles.json: {ex.Message}");
            }
        }

        private static void LoadFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            try
            {
                string raw = File.ReadAllText(filePath);
                GuideData data = JsonConvert.DeserializeObject<GuideData>(raw);

                if (data == null || data.Stages == null || data.Stages.Count == 0) return;

                int accepted = 0;

                foreach (Stage stage in data.Stages)
                {
                    if (!ValidateStage(stage, fileName)) continue;

                    if (!string.IsNullOrEmpty(stage.ModRequired) &&
                        !InstalledMods.Contains(stage.ModRequired))
                        continue;

                    stage.Gear = FilterByMod(stage.Gear, g => g.ModRequired);
                    stage.Mobs = FilterByMod(stage.Mobs, m => m.ModRequired);
                    stage.Recipes = FilterByMod(stage.Recipes, r => r.ModRequired);
                    stage.Tips = FilterByMod(stage.Tips, t => t.ModRequired);
                    stage.Objectives = FilterByMod(stage.Objectives, o => o.ModRequired);
                    stage.BonusBosses = FilterByMod(stage.BonusBosses, b => b.ModRequired);

                    // --- NEW MERGE LOGIC ---
                    Stage existing = _allStages.FirstOrDefault(s => s.Id == stage.Id);
                    if (existing != null)
                    {
                        // If the stage already exists (e.g., vanilla Meadows), append the modded gear to it
                        if (stage.Gear != null) existing.Gear.AddRange(stage.Gear);
                        if (stage.Mobs != null) existing.Mobs.AddRange(stage.Mobs);
                        if (stage.Recipes != null) existing.Recipes.AddRange(stage.Recipes);
                    }
                    else
                    {
                        // It's a new stage, add it to the sidebar
                        AssignBaseOrder(stage);
                        _allStages.Add(stage);
                    }
                    accepted++;
                }
                _log.LogInfo($"[GuideDataLoader] {fileName} → {accepted} stage(s) merged/accepted.");
            }
            catch (Exception ex) { _log.LogError($"[GuideDataLoader] Error in {fileName}: {ex.Message}"); }
        }

        private static void AssignBaseOrder(Stage stage)
        {
            int baseOrder = BiomeOrder.FromStageId(stage.Id);
            if (stage.ModRequired == "Therzie.Armory") baseOrder += 1;
            else if (stage.ModRequired == "Therzie.Warfare") baseOrder += 2;
            stage.Order = baseOrder;
        }

        private static List<T> FilterByMod<T>(List<T> list, Func<T, string> getModRequired)
        {
            if (list == null) return new List<T>();
            return list.Where(item =>
            {
                string mod = getModRequired(item);
                return string.IsNullOrEmpty(mod) || InstalledMods.Contains(mod);
            }).ToList();
        }

        private static bool ValidateStage(Stage stage, string fileName)
        {
            if (string.IsNullOrEmpty(stage.Id)) return false;
            if (string.IsNullOrEmpty(stage.Label)) return false;
            if (stage.UnlockTrigger == null) return false;
            return true;
        }

        public static Stage GetStageById(string id)
            => _allStages.FirstOrDefault(s => s.Id == id);

        public static Stage GetNextStage(string currentId)
        {
            int index = _allStages.FindIndex(s => s.Id == currentId);
            if (index < 0 || index >= _allStages.Count - 1) return null;
            return _allStages[index + 1];
        }

        public static PlaystyleDefinition GetPlaystyle(string id)
            => _playstyles.FirstOrDefault(p => p.Id == id);
    }
}