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

        private Harmony _harmony;
        private ConfigEntry<KeyboardShortcut> _toggleGuideKey;
        private void Awake()
        {
            GuideDataLoader.InstalledMods = new HashSet<string>();
            foreach (var plugin in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
                GuideDataLoader.InstalledMods.Add(plugin.Metadata.GUID);

            ProgressionTracker.Initialise(Logger);
            ProgressSaver.Initialise(Logger);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            _toggleGuideKey = Config.Bind("General", "ToggleGuide", new KeyboardShortcut(KeyCode.F8), "Key to open/close the guide.");

            Jotunn.Managers.ItemManager.OnItemsRegistered += OnItemsRegistered;

            Logger.LogInfo($"{PluginName} loaded.");
        }

        private void OnItemsRegistered()
        {
            string dataFolder = System.IO.Path.Combine(Paths.PluginPath, "ValheimGuide", "data");

            TherzieDataGenerator.GenerateIfPresent(); // 1. write generated .guide files if mods present
            GuideDataLoader.Load(dataFolder, Logger); // 2. load all files including generated ones
            GuideDataEnricher.Run();                 // 3. enrich with live ObjectDB data
            ProgressionTracker.RefreshCurrentStage(); // 4. set initial stage

            Logger.LogInfo($"{PluginName} ready. Stages: {GuideDataLoader.AllStages.Count}");
        }
        private void Update()
        {
            // Enforce pause every frame while guide is open
            if (GuidePanel.IsVisible)
                Time.timeScale = 0f;

            if (_toggleGuideKey.Value.IsDown())
                GuidePanel.Toggle();

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && GuidePanel.IsVisible)
                GuidePanel.Hide();
        }
    }
}