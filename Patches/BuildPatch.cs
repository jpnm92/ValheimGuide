using HarmonyLib;
using System.Linq;
using ValheimGuide.Data;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    public static class BuildPatch
    {
        private static void Postfix(Player __instance, Piece piece)
        {
            // Only track pieces placed by the local player
            if (piece == null || __instance != Player.m_localPlayer) return;

            bool updated = false;
            Stage current = ProgressionTracker.CurrentStage;
            var stagesToCheck = current != null ? new[] { current } : GuideDataLoader.AllStages.ToArray();

            // Clean up the piece name (removes "(Clone)" if Unity added it)
            string pieceName = piece.gameObject.name.Replace("(Clone)", "").Trim();

            foreach (var stage in stagesToCheck)
            {
                if (stage.Objectives == null) continue;

                foreach (var obj in stage.Objectives)
                {
                    if (obj.Type.ToLowerInvariant() == "build" && obj.AutoComplete && !string.IsNullOrEmpty(obj.Value))
                    {
                        string objKey = "obj_" + obj.Id;
                        if (ProgressSaver.IsChecked(objKey)) continue;

                        bool isMatch = string.Equals(pieceName, obj.Value, System.StringComparison.OrdinalIgnoreCase) ||
                                       (piece.m_name != null && piece.m_name.IndexOf(obj.Value, System.StringComparison.OrdinalIgnoreCase) >= 0);

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

            if (updated)
            {
                ObjectiveTracker.ForceRefresh();
                ProgressionTracker.RefreshCurrentStage();
            }
        }
    }
}