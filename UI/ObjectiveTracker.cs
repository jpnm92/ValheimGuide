using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ValheimGuide.Data;
using Jotunn.Managers;

namespace ValheimGuide.UI
{
    /// <summary>
    /// WoW-style on-screen objective tracker. Anchored top-right, below the minimap.
    /// Defers all UI construction to GUIManager.OnCustomGUIAvailable so Jotunn fonts
    /// are guaranteed to exist before we touch them.
    /// </summary>
    public class ObjectiveTracker : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        private static ObjectiveTracker _instance;

        public static void Initialise()
        {
            if (_instance != null) return;
            var go = new GameObject("ValheimGuideTrackerRoot");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ObjectiveTracker>();
        }

        public static void ForceRefresh()
        {
            if (_instance != null && _instance._isBuilt)
                _instance.RefreshTracker();
        }

        // ── State ─────────────────────────────────────────────────────────────
        private bool _isBuilt = false;
        private bool _collapsed = false;
        private float _timer = 0f;

        // ── UI refs ───────────────────────────────────────────────────────────
        private GameObject _canvas;
        private GameObject _panel;
        private GameObject _contentRoot;
        private Text _stageLabel;
        private Text _collapseArrow;

        // ── Layout constants ──────────────────────────────────────────────────
        private const float PanelWidth = 230f;
        private const float HeaderHeight = 24f;
        private const float RowHeight = 15f;
        private const float RowSpacing = 2f;
        private const float PaddingV = 4f;
        private const int MaxRows = 6;
        private const float RefreshSecs = 3f;

        // Adjust Y to sit just below your minimap. Negative = down from top-right.
        private static readonly Vector2 TrackerAnchor = new Vector2(-12f, -162f);

        // ── Reflection ────────────────────────────────────────────────────────
        private static readonly FieldInfo KnownRecipesField =
            typeof(Player).GetField("m_knownRecipes",
                BindingFlags.NonPublic | BindingFlags.Instance);

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Subscribe to Jotunn's event — fires once fonts + GUI system are ready.
            // This is the ONLY safe place to create UI that uses GUIManager fonts.
            GUIManager.OnCustomGUIAvailable += OnGUIReady;
            ProgressionTracker.OnStageChanged += OnStageChanged;
        }

        private void OnDestroy()
        {
            GUIManager.OnCustomGUIAvailable -= OnGUIReady;
            ProgressionTracker.OnStageChanged -= OnStageChanged;
        }

        private void OnGUIReady()
        {
            if (_isBuilt) return;
            BuildUI();
            _isBuilt = true;
            RefreshTracker();
        }

        private void OnStageChanged(Stage _) => RefreshTracker();

        private void Update()
        {
            if (!_isBuilt) return;

            _timer += Time.unscaledDeltaTime;
            if (_timer >= RefreshSecs)
            {
                _timer = 0f;
                RefreshTracker();
            }
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            // Dedicated canvas — sits above HUD but below the guide panel
            _canvas = new GameObject("ValheimGuideTrackerCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(_canvas);

            var cv = _canvas.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.overrideSorting = true;
            cv.sortingOrder = 19998;

            // Outer panel
            _panel = MakeObject("TrackerPanel", _canvas.transform,
                typeof(RectTransform), typeof(Image));
            _panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.04f, 0.82f);

            var pr = _panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(1, 1);
            pr.anchorMax = new Vector2(1, 1);
            pr.pivot = new Vector2(1, 1);
            pr.anchoredPosition = TrackerAnchor;
            pr.sizeDelta = new Vector2(PanelWidth, HeaderHeight);

            // ── Header ──────────────────────────────────────────────────────
            var header = MakeObject("Header", _panel.transform,
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var hr = header.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0, 1);
            hr.anchorMax = new Vector2(1, 1);
            hr.pivot = new Vector2(0.5f, 1);
            hr.offsetMin = new Vector2(6, -HeaderHeight);
            hr.offsetMax = new Vector2(-4, 0);

            var hlg = header.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            _stageLabel = MakeText(header.transform, "StageLabel", "—",
                12, FontStyle.Bold, new Color(1f, 0.75f, 0.3f), flexWidth: true);

            // Collapse button
            var colBtn = MakeObject("CollapseBtn", header.transform,
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            colBtn.GetComponent<LayoutElement>().preferredWidth = 16;
            colBtn.GetComponent<Image>().color = Color.clear;
            _collapseArrow = MakeText(colBtn.transform, "Arrow", "▲",
                10, FontStyle.Normal, new Color(0.7f, 0.7f, 0.7f));
            var ar = _collapseArrow.GetComponent<RectTransform>();
            ar.anchorMin = Vector2.zero;
            ar.anchorMax = Vector2.one;
            ar.offsetMin = ar.offsetMax = Vector2.zero;
            _collapseArrow.alignment = TextAnchor.MiddleCenter;
            colBtn.GetComponent<Button>().onClick.AddListener(ToggleCollapse);

            // ── Content area ─────────────────────────────────────────────────
            _contentRoot = MakeObject("Content", _panel.transform,
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var cr = _contentRoot.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1);
            cr.anchorMax = new Vector2(1, 1);
            cr.pivot = new Vector2(0.5f, 1);
            cr.offsetMin = new Vector2(6, -400);
            cr.offsetMax = new Vector2(-4, -HeaderHeight - 2);

            var vlg = _contentRoot.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = RowSpacing;
            vlg.padding = new RectOffset(0, 0, 2, 2);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            _contentRoot.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _panel.SetActive(false);
        }

        // ── Collapse ──────────────────────────────────────────────────────────

        private void ToggleCollapse()
        {
            _collapsed = !_collapsed;
            _contentRoot.SetActive(!_collapsed);
            _collapseArrow.text = _collapsed ? "▼" : "▲";
            UpdatePanelHeight();
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void RefreshTracker()
        {
            if (!_isBuilt || _panel == null) return;

            var stage = ProgressionTracker.CurrentStage;

            if (stage == null || !IsInGame())
            {
                _panel.SetActive(false);
                return;
            }

            _stageLabel.text = stage.Label.ToUpper();

            // Rebuild rows
            foreach (Transform child in _contentRoot.transform)
                Destroy(child.gameObject);

            if (!_collapsed)
            {
                var visible = (stage.Objectives ?? new List<Objective>())
                    .Where(o => string.IsNullOrEmpty(o.ModRequired) ||
                                GuideDataLoader.InstalledMods.Contains(o.ModRequired))
                    .ToList();

                if (visible.Count == 0)
                {
                    _panel.SetActive(false);
                    return;
                }

                // Incomplete first, then done — cap total at MaxRows
                var pending = visible.Where(o => !IsComplete(o)).ToList();
                var done = visible.Where(o => IsComplete(o)).ToList();

                int shown = 0;
                foreach (var obj in pending)
                {
                    if (shown >= MaxRows) break;
                    AddRow(obj, false);
                    shown++;
                }
                foreach (var obj in done)
                {
                    if (shown >= MaxRows) break;
                    AddRow(obj, true);
                    shown++;
                }
            }

            _panel.SetActive(true);
            UpdatePanelHeight();
        }

        private void AddRow(Objective obj, bool done)
        {
            var row = MakeObject("Row_" + obj.Id, _contentRoot.transform,
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.GetComponent<LayoutElement>().preferredHeight = RowHeight;

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 3;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            string tick = done
                ? "✔"
                : (obj.Type == "build" ? "□" : "○");
            Color tickCol = done
                ? new Color(0.4f, 0.8f, 0.4f)
                : new Color(0.65f, 0.65f, 0.65f);
            Color textCol = done
                ? new Color(0.45f, 0.45f, 0.45f)
                : Color.white;

            var t = MakeText(row.transform, "Tick", tick,
                10, FontStyle.Normal, tickCol, preferWidth: 12f);
            t.alignment = TextAnchor.UpperCenter;

            var lbl = MakeText(row.transform, "Label", obj.Text,
                10, FontStyle.Normal, textCol, flexWidth: true);
            lbl.alignment = TextAnchor.UpperLeft;
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void UpdatePanelHeight()
        {
            Canvas.ForceUpdateCanvases();
            float contentH = _collapsed
                ? 0f
                : _contentRoot.GetComponent<RectTransform>().rect.height;
            float total = HeaderHeight + (_collapsed ? 0f : contentH + PaddingV);
            _panel.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelWidth, total);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsInGame() =>
            Player.m_localPlayer != null && ZNet.instance != null;

        private bool IsComplete(Objective obj)
        {
            if (!obj.AutoComplete)
                return ProgressSaver.IsChecked("obj_" + obj.Id);

            switch (obj.Type.ToLowerInvariant())
            {
                case "globalkey":
                case "boss":
                    return ZoneSystem.instance?.GetGlobalKey(obj.Value) ?? false;
                case "craftitem":
                case "knownrecipe":
                    var p = Player.m_localPlayer;
                    if (p == null) return false;
                    var recipes = KnownRecipesField?.GetValue(p) as HashSet<string>;
                    return recipes?.Contains(obj.Value) ?? false;
                case "hasitem":
                    var pl = Player.m_localPlayer;
                    return pl != null && pl.GetInventory().CountItems(obj.Value) > 0;
                default:
                    return ProgressSaver.IsChecked("obj_" + obj.Id);
            }
        }

        // ── UI factory helpers ────────────────────────────────────────────────

        private static GameObject MakeObject(string name, Transform parent,
            params System.Type[] components)
        {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Text MakeText(Transform parent, string name, string content,
            int size, FontStyle style, Color color,
            bool flexWidth = false, float preferWidth = -1f)
        {
            var go = MakeObject(name, parent,
                typeof(RectTransform), typeof(Text), typeof(LayoutElement));

            var t = go.GetComponent<Text>();
            t.text = content;
            t.font = GUIManager.Instance.AveriaSerifBold;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;

            var le = go.GetComponent<LayoutElement>();
            if (flexWidth) le.flexibleWidth = 1f;
            if (preferWidth > 0) le.preferredWidth = preferWidth;

            return t;
        }
    }
}