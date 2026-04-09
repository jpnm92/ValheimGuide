using HarmonyLib;
using UnityEngine;
using ValheimGuide.Data;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
    public static class InventoryPatch
    {
        private static void Postfix(ItemDrop.ItemData item, bool __result)
        {
            if (__result && item != null)
            {
#if DEBUG
                Debug.Log($"[ValheimGuide] InventoryPatch: Item added - {item?.m_shared?.m_name}");
#endif
                ProgressionTracker.RefreshCurrentStage();
            }
        }
    }
}