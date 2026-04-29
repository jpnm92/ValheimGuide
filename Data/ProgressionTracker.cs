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

        // Reflection cache for Player.m_knownRecipes (HashSet<string>)
        public static readonly FieldInfo _knownRecipesField =
            typeof(Player).GetField("m_knownRecipes",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);


        public static Stage CurrentStage { get; private set; }
        public static Stage ManualOverrideStage { get; private set; }
        public static event Action<Stage> OnStageChanged;

        // ── Stage result cache ────────────────────────────────────────────────
        private static Stage _cachedHighestStage;
        private static bool _stageCacheDirty = true;

        /// <summary>
        /// Call this from any patch that could change which stages are unlocked
        /// (boss kill, item pickup, recipe learned). Causes the next refresh to
        /// recompute the highest completed stage instead of using the cached value.
        /// </summary>
        public static void MarkStageDirty() => _stageCacheDirty = true;

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
                ProgressSaver.Current.ManualStageOverride = stageId;
            _stageCacheDirty = true;
            ForceRefresh();
        }

        public static void ClearManualOverride()
        {
            ManualOverrideStage = null;
            if (ProgressSaver.Current != null)
                ProgressSaver.Current.ManualStageOverride = null;
            _stageCacheDirty = true;
            ForceRefresh();
        }

        private static Stage GetHighestCompletedStage()
        {
            if (!_stageCacheDirty) return _cachedHighestStage;

            Stage highest = null;
            foreach (Stage stage in GuideDataLoader.AllStages)
            {
                if (IsTriggerSatisfied(stage.UnlockTrigger))
                    highest = stage;
            }

            _cachedHighestStage = highest;
            _stageCacheDirty = false;
            return highest;
        }

        public static int CountItemsByPrefab(Inventory inv, string prefabName)
        {
            if (inv == null || string.IsNullOrEmpty(prefabName)) return 0;
            int count = 0;
            foreach (ItemDrop.ItemData item in inv.GetAllItems())
            {
                string name = item.m_dropPrefab != null
                    ? item.m_dropPrefab.name
                    : item.m_shared?.m_name?.Replace("$item_", "");
                if (string.Equals(name, prefabName, StringComparison.OrdinalIgnoreCase))
                    count += item.m_stack;
            }
            return count;
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
                    return CountItemsByPrefab(player.GetInventory(), trigger.Value) > 0;
                case "knownrecipe":
                    Player p = Player.m_localPlayer;
                    if (p == null) return false;
                    var recipes = _knownRecipesField?.GetValue(p) as HashSet<string>;
                    if (recipes == null)
                    {
                        Plugin.Log.LogWarning(
                            "[ProgressionTracker] m_knownRecipes reflection returned null — " +
                            "knownRecipe triggers will not function.");
                        return false;
                    }
                    return recipes.Contains(trigger.Value);
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

            // ── Manual / auto-set override — checked first for ALL types ──────────
            // BuildPatch and InventoryPatch write this key automatically when they
            // detect a matching action. The player can also set it from the UI.
            // Once set, this short-circuits all further checks so already-built
            // pieces (e.g. a Cauldron placed before the mod was installed) stay done.
            if (ProgressSaver.IsChecked("obj_" + obj.Id)) return true;

            // Not overridden and this is a manual-only objective — done only when ticked
            if (!obj.AutoComplete) return false;

            // ── MAGIC LINK: gear/recipe checkbox in the Guide panel ───────────────
            if (!string.IsNullOrEmpty(obj.Value) && ProgressSaver.IsChecked(obj.Value))
                return true;

            // ── AutoComplete game-state checks ────────────────────────────────────
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
                    if (pl == null) return false;
                    int required = obj.Count > 0 ? obj.Count : 1;
                    return CountItemsByPrefab(pl.GetInventory(), obj.Value) >= required;

                default:
                    return false;
            }
        }
    }
}