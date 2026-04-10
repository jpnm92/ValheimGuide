using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Jotunn.Managers;
using Newtonsoft.Json;
using UnityEngine;
using ValheimGuide.Data;

namespace ValheimGuide.DataGenerators
{
    public static class TherzieDataGenerator
    {
        private static bool _hasRun = false;

        public static void Register()
        {
            ItemManager.OnItemsRegistered += Run;
        }

        private static void Run()
        {
            if (_hasRun) return;
            _hasRun = true;

            Debug.Log("[TherzieDataGenerator] Generation starting.");

            try
            {
                GenerateArmoryData();
                GenerateWarfareData();
                Debug.Log("[TherzieDataGenerator] Generation complete.");

                string dataFolder = Path.Combine(Paths.PluginPath, "ValheimGuide", "data");
                var logger = BepInEx.Logging.Logger.CreateLogSource("ValheimGuide_Gen");
                GuideDataLoader.Load(dataFolder, logger);

                ProgressionTracker.RefreshCurrentStage();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TherzieDataGenerator] CRASH: {ex}");
            }
        }

        private static void GenerateArmoryData()
        {
            const string modGuid = "Therzie.Armory";
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGuid))
            {
                Debug.LogWarning("[TherzieDataGenerator] Armory not detected, skipping.");
                return;
            }

            var items = ObjectDB.instance.m_items
                .Where(i => i != null &&
                            i.name.EndsWith("_TW") &&
                            !i.name.ToLower().Contains("monster") && // Hard ban on enemy attacks
                            IsArmorPiece(i))
                .ToList();

            Debug.Log($"[TherzieDataGenerator] Found {items.Count} Armory items.");
            SaveToFile(GroupByTier(items, "Armory", modGuid, GetArmorTier), "armory_generated.guide");
        }

        private static void GenerateWarfareData()
        {
            const string modGuid = "Therzie.Warfare";
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGuid))
            {
                Debug.LogWarning("[TherzieDataGenerator] Warfare not detected, skipping.");
                return;
            }

            var items = ObjectDB.instance.m_items
                .Where(i => i != null &&
                            i.name.EndsWith("_TW") &&
                            !i.name.ToLower().Contains("monster") && // Hard ban on enemy attacks
                            IsWeaponOrTool(i))
                .ToList();

            Debug.Log($"[TherzieDataGenerator] Found {items.Count} Warfare items.");
            SaveToFile(GroupByTier(items, "Warfare", modGuid, GetWarfareTier), "warfare_generated.guide");
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

        private static int GetBaseOrderForTier(string tier)
        {
            switch (tier)
            {
                case "Meadows": return 0;
                case "Black Forest": return 10;
                case "Swamp": return 20;
                case "Mountain": return 30;
                case "Plains": return 40;
                case "Mistlands": return 50;
                case "Ashlands": return 60;
                case "DeepNorth": return 70;
                case "Other": return 80;
                default: return 90;
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
                    Id = $"{modName.ToLower()}_{group.Key.ToLower().Replace(" ", "")}",
                    Label = $"{modName} ({group.Key})",
                    Order = GetBaseOrderForTier(group.Key) + (modName == "Armory" ? 1 : 2),
                    BiomeDescription = $"{modName} items for {group.Key} tier.",
                    ModRequired = modGuid,
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

        private static string CleanLabel(string locKey)
        {
            return locKey
                .TrimStart('$')
                .Replace("_TW", "")
                .Replace("_", " ")
                .ToLower()
                .Replace("armorchest", "Chest")
                .Replace("armorlegs", "Legs")
                .Replace("helmet", "Helmet");
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

        private static string GetArmorTier(GameObject prefab)
        {
            string name = prefab.name.ToLower();

            if (name.Contains("tyranium") || name.Contains("thoradus") || name.Contains("lokvyr") || name.Contains("njord") || name.Contains("skadi") || name.Contains("jotunn") || name.Contains("glacier") || name.Contains("storm") || name.Contains("polar") || name.Contains("frost") || name.Contains("ice") || name.Contains("north")) return "DeepNorth";
            if (name.Contains("ragnorite") || name.Contains("surtr") || name.Contains("volcanic") || name.Contains("demon") || name.Contains("muspelheim") || name.Contains("charred") || name.Contains("fader") || name.Contains("ash") || name.Contains("flametal")) return "Ashlands";
            if (name.Contains("carapace") || name.Contains("dvergr") || name.Contains("queen") || name.Contains("seeker") || name.Contains("demolisher") || name.Contains("legion")) return "Mistlands";
            if (name.Contains("blackmetal") || name.Contains("bm") || name.Contains("lox") || name.Contains("scimitar") || name.Contains("yagluth") || name.Contains("blood") || name.Contains("padded") || name.Contains("bold")) return "Plains";
            if (name.Contains("silver") || name.Contains("wolf") || name.Contains("crystal") || name.Contains("obsidian") || name.Contains("drake") || name.Contains("spirit") || name.Contains("vidar") || name.Contains("fenrir")) return "Mountain";
            if (name.Contains("iron") || name.Contains("rotten") || name.Contains("swamp") || name.Contains("bonemass") || name.Contains("vampiric") || name.Contains("leech") || name.Contains("warrior")) return "Swamp";
            if (name.Contains("bronze") || name.Contains("chitin") || name.Contains("troll") || name.Contains("elder") || name.Contains("copper") || name.Contains("tin") || name.Contains("viper") || name.Contains("hunter") || name.Contains("rogue") || name.Contains("vigorous")) return "Black Forest";
            if (name.Contains("leather") || name.Contains("razorback") || name.Contains("flint") || name.Contains("bone") || name.Contains("eikthyr") || name.Contains("stag") || name.Contains("wood") || name.Contains("scythe") || name.Contains("wrench") || name.Contains("knife") || name.Contains("club") || name.Contains("spear") || name.Contains("axe") || name.Contains("bow") || name.Contains("shield") || name.Contains("mace") || name.Contains("sword") || name.Contains("atgeir") || name.Contains("sledge") || name.Contains("buckler") || name.Contains("tower")) return "Meadows";

            return "Other";
        }

        private static string GetWarfareTier(GameObject prefab)
        {
            // For Warfare, the logic is identical to Armor, keeping the items perfectly synced
            return GetArmorTier(prefab);
        }

        private static void SaveToFile(List<Stage> stages, string fileName)
        {
            string dataFolder = Path.Combine(Paths.PluginPath, "ValheimGuide", "data");
            Directory.CreateDirectory(dataFolder);
            string filePath = Path.Combine(dataFolder, fileName);
            File.WriteAllText(filePath,
                JsonConvert.SerializeObject(new GuideData { Stages = stages }, Formatting.Indented));
            Debug.Log($"[TherzieDataGenerator] Saved {stages.Sum(s => s.Gear.Count)} items to {filePath}");
        }
    }
}