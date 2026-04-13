using HarmonyLib;
using UnityEngine;
using ValheimGuide.Data;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
    public static class InventoryPatch
    {
        private static void Postfix(ItemDrop.ItemData item, int amount, int x, int y)
        {
            if (item == null || item.m_shared == null) return;

            bool updated = false;

            foreach (var stage in GuideDataLoader.AllStages)
            {
                if (stage.Objectives == null) continue;

                foreach (var obj in stage.Objectives)
                {
                    if (obj.Type.ToLowerInvariant() == "hasitem" && obj.AutoComplete && !string.IsNullOrEmpty(obj.Value))
                    {
                        string objKey = "obj_" + obj.Id;

                        if (ProgressSaver.IsChecked(objKey)) continue;

                        GameObject objPrefab = ObjectDB.instance.GetItemPrefab(obj.Value);
                        if (objPrefab != null)
                        {
                            ItemDrop objItemDrop = objPrefab.GetComponent<ItemDrop>();
                            if (objItemDrop != null && objItemDrop.m_itemData.m_shared.m_name == item.m_shared.m_name)
                            {
                                ProgressSaver.SetChecked(objKey, true);
                                updated = true;
                                Plugin.Log.LogInfo($"[ValheimGuide] Auto-completed gathering objective: {obj.Text}"); // FIXED HERE
                            }
                        }
                        else
                        {
                            if (item.m_shared.m_name.IndexOf(obj.Value, System.StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                ProgressSaver.SetChecked(objKey, true);
                                updated = true;
                            }
                        }
                    }
                }
            }

            if (updated)
            {
                ObjectiveTracker.ForceRefresh();
                ProgressionTracker.RefreshCurrentStage();
            }
        }
    }
}