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

            bool updated = false;
            var stagesToCheck = GuideDataLoader.GetStagesToScan();

            string pieceName = piece.gameObject.name.Replace("(Clone)", "").Trim();

            foreach (var stage in stagesToCheck)
            {
                if (stage.Objectives == null) continue;

                foreach (var obj in stage.Objectives)
                {
                    // REMOVED AutoComplete check! If it has a Value, we track it!
                    if (obj.Type.ToLowerInvariant() == "build" && !string.IsNullOrEmpty(obj.Value))
                    {
                        string objKey = "obj_" + obj.Id;
                        if (ProgressSaver.IsChecked(objKey)) continue;

                        bool isMatch = string.Equals(pieceName, obj.Value, System.StringComparison.OrdinalIgnoreCase);

                        if (isMatch)
                        {
                            ProgressSaver.SetChecked(objKey, true);
                            updated = true;
                            Plugin.Log.LogInfo($"[ValheimGuide] Auto-completed build objective: {obj.Text}");

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
                ProgressionTracker.MarkStageDirty();
                ObjectiveTracker.ForceRefresh();
                ProgressionTracker.RefreshCurrentStage();
            }
        }
    }
}