using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimGuide.Data;

namespace ValheimGuide.DataGenerators
{
    /// <summary>
    /// Scans all Character prefabs registered in ZNetScene and injects MobEntry
    /// objects into the appropriate guide stages for any mob not already authored
    /// in the JSON data. Runs after GuideDataLoader.Load() and after all mods have
    /// registered their prefabs (called from ObjectDBAwakePatch alongside enrichment).
    ///
    /// Covers all mods — not just Therzie. Any Character prefab that passes the
    /// denylist is eligible.
    /// </summary>
    public static class MobDataGenerator
    {
        // ── Denylist ──────────────────────────────────────────────────────────
        // Prefab name substrings that identify non-mob characters: ragdolls,
        // projectiles, VFX objects, summons, player characters, and internal AI.
        // Matched case-insensitively.
        private static readonly HashSet<string> DenylistSubstrings = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            "_ragdoll", "_ghost", "_attack", "_projectile", "_summon",
            "_tentacle", "_root", "_vfx", "_sfx", "_fx", "_lod",
            "player", "playerfemale", "playermale",
            "dverger",          // covered manually in guide data
            "offering",         // altar objects
            "spawner",
            "chest",
            "pickable",
            "destructible",
            "sapling",
            "tree",
            "rock",
            "bush",
        };

        public static void Run()
        {
            if (ZNetScene.instance == null)
            {
                Debug.LogWarning("[MobDataGenerator] ZNetScene not ready — skipping.");
                return;
            }

            if (GuideDataLoader.AllStages.Count == 0)
            {
                Debug.LogWarning("[MobDataGenerator] No stages loaded — skipping.");
                return;
            }

            // Build a set of all prefab IDs already authored in any stage
            var authoredIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (Stage stage in GuideDataLoader.AllStages)
                foreach (MobEntry mob in stage.Mobs)
                    if (!string.IsNullOrEmpty(mob.PrefabId))
                        authoredIds.Add(mob.PrefabId);

            // Build a lookup: stageId → Stage for fast injection
            var stageById = GuideDataLoader.AllStages.ToDictionary(
                s => s.Id, s => s, System.StringComparer.OrdinalIgnoreCase);

            int injected = 0;
            int skipped = 0;

            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null) continue;

                string prefabName = prefab.name;

                // Skip already-authored mobs
                if (authoredIds.Contains(prefabName)) { skipped++; continue; }

                // Skip denylist matches
                if (IsOnDenylist(prefabName)) continue;

                // Must have a Character component
                Character character = prefab.GetComponent<Character>();
                if (character == null) continue;

                // Skip player faction (player characters, tamed animals acting as allies)
                if (character.m_faction == Character.Faction.Players) continue;

                // Skip if health is 0 or negative (inactive/placeholder)
                if (character.m_health <= 0) continue;

                // Determine target stage from prefab name
                string stageId = GetStageIdFromName(prefabName.ToLowerInvariant());
                if (!stageById.TryGetValue(stageId, out Stage targetStage)) continue;

                // Build the entry
                MobEntry entry = BuildEntry(prefab, prefabName, character, targetStage.Label);

