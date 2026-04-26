using BepInEx;
using BepInEx.Logging;
using Jotunn.Managers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ValheimGuide.Data;

namespace ValheimGuide.DataGenerators
{
    public class TherzieDataProvider : IModDataProvider
    {
        public string ProviderId => "Therzie";

        public bool CanProvide(HashSet<string> installedMods)
            => installedMods.Contains("Therzie.Armory") || installedMods.Contains("Therzie.Warfare");

        public void Generate(string dataFolder, ManualLogSource log)
            => TherzieDataGenerator.GenerateIfPresent(dataFolder, log);
    }

    public static class TherzieDataGenerator
    {
        public static void GenerateIfPresent(string dataFolder, ManualLogSource log)
        {

            // Always regenerate — ensures tier fixes and new items are picked up
            string armoryPath = Path.Combine(dataFolder, "armory_generated.guide");
            string warfarePath = Path.Combine(dataFolder, "warfare_generated.guide");
            if (File.Exists(armoryPath)) File.Delete(armoryPath);
            if (File.Exists(warfarePath)) File.Delete(warfarePath);
            log.LogInfo("[TherzieDataGenerator] Generation starting.");

            try
            {
                GenerateArmoryData(log);
                GenerateWarfareData(log);
                log.LogInfo("[TherzieDataGenerator] Generation complete.");
            }
            catch (Exception ex)
            {
                log.LogError($"[TherzieDataGenerator] CRASH: {ex}");
            }
        }

        private static void GenerateArmoryData(ManualLogSource log)
        {
            const string modGuid = "Therzie.Armory";
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGuid))
            {
                log.LogWarning("[TherzieDataGenerator] Armory not detected, skipping.");
                return;
            }

            var items = ObjectDB.instance.m_items
                .Where(i => i != null &&
                            i.name.EndsWith("_TW") &&
                            !i.name.ToLower().Contains("monster") && // Hard ban on enemy attacks
                            IsArmorPiece(i))
                .ToList();

            log.LogInfo($"[TherzieDataGenerator] Found {items.Count} Armory items.");
            SaveToFile(GroupByTier(items, "Armory", modGuid, GetTierFromRecipe), "armory_generated.guide");
        }

        private static void GenerateWarfareData(ManualLogSource log)
        {
            const string modGuid = "Therzie.Warfare";
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGuid))
            {
                log.LogWarning("[TherzieDataGenerator] Warfare not detected, skipping.");
                return;
            }

            var items = ObjectDB.instance.m_items
                .Where(i => i != null &&
                            i.name.EndsWith("_TW") &&
                            !i.name.ToLower().Contains("monster") && // Hard ban on enemy attacks
                            IsWeaponOrTool(i))
                .ToList();

            log.LogInfo($"[TherzieDataGenerator] Found {items.Count} Warfare items.");
            SaveToFile(GroupByTier(items, "Warfare", modGuid, GetTierFromRecipe), "warfare_generated.guide");
        }

        private static bool IsArmorPiece(GameObject prefab)
        {
            var type = prefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_itemType;
            return type == ItemDrop.ItemData.ItemType.Chest
                || type == ItemDrop.ItemData.ItemType.Legs
                || type == ItemDrop.ItemData.ItemType.Helmet
                || type == ItemDrop.ItemData.ItemType.Shoulder;
        }

        private static bool IsWeaponOrTool(GameObject prefab)
        {
            var type = prefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_itemType;
            return type == ItemDrop.ItemData.ItemType.OneHandedWeapon
                || type == ItemDrop.ItemData.ItemType.TwoHandedWeapon
                || type == ItemDrop.ItemData.ItemType.Bow
                || type == ItemDrop.ItemData.ItemType.Shield
                || type == ItemDrop.ItemData.ItemType.Tool
                || type == ItemDrop.ItemData.ItemType.Torch;
        }

        private static Trigger GetTriggerForTier(string tier)
        {
            switch (tier)
            {
                case "Black Forest": return new Trigger { Type = "globalKey", Value = "defeated_eikthyr" };
                case "Swamp": return new Trigger { Type = "globalKey", Value = "defeated_gdking" };
                case "Mountain": return new Trigger { Type = "globalKey", Value = "defeated_bonemass" };
                case "Plains": return new Trigger { Type = "globalKey", Value = "defeated_dragon" };
                case "Mistlands": return new Trigger { Type = "globalKey", Value = "defeated_goblinking" };
                case "Ashlands": return new Trigger { Type = "globalKey", Value = "defeated_queen" };
                case "DeepNorth": return new Trigger { Type = "globalKey", Value = "defeated_fader" };
                case "Other": return new Trigger { Type = "globalKey", Value = "defeated_fader" };
                case "Meadows":
                default: return new Trigger { Type = "none", Value = "" };
            }
        }

        private static List<Stage> GroupByTier(List<GameObject> items, string modName,
            string modGuid, Func<GameObject, string> tierSelector)
        {
            var stages = new List<Stage>();
            var groups = items.GroupBy(tierSelector).OrderBy(g => g.Key).ToList();

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var stage = new Stage
                {
                    Id = GetVanillaStageId(group.Key), // Targets the vanilla tab
                    Label = group.Key,
                    Order = BiomeOrder.FromTier(group.Key),
                    BiomeDescription = "",
                    ModRequired = null, // Null ensures it merges cleanly
                    UnlockTrigger = GetTriggerForTier(group.Key),
                    Gear = new List<GearEntry>()
                };

                foreach (var prefab in group)
                {
                    var entry = CreateGearEntry(prefab, modGuid);
                    if (entry != null) stage.Gear.Add(entry);
                }

                if (stage.Gear.Any()) stages.Add(stage);
            }
            return stages;
        }

        private static string GetVanillaStageId(string tier)
        {
            switch (tier)
            {
                case "Meadows": return "meadows";
                case "Black Forest": return "blackforest";
                case "Swamp": return "swamp";
                case "Mountain": return "mountain";
                case "Plains": return "plains";
                case "Mistlands": return "mistlands";
                case "Ashlands": return "ashlands";
                case "DeepNorth": return "deepnorth";
                default: return "other";
            }
        }

        private static string CleanLabel(string locKey)
        {
            string localized = Jotunn.Managers.LocalizationManager.Instance?.TryTranslate(locKey);
            if (!string.IsNullOrEmpty(localized) && localized != locKey)
                return localized;

            return locKey
                .TrimStart('$')
                .Replace("_TW", "")
                .Replace("_", " ");
        }

        private static GearEntry CreateGearEntry(GameObject prefab, string modGuid)
        {
            var itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null) return null;
            var shared = itemDrop.m_itemData.m_shared;

            string label = CleanLabel(shared.m_name);

            string station = "Workbench";
            int stationLevel = 1;
            var recipe = ObjectDB.instance.GetRecipe(itemDrop.m_itemData);

            if (recipe != null)
            {
                string stationKey = recipe.m_craftingStation?.m_name ?? "";
                station = LocalizeStation(stationKey);
                stationLevel = recipe.m_minStationLevel;
            }
            else
            {
                string name = prefab.name.ToLower();
                if (name.Contains("flametal") || name.Contains("ragnorite") || name.Contains("surtr") || name.Contains("thoradus") || name.Contains("tyranium")) { station = "YmirForge"; stationLevel = 1; }
                else if (name.Contains("dvergr") || name.Contains("carapace")) { station = "BlackForge"; stationLevel = 1; }
                else if (name.Contains("blackmetal")) { station = "Forge"; stationLevel = 4; }
                else if (name.Contains("silver")) { station = "Forge"; stationLevel = 3; }
                else if (name.Contains("iron")) { station = "Forge"; stationLevel = 2; }
                else if (name.Contains("bronze")) { station = "Forge"; stationLevel = 1; }
            }

            var ingredients = new List<ItemStack>();
            if (recipe != null)
            {
                foreach (var req in recipe.m_resources)
                {
                    if (req.m_resItem == null) continue;
                    ingredients.Add(new ItemStack
                    {
                        ItemId = req.m_resItem.name,
                        Label = CleanLabel(req.m_resItem.m_itemData.m_shared.m_name),
                        Amount = req.m_amount
                    });
                }
            }

            return new GearEntry
            {
                ItemId = prefab.name,
                Label = label,
                Type = GetItemTypeString(shared.m_itemType),
                Station = station,
                StationLevel = stationLevel,
                ModRequired = modGuid,
                Recipe = ingredients
            };
        }

        private static string LocalizeStation(string key)
        {
            switch (key)
            {
                case "$piece_forge": return "Forge";
                case "$piece_workbench": return "Workbench";
                case "$piece_blackforge": return "BlackForge";
                case "$piece_magetable": return "GaldrTable";
                default:
                    return key.Replace("$piece_", "").Replace("$", "");
            }
        }

        private static string GetItemTypeString(ItemDrop.ItemData.ItemType type)
        {
            switch (type)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon: return "Weapon";
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon: return "Weapon";
                case ItemDrop.ItemData.ItemType.Bow: return "Bow";
                case ItemDrop.ItemData.ItemType.Shield: return "Shield";
                case ItemDrop.ItemData.ItemType.Tool: return "Tool";
                case ItemDrop.ItemData.ItemType.Chest: return "Armor";
                case ItemDrop.ItemData.ItemType.Legs: return "Armor";
                case ItemDrop.ItemData.ItemType.Helmet: return "Armor";
                case ItemDrop.ItemData.ItemType.Shoulder: return "Armor";
                default: return "Misc";
            }
        }

        private static string GetTierFromRecipe(GameObject prefab)
        {
            var itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null) return "Other";

            var recipe = ObjectDB.instance.GetRecipe(itemDrop.m_itemData);
            if (recipe != null)
            {
                // 1. INGREDIENT CHECK: The most reliable way to sort modded items
                string ingredientTier = GetTierFromIngredients(recipe);
                if (ingredientTier != null)
                    return ingredientTier;

                // 2. STATION CHECK: Fallback if the recipe only uses generic materials
                string stationKey = (recipe.m_craftingStation?.m_name ?? "").ToLower();
                int level = recipe.m_minStationLevel;

                if (stationKey.Contains("forge") && !stationKey.Contains("black") && !stationKey.Contains("ymir"))
                {
                    if (level >= 4) return "Plains";
                    if (level >= 3) return "Mountain";
                    if (level >= 2) return "Swamp";
                    return "Black Forest";
                }

                if (stationKey.Contains("workbench"))
                {
                    if (level >= 4) return "Mountain";
                    if (level >= 2) return "Black Forest";
                    return "Meadows";
                }

                if (stationKey.Contains("fletcher"))
                {
                    if (level >= 5) return "Mistlands";
                    if (level >= 4) return "Plains";
                    if (level >= 3) return "Mountain";
                    if (level >= 2) return "Swamp";
                    return "Meadows";
                }

                if (stationKey.Contains("armory"))
                {
                    if (level >= 6) return "Ashlands";
                    if (level >= 5) return "Mistlands";
                    if (level >= 4) return "Plains";
                    if (level >= 3) return "Mountain";
                    if (level >= 2) return "Black Forest";
                    return "Meadows"; // Fixed! It will correctly allow Level 1 armory into Meadows now.
                }

                if (stationKey.Contains("blackforge"))
                    return level >= 2 ? "Ashlands" : "Mistlands";

                if (stationKey.Contains("ymir"))
                    return "Ashlands";

                if (stationKey.Contains("galdr") || stationKey.Contains("magetable"))
                    return "Mistlands";
            }

            // 3. FALLBACK: Name-based matching
            Plugin.Log.LogWarning($"[TherzieDataGenerator] No defining recipe features for {prefab.name} — falling back to name-based tier detection.");
            return GetTierFromName(prefab.name.ToLower());
        }

        private static string GetTierFromIngredients(Recipe recipe)
        {
            bool hasAshlands = false, hasMistlands = false, hasPlains = false, hasMountain = false, hasSwamp = false, hasBlackForest = false, hasMeadows = false;

            foreach (var req in recipe.m_resources)
            {
                if (req.m_resItem == null) continue;
                string name = req.m_resItem.name.ToLower();

                if (name.Contains("flametal") || name.Contains("charred") || name.Contains("asksvin") || name.Contains("fader")) hasAshlands = true;
                else if (name.Contains("carapace") || name.Contains("eitr") || name.Contains("blackmarble") || name.Contains("yggdrasil") || name.Contains("seeker")) hasMistlands = true;
                else if (name.Contains("blackmetal") || name.Contains("lox") || name.Contains("linen") || name.Contains("needle") || name.Contains("yagluth")) hasPlains = true;
                else if (name.Contains("silver") || name.Contains("wolf") || name.Contains("obsidian") || name.Contains("crystal") || name.Contains("dragon")) hasMountain = true;
                else if (name.Contains("iron") || name.Contains("chain") || name.Contains("root") || name.Contains("ooze") || name.Contains("bloodbag") || name.Contains("bonemass")) hasSwamp = true;
                else if (name.Contains("bronze") || name.Contains("copper") || name.Contains("tin") || name.Contains("troll") || name.Contains("finewood") || name.Contains("corewood") || name.Contains("elder")) hasBlackForest = true;
                else if (name.Contains("flint") || name.Contains("leatherscraps") || name.Contains("deerhide") || name.Contains("bonefragments") || name.Contains("resin") || name.Contains("chitin")) hasMeadows = true;
            }

            if (hasAshlands) return "Ashlands";
            if (hasMistlands) return "Mistlands";
            if (hasPlains) return "Plains";
            if (hasMountain) return "Mountain";
            if (hasSwamp) return "Swamp";
            if (hasBlackForest) return "Black Forest";
            if (hasMeadows) return "Meadows"; // Correctly identifies lower-tier materials

            return null;
        }

        // FALLBACK: original logic, now only reached when no recipe exists
        private static string GetTierFromName(string name)
        {
            // This method is a LAST RESORT — it only runs when the ingredient and
            // station checks both failed. Generic weapon-type words (sword, axe, bow)
            // are intentionally NOT included here because they will match items from
            // every tier and produce silent miscategorisations. Only use unambiguous
            // material or character names that belong exclusively to one biome tier.

            // Ashlands-exclusive materials and characters
            if (name.Contains("flametal") || name.Contains("ragnorite") || name.Contains("surtr")
                || name.Contains("asksvin") || name.Contains("thoradus") || name.Contains("tyranium")
                || name.Contains("charred") || name.Contains("fader"))
                return "Ashlands";

            // Mistlands-exclusive materials and characters
            if (name.Contains("carapace") || name.Contains("dvergr") || name.Contains("eitr")
                || name.Contains("yggdrasil") || name.Contains("seeker") || name.Contains("queen"))
                return "Mistlands";

            // Plains-exclusive materials and characters
            if (name.Contains("blackmetal") || name.Contains("linen") || name.Contains("fuling")
                || name.Contains("yagluth") || name.Contains("deathsquito") || name.Contains("needle"))
                return "Plains";

            // Mountain-exclusive materials and characters
            if (name.Contains("silver") || name.Contains("fenris") || name.Contains("moder")
                || name.Contains("obsidian") || name.Contains("frostner"))
                return "Mountain";

            // Swamp-exclusive materials and characters
            if (name.Contains("iron") || name.Contains("bonemass") || name.Contains("abomination"))
                return "Swamp";

            // Black Forest-exclusive materials and characters
            if (name.Contains("bronze") || name.Contains("chitin") || name.Contains("troll")
                || name.Contains("copper") || name.Contains("tin"))
                return "Black Forest";

            // Meadows-exclusive materials and characters
            if (name.Contains("leather") || name.Contains("razorback") || name.Contains("flint")
                || name.Contains("eikthyr"))
                return "Meadows";

            // Genuinely unidentifiable — log it clearly so the JSON data can be fixed
            UnityEngine.Debug.LogWarning(
                $"[TherzieDataGenerator] Name-based tier detection failed for '{name}'. " +
                $"Assigning 'Other'. Consider adding a recipe or fixing the prefab name prefix.");
            return "Other";
        }


        private static void SaveToFile(List<Stage> stages, string fileName)
        {
            string dataFolder = Path.Combine(Paths.PluginPath, Plugin.PluginName, "data");
            Directory.CreateDirectory(dataFolder);
            string filePath = Path.Combine(dataFolder, fileName);
            File.WriteAllText(filePath,
                JsonConvert.SerializeObject(new GuideData { Stages = stages }, Formatting.Indented));
            UnityEngine.Debug.Log(
                $"[TherzieDataGenerator] Saved {stages.Sum(s => s.Gear.Count)} items to {filePath}");
        }

    }
}