using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using ValheimGuide.Data;
using ValheimGuide.DataGenerators;
using ValheimGuide.UI;

namespace ValheimGuide
{
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("Therzie.Armory", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Therzie.Warfare", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.fafo.valheimguide";
        public const string PluginName = "ValheimGuide";
        public const string PluginVersion = "1.0.0";

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        private Harmony _harmony;
        private ConfigEntry<KeyboardShortcut> _toggleGuideKey;
        private bool _wasGuideOpen = false;

        public static ConfigEntry<float> TrackerOffsetX;
        public static ConfigEntry<float> TrackerOffsetY;
        public static ConfigEntry<float> TrackerScale;
        public static ConfigEntry<float> TrackerWidth;
        public static ConfigEntry<int> TrackerMaxRows;
        public static ConfigEntry<float> TrackerRefreshRate;
        public static ConfigEntry<int> TrackerFontSize;
        public static ConfigEntry<int> TrackerMaxPins;
        public static ConfigEntry<float> TrackerOpacity;
        public static ConfigEntry<bool> PauseOnGuideOpen;

        private void Awake()
        {
            Instance = this;
            Log = base.Logger;

            GuideDataLoader.InstalledMods.Clear();
            foreach (var plugin in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
                GuideDataLoader.InstalledMods.Add(plugin.Metadata.GUID);

            ProgressionTracker.Initialise(Log);
            ProgressSaver.Initialise(Log);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            _toggleGuideKey = Config.Bind("General", "ToggleGuide",
                new KeyboardShortcut(KeyCode.F8), "Key to open/close the guide.");

            PauseOnGuideOpen = Config.Bind("General", "PauseOnGuideOpen", true,
                "Pause the game when the guide is opened. Recommended to disable in multiplayer.");

            TrackerMaxPins = Config.Bind("UI", "TrackerMaxPins", 5,
                new ConfigDescription("Maximum number of recipes that can be pinned to the tracker.",
                new AcceptableValueRange<int>(1, 10)));

            TrackerOffsetX = Config.Bind("UI", "TrackerOffsetX", -20f,
                "X offset for the on-screen objective tracker (from the top-right corner).");

            TrackerOffsetY = Config.Bind("UI", "TrackerOffsetY", -400f,  // Pushed down to clear Buffs!
                "Y offset for the on-screen objective tracker (from the top-right corner).");

            TrackerScale = Config.Bind("UI", "TrackerScale", 1.0f,
                "Master scale multiplier for the objective tracker.");

            TrackerWidth = Config.Bind("UI", "TrackerWidth", 320f,
                "How wide the tracker panel is. Increase this if your objective text is wrapping too much.");

            TrackerMaxRows = Config.Bind("UI", "TrackerMaxRows", 6,
                "The maximum number of objectives to show on screen at once.");

            TrackerRefreshRate = Config.Bind("UI", "TrackerRefreshRate", 1.5f,
                "How often (in seconds) the tracker checks your inventory for materials. Higher = better performance, lower = more responsive.");

            TrackerFontSize = Config.Bind("UI", "TrackerFontSize", 15,
                new ConfigDescription(
                    "Font size for text in the on-screen objective tracker.",
                new AcceptableValueRange<int>(10, 22)));

            TrackerOpacity = Config.Bind("UI", "TrackerOpacity", 0.82f,
                new ConfigDescription("Background opacity of the on-screen objective tracker.",
                new AcceptableValueRange<float>(0.1f, 1.0f)));
            Jotunn.Managers.PrefabManager.OnVanillaPrefabsAvailable += LoadGuideData;

            ObjectiveTracker.Initialise();
            Log.LogInfo($"{PluginName} loaded.");
        }
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Jotunn.Managers.PrefabManager.OnVanillaPrefabsAvailable -= LoadGuideData;

            // Restore timescale if guide was open when plugin was destroyed
            if (GuidePanel.IsVisible)
                GuidePanel.Hide();
        }

        // Phase 1: load JSON data — runs on OnVanillaPrefabsAvailable.
        // Enricher is intentionally NOT called here because mod items
        // (Therzie etc.) are not yet registered into ObjectDB at this point.
        public static void LoadGuideData()
        {
            if (GuideDataLoader.AllStages.Count > 0)
            {
                Log.LogWarning($"[{PluginName}] LoadGuideData called again — skipping.");
                return;
            }

            string dataFolder = System.IO.Path.Combine(
                Paths.PluginPath, PluginName, "data");

            TherzieDataGenerator.GenerateIfPresent();
            GuideDataLoader.Load(dataFolder, Log);
            ObjectiveTracker.InvalidateLabelCache();
            ProgressionTracker.RefreshCurrentStage();

            Log.LogInfo($"{PluginName} guide data loaded. " +
                        $"Stages: {GuideDataLoader.AllStages.Count}, " +
                        $"Playstyles: {GuideDataLoader.Playstyles.Count}");
        }

        // Phase 2: enrich with ObjectDB data — called from ObjectDBAwakePatch
        // after ALL mods (including Therzie) have registered their items.
        public static void EnrichGuideData()
        {
            if (GuideDataLoader.AllStages.Count == 0)
            {
                Log.LogWarning($"[{PluginName}] EnrichGuideData called before LoadGuideData — skipping.");
                return;
            }

            GuideDataEnricher.Run();
            EncyclopediaIndex.Invalidate();

            Log.LogInfo($"{PluginName} enrichment complete.");
        }


        private void Update()
        {
            if (_toggleGuideKey.Value.IsDown())
                GuidePanel.Toggle();

            if (GuidePanel.IsVisible &&
                (Input.GetKeyDown(KeyCode.Escape) ||
                 Input.GetKeyDown(KeyCode.Tab)))
                GuidePanel.Hide();

            bool guideOpen = GuidePanel.IsVisible;
            if (guideOpen != _wasGuideOpen)
            {
                _wasGuideOpen = guideOpen;
                ObjectiveTracker.SetVisible(!guideOpen);
            }

            // ── Re-enforce pause every frame while guide is open ──────────────────
            // Valheim's own update loop (ZNet, Game) resets Time.timeScale each
            // frame, so a one-shot set in Show() loses the race. Enforcing here
            // in Update() wins reliably.
            if (guideOpen && PauseOnGuideOpen.Value)
            {
                // > 0 peers = someone is connected = multiplayer — don't pause
                bool isMultiplayer = ZNet.instance != null &&
                                     ZNet.instance.GetNrOfPlayers() > 1;
                if (!isMultiplayer)
                    Time.timeScale = 0f;
            }
        }

    }
}