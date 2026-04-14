using HarmonyLib;
using ValheimGuide.Data;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class PlayerSpawnPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;

            // Always call LoadGuideData() on every spawn.
            // TherzieDataGenerator has its own _hasRun guard so generation only
            // happens once per session. GuideDataLoader.Load() is fast (JSON reads)
            // and must re-run so playstyles + stages reload for every character,
            // which fixes the playstyle prompt showing only "Show All" on char switch.
            Plugin.LoadGuideData();

            if (Game.instance == null || Game.instance.GetPlayerProfile() == null)
                return;

            string playerName = __instance.GetPlayerName();
            long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
            string saveName = $"{playerName}_{playerID}";

            ProgressSaver.Load(saveName);

            // Restore any manual stage override the player had set
            if (!string.IsNullOrEmpty(ProgressSaver.Current?.ManualStageOverride))
                ProgressionTracker.SetManualOverride(ProgressSaver.Current.ManualStageOverride);

            ObjectiveTracker.ForceRefresh();
        }
    }
}