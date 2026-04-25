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

            Debug.Log($"[ValheimGuide] Enricher done — " +
                      $"{foundCount} prefabs resolved, " +
                      $"{damageCount} damage types, " +
                      $"{armorClassCount} armor classes, " +
                      $"{recipeCount} recipes, " +
                      $"{stationCount} stations auto-populated.");
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