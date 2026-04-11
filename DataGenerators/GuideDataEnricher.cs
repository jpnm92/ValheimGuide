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

            int enrichedCount = 0;

            foreach (Stage stage in GuideDataLoader.AllStages)
            {
                foreach (GearEntry gear in stage.Gear)
                {
                    GameObject prefab = ObjectDB.instance.GetItemPrefab(gear.ItemId);
                    if (prefab == null) continue;

                    ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                    if (itemDrop == null) continue;

                    var shared = itemDrop.m_itemData.m_shared;

                    // 1. Auto-Detect Damage Types
                    if (gear.Type == "Weapon" || gear.Type == "Bow")
                    {
                        gear.DamageTypes = GetDamageTypes(shared.m_damages);
                    }

                    // 2. Auto-Detect Armor Class (Based on movement penalty)
                    if (gear.Type == "Armor")
                    {
                        // Vanilla heavy armor has a negative movement modifier (-5% to -10%)
                        gear.ArmorClass = shared.m_movementModifier < 0f ? "Heavy" : "Light";
                    }

                    enrichedCount++;
                }
            }

            Debug.Log($"[ValheimGuide] Auto-enriched {enrichedCount} gear entries with DamageTypes and ArmorClasses.");
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
    }
}