using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ValheimGuide.Data;
using Jotunn.Managers;

namespace ValheimGuide.UI
{
    public static class GuidePanel
    {
        private static GameObject _panel;
        private static bool _isVisible;
        public static bool IsVisible => _isVisible;

        private static GameObject _stageListContainer;
        private static GameObject _smartPanelContainer;
        private static GameObject _referenceAreaContainer;

        private static Stage _selectedStage;
        private static readonly List<(Stage stage, Button button)> _stageButtons = new List<(Stage, Button)>();
        private static int _referenceTabIndex = 0;
        private static string _searchQuery = "";

        public static void Show()
        {
            if (_panel == null)
                CreatePanel();

            _panel.SetActive(true);
            _isVisible = true;
            GUIManager.BlockInput(true);
            RefreshContent();
        }

        public static void Hide()
        {
            if (_panel != null)
                _panel.SetActive(false);
            _isVisible = false;
            GUIManager.BlockInput(false);
        }

        public static void Toggle()
        {
            if (_isVisible) Hide();
            else Show();
        }

        private static void CreatePanel()
        {
            GameObject canvasObj = new GameObject("ValheimGuideCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20000;
            UnityEngine.Object.DontDestroyOnLoad(canvasObj);

            _panel = new GameObject("GuidePanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvasObj.transform, false);
            _panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(50, 50);
            panelRect.offsetMax = new Vector2(-50, -50);

            GameObject closeBtn = CreateButton(_panel.transform, "CloseButton", "✖");
            RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-10, -10);
            closeRect.sizeDelta = new Vector2(40, 40);
            closeBtn.GetComponent<Button>().onClick.AddListener(Hide);

            GameObject title = CreateText(_panel.transform, "Title", "VALHEIM GUIDE");
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(0, 1);
            titleRect.pivot = new Vector2(0, 1);
            titleRect.anchoredPosition = new Vector2(20, -20);
            titleRect.sizeDelta = new Vector2(400, 40);
            title.GetComponent<Text>().fontSize = 28;
            title.GetComponent<Text>().fontStyle = FontStyle.Bold;

            _stageListContainer = CreatePanelSection(_panel.transform, "StageList",
                new Vector2(0, 0), new Vector2(0.25f, 1),
                new Vector2(10, 10), new Vector2(-5, -70));
            _stageListContainer.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            _smartPanelContainer = CreatePanelSection(_panel.transform, "SmartPanel",
                new Vector2(0.25f, 0.5f), new Vector2(1, 1),
                new Vector2(5, 5), new Vector2(-10, -70));
            _smartPanelContainer.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            _referenceAreaContainer = CreatePanelSection(_panel.transform, "ReferenceArea",
                new Vector2(0.25f, 0), new Vector2(1, 0.5f),
                new Vector2(5, 10), new Vector2(-10, -5));
            _referenceAreaContainer.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            _panel.SetActive(false);
        }

        private static void RefreshContent()
        {
            BuildStageList();

            Stage toSelect = ProgressionTracker.CurrentStage
                             ?? (GuideDataLoader.AllStages.Count > 0
                                 ? GuideDataLoader.AllStages[0]
                                 : null);

            if (toSelect != null)
                SelectStage(toSelect);
        }

        private static void BuildStageList()
        {
            _stageButtons.Clear();
            foreach (Transform child in _stageListContainer.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(_stageListContainer.transform, false);

            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero; scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(4, 4); scrollRect.offsetMax = new Vector2(-18, -4);

            ScrollRect scroll = scrollObj.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            scrollObj.AddComponent<SmoothScroll>();

            Scrollbar scrollbar = CreateScrollbar(_stageListContainer.transform);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;
            scroll.viewport = vpRect;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero; contentRect.offsetMax = Vector2.zero;
            scroll.content = contentRect;

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4; vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childControlHeight = true;

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

                GameObject labelObj = CreateText(btnObj.transform, "Label", stage.Label.ToUpper());
                RectTransform labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8, 0); labelRect.offsetMax = new Vector2(-8, 0);
                Text labelText = labelObj.GetComponent<Text>();
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.fontSize = 16;

                if (ProgressionTracker.IsStageCompleted(stage))
                {
                    GameObject checkObj = CreateText(btnObj.transform, "Check", "✔");
                    RectTransform checkRect = checkObj.GetComponent<RectTransform>();
                    checkRect.anchorMin = new Vector2(1, 0); checkRect.anchorMax = new Vector2(1, 1);
                    checkRect.pivot = new Vector2(1, 0.5f);
                    checkRect.offsetMin = new Vector2(-28, 0); checkRect.offsetMax = new Vector2(-4, 0);
                    checkObj.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
                    checkObj.GetComponent<Text>().color = new Color(0.4f, 0.9f, 0.4f);
                    checkObj.GetComponent<Text>().fontSize = 14;
                }

                Button btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() => SelectStage(captured));

                _stageButtons.Add((stage, btn));
            }
        }

