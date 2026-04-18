using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using ValheimGuide.Data;

namespace ValheimGuide.Data
{
    public static class ProgressionTracker
    {
        private static ManualLogSource _log;
        private static float _lastRefreshTime;

        // Cached reflection field
        private static readonly FieldInfo _knownRecipesField =
            typeof(Player).GetField("m_knownRecipes", BindingFlags.NonPublic | BindingFlags.Instance);

        public static Stage CurrentStage { get; private set; }
        public static Stage ManualOverrideStage { get; set; }
        public static event Action<Stage> OnStageChanged;

        public static void Initialise(ManualLogSource logger)
        {
            _log = logger;
            ForceRefresh(); // ✅ bypass debounce on startup
        }

        // ✅ Internal forced refresh (ignores debounce)
        private static void ForceRefresh()
        {
            _lastRefreshTime = Time.time;
            Stage newStage = ManualOverrideStage ?? GetHighestCompletedStage();

            if (newStage != CurrentStage)
            {
                CurrentStage = newStage;
                OnStageChanged?.Invoke(CurrentStage);
                _log?.LogInfo($"[ProgressionTracker] Current stage → {CurrentStage?.Label ?? "None"}");
            }
        }

        // ✅ Public debounced refresh
        public static void RefreshCurrentStage()
        {
            if (Time.time - _lastRefreshTime < 1f) return;
            ForceRefresh();
        }

        public static void SetManualOverride(string stageId)
        {
            ManualOverrideStage = GuideDataLoader.GetStageById(stageId);
            if (ProgressSaver.Current != null)
            {
                ProgressSaver.Current.ManualStageOverride = stageId;
            }
            ForceRefresh();
        }

        public static void ClearManualOverride()
        {
            ManualOverrideStage = null;
            if (ProgressSaver.Current != null)
                ProgressSaver.Current.ManualStageOverride = null;
            ForceRefresh();
        }

        private static Stage GetHighestCompletedStage()
        {
            Stage highest = null;
            foreach (Stage stage in GuideDataLoader.AllStages)
            {
                if (IsTriggerSatisfied(stage.UnlockTrigger))
                    highest = stage;
            }
            return highest;
        }

        public static bool IsTriggerSatisfied(Trigger trigger)
        {
            if (trigger == null) return false;

            switch (trigger.Type.ToLowerInvariant())
            {
                case "none":
                    return true;
                case "globalkey":
                    return ZoneSystem.instance?.GetGlobalKey(trigger.Value) ?? false;
                case "hasitem":
                    Player player = Player.m_localPlayer;
                    if (player == null) return false;
                    return player.GetInventory().CountItems(trigger.Value) > 0;
                case "knownrecipe":
                    Player p = Player.m_localPlayer;
                    if (p == null) return false;
                    var recipes = _knownRecipesField?.GetValue(p) as HashSet<string>;
                    return recipes?.Contains(trigger.Value) ?? false;
                default:
                    _log?.LogWarning($"[ProgressionTracker] Unknown trigger type: {trigger.Type}");
                    return false;
            }
        }

        public static bool IsStageCompleted(Stage stage)
        {
            return IsTriggerSatisfied(stage.UnlockTrigger);
        }

        public static bool IsObjectiveComplete(Objective obj)
        {
            if (obj == null) return false;

            // Manual-tick objectives
            if (!obj.AutoComplete)
                return ProgressSaver.IsChecked("obj_" + obj.Id);

            // MAGIC LINK: if the player ticked the matching gear/recipe entry,
            // count the objective as done too.
            if (!string.IsNullOrEmpty(obj.Value) && ProgressSaver.IsChecked(obj.Value))
                return true;

            switch (obj.Type.ToLowerInvariant())
            {
                case "globalkey":
                case "boss":
                    return ZoneSystem.instance?.GetGlobalKey(obj.Value) ?? false;

                case "craftitem":
                case "knownrecipe":
                    Player p = Player.m_localPlayer;
                    if (p == null) return false;
                    var recipes = _knownRecipesField?.GetValue(p) as HashSet<string>;
                    return recipes?.Contains(obj.Value) ?? false;

                case "hasitem":
                    Player pl = Player.m_localPlayer;
                    return pl != null && pl.GetInventory().CountItems(obj.Value) > 0;

                default:
                    return ProgressSaver.IsChecked("obj_" + obj.Id);
            }
        }
    }
}