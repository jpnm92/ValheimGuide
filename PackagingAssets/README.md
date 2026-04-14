# ValheimGuide

An in-game progression guide for Valheim. Tracks where you are in the game, shows recommended gear, boss info, drop sources, crafting recipes, and objectives for each biome — all without leaving the game.

Supports vanilla progression and optionally integrates with **Therzie's Armory** and **Therzie's Warfare** if those mods are installed.

---

## Features

- **Auto-detects your current progression stage** based on which bosses you have defeated
- **Per-stage overview** — priority materials, boss location, summon requirements, recommended gear, unlock rewards, and one-line fight strategy
- **Build checklist** — station-by-station build objectives per biome, auto-ticked when you place the piece in-game
- **Objectives** — crafting, gathering, and boss objectives with automatic completion detection
- **On-screen quest tracker** — WoW-style collapsible overlay showing current objectives, always visible without opening the guide
- **Read mode** — full long-form biome guide with boss strategies, rare discoveries, and advanced tips
- **Gear tab** — craftable armor and weapons with full recipes, station requirements, and playstyle highlights (★)
- **Drops tab** — mob entries with HP, spawn chances, resistances, drop tables, and taming info
- **Recipes tab** — consumables and misc crafts with ingredients and station level
- **Filters** — filter gear by damage type, armor class, and source (Vanilla / Armory / Warfare)
- **Checkboxes** — mark off gear and recipes you have already crafted, saved per character
- **Search bar** — filter any tab by item name
- **Playstyle system** — choose your combat style on first launch; matching gear is highlighted in each tier
- **Spoiler control** — choose whether to see future biome content before you reach it
- **Stage memory** — the guide remembers which stage and view mode you last had open
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
4. Extract `ValheimGuide.dll` into:
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
| Close the guide | `Escape` or `Tab` |
| Open from the pause menu | Click the **GUIDE** button |
| Collapse the quest tracker | Click the ▲ arrow (top-right of screen) |

The keybind can be changed in the BepInEx config file at:
```
Valheim/BepInEx/config/com.custom.valheimguide.cfg
```

---

## First Launch

On your first character load, ValheimGuide will ask two questions:

1. **Spoilers** — do you want to see objectives and tips for future biomes before you reach them? Most returning players say yes.
2. **Playstyle** — which weapon type do you prefer? ValheimGuide will mark matching gear with a ★ in each tier's gear list.

Both preferences are saved per character and can be reset by deleting your progress file.

---

## Optional Mod Support

ValheimGuide automatically detects whether the following mods are installed and adds their content if so. Neither is required.

| Mod | What gets added |
|-----|-----------------|
| [Therzie's Armory](https://thunderstore.io/c/valheim/p/Therzie/Armory/) | Armory armor sets grouped by biome tier |
| [Therzie's Warfare](https://thunderstore.io/c/valheim/p/Therzie/Warfare/) | Warfare weapons and tools grouped by biome tier |

When both are installed, a **Source** filter appears in the Gear tab so you can view Vanilla, Armory, and Warfare items separately.

---

## Compatibility

- **Valheim** — tested on current live branch
- **BepInEx** — 5.4.x
- **Jotunn** — 2.x
- Should be compatible with most other mods. ValheimGuide only reads game data and patches `ZoneSystem.SetGlobalKey`, `Inventory.AddItem`, `Player.OnSpawned`, `Player.PlacePiece`, and `Menu.Show` — all via non-destructive Harmony postfixes.

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

Progress (checked items, playstyle, last viewed stage) is saved per character at:
```
Valheim/BepInEx/config/ValheimGuide/progress/{CharacterName}_{CharacterID}.json
```

Deleting this file resets all checkboxes and preferences for that character.

---

## Known Issues

- The GUIDE button in the pause menu requires the menu to be opened at least once per session before it appears — this is a limitation of how Valheim initialises the menu UI.
- The on-screen quest tracker position is fixed relative to the top-right corner. If it overlaps another mod's UI, the `TrackerAnchor` offset can be adjusted in `ObjectiveTracker.cs`.

---

## Source Code

[GitHub](https://github.com/jpnm92/ValheimGuide)

---

## Changelog

### 1.0.0
- Initial release
- Full vanilla biome progression (Meadows through Deep North)
- Gear, Drops, and Recipes tabs with search and filters
- Build and progress objectives with automatic completion detection
- On-screen quest tracker (collapsible, WoW-style)
- Read mode with full long-form biome guides
- Playstyle system with per-tier gear highlighting
- Spoiler control and stage memory per character
- Per-character progress saving
- Armory and Warfare auto-generation support with source filtering