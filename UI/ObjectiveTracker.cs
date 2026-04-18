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

        /// <summary>
        /// Called by InventoryGuiPatch and GuidePanel to hide/show the tracker
        /// whenever another full-screen UI takes over.
        /// </summary>
        public static void SetVisible(bool visible)
        {
            if (_instance == null) return;
            _instance._forcedHidden = !visible;

            if (!visible && _instance._panel != null)
            {
                _instance._panel.SetActive(false);
            }
            else if (visible && _instance._isBuilt)
            {
                _instance.RefreshTracker(); // re-render with current state
            }
        }

        // ── State ─────────────────────────────────────────────────────────────
        private bool   _isBuilt      = false;
        private bool   _collapsed    = false;
        private bool   _forcedHidden = false;  // true while inventory / guide is open
        private float  _timer        = 0f;

        // ── UI refs ───────────────────────────────────────────────────────────
        private GameObject _canvas;
        private GameObject _panel;
        private GameObject _contentRoot;
        private Text        _stageLabel;
        private Text        _collapseArrow;

        // ── Layout constants ──────────────────────────────────────────────────
        private const float HeaderHeight  = 24f;
        private const float RowSpacing    = 6f;

        // ── Label cache — rebuilt once per LoadGuideData call ─────────────────
        // Key: ItemId   Value: human-readable label
        private static Dictionary<string, string> _labelCache = new Dictionary<string, string>();

        /// <summary>Call after every GuideDataLoader.Load to invalidate stale labels.</summary>
        public static void InvalidateLabelCache() => _labelCache.Clear();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            GUIManager.OnCustomGUIAvailable  += OnGUIReady;
            ProgressionTracker.OnStageChanged += OnStageChanged;
        }

        private void OnDestroy()
        {
            GUIManager.OnCustomGUIAvailable  -= OnGUIReady;
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
            cv.renderMode      = RenderMode.ScreenSpaceOverlay;
            cv.overrideSorting = true;
            cv.sortingOrder    = 19998;
            cv.pixelPerfect = true;

            var scaler = _canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            // Outer panel
            _panel = new GameObject("TrackerPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_canvas.transform, false);
            _panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.04f, Plugin.TrackerOpacity.Value);

            var pr = _panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(1, 1);
            pr.anchorMax = new Vector2(1, 1);
            pr.pivot     = new Vector2(1, 1);
            pr.anchoredPosition = new Vector2(Plugin.TrackerOffsetX.Value, Plugin.TrackerOffsetY.Value);
            pr.sizeDelta        = new Vector2(Plugin.TrackerWidth.Value, HeaderHeight);
            pr.localScale       = new Vector3(Plugin.TrackerScale.Value, Plugin.TrackerScale.Value, 1f);

            // ── Header ────────────────────────────────────────────────────────
            var header = MakeObject("Header", _panel.transform,
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var hr = header.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0, 1);
            hr.anchorMax = new Vector2(1, 1);
            hr.pivot     = new Vector2(0.5f, 1);
            hr.offsetMin = new Vector2(6, -HeaderHeight);
            hr.offsetMax = new Vector2(-4, 0);

            var hlg = header.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing             = 4;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth   = false;
            hlg.childControlHeight  = true;

            _stageLabel = MakeText(header.transform, "StageLabel", "—",
                Plugin.TrackerFontSize.Value, FontStyle.Bold,
                new Color(1f, 0.75f, 0.3f), flexWidth: true);

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

            // ── Content area ──────────────────────────────────────────────────
            _contentRoot = MakeObject("Content", _panel.transform,
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var cr = _contentRoot.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1);
            cr.anchorMax = new Vector2(1, 1);
            cr.pivot     = new Vector2(0.5f, 1);
            cr.offsetMin = new Vector2(6, -400);
            cr.offsetMax = new Vector2(-4, -HeaderHeight - 2);

            var vlg = _contentRoot.GetComponent<VerticalLayoutGroup>();
            vlg.spacing             = RowSpacing;
            vlg.padding             = new RectOffset(0, 0, 2, 2);
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth   = true;
            vlg.childControlHeight  = true;

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

            if (_forcedHidden)
            {
                _panel.SetActive(false);
                return;
            }

            var stage = ProgressionTracker.CurrentStage;
            if (stage == null || !IsInGame())
            {
                _panel.SetActive(false);
                return;
            }

            _stageLabel.text = stage.Label.ToUpper();

            foreach (Transform child in _contentRoot.transform)
                Destroy(child.gameObject);

            if (!_collapsed)
            {
                int shown = 0;
                int maxRows = Plugin.TrackerMaxRows.Value;

                var pins = ProgressSaver.PinnedRecipeIds;
                if (pins.Count > 0)
                {
                    AddSectionHeader("◆ PINNED");
                    foreach (string itemId in pins)
                    {
                        if (shown >= maxRows) break;
                        AddPinnedRow(itemId);
                        shown++;
                    }
                }

                var visible = (stage.Objectives ?? new List<Objective>())
                    .Where(o =>
                        (string.IsNullOrEmpty(o.ModRequired) ||
                         GuideDataLoader.InstalledMods.Contains(o.ModRequired)) &&
                        (string.IsNullOrEmpty(o.PlaystyleFilter) ||
                         (ProgressSaver.Current?.PlaystyleId ?? "all") == "all" ||
                         (ProgressSaver.Current?.PlaystyleId ?? "all") == o.PlaystyleFilter))
                    .ToList();

                if (visible.Count == 0 && pins.Count == 0)
                {
                    _panel.SetActive(false);
                    return;
                }

                var pending = visible.Where(o => !ProgressionTracker.IsObjectiveComplete(o)).ToList();
                var done = visible.Where(o => ProgressionTracker.IsObjectiveComplete(o)).ToList();

                bool hasObjectives = pending.Count > 0 || done.Count > 0;
                bool hasRoomForObjectives = shown < maxRows;

                if (pins.Count > 0 && hasObjectives && hasRoomForObjectives)
                {
                    GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
                    spacer.transform.SetParent(_contentRoot.transform, false);
                    spacer.GetComponent<LayoutElement>().preferredHeight = 6f;
                }

                if (hasObjectives && hasRoomForObjectives)
                    AddSectionHeader("OBJECTIVES");

                foreach (var obj in pending)
                {
                    if (shown >= maxRows) break;
                    AddRow(obj, false);
                    shown++;
                }
                foreach (var obj in done)
                {
                    if (shown >= maxRows) break;
                    AddRow(obj, true);
                    shown++;
                }
            }

            _panel.SetActive(true);
            UpdatePanelSize();
        }

        // ── Section header ────────────────────────────────────────────────────

        private void AddSectionHeader(string text)
        {
            var header = MakeText(_contentRoot.transform, "SectionHeader", text,
                Plugin.TrackerFontSize.Value - 2, FontStyle.Bold, new Color(1f, 0.75f, 0.3f));
            header.alignment = TextAnchor.MiddleLeft;
            header.gameObject.GetComponent<LayoutElement>().minHeight = 16f;
        }

        // ── Pinned row ────────────────────────────────────────────────────────

        private void AddPinnedRow(string itemId)
        {
            int baseFont = Plugin.TrackerFontSize.Value;

            // Check if the player has already manually ticked this item off
            bool isCompleted = ProgressSaver.IsChecked(itemId);

            Color labelColor = isCompleted
                ? new Color(0.5f, 0.5f, 0.5f)      // greyed out — already crafted
                : new Color(0.95f, 0.85f, 0.6f);    // amber — still needs crafting

            GameObject row = new GameObject("PinnedRow_" + itemId,
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_contentRoot.transform, false);
            row.GetComponent<LayoutElement>().minHeight = 18f;

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing             = 6;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth   = true;
            hlg.childControlHeight  = true;
            hlg.childAlignment      = TextAnchor.MiddleLeft;

            // ◆ icon — filled = pinned (greyed when done)
            var pinIcon = MakeText(row.transform, "PinIcon",
                isCompleted ? "✔" : "◆",
                baseFont, FontStyle.Normal,
                isCompleted ? new Color(0.4f, 0.8f, 0.4f) : new Color(1f, 0.75f, 0.3f),
                preferWidth: 16f);
            pinIcon.alignment = TextAnchor.MiddleCenter;

            string label   = ResolveItemLabel(itemId);
            string matProg = isCompleted ? "" : GetMaterialProgress(itemId);
            string full    = label.ToUpper() + matProg;

            var lbl = MakeText(row.transform, "Label", full,
                baseFont, FontStyle.Normal, labelColor, flexWidth: true);
            lbl.alignment          = TextAnchor.MiddleLeft;
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.verticalOverflow   = VerticalWrapMode.Truncate;
            lbl.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
        }

        // ── Make row ─────────────────────────────────────────────────────
        private GameObject MakeRowContainer(string name, float minHeight = 18f)
        {
            GameObject row = new GameObject(name,
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_contentRoot.transform, false);
            row.GetComponent<LayoutElement>().minHeight = minHeight;

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            return row;
        }

        // ── Make Wrapping Label ─────────────────────────────────────────────────────
        private Text MakeWrappingLabel(Transform parent, string name, string content,
            int fontSize, FontStyle style, Color color)
        {
            Text lbl = MakeText(parent, name, content,
                fontSize, style, color, flexWidth: true);
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.verticalOverflow = VerticalWrapMode.Truncate;
            lbl.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            return lbl;
        }

        // ── Add section header ─────────────────────────────────────────────────────
        private void AddHeaderRow(string text)
        {
            int baseFont = Plugin.TrackerFontSize.Value;

            GameObject row = MakeRowContainer("Header");

            MakeWrappingLabel(row.transform, "HeaderLabel", text,
                baseFont + 1, FontStyle.Bold,
                new Color(1f, 0.84f, 0.4f)); // gold-ish header color
        }
        // ── Add objective row ─────────────────────────────────────────────────────

        private void AddRow(Objective obj, bool done)
        {
            int baseFont = Plugin.TrackerFontSize.Value;

            GameObject row = MakeRowContainer("Row");

            var tick = MakeText(row.transform, "Tick",
                done ? "✔" : "▪",
                baseFont + 1, FontStyle.Normal,
                done ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.5f, 0.5f, 0.5f),
                preferWidth: 16f);
            tick.alignment = TextAnchor.MiddleCenter;

            string fullText = obj.Text;
            if (!done && !string.IsNullOrEmpty(obj.Value))
                fullText += GetMaterialProgress(obj);

            MakeWrappingLabel(row.transform, "Label", fullText,
                baseFont, FontStyle.Normal,
                done ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.9f, 0.9f, 0.9f));
        }

        // ── Panel sizing ──────────────────────────────────────────────────────

        private void UpdatePanelSize()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases(); // second pass after rebuild

            float contentH = _collapsed
                ? 0f
                : _contentRoot.GetComponent<RectTransform>().rect.height;

            float totalH = HeaderHeight + (_collapsed ? 0f : contentH + 4f);
            _panel.GetComponent<RectTransform>().sizeDelta =
                new Vector2(Plugin.TrackerWidth.Value, totalH);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsInGame() =>
            Player.m_localPlayer != null && ZNet.instance != null;

        // Thin wrapper: keeps all existing callers (AddRow) compiling unchanged.
        private string GetMaterialProgress(Objective obj) => GetMaterialProgress(obj.Value);

        private string GetMaterialProgress(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "";
            if (Player.m_localPlayer == null ||
                ObjectDB.instance    == null ||
                ZNetScene.instance   == null) return "";

            Piece.Requirement[] requirements = null;

            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemId);
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
                GameObject piecePrefab = ZNetScene.instance.GetPrefab(itemId);
                if (piecePrefab != null)
                {
                    var piece = piecePrefab.GetComponent<Piece>();
                    if (piece != null) requirements = piece.m_resources;
                }
            }

            if (requirements == null || requirements.Length == 0) return "";

            Inventory inv = Player.m_localPlayer.GetInventory();
            var reqStrings = new List<string>();

            foreach (var req in requirements)
            {
                if (req.m_resItem == null) continue;
                string matName = req.m_resItem.m_itemData.m_shared.m_name;
                string locName = global::Localization.instance.Localize(matName);
                int    have    = inv.CountItems(matName);
                int    need    = req.m_amount;
                string color   = have >= need ? "#80FF80" : "#FF8080";
                reqStrings.Add($"<color={color}>{have}/{need}</color> {locName}");
            }

            int smallFont = Plugin.TrackerFontSize.Value - 3;
            return reqStrings.Count == 0
                ? ""
                : $"\n<color=#B0B0B0><size={smallFont}>Requires: {string.Join(", ", reqStrings)}</size></color>";
        }

        /// <summary>
        /// Looks up a human-readable label for an itemId.
        /// Results are cached in _labelCache after the first lookup.
        /// Call InvalidateLabelCache() after loading new guide data.
        /// </summary>
        private string ResolveItemLabel(string itemId)
        {
            if (_labelCache.TryGetValue(itemId, out string cached))
                return cached;

            // Search stages
            foreach (Stage stage in GuideDataLoader.AllStages)
            {
                if (stage.Gear != null)
                {
                    var gear = stage.Gear.Find(g => g.ItemId == itemId);
                    if (gear != null) { _labelCache[itemId] = gear.Label; return gear.Label; }
                }
                if (stage.Recipes != null)
                {
                    var rec = stage.Recipes.Find(r => r.ItemId == itemId);
                    if (rec != null) { _labelCache[itemId] = rec.Label; return rec.Label; }
                }
            }

            // Fallback: ObjectDB localization
            GameObject prefab = ObjectDB.instance?.GetItemPrefab(itemId);
            if (prefab != null)
            {
                string locKey = prefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_name;
                if (!string.IsNullOrEmpty(locKey))
                {
                    string loc = global::Localization.instance?.Localize(locKey);
                    if (!string.IsNullOrEmpty(loc) && loc != locKey)
                    {
                        _labelCache[itemId] = loc;
                        return loc;
                    }
                }
            }

            _labelCache[itemId] = itemId; // cache the fallback too
            return itemId;
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
            t.text      = content;
            t.font      = GUIManager.Instance.AveriaSerifBold;
            t.fontSize  = size;
            t.fontStyle = style;
            t.color     = color;
            t.raycastTarget = false;

            var le = go.GetComponent<LayoutElement>();
            if (flexWidth)       le.flexibleWidth  = 1f;
            if (preferWidth > 0) le.preferredWidth = preferWidth;

            return t;
        }
    }
}