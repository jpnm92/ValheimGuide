using HarmonyLib;

namespace ValheimGuide.Patches
{
    /// <summary>
    /// ObjectDB.Awake fires after ALL mods have registered their items,
    /// including soft dependencies like Therzie.Armory and Therzie.Warfare.
    /// This is the correct place to run the enricher so modded prefabs
    /// are guaranteed to be present in ObjectDB.
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    internal static class ObjectDBAwakePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            Plugin.EnrichGuideData();
        }
    }
}
