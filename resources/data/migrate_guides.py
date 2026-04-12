import json
import os
import glob
import sys

# Default resistance block for vanilla mobs where we don't have spreadsheet data
DEFAULT_RESISTANCES = {
    "Blunt": "Normal",
    "Slash": "Normal", 
    "Pierce": "Normal",
    "Fire": "Normal",
    "Poison": "Normal",
    "Lightning": "Normal",
    "Spirit": "Immune",
    "Frost": "Normal"
}

# Known vanilla mob data — health, resistances, taming, notes
# Sourced from game data / wiki
VANILLA_MOBS = {
    "Boar": {
        "Health": 10, "SpawnChanceDay": 0.25, "SpawnChanceNight": 0.35,
        "IsTameable": True,
        "Resistances": DEFAULT_RESISTANCES.copy(),
        "Taming": {"FoodItems": ["Mushroom", "Raspberry", "Blueberries", "Carrot"], "Note": "Pen with roundpole fences before taming"},
        "Note": None
    },
    "Deer": {
        "Health": 10, "SpawnChanceDay": 0.30, "SpawnChanceNight": 0.10,
        "IsTameable": False,
        "Resistances": DEFAULT_RESISTANCES.copy(),
        "Taming": None,
        "Note": "Spooked easily — sneak or use a bow"
    },
    "Neck": {
        "Health": 12, "SpawnChanceDay": 0.0, "SpawnChanceNight": 0.40,
        "IsTameable": False,
        "Resistances": DEFAULT_RESISTANCES.copy(),
        "Taming": None,
        "Note": "Spawns near water, mostly at night"
    },
    "Greydwarf": {
        "Health": 40, "SpawnChanceDay": 0.20, "SpawnChanceNight": 0.40,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Fire": "Weak", "Poison": "Resistant", "Spirit": "Immune"},
        "Taming": None,
        "Note": "Weak to fire — fire arrows effective"
    },
    "Greydwarf_Elite": {
        "Health": 120, "SpawnChanceDay": 0.05, "SpawnChanceNight": 0.15,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Fire": "Weak", "Poison": "Resistant", "Spirit": "Immune"},
        "Taming": None,
        "Note": "Throws rocks at range — stay mobile"
    },
    "Greydwarf_Shaman": {
        "Health": 80, "SpawnChanceDay": 0.05, "SpawnChanceNight": 0.10,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Fire": "Weak", "Poison": "Resistant", "Spirit": "Immune"},
        "Taming": None,
        "Note": "Heals nearby enemies — prioritise killing first"
    },
    "Troll": {
        "Health": 600, "SpawnChanceDay": 0.05, "SpawnChanceNight": 0.10,
        "IsTameable": False,
        "Resistances": DEFAULT_RESISTANCES.copy(),
        "Taming": None,
        "Note": "Stay ranged — one hit can kill early characters"
    },
    "Surtling": {
        "Health": 20, "SpawnChanceDay": 0.10, "SpawnChanceNight": 0.10,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Fire": "Immune", "Frost": "Weak"},
        "Taming": None,
        "Note": "Immune to fire — use frost or physical damage"
    },
    "Skeleton": {
        "Health": 40, "SpawnChanceDay": 0.10, "SpawnChanceNight": 0.30,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Blunt": "Weak", "Poison": "Immune", "Spirit": "Weak"},
        "Taming": None,
        "Note": "Weak to blunt and spirit damage"
    },
    "Draugr": {
        "Health": 100, "SpawnChanceDay": 0.30, "SpawnChanceNight": 0.50,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Spirit": "Weak"},
        "Taming": None,
        "Note": "Weak to Spirit damage"
    },
    "Draugr_Elite": {
        "Health": 200, "SpawnChanceDay": 0.10, "SpawnChanceNight": 0.20,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Spirit": "Weak"},
        "Taming": None,
        "Note": "Hits hard — keep distance and use spirit weapons"
    },
    "Leech": {
        "Health": 40, "SpawnChanceDay": 0.30, "SpawnChanceNight": 0.30,
        "IsTameable": False,
        "Resistances": DEFAULT_RESISTANCES.copy(),
        "Taming": None,
        "Note": "Only found in water — fight from the shore"
    },
    "Blob": {
        "Health": 40, "SpawnChanceDay": 0.20, "SpawnChanceNight": 0.40,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Poison": "Immune"},
        "Taming": None,
        "Note": "Immune to poison — use blunt or fire"
    },
    "BlobElite": {
        "Health": 200, "SpawnChanceDay": 0.05, "SpawnChanceNight": 0.15,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Poison": "Immune"},
        "Taming": None,
        "Note": "Explodes on death — back away when it dies"
    },
    "Wolf": {
        "Health": 60, "SpawnChanceDay": 0.20, "SpawnChanceNight": 0.40,
        "IsTameable": True,
        "Resistances": {**DEFAULT_RESISTANCES, "Frost": "Resistant"},
        "Taming": {"FoodItems": ["RawMeat", "DeerMeat", "NeckTail", "LoxMeat"], "Note": "Chain it first — approach slowly"},
        "Note": "Pack hunter — lure singles away before engaging"
    },
    "Drake": {
        "Health": 100, "SpawnChanceDay": 0.15, "SpawnChanceNight": 0.15,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Frost": "Immune", "Fire": "Weak"},
        "Taming": None,
        "Note": "Immune to frost — use fire damage"
    },
    "StoneGolem": {
        "Health": 800, "SpawnChanceDay": 0.05, "SpawnChanceNight": 0.05,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Blunt": "Weak", "Pierce": "Resistant", "Slash": "Resistant"},
        "Taming": None,
        "Note": "Weak to blunt — pickaxe deals bonus damage"
    },
    "Fenring": {
        "Health": 200, "SpawnChanceDay": 0.0, "SpawnChanceNight": 0.20,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Frost": "Resistant"},
        "Taming": None,
        "Note": "Night only — very fast and aggressive"
    },
    "Fuling": {
        "Health": 90, "SpawnChanceDay": 0.30, "SpawnChanceNight": 0.50,
        "IsTameable": False,
        "Resistances": DEFAULT_RESISTANCES.copy(),
        "Taming": None,
        "Note": "Often in groups — do not engage large camps alone"
    },
    "Fuling_Berserker": {
        "Health": 300, "SpawnChanceDay": 0.10, "SpawnChanceNight": 0.20,
        "IsTameable": False,
        "Resistances": DEFAULT_RESISTANCES.copy(),
        "Taming": None,
        "Note": "Charges aggressively — parry the charge for a stagger"
    },
    "Lox": {
        "Health": 1000, "SpawnChanceDay": 0.10, "SpawnChanceNight": 0.10,
        "IsTameable": True,
        "Resistances": {**DEFAULT_RESISTANCES, "Poison": "Resistant"},
        "Taming": {"FoodItems": ["Cloudberry", "Barley", "Flax"], "Note": "Tamed Lox can be ridden with a saddle"},
        "Note": "Avoid provoking herds — focus single targets"
    },
    "Deathsquito": {
        "Health": 20, "SpawnChanceDay": 0.30, "SpawnChanceNight": 0.30,
        "IsTameable": False,
        "Resistances": DEFAULT_RESISTANCES.copy(),
        "Taming": None,
        "Note": "One-shots weak characters — always wear Plains-tier armor"
    },
    "Seeker": {
        "Health": 300, "SpawnChanceDay": 0.20, "SpawnChanceNight": 0.30,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Poison": "Resistant"},
        "Taming": None,
        "Note": "Flanks aggressively — keep your back to a wall"
    },
    "SeekerBrood": {
        "Health": 50, "SpawnChanceDay": 0.20, "SpawnChanceNight": 0.20,
        "IsTameable": False,
        "Resistances": DEFAULT_RESISTANCES.copy(),
        "Taming": None,
        "Note": "Swarms in numbers — AoE weapons effective"
    },
    "Tick": {
        "Health": 20, "SpawnChanceDay": 0.20, "SpawnChanceNight": 0.20,
        "IsTameable": False,
        "Resistances": DEFAULT_RESISTANCES.copy(),
        "Taming": None,
        "Note": "Latches on and drains health — check your character regularly"
    },
    "Charred_Melee": {
        "Health": 150, "SpawnChanceDay": 0.30, "SpawnChanceNight": 0.40,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Fire": "Immune", "Frost": "Weak", "Poison": "Weak"},
        "Taming": None,
        "Note": "Immune to fire — use frost or poison weapons"
    },
    "Jotunn": {
        "Health": 400, "SpawnChanceDay": 0.10, "SpawnChanceNight": 0.10,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Frost": "Immune", "Fire": "Weak"},
        "Taming": None,
        "Note": "Immune to frost — fire weapons recommended"
    },
    "IceGolem": {
        "Health": 600, "SpawnChanceDay": 0.05, "SpawnChanceNight": 0.05,
        "IsTameable": False,
        "Resistances": {**DEFAULT_RESISTANCES, "Frost": "Immune", "Fire": "Weak", "Blunt": "Weak"},
        "Taming": None,
        "Note": "Weak to fire and blunt — pickaxe or fire weapons"
    }
}

def mob_id_to_known(mob_name):
    """Try to match a mob source name to our known mob database."""
    # Direct match
    if mob_name in VANILLA_MOBS:
        return mob_name, VANILLA_MOBS[mob_name]
    # Partial match
    for key in VANILLA_MOBS:
        if key.lower() in mob_name.lower() or mob_name.lower() in key.lower():
            return key, VANILLA_MOBS[key]
    return None, None

def convert_drops_to_mobs(drops):
    """
    Convert old DropEntry list (item-centric) to new MobEntry list (mob-centric).
    Groups drop sources by mob name.
    """
    mob_map = {}  # mob_name -> MobEntry dict

    for drop in drops:
        item_id = drop.get("ItemId", "")
        item_label = drop.get("Label", "")
        mod_required = drop.get("ModRequired")
        sources = drop.get("Sources", [])

        for source in sources:
            mob_name = source.get("Mob", "Unknown")
            biome = source.get("Biome", "")
            chance = source.get("Chance", 1.0)
            min_amt = source.get("Min", 1)
            max_amt = source.get("Max", 1)

            if mob_name not in mob_map:
                # Look up known data
                known_key, known_data = mob_id_to_known(mob_name)

                if known_data:
                    mob_map[mob_name] = {
                        "PrefabId": known_key,
                        "Label": mob_name,
                        "Biome": biome,
                        "ModRequired": mod_required,
                        "IsTameable": known_data["IsTameable"],
                        "Health": known_data["Health"],
                        "SpawnChanceDay": known_data["SpawnChanceDay"],
                        "SpawnChanceNight": known_data["SpawnChanceNight"],
                        "Resistances": known_data["Resistances"],
                        "Drops": [],
                        "Taming": known_data["Taming"],
                        "Note": known_data["Note"]
                    }
                else:
                    # Unknown mob — create skeleton with TODO markers
                    print(f"  [WARN] Unknown mob '{mob_name}' — generating skeleton, fill in manually")
                    mob_map[mob_name] = {
                        "PrefabId": mob_name,
                        "Label": mob_name,
                        "Biome": biome,
                        "ModRequired": mod_required,
                        "IsTameable": False,
                        "Health": 0,
                        "SpawnChanceDay": 0.0,
                        "SpawnChanceNight": 0.0,
                        "Resistances": DEFAULT_RESISTANCES.copy(),
                        "Drops": [],
                        "Taming": None,
                        "Note": "TODO: fill in note"
                    }

            # Add drop to this mob
            mob_map[mob_name]["Drops"].append({
                "ItemId": item_id,
                "Label": item_label,
                "Chance": chance,
                "Min": min_amt,
                "Max": max_amt
            })

    return list(mob_map.values())

def migrate_stage(stage):
    """Migrate a single stage to the new schema."""
    # Convert Drops -> Mobs
    old_drops = stage.pop("Drops", [])
    if old_drops:
        stage["Mobs"] = convert_drops_to_mobs(old_drops)
    else:
        stage["Mobs"] = []

    # Add new fields with defaults if missing
    if "Article" not in stage:
        stage["Article"] = ""
    if "BonusBosses" not in stage:
        stage["BonusBosses"] = []
    if "Objectives" not in stage:
        stage["Objectives"] = []

    # Convert flat Tips strings -> Tip objects if needed
    raw_tips = stage.get("Tips", [])
    if raw_tips and isinstance(raw_tips[0], str):
        stage["Tips"] = [{"Text": t, "Category": "general", "ModRequired": None} for t in raw_tips]
    elif not raw_tips:
        stage["Tips"] = []

    # Add PlaystyleTag to gear entries if missing
    for gear in stage.get("Gear", []):
        if "PlaystyleTag" not in gear:
            gear["PlaystyleTag"] = None

    # Add Strategy and PrefabId to boss if present
    boss = stage.get("Boss")
    if boss:
        if "Strategy" not in boss:
            boss["Strategy"] = ""
        if "PrefabId" not in boss:
            boss["PrefabId"] = boss.get("Name", "").replace(" ", "")
        if "IsBonus" not in boss:
            boss["IsBonus"] = False
        if "ModRequired" not in boss:
            boss["ModRequired"] = None
        if "Drops" not in boss:
            boss["Drops"] = []

    return stage

def migrate_file(input_path, output_path):
    print(f"\nMigrating: {os.path.basename(input_path)}")

    with open(input_path, "r", encoding="utf-8-sig") as f:
        data = json.load(f)

    stages = data.get("Stages", [])
    migrated = []

    for stage in stages:
        migrated.append(migrate_stage(stage))
        print(f"  Stage '{stage['Id']}' — {len(stage.get('Mobs', []))} mobs, {len(stage.get('Objectives', []))} objectives")

    data["Stages"] = migrated

    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    print(f"  -> Written to {output_path}")

def main():
    input_dir = sys.argv[1] if len(sys.argv) > 1 else "."
    output_dir = sys.argv[2] if len(sys.argv) > 2 else os.path.join(input_dir, "migrated")

    os.makedirs(output_dir, exist_ok=True)

    guide_files = glob.glob(os.path.join(input_dir, "*.guide"))

    if not guide_files:
        print(f"No .guide files found in {input_dir}")
        return

    print(f"Found {len(guide_files)} .guide files")
    print(f"Output directory: {output_dir}")

    for path in sorted(guide_files):
        filename = os.path.basename(path)
        out_path = os.path.join(output_dir, filename)
        try:
            migrate_file(path, out_path)
        except Exception as e:
            print(f"  [ERROR] {filename}: {e}")

    print(f"\nDone. Migrated files are in: {output_dir}")
    print("Review any [WARN] lines above — those mobs need manual data.")

if __name__ == "__main__":
    main()
