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

        public static List<Stage> AllStages { get; private set; } = new List<Stage>();
        public static HashSet<string> InstalledMods { get; set; } = new HashSet<string>();

        public static void Load(string dataFolderPath, ManualLogSource log)
        {
            _log = log;
            AllStages.Clear();

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

            // Groups perfectly by biome, then vanilla -> armory -> warfare
            AllStages = AllStages
                .OrderBy(s => s.Order)
                .ToList();

            _log.LogInfo($"[GuideDataLoader] Loaded {AllStages.Count} stages from {guideFiles.Length} files.");
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

                    // 🔥 Forces stages to group cleanly regardless of what the JSON says
                    AssignBaseOrder(stage);

                    AllStages.Add(stage);
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

        // Overrides JSON orders to force Biome Grouping (Spacing by 10s)
        private static void AssignBaseOrder(Stage stage)
        {
            string id = stage.Id.ToLower();
            int baseOrder = 80; // Defaults to Other

            if (id.Contains("meadows")) baseOrder = 0;
            else if (id.Contains("blackforest")) baseOrder = 10;
            else if (id.Contains("swamp")) baseOrder = 20;
            else if (id.Contains("mountain")) baseOrder = 30;
            else if (id.Contains("plains")) baseOrder = 40;
            else if (id.Contains("mistlands")) baseOrder = 50;
            else if (id.Contains("ashlands")) baseOrder = 60;
            else if (id.Contains("deepnorth")) baseOrder = 70;

            // Put Armory immediately under Vanilla, then Warfare right under Armory
            if (stage.ModRequired == "Therzie.Armory") baseOrder += 1;
            else if (stage.ModRequired == "Therzie.Warfare") baseOrder += 2;

            stage.Order = baseOrder;
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

            if ((stage.Id.StartsWith("armory_") || stage.Id.StartsWith("warfare_")) && stage.UnlockTrigger?.Type == "none")
            {
                if (stage.Id.Contains("blackforest")) stage.UnlockTrigger = new Trigger { Type = "globalKey", Value = "defeated_eikthyr" };
                else if (stage.Id.Contains("swamp")) stage.UnlockTrigger = new Trigger { Type = "globalKey", Value = "defeated_gdking" };
                else if (stage.Id.Contains("mountain")) stage.UnlockTrigger = new Trigger { Type = "globalKey", Value = "defeated_bonemass" };
                else if (stage.Id.Contains("plains")) stage.UnlockTrigger = new Trigger { Type = "globalKey", Value = "defeated_dragon" };
                else if (stage.Id.Contains("mistlands")) stage.UnlockTrigger = new Trigger { Type = "globalKey", Value = "defeated_goblinking" };
                else if (stage.Id.Contains("ashlands")) stage.UnlockTrigger = new Trigger { Type = "globalKey", Value = "defeated_queen" };
                else if (stage.Id.Contains("deepnorth")) stage.UnlockTrigger = new Trigger { Type = "globalKey", Value = "defeated_fader" };
            }

            if (stage.UnlockTrigger == null) return false;

            return true;
        }

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