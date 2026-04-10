using HarmonyLib;
using UnityEngine;
using ValheimGuide.Data;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
    public static class InventoryPatch
    {
        private static void Postfix(ItemDrop.ItemData item, int amount, int x, int y)
        {
            if (item == null) return;
#if DEBUG
            Debug.Log($"[ValheimGuide] InventoryPatch: Item added - {item?.m_shared?.m_name}");
#endif
            ProgressionTracker.RefreshCurrentStage();
        }
    }
}