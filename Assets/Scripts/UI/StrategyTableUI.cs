using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blackjack.UI
{
    /// <summary>
    /// Builds the Blackjack Basic Strategy reference table at runtime using
    /// pure absolute positioning (no LayoutGroups). All coordinates use a
    /// top-left origin: x increases right, y increases downward.
    /// Sections order: Soft Totals → Hard Totals → Pairs.
    /// </summary>
    public class StrategyTableUI : MonoBehaviour
    {
        // ── Action enum ───────────────────────────────────────────────────────────
        private enum Act { H, S, D, P, R }

        // ── Colors ────────────────────────────────────────────────────────────────
        private static readonly Color ColBg         = new Color(0.08f, 0.08f, 0.10f, 1f);
        private static readonly Color ColHit        = new Color(0.22f, 0.38f, 0.60f, 1f);
        private static readonly Color ColStand      = new Color(0.70f, 0.67f, 0.22f, 1f);
        private static readonly Color ColDouble     = new Color(0.20f, 0.50f, 0.20f, 1f);
        private static readonly Color ColSplit      = new Color(0.48f, 0.25f, 0.62f, 1f);
        private static readonly Color ColSurrender  = new Color(0.75f, 0.20f, 0.20f, 1f);
        private static readonly Color ColHeader     = new Color(0.16f, 0.16f, 0.20f, 1f);
        private static readonly Color ColSectionBg  = new Color(0.11f, 0.11f, 0.14f, 1f);
        private static readonly Color ColDealerBg   = new Color(0.12f, 0.18f, 0.25f, 1f);
        private static readonly Color ColBorder     = new Color(0.28f, 0.28f, 0.32f, 1f);
        private static readonly Color ColCellText   = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color ColHeaderText = new Color(0.75f, 0.78f, 0.85f, 1f);

        // ── Layout constants ──────────────────────────────────────────────────────
        private const float Pad         = 4f;
        private const float SectionLblW = 22f;
        private const float LabelColW   = 90f;
        private const float DataColW    = 28f;
        private const float RowH        = 18f;
        private const float HeaderRowH  = 15f;
        private const float Border      = 1f;
        private const int   FontCell    = 18;
        private const int   FontHead    = 17;
        private const int   FontSection = 13;

        // TableW = sectionLabel + border + labelCol + border + 10 dataCols + 10 col-borders
        private static float TableW =>
            SectionLblW + Border + LabelColW + Border + DataColW * 10f + Border * 10f;

        // Section height = top-border + dealerHeader + border + colHeader + border + N data rows
        private static float SectionH(int rowCount) =>
            Border + HeaderRowH + Border + RowH + Border + rowCount * (RowH + Border);

        // ── Dealer column labels ──────────────────────────────────────────────────
        private static readonly string[] DealerLabels = { "2","3","4","5","6","7","8","9","10","A" };

        // ── Strategy data (Dealer cols: 2 3 4 5 6 7 8 9 10 A) ────────────────────
        private static readonly (string label, Act[] cols)[] SoftRows =
        {
            ("A,2 (Soft 13)", new[]{ Act.H, Act.H, Act.H, Act.D, Act.D, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("A,3 (Soft 14)", new[]{ Act.H, Act.H, Act.H, Act.D, Act.D, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("A,4 (Soft 15)", new[]{ Act.H, Act.H, Act.D, Act.D, Act.D, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("A,5 (Soft 16)", new[]{ Act.H, Act.H, Act.D, Act.D, Act.D, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("A,6 (Soft 17)", new[]{ Act.H, Act.D, Act.D, Act.D, Act.D, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("A,7 (Soft 18)", new[]{ Act.D, Act.D, Act.D, Act.D, Act.D, Act.S, Act.S, Act.H, Act.H, Act.H }),
            ("A,8 (Soft 19)", new[]{ Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S }),
            ("A,9 (Soft 20)", new[]{ Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S }),
        };

        private static readonly (string label, Act[] cols)[] HardRows =
        {
            ("16", new[]{ Act.S, Act.S, Act.S, Act.S, Act.S, Act.H, Act.H, Act.R, Act.R, Act.R }),
            ("15", new[]{ Act.S, Act.S, Act.S, Act.S, Act.S, Act.H, Act.H, Act.H, Act.R, Act.H }),
            ("14", new[]{ Act.S, Act.S, Act.S, Act.S, Act.S, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("13", new[]{ Act.S, Act.S, Act.S, Act.S, Act.S, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("12", new[]{ Act.H, Act.H, Act.S, Act.S, Act.S, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("11", new[]{ Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.H }),
            ("10", new[]{ Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.H, Act.H }),
            ("9",  new[]{ Act.H, Act.D, Act.D, Act.D, Act.D, Act.H, Act.H, Act.H, Act.H, Act.H }),
        };

        private static readonly (string label, Act[] cols)[] PairRows =
        {
            ("A,A",   new[]{ Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.P }),
            ("10,10", new[]{ Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S, Act.S }),
            ("9,9",   new[]{ Act.P, Act.P, Act.P, Act.P, Act.P, Act.S, Act.P, Act.P, Act.S, Act.S }),
            ("8,8",   new[]{ Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.P }),
            ("7,7",   new[]{ Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.H, Act.H, Act.H, Act.H }),
            ("6,6",   new[]{ Act.P, Act.P, Act.P, Act.P, Act.P, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("5,5",   new[]{ Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.D, Act.H, Act.H }),
            ("4,4",   new[]{ Act.H, Act.H, Act.H, Act.P, Act.P, Act.H, Act.H, Act.H, Act.H, Act.H }),
            ("3,3",   new[]{ Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.H, Act.H, Act.H, Act.H }),
            ("2,2",   new[]{ Act.P, Act.P, Act.P, Act.P, Act.P, Act.P, Act.H, Act.H, Act.H, Act.H }),
        };

        // ── Cell registry ─────────────────────────────────────────────────────────
        private GameObject[,] _softCells;
        private GameObject[,] _hardCells;
        private GameObject[,] _pairCells;
        private GameObject    _currentHighlight;
        private bool          _built;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Start() => EnsureBuilt();

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            BuildUI();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows or hides the strategy table overlay.</summary>
        public void SetVisible(bool visible)
        {
            EnsureBuilt();
            gameObject.SetActive(visible);
        }

        /// <summary>Highlights the strategy cell for the current hand state.</summary>
        public void HighlightRecommendation(Hand hand, CardData dealerUpcard,
                                            bool canSplit, bool canDouble, bool canSurrender)
        {
            ClearHighlight();
            int col = DealerCol(dealerUpcard);
            var cell = FindCell(hand, canSplit, col);
            if (cell != null) ShowHighlight(cell);
        }

        /// <summary>Removes the current highlighted cell overlay.</summary>
        public void ClearHighlight()
        {
            if (_currentHighlight == null) return;
            var hl = _currentHighlight.transform.Find("HL");
            if (hl != null) Destroy(hl.gameObject);
            _currentHighlight = null;
        }

        // ── UI construction ───────────────────────────────────────────────────────

        private void BuildUI()
        {
            gameObject.AddComponent<Image>().color = ColBg;

            float curY = Pad;

            _softCells = BuildSection("SOFT TOTALS", SoftRows, curY);
            curY += SectionH(SoftRows.Length) + Border;

            _hardCells = BuildSection("HARD TOTALS", HardRows, curY);
            curY += SectionH(HardRows.Length) + Border;

            _pairCells = BuildSection("PAIRS", PairRows, curY);
            curY += SectionH(PairRows.Length) + Pad;

            BuildLegend(curY);
            curY += LegendH + Pad;

            // Auto-size root to fit all content
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(TableW + Pad * 2f, curY);
        }

        // ── Legend ────────────────────────────────────────────────────────────────

        private static readonly (Act act, string name)[] LegendEntries =
        {
            (Act.H, "Hit"),
            (Act.S, "Stand"),
            (Act.D, "Double"),
            (Act.P, "Split"),
            (Act.R, "Surrender"),
        };

        private const float LegendH        = 22f;
        private const float LegendSwatchW  = 22f;
        private const float LegendItemGap  = 8f;
        private const float LegendPadH     = 4f;

        private void BuildLegend(float topY)
        {
            // Measure total legend width to centre it under the table
            int   n         = LegendEntries.Length;
            float itemW     = LegendSwatchW + Pad + DataColW * 3f; // swatch + label space
            float totalW    = n * itemW + (n - 1) * LegendItemGap;
            float startX    = (TableW - totalW) / 2f + Pad;        // centred

            var legendGO = Rect(gameObject, "Legend", Pad, topY, TableW, LegendH);
            legendGO.AddComponent<Image>().color = ColSectionBg;

            float swatchH   = LegendH - LegendPadH * 2f;

            for (int i = 0; i < n; i++)
            {
                var (act, label) = LegendEntries[i];
                float itemX = startX + i * (itemW + LegendItemGap);

                // Coloured swatch
                var swatch = Rect(legendGO, "Swatch" + i,
                                  itemX, LegendPadH, LegendSwatchW, swatchH);
                swatch.AddComponent<Image>().color = ActColor(act);
                AddLabel(swatch, ActLabel(act), FontCell, FontStyles.Bold,
                         ColCellText, TextAlignmentOptions.Center);

                // Text label next to swatch
                var txt = Rect(legendGO, "LegendTxt" + i,
                               itemX + LegendSwatchW + Pad, LegendPadH,
                               itemW - LegendSwatchW - Pad, swatchH);
                AddLabel(txt, label, FontHead, FontStyles.Normal,
                         ColCellText, TextAlignmentOptions.MidlineLeft);
            }
        }

        private GameObject[,] BuildSection(string title,
                                           (string label, Act[] cols)[] rows,
                                           float topY)
        {
            int   rowCount = rows.Length;
            float secH     = SectionH(rowCount);

            // Section GO sits inside the root, offset by Pad on left
            var secGO = Rect(gameObject, "Sec_" + title, Pad, topY, TableW, secH);
            secGO.AddComponent<Image>().color = ColBorder;

            // ── Rotated section label ─────────────────────────────────────────────
            // After 90° rotation, visual dimensions swap:
            //   visual width  = SectionLblW  → pre-rotation height
            //   visual height = secH         → pre-rotation width
            {
                float rtW   = secH - 2f * Border;    // pre-rotation width  = visual height
                float rtH   = SectionLblW - 2f * Border; // pre-rotation height = visual width
                float ctrX  = SectionLblW / 2f;
                float ctrY  = secH / 2f;

                var lblGO = new GameObject("SectionLabel", typeof(RectTransform));
                lblGO.transform.SetParent(secGO.transform, false);

                var lblRT = lblGO.GetComponent<RectTransform>();
                lblRT.anchorMin        = new Vector2(0f, 1f);
                lblRT.anchorMax        = new Vector2(0f, 1f);
                lblRT.pivot            = new Vector2(0.5f, 0.5f);
                lblRT.anchoredPosition = new Vector2(ctrX, -ctrY);
                lblRT.sizeDelta        = new Vector2(rtW, rtH);

                lblGO.AddComponent<Image>().color = ColSectionBg;
                AddLabel(lblGO, title, FontSection, FontStyles.Bold,
                         ColHeaderText, TextAlignmentOptions.Center);
                lblGO.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // Content starts after section label + border
            float cx    = SectionLblW + Border;
            float dataX = cx + LabelColW + Border;

            // ── DEALER UPCARD header ──────────────────────────────────────────────
            float rowY = Border;
            var dealerGO = Rect(secGO, "DealerHdr", cx, rowY, TableW - cx - Border, HeaderRowH);
            dealerGO.AddComponent<Image>().color = ColDealerBg;
            AddLabel(dealerGO, "DEALER UPCARD", FontHead, FontStyles.Bold,
                     ColHeaderText, TextAlignmentOptions.Center);
            rowY += HeaderRowH + Border;

            // ── Column header row ─────────────────────────────────────────────────
            Rect(secGO, "HdrBlank", cx, rowY, LabelColW, RowH)
                .AddComponent<Image>().color = ColHeader;

            for (int c = 0; c < 10; c++)
            {
                var hdr = Rect(secGO, "ColHdr" + c,
                               dataX + c * (DataColW + Border), rowY, DataColW, RowH);
                hdr.AddComponent<Image>().color = ColHeader;
                AddLabel(hdr, DealerLabels[c], FontCell, FontStyles.Bold,
                         ColHeaderText, TextAlignmentOptions.Center);
            }
            rowY += RowH + Border;

            // ── Data rows ─────────────────────────────────────────────────────────
            var cells = new GameObject[rowCount, 10];
            for (int r = 0; r < rowCount; r++)
            {
                var (lbl, acts) = rows[r];

                var lblCell = Rect(secGO, "Lbl" + r, cx, rowY, LabelColW, RowH);
                lblCell.AddComponent<Image>().color = ColHeader;
                AddLabel(lblCell, lbl, FontCell, FontStyles.Normal,
                         ColHeaderText, TextAlignmentOptions.MidlineLeft,
                         new RectOffset(3, 0, 0, 0));

                for (int c = 0; c < acts.Length; c++)
                {
                    var cell = Rect(secGO, $"Cell{r}_{c}",
                                    dataX + c * (DataColW + Border), rowY, DataColW, RowH);
                    cell.AddComponent<Image>().color = ActColor(acts[c]);
                    AddLabel(cell, ActLabel(acts[c]), FontCell, FontStyles.Bold,
                             ColCellText, TextAlignmentOptions.Center);
                    cells[r, c] = cell;
                }

                rowY += RowH + Border;
            }

            return cells;
        }

        // ── Highlight helpers ─────────────────────────────────────────────────────

        private GameObject FindCell(Hand hand, bool canSplit, int col)
        {
            if (canSplit && hand.Count == 2)
            {
                int v0 = hand.Cards[0].BlackjackValue;
                int v1 = hand.Cards[1].BlackjackValue;
                bool isPair = v0 == v1 || (v0 >= 10 && v1 >= 10);
                if (isPair)
                {
                    int key = (v0 >= 10 && v0 != 11) ? 10 : Mathf.Min(v0, 11);
                    int row = PairRowIndex(key);
                    if (_pairCells != null && row >= 0 && row < _pairCells.GetLength(0))
                        return _pairCells[row, col];
                }
            }

            if (hand.IsSoft())
            {
                int row = Mathf.Clamp(hand.BestValue(), 13, 20) - 13;
                if (_softCells != null && row < _softCells.GetLength(0))
                    return _softCells[row, col];
            }

            int hardRow = HardRowIndex(hand.BestValue());
            if (_hardCells != null && hardRow >= 0 && hardRow < _hardCells.GetLength(0))
                return _hardCells[hardRow, col];

            return null;
        }

        private void ShowHighlight(GameObject cell)
        {
            _currentHighlight = cell;

            var hlGO = new GameObject("HL", typeof(RectTransform));
            hlGO.transform.SetParent(cell.transform, false);
            hlGO.transform.SetSiblingIndex(0);

            var rt = hlGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-2f, -2f);
            rt.offsetMax = new Vector2(2f, 2f);
            hlGO.AddComponent<Image>().color = Color.white;

            var innerGO = new GameObject("HLInner", typeof(RectTransform));
            innerGO.transform.SetParent(hlGO.transform, false);
            var irt = innerGO.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(2f, 2f);
            irt.offsetMax = new Vector2(-2f, -2f);
            innerGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
        }

        // ── Row index mappers ─────────────────────────────────────────────────────

        private static int HardRowIndex(int total)
        {
            if (total >= 16) return 0;
            if (total == 15) return 1;
            if (total == 14) return 2;
            if (total == 13) return 3;
            if (total == 12) return 4;
            if (total == 11) return 5;
            if (total == 10) return 6;
            return 7;
        }

        private static int PairRowIndex(int pairKey)
        {
            switch (pairKey)
            {
                case 11: return 0;
                case 10: return 1;
                case 9:  return 2;
                case 8:  return 3;
                case 7:  return 4;
                case 6:  return 5;
                case 5:  return 6;
                case 4:  return 7;
                case 3:  return 8;
                case 2:  return 9;
                default: return -1;
            }
        }

        private static int DealerCol(CardData card)
        {
            switch (card.Rank)
            {
                case Rank.Two:                                   return 0;
                case Rank.Three:                                 return 1;
                case Rank.Four:                                  return 2;
                case Rank.Five:                                  return 3;
                case Rank.Six:                                   return 4;
                case Rank.Seven:                                 return 5;
                case Rank.Eight:                                 return 6;
                case Rank.Nine:                                  return 7;
                case Rank.Ten: case Rank.Jack:
                case Rank.Queen: case Rank.King:                 return 8;
                case Rank.Ace:                                   return 9;
                default:                                         return 8;
            }
        }

        // ── RectTransform helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Creates a child with top-left anchor+pivot.
        /// x = distance from parent left, y = distance from parent top.
        /// </summary>
        private static GameObject Rect(GameObject parent, string name,
                                       float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta        = new Vector2(w, h);
            return go;
        }

        private static void AddLabel(GameObject parent, string text, float size,
                                     FontStyles style, Color color,
                                     TextAlignmentOptions align,
                                     RectOffset padding = null)
        {
            var lblGO = new GameObject("Txt", typeof(RectTransform));
            lblGO.transform.SetParent(parent.transform, false);

            var rt = lblGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            if (padding != null)
            {
                rt.offsetMin = new Vector2(padding.left,   padding.bottom);
                rt.offsetMax = new Vector2(-padding.right, -padding.top);
            }
            else
            {
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text               = text;
            tmp.fontSize           = size;
            tmp.fontStyle          = style;
            tmp.alignment          = align;
            tmp.color              = color;
            tmp.overflowMode       = TextOverflowModes.Overflow;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget      = false;
        }

        // ── Enum helpers ──────────────────────────────────────────────────────────

        private static string ActLabel(Act a)
        {
            switch (a)
            {
                case Act.H: return "H";
                case Act.S: return "S";
                case Act.D: return "D";
                case Act.P: return "P";
                case Act.R: return "R";
                default:    return "?";
            }
        }

        private static Color ActColor(Act a)
        {
            switch (a)
            {
                case Act.H: return ColHit;
                case Act.S: return ColStand;
                case Act.D: return ColDouble;
                case Act.P: return ColSplit;
                case Act.R: return ColSurrender;
                default:    return Color.white;
            }
        }
    }
}
