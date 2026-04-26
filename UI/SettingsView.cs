using System;
using UnityEngine;
using UnityEngine.UI;
using ValheimGuide.Data;
using Jotunn.Managers;

namespace ValheimGuide.UI
{
    /// <summary>
    /// Settings tab content. Mirrors FirstLaunchOverlay.ShowSettingsScreen()
    /// but renders inside the persistent tab container instead of a modal overlay.
    /// Rebuilds every time Show() is called so active-state indicators are always fresh.
    /// </summary>
    internal class SettingsView
    {
        private readonly GameObject _container;
        private GameObject _scrollContent;

        public SettingsView(GameObject container)
        {
            _container = container;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show()
        {
            for (int i = _container.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(
                    _container.transform.GetChild(i).gameObject);

            BuildScrollArea();
            Populate();
        }


        // ── Layout ────────────────────────────────────────────────────────────

        private void BuildScrollArea()
        {
            GameObject scrollRoot = new GameObject("SettingsScroll",
                typeof(RectTransform), typeof(ScrollRect));
            scrollRoot.transform.SetParent(_container.transform, false);

            RectTransform srRect = scrollRoot.GetComponent<RectTransform>();
            srRect.anchorMin = Vector2.zero;
            srRect.anchorMax = Vector2.one;
            srRect.offsetMin = srRect.offsetMax = Vector2.zero;

            GameObject vp = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            vp.transform.SetParent(scrollRoot.transform, false);
            vp.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            vp.GetComponent<Mask>().showMaskGraphic = false;
            RectTransform vpRect = vp.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = vpRect.offsetMax = Vector2.zero;

            _scrollContent = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            _scrollContent.transform.SetParent(vp.transform, false);

            RectTransform cRect = _scrollContent.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.offsetMin = cRect.offsetMax = Vector2.zero;

            var vlg = _scrollContent.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(60, 60, 30, 30);
            vlg.spacing = 0;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            _scrollContent.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollRoot.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.viewport = vpRect;
            sr.content = cRect;

            Scrollbar sb = GuidePanel.CreateScrollbar(scrollRoot.transform);
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private void Populate()
        {
            var progress = ProgressSaver.Current;

            // ── SPOILERS ─────────────────────────────────────────────────────
            AddLabel("SPOILERS", 16, FontStyle.Bold, new Color(1f, 0.75f, 0.3f));
            AddSpacer(6);
            AddLabel("Control whether future biomes are visible in the stage list.",
                13, FontStyle.Italic, new Color(0.55f, 0.55f, 0.55f));
            AddSpacer(10);

            bool showAll = progress?.ShowFutureStages ?? true;

            AddSettingButton(
                "SHOW ALL BIOMES",
                "All stages visible regardless of progression.",
                isActive: showAll,
                onClick: () => { ProgressSaver.SetSpoilersPreference(true); Show(); });

            AddSpacer(6);

            AddSettingButton(
                "KEEP IT MYSTERIOUS",
                "Only biomes you have reached are shown.",
                isActive: !showAll,
                onClick: () => { ProgressSaver.SetSpoilersPreference(false); Show(); });

            AddSpacer(28);

            // ── PLAYSTYLE ────────────────────────────────────────────────────
            AddLabel("PLAYSTYLE", 16, FontStyle.Bold, new Color(1f, 0.75f, 0.3f));
            AddSpacer(6);
            AddLabel("ValheimGuide highlights armor and weapons that match your playstyle.",
                13, FontStyle.Italic, new Color(0.55f, 0.55f, 0.55f));
            AddSpacer(10);

            string currentPid = progress?.PlaystyleId ?? "all";

            foreach (var playstyle in GuideDataLoader.Playstyles)
            {
                string pid = playstyle.Id;
                string capturedPid = pid;
                AddSettingButton(
                    playstyle.Label.ToUpper(),
                    playstyle.Description,
                    isActive: currentPid == pid,
                    onClick: () => { ProgressSaver.SetPlaystylePreference(capturedPid); Show(); });
                AddSpacer(6);
            }

            AddSettingButton(
                "SHOW ALL / UNSURE",
                "Display all gear regardless of playstyle.",
                isActive: currentPid == "all",
                onClick: () => { ProgressSaver.SetPlaystylePreference("all"); Show(); });
        }

        // ── Widgets ───────────────────────────────────────────────────────────

        private void AddSettingButton(string label, string description,
    bool isActive, Action onClick)
        {
            // ContentSizeFitter on the row lets it grow when text wraps
            GameObject row = new GameObject("SettingBtn",
                typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement), typeof(ContentSizeFitter));
            row.transform.SetParent(_scrollContent.transform, false);
            row.GetComponent<LayoutElement>().minHeight = 48;
            row.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            row.GetComponent<Image>().color = isActive
                ? new Color(0.35f, 0.28f, 0.15f)
                : new Color(0.20f, 0.20f, 0.20f);

            // Horizontal: tick | stack(label + desc)
            // Must also size-fit vertically so the row inherits the stack's height
            GameObject inner = new GameObject("Inner",
                typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));
            inner.transform.SetParent(row.transform, false);
            RectTransform ir = inner.GetComponent<RectTransform>();
            ir.anchorMin = Vector2.zero;
            ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(14, 0);
            ir.offsetMax = new Vector2(-14, 0);
            inner.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var hlg = inner.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;  // let children dictate their own height
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            // Active tick — vertically centred via alignment on the Text
            GameObject tick = MakeText(inner.transform, "Tick",
                isActive ? "►" : " ", 14,
                new Color(1f, 0.85f, 0.4f));
            tick.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 0);
            Text tickText = tick.GetComponent<Text>();
            tickText.alignment = TextAnchor.UpperLeft;
            tickText.verticalOverflow = VerticalWrapMode.Overflow;

            // Label + description stack
            GameObject stack = new GameObject("Stack",
                typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(LayoutElement), typeof(ContentSizeFitter));
            stack.transform.SetParent(inner.transform, false);
            stack.GetComponent<LayoutElement>().flexibleWidth = 1f;
            stack.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var svlg = stack.GetComponent<VerticalLayoutGroup>();
            svlg.childForceExpandWidth = true;
            svlg.childForceExpandHeight = false;
            svlg.childControlWidth = true;
            svlg.childControlHeight = true;
            svlg.spacing = 3;
            svlg.padding = new RectOffset(0, 0, 10, 10);

            var lbl = MakeText(stack.transform, "Label", label, 14, Color.white);
            lbl.GetComponent<Text>().fontStyle = FontStyle.Bold;

            if (!string.IsNullOrEmpty(description))
                MakeText(stack.transform, "Desc", description, 12,
                    new Color(0.60f, 0.60f, 0.60f));

            row.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }


        private void AddLabel(string text, int size, FontStyle style, Color color)
        {
            GameObject go = new GameObject("Lbl",
                typeof(RectTransform), typeof(Text),
                typeof(LayoutElement), typeof(ContentSizeFitter));
            go.transform.SetParent(_scrollContent.transform, false);

            Text t = go.GetComponent<Text>();
            t.text = text;
            t.font = GUIManager.Instance.AveriaSerifBold;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            go.GetComponent<LayoutElement>().flexibleWidth = 1f;
            go.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
        }

        private void AddSpacer(float h)
        {
            GameObject go = new GameObject("Spc",
                typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(_scrollContent.transform, false);
            go.GetComponent<LayoutElement>().preferredHeight = h;
        }

        private static GameObject MakeText(Transform parent, string name,
            string txt, int size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>();
            t.text = txt;
            t.font = GUIManager.Instance.AveriaSerifBold;
            t.fontSize = size;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return go;
        }
    }
}