using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ValheimGuide.Data;
using Jotunn.Managers;

namespace ValheimGuide.UI
{
    public class ObjectiveTracker : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        private static ObjectiveTracker _instance;

        public static void Initialise()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("ValheimGuideTrackerRoot");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ObjectiveTracker>();
        }

        public static void ForceRefresh()
        {
            _instance?.RefreshTracker();
        }

        // ── UI references ─────────────────────────────────────────────────────
        private GameObject _canvas;
        private GameObject _panel;
        private GameObject _contentRoot;       // holds objective rows
        private Text _stageLabel;
        private Text _collapseArrow;
        private bool _collapsed = false;

        // ── Config ────────────────────────────────────────────────────────────
        private const float RefreshInterval = 3f;
        private const int MaxShown = 6;
        private const float PanelWidth = 230f;
        private const float HeaderHeight = 24f;
        private const float RowHeight = 15f;
        private const float RowSpacing = 2f;
        private const float PanelPaddingV = 4f;


        // ── Reflection cache ──────────────────────────────────────────────────
        private static readonly FieldInfo KnownRecipesField =
            typeof(Player).GetField("m_knownRecipes",
                BindingFlags.NonPublic | BindingFlags.Instance);

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private float _timer;

        private void Start()
        {
            ProgressionTracker.OnStageChanged += _ => RefreshTracker();
            BuildUI();
        }

        private void OnDestroy()
        {
            ProgressionTracker.OnStageChanged -= _ => RefreshTracker();
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= RefreshInterval)
            {
                _timer = 0f;
                RefreshTracker();
            }
        }

        // ── UI construction ───────────────────────────────────────────────────
        private void BuildUI()
        {
            // Dedicated canvas so it never interferes with the guide panel
            _canvas = new GameObject("ValheimGuideTrackerCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(_canvas);

            var cv = _canvas.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.overrideSorting = true;
            cv.sortingOrder = 19998;   // below guide (20000) but above HUD

            // Outer panel
            _panel = new GameObject("TrackerPanel",
                typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_canvas.transform, false);
            _panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.04f, 0.82f);

            var pr = _panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(1, 1);
            pr.anchorMax = new Vector2(1, 1);
            pr.pivot = new Vector2(1, 1);

            // --- USE THE CONFIG VALUES HERE ---
            pr.anchoredPosition = new Vector2(Plugin.TrackerOffsetX.Value, Plugin.TrackerOffsetY.Value);

            pr.sizeDelta = new Vector2(PanelWidth, HeaderHeight);

            // ── Header row ──────────────────────────────────────────────────
            GameObject header = new GameObject("Header",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            header.transform.SetParent(_panel.transform, false);

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

            // Stage name
            _stageLabel = MakeText(header.transform, "StageLabel", "MEADOWS", 12, FontStyle.Bold,
                new Color(1f, 0.75f, 0.3f), flexWidth: true);

            // Collapse toggle
            GameObject collapseBtn = new GameObject("CollapseBtn",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            collapseBtn.transform.SetParent(header.transform, false);
            collapseBtn.GetComponent<LayoutElement>().preferredWidth = 16;
            collapseBtn.GetComponent<Image>().color = Color.clear;
            _collapseArrow = MakeText(collapseBtn.transform, "Arrow", "▲", 10, FontStyle.Normal,
                new Color(0.7f, 0.7f, 0.7f));
            var arrowRect = _collapseArrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = Vector2.zero;
            arrowRect.anchorMax = Vector2.one;
            arrowRect.offsetMin = arrowRect.offsetMax = Vector2.zero;
            _collapseArrow.alignment = TextAnchor.MiddleCenter;
            collapseBtn.GetComponent<Button>().onClick.AddListener(ToggleCollapse);

            // ── Content container ────────────────────────────────────────────
            _contentRoot = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _contentRoot.transform.SetParent(_panel.transform, false);

            var cr = _contentRoot.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1);
            cr.anchorMax = new Vector2(1, 1);
            cr.pivot = new Vector2(0.5f, 1);
            cr.offsetMin = new Vector2(6, -300);
            cr.offsetMax = new Vector2(-4, -HeaderHeight - 2);

            var cvlg = _contentRoot.GetComponent<VerticalLayoutGroup>();
            cvlg.spacing = RowSpacing;
            cvlg.padding = new RectOffset(0, 0, 2, 2);
            cvlg.childForceExpandWidth = true;
            cvlg.childForceExpandHeight = false;
            cvlg.childControlWidth = true;
            cvlg.childControlHeight = true;

            _contentRoot.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _panel.SetActive(false);
            RefreshTracker();
        }

        // ── Toggle ────────────────────────────────────────────────────────────
        private void ToggleCollapse()
        {
            _collapsed = !_collapsed;
            _contentRoot.SetActive(!_collapsed);
            _collapseArrow.text = _collapsed ? "▼" : "▲";
            UpdatePanelSize();
        }

        // ── Refresh ───────────────────────────────────────────────────────────
        private void RefreshTracker()
        {
            if (_panel == null) return;

            Stage stage = ProgressionTracker.CurrentStage;

            if (stage == null || !IsInGame())
            {
                _panel.SetActive(false);
                return;
            }

            _stageLabel.text = stage.Label.ToUpper();

            // Clear rows
            foreach (Transform child in _contentRoot.transform)
                Destroy(child.gameObject);

            if (!_collapsed)
            {
                string playstyleId = ProgressSaver.Current?.PlaystyleId ?? "all";

                // FILTER UPDATE: Now respects the Playstyle filter exactly like the Guide Panel!
                List<Objective> all = (stage.Objectives ?? new List<Objective>())
                    .Where(o =>
                        (string.IsNullOrEmpty(o.ModRequired) || GuideDataLoader.InstalledMods.Contains(o.ModRequired)) &&
                        (string.IsNullOrEmpty(o.PlaystyleFilter) || playstyleId == "all" || playstyleId == o.PlaystyleFilter)
                    ).ToList();

                // Incomplete first, then ticked — cap at MaxShown
                var incomplete = all.Where(o => !IsComplete(o)).ToList();
                var complete = all.Where(o => IsComplete(o)).ToList();

                int shown = 0;
                foreach (var obj in incomplete)
                {
                    if (shown >= MaxShown) break;
                    AddRow(obj, done: false);
                    shown++;
                }
                foreach (var obj in complete)
                {
                    if (shown >= MaxShown) break;
                    AddRow(obj, done: true);
                    shown++;
                }

                if (all.Count == 0)
                {
                    _panel.SetActive(false);
                    return;
                }
            }

            _panel.SetActive(true);
            UpdatePanelSize();
        }

        private void AddRow(Objective obj, bool done)
        {
            GameObject row = new GameObject("Row",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_contentRoot.transform, false);
            row.GetComponent<LayoutElement>().preferredHeight = RowHeight;

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 3;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            // Tick (Replaced the ugly circles with a clean bullet point)
            var tick = MakeText(row.transform, "Tick",
                done ? "✔" : "▪",
                12, FontStyle.Normal,
                done ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.5f, 0.5f, 0.5f),
                preferWidth: 16f);
            tick.alignment = TextAnchor.UpperCenter;

            // Text (Adjusted color and sizing to look more native)
            var lbl = MakeText(row.transform, "Label", obj.Text,
                11, FontStyle.Normal,
                done ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.9f, 0.9f, 0.9f),
                flexWidth: true);
            lbl.alignment = TextAnchor.UpperLeft;
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void UpdatePanelSize()
        {
            Canvas.ForceUpdateCanvases();

            float contentH = _collapsed
                ? 0f
                : _contentRoot.GetComponent<RectTransform>().rect.height;

            float totalH = HeaderHeight + (_collapsed ? 0f : contentH + PanelPaddingV);
            _panel.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelWidth, totalH);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static bool IsInGame() =>
            Player.m_localPlayer != null && ZNet.instance != null;

        private bool IsComplete(Objective obj)
        {
            if (!obj.AutoComplete)
                return ProgressSaver.IsChecked("obj_" + obj.Id);

            // LOGIC UPDATE: Match the Guide Panel! Check if it was manually ticked in the Gear tab.
            if (!string.IsNullOrEmpty(obj.Value) && ProgressSaver.IsChecked(obj.Value))
                return true;

            // LOGIC UPDATE: Check if our new Build or Inventory patches permanently completed it.
            if (ProgressSaver.IsChecked("obj_" + obj.Id))
                return true;

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
                    if (pl == null) return false;
                    return pl.GetInventory().CountItems(obj.Value) > 0;

                default:
                    return false;
            }
        }

        private static Text MakeText(Transform parent, string name, string content,
            int fontSize, FontStyle style, Color color,
            bool flexWidth = false, float preferWidth = -1f)
        {
            var go = new GameObject(name,
                typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var t = go.GetComponent<Text>();
            t.text = content;
            t.font = GUIManager.Instance.AveriaSerifBold;
            t.fontSize = fontSize;
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