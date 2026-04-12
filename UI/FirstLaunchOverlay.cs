using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ValheimGuide.Data;

namespace ValheimGuide.UI
{
    public static class FirstLaunchOverlay
    {
        private static GameObject _overlay;

        public static bool IsNeeded()
        {
            var progress = ProgressSaver.Current;
            if (progress == null) return false;
            return progress.ShowFutureStages == null || progress.PlaystyleId == null;
        }

        public static void Show(GameObject parent, Action onComplete)
        {
            if (_overlay != null)
                UnityEngine.Object.Destroy(_overlay);

            _overlay = new GameObject("FirstLaunchOverlay",
                typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _overlay.transform.SetParent(parent.transform, false);

            // Full-panel dark overlay
            RectTransform rt = _overlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _overlay.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.97f);

            var progress = ProgressSaver.Current;

            if (progress.ShowFutureStages == null)
                ShowSpoilersScreen(onComplete);
            else
                ShowPlaystyleScreen(onComplete);
        }

        private static void ShowSpoilersScreen(Action onComplete)
        {
            ClearOverlayContent();

            AddLabel("WELCOME TO VALHEIMGUIDE", 26, TMPro.FontStyles.Bold, Color.white);
            AddSpacer(20);
            AddLabel("Would you like to see objectives and tips\nfor future biomes before you reach them?", 16, TMPro.FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f));
            AddSpacer(10);
            AddLabel("Most players are not new — future stages are shown by default.", 13, TMPro.FontStyles.Italic, new Color(0.6f, 0.6f, 0.6f));
            AddSpacer(30);

            AddButton("YES, SHOW EVERYTHING", new Color(0.25f, 0.45f, 0.25f), () =>
            {
                ProgressSaver.SetSpoilersPreference(true);
                var progress = ProgressSaver.Current;
                if (progress.PlaystyleId == null)
                    ShowPlaystyleScreen(onComplete);
                else
                    Dismiss(onComplete);
            });

            AddSpacer(8);

            AddButton("NO, KEEP IT MYSTERIOUS", new Color(0.35f, 0.25f, 0.15f), () =>
            {
                ProgressSaver.SetSpoilersPreference(false);
                var progress = ProgressSaver.Current;
                if (progress.PlaystyleId == null)
                    ShowPlaystyleScreen(onComplete);
                else
                    Dismiss(onComplete);
            });

            AddSpacer(16);
            AddLabel("You can change this later in the BepInEx config.", 12, TMPro.FontStyles.Italic, new Color(0.5f, 0.5f, 0.5f));
        }

        private static void ShowPlaystyleScreen(Action onComplete)
        {
            ClearOverlayContent();

            AddLabel("HOW DO YOU LIKE TO FIGHT?", 26, TMPro.FontStyles.Bold, Color.white);
            AddSpacer(10);
            AddLabel("ValheimGuide will highlight the matching armor set\nand weapons for your playstyle at each tier.", 15, TMPro.FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f));
            AddSpacer(20);

            var playstyles = GuideDataLoader.Playstyles;

            foreach (var playstyle in playstyles)
            {
                string pid = playstyle.Id;
                string label = $"{playstyle.Label.ToUpper()}  —  {playstyle.Description}";
                AddButton(label, new Color(0.22f, 0.22f, 0.28f), () =>
                {
                    ProgressSaver.SetPlaystylePreference(pid);
                    Dismiss(onComplete);
                });
                AddSpacer(4);
            }

            AddSpacer(8);
            AddButton("SHOW ALL / UNSURE", new Color(0.18f, 0.18f, 0.18f), () =>
            {
                ProgressSaver.SetPlaystylePreference("all");
                Dismiss(onComplete);
            });
        }

        private static void Dismiss(Action onComplete)
        {
            if (_overlay != null)
            {
                UnityEngine.Object.Destroy(_overlay);
                _overlay = null;
            }
            onComplete?.Invoke();
        }

        private static void ClearOverlayContent()
        {
            if (_overlay == null) return;
            foreach (Transform child in _overlay.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            // Re-add vertical layout
            if (_overlay.GetComponent<VerticalLayoutGroup>() == null)
            {
                var vlg = _overlay.AddComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childForceExpandWidth = false;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth = false;
                vlg.childControlHeight = true;
                vlg.spacing = 0;
                vlg.padding = new RectOffset(80, 80, 60, 60);
            }
        }

        private static void AddLabel(string text, int fontSize, TMPro.FontStyles style, Color color)
        {
            if (_overlay == null) return;

            GameObject go = new GameObject("Label",
                typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(_overlay.transform, false);

            Text t = go.GetComponent<Text>();
            t.text = text;
            t.font = Jotunn.Managers.GUIManager.Instance.AveriaSerifBold;
            t.fontSize = fontSize;
            t.fontStyle = style == TMPro.FontStyles.Bold ? FontStyle.Bold
                        : style == TMPro.FontStyles.Italic ? FontStyle.Italic
                        : FontStyle.Normal;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 700;
        }

        private static void AddSpacer(float height)
        {
            if (_overlay == null) return;

            GameObject go = new GameObject("Spacer",
                typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(_overlay.transform, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
        }

        private static void AddButton(string label, Color bgColor, Action onClick)
        {
            if (_overlay == null) return;

            GameObject btn = new GameObject("Btn",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btn.transform.SetParent(_overlay.transform, false);
            btn.GetComponent<Image>().color = bgColor;

            var le = btn.GetComponent<LayoutElement>();
            le.preferredWidth = 600;
            le.preferredHeight = 44;

            GameObject textGo = new GameObject("Text",
                typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(btn.transform, false);

            Text t = textGo.GetComponent<Text>();
            t.text = label;
            t.font = Jotunn.Managers.GUIManager.Instance.AveriaSerifBold;
            t.fontSize = 14;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 0);
            textRect.offsetMax = new Vector2(-12, 0);

            btn.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }
    }
}