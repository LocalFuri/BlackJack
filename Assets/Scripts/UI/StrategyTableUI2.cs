using UnityEngine;
using UnityEngine.UI;
// ReSharper disable Unity.PerformanceAnalysis

namespace Blackjack.UI
{
    /// <summary>
    /// Builds the Blackjack Basic Strategy reference table overlay at runtime.
    /// Grid cells are placed using explicit RectTransform positions to avoid
    /// nested LayoutGroup sizing conflicts.
    /// Call HighlightRecommendation to highlight the recommended cell for the current hand,
    /// and ClearHighlight when the round ends.
    /// </summary>
    public class StrategyTableUI : MonoBehaviour
    {
        // ── Action enum (internal display only) ───────────────────────────────────
        private enum Act { H, S, D, P, Ph, Pd, Ds }

        // ── Colors ────────────────────────────────────────────────────────────────
        private static readonly Color ColHit       = new Color(0.91f, 0.49f, 0.49f, 1f);
        private static readonly Color ColStand     = new Color(0.56f, 0.81f, 0.58f, 1f);
        private static readonly Color ColDouble    = new Color(0.53f, 0.71f, 0.90f, 1f);
        private static readonly Color ColSplit     = new Color(0.98f, 0.82f, 0.47f, 1f);
        private static readonly Color ColDs        = new Color(0.42f, 0.79f, 0.76f, 1f);
        private static readonly Color ColHeader    = new Color(0.72f, 0.72f, 0.72f, 1f);
        private static readonly Color ColBorder    = new Color(0.50f, 0.50f, 0.50f, 1f);
        private static readonly Color ColPanel     = new Color(0.13f, 0.38f, 0.18f, 1f);
        private static readonly Color ColCellText  = new Color(0.08f, 0.08f, 0.08f, 1f);
        private static readonly Color ColUIText    = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color ColLegend    = new Color(0.80f, 0.95f, 0.80f, 1f);

        // ── Layout ────────────────────────────────────────────────────────────────
        private const float FirstColW = 52f;
        private const float DataColW  = 24f;
        private const float RowH      = 18f;
        private const float Border    = 1f;
        private const int   FontCell  = 9;
        private const int   FontHead  = 11;
        private const int   FontTitle = 12;

        private static float TableW => Border * 12f + FirstColW + DataColW * 10f;

        private static float GetTableH(int rowCount) => Border * (rowCount + 1) + RowH * rowCount;

