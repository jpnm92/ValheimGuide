using HarmonyLib;
using ValheimGuide.Data;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class PlayerSpawnPatch
    {
        private static bool _dataLoaded = false; // ACTUAL GUARD VARIABLE

        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;

            // Generate and load guide data ONCE per session
            if (!_dataLoaded)
            {
                Plugin.LoadGuideData();
                _dataLoaded = true;
            }

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