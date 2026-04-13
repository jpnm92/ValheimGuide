using BepInEx;
using BepInEx.Configuration;
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

        private Harmony _harmony;
        private ConfigEntry<KeyboardShortcut> _toggleGuideKey;

        private void Awake()
        {
            Instance = this;

            GuideDataLoader.InstalledMods = new HashSet<string>();
            foreach (var plugin in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
                GuideDataLoader.InstalledMods.Add(plugin.Metadata.GUID);

            ProgressionTracker.Initialise(Logger);
            ProgressSaver.Initialise(Logger);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            _toggleGuideKey = Config.Bind("General", "ToggleGuide",
                new KeyboardShortcut(KeyCode.F8), "Key to open/close the guide.");

            // Initialise the on-screen tracker
            ObjectiveTracker.Initialise();

            Logger.LogInfo($"{PluginName} loaded.");
        }

        public static void LoadGuideData()
        {
            string dataFolder = System.IO.Path.Combine(
                Paths.PluginPath, PluginName, "data");

            TherzieDataGenerator.GenerateIfPresent(); // 1. write generated files (guarded by _hasRun)
            GuideDataLoader.Load(dataFolder, Instance.Logger); // 2. reload all stages + playstyles
            GuideDataEnricher.Run();                  // 3. enrich with live ObjectDB data
            ProgressionTracker.RefreshCurrentStage(); // 4. set current stage

            Instance.Logger.LogInfo(
                $"{PluginName} ready. Stages: {GuideDataLoader.AllStages.Count}, " +
                $"Playstyles: {GuideDataLoader.Playstyles.Count}");
        }

        private void Update()
        {
            // Enforce pause every frame while guide is open
            if (GuidePanel.IsVisible)
                Time.timeScale = 0f;

            // Toggle key
            if (_toggleGuideKey.Value.IsDown())
                GuidePanel.Toggle();

            // Tab or Escape closes the guide
            if (GuidePanel.IsVisible &&
                (UnityEngine.Input.GetKeyDown(KeyCode.Escape) ||
                 UnityEngine.Input.GetKeyDown(KeyCode.Tab)))
                GuidePanel.Hide();
        }
    }
}