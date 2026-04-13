using HarmonyLib;
using ValheimGuide.Data;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class PlayerSpawnPatch
    {
        private static bool _generationDone = false; // Therzie gen only once per session

        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;

            // Always reload guide data on spawn so playstyles + stages are fresh
            // for every character (including character switches mid-session).
            // TherzieDataGenerator has its own _hasRun guard so generation only
            // happens once; GuideDataLoader.Load() is fast (JSON only).
            Plugin.LoadGuideData();

            if (Game.instance == null || Game.instance.GetPlayerProfile() == null)
                return;

            string playerName = __instance.GetPlayerName();
            long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
            string saveName = $"{playerName}_{playerID}";

            ProgressSaver.Load(saveName);

            // Force-refresh tracker now that progress is loaded
            ObjectiveTracker.ForceRefresh();
        }
    }
}