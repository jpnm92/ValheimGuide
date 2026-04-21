# ValheimGuide

An in-game progression guide for Valheim. Tracks where you are in the game, shows recommended gear, boss info, drop sources, crafting recipes, and objectives for each biome — all without leaving the game. Supports vanilla progression and optionally integrates with **Therzie's Armory** and **Therzie's Warfare** if those mods are installed.

---

## Features

- **Auto-detects your current progression stage** based on which bosses you have defeated
- **Per-stage overview** — priority materials, boss location, summon requirements, recommended gear, unlock rewards, and one-line fight strategy
- **Build checklist** — station-by-station build objectives per biome, auto-ticked when you place the piece in-game
- **Objectives** — crafting, gathering, and boss objectives with automatic completion detection
- **On-screen quest tracker** — WoW-style collapsible overlay showing current objectives, including live material tracking for crafting and building requirements
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
3. Dependencies (BepInEx, Jötunn) are installed automatically
4. Launch Valheim through the mod manager

### Manual

1. Install [BepInEx for Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
2. Install [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)
3. Download the latest ValheimGuide release
4. Extract `ValheimGuide.dll` and the `data/` folder into:
   ```
   BepInEx/plugins/ValheimGuide/
   ```
   The final layout should look like this:
   ```
   BepInEx/
   └── plugins/
       └── ValheimGuide/
           ├── ValheimGuide.dll
           └── data/
               ├── meadows.guide
               ├── blackforest.guide
               └── ...
   ```
5. Launch Valheim

---

## Usage

| Action | Default |
|--------|---------|
| Open / close the guide | `F8` |
| Close the guide | `Escape` or `Tab` |
| Open from the pause menu | Click the **GUIDE** button |

When you open the guide for the first time you will be asked two questions:

- **Show future stages?** — whether to display content for biomes you haven't reached yet
- **Playstyle** — your preferred combat style; matching gear will be highlighted with ★ throughout the guide

Your answers are saved per character and can be changed at any time from inside the guide.

---

## Configuration

A config file is created at `BepInEx/config/com.fafo.valheimguide.cfg` on first launch. All options can also be edited live with a config manager mod.

| Option | Default | Description |
|--------|---------|-------------|
| `ToggleGuide` | `F8` | Hotkey to open/close the guide |
| `PauseOnGuideOpen` | `true` | Pause the game when the guide opens. Recommended to disable in multiplayer |
| `TrackerOffsetX` | `-20` | Horizontal offset of the on-screen tracker from the top-right corner |
| `TrackerOffsetY` | `-400` | Vertical offset of the on-screen tracker from the top-right corner |
| `TrackerScale` | `1.0` | Master scale multiplier for the tracker |
| `TrackerWidth` | `320` | Width of the tracker panel in pixels |
| `TrackerMaxRows` | `6` | Maximum number of objectives visible at once |
| `TrackerMaxPins` | `5` | Maximum number of recipes pinnable to the tracker (1–10) |
| `TrackerRefreshRate` | `1.5` | How often (in seconds) the tracker checks your inventory. Higher = better performance |
| `TrackerFontSize` | `15` | Font size for tracker text (10–22) |
| `TrackerOpacity` | `0.82` | Background opacity of the tracker panel (0.1–1.0) |

---

## Mod Compatibility

### Therzie's Armory & Warfare

ValheimGuide automatically detects whether **Therzie's Armory** and/or **Therzie's Warfare** are installed. If they are, their items are scanned at startup, automatically sorted into the correct biome tier, and merged into the relevant guide stages. No manual configuration is required.

### Multiplayer

The guide works in multiplayer. It is recommended to set `PauseOnGuideOpen = false` in the config when playing on a server, as the pause function has no effect in multiplayer and may cause a brief freeze for the host on some setups.

### Other Mods

ValheimGuide reads from Valheim's own game data at runtime, so it is generally compatible with other content mods. Items added by other mods will not appear in the guide unless a `.guide` data file is provided for them (see **Custom Data** below).

---

## Custom Data

The guide is entirely data-driven. Each biome's content lives in a `.guide` file (JSON format) inside the `data/` folder. You can edit these files to add, remove, or modify entries without recompiling anything.

Other mod authors can also ship their own `.guide` files to add entries for their items. Files are loaded and merged automatically at startup — stages with the same `Id` are merged rather than duplicated.

Refer to the existing `.guide` files as a template for the JSON structure.

---

## Dependencies

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) `5.4.2202+`
- [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) `2.21.3+`

---

## Source Code

[github.com/jpnm92/ValheimGuide](https://github.com/jpnm92/ValheimGuide)

---

## Changelog

### 1.0.0
- Initial release