        // ── Strategy table data ───────────────────────────────────────────────────
        // Dealer up-card columns: 2 3 4 5 6 7 8 9 10 A
        private static readonly (string label, Act[] cols)[] HardRows =
        {
            ("4-7",   new[]{ Act.H, Act.H, Act.H, Act.H, Act.H, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("8",     new[]{ Act.H, Act.H, Act.H, Act.D, Act.D, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("9",     new[]{ Act.D, Act.D, Act.D, Act.D, Act.D, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("10",    new[]{ Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.H, Act.H }),
            ("11",    new[]{ Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D }),
            ("12",    new[]{ Act.H, Act.H, Act.S, Act.S, Act.S, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("13-16", new[]{ Act.S, Act.S, Act.S, Act.S, Act.S, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("17+",   new[]{ Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S }),
        };

        private static readonly (string label, Act[] cols)[] SoftRows =
        {
            ("A,2-A,5", new[]{ Act.H, Act.H,  Act.D,  Act.D,  Act.D,  Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("A,6",     new[]{ Act.D, Act.D,  Act.D,  Act.D,  Act.D,  Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("A,7",     new[]{ Act.S, Act.Ds, Act.Ds, Act.Ds, Act.Ds, Act.S, Act.S, Act.H, Act.H, Act.S }),
            ("A,8",     new[]{ Act.S, Act.S,  Act.S,  Act.S,  Act.Ds, Act.S, Act.S, Act.S, Act.S, Act.S }),
            ("A,9",     new[]{ Act.S, Act.S,  Act.S,  Act.S,  Act.S,  Act.S, Act.S, Act.S, Act.S, Act.S }),
        };

        private static readonly (string label, Act[] cols)[] PairRows =
        {
            ("2,2",   new[]{ Act.Ph, Act.P,  Act.P,  Act.P,  Act.P,  Act.P,  Act.H,  Act.H, Act.H, Act.H }),
            ("3,3",   new[]{ Act.Ph, Act.Ph, Act.P,  Act.P,  Act.P,  Act.P,  Act.H,  Act.H, Act.H, Act.H }),
            ("4,4",   new[]{ Act.H,  Act.H,  Act.Ph, Act.Pd, Act.Pd, Act.H,  Act.H,  Act.H, Act.H, Act.H }),
            ("5,5",   new[]{ Act.D,  Act.D,  Act.D,  Act.D,  Act.D,  Act.D,  Act.D,  Act.D, Act.H, Act.H }),
            ("6,6",   new[]{ Act.Ph, Act.P,  Act.P,  Act.P,  Act.P,  Act.H,  Act.H,  Act.H, Act.H, Act.H }),
            ("7,7",   new[]{ Act.P,  Act.P,  Act.P,  Act.P,  Act.P,  Act.P,  Act.Ph, Act.H, Act.H, Act.H }),
            ("8,8",   new[]{ Act.P,  Act.P,  Act.P,  Act.P,  Act.P,  Act.P,  Act.P,  Act.P, Act.P, Act.P }),
            ("9,9",   new[]{ Act.S,  Act.P,  Act.P,  Act.P,  Act.P,  Act.S,  Act.P,  Act.P, Act.S, Act.S }),
            ("10,10", new[]{ Act.S,  Act.S,  Act.S,  Act.S,  Act.S,  Act.S,  Act.S,  Act.S, Act.S, Act.S }),
            ("A,A",   new[]{ Act.P,  Act.P,  Act.P,  Act.P,  Act.P,  Act.P,  Act.P,  Act.P, Act.P, Act.P }),
        };

        // ── Cell registry for highlighting ────────────────────────────────────────
        // [dataRow, dealerCol] — data cells only (no header row, no label column)
        private GameObject[,] _hardCells;
        private GameObject[,] _softCells;
        private GameObject[,] _pairCells;
        private GameObject    _currentHighlight;
        private bool          _built;

        // ── Lifecycle ──────────────────────────────────────────────────────────────
        private void Awake() => EnsureBuilt();

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            BuildUI();
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>Shows or hides the strategy table overlay.</summary>
        public void SetVisible(bool visible)
        {
            EnsureBuilt();
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Highlights the strategy cell that corresponds to the current hand.
        /// Uses the same lookup logic as BasicStrategyTable.GetRecommendation.
        /// </summary>
        public void HighlightRecommendation(Hand hand, CardData dealerUpcard, bool canSplit, bool canDouble, bool canSurrender)
        {
            ClearHighlight();
            int col = DealerCol(dealerUpcard);
            GameObject cell = FindRecommendedCell(hand, dealerUpcard, canSplit, col);
            if (cell != null)
                ShowHighlight(cell);
        }

        /// <summary>Removes the current highlighted cell overlay.</summary>
        public void ClearHighlight()
        {
            if (_currentHighlight == null) return;
            Transform overlay = _currentHighlight.transform.Find("HL");
            if (overlay != null) Destroy(overlay.gameObject);
            _currentHighlight = null;
        }

        // ── Highlight helpers ─────────────────────────────────────────────────────

        private GameObject FindRecommendedCell(Hand hand, CardData dealerUpcard, bool canSplit, int col)
        {
            // Pair check
            if (canSplit && hand.Count == 2)
            {
                int v0 = hand.Cards[0].BlackjackValue;
                int v1 = hand.Cards[1].BlackjackValue;
                bool isPair = v0 == v1 || (v0 >= 10 && v1 >= 10);
                if (isPair)
                {
                    int key = (v0 >= 10 && v0 != 11) ? 10 : Mathf.Min(v0, 11);
                    int row = PairRowIndex(key);
                    if (_pairCells != null && row >= 0 && row < _pairCells.GetLength(0) && col < _pairCells.GetLength(1))
                        return _pairCells[row, col];
                }
            }

            // Soft check
            if (hand.IsSoft())
            {
                int row = Mathf.Clamp(hand.BestValue(), 13, 21) - 13;
                if (_softCells != null && row < _softCells.GetLength(0) && col < _softCells.GetLength(1))
                    return _softCells[row, col];
            }

            // Hard
            int hardRow = HardRowIndex(hand.BestValue());
            if (_hardCells != null && hardRow >= 0 && hardRow < _hardCells.GetLength(0) && col < _hardCells.GetLength(1))
                return _hardCells[hardRow, col];

            return null;
        }

        private void ShowHighlight(GameObject cell)
        {
            _currentHighlight = cell;

            // White border ring that sits behind the text label
            var hlGO = new GameObject("HL", typeof(RectTransform));
            hlGO.transform.SetParent(cell.transform, false);
            hlGO.transform.SetSiblingIndex(0);

            var rt = hlGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-3f, -3f);
            rt.offsetMax = new Vector2(3f, 3f);
            hlGO.AddComponent<Image>().color = Color.white;

            // Semi-transparent white tint over the cell interior
            var innerGO = new GameObject("HLInner", typeof(RectTransform));
            innerGO.transform.SetParent(hlGO.transform, false);
            var irt = innerGO.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(3f, 3f);
            irt.offsetMax = new Vector2(-3f, -3f);
            innerGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.40f);
        }

        // ── Row index mappers (mirrors BasicStrategyTable) ────────────────────────

        private static int HardRowIndex(int total)
        {
            if (total <= 7)  return 0; // 4-7
            if (total == 8)  return 1;
            if (total == 9)  return 2;
            if (total == 10) return 3;
            if (total == 11) return 4;
            if (total == 12) return 5;
            if (total <= 16) return 6; // 13-16
            return 7;                  // 17+
        }

        private static int PairRowIndex(int pairKey)
        {
            switch (pairKey)
            {
                case 2:  return 0;
                case 3:  return 1;
                case 4:  return 2;
                case 5:  return 3;
                case 6:  return 4;
                case 7:  return 5;
                case 8:  return 6;
                case 9:  return 7;
                case 10: return 8;
                case 11: return 9; // Aces
                default: return -1;
            }
        }

        private static int DealerCol(CardData card)
        {
            switch (card.Rank)
            {
                case Rank.Two:   return 0;
                case Rank.Three: return 1;
                case Rank.Four:  return 2;
                case Rank.Five:  return 3;
                case Rank.Six:   return 4;
                case Rank.Seven: return 5;
                case Rank.Eight: return 6;
                case Rank.Nine:  return 7;
                case Rank.Ten:
                case Rank.Jack:
                case Rank.Queen:
                case Rank.King:  return 8;
                case Rank.Ace:   return 9;
                default:         return 8;
            }
        }

        // ── UI construction ────────────────────────────────────────────────────────
        private void BuildUI()
        {
            gameObject.AddComponent<Image>().color = ColPanel;

            var vpGO = MakeChild("Viewport", gameObject);
            Stretch(vpGO.GetComponent<RectTransform>());
            vpGO.AddComponent<Image>().color = Color.clear;
            vpGO.AddComponent<Mask>().showMaskGraphic = false;

            var contentGO = MakeChild("Content", vpGO);
            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding                = new RectOffset(8, 8, 8, 8);
            vlg.spacing                = 5f;
            vlg.childAlignment         = TextAnchor.UpperLeft;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = vpGO.AddComponent<ScrollRect>();
            scroll.content          = contentRt;
            scroll.viewport         = vpGO.GetComponent<RectTransform>();
            scroll.horizontal       = false;
            scroll.vertical         = true;
            scroll.scrollSensitivity = 40f;
            scroll.movementType     = ScrollRect.MovementType.Clamped;
            scroll.inertia          = true;
            scroll.decelerationRate = 0.15f;

            AddTextRow(contentGO, "Blackjack Basic Strategy (Single Deck, S17)", FontTitle, FontStyle.Bold, ColUIText, 18f);
            AddTextRow(contentGO, "Color legend:  Hit=Red  Stand=Green  Double=Blue  Split=Yellow", 9, FontStyle.Normal, ColLegend, 14f);

            AddSectionTitle(contentGO, "Hard Totals");
            _hardCells = BuildTable(contentGO, "Total", HardRows);

            AddSectionTitle(contentGO, "Soft Totals");
            _softCells = BuildTable(contentGO, "Hand", SoftRows);

            AddSectionTitle(contentGO, "Pairs");
            _pairCells = BuildTable(contentGO, "Pair", PairRows);
        }

        private GameObject[,] BuildTable(GameObject parent, string firstHeader, (string label, Act[] cols)[] rows)
        {
            int dataRowCount = rows.Length;
            int totalRows    = dataRowCount + 1; // +1 for the column header row
            float h = GetTableH(totalRows);
            float w = TableW;

            var tableGO = MakeChild("Table", parent);
            var le = tableGO.AddComponent<LayoutElement>();
            le.preferredWidth  = w;
            le.preferredHeight = h;
            le.flexibleWidth   = 0f;
            le.flexibleHeight  = 0f;
            tableGO.AddComponent<Image>().color = ColBorder;

            // Column header row — not stored in cell registry
            string[] hdrLabels = { firstHeader, "2", "3", "4", "5", "6", "7", "8", "9", "10", "A" };
            Color[]  hdrBg     = new Color[11];
            for (int i = 0; i < 11; i++) hdrBg[i] = ColHeader;
            GameObject[,] noRegistry = null;
            BuildTableRow(tableGO, hdrLabels, hdrBg, 0, h, FontStyle.Bold, noRegistry, 0);

            // Data rows
            var cells = new GameObject[dataRowCount, 10];
            for (int r = 0; r < dataRowCount; r++)
            {
                var (lbl, acts) = rows[r];
                var labels = new string[11];
                var bgs    = new Color[11];
                labels[0] = lbl;
                bgs[0]    = ColHeader;
                for (int c = 0; c < acts.Length; c++)
                {
                    labels[c + 1] = ActLabel(acts[c]);
                    bgs[c + 1]    = ActColor(acts[c]);
                }
                BuildTableRow(tableGO, labels, bgs, r + 1, h, FontStyle.Normal, cells, r);
            }
            return cells;
        }

        // rowIdx: position in the table (0 = column header, 1+ = data)
        // registry: pass null for the header row to skip cell registration.
        private void BuildTableRow(GameObject table, string[] labels, Color[] bgs,
                                   int rowIdx, float tableH, FontStyle style,
                                   GameObject[,] registry, int registryRow)
        {
            float yTop  = tableH - Border - rowIdx * (RowH + Border);
            float yBot  = yTop - RowH;
            float xLeft = Border;

            for (int i = 0; i < labels.Length; i++)
            {
                float cellW = (i == 0) ? FirstColW : DataColW;
                var cell = BuildCell(table, labels[i], bgs[i], xLeft, xLeft + cellW, yBot, yTop, style);
                if (registry != null && i >= 1)
                    registry[registryRow, i - 1] = cell;
                xLeft += cellW + Border;
            }
        }

        private GameObject BuildCell(GameObject parent, string text, Color bg,
                                     float left, float right, float bottom, float top, FontStyle style)
        {
            var cellGO = MakeChild("Cell", parent);
            var rt = cellGO.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.zero;
            rt.pivot            = Vector2.zero;
            rt.anchoredPosition = new Vector2(left, bottom);
            rt.sizeDelta        = new Vector2(right - left, top - bottom);
            cellGO.AddComponent<Image>().color = bg;

            var lblGO = MakeChild("Txt", cellGO);
            Stretch(lblGO.GetComponent<RectTransform>());
            var txt = lblGO.AddComponent<Text>();
            txt.text               = text;
            txt.fontSize           = FontCell;
            txt.fontStyle          = style;
            txt.alignment          = TextAnchor.MiddleCenter;
            txt.color              = ColCellText;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            txt.raycastTarget      = false;

            return cellGO;
        }

        private void AddTextRow(GameObject parent, string text, int fontSize, FontStyle style, Color color, float height)
        {
            var go = MakeChild("Txt", parent);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var txt = go.AddComponent<Text>();
            txt.text               = text;
            txt.fontSize           = fontSize;
            txt.fontStyle          = style;
            txt.color              = color;
            txt.alignment          = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            txt.raycastTarget      = false;
        }

        private void AddSectionTitle(GameObject parent, string text)
        {
            var go = MakeChild("Sec", parent);
            go.AddComponent<LayoutElement>().preferredHeight = 20f;
            var txt = go.AddComponent<Text>();
            txt.text               = text;
            txt.fontSize           = FontHead;
            txt.fontStyle          = FontStyle.Bold;
            txt.color              = ColUIText;
            txt.alignment          = TextAnchor.LowerLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            txt.raycastTarget      = false;
        }

        // ── Enum helpers ──────────────────────────────────────────────────────────
        private static string ActLabel(Act a)
        {
            switch (a)
            {
                case Act.H:  return "H";
                case Act.S:  return "S";
                case Act.D:  return "D";
                case Act.P:  return "P";
                case Act.Ph: return "Ph";
                case Act.Pd: return "Pd";
                case Act.Ds: return "Ds";
                default:     return "?";
            }
        }

        private static Color ActColor(Act a)
        {
            switch (a)
            {
                case Act.H:  return ColHit;
                case Act.S:  return ColStand;
                case Act.D:  return ColDouble;
                case Act.P:  return ColSplit;
                case Act.Ph: return ColSplit;
                case Act.Pd: return ColDouble;
                case Act.Ds: return ColDs;
                default:     return Color.white;
            }
        }

        // ── RectTransform helpers ─────────────────────────────────────────────────
        private static GameObject MakeChild(string name, GameObject parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
