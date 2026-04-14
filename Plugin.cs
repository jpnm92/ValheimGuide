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
        public const string PluginGuid = "com.custom.valheimguide";
        public const string PluginName = "ValheimGuide";
        public const string PluginVersion = "0.1.0";

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        private Harmony _harmony;
        private ConfigEntry<KeyboardShortcut> _toggleGuideKey;
        public static ConfigEntry<float> TrackerOffsetX;
        public static ConfigEntry<float> TrackerOffsetY;
        public static ConfigEntry<float> TrackerScale;
        public static ConfigEntry<float> TrackerWidth;
        public static ConfigEntry<int> TrackerMaxRows;
        public static ConfigEntry<float> TrackerRefreshRate;

        private void Awake()
        {
            Instance = this;
            Log = base.Logger;

            GuideDataLoader.InstalledMods = new HashSet<string>();
            foreach (var plugin in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
                GuideDataLoader.InstalledMods.Add(plugin.Metadata.GUID);

            ProgressionTracker.Initialise(Log);
            ProgressSaver.Initialise(Log);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            _toggleGuideKey = Config.Bind("General", "ToggleGuide",
                new KeyboardShortcut(KeyCode.F8), "Key to open/close the guide.");

            // UPDATED OFFSETS AND NEW SCALE CONFIG
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

            TrackerRefreshRate = Config.Bind("UI", "TrackerRefreshRate", 3f,
                "How often (in seconds) the tracker checks your inventory for materials. Higher = better performance, lower = more responsive.");
            ObjectiveTracker.Initialise();

            Log.LogInfo($"{PluginName} loaded.");
        }

        public static void LoadGuideData()
        {
            string dataFolder = System.IO.Path.Combine(
                Paths.PluginPath, PluginName, "data");

            TherzieDataGenerator.GenerateIfPresent();
            GuideDataLoader.Load(dataFolder, Log);
            GuideDataEnricher.Run();
            ProgressionTracker.RefreshCurrentStage();

            Log.LogInfo($"{PluginName} ready. " +
                        $"Stages: {GuideDataLoader.AllStages.Count}, " +
                        $"Playstyles: {GuideDataLoader.Playstyles.Count}");
        }

        private void Update()
        {
            if (GuidePanel.IsVisible)
                Time.timeScale = 0f;

            if (_toggleGuideKey.Value.IsDown())
                GuidePanel.Toggle();

            if (GuidePanel.IsVisible &&
                (Input.GetKeyDown(KeyCode.Escape) ||
                 Input.GetKeyDown(KeyCode.Tab)))
                GuidePanel.Hide();
        }
    }
}