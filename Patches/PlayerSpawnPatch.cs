using HarmonyLib;
using ValheimGuide.Data;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class PlayerSpawnPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;

            if (Game.instance == null || Game.instance.GetPlayerProfile() == null)
                return;

            string playerName = __instance.GetPlayerName();
            long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
            string uniqueSaveName = $"{playerName}_{playerID}";

            ProgressSaver.Load(uniqueSaveName);
        }
    }
}