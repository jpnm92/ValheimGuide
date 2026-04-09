using HarmonyLib;
using Jotunn.Managers;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ValheimGuide.UI;
using TMPro;

namespace ValheimGuide.Patches
{
    [HarmonyPatch(typeof(Menu), nameof(Menu.Show))]
    public static class MenuPatch
    {
        private static Button _guideButton;

        private static void Postfix(Menu __instance)
        {
            if (_guideButton == null)
                CreateGuideButton(__instance);
            else
                _guideButton.gameObject.SetActive(true);
        }

        private static void CreateGuideButton(Menu menu)
        {
            Button[] allButtons = menu.GetComponentsInChildren<Button>(true);
            Button reference = allButtons.FirstOrDefault(b =>
                b.name == "Settings" || b.name == "Save" || b.name == "Logout");

            if (reference == null && allButtons.Length > 0)
                reference = allButtons[0];

            if (reference == null)
            {
                Debug.LogError("[ValheimGuide] No reference button found in menu.");
                return;
            }

            Transform container = reference.transform.parent;
            RectTransform refRect = reference.GetComponent<RectTransform>();

            GameObject newButtonObj = Object.Instantiate(reference.gameObject, container);
            newButtonObj.name = "GuideButton";

            // Target TMP, not legacy Text
            TextMeshProUGUI tmp = newButtonObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = "GUIDE";

            _guideButton = newButtonObj.GetComponent<Button>();
            _guideButton.onClick = new Button.ButtonClickedEvent();
            _guideButton.onClick.AddListener(() =>
            {
                Menu.instance?.Hide();
                GuidePanel.Show();
            });

            // Insert between Save and Settings
            Button settingsBtn = allButtons.FirstOrDefault(b => b.name == "Settings");
            if (settingsBtn != null)
                newButtonObj.transform.SetSiblingIndex(settingsBtn.transform.GetSiblingIndex());
            else
                newButtonObj.transform.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);

            Debug.Log("[ValheimGuide] Guide button created.");
        }
    }
}