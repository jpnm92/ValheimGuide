using HarmonyLib;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
    // This patch hides the objective tracker when the inventory is opened, and restores it when the inventory is closed (if the guide panel isn't open).
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    public static class InventoryGuiShowPatch
    {
        private static void Postfix()
        {
            ObjectiveTracker.SetVisible(false);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    public static class InventoryGuiHidePatch
    {
        private static void Postfix()
        {
            // Only restore if the guide panel itself isn't open
            if (!GuidePanel.IsVisible)
                ObjectiveTracker.SetVisible(true);
        }
    }
}