using HarmonyLib;
using UnityEngine;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Game), "UpdatePause")]
    public static class GamePausePatch
    {
        private static void Postfix()
        {
            if (!GuidePanel.IsVisible || !Plugin.PauseOnGuideOpen.Value) return;
            bool isMultiplayer = ZNet.instance != null && ZNet.instance.GetNrOfPlayers() > 1;
            if (!isMultiplayer)
                Time.timeScale = 0f;
        }
    }
}