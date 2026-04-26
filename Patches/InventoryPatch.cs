using HarmonyLib;
using System;
using ValheimGuide.Data;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Inventory), "Changed")]
    public static class InventoryPatch
    {
        private static void Postfix(Inventory __instance)
        {
            if (Player.m_localPlayer == null || ZNet.instance == null) return;

            // Only care about the local player's inventory
            if (Player.m_localPlayer.GetInventory() != __instance) return;

            bool updated = false;
            var stagesToCheck = GuideDataLoader.GetStagesToScan();

            foreach (var stage in stagesToCheck)
            {
                if (stage.Objectives == null) continue;

                foreach (var obj in stage.Objectives)
                {
                    if (obj.Type.ToLowerInvariant() != "hasitem" || string.IsNullOrEmpty(obj.Value))
                        continue;

                    string objKey = "obj_" + obj.Id;
                    if (ProgressSaver.IsChecked(objKey)) continue;

                    int required = obj.Count > 0 ? obj.Count : 1;
                    int have = ProgressionTracker.CountItemsByPrefab(__instance, obj.Value);

                    if (have >= required)
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

            if (updated)
            {
                ProgressionTracker.MarkStageDirty();
                ObjectiveTracker.ForceRefresh();
                ProgressionTracker.RefreshCurrentStage();
            }
        }
    }
}