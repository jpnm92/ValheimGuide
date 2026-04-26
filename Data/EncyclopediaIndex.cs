using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimGuide.Data;

namespace ValheimGuide.Data
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Category enum — shared between index and UI
    // ─────────────────────────────────────────────────────────────────────────

    public enum EncyclopediaCategory { Weapon, Armor, Shield, Mob }

    // ─────────────────────────────────────────────────────────────────────────
    //  A single searchable entry
    // ─────────────────────────────────────────────────────────────────────────

    public class EncyclopediaEntry
    {
        public string Id;           // prefab name / PrefabId
        public string Label;        // human-readable display name
        public EncyclopediaCategory Category;
        public string Biome;        // stage label ("Black Forest") or mob biome field
        public string ModRequired;  // null = vanilla

        // Exactly one of these three will be non-null
        public GearEntry GuideGear;                         // item from guide data
        public MobEntry GuideMob;                          // mob from guide data
        public ItemDrop.ItemData.SharedData LiveShared;     // item NOT in guide data (mods)
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Index builder
    // ─────────────────────────────────────────────────────────────────────────

    public static class EncyclopediaIndex
    {
        private static List<EncyclopediaEntry> _entries;
        private static bool _built = false;

        /// <summary>
        /// Returns the full entry list, building it on first access.
        /// Call Invalidate() after guide data reloads.
        /// </summary>
        public static IReadOnlyList<EncyclopediaEntry> Entries
        {
            get
            {
                if (!_built) Build();
                return _entries;
            }
        }

        /// <summary>Forces a rebuild on next access (call after LoadGuideData).</summary>
        public static void Invalidate() => _built = false;

        // ── Build ─────────────────────────────────────────────────────────────

        private static void Build()
        {
            _entries = new List<EncyclopediaEntry>();
            var indexedIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            // ── 1. Guide GearEntries (already enriched by GuideDataEnricher) ──────
            foreach (Stage stage in GuideDataLoader.AllStages)
            {
                if (stage.Gear != null)
                {
                    foreach (GearEntry g in stage.Gear)
                    {
                        if (string.IsNullOrEmpty(g.ItemId) || indexedIds.Contains(g.ItemId))
                            continue;
                        indexedIds.Add(g.ItemId);

                        _entries.Add(new EncyclopediaEntry
                        {
                            Id = g.ItemId,
                            Label = g.Label,
                            Category = GearTypeToCategory(g.Type),
                            Biome = stage.Label,
                            ModRequired = g.ModRequired,
                            GuideGear = g
                        });
                    }
                }

                // ── 2. Guide MobEntries ─────────────────────────────────────────
                if (stage.Mobs != null)
                {
                    foreach (MobEntry m in stage.Mobs)
                    {
                        if (string.IsNullOrEmpty(m.PrefabId) || indexedIds.Contains(m.PrefabId))
                            continue;
                        indexedIds.Add(m.PrefabId);

                        _entries.Add(new EncyclopediaEntry
                        {
                            Id = m.PrefabId,
                            Label = m.Label,
                            Category = EncyclopediaCategory.Mob,
                            Biome = !string.IsNullOrEmpty(m.Biome) ? m.Biome : stage.Label,
                            ModRequired = m.ModRequired,
                            GuideMob = m
                        });
                    }
                }
            }

            // ── 3. Live ObjectDB scan — catches every weapon/armor from any mod ──
            if (ObjectDB.instance != null && ObjectDB.instance.m_items != null)
            {
                foreach (GameObject itemPrefab in ObjectDB.instance.m_items)
                {
                    if (itemPrefab == null) continue;

                    ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
                    if (itemDrop == null) continue;

                    ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData?.m_shared;
                    if (shared == null) continue;

                    string prefabName = itemPrefab.name;
                    if (indexedIds.Contains(prefabName)) continue;

                    EncyclopediaCategory? cat = ItemTypeToCategory(shared.m_itemType);
                    if (cat == null) continue;

                    indexedIds.Add(prefabName);

                    // Try to get a nice localised name
                    string locLabel = TryLocalise(shared.m_name);
                    if (string.IsNullOrEmpty(locLabel) || locLabel == shared.m_name)
                        locLabel = prefabName; // fallback to prefab name

                    _entries.Add(new EncyclopediaEntry
                    {
                        Id = prefabName,
                        Label = locLabel,
                        Category = cat.Value,
                        Biome = "Unknown",
                        LiveShared = shared
                    });
                }
            }

            // ── Sort: guide entries first (by biome order, then label), unknowns last ──
            _entries.Sort((a, b) =>
            {
                bool aGuide = a.GuideGear != null || a.GuideMob != null;
                bool bGuide = b.GuideGear != null || b.GuideMob != null;
                if (aGuide != bGuide) return aGuide ? -1 : 1;
                int biomeOrd = BiomeOrder.FromTier(NormaliseBiome(a.Biome))
                             .CompareTo(BiomeOrder.FromTier(NormaliseBiome(b.Biome)));
                if (biomeOrd != 0) return biomeOrd;
                return string.Compare(a.Label, b.Label, System.StringComparison.OrdinalIgnoreCase);
            });

            _built = true;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static EncyclopediaCategory GearTypeToCategory(string type)
        {
            if (string.IsNullOrEmpty(type)) return EncyclopediaCategory.Weapon;
            switch (type.ToLowerInvariant())
            {
                case "shield": return EncyclopediaCategory.Shield;
                case "armor": return EncyclopediaCategory.Armor;
                default: return EncyclopediaCategory.Weapon; // Weapon, Bow, Tool
            }
        }

        private static EncyclopediaCategory? ItemTypeToCategory(ItemDrop.ItemData.ItemType t)
        {
            switch (t)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Tool:
                case ItemDrop.ItemData.ItemType.Torch:
                    return EncyclopediaCategory.Weapon;

                case ItemDrop.ItemData.ItemType.Shield:
                    return EncyclopediaCategory.Shield;

                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Hands:
                case ItemDrop.ItemData.ItemType.Utility:
                case ItemDrop.ItemData.ItemType.Shoulder:
                    return EncyclopediaCategory.Armor;

                default:
                    return null;
            }
        }

        private static string TryLocalise(string key)
        {
            if (Localization.instance == null || string.IsNullOrEmpty(key)) return key;
            try { return Localization.instance.Localize(key); }
            catch { return key; }
        }

        /// <summary>Maps stage labels back to the tier string BiomeOrder expects.</summary>
        private static string NormaliseBiome(string biome)
        {
            if (biome == null) return "Other";
            switch (biome.ToLowerInvariant())
            {
                case "meadows": return "Meadows";
                case "black forest": return "Black Forest";
                case "swamp": return "Swamp";
                case "mountain": return "Mountain";
                case "plains": return "Plains";
                case "mistlands": return "Mistlands";
                case "ashlands": return "Ashlands";
                case "deep north": return "DeepNorth";
                default: return biome;
            }
        }
    }
}