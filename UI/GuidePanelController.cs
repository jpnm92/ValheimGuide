using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ValheimGuide.Data;

namespace ValheimGuide.UI
{
    internal class GuidePanelController
    {
        private readonly GameObject _stageListContainer;
        private readonly GameObject _smartPanelContainer;
        private readonly GameObject _referenceAreaContainer;

        private Stage _selectedStage;
        private readonly List<(Stage stage, Button button)> _stageButtons = new List<(Stage, Button)>();
        private int _referenceTabIndex;
        private string _searchQuery = "";
        private GameObject _currentTabContentContainer;
        private bool _isReadMode = false;

        private readonly HashSet<string> _activeDamageFilters = new HashSet<string>();
        private readonly HashSet<string> _activeArmorFilters = new HashSet<string>();
        private readonly HashSet<string> _activeSourceFilters = new HashSet<string>();
        private readonly List<Image> _tabButtonImages = new List<Image>();

        public GuidePanelController(GameObject stageListContainer, GameObject smartPanelContainer, GameObject referenceAreaContainer)
        {
            _stageListContainer = stageListContainer;
            _smartPanelContainer = smartPanelContainer;
            _referenceAreaContainer = referenceAreaContainer;
        }

        private void RebuildFilterBar(Stage stage)
        {
            Transform existing = _referenceAreaContainer.transform.Find("FilterBar");
            if (existing != null)
                UnityEngine.Object.Destroy(existing.gameObject);

            BuildFilterBar(_referenceAreaContainer.transform, stage);
        }

        public void RefreshContent()
        {
            BuildStageList();

            Stage toSelect = ProgressionTracker.CurrentStage
                             ?? (GuideDataLoader.AllStages.Count > 0
                                 ? GuideDataLoader.AllStages[0]
                                 : null);

            if (toSelect != null)
                SelectStage(toSelect);
        }

        private void BuildStageList()
        {
            _stageButtons.Clear();
            foreach (Transform child in _stageListContainer.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(_stageListContainer.transform, false);

            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(4, 4);
            scrollRect.offsetMax = new Vector2(-18, -4);

            ScrollRect scroll = scrollObj.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            scrollObj.AddComponent<SmoothScroll>();

            Scrollbar scrollbar = GuidePanel.CreateScrollbar(_stageListContainer.transform);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            scroll.viewport = vpRect;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            scroll.content = contentRect;

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (Stage stage in GuideDataLoader.AllStages)
            {
                Stage captured = stage;

                GameObject btnObj = new GameObject("StageBtn_" + stage.Id,
                    typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                btnObj.transform.SetParent(content.transform, false);

                btnObj.GetComponent<LayoutElement>().preferredHeight = 36;
                btnObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

                GameObject labelObj = GuidePanel.CreateText(btnObj.transform, "Label", stage.Label.ToUpper());
                RectTransform labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8, 0);
                labelRect.offsetMax = new Vector2(-8, 0);
                Text labelText = labelObj.GetComponent<Text>();
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.fontSize = 16;

                if (ProgressionTracker.IsStageCompleted(stage))
                {
                    GameObject checkObj = GuidePanel.CreateText(btnObj.transform, "Check", "✔");
                    RectTransform checkRect = checkObj.GetComponent<RectTransform>();
                    checkRect.anchorMin = new Vector2(1, 0);
                    checkRect.anchorMax = new Vector2(1, 1);
                    checkRect.pivot = new Vector2(1, 0.5f);
                    checkRect.offsetMin = new Vector2(-28, 0);
                    checkRect.offsetMax = new Vector2(-4, 0);
                    checkObj.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
                    checkObj.GetComponent<Text>().color = new Color(0.4f, 0.9f, 0.4f);
                    checkObj.GetComponent<Text>().fontSize = 14;
                }

                Button btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() => SelectStage(captured));

                _stageButtons.Add((stage, btn));
            }
        }

        private void SelectStage(Stage stage)
        {
            _selectedStage = stage;
            _searchQuery = "";
            _activeDamageFilters.Clear();
            _activeArmorFilters.Clear();
            _activeSourceFilters.Clear();
            _referenceAreaContainer.SetActive(!_isReadMode);

            foreach (var stageButton in _stageButtons)
            {
                Stage s = stageButton.stage;
                Button btn = stageButton.button;
                bool isSelected = s == stage;
                bool isCurrent = s == ProgressionTracker.CurrentStage;

                Color bg = isSelected
                    ? new Color(0.35f, 0.28f, 0.15f, 1f)
                    : new Color(0.2f, 0.2f, 0.2f, 1f);
                btn.GetComponent<Image>().color = bg;

                if (isCurrent && !isSelected)
                {
                    GameObject dot = new GameObject("CurrentDot",
                        typeof(RectTransform), typeof(Image));
                    dot.transform.SetParent(btn.transform, false);
                    RectTransform dotRect = dot.GetComponent<RectTransform>();
                    dotRect.anchorMin = new Vector2(0, 0.5f);
                    dotRect.anchorMax = new Vector2(0, 0.5f);
                    dotRect.pivot = new Vector2(0, 0.5f);
                    dotRect.anchoredPosition = new Vector2(4, 0);
                    dotRect.sizeDelta = new Vector2(4, 4);
                    dot.GetComponent<Image>().color = new Color(0.4f, 0.9f, 0.4f);
                }
            }

            BuildSmartPanel(stage);
            BuildReferenceArea(stage);
        }

        private void BuildSmartPanel(Stage stage)
        {
            foreach (Transform child in _smartPanelContainer.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            // ── Mode toggle bar ──────────────────────────────────────
            GameObject modeBar = new GameObject("ModeBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            modeBar.transform.SetParent(_smartPanelContainer.transform, false);
            RectTransform modeBarRect = modeBar.GetComponent<RectTransform>();
            modeBarRect.anchorMin = new Vector2(0, 1);
            modeBarRect.anchorMax = new Vector2(1, 1);
            modeBarRect.pivot = new Vector2(0.5f, 1);
            modeBarRect.offsetMin = new Vector2(8, -34);
            modeBarRect.offsetMax = new Vector2(-8, -4);

            HorizontalLayoutGroup modeHlg = modeBar.GetComponent<HorizontalLayoutGroup>();
            modeHlg.spacing = 4;
            modeHlg.childForceExpandWidth = false;
            modeHlg.childForceExpandHeight = true;
            modeHlg.childControlWidth = false;
            modeHlg.childControlHeight = true;

            GameObject guideBtn = new GameObject("GuideModeBtn",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            guideBtn.transform.SetParent(modeBar.transform, false);
            guideBtn.GetComponent<LayoutElement>().preferredWidth = 80;
            guideBtn.GetComponent<Image>().color = !_isReadMode
                ? new Color(0.35f, 0.28f, 0.15f, 1f)
                : new Color(0.2f, 0.2f, 0.2f, 1f);

            GameObject guideBtnLabel = GuidePanel.CreateText(guideBtn.transform, "Label", "GUIDE");
            guideBtnLabel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            guideBtnLabel.GetComponent<RectTransform>().anchorMax = Vector2.one;
            guideBtnLabel.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            guideBtnLabel.GetComponent<Text>().fontSize = 13;

            GameObject readBtn = new GameObject("ReadModeBtn",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            readBtn.transform.SetParent(modeBar.transform, false);
            readBtn.GetComponent<LayoutElement>().preferredWidth = 80;
            readBtn.GetComponent<Image>().color = _isReadMode
                ? new Color(0.35f, 0.28f, 0.15f, 1f)
                : new Color(0.2f, 0.2f, 0.2f, 1f);

            GameObject readBtnLabel = GuidePanel.CreateText(readBtn.transform, "Label", "READ");
            readBtnLabel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            readBtnLabel.GetComponent<RectTransform>().anchorMax = Vector2.one;
            readBtnLabel.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            readBtnLabel.GetComponent<Text>().fontSize = 13;

            guideBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                _isReadMode = false;
                BuildSmartPanel(stage);
                _referenceAreaContainer.SetActive(true);
            });

            readBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                _isReadMode = true;
                BuildSmartPanel(stage);
                _referenceAreaContainer.SetActive(false);
            });

            RectTransform smartRect = _smartPanelContainer.GetComponent<RectTransform>();
            if (_isReadMode)
            {
                smartRect.anchorMin = new Vector2(0.25f, 0f);
                smartRect.anchorMax = new Vector2(1f, 1f);
                smartRect.offsetMin = new Vector2(5, 10);
                smartRect.offsetMax = new Vector2(-10, -70);
                _referenceAreaContainer.SetActive(false);
            }
            else
            {
                smartRect.anchorMin = new Vector2(0.25f, 0.5f);
                smartRect.anchorMax = new Vector2(1f, 1f);
                smartRect.offsetMin = new Vector2(5, 5);
                smartRect.offsetMax = new Vector2(-10, -70);
                _referenceAreaContainer.SetActive(true);
            }

            // ── Scroll view for content ───────────────────────────────
            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(_smartPanelContainer.transform, false);
            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(8, 8);
            scrollRect.offsetMax = new Vector2(-20, -40);

            ScrollRect scroll = scrollObj.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scrollObj.AddComponent<SmoothScroll>();

            Scrollbar scrollbar = GuidePanel.CreateScrollbar(_smartPanelContainer.transform);
            RectTransform sbRect = scrollbar.GetComponent<RectTransform>();
            sbRect.offsetMax = new Vector2(-4, -40);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            GameObject viewport = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = vpRect.offsetMax = Vector2.zero;
            scroll.viewport = vpRect;

            GameObject content = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;
            scroll.content = contentRect;

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (_isReadMode)
            {
                BuildReadContent(content.transform, stage);
                return;
            }

            // ── GUIDE MODE ────────────────────────────────────────────
            GuidePanel.AddLabel(content.transform, stage.Label.ToUpper(), 22,
                TMPro.FontStyles.Bold, Color.white);

            if (!string.IsNullOrEmpty(stage.BiomeDescription))
                GuidePanel.AddLabel(content.transform, stage.BiomeDescription, 14,
                    TMPro.FontStyles.Italic, new Color(0.8f, 0.8f, 0.8f));

            if (stage.PriorityMaterials != null && stage.PriorityMaterials.Count > 0)
            {
                GuidePanel.AddSpacer(content.transform);
                GuidePanel.AddLabel(content.transform, "PRIORITY MATERIALS", 15,
                    TMPro.FontStyles.Bold, new Color(1f, 0.75f, 0.3f));
                GuidePanel.AddLabel(content.transform, string.Join("  ·  ", stage.PriorityMaterials),
                    14, TMPro.FontStyles.Normal, Color.white);
            }

            if (stage.Boss != null)
            {
                GuidePanel.AddSpacer(content.transform);
                GuidePanel.AddLabel(content.transform, "BOSS", 15,
                    TMPro.FontStyles.Bold, new Color(1f, 0.75f, 0.3f));
                GuidePanel.AddLabel(content.transform, stage.Boss.Name, 18,
                    TMPro.FontStyles.Bold, new Color(1f, 0.4f, 0.4f));

                if (!string.IsNullOrEmpty(stage.Boss.Location))
                    GuidePanel.AddLabel(content.transform, "Location: " + stage.Boss.Location,
                        14, TMPro.FontStyles.Normal, Color.white);

                if (!string.IsNullOrEmpty(stage.Boss.RecommendedGear))
                    GuidePanel.AddLabel(content.transform, "Gear: " + stage.Boss.RecommendedGear,
                        14, TMPro.FontStyles.Normal, Color.white);

                if (stage.Boss.SummonMaterials != null && stage.Boss.SummonMaterials.Count > 0)
                {
                    string summon = "Summon: " + string.Join(", ",
                        stage.Boss.SummonMaterials.ConvertAll(m => $"{m.Amount}x {m.Label}"));
                    GuidePanel.AddLabel(content.transform, summon, 14, TMPro.FontStyles.Normal, Color.white);
                }

                if (stage.Boss.WeakAgainst != null && stage.Boss.WeakAgainst.Count > 0)
                    GuidePanel.AddLabel(content.transform, "Weak: " + string.Join(", ", stage.Boss.WeakAgainst),
                        14, TMPro.FontStyles.Normal, new Color(1f, 0.5f, 0.5f));

                if (!string.IsNullOrEmpty(stage.Boss.Strategy))
                    GuidePanel.AddLabel(content.transform, stage.Boss.Strategy, 13,
                        TMPro.FontStyles.Italic, new Color(0.8f, 0.95f, 0.8f));

                if (!string.IsNullOrEmpty(stage.Boss.GlobalUnlock))
                    GuidePanel.AddLabel(content.transform, "Unlocks: " + stage.Boss.GlobalUnlock,
                        14, TMPro.FontStyles.Normal, new Color(0.6f, 1f, 0.6f));
            }

            if (stage.Objectives != null && stage.Objectives.Count > 0)
            {
                string playstyleId = ProgressSaver.Current?.PlaystyleId ?? "all";

                var visibleObjs = stage.Objectives.Where(o =>
                    (string.IsNullOrEmpty(o.ModRequired) ||
                     GuideDataLoader.InstalledMods.Contains(o.ModRequired)) &&
                    (string.IsNullOrEmpty(o.PlaystyleFilter) ||
                     playstyleId == "all" ||
                     playstyleId == o.PlaystyleFilter)
                ).ToList();

                var buildObjs = visibleObjs.Where(o => o.Type == "build").ToList();
                var progressObjs = visibleObjs.Where(o => o.Type != "build").ToList();

                if (buildObjs.Count > 0)
                {
                    GuidePanel.AddSpacer(content.transform);
                    GuidePanel.AddLabel(content.transform, "BUILD", 15,
                        TMPro.FontStyles.Bold, new Color(0.5f, 0.8f, 1f));
                    foreach (var obj in buildObjs)
                        RenderObjectiveRow(content.transform, obj);
                }

                if (progressObjs.Count > 0)
                {
                    GuidePanel.AddSpacer(content.transform);
                    GuidePanel.AddLabel(content.transform, "OBJECTIVES", 15,
                        TMPro.FontStyles.Bold, new Color(1f, 0.75f, 0.3f));
                    foreach (var obj in progressObjs)
                        RenderObjectiveRow(content.transform, obj);
                }
            }

            if (stage.Tips != null && stage.Tips.Count > 0)
            {
                GuidePanel.AddSpacer(content.transform);
                GuidePanel.AddLabel(content.transform, "TIPS", 15,
                    TMPro.FontStyles.Bold, new Color(1f, 0.75f, 0.3f));

                foreach (Tip tip in stage.Tips)
                {
                    string prefix;
                    Color tipColor;
                    switch (tip.Category)
                    {
                        case "combat": prefix = "[Combat] "; tipColor = new Color(1f, 0.6f, 0.6f); break;
                        case "gathering": prefix = "[Gather] "; tipColor = new Color(0.6f, 1f, 0.6f); break;
                        case "secret": prefix = "[Secret] "; tipColor = new Color(1f, 0.9f, 0.4f); break;
                        case "building": prefix = "[Build] "; tipColor = new Color(0.6f, 0.8f, 1f); break;
                        default: prefix = ""; tipColor = new Color(0.85f, 0.85f, 0.85f); break;
                    }
                    GuidePanel.AddLabel(content.transform, prefix + tip.Text, 13,
                        TMPro.FontStyles.Normal, tipColor);
                }
            }
        }

        private void BuildReferenceArea(Stage stage)
        {
            foreach (Transform child in _referenceAreaContainer.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            GameObject tabBar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabBar.transform.SetParent(_referenceAreaContainer.transform, false);
            RectTransform tabBarRect = tabBar.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0, 1);
            tabBarRect.anchorMax = new Vector2(1, 1);
            tabBarRect.pivot = new Vector2(0.5f, 1);
            tabBarRect.offsetMin = new Vector2(8, -40);
            tabBarRect.offsetMax = new Vector2(-8, -4);

            HorizontalLayoutGroup hlg = tabBar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            GameObject searchBarObj = new GameObject("SearchBar", typeof(RectTransform), typeof(Image), typeof(InputField));
            searchBarObj.transform.SetParent(_referenceAreaContainer.transform, false);
            RectTransform searchRect = searchBarObj.GetComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(0, 1);
            searchRect.anchorMax = new Vector2(1, 1);
            searchRect.pivot = new Vector2(0.5f, 1);
            searchRect.offsetMin = new Vector2(8, -75);
            searchRect.offsetMax = new Vector2(-45, -45);
            searchBarObj.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 1f);

            GameObject placeholderObj = GuidePanel.CreateText(searchBarObj.transform, "Placeholder", "Search Items...");
            placeholderObj.GetComponent<Text>().color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderObj.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            RectTransform pRect = placeholderObj.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero; pRect.anchorMax = Vector2.one;
            pRect.offsetMin = new Vector2(10, 0); pRect.offsetMax = new Vector2(-10, 0);

            GameObject textObj = GuidePanel.CreateText(searchBarObj.transform, "Text", "");
            textObj.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(10, 0); tRect.offsetMax = new Vector2(-10, 0);

            InputField inputField = searchBarObj.GetComponent<InputField>();
            inputField.textComponent = textObj.GetComponent<Text>();
            inputField.placeholder = placeholderObj.GetComponent<Text>();
            inputField.text = _searchQuery;

            inputField.onValueChanged.AddListener((val) =>
            {
                _searchQuery = val.ToLower();
                PopulateActiveTab(stage);
            });

            GameObject clearBtnObj = GuidePanel.CreateButton(_referenceAreaContainer.transform, "ClearBtn", "✖");
            RectTransform clearBtnRect = clearBtnObj.GetComponent<RectTransform>();
            clearBtnRect.anchorMin = new Vector2(1, 1);
            clearBtnRect.anchorMax = new Vector2(1, 1);
            clearBtnRect.pivot = new Vector2(1, 1);
            clearBtnRect.offsetMin = new Vector2(-38, -75);
            clearBtnRect.offsetMax = new Vector2(-8, -45);
            clearBtnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                inputField.text = "";
                _searchQuery = "";
                PopulateActiveTab(stage);
            });

            _tabButtonImages.Clear();
            string[] tabNames = { "GEAR", "DROPS", "RECIPES" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int captured = i;
                GameObject tabBtn = new GameObject("Tab_" + tabNames[i], typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                tabBtn.transform.SetParent(tabBar.transform, false);
                tabBtn.GetComponent<LayoutElement>().preferredWidth = 90;

                Image tabImage = tabBtn.GetComponent<Image>();
                tabImage.color = i == _referenceTabIndex ? new Color(0.35f, 0.28f, 0.15f, 1f) : new Color(0.2f, 0.2f, 0.2f, 1f);
                _tabButtonImages.Add(tabImage);

                GameObject tabLabel = GuidePanel.CreateText(tabBtn.transform, "Label", tabNames[i]);
                RectTransform labelRect = tabLabel.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                tabLabel.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                tabLabel.GetComponent<Text>().fontSize = 14;

                tabBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _referenceTabIndex = captured;
                    _activeDamageFilters.Clear();
                    _activeArmorFilters.Clear();
                    _activeSourceFilters.Clear(); // Clears source filters on tab change

                    for (int j = 0; j < _tabButtonImages.Count; j++)
                        _tabButtonImages[j].color = j == _referenceTabIndex ? new Color(0.35f, 0.28f, 0.15f, 1f) : new Color(0.2f, 0.2f, 0.2f, 1f);

                    RebuildFilterBar(stage);
                    PopulateActiveTab(stage);
                });
            }

            // DYNAMIC SCROLL OFFSET CALCULATION
            int filterRows = 0;
            bool hasWeapons = stage.Gear.Exists(g => g.Type == "Weapon" || g.Type == "Bow");
            bool hasArmor = stage.Gear.Exists(g => g.Type == "Armor");
            bool hasMods = stage.Gear.Exists(g => g.ModRequired == "Therzie.Armory" || g.ModRequired == "Therzie.Warfare");

            if (_referenceTabIndex == 0)
            {
                if (hasMods) filterRows++;
                if (hasWeapons) filterRows++;
                if (hasArmor) filterRows++;
            }
            float dynamicTopOffset = -80f - (filterRows * 30f);

            BuildFilterBar(_referenceAreaContainer.transform, stage, dynamicTopOffset);

            GameObject contentArea = new GameObject("ContentArea", typeof(RectTransform), typeof(ScrollRect));
            contentArea.transform.SetParent(_referenceAreaContainer.transform, false);
            RectTransform contentAreaRect = contentArea.GetComponent<RectTransform>();
            contentAreaRect.anchorMin = Vector2.zero;
            contentAreaRect.anchorMax = new Vector2(1, 1);
            contentAreaRect.offsetMin = new Vector2(8, 8);
            contentAreaRect.offsetMax = new Vector2(-20, dynamicTopOffset);

            ScrollRect scroll = contentArea.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            contentArea.AddComponent<SmoothScroll>();

            Scrollbar scrollbar = GuidePanel.CreateScrollbar(_referenceAreaContainer.transform);
            RectTransform sbRect = scrollbar.GetComponent<RectTransform>();
            sbRect.offsetMax = new Vector2(-4, dynamicTopOffset);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(contentArea.transform, false);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = vpRect.offsetMax = Vector2.zero;
            scroll.viewport = vpRect;

            _currentTabContentContainer = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _currentTabContentContainer.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = _currentTabContentContainer.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;
            scroll.content = contentRect;

            VerticalLayoutGroup vlgContent = _currentTabContentContainer.GetComponent<VerticalLayoutGroup>();
            vlgContent.spacing = 6;
            vlgContent.padding = new RectOffset(4, 4, 4, 4);
            vlgContent.childForceExpandWidth = true;
            vlgContent.childForceExpandHeight = false;
            vlgContent.childControlWidth = true;
            vlgContent.childControlHeight = true;
            _currentTabContentContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            PopulateActiveTab(stage);
        }

        private void BuildFilterBar(Transform parent, Stage stage, float topOffset = -140f)
        {
            if (_referenceTabIndex != 0) return;

            bool hasWeapons = stage.Gear.Exists(g => g.Type == "Weapon" || g.Type == "Bow");
            bool hasArmor = stage.Gear.Exists(g => g.Type == "Armor");
            bool hasArmory = stage.Gear.Exists(g => g.ModRequired == "Therzie.Armory");
            bool hasWarfare = stage.Gear.Exists(g => g.ModRequired == "Therzie.Warfare");
            bool hasMods = hasArmory || hasWarfare;

            if (!hasWeapons && !hasArmor && !hasMods) return;

            GameObject filterBar = new GameObject("FilterBar", typeof(RectTransform), typeof(VerticalLayoutGroup));
            filterBar.transform.SetParent(parent, false);

            RectTransform filterRect = filterBar.GetComponent<RectTransform>();
            filterRect.anchorMin = new Vector2(0, 1);
            filterRect.anchorMax = new Vector2(1, 1);
            filterRect.pivot = new Vector2(0.5f, 1);
            filterRect.offsetMin = new Vector2(8, topOffset);
            filterRect.offsetMax = new Vector2(-8, -80);

            VerticalLayoutGroup vlg = filterBar.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            // 1. SOURCE FILTERS
            if (hasMods)
            {
                GameObject sRow = new GameObject("SourceFilters", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                sRow.transform.SetParent(filterBar.transform, false);
                sRow.GetComponent<LayoutElement>().preferredHeight = 24;

                HorizontalLayoutGroup hlg = sRow.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 4; hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true; hlg.childControlWidth = false; hlg.childControlHeight = true;

                string[] sources = { "Vanilla", "Armory", "Warfare" };
                foreach (string src in sources)
                {
                    if (src == "Armory" && !hasArmory) continue;
                    if (src == "Warfare" && !hasWarfare) continue;

                    string captured = src;
                    bool active = _activeSourceFilters.Contains(src);
                    CreateFilterPill(sRow.transform, src, active, () =>
                    {
                        if (_activeSourceFilters.Contains(captured)) _activeSourceFilters.Remove(captured);
                        else _activeSourceFilters.Add(captured);
                        BuildReferenceArea(_selectedStage);
                    });
                }
            }

            // 2. WEAPON FILTERS
            if (hasWeapons)
            {
                GameObject wRow = new GameObject("WeaponFilters", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                wRow.transform.SetParent(filterBar.transform, false);
                wRow.GetComponent<LayoutElement>().preferredHeight = 24;

                HorizontalLayoutGroup hlg = wRow.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 4; hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true; hlg.childControlWidth = false; hlg.childControlHeight = true;

                string[] damageTypes = { "Blunt", "Slash", "Pierce", "Fire", "Frost", "Lightning", "Poison", "Spirit" };
                foreach (string dt in damageTypes)
                {
                    string captured = dt;
                    bool active = _activeDamageFilters.Contains(dt);
                    CreateFilterPill(wRow.transform, dt, active, () =>
                    {
                        if (_activeDamageFilters.Contains(captured)) _activeDamageFilters.Remove(captured);
                        else _activeDamageFilters.Add(captured);
                        BuildReferenceArea(_selectedStage);
                    });
                }
            }

            // 3. ARMOR FILTERS
            if (hasArmor)
            {
                GameObject aRow = new GameObject("ArmorFilters", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                aRow.transform.SetParent(filterBar.transform, false);
                aRow.GetComponent<LayoutElement>().preferredHeight = 24;

                HorizontalLayoutGroup hlg = aRow.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 4; hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true; hlg.childControlWidth = false; hlg.childControlHeight = true;

                string[] armorClasses = { "Light", "Heavy" };
                foreach (string ac in armorClasses)
                {
                    string captured = ac;
                    bool active = _activeArmorFilters.Contains(ac);
                    CreateFilterPill(aRow.transform, ac, active, () =>
                    {
                        if (_activeArmorFilters.Contains(captured)) _activeArmorFilters.Remove(captured);
                        else _activeArmorFilters.Add(captured);
                        BuildReferenceArea(_selectedStage);
                    });
                }
            }
        }

        private GameObject CreateFilterPill(Transform parent, string label, bool active, System.Action onClick)
        {
            GameObject pill = new GameObject("Pill_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            pill.transform.SetParent(parent, false);
            pill.GetComponent<LayoutElement>().preferredWidth = 62;
            pill.GetComponent<Image>().color = active
                ? new Color(0.6f, 0.45f, 0.1f)
                : new Color(0.22f, 0.22f, 0.22f);

            GameObject textObj = GuidePanel.CreateText(pill.transform, "Label", label);
            RectTransform tr = textObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            Text t = textObj.GetComponent<Text>();
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = 12;

            pill.GetComponent<Button>().onClick.AddListener(() => onClick());
            return pill;
        }

        private void PopulateActiveTab(Stage stage)
        {
            if (_currentTabContentContainer == null) return;

            foreach (Transform child in _currentTabContentContainer.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            switch (_referenceTabIndex)
            {
                case 0: BuildGearTab(_currentTabContentContainer.transform, stage); break;
                case 1: BuildDropsTab(_currentTabContentContainer.transform, stage); break;
                case 2: BuildRecipesTab(_currentTabContentContainer.transform, stage); break;
            }
        }

        private void BuildGearTab(Transform parent, Stage stage)
        {
            if (stage.Gear == null || stage.Gear.Count == 0)
            {
                GuidePanel.AddLabel(parent, "No gear data.", 14, TMPro.FontStyles.Italic, new Color(0.6f, 0.6f, 0.6f));
                return;
            }

            int count = 0;
            foreach (GearEntry gear in stage.Gear)
            {
                if (!string.IsNullOrEmpty(_searchQuery) && !gear.Label.ToLower().Contains(_searchQuery))
                    continue;

                if (_activeDamageFilters.Count > 0 && (gear.Type == "Weapon" || gear.Type == "Bow"))
                {
                    bool match = gear.DamageTypes != null &&
                                 gear.DamageTypes.Exists(d => _activeDamageFilters.Contains(d));
                    if (!match) continue;
                }

                if (_activeArmorFilters.Count > 0 && gear.Type == "Armor")
                {
                    if (string.IsNullOrEmpty(gear.ArmorClass) ||
                        !_activeArmorFilters.Contains(gear.ArmorClass))
                        continue;
                }

                if (_activeSourceFilters.Count > 0)
                {
                    string source = string.IsNullOrEmpty(gear.ModRequired) ? "Vanilla" :
                                    gear.ModRequired == "Therzie.Armory" ? "Armory" : "Warfare";
                    if (!_activeSourceFilters.Contains(source)) continue;
                }

                count++;
                GameObject row = new GameObject("Row_" + gear.ItemId, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(parent, false);
                row.GetComponent<LayoutElement>().preferredHeight = 24;

                HorizontalLayoutGroup rowHlg = row.GetComponent<HorizontalLayoutGroup>();
                rowHlg.spacing = 6;
                rowHlg.childForceExpandWidth = false;
                rowHlg.childForceExpandHeight = true;
                rowHlg.childControlHeight = true;
                rowHlg.childControlWidth = false;

                GameObject checkBox = new GameObject("Checkbox", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                checkBox.transform.SetParent(row.transform, false);
                checkBox.GetComponent<LayoutElement>().preferredWidth = 18;
                string itemId = gear.ItemId;
                bool isChecked = ProgressSaver.IsChecked(itemId);
                Image checkImg = checkBox.GetComponent<Image>();
                checkImg.color = isChecked ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);

                GameObject checkMark = GuidePanel.CreateText(checkBox.transform, "Mark", isChecked ? "✔" : "");
                RectTransform markRect = checkMark.GetComponent<RectTransform>();
                markRect.anchorMin = Vector2.zero; markRect.anchorMax = Vector2.one;
                markRect.offsetMin = markRect.offsetMax = Vector2.zero;
                Text markText = checkMark.GetComponent<Text>();
                markText.alignment = TextAnchor.MiddleCenter;
                markText.fontSize = 12;
                markText.color = Color.white;

                GameObject nameObj = GuidePanel.CreateText(row.transform, "Name", gear.Label.ToUpper());
                nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 0);
                nameObj.GetComponent<Text>().fontSize = 15;
                nameObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
                nameObj.GetComponent<Text>().color = isChecked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;

                checkBox.GetComponent<Button>().onClick.AddListener(() =>
                {
                    bool nowChecked = !ProgressSaver.IsChecked(itemId);
                    ProgressSaver.SetChecked(itemId, nowChecked);
                    checkImg.color = nowChecked ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);
                    markText.text = nowChecked ? "✔" : "";
                    nameObj.GetComponent<Text>().color = nowChecked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;

                    // Immediately push changes to the HUD tracker and refresh objectives
                    ObjectiveTracker.ForceRefresh();
                    BuildSmartPanel(_selectedStage);
                });

                string type = gear.Type + "  ·  " + gear.Station;
                if (gear.StationLevel > 1) type += " (Lv " + gear.StationLevel + ")";
                GuidePanel.AddLabel(parent, type, 13, TMPro.FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));

                if (gear.Recipe != null && gear.Recipe.Count > 0)
                {
                    string recipe = "Recipe: " + string.Join(", ", gear.Recipe.ConvertAll(r => $"{r.Amount}x {r.Label}"));
                    GuidePanel.AddLabel(parent, recipe, 13, TMPro.FontStyles.Normal, Color.white);
                }

                GuidePanel.AddSpacer(parent);
            }

            if (count == 0)
                GuidePanel.AddLabel(parent, "No results found.", 14, TMPro.FontStyles.Italic, new Color(0.6f, 0.6f, 0.6f));
        }

        private void BuildDropsTab(Transform parent, Stage stage)
        {
            if (stage.Mobs == null || stage.Mobs.Count == 0)
            {
                GuidePanel.AddLabel(parent, "No mob data.", 14, TMPro.FontStyles.Italic, new Color(0.6f, 0.6f, 0.6f));
                return;
            }

            int count = 0;
            foreach (MobEntry mob in stage.Mobs)
            {
                if (!string.IsNullOrEmpty(_searchQuery) && !mob.Label.ToLower().Contains(_searchQuery))
                    continue;

                count++;

                string header = mob.Label.ToUpper();
                if (mob.Health > 0)
                    header += $"  ·  {mob.Health} HP";
                GuidePanel.AddLabel(parent, header, 15, TMPro.FontStyles.Bold, Color.white);

                if (mob.SpawnChanceDay > 0 || mob.SpawnChanceNight > 0)
                {
                    string spawn = $"Spawn: Day {(int)(mob.SpawnChanceDay * 100)}%  ·  Night {(int)(mob.SpawnChanceNight * 100)}%";
                    GuidePanel.AddLabel(parent, spawn, 13, TMPro.FontStyles.Normal, new Color(0.65f, 0.65f, 0.65f));
                }

                if (mob.Resistances != null && mob.Resistances.Count > 0)
                {
                    var weak = mob.Resistances.Where(r => r.Value == "Weak").Select(r => r.Key).ToList();
                    var immune = mob.Resistances.Where(r => r.Value == "Immune").Select(r => r.Key).ToList();
                    var resist = mob.Resistances.Where(r => r.Value == "Resistant").Select(r => r.Key).ToList();

                    if (weak.Count > 0)
                        GuidePanel.AddLabel(parent, "Weak: " + string.Join(", ", weak), 13, TMPro.FontStyles.Normal, new Color(1f, 0.5f, 0.5f));
                    if (immune.Count > 0)
                        GuidePanel.AddLabel(parent, "Immune: " + string.Join(", ", immune), 13, TMPro.FontStyles.Normal, new Color(0.5f, 0.8f, 1f));
                    if (resist.Count > 0)
                        GuidePanel.AddLabel(parent, "Resistant: " + string.Join(", ", resist), 13, TMPro.FontStyles.Normal, new Color(0.7f, 0.9f, 0.7f));
                }

                if (mob.Drops != null && mob.Drops.Count > 0)
                {
                    string drops = "Drops: " + string.Join(", ", mob.Drops.ConvertAll(d =>
                        $"{d.Label} ({(int)(d.Chance * 100)}%{(d.Max > 1 ? $" · {d.Min}-{d.Max}" : "")})"));
                    GuidePanel.AddLabel(parent, drops, 13, TMPro.FontStyles.Normal, Color.white);
                }

                if (mob.IsTameable && mob.Taming != null)
                {
                    string food = "Tame with: " + string.Join(", ", mob.Taming.FoodItems);
                    GuidePanel.AddLabel(parent, food, 13, TMPro.FontStyles.Italic, new Color(0.8f, 1f, 0.6f));
                    if (!string.IsNullOrEmpty(mob.Taming.Note))
                        GuidePanel.AddLabel(parent, mob.Taming.Note, 13, TMPro.FontStyles.Italic, new Color(0.7f, 0.9f, 0.5f));
                }

                if (!string.IsNullOrEmpty(mob.Note))
                    GuidePanel.AddLabel(parent, mob.Note, 13, TMPro.FontStyles.Italic, new Color(1f, 0.85f, 0.4f));

                GuidePanel.AddSpacer(parent);
            }

            if (count == 0)
                GuidePanel.AddLabel(parent, "No results found.", 14, TMPro.FontStyles.Italic, new Color(0.6f, 0.6f, 0.6f));
        }

        private void BuildRecipesTab(Transform parent, Stage stage)
        {
            if (stage.Recipes == null || stage.Recipes.Count == 0)
            {
                GuidePanel.AddLabel(parent, "No recipe data.", 14, TMPro.FontStyles.Italic, new Color(0.6f, 0.6f, 0.6f));
                return;
            }

            int count = 0;
            foreach (RecipeEntry recipe in stage.Recipes)
            {
                if (!string.IsNullOrEmpty(_searchQuery) && !recipe.Label.ToLower().Contains(_searchQuery))
                    continue;

                count++;
                GameObject row = new GameObject("Row_" + recipe.ItemId, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(parent, false);
                row.GetComponent<LayoutElement>().preferredHeight = 24;

                HorizontalLayoutGroup rowHlg = row.GetComponent<HorizontalLayoutGroup>();
                rowHlg.spacing = 6;
                rowHlg.childForceExpandWidth = false;
                rowHlg.childForceExpandHeight = true;
                rowHlg.childControlHeight = true;
                rowHlg.childControlWidth = false;

                string itemId = recipe.ItemId;
                bool isChecked = ProgressSaver.IsChecked(itemId);

                GameObject checkBox = new GameObject("Checkbox", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                checkBox.transform.SetParent(row.transform, false);
                checkBox.GetComponent<LayoutElement>().preferredWidth = 18;
                Image checkImg = checkBox.GetComponent<Image>();
                checkImg.color = isChecked ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);

                GameObject checkMark = GuidePanel.CreateText(checkBox.transform, "Mark", isChecked ? "✔" : "");
                RectTransform markRect = checkMark.GetComponent<RectTransform>();
                markRect.anchorMin = Vector2.zero; markRect.anchorMax = Vector2.one;
                markRect.offsetMin = markRect.offsetMax = Vector2.zero;
                Text markText = checkMark.GetComponent<Text>();
                markText.alignment = TextAnchor.MiddleCenter;
                markText.fontSize = 12; markText.color = Color.white;

                GameObject nameObj = GuidePanel.CreateText(row.transform, "Name", recipe.Label.ToUpper());
                nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 0);
                nameObj.GetComponent<Text>().fontSize = 15;
                nameObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
                nameObj.GetComponent<Text>().color = isChecked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;

                checkBox.GetComponent<Button>().onClick.AddListener(() =>
                {
                    bool nowChecked = !ProgressSaver.IsChecked(itemId);
                    ProgressSaver.SetChecked(itemId, nowChecked);
                    checkImg.color = nowChecked ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);
                    markText.text = nowChecked ? "✔" : "";
                    nameObj.GetComponent<Text>().color = nowChecked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;

                    ObjectiveTracker.ForceRefresh();
                    BuildSmartPanel(_selectedStage);
                });

                string station = recipe.Station;
                if (recipe.StationLevel > 1) station += " (Lv " + recipe.StationLevel + ")";
                GuidePanel.AddLabel(parent, station, 13, TMPro.FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));

                if (!string.IsNullOrEmpty(recipe.UnlockNote))
                    GuidePanel.AddLabel(parent, recipe.UnlockNote, 13, TMPro.FontStyles.Italic, new Color(0.6f, 1f, 0.6f));

                if (recipe.Ingredients != null && recipe.Ingredients.Count > 0)
                {
                    string ingredients = "Ingredients: " + string.Join(", ", recipe.Ingredients.ConvertAll(i => $"{i.Amount}x {i.Label}"));
                    GuidePanel.AddLabel(parent, ingredients, 13, TMPro.FontStyles.Normal, Color.white);
                }

                GuidePanel.AddSpacer(parent);
            }
            if (count == 0)
                GuidePanel.AddLabel(parent, "No results found.", 14, TMPro.FontStyles.Italic, new Color(0.6f, 0.6f, 0.6f));
        }

        private bool IsObjectiveComplete(Objective obj)
        {
            if (!obj.AutoComplete)
                return ProgressSaver.IsChecked("obj_" + obj.Id);

            // MAGIC LINK: If the user manually ticked the associated gear/recipe item, count the objective as completed!
            if (!string.IsNullOrEmpty(obj.Value) && ProgressSaver.IsChecked(obj.Value))
                return true;

            switch (obj.Type.ToLowerInvariant())
            {
                case "globalkey":
                case "boss":
                    return ZoneSystem.instance?.GetGlobalKey(obj.Value) ?? false;
                case "craftitem":
                case "knownrecipe":
                    Player p = Player.m_localPlayer;
                    if (p == null) return false;
                    var recipes = typeof(Player)
                        .GetField("m_knownRecipes",
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance)
                        ?.GetValue(p) as System.Collections.Generic.HashSet<string>;
                    return recipes?.Contains(obj.Value) ?? false;
                case "hasitem":
                    Player player = Player.m_localPlayer;
                    if (player == null) return false;
                    return player.GetInventory().CountItems(obj.Value) > 0;
                default:
                    return ProgressSaver.IsChecked("obj_" + obj.Id);
            }
        }

        private void BuildReadContent(Transform parent, Stage stage)
        {
            if (string.IsNullOrEmpty(stage.Article))
            {
                string parentBiome = stage.Label;
                int start = stage.Label.IndexOf('(');
                int end = stage.Label.IndexOf(')');
                if (start >= 0 && end > start)
                    parentBiome = stage.Label.Substring(start + 1, end - start - 1);

                GuidePanel.AddLabel(parent,
                    $"This tier's guide is covered in the {parentBiome} Read section.",
                    14, TMPro.FontStyles.Italic, new Color(0.7f, 0.7f, 0.7f));
                GuidePanel.AddSpacer(parent);
                GuidePanel.AddLabel(parent,
                    "Select the base biome stage on the left and click READ.",
                    13, TMPro.FontStyles.Normal, new Color(0.6f, 0.6f, 0.6f));
                return;
            }

            string[] paragraphs = stage.Article.Split(
                new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string paragraph in paragraphs)
            {
                string trimmed = paragraph.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                bool isHeader = trimmed.StartsWith("<size=");
                int fontSize = isHeader ? 15 : 13;

                GuidePanel.AddLabel(parent, trimmed, fontSize,
                    TMPro.FontStyles.Normal, Color.white);
                GuidePanel.AddSpacer(parent);
            }
        }

        private void RenderObjectiveRow(Transform parent, Objective obj)
        {
            bool done = IsObjectiveComplete(obj);
            Color textColor = done ? new Color(0.5f, 0.8f, 0.5f) : Color.white;

            GameObject row = new GameObject("ObjRow_" + obj.Id,
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().minHeight = 22;

            var rowHlg = row.GetComponent<HorizontalLayoutGroup>();
            rowHlg.spacing = 6;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = true;
            rowHlg.childControlWidth = false;
            rowHlg.childControlHeight = true;

            if (!obj.AutoComplete)
            {
                string objKey = "obj_" + obj.Id;
                bool isChecked = ProgressSaver.IsChecked(objKey);

                GameObject box = new GameObject("Check",
                    typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                box.transform.SetParent(row.transform, false);
                box.GetComponent<LayoutElement>().preferredWidth = 18;
                Image checkImg = box.GetComponent<Image>();
                checkImg.color = isChecked ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);

                GameObject mark = GuidePanel.CreateText(box.transform, "Mark", isChecked ? "✔" : "");
                var markRect = mark.GetComponent<RectTransform>();
                markRect.anchorMin = Vector2.zero;
                markRect.anchorMax = Vector2.one;
                markRect.offsetMin = markRect.offsetMax = Vector2.zero;
                Text markText = mark.GetComponent<Text>();
                markText.alignment = TextAnchor.MiddleCenter;
                markText.fontSize = 11;

                GameObject labelGo = GuidePanel.CreateText(row.transform, "Text", obj.Text);
                labelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 0);
                Text labelText = labelGo.GetComponent<Text>();
                labelText.fontSize = 13;
                labelText.color = isChecked ? new Color(0.5f, 0.8f, 0.5f) : Color.white;

                box.GetComponent<Button>().onClick.AddListener(() =>
                {
                    bool now = !ProgressSaver.IsChecked(objKey);
                    ProgressSaver.SetChecked(objKey, now);
                    checkImg.color = now ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);
                    markText.text = now ? "✔" : "";

                    // Now correctly visually updates text
                    labelText.color = now ? new Color(0.5f, 0.8f, 0.5f) : Color.white;

                    ObjectiveTracker.ForceRefresh();
                });
            }
            else
            {
                GameObject tickGo = GuidePanel.CreateText(row.transform, "Tick", done ? "✔" : "○");
                tickGo.GetComponent<RectTransform>().sizeDelta = new Vector2(18, 0);
                Text tickText = tickGo.GetComponent<Text>();
                tickText.alignment = TextAnchor.MiddleCenter;
                tickText.fontSize = 13;
                tickText.color = done ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);

                GameObject labelGo = GuidePanel.CreateText(row.transform, "Text", obj.Text);
                labelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 0);
                Text labelText = labelGo.GetComponent<Text>();
                labelText.fontSize = 13;
                labelText.color = textColor;
            }
        }
    }
}