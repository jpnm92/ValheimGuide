using HarmonyLib;
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
        private static void Postfix(Player __instance, Piece piece)
        {
            if (piece == null || __instance != Player.m_localPlayer) return;

            string pieceName = piece.gameObject.name.ToLowerInvariant();
            bool updated = false;

            foreach (var stage in GuideDataLoader.AllStages)
            {
                if (stage.Objectives == null) continue;

                foreach (var obj in stage.Objectives)
                {
                    // Match any build objective that has a Value and isn't already ticked
                    if (obj.Type != "build") continue;
                    if (string.IsNullOrEmpty(obj.Value)) continue;

                    string objKey = "obj_" + obj.Id;
                    if (ProgressSaver.IsChecked(objKey)) continue;

                    if (pieceName.Contains(obj.Value.ToLowerInvariant()))
                    {
                        ProgressSaver.SetChecked(objKey, true);
                        updated = true;
                        Plugin.Log.LogInfo(
                            $"[ValheimGuide] Build objective completed: {obj.Text}");
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