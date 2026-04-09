using HarmonyLib;
using System; 
using ValheimGuide.Data;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(ZoneSystem), "SetGlobalKey", new Type[] { typeof(string) })]
    public static class GlobalKeyPatch
    {
        private static void Postfix(string name)
        {
            if (name.StartsWith("defeated_"))
            {
                ProgressionTracker.RefreshCurrentStage();
            }
        }
    }
}