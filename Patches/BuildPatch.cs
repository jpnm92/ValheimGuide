using HarmonyLib;
using ValheimGuide.Data;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
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
                    if (obj.Type == "build" && obj.AutoComplete && !string.IsNullOrEmpty(obj.Value))
                    {
                        if (pieceName.Contains(obj.Value.ToLowerInvariant()))
                        {
                            string objKey = "obj_" + obj.Id;
                            if (!ProgressSaver.IsChecked(objKey))
                            {
                                ProgressSaver.SetChecked(objKey, true);
                                updated = true;
                                Plugin.Log.LogInfo($"[ValheimGuide] Auto-completed build objective: {obj.Text}"); // FIXED HERE
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