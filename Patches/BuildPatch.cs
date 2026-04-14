using HarmonyLib;
using System.Linq;
using ValheimGuide.Data;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
    /// <summary>
    /// Watches Player.PlacePiece and auto-ticks matching build objectives.
    /// Build objectives in .guide files have AutoComplete:false because there
    /// is no single reliable hook — this patch provides one.
    /// Matching is done by checking if the placed piece name contains obj.Value
    /// (case-insensitive). Set obj.Value to a unique substring of the piece's
    /// prefab name, e.g. "forge" for the Forge, "workbench" for the Workbench.
    /// Leave obj.Value null/empty to keep a purely manual objective.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    public static class BuildPatch
    {
        private static void Postfix(ItemDrop.ItemData item, int amount, int x, int y)
        {
            if (item == null || item.m_shared == null) return;
            if (Player.m_localPlayer == null || ZNet.instance == null) return;

            bool updated = false;
            Stage current = ProgressionTracker.CurrentStage;
            var stagesToCheck = current != null ? new[] { current } : GuideDataLoader.AllStages.ToArray();

            // Get the prefab name directly from the item being picked up (e.g., "Wood", "Flametal")
            string prefabName = item.m_dropPrefab ? item.m_dropPrefab.name : "";

            foreach (var stage in stagesToCheck)
            {
                if (stage.Objectives == null) continue;

                foreach (var obj in stage.Objectives)
                {
                    if (obj.Type.ToLowerInvariant() == "hasitem" && obj.AutoComplete && !string.IsNullOrEmpty(obj.Value))
                    {
                        string objKey = "obj_" + obj.Id;
                        if (ProgressSaver.IsChecked(objKey)) continue;

                        // OPTIMIZATION: Compare directly against the prefab name or localization key! No ObjectDB lookups!
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