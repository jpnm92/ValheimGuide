using System.Collections.Generic;

namespace ValheimGuide.Data
{
    // ─────────────────────────────────────────
    //  TOP LEVEL — one per JSON file
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
        public string Id { get; set; }   // e.g. "swamp"
        public string Label { get; set; }   // e.g. "Swamp"
        public int Order { get; set; }   // sort order across files
        public string BiomeDescription { get; set; }   // short flavour line
        public string ModRequired { get; set; }   // null = vanilla

        public Trigger UnlockTrigger { get; set; }   // what marks this stage as reached
        public List<string> PriorityMaterials { get; set; } = new List<string>(); // item IDs
        public BossInfo Boss { get; set; }
        public List<GearEntry> Gear { get; set; } = new List<GearEntry>();
        public List<DropEntry> Drops { get; set; } = new List<DropEntry>();
        public List<RecipeEntry> Recipes { get; set; } = new List<RecipeEntry>();
    }

    // ─────────────────────────────────────────
    //  TRIGGER  (auto-detection)
    // ─────────────────────────────────────────

    public class Trigger
    {
        // "globalKey" | "hasItem" | "knownRecipe" | "none"
        public string Type { get; set; }
        public string Value { get; set; }
    }

    // ─────────────────────────────────────────
    //  BOSS
    // ─────────────────────────────────────────

    public class BossInfo
    {
        public string Name { get; set; }
        public string Location { get; set; }   // e.g. "Sunken Crypt altar"
        public string GlobalKeyDrop { get; set; }   // e.g. "defeated_bonemass"
        public string GlobalUnlock { get; set; }   // human-readable note
        public string RecommendedGear { get; set; }   // short note
        public List<ItemStack> SummonMaterials { get; set; } = new List<ItemStack>();
        public List<string> KeyDrops { get; set; } = new List<string>(); // item IDs
    }

    // ─────────────────────────────────────────
    //  GEAR
    // ─────────────────────────────────────────

    public class GearEntry
    {
        public string ItemId { get; set; }   // internal name e.g. "SwordIron"
        public string Label { get; set; }   // display name
        public string Type { get; set; }   // "Weapon" | "Armor" | "Tool" | "Shield"
        public string Station { get; set; }   // "Forge" | "Workbench" etc.
        public int StationLevel { get; set; }
        public string ModRequired { get; set; }   // null = vanilla
        public List<ItemStack> Recipe { get; set; } = new List<ItemStack>();
    }

    // ─────────────────────────────────────────
    //  DROPS
    // ─────────────────────────────────────────

    public class DropEntry
    {
        public string ItemId { get; set; }
        public string Label { get; set; }
        public string ModRequired { get; set; }
        public List<DropSource> Sources { get; set; } = new List<DropSource>();
    }

    public class DropSource
    {
        public string Mob { get; set; }   // display name
        public string Biome { get; set; }
        public float Chance { get; set; }   // 0.0 – 1.0
        public int Min { get; set; }
        public int Max { get; set; }
        public bool StarVariantOnly { get; set; }   // true = only drops from ★ mobs
        public float StarChanceBonus { get; set; }   // extra multiplier for ★ variants
    }

    // ─────────────────────────────────────────
    //  RECIPES  (separate from gear — covers
    //  consumables, misc crafts, Therzie items)
    // ─────────────────────────────────────────

    public class RecipeEntry
    {
        public string ItemId { get; set; }
        public string Label { get; set; }
        public string Station { get; set; }
        public int StationLevel { get; set; }
        public string ModRequired { get; set; }
        public string UnlockNote { get; set; }   // e.g. "Discovered on pickup of FlametalOre"
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
    //  PROGRESS SAVE  (per character)
    // ─────────────────────────────────────────

    public class GuideProgress
    {
        public string CharacterName { get; set; }
        public string ManualStageOverride { get; set; }   // null = auto-detected
        public List<string> CheckedItems { get; set; } = new List<string>(); // gear/recipe ItemIds
    }
}