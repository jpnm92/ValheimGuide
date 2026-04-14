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
        private const float HeaderHeight = 24f;
        private const float RowHeight = 15f;
        private const float RowSpacing = 6f;
        private const float PanelPaddingV = 8f;

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
            if (_timer >= Plugin.TrackerRefreshRate.Value)
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

            // --- THE MAGIC BULLET FOR 4K / 768p SCALING ---
            var scaler = _canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // 1080p is our master blueprint
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Outer panel
            _panel = new GameObject("TrackerPanel",
                typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_canvas.transform, false);
            _panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.04f, 0.82f);

            var pr = _panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(1, 1);
            pr.anchorMax = new Vector2(1, 1);
            pr.pivot = new Vector2(1, 1);

            // Apply our Config X/Y Offsets
            pr.anchoredPosition = new Vector2(Plugin.TrackerOffsetX.Value, Plugin.TrackerOffsetY.Value);
            pr.sizeDelta = new Vector2(Plugin.TrackerWidth.Value, HeaderHeight);

            // Apply our Config Scale
            pr.localScale = new Vector3(Plugin.TrackerScale.Value, Plugin.TrackerScale.Value, 1f);

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
            UpdatePanelSize();
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
                var pending = visible.Where(o => !ProgressionTracker.IsObjectiveComplete(o)).ToList();
                var done = visible.Where(o => ProgressionTracker.IsObjectiveComplete(o)).ToList();

                int shown = 0;
                foreach (var obj in pending)
                {
                    if (shown >= Plugin.TrackerMaxRows.Value) break;
                    AddRow(obj, false);
                    shown++;
                }
                foreach (var obj in done)
                {
                    if (shown >= Plugin.TrackerMaxRows.Value) break;
                    AddRow(obj, true);
                    shown++;
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

            // Change from preferredHeight to minHeight so it can expand!
            var le = row.GetComponent<LayoutElement>();
            le.minHeight = 18f;

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false; // Changed to false so text pushes the height
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft; // Vertically centers the text

            // Tick (Clean bullet point)
            var tick = MakeText(row.transform, "Tick",
                done ? "✔" : "▪",
                16, FontStyle.Normal, // BUMPED TO 16
                done ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.5f, 0.5f, 0.5f),
                preferWidth: 16f);
            tick.alignment = TextAnchor.MiddleCenter;

            // Create the base text
            string fullText = obj.Text;

            // If the objective isn't done yet, try to attach the live resource tracker!
            if (!done && !string.IsNullOrEmpty(obj.Value))
            {
                fullText += GetMaterialProgress(obj);
            }

            // Text (More legible font size)
            var lbl = MakeText(row.transform, "Label", fullText, // Notice we use fullText here now!
                15, FontStyle.Normal,
                done ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.9f, 0.9f, 0.9f),
                flexWidth: true);
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.verticalOverflow = VerticalWrapMode.Truncate;

            // Add a ContentSizeFitter to the text so it calculates its own height when wrapping!
            lbl.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void UpdatePanelSize()
        {
            // Force layout rebuild so Unity calculates the new wrapped text heights BEFORE we read them
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();

            float contentH = _collapsed
                ? 0f
                : _contentRoot.GetComponent<RectTransform>().rect.height;

            float totalH = HeaderHeight + (_collapsed ? 0f : contentH + 8f); // Added 8f padding at the bottom
            _panel.GetComponent<RectTransform>().sizeDelta = new Vector2(Plugin.TrackerWidth.Value, totalH);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsInGame() =>
            Player.m_localPlayer != null && ZNet.instance != null;

        private string GetMaterialProgress(Objective obj)
        {
            if (Player.m_localPlayer == null || ObjectDB.instance == null || ZNetScene.instance == null) return "";

            Piece.Requirement[] requirements = null;

            // 1. Try to find the item as a Crafting Recipe (e.g., "SpearFlint")
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(obj.Value);
            if (itemPrefab != null)
            {
                var itemDrop = itemPrefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    var recipe = ObjectDB.instance.GetRecipe(itemDrop.m_itemData);
                    if (recipe != null) requirements = recipe.m_resources;
                }
            }
            else
            {
                // 2. Try to find the item as a Build Piece (e.g., "piece_workbench")
                GameObject piecePrefab = ZNetScene.instance.GetPrefab(obj.Value);
                if (piecePrefab != null)
                {
                    var piece = piecePrefab.GetComponent<Piece>();
                    if (piece != null) requirements = piece.m_resources;
                }
            }

            // If we couldn't find any recipe or costs, return nothing
            if (requirements == null || requirements.Length == 0) return "";

            Inventory inv = Player.m_localPlayer.GetInventory();
            List<string> reqStrings = new List<string>();

            // Count the items!
            foreach (var req in requirements)
            {
                if (req.m_resItem == null) continue;

                string matName = req.m_resItem.m_itemData.m_shared.m_name;
                string locName = global::Localization.instance.Localize(matName);
                int have = inv.CountItems(matName);
                int need = req.m_amount;

                // Color it Green if they have enough, Red if they are missing some
                string color = have >= need ? "#80FF80" : "#FF8080";
                reqStrings.Add($"<color={color}>{have}/{need}</color> {locName}");
            }

            return $"\n<color=#B0B0B0><size=12>Requires: {string.Join(", ", reqStrings)}</size></color>";
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