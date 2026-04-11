using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
        private static GuidePanelController _controller;

        public static void Show()
        {
            if (_panel == null || !_panel)
                CreatePanel();

            _panel.SetActive(true);
            _isVisible = true;
            GUIManager.BlockInput(true);
            _controller.RefreshContent();
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
            // Reuse an existing canvas if one survived a scene reload
            GameObject canvasObj = GameObject.Find("ValheimGuideCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("ValheimGuideCanvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 20000;
                UnityEngine.Object.DontDestroyOnLoad(canvasObj);
            }

            _panel = new GameObject("GuidePanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvasObj.transform, false);
            UnityEngine.Object.DontDestroyOnLoad(_panel);
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
            title.GetComponent<TextMeshProUGUI>().fontSize = 28;
            title.GetComponent<TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

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

            _controller = new GuidePanelController(_stageListContainer, _smartPanelContainer, _referenceAreaContainer);
            _panel.SetActive(false);
        }

        internal static void AddLabel(Transform parent, string content, int fontSize, TMPro.FontStyles style, Color color)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            t.overflowMode = TMPro.TextOverflowModes.Overflow;

            go.GetComponent<LayoutElement>().flexibleWidth = 1;
        }

        internal static void AddSpacer(Transform parent)
        {
            GameObject go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 6;
        }

        internal static GameObject CreatePanelSection(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        internal static GameObject CreateButton(Transform parent, string name, string label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);

            GameObject textGo = CreateText(go.transform, "Text", label);
            Text text = textGo.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 20;

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return go;
        }

        internal static GameObject CreateText(Transform parent, string name, string content)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.color = Color.white;
            text.fontSize = 18;
            text.raycastTarget = false;

            return go;
        }

        internal static Scrollbar CreateScrollbar(Transform parent)
        {
            GameObject scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObj.transform.SetParent(parent, false);

            RectTransform scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-14, 4);
            scrollbarRect.offsetMax = new Vector2(-4, -4);
            scrollbarObj.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.8f);

            GameObject slidingAreaObj = new GameObject("Sliding Area", typeof(RectTransform));
            slidingAreaObj.transform.SetParent(scrollbarObj.transform, false);
            RectTransform slidingAreaRect = slidingAreaObj.GetComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.offsetMin = new Vector2(2, 2);
            slidingAreaRect.offsetMax = new Vector2(-2, -2);

            GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObj.transform.SetParent(slidingAreaObj.transform, false);
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            handleRect.sizeDelta = Vector2.zero;
            handleObj.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);

            Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            return scrollbar;
        }
    }

    public class SmoothScroll : MonoBehaviour, IScrollHandler, IBeginDragHandler
    {
        private const float ScrollSpeed = 350f;

        private ScrollRect _scrollRect;
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

            _targetPos.y -= eventData.scrollDelta.y * ScrollSpeed;

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