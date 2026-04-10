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
    [BepInDependency("Therzie.Armory", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Therzie.Warfare", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.custom.valheimguide";
        public const string PluginName = "ValheimGuide";
        public const string PluginVersion = "0.1.0";

        private Harmony _harmony;

        private void Awake()
        {
            // Detect installed mods
            GuideDataLoader.InstalledMods = new HashSet<string>();
            foreach (var plugin in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
            {
                GuideDataLoader.InstalledMods.Add(plugin.Metadata.GUID);
            }

            // Load JSON data
            string dataFolder = System.IO.Path.Combine(Paths.PluginPath, "ValheimGuide", "data");
            GuideDataLoader.Load(dataFolder, Logger);

            // Initialise other systems
            ProgressionTracker.Initialise(Logger);
            ProgressSaver.Initialise(Logger);
            // Apply Harmony patches
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            TherzieDataGenerator.Register();
            // Hotkey – F8 by default
            Config.Bind("General", "ToggleGuide", new KeyboardShortcut(KeyCode.F8), "Key to open/close the guide.");

            Logger.LogInfo($"{PluginName} loaded. Stages: {GuideDataLoader.AllStages.Count}");
        }

        private void Update()
        {
            if (Config["General", "ToggleGuide"].BoxedValue is KeyboardShortcut shortcut &&
                shortcut.IsDown())
            {
                GuidePanel.Toggle();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && GuidePanel.IsVisible)
            {
                GuidePanel.Hide();
            }
        }
    }
}