using System.Collections.Generic;

namespace ValheimGuide.Data
{
    // ─────────────────────────────────────────
    //  TOP LEVEL — one per .guide file
    // ─────────────────────────────────────────

    public class GuideData
    {
        public List<Stage> Stages { get; set; } = new List<Stage>();
    }

    // ─────────────────────────────────────────
    //  STAGE
    // ─────────────────────────────────────────

    public class Stage
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public int Order { get; set; }
        public string BiomeDescription { get; set; }   // one-line flavour
        public string Article { get; set; }            // full guide text, rich-text formatted
        public string ModRequired { get; set; }        // null = vanilla

        public Trigger UnlockTrigger { get; set; }
        public List<string> PriorityMaterials { get; set; } = new List<string>();

        public BossInfo Boss { get; set; }
        public List<BossInfo> BonusBosses { get; set; } = new List<BossInfo>(); // Therzie bonus bosses

        public List<Objective> Objectives { get; set; } = new List<Objective>();
        public List<GearEntry> Gear { get; set; } = new List<GearEntry>();
        public List<MobEntry> Mobs { get; set; } = new List<MobEntry>();        // replaces Drops
        public List<RecipeEntry> Recipes { get; set; } = new List<RecipeEntry>();
        public List<Tip> Tips { get; set; } = new List<Tip>();            // short tip strings
    }

    // ─────────────────────────────────────────
    //  TIP
    // ─────────────────────────────────────────

    public class Tip
    {
        public string Text { get; set; }
        public string Category { get; set; }  // "combat"|"gathering"|"secret"|"building"|"general"
        public string ModRequired { get; set; } // null = always shown
    }

    // ─────────────────────────────────────────
    //  TRIGGER
    // ─────────────────────────────────────────

    public class Trigger
    {
        // "globalKey" | "hasItem" | "knownRecipe" | "none"
        public string Type { get; set; }
        public string Value { get; set; }
    }

    // ─────────────────────────────────────────
    //  OBJECTIVE
    // ─────────────────────────────────────────

    public class Objective
    {
        public string Id { get; set; }          // unique within stage e.g. "craft_forge"
        public string Text { get; set; }        // display string e.g. "Build a Forge"
        public string Type { get; set; }        // see below
        public string Value { get; set; }       // item id, global key, etc — depends on Type
        public int Count { get; set; }          // for hasItem: required quantity (0 = just needs > 0)
        public bool AutoComplete { get; set; }  // true = mod checks it, false = player ticks manually
        public string PlaystyleFilter { get; set; } // null = always shown, or "hunter","warrior" etc
        public string ModRequired { get; set; } // null = always shown
    }

    // Objective.Type values:
    //   "globalKey"    — AutoComplete: checks ZoneSystem for Value
    //   "craftItem"    — AutoComplete: checks known recipes for Value  
    //   "hasItem"      — AutoComplete: checks inventory for Value
    //   "build"        — AutoComplete: false, manual tick (build stations)
    //   "boss"         — AutoComplete: checks globalKey, shown in boss brief
    //   "explore"      — AutoComplete: false, manual tick (find locations)
    //   "manual"       — AutoComplete: false, always manual

    // ─────────────────────────────────────────
    //  BOSS
    // ─────────────────────────────────────────

    public class BossInfo
    {
        public string Name { get; set; }
        public string PrefabId { get; set; }           // for future icon lookup
        public string Location { get; set; }
        public string GlobalKeyDrop { get; set; }
        public string GlobalUnlock { get; set; }
        public string RecommendedGear { get; set; }
        public string Strategy { get; set; }           // one-line fight tip
        public bool IsBonus { get; set; }              // true = Therzie bonus boss
        public string ModRequired { get; set; }        // null = vanilla

        public List<ItemStack> SummonMaterials { get; set; } = new List<ItemStack>();
        public List<string> KeyDrops { get; set; } = new List<string>();
        public List<string> WeakAgainst { get; set; } = new List<string>();
        public List<string> ResistantTo { get; set; } = new List<string>();
        public List<string> ImmuneAgainst { get; set; } = new List<string>();

        // Drop table
        public List<BossDrop> Drops { get; set; } = new List<BossDrop>();
    }

    public class BossDrop
    {
        public string ItemId { get; set; }
        public string Label { get; set; }
        public float Chance { get; set; }   // 0.0 – 1.0
    }

    // ─────────────────────────────────────────
    //  GEAR
    // ─────────────────────────────────────────

    public class GearEntry
    {
        public string ItemId { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }        // "Weapon"|"Armor"|"Tool"|"Shield"|"Bow"
        public string Station { get; set; }
        public int StationLevel { get; set; }
        public string ModRequired { get; set; }
        public string PlaystyleTag { get; set; }  // "hunter"|"warrior" etc, null = always shown
        public List<ItemStack> Recipe { get; set; } = new List<ItemStack>();

        // Enriched at runtime by GuideDataEnricher
        public List<string> DamageTypes { get; set; } = new List<string>();
        public string ArmorClass { get; set; }      // "Light"|"Heavy"
    }

    // ─────────────────────────────────────────
    //  MOB  (replaces DropEntry + DropSource)
    // ─────────────────────────────────────────

    public class MobEntry
    {
        public string PrefabId { get; set; }        // e.g. "Troll", "Razorback_TW"
        public string Label { get; set; }           // display name
        public string Biome { get; set; }
        public string ModRequired { get; set; }     // null = vanilla
        public bool IsTameable { get; set; }
        public int Health { get; set; }

        // Spawn
        public float SpawnChanceDay { get; set; }   // 0.0 – 1.0
        public float SpawnChanceNight { get; set; }

        // Resistances — value is "Normal"|"Weak"|"Resistant"|"Immune"
        public Dictionary<string, string> Resistances { get; set; } = new Dictionary<string, string>();

        // Drops
        public List<MobDrop> Drops { get; set; } = new List<MobDrop>();

        // Taming
        public TamingInfo Taming { get; set; }      // null if not tameable

        // Short tip shown in mob brief
        public string Note { get; set; }
    }

    public class MobDrop
    {
        public string ItemId { get; set; }
        public string Label { get; set; }
        public float Chance { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class TamingInfo
    {
        public List<string> FoodItems { get; set; } = new List<string>();  // item display names
        public string Note { get; set; }   // optional extra tip e.g. "pen it first"
    }

    // ─────────────────────────────────────────
    //  RECIPES
    // ─────────────────────────────────────────

    public class RecipeEntry
    {
        public string ItemId { get; set; }
        public string Label { get; set; }
        public string Station { get; set; }
        public int StationLevel { get; set; }
        public string ModRequired { get; set; }
        public string UnlockNote { get; set; }
        public List<ItemStack> Ingredients { get; set; } = new List<ItemStack>();
    }

    // ─────────────────────────────────────────
    //  SHARED
    // ─────────────────────────────────────────

    public class ItemStack
    {
        public string ItemId { get; set; }
        public string Label { get; set; }
        public int Amount { get; set; }
    }

    // ─────────────────────────────────────────
    //  PLAYSTYLE  (loaded from playstyles.json)
    // ─────────────────────────────────────────

    public class PlaystyleDefinition
    {
        public string Id { get; set; }          // "hunter"|"warrior"|"rogue" etc
        public string Label { get; set; }       // "Bow / Crossbow"
        public string Description { get; set; } // "Stay at range, pierce damage focus"
        public string ArmorSet { get; set; }    // "Hunter"|"Warrior" etc — for gear filtering
        public string ArmorClass { get; set; }  // "Light"|"Heavy" — matches GearEntry.ArmorClass
        public List<string> WeaponTypes { get; set; } = new List<string>(); // for gear filtering
    }

    public class PlaystyleData
    {
        public List<PlaystyleDefinition> Playstyles { get; set; } = new List<PlaystyleDefinition>();
    }

    // ─────────────────────────────────────────
    //  PROGRESS SAVE  (per character)
    // ─────────────────────────────────────────

    public class GuideProgress
    {
        public string CharacterName { get; set; }
        public string ManualStageOverride { get; set; }
        public HashSet<string> CheckedItems { get; set; } = new HashSet<string>();
        public List<string> PinnedRecipes { get; set; } = new List<string>();

        // First-launch prompt answers
        public bool? ShowFutureStages { get; set; }     // null = not answered yet
        public string PlaystyleId { get; set; }         // null = not chosen yet / show all

        // View state
        public string LastViewMode { get; set; }        // "guide"|"read"
        public string LastStageId { get; set; }         // remember last selected stage
    }
}