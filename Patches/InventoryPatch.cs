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
            if (Player.m_localPlayer == null || ZNet.instance == null) return;

            bool updated = false;
            var stagesToCheck = GuideDataLoader.GetStagesToScan();

            string prefabName = item.m_dropPrefab ? item.m_dropPrefab.name : "";

            foreach (var stage in stagesToCheck)
            {
                if (stage.Objectives == null) continue;

                foreach (var obj in stage.Objectives)
                {
                    // REMOVED AutoComplete check!
                    if (obj.Type.ToLowerInvariant() == "hasitem" && !string.IsNullOrEmpty(obj.Value))
                    {
                        string objKey = "obj_" + obj.Id;
                        if (ProgressSaver.IsChecked(objKey)) continue;

                        bool isMatch = string.Equals(prefabName, obj.Value, System.StringComparison.OrdinalIgnoreCase) ||
                                       item.m_shared.m_name.IndexOf(obj.Value, System.StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isMatch)
                        {
                            ProgressSaver.SetChecked(objKey, true);
                            updated = true;
                            Plugin.Log.LogInfo($"[ValheimGuide] Auto-completed gathering objective: {obj.Text}");

                            Player.m_localPlayer?.Message(
                                MessageHud.MessageType.TopLeft,
                                $"<color=#80FF80>Objective Complete</color>\n{obj.Text}"
                            );
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