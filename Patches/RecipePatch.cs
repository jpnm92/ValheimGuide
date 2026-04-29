using HarmonyLib;
using ValheimGuide.Data;
using ValheimGuide.UI;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownRecipe))]
    public static class RecipePatch
    {
        private static void Postfix(Player __instance, Recipe recipe)
        {
            if (recipe == null || __instance != Player.m_localPlayer) return;
            if (recipe.m_item == null) return;

            string itemName = recipe.m_item.name;
            bool updated = false;

            foreach (var stage in GuideDataLoader.GetStagesToScan())
            {
                if (stage.Objectives == null) continue;

                foreach (var obj in stage.Objectives)
                {
                    string t = obj.Type?.ToLowerInvariant() ?? "";
                    if (t != "craftitem" && t != "knownrecipe") continue;
                    if (string.IsNullOrEmpty(obj.Value)) continue;

                    string objKey = "obj_" + obj.Id;
                    if (ProgressSaver.IsChecked(objKey)) continue;

                    if (string.Equals(obj.Value, itemName,
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        ProgressSaver.SetChecked(objKey, true);
                        updated = true;
                        Plugin.Log.LogInfo(
                            $"[ValheimGuide] Auto-completed recipe objective: {obj.Text}");

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