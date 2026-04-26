using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using ValheimGuide.Data;

namespace ValheimGuide.DataGenerators
{
    public static class GuideDataEnricher
    {
        public static void Run()
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
                return;

            int foundCount = 0; // prefab resolved
            int damageCount = 0; // DamageTypes enriched
            int armorClassCount = 0; // ArmorClass enriched
            int recipeCount = 0; // Recipe auto-populated
            int stationCount = 0; // Station auto-populated

            foreach (Stage stage in GuideDataLoader.AllStages)
            {
                foreach (GearEntry gear in stage.Gear)
                {
                    if (string.IsNullOrEmpty(gear.ItemId)) continue;

                    GameObject prefab = ObjectDB.instance.GetItemPrefab(gear.ItemId);
                    if (prefab == null) continue;

                    ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                    if (itemDrop == null) continue;

                    var shared = itemDrop.m_itemData.m_shared;
                    foundCount++;

                    string type = gear.Type ?? "";

                    if ((type.Equals("Weapon", System.StringComparison.OrdinalIgnoreCase) ||
                         type.Equals("Bow", System.StringComparison.OrdinalIgnoreCase))
                        && (gear.DamageTypes == null || gear.DamageTypes.Count == 0))
                    {
                        gear.DamageTypes = GetDamageTypes(shared.m_damages);
                        damageCount++;
                    }

                    if (type.Equals("Armor", System.StringComparison.OrdinalIgnoreCase)
                        && string.IsNullOrEmpty(gear.ArmorClass))
                    {
                        gear.ArmorClass = shared.m_movementModifier < 0f ? "Heavy" : "Light";
                        armorClassCount++;
                    }

                    if (gear.Recipe == null || gear.Recipe.Count == 0)
                    {
                        Recipe recipe = ObjectDB.instance.GetRecipe(itemDrop.m_itemData);
                        if (recipe != null
                            && recipe.m_resources != null
                            && recipe.m_resources.Length > 0)
                        {
                            gear.Recipe = new List<ItemStack>();

                            if (recipe.m_craftingStation != null
                                && string.IsNullOrEmpty(gear.Station))
                            {
                                gear.Station = TryLocalise(recipe.m_craftingStation.m_name);
                                gear.StationLevel = recipe.m_minStationLevel;
                                stationCount++;
                            }

                            foreach (Piece.Requirement req in recipe.m_resources)
                            {
                                if (req.m_resItem == null) continue;

                                gear.Recipe.Add(new ItemStack
                                {
                                    ItemId = req.m_resItem.name,
                                    Label = TryLocalise(req.m_resItem.m_itemData.m_shared.m_name),
                                    Amount = req.m_amount
                                });
                            }

                            recipeCount++;
                        }
                    }
                }
            }

            int objMatCount = 0;
            foreach (Stage stage in GuideDataLoader.AllStages)
            {
                foreach (Objective obj in stage.Objectives)
                {

                    if (string.IsNullOrEmpty(obj.Value)) continue;
                    if (obj.ObjectiveMaterials != null && obj.ObjectiveMaterials.Count > 0) continue;

                    string t = obj.Type?.ToLowerInvariant() ?? "";
                    bool isPieceType = t == "build";
                    bool isItemType = t == "craftitem" || t == "knownrecipe";

                    if (!isPieceType && !isItemType) continue;

                    if (isPieceType)
                    {
                        // Piece recipes live on the Piece component, not ObjectDB recipes
                        GameObject piecePrefab = PrefabManager.Instance != null
                            ? PrefabManager.Instance.GetPrefab(obj.Value)
                            : null;

                        // Fallback: try ZNetScene which has all registered prefabs
                        if (piecePrefab == null && ZNetScene.instance != null)
                            piecePrefab = ZNetScene.instance.GetPrefab(obj.Value);

                        if (piecePrefab == null) continue;

                        Piece piece = piecePrefab.GetComponent<Piece>();
                        if (piece == null || piece.m_resources == null
                            || piece.m_resources.Length == 0) continue;

                        obj.ObjectiveMaterials = new List<ItemStack>();
                        foreach (var req in piece.m_resources)
                        {
                            if (req.m_resItem == null) continue;
                            obj.ObjectiveMaterials.Add(new ItemStack
                            {
                                ItemId = req.m_resItem.name,
                                Label = TryLocalise(req.m_resItem.m_itemData.m_shared.m_name),
                                Amount = req.m_amount
                            });
                        }
                        if (obj.ObjectiveMaterials.Count > 0) objMatCount++;
                    }
                    else // craftitem / knownrecipe — uses ObjectDB recipes
                    {
                        GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(obj.Value);
                        if (itemPrefab == null) continue;

                        ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
                        if (itemDrop == null) continue;

                        Recipe recipe = ObjectDB.instance.GetRecipe(itemDrop.m_itemData);
                        if (recipe == null || recipe.m_resources == null
                            || recipe.m_resources.Length == 0) continue;

                        obj.ObjectiveMaterials = new List<ItemStack>();
                        foreach (var req in recipe.m_resources)
                        {
                            if (req.m_resItem == null) continue;
                            obj.ObjectiveMaterials.Add(new ItemStack
                            {
                                ItemId = req.m_resItem.name,
                                Label = TryLocalise(req.m_resItem.m_itemData.m_shared.m_name),
                                Amount = req.m_amount
                            });
                        }
                        if (obj.ObjectiveMaterials.Count > 0) objMatCount++;
                    }
                }
            }

            Debug.Log($"[ValheimGuide] Enricher done — " +
                      $"{foundCount} prefabs resolved, " +
                      $"{damageCount} damage types, " +
                      $"{armorClassCount} armor classes, " +
                      $"{recipeCount} recipes, " +
                      $"{stationCount} stations, " +
                      $"{objMatCount} objective material lists auto-populated.");

        }
        public static void EnrichMobResistances()
        {
            if (ZNetScene.instance == null) return;

            int enriched = 0;
            foreach (Stage stage in GuideDataLoader.AllStages)
            {
                foreach (MobEntry mob in stage.Mobs)
                {
                    if (mob.Resistances != null && mob.Resistances.Count > 0) continue;
                    if (string.IsNullOrEmpty(mob.PrefabId)) continue;

                    GameObject prefab = ZNetScene.instance.GetPrefab(mob.PrefabId);
                    if (prefab == null) continue;

                    Character character = prefab.GetComponent<Character>();
                    if (character == null) continue;

                    mob.Resistances = ExtractResistances(character.m_damageModifiers);
                    enriched++;
                }
            }

            Debug.Log($"[ValheimGuide] Mob resistance enrichment done — {enriched} mobs enriched.");
        }

        private static Dictionary<string, string> ExtractResistances(HitData.DamageModifiers mods)
        {
            var dict = new Dictionary<string, string>();
            AddResistance(dict, "Blunt", mods.m_blunt);
            AddResistance(dict, "Slash", mods.m_slash);
            AddResistance(dict, "Pierce", mods.m_pierce);
            AddResistance(dict, "Fire", mods.m_fire);
            AddResistance(dict, "Frost", mods.m_frost);
            AddResistance(dict, "Lightning", mods.m_lightning);
            AddResistance(dict, "Poison", mods.m_poison);
            AddResistance(dict, "Spirit", mods.m_spirit);
            return dict;
        }

        private static void AddResistance(Dictionary<string, string> dict, string type, HitData.DamageModifier mod)
        {
            switch (mod)
            {
                case HitData.DamageModifier.Weak:
                case HitData.DamageModifier.VeryWeak:
                    dict[type] = "Weak"; break;
                case HitData.DamageModifier.Resistant:
                case HitData.DamageModifier.VeryResistant:
                    dict[type] = "Resistant"; break;
                case HitData.DamageModifier.Immune:
                case HitData.DamageModifier.Ignore:
                    dict[type] = "Immune"; break;
            }
        }

        private static List<string> GetDamageTypes(HitData.DamageTypes damages)
        {
            var list = new List<string>();
            if (damages.m_blunt > 0) list.Add("Blunt");
            if (damages.m_slash > 0) list.Add("Slash");
            if (damages.m_pierce > 0) list.Add("Pierce");
            if (damages.m_fire > 0) list.Add("Fire");
            if (damages.m_frost > 0) list.Add("Frost");
            if (damages.m_lightning > 0) list.Add("Lightning");
            if (damages.m_poison > 0) list.Add("Poison");
            if (damages.m_spirit > 0) list.Add("Spirit");
            return list;
        }

        private static string TryLocalise(string key)
        {
            if (Localization.instance == null || string.IsNullOrEmpty(key)) return key;
            try { return Localization.instance.Localize(key); }
            catch { return key; }
        }
    }
}