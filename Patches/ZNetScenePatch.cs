using HarmonyLib;
using ValheimGuide.DataGenerators;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScenePatch
    {
        private static void Postfix()
        {
            GuideDataEnricher.EnrichMobResistances();
        }
    }
}