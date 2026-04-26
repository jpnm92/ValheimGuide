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
        public static HashSet<string> InstalledMods { get; private set; } = new HashSet<string>();

        // Playstyles loaded separately from playstyles.json
        private static readonly List<PlaystyleDefinition> _playstyles = new List<PlaystyleDefinition>();
        public static IReadOnlyList<PlaystyleDefinition> Playstyles => _playstyles;
        public static IEnumerable<Stage> GetStagesToScan() => AllStages;
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
            System.Diagnostics.Debug.Assert(
                _allStages.SequenceEqual(_allStages.OrderBy(s => s.Order)),
                "AllStages must be sorted by Order");
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
                        continue;

                    stage.Gear = FilterByMod(stage.Gear, g => g.ModRequired);
                    stage.Mobs = FilterByMod(stage.Mobs, m => m.ModRequired);
                    stage.Recipes = FilterByMod(stage.Recipes, r => r.ModRequired);
                    stage.Tips = FilterByMod(stage.Tips, t => t.ModRequired);
                    stage.Objectives = FilterByMod(stage.Objectives, o => o.ModRequired);
                    stage.BonusBosses = FilterByMod(stage.BonusBosses, b => b.ModRequired);

                    // --- MERGE LOGIC ---
                    Stage existing = _allStages.FirstOrDefault(s => s.Id == stage.Id);
                    if (existing != null)
                    {
                        _log.LogInfo($"[GuideDataLoader] Stage '{stage.Id}' from '{fileName}' merging into existing stage.");
                        // Safely merge text and boss data if the existing stage is a generated shell
                        if (string.IsNullOrEmpty(existing.Article) && !string.IsNullOrEmpty(stage.Article)) existing.Article = stage.Article;
                        if (string.IsNullOrEmpty(existing.BiomeDescription) && !string.IsNullOrEmpty(stage.BiomeDescription)) existing.BiomeDescription = stage.BiomeDescription;

                        if (existing.Boss == null && stage.Boss != null) 
                            existing.Boss = stage.Boss;
                        else if (existing.Boss != null && stage.Boss != null)
                            _log.LogWarning($"[GuideDataLoader] Stage '{stage.Id}' from '{fileName}' has a Boss conflict — keeping first loaded.");

                        if (existing.UnlockTrigger == null || existing.UnlockTrigger.Type == "none") existing.UnlockTrigger = stage.UnlockTrigger;

                        // Merge lists safely
                        existing.Objectives = MergeLists(existing.Objectives, stage.Objectives);
                        existing.Tips = MergeLists(existing.Tips, stage.Tips);
                        existing.PriorityMaterials = MergeLists(existing.PriorityMaterials, stage.PriorityMaterials);
                        existing.Gear = MergeLists(existing.Gear, stage.Gear);
                        existing.Mobs = MergeLists(existing.Mobs, stage.Mobs);
                        existing.Recipes = MergeLists(existing.Recipes, stage.Recipes);
                    }
                    else
                    {
                        AssignBaseOrder(stage);
                        _allStages.Add(stage);
                    }

                    accepted++;
                }

                _log.LogInfo($"[GuideDataLoader] {fileName} → {accepted} stage(s) accepted/merged.");
                _allStages.Sort((a, b) => a.Order.CompareTo(b.Order));
                _log.LogInfo($"[GuideDataLoader] Load complete. Total stages: {_allStages.Count}");
            }
            catch (JsonException ex) { _log.LogError($"[GuideDataLoader] Parse error in {fileName}: {ex.Message}"); }
            catch (IOException ex) { _log.LogError($"[GuideDataLoader] File read error for {fileName}: {ex.Message}"); }
        }

        // Within a biome, Armory (armor) stages sort before Warfare (weapons) stages,
        // which in turn sort before vanilla stages. Offsets must stay within 0–9
        // to avoid colliding with the next biome's base order.
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
        private static List<T> MergeLists<T>(List<T> target, List<T> source)
        {
            if (source == null) return target;
            if (target == null) return new List<T>(source);
            target.AddRange(source);
            return target;
        }
        private static bool ValidateStage(Stage stage, string fileName)
        {
            if (string.IsNullOrEmpty(stage.Id))
            {
                _log.LogWarning($"[GuideDataLoader] Stage in '{fileName}' has no Id — skipping.");
                return false;
            }
            if (string.IsNullOrEmpty(stage.Label))
            {
                _log.LogWarning($"[GuideDataLoader] Stage '{stage.Id}' in '{fileName}' has no Label — skipping.");
                return false;
            }
            if (stage.UnlockTrigger == null)
            {
                _log.LogWarning($"[GuideDataLoader] Stage '{stage.Id}' in '{fileName}' has no UnlockTrigger — skipping.");
                return false;
            }
            return true;
        }

        public static Stage GetStageById(string id)
        {
            var stage = _allStages.FirstOrDefault(s => s.Id == id);
            if (stage == null)
                _log?.LogWarning($"[GuideDataLoader] GetStageById: no stage found for id '{id}'");
            return stage;
        }

        public static Stage GetNextStage(string currentId)
        {
            Stage current = _allStages.FirstOrDefault(s => s.Id == currentId);
            if (current == null) return null;
            return _allStages.FirstOrDefault(s => s.Order > current.Order);
        }
        public static PlaystyleDefinition GetPlaystyle(string id)
            => _playstyles.FirstOrDefault(p => p.Id == id);
    }
}