        private static void SelectStage(Stage stage)
        {
            _selectedStage = stage;
            _searchQuery = "";

            foreach (var (s, btn) in _stageButtons)
            {
                bool isSelected = s == stage;
                bool isCurrent = s == ProgressionTracker.CurrentStage;

                Color bg = isSelected ? new Color(0.35f, 0.28f, 0.15f, 1f)
                         : isCurrent ? new Color(0.25f, 0.22f, 0.12f, 1f)
                         : new Color(0.2f, 0.2f, 0.2f, 1f);

                btn.GetComponent<Image>().color = bg;
            }

            BuildSmartPanel(stage);
            BuildReferenceArea(stage);
        }

        private static void BuildSmartPanel(Stage stage)
        {
            foreach (Transform child in _smartPanelContainer.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(_smartPanelContainer.transform, false);
            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero; scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(8, 8); scrollRect.offsetMax = new Vector2(-20, -8);

            ScrollRect scroll = scrollObj.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            scrollObj.AddComponent<SmoothScroll>();

            Scrollbar scrollbar = CreateScrollbar(_smartPanelContainer.transform);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;
            scroll.viewport = vpRect;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero; contentRect.offsetMax = Vector2.zero;
            scroll.content = contentRect;

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8; vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childControlHeight = true;

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddLabel(content.transform, stage.Label.ToUpper(), 22, FontStyle.Bold, Color.white);

            if (!string.IsNullOrEmpty(stage.BiomeDescription))
                AddLabel(content.transform, stage.BiomeDescription, 14, FontStyle.Italic, new Color(0.8f, 0.8f, 0.8f));

            if (stage.PriorityMaterials != null && stage.PriorityMaterials.Count > 0)
            {
                AddSpacer(content.transform);
                AddLabel(content.transform, "PRIORITY MATERIALS", 15, FontStyle.Bold, new Color(1f, 0.75f, 0.3f));
                AddLabel(content.transform, string.Join("  ·  ", stage.PriorityMaterials), 14, FontStyle.Normal, Color.white);
            }

            if (stage.Boss != null)
            {
                AddSpacer(content.transform);
                AddLabel(content.transform, "BOSS", 15, FontStyle.Bold, new Color(1f, 0.75f, 0.3f));
                AddLabel(content.transform, stage.Boss.Name, 18, FontStyle.Bold, new Color(1f, 0.4f, 0.4f));

                if (!string.IsNullOrEmpty(stage.Boss.Location))
                    AddLabel(content.transform, "Location: " + stage.Boss.Location, 14, FontStyle.Normal, Color.white);

                if (!string.IsNullOrEmpty(stage.Boss.RecommendedGear))
                    AddLabel(content.transform, "Gear: " + stage.Boss.RecommendedGear, 14, FontStyle.Normal, Color.white);

                if (stage.Boss.SummonMaterials != null && stage.Boss.SummonMaterials.Count > 0)
                {
                    string summon = "Summon: " + string.Join(", ", stage.Boss.SummonMaterials.ConvertAll(m => $"{m.Amount}x {m.Label}"));
                    AddLabel(content.transform, summon, 14, FontStyle.Normal, Color.white);
                }

                if (!string.IsNullOrEmpty(stage.Boss.GlobalUnlock))
                    AddLabel(content.transform, "Unlocks: " + stage.Boss.GlobalUnlock, 14, FontStyle.Normal, new Color(0.6f, 1f, 0.6f));
            }
        }

        private static void AddLabel(Transform parent, string content, int fontSize, FontStyle style, Color color)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Text t = go.GetComponent<Text>();
            t.text = content;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize; t.fontStyle = style; t.color = color;
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow; t.resizeTextForBestFit = false;

            go.GetComponent<LayoutElement>().flexibleWidth = 1;
        }

        private static void AddSpacer(Transform parent)
        {
            GameObject go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 6;
        }

        private static GameObject CreatePanelSection(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);

            GameObject textGo = CreateText(go.transform, "Text", label);
            Text text = textGo.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.fontSize = 20;

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string content)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 18);
            text.color = Color.white; text.fontSize = 18; text.raycastTarget = false;

            return go;
        }

        private static Scrollbar CreateScrollbar(Transform parent)
        {
            GameObject scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObj.transform.SetParent(parent, false);

            RectTransform scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0); scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-14, 4);
            scrollbarRect.offsetMax = new Vector2(-4, -4);
            scrollbarObj.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.8f);

            GameObject slidingAreaObj = new GameObject("Sliding Area", typeof(RectTransform));
            slidingAreaObj.transform.SetParent(scrollbarObj.transform, false);
            RectTransform slidingAreaRect = slidingAreaObj.GetComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero; slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.offsetMin = new Vector2(2, 2); slidingAreaRect.offsetMax = new Vector2(-2, -2);

            GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObj.transform.SetParent(slidingAreaObj.transform, false);
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();

            handleRect.anchorMin = Vector2.zero; handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero; handleRect.offsetMax = Vector2.zero;
            handleRect.sizeDelta = Vector2.zero;

            handleObj.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);

            Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;

            return scrollbar;
        }

        private static GameObject _currentTabContentContainer;

        private static void BuildReferenceArea(Stage stage)
        {
            foreach (Transform child in _referenceAreaContainer.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            GameObject tabBar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabBar.transform.SetParent(_referenceAreaContainer.transform, false);
            RectTransform tabBarRect = tabBar.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0, 1); tabBarRect.anchorMax = new Vector2(1, 1);
            tabBarRect.pivot = new Vector2(0.5f, 1);
            tabBarRect.offsetMin = new Vector2(8, -40); tabBarRect.offsetMax = new Vector2(-8, -4);

            HorizontalLayoutGroup hlg = tabBar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4; hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true; hlg.childControlWidth = false; hlg.childControlHeight = true;

            GameObject searchBarObj = new GameObject("SearchBar", typeof(RectTransform), typeof(Image), typeof(InputField));
            searchBarObj.transform.SetParent(_referenceAreaContainer.transform, false);
            RectTransform searchRect = searchBarObj.GetComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(0, 1); searchRect.anchorMax = new Vector2(1, 1);
            searchRect.pivot = new Vector2(0.5f, 1);
            searchRect.offsetMin = new Vector2(8, -75); searchRect.offsetMax = new Vector2(-45, -45);
            searchBarObj.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 1f);

            GameObject placeholderObj = CreateText(searchBarObj.transform, "Placeholder", "Search Items...");
            placeholderObj.GetComponent<Text>().color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderObj.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            RectTransform pRect = placeholderObj.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero; pRect.anchorMax = Vector2.one;
            pRect.offsetMin = new Vector2(10, 0); pRect.offsetMax = new Vector2(-10, 0);

            GameObject textObj = CreateText(searchBarObj.transform, "Text", "");
            textObj.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(10, 0); tRect.offsetMax = new Vector2(-10, 0);

            InputField inputField = searchBarObj.GetComponent<InputField>();
            inputField.textComponent = textObj.GetComponent<Text>();
            inputField.placeholder = placeholderObj.GetComponent<Text>();
            inputField.text = _searchQuery;

            inputField.onValueChanged.AddListener((val) => {
                _searchQuery = val.ToLower();
                PopulateActiveTab(stage);
            });

            // X (Clear) Button next to Search bar
            GameObject clearBtnObj = CreateButton(_referenceAreaContainer.transform, "ClearBtn", "✖");
            RectTransform clearBtnRect = clearBtnObj.GetComponent<RectTransform>();
            clearBtnRect.anchorMin = new Vector2(1, 1); clearBtnRect.anchorMax = new Vector2(1, 1);
            clearBtnRect.pivot = new Vector2(1, 1);
            clearBtnRect.offsetMin = new Vector2(-38, -75); clearBtnRect.offsetMax = new Vector2(-8, -45);
            clearBtnObj.GetComponent<Button>().onClick.AddListener(() => {
                inputField.text = "";
                _searchQuery = "";
                PopulateActiveTab(stage);
            });

            GameObject contentArea = new GameObject("ContentArea", typeof(RectTransform), typeof(ScrollRect));
            contentArea.transform.SetParent(_referenceAreaContainer.transform, false);
            RectTransform contentAreaRect = contentArea.GetComponent<RectTransform>();
            contentAreaRect.anchorMin = Vector2.zero; contentAreaRect.anchorMax = new Vector2(1, 1);
            contentAreaRect.offsetMin = new Vector2(8, 8); contentAreaRect.offsetMax = new Vector2(-20, -80);

            ScrollRect scroll = contentArea.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            contentArea.AddComponent<SmoothScroll>();

            Scrollbar scrollbar = CreateScrollbar(_referenceAreaContainer.transform);
            RectTransform sbRect = scrollbar.GetComponent<RectTransform>();
            sbRect.offsetMax = new Vector2(-4, -80);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(contentArea.transform, false);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;
            scroll.viewport = vpRect;

            _currentTabContentContainer = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _currentTabContentContainer.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = _currentTabContentContainer.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero; contentRect.offsetMax = Vector2.zero;
            scroll.content = contentRect;

            VerticalLayoutGroup vlgContent = _currentTabContentContainer.GetComponent<VerticalLayoutGroup>();
            vlgContent.spacing = 6; vlgContent.padding = new RectOffset(4, 4, 4, 4);
            vlgContent.childForceExpandWidth = true; vlgContent.childForceExpandHeight = false;
            vlgContent.childControlWidth = true; vlgContent.childControlHeight = true;
            _currentTabContentContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            string[] tabNames = { "GEAR", "DROPS", "RECIPES" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int captured = i;
                GameObject tabBtn = new GameObject("Tab_" + tabNames[i], typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                tabBtn.transform.SetParent(tabBar.transform, false);
                tabBtn.GetComponent<LayoutElement>().preferredWidth = 90;

                bool isActive = i == _referenceTabIndex;
                tabBtn.GetComponent<Image>().color = isActive
                    ? new Color(0.35f, 0.28f, 0.15f, 1f)
                    : new Color(0.2f, 0.2f, 0.2f, 1f);

                GameObject tabLabel = CreateText(tabBtn.transform, "Label", tabNames[i]);
                RectTransform labelRect = tabLabel.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
                tabLabel.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                tabLabel.GetComponent<Text>().fontSize = 14;

                tabBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _referenceTabIndex = captured;
                    BuildReferenceArea(stage);
                });
            }

            PopulateActiveTab(stage);
        }

        private static void PopulateActiveTab(Stage stage)
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

        private static void BuildGearTab(Transform parent, Stage stage)
        {
            if (stage.Gear == null || stage.Gear.Count == 0)
            {
                AddLabel(parent, "No gear data.", 14, FontStyle.Italic, new Color(0.6f, 0.6f, 0.6f));
                return;
            }

            int count = 0;
            foreach (GearEntry gear in stage.Gear)
            {
                if (!string.IsNullOrEmpty(_searchQuery) && !gear.Label.ToLower().Contains(_searchQuery))
                    continue;

                count++;
                GameObject row = new GameObject("Row_" + gear.ItemId, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(parent, false);
                row.GetComponent<LayoutElement>().preferredHeight = 24;

                HorizontalLayoutGroup rowHlg = row.GetComponent<HorizontalLayoutGroup>();
                rowHlg.spacing = 6;
                rowHlg.childForceExpandWidth = false; rowHlg.childForceExpandHeight = true;
                rowHlg.childControlHeight = true; rowHlg.childControlWidth = false;

                GameObject checkBox = new GameObject("Checkbox", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                checkBox.transform.SetParent(row.transform, false);
                checkBox.GetComponent<LayoutElement>().preferredWidth = 18;
                string itemId = gear.ItemId;
                bool isChecked = ProgressSaver.IsChecked(itemId);
                Image checkImg = checkBox.GetComponent<Image>();
                checkImg.color = isChecked ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);

                GameObject checkMark = CreateText(checkBox.transform, "Mark", isChecked ? "✔" : "");
                RectTransform markRect = checkMark.GetComponent<RectTransform>();
                markRect.anchorMin = Vector2.zero; markRect.anchorMax = Vector2.one;
                markRect.offsetMin = Vector2.zero; markRect.offsetMax = Vector2.zero;
                Text markText = checkMark.GetComponent<Text>();
                markText.alignment = TextAnchor.MiddleCenter;
                markText.fontSize = 12; markText.color = Color.white;

                checkBox.GetComponent<Button>().onClick.AddListener(() =>
                {
                    bool nowChecked = !ProgressSaver.IsChecked(itemId);
                    ProgressSaver.SetChecked(itemId, nowChecked);
                    checkImg.color = nowChecked ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);
                    markText.text = nowChecked ? "✔" : "";
                });

                GameObject nameObj = CreateText(row.transform, "Name", gear.Label.ToUpper());
                nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 0);
                nameObj.GetComponent<Text>().fontSize = 15;
                nameObj.GetComponent<Text>().fontStyle = FontStyle.Bold;

                string type = gear.Type + "  ·  " + gear.Station;
                if (gear.StationLevel > 1) type += " (Lv " + gear.StationLevel + ")";
                AddLabel(parent, type, 13, FontStyle.Normal, new Color(0.7f, 0.7f, 0.7f));

                if (gear.Recipe != null && gear.Recipe.Count > 0)
                {
                    string recipe = "Recipe: " + string.Join(", ", gear.Recipe.ConvertAll(r => $"{r.Amount}x {r.Label}"));
                    AddLabel(parent, recipe, 13, FontStyle.Normal, Color.white);
                }

                AddSpacer(parent);
            }

            if (count == 0)
                AddLabel(parent, "No results found.", 14, FontStyle.Italic, new Color(0.6f, 0.6f, 0.6f));
        }

        private static void BuildDropsTab(Transform parent, Stage stage)
        {
            if (stage.Drops == null || stage.Drops.Count == 0)
            {
                AddLabel(parent, "No drop data.", 14, FontStyle.Italic, new Color(0.6f, 0.6f, 0.6f));
                return;
            }

            int count = 0;
            foreach (DropEntry drop in stage.Drops)
            {
                if (!string.IsNullOrEmpty(_searchQuery) && !drop.Label.ToLower().Contains(_searchQuery))
                    continue;

                count++;
                AddLabel(parent, drop.Label.ToUpper(), 15, FontStyle.Bold, Color.white);

                foreach (DropSource src in drop.Sources)
                {
                    string line = $"{src.Mob}  ·  {src.Min}-{src.Max}  ·  {(int)(src.Chance * 100)}%";
                    if (src.StarVariantOnly) line += "  (★ only)";
                    AddLabel(parent, line, 13, FontStyle.Normal, new Color(0.75f, 0.75f, 0.75f));
                }

                AddSpacer(parent);
            }
            if (count == 0)
                AddLabel(parent, "No results found.", 14, FontStyle.Italic, new Color(0.6f, 0.6f, 0.6f));
        }

        private static void BuildRecipesTab(Transform parent, Stage stage)
        {
            if (stage.Recipes == null || stage.Recipes.Count == 0)
            {
                AddLabel(parent, "No recipe data.", 14, FontStyle.Italic, new Color(0.6f, 0.6f, 0.6f));
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
                rowHlg.childForceExpandWidth = false; rowHlg.childForceExpandHeight = true;
                rowHlg.childControlHeight = true; rowHlg.childControlWidth = false;

                string itemId = recipe.ItemId;
                bool isChecked = ProgressSaver.IsChecked(itemId);

                GameObject checkBox = new GameObject("Checkbox", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                checkBox.transform.SetParent(row.transform, false);
                checkBox.GetComponent<LayoutElement>().preferredWidth = 20;
                Image checkImg = checkBox.GetComponent<Image>();
                checkImg.color = isChecked ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.3f, 0.3f, 0.3f);

                checkBox.GetComponent<Button>().onClick.AddListener(() =>
                {
                    bool nowChecked = !ProgressSaver.IsChecked(itemId);
                    ProgressSaver.SetChecked(itemId, nowChecked);
                    checkImg.color = nowChecked ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.3f, 0.3f, 0.3f);
                });

                GameObject nameObj = CreateText(row.transform, "Name", recipe.Label.ToUpper());
                nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 0);
                nameObj.GetComponent<Text>().fontSize = 15;
                nameObj.GetComponent<Text>().fontStyle = FontStyle.Bold;

                string station = recipe.Station;
                if (recipe.StationLevel > 1) station += " (Lv " + recipe.StationLevel + ")";
                AddLabel(parent, station, 13, FontStyle.Normal, new Color(0.7f, 0.7f, 0.7f));

                if (!string.IsNullOrEmpty(recipe.UnlockNote))
                    AddLabel(parent, recipe.UnlockNote, 13, FontStyle.Italic, new Color(0.6f, 1f, 0.6f));

                if (recipe.Ingredients != null && recipe.Ingredients.Count > 0)
                {
                    string ingredients = "Ingredients: " + string.Join(", ", recipe.Ingredients.ConvertAll(i => $"{i.Amount}x {i.Label}"));
                    AddLabel(parent, ingredients, 13, FontStyle.Normal, Color.white);
                }

                AddSpacer(parent);
            }
            if (count == 0)
                AddLabel(parent, "No results found.", 14, FontStyle.Italic, new Color(0.6f, 0.6f, 0.6f));
        }
    }

    // ─────────────────────────────────────────
    //  BUTTERY SMOOTH SCROLL COMPONENT
    // ─────────────────────────────────────────
    public class SmoothScroll : MonoBehaviour, IScrollHandler, IBeginDragHandler
    {
        private ScrollRect _scrollRect;
        private float _scrollSpeed = 350f; // Extreme Scroll Speed
        private float _smoothTime = 0.08f;

        private Vector2 _targetPos;
        private Vector2 _velocity = Vector2.zero;
        private bool _isScrolling = false;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            _scrollRect.scrollSensitivity = 0f;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isScrolling = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!_isScrolling)
            {
                _targetPos = _scrollRect.content.anchoredPosition;
                _isScrolling = true;
            }

            // -= to fix Unity's inverted Y-axis math
            _targetPos.y -= eventData.scrollDelta.y * _scrollSpeed;

            float minY = 0;
            float maxY = Mathf.Max(0, _scrollRect.content.rect.height - _scrollRect.viewport.rect.height);
            _targetPos.y = Mathf.Clamp(_targetPos.y, minY, maxY);
        }

        private void Update()
        {
            if (_isScrolling && _scrollRect.content != null)
            {
                _scrollRect.content.anchoredPosition = Vector2.SmoothDamp(
                    _scrollRect.content.anchoredPosition,
                    _targetPos,
                    ref _velocity,
                    _smoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);

                if (Vector2.Distance(_scrollRect.content.anchoredPosition, _targetPos) < 0.5f)
                {
                    _isScrolling = false;
                }
            }
        }
    }
}