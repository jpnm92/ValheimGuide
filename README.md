# ValheimGuide

An in-game progression guide for Valheim. Tracks where you are in the game, shows recommended gear, boss info, drop sources, and crafting recipes for each biome — all without leaving the game.

Supports vanilla progression and optionally integrates with **Therzie's Armory** and **Therzie's Warfare** if those mods are installed.

---

## Features

- **Auto-detects your current progression stage** based on which bosses you have defeated
- **Per-stage overview** — priority materials, boss location, summon requirements, recommended gear, and unlock rewards
- **Gear tab** — craftable armor and weapons for the current biome with full recipes and station requirements
- **Drops tab** — mob drop sources with chance, min/max quantities, and star-variant notes
- **Recipes tab** — consumables and misc crafts with ingredients and station level
- **Checkboxes** — mark off gear and recipes you have already crafted, saved per character
- **Search bar** — filter any tab by item name
- **Manual stage override** — jump to any biome stage regardless of your actual progression
- **Armory + Warfare support** — if Therzie's Armory or Warfare are installed, their items are automatically generated and grouped under the correct biome tier

---

## Installation

### Recommended — r2modman or Thunderstore Mod Manager

1. Install [r2modman](https://thunderstore.io/package/ebkr/r2modman/) or the Thunderstore Mod Manager
2. Search for **ValheimGuide** and click Install
3. Dependencies (BepInEx, Jotunn) are installed automatically
4. Launch Valheim through the mod manager

### Manual

1. Install [BepInEx for Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
2. Install [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)
3. Download the latest ValheimGuide release
4. Extract `ValheimGuide.dll` and `Newtonsoft.Json.dll` into:
   ```
   Valheim/BepInEx/plugins/ValheimGuide/
   ```
5. Extract the `data/` folder into the same directory:
   ```
   Valheim/BepInEx/plugins/ValheimGuide/data/
   ```

---

## Usage

| Action | Default |
|--------|---------|
| Open / close the guide | `F8` |
| Close the guide | `Escape` |
| Open from the pause menu | Click the **GUIDE** button |

The keybind can be changed in the BepInEx config file at:
```
Valheim/BepInEx/config/com.custom.valheimguide.cfg
```

---

## Optional Mod Support

ValheimGuide automatically detects whether the following mods are installed and adds their content if so. Neither is required.

| Mod | What gets added |
|-----|-----------------|
| [Therzie's Armory](https://thunderstore.io/c/valheim/p/Therzie/Armory/) | Armory armor sets grouped by biome tier |
| [Therzie's Warfare](https://thunderstore.io/c/valheim/p/Therzie/Warfare/) | Warfare weapons and tools grouped by biome tier |

---

## Compatibility

- **Valheim** — tested on current live branch
- **BepInEx** — 5.4.x
- **Jotunn** — 2.x
- Should be compatible with most other mods. ValheimGuide only reads game data and patches `ZoneSystem.SetGlobalKey`, `Inventory.AddItem`, `Player.OnSpawned`, and `Menu.Show` — all via non-destructive Harmony postfixes.

---

## Configuration

The config file is generated on first launch at:
```
Valheim/BepInEx/config/com.custom.valheimguide.cfg
```

| Key | Default | Description |
|-----|---------|-------------|
| `ToggleGuide` | `F8` | Keyboard shortcut to open and close the guide |

---

## Save Data

Progress (checked items) is saved per character at:
```
Valheim/BepInEx/config/ValheimGuide/progress/{CharacterName}_{CharacterID}.json
```

Deleting this file resets all checkboxes for that character.

---

## Known Issues

- The GUIDE button in the pause menu requires the menu to be opened at least once per session before it appears — this is a limitation of how Valheim initialises the menu UI.

---

## Source Code

[GitHub](https://github.com/PLACEHOLDER/ValheimGuide)

---

## Changelog

### 1.0.0
- Initial release
- Full vanilla biome progression (Meadows through Deep North)
- Gear, Drops, and Recipes tabs
- Per-character progress saving
- Armory and Warfare auto-generation support