                targetStage.Mobs.Add(entry);
                authoredIds.Add(prefabName); // prevent duplicates if prefab appears twice
                injected++;
            }

            Debug.Log($"[MobDataGenerator] Done — {injected} mob entries injected, " +
                      $"{skipped} already authored.");
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private static MobEntry BuildEntry(GameObject prefab, string prefabName,
            Character character, string biomeLabel)
        {
            string label = TryLocalise(character.m_name);
            if (string.IsNullOrEmpty(label) || label == character.m_name)
                label = FormatPrefabName(prefabName);

            var entry = new MobEntry
            {
                PrefabId = prefabName,
                Label = label,
                Biome = biomeLabel,
                ModRequired = null,   // can't reliably detect owning mod from prefab alone
                IsTameable = prefab.GetComponent<Tameable>() != null,
                Health = (int)character.m_health,
                SpawnChanceDay = 0,
                SpawnChanceNight = 0,
                Resistances = ExtractResistances(character.m_damageModifiers),
                Drops = ExtractDrops(prefab),
                Taming = null,
                Note = "Auto-detected"
            };

            // Taming food items
            Tameable tameable = prefab.GetComponent<Tameable>();
            if (tameable != null && tameable.m_fedDuration > 0)
            {
                entry.Taming = new TamingInfo
                {
                    FoodItems = new List<string>(),
                    Note = null
                };
            }

            return entry;
        }

        private static List<MobDrop> ExtractDrops(GameObject prefab)
        {
            CharacterDrop dropComp = prefab.GetComponent<CharacterDrop>();
            if (dropComp == null || dropComp.m_drops == null || dropComp.m_drops.Count == 0)
                return new List<MobDrop>();

            var drops = new List<MobDrop>();
            foreach (var drop in dropComp.m_drops)
            {
                if (drop.m_prefab == null) continue;

                ItemDrop itemDrop = drop.m_prefab.GetComponent<ItemDrop>();
                string label = itemDrop != null
                    ? TryLocalise(itemDrop.m_itemData.m_shared.m_name)
                    : FormatPrefabName(drop.m_prefab.name);

                drops.Add(new MobDrop
                {
                    ItemId = drop.m_prefab.name,
                    Label = label,
                    Chance = drop.m_chance,
                    Min = drop.m_amountMin,
                    Max = drop.m_amountMax
                });
            }
            return drops;
        }

        private static Dictionary<string, string> ExtractResistances(
            HitData.DamageModifiers mods)
        {
            var dict = new Dictionary<string, string>();
            AddIfNotNormal(dict, "Blunt", mods.m_blunt);
            AddIfNotNormal(dict, "Slash", mods.m_slash);
            AddIfNotNormal(dict, "Pierce", mods.m_pierce);
            AddIfNotNormal(dict, "Fire", mods.m_fire);
            AddIfNotNormal(dict, "Frost", mods.m_frost);
            AddIfNotNormal(dict, "Lightning", mods.m_lightning);
            AddIfNotNormal(dict, "Poison", mods.m_poison);
            AddIfNotNormal(dict, "Spirit", mods.m_spirit);
            return dict;
        }

        private static void AddIfNotNormal(Dictionary<string, string> dict,
            string type, HitData.DamageModifier mod)
        {
            // Only add non-Normal entries — keeps the resistance display clean
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

        // ── Stage mapping ─────────────────────────────────────────────────────

        // Maps a lowercased prefab name to the stage it belongs to.
        // Uses the same material-name heuristics as TherzieDataGenerator but
        // with character/creature-specific terms added.
        private static string GetStageIdFromName(string lower)
        {
            // Ashlands
            if (Contains(lower, "charred", "flametal", "asksvin", "morgen",
                "valkyrie", "bonemaw", "fader", "surtr", "gjall_ashlands"))
                return "ashlands";

            // Mistlands
            if (Contains(lower, "seeker", "gjall", "tick", "dvergr",
                "hare", "jotun_pufferfish", "queen"))
                return "mistlands";

            // Plains
            if (Contains(lower, "goblin", "fuling", "lox", "deathsquito",
                "growth", "blobtar"))
                return "plains";

            // Mountain
            if (Contains(lower, "wolf", "hatchling", "drake", "fenring",
                "bat", "stonegolem", "golem_stone", "cultist"))
                return "mountain";

            // Swamp
            if (Contains(lower, "draugr", "blob", "wraith", "leech",
                "abomination", "surtling_swamp", "skeleton_swamp"))
                return "swamp";

            // Black Forest
            if (Contains(lower, "greydwarf", "skeleton", "troll",
                "ghost", "surtling_blackforest"))
                return "blackforest";

            // Meadows
            if (Contains(lower, "boar", "deer", "neck", "bird",
                "greyling", "crow"))
                return "meadows";

            // Deep North / Ocean — map to deepnorth or skip
            if (Contains(lower, "serpent", "jotunn", "icegolem"))
                return "deepnorth";

            // Unidentifiable — use Other so it's not lost
            Debug.LogWarning(
                $"[MobDataGenerator] Could not map '{lower}' to a stage — assigning Other.");
            return "other";
        }

        private static bool Contains(string value, params string[] terms)
        {
            foreach (string term in terms)
                if (value.Contains(term)) return true;
            return false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsOnDenylist(string name)
        {
            string lower = name.ToLowerInvariant();
            foreach (string sub in DenylistSubstrings)
                if (lower.Contains(sub)) return true;
            return false;
        }

        private static string TryLocalise(string key)
        {
            if (Localization.instance == null || string.IsNullOrEmpty(key)) return key;
            try { return Localization.instance.Localize(key); }
            catch { return key; }
        }

        /// <summary>
        /// Converts a prefab name like "Razorback_TW" into "Razorback Tw"
        /// as a readable fallback when localisation returns nothing useful.
        /// </summary>
        private static string FormatPrefabName(string prefabName)
        {
            return prefabName
                .Replace("_", " ")
                .Replace("-", " ");
        }
    }
}