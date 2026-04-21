using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ValheimGuide.Data;
using Jotunn.Managers;

namespace ValheimGuide.UI
{
    /// <summary>
    /// Full-panel encyclopedia view. Renders inside the container passed to the
    /// constructor. Call Build() once after the container is shown for the first
    /// time, then Rebuild() if guide data is reloaded.
    /// </summary>
    internal class EncyclopediaView
    {
        private readonly GameObject _container;

        private string _searchQuery = "";
        private EncyclopediaCategory? _categoryFilter = null;

        private GameObject _resultsContent;
        private GameObject _detailContent;
        private List<Image> _filterButtonImages = new List<Image>();
        private bool _built = false;

        public EncyclopediaView(GameObject container)
        {
            _container = container;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Build()
        {
            if (_built) return;
            _built = true;

            foreach (Transform child in _container.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            _filterButtonImages.Clear();
            _searchQuery = "";
            _categoryFilter = null;

            BuildSearchBar();
            BuildFilterRow();
            BuildSplitArea();

            PopulateResults();
        }

        public void Rebuild()
        {
            _built = false;
            Build();
        }

        // ── Layout construction ───────────────────────────────────────────────

        private void BuildSearchBar()
        {
            // Dark search field at the top of the container
            GameObject bar = new GameObject("SearchBar",
                typeof(RectTransform), typeof(Image), typeof(InputField));
            bar.transform.SetParent(_container.transform, false);

            RectTransform rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(10, -44);
            rt.offsetMax = new Vector2(-10, -4);
            bar.GetComponent<Image>().color = new Color(0.07f, 0.07f, 0.07f, 1f);

            // Placeholder text
            GameObject phGo = MakeText(bar.transform, "Placeholder",
                "Search weapons, armor, mobs...", 14,
                new Color(0.38f, 0.38f, 0.38f));
            phGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            StretchRect(phGo, 10, 0, -10, 0);

            // Input text
            GameObject txGo = MakeText(bar.transform, "Text", "", 14, Color.white);
            txGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            StretchRect(txGo, 10, 0, -10, 0);

            InputField field = bar.GetComponent<InputField>();
            field.textComponent = txGo.GetComponent<Text>();
            field.placeholder = phGo.GetComponent<Text>();
            field.text = _searchQuery;
            field.onValueChanged.AddListener(v =>
            {
                _searchQuery = v.ToLower();
                PopulateResults();
            });
        }

        private void BuildFilterRow()
        {
            // Row of category toggle buttons below the search bar
            GameObject row = new GameObject("FilterRow",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(_container.transform, false);

            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(10, -82);
            rt.offsetMax = new Vector2(-10, -50);

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            var filters = new (string label, EncyclopediaCategory? cat)[]
            {
                ("ALL",     null),
                ("WEAPONS", EncyclopediaCategory.Weapon),
                ("ARMOR",   EncyclopediaCategory.Armor),
                ("SHIELDS", EncyclopediaCategory.Shield),
                ("MOBS",    EncyclopediaCategory.Mob),
            };

            for (int i = 0; i < filters.Length; i++)
            {
                var (label, cat) = filters[i];
                var capturedCat = cat;
                int capturedIdx = i;

                GameObject btn = new GameObject("Filter_" + label,
                    typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                btn.transform.SetParent(row.transform, false);
                btn.GetComponent<LayoutElement>().preferredWidth = 95;

                bool active = _categoryFilter == cat;
                Image img = btn.GetComponent<Image>();
                img.color = active ? new Color(0.35f, 0.28f, 0.15f) : new Color(0.2f, 0.2f, 0.2f);
                _filterButtonImages.Add(img);

                GameObject lbl = MakeText(btn.transform, "Lbl", label, 13, Color.white);
                lbl.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                StretchRect(lbl, 0, 0, 0, 0);

                btn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _categoryFilter = capturedCat;
                    for (int j = 0; j < _filterButtonImages.Count; j++)
                        _filterButtonImages[j].color = j == capturedIdx
                            ? new Color(0.35f, 0.28f, 0.15f)
                            : new Color(0.2f, 0.2f, 0.2f);
                    PopulateResults();
                });
            }
        }

        private void BuildSplitArea()
        {
            // Left 38% — scrollable results list
            GameObject resultsArea = new GameObject("ResultsArea",
                typeof(RectTransform), typeof(ScrollRect));
            resultsArea.transform.SetParent(_container.transform, false);

            RectTransform raRect = resultsArea.GetComponent<RectTransform>();
            raRect.anchorMin = new Vector2(0f, 0f);
            raRect.anchorMax = new Vector2(0.38f, 1f);
            raRect.offsetMin = new Vector2(10, 10);
            raRect.offsetMax = new Vector2(-3, -88);
            resultsArea.AddComponent<SmoothScroll>();

            Scrollbar rSb = GuidePanel.CreateScrollbar(resultsArea.transform);
            ScrollRect rScroll = resultsArea.GetComponent<ScrollRect>();
            rScroll.horizontal = false;
            rScroll.vertical = true;
            rScroll.movementType = ScrollRect.MovementType.Clamped;
            rScroll.verticalScrollbar = rSb;
            rScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            rScroll.viewport = BuildViewport(resultsArea.transform);

            _resultsContent = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _resultsContent.transform.SetParent(rScroll.viewport.transform, false);

            RectTransform rcRect = _resultsContent.GetComponent<RectTransform>();
            rcRect.anchorMin = new Vector2(0, 1);
            rcRect.anchorMax = new Vector2(1, 1);
            rcRect.pivot = new Vector2(0.5f, 1f);
            rcRect.offsetMin = rcRect.offsetMax = Vector2.zero;
            rScroll.content = rcRect;

            ConfigureContentVLG(_resultsContent, 3);

            // Right 62% — detail pane
            GameObject detailArea = new GameObject("DetailArea",
                typeof(RectTransform), typeof(ScrollRect));
            detailArea.transform.SetParent(_container.transform, false);

            RectTransform daRect = detailArea.GetComponent<RectTransform>();
            daRect.anchorMin = new Vector2(0.38f, 0f);
            daRect.anchorMax = new Vector2(1f, 1f);
            daRect.offsetMin = new Vector2(3, 10);
            daRect.offsetMax = new Vector2(-10, -88);
            detailArea.AddComponent<SmoothScroll>();

            Scrollbar dSb = GuidePanel.CreateScrollbar(detailArea.transform);
            ScrollRect dScroll = detailArea.GetComponent<ScrollRect>();
            dScroll.horizontal = false;
            dScroll.vertical = true;
            dScroll.movementType = ScrollRect.MovementType.Clamped;
            dScroll.verticalScrollbar = dSb;
            dScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            dScroll.viewport = BuildViewport(detailArea.transform);

            _detailContent = new GameObject("DetailContent",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _detailContent.transform.SetParent(dScroll.viewport.transform, false);

            RectTransform dcRect = _detailContent.GetComponent<RectTransform>();
            dcRect.anchorMin = new Vector2(0, 1);
            dcRect.anchorMax = new Vector2(1, 1);
            dcRect.pivot = new Vector2(0.5f, 1f);
            dcRect.offsetMin = dcRect.offsetMax = Vector2.zero;
            dScroll.content = dcRect;

            ConfigureContentVLG(_detailContent, 8, new RectOffset(14, 14, 12, 12));
        }

        // ── Populate / refresh ────────────────────────────────────────────────

        private void PopulateResults()
        {
            if (_resultsContent == null) return;

            foreach (Transform child in _resultsContent.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            string q = _searchQuery;
            var results = EncyclopediaIndex.Entries
                .Where(e =>
                {
                    if (_categoryFilter != null && e.Category != _categoryFilter.Value)
                        return false;
                    if (!string.IsNullOrEmpty(q))
                    {
                        bool hit = e.Label.ToLower().Contains(q)
                                || (e.Biome != null && e.Biome.ToLower().Contains(q))
                                || e.Id.ToLower().Contains(q);
                        if (!hit) return false;
                    }
                    return true;
                })
                .ToList();

            if (results.Count == 0)
            {
                AddDetail("No results.", 14, FontStyle.Italic, new Color(0.5f, 0.5f, 0.5f));
                return;
            }

            // Render result rows
            EncyclopediaEntry firstEntry = results[0];
            foreach (var entry in results)
            {
                var captured = entry;

                GameObject row = new GameObject("Row_" + entry.Id,
                    typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                row.transform.SetParent(_resultsContent.transform, false);
                row.GetComponent<LayoutElement>().preferredHeight = 40;
                row.GetComponent<Image>().color = new Color(0.17f, 0.17f, 0.17f);

                // Vertical stack inside the row
                GameObject stack = new GameObject("Stack",
                    typeof(RectTransform), typeof(VerticalLayoutGroup));
                stack.transform.SetParent(row.transform, false);
                StretchRect(stack, 8, 3, -8, -3);
                var vl = stack.GetComponent<VerticalLayoutGroup>();
                vl.childForceExpandWidth = true;
                vl.childForceExpandHeight = false;
                vl.childControlWidth = true;
                vl.childControlHeight = true;
                vl.spacing = 1;

                // Label (coloured by category)
                MakeText(stack.transform, "Name", entry.Label, 14,
                    CategoryColour(entry.Category)).GetComponent<Text>().fontStyle = FontStyle.Bold;

                // Sub-label: icon + biome + mod badge
                string sub = CategoryIcon(entry.Category) + "  " + (entry.Biome ?? "");
                if (!string.IsNullOrEmpty(entry.ModRequired)) sub += "  [Mod]";
                MakeText(stack.transform, "Sub", sub, 11, new Color(0.5f, 0.5f, 0.5f));

                Image rowImg = row.GetComponent<Image>();
                row.GetComponent<Button>().onClick.AddListener(() =>
                {
                    // Reset all row backgrounds then highlight selected
                    foreach (Transform t in _resultsContent.transform)
                    {
                        var img = t.GetComponent<Image>();
                        if (img != null) img.color = new Color(0.17f, 0.17f, 0.17f);
                    }
                    rowImg.color = new Color(0.30f, 0.24f, 0.11f);
                    ShowDetail(captured);
                });
            }

            // Auto-select first result
            if (_resultsContent.transform.childCount > 0)
            {
                _resultsContent.transform.GetChild(0)
                    .GetComponent<Image>().color = new Color(0.30f, 0.24f, 0.11f);
            }
            ShowDetail(firstEntry);
        }

        private void ShowDetail(EncyclopediaEntry entry)
        {
            if (_detailContent == null || entry == null) return;

            foreach (Transform child in _detailContent.transform)
                UnityEngine.Object.Destroy(child.gameObject);

            Color sectionClr = new Color(1f, 0.75f, 0.3f);

            // ── Title ───────────────────────────────────────────────────────
            AddDetail(entry.Label.ToUpper(), 20, FontStyle.Bold, CategoryColour(entry.Category));

            string tag = $"{entry.Category}";
            if (!string.IsNullOrEmpty(entry.Biome)) tag += $"  ·  {entry.Biome}";
            if (!string.IsNullOrEmpty(entry.ModRequired)) tag += $"  ·  {entry.ModRequired}";
            AddDetail(tag, 13, FontStyle.Italic, new Color(0.5f, 0.5f, 0.5f));

            AddSpc(8);

            // ── GUIDE GEAR ──────────────────────────────────────────────────
            if (entry.GuideGear != null)
            {
                GearEntry g = entry.GuideGear;

                if (!string.IsNullOrEmpty(g.Station))
                {
                    string st = g.Station;
                    if (g.StationLevel > 0) st += $" (level {g.StationLevel})";
                    AddDetail("Station: " + st, 14, FontStyle.Normal, Color.white);
                }

                if (g.DamageTypes != null && g.DamageTypes.Count > 0)
                    AddDetail("Damage: " + string.Join(", ", g.DamageTypes),
                        14, FontStyle.Normal, new Color(1f, 0.6f, 0.5f));

                if (!string.IsNullOrEmpty(g.ArmorClass))
                    AddDetail("Armor class: " + g.ArmorClass,
                        14, FontStyle.Normal, new Color(0.6f, 0.8f, 1f));

                if (g.Recipe != null && g.Recipe.Count > 0)
                {
                    AddSpc(6);
                    AddDetail("RECIPE", 14, FontStyle.Bold, sectionClr);
                    foreach (var mat in g.Recipe)
                        AddDetail($"  {mat.Amount}×  {mat.Label}",
                            13, FontStyle.Normal, new Color(0.85f, 0.85f, 0.85f));
                }

                if (g.PlaystyleTag != null)
                {
                    AddSpc(4);
                    AddDetail("Playstyle: " + g.PlaystyleTag, 12, FontStyle.Italic,
                        new Color(0.8f, 0.7f, 1f));
                }
            }

            // ── GUIDE MOB ───────────────────────────────────────────────────
            else if (entry.GuideMob != null)
            {
                MobEntry m = entry.GuideMob;

                if (m.Health > 0)
                    AddDetail($"Health: {m.Health} HP", 14, FontStyle.Normal,
                        new Color(0.7f, 1f, 0.55f));

                if (m.SpawnChanceDay > 0 || m.SpawnChanceNight > 0)
                    AddDetail(
                        $"Spawn: Day {(int)(m.SpawnChanceDay * 100)}%  ·  " +
                        $"Night {(int)(m.SpawnChanceNight * 100)}%",
                        13, FontStyle.Normal, new Color(0.6f, 0.6f, 0.6f));

                if (m.Resistances != null && m.Resistances.Count > 0)
                {
                    var weak = m.Resistances.Where(r => r.Value == "Weak").Select(r => r.Key).ToList();
                    var immune = m.Resistances.Where(r => r.Value == "Immune").Select(r => r.Key).ToList();
                    var resist = m.Resistances.Where(r => r.Value == "Resistant").Select(r => r.Key).ToList();

                    if (weak.Count > 0 || immune.Count > 0 || resist.Count > 0)
                    {
                        AddSpc(6);
                        AddDetail("RESISTANCES", 14, FontStyle.Bold, sectionClr);
                        if (weak.Count > 0) AddDetail("Weak: " + string.Join(", ", weak),
                            13, FontStyle.Normal, new Color(1f, 0.5f, 0.5f));
                        if (immune.Count > 0) AddDetail("Immune: " + string.Join(", ", immune),
                            13, FontStyle.Normal, new Color(0.5f, 0.8f, 1f));
                        if (resist.Count > 0) AddDetail("Resistant: " + string.Join(", ", resist),
                            13, FontStyle.Normal, new Color(0.7f, 0.9f, 0.7f));
                    }
                }

                if (m.Drops != null && m.Drops.Count > 0)
                {
                    AddSpc(6);
                    AddDetail("DROPS", 14, FontStyle.Bold, sectionClr);
                    foreach (var d in m.Drops)
                    {
                        string line = $"  {d.Label}  —  {(int)(d.Chance * 100)}%";
                        if (d.Max > 1) line += $"  ({d.Min}–{d.Max})";
                        AddDetail(line, 13, FontStyle.Normal, Color.white);
                    }
                }

                if (m.IsTameable && m.Taming != null)
                {
                    AddSpc(6);
                    AddDetail("TAMING", 14, FontStyle.Bold, new Color(0.65f, 1f, 0.45f));
                    AddDetail("Food: " + string.Join(", ", m.Taming.FoodItems),
                        13, FontStyle.Normal, Color.white);
                    if (!string.IsNullOrEmpty(m.Taming.Note))
                        AddDetail(m.Taming.Note, 13, FontStyle.Italic,
                            new Color(0.7f, 0.9f, 0.5f));
                }

                if (!string.IsNullOrEmpty(m.Note))
                {
                    AddSpc(4);
                    AddDetail(m.Note, 13, FontStyle.Italic, new Color(1f, 0.85f, 0.4f));
                }
            }

            // ── LIVE ObjectDB ENTRY (not in guide data) ─────────────────────
            else if (entry.LiveShared != null)
            {
                var s = entry.LiveShared;

                // Damage breakdown
                var dmgParts = new List<string>();
                if (s.m_damages.m_blunt > 0) dmgParts.Add($"Blunt {s.m_damages.m_blunt}");
                if (s.m_damages.m_slash > 0) dmgParts.Add($"Slash {s.m_damages.m_slash}");
                if (s.m_damages.m_pierce > 0) dmgParts.Add($"Pierce {s.m_damages.m_pierce}");
                if (s.m_damages.m_fire > 0) dmgParts.Add($"Fire {s.m_damages.m_fire}");
                if (s.m_damages.m_frost > 0) dmgParts.Add($"Frost {s.m_damages.m_frost}");
                if (s.m_damages.m_lightning > 0) dmgParts.Add($"Lightning {s.m_damages.m_lightning}");
                if (s.m_damages.m_poison > 0) dmgParts.Add($"Poison {s.m_damages.m_poison}");
                if (s.m_damages.m_spirit > 0) dmgParts.Add($"Spirit {s.m_damages.m_spirit}");
                if (dmgParts.Count > 0)
                    AddDetail("Damage: " + string.Join(", ", dmgParts),
                        14, FontStyle.Normal, new Color(1f, 0.6f, 0.5f));

                if (s.m_armor > 0)
                    AddDetail($"Armor: {s.m_armor}", 14, FontStyle.Normal,
                        new Color(0.6f, 0.8f, 1f));

                if (s.m_blockPower > 0)
                    AddDetail($"Block: {s.m_blockPower}", 14, FontStyle.Normal,
                        new Color(0.6f, 0.9f, 0.6f));

                if (s.m_maxDurability > 0)
                    AddDetail($"Durability: {s.m_maxDurability}", 14, FontStyle.Normal,
                        new Color(0.75f, 0.75f, 0.75f));

                // Try to pull its recipe live from ObjectDB
                AddSpc(6);
                GameObject prefab = ObjectDB.instance?.GetItemPrefab(entry.Id);
                if (prefab != null)
                {
                    ItemDrop drop = prefab.GetComponent<ItemDrop>();
                    if (drop != null)
                    {
                        Recipe recipe = ObjectDB.instance.GetRecipe(drop.m_itemData);
                        if (recipe != null && recipe.m_resources != null
                            && recipe.m_resources.Length > 0)
                        {
                            AddDetail("RECIPE", 14, FontStyle.Bold, sectionClr);
                            if (recipe.m_craftingStation != null)
                                AddDetail("Station: " + recipe.m_craftingStation.name,
                                    13, FontStyle.Normal, Color.white);

                            foreach (var req in recipe.m_resources)
                            {
                                if (req.m_resItem == null) continue;
                                string matKey = req.m_resItem.m_itemData.m_shared.m_name;
                                string matName = TryLoc(matKey);
                                AddDetail($"  {req.m_amount}×  {matName}",
                                    13, FontStyle.Normal, new Color(0.85f, 0.85f, 0.85f));
                            }
                        }
                        else
                        {
                            AddDetail("No recipe found in ObjectDB.", 13, FontStyle.Italic,
                                new Color(0.45f, 0.45f, 0.45f));
                        }
                    }
                }

                AddSpc(6);
                AddDetail("Not in guide data — stats read live from game.",
                    11, FontStyle.Italic, new Color(0.35f, 0.35f, 0.35f));
            }
        }

        // ── Detail helpers ────────────────────────────────────────────────────

        private void AddDetail(string text, int fontSize, FontStyle style, Color color)
        {
            if (_detailContent == null) return;

            GameObject go = new GameObject("Lbl",
                typeof(RectTransform), typeof(Text),
                typeof(LayoutElement), typeof(ContentSizeFitter));
            go.transform.SetParent(_detailContent.transform, false);

            Text t = go.GetComponent<Text>();
            t.text = text;
            t.font = GUIManager.Instance.AveriaSerifBold;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            go.GetComponent<LayoutElement>().flexibleWidth = 1f;
            go.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
        }

        private void AddSpc(float h)
        {
            if (_detailContent == null) return;
            GameObject go = new GameObject("Spc",
                typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(_detailContent.transform, false);
            go.GetComponent<LayoutElement>().preferredHeight = h;
        }

        // ── Layout utilities ──────────────────────────────────────────────────

        private static RectTransform BuildViewport(Transform parent)
        {
            GameObject vp = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            vp.transform.SetParent(parent, false);
            vp.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            vp.GetComponent<Mask>().showMaskGraphic = false;

            RectTransform rt = vp.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void ConfigureContentVLG(GameObject go, int spacing,
            RectOffset padding = null)
        {
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.padding = padding ?? new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            go.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
        }

        private static GameObject MakeText(Transform parent, string name, string txt,
            int fontSize, Color color)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text t = go.GetComponent<Text>();
            t.text = txt;
            t.font = GUIManager.Instance.AveriaSerifBold;
            t.fontSize = fontSize;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return go;
        }

        private static void StretchRect(GameObject go,
            float l, float b, float r, float t)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(r, t);
        }

        private static string TryLoc(string key)
        {
            if (Localization.instance == null || string.IsNullOrEmpty(key)) return key;
            try { return Localization.instance.Localize(key); }
            catch { return key; }
        }

        // ── Category helpers ──────────────────────────────────────────────────

        private static Color CategoryColour(EncyclopediaCategory cat)
        {
            switch (cat)
            {
                case EncyclopediaCategory.Weapon: return new Color(1.0f, 0.70f, 0.45f);
                case EncyclopediaCategory.Armor: return new Color(0.50f, 0.80f, 1.0f);
                case EncyclopediaCategory.Shield: return new Color(0.55f, 1.0f, 0.55f);
                case EncyclopediaCategory.Mob: return new Color(1.0f, 0.50f, 0.50f);
                default: return Color.white;
            }
        }

        private static string CategoryIcon(EncyclopediaCategory cat)
        {
            switch (cat)
            {
                case EncyclopediaCategory.Weapon: return "⚔";
                case EncyclopediaCategory.Armor: return "◉";
                case EncyclopediaCategory.Shield: return "◈";
                case EncyclopediaCategory.Mob: return "☠";
                default: return "•";
            }
        }
    }
}