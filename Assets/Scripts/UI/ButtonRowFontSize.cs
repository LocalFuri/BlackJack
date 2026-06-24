using TMPro;
using UnityEngine;

namespace Blackjack.UI
{
    /// <summary>
    /// Keeps every direct child button in a row the same width (based on the widest label,
    /// e.g. "SURRENDER") and applies a uniform font size that fits inside that width.
    /// </summary>
    [ExecuteAlways]
    public class ButtonRowFontSize : MonoBehaviour
    {
        [Header("Button width")]
        [Tooltip("When enabled, every direct child button is resized to fit the reference/widest label.")]
        [SerializeField] private bool syncButtonWidths = true;

        [Tooltip("Optional label used to size buttons (e.g. Surrender). When empty, the widest child label is used.")]
        [SerializeField] private TMP_Text referenceLabel;

        [Tooltip("When enabled, each side gets padding equal to one or more 'M' character widths from the sizing label's font.")]
        [SerializeField] private bool useLetterPadding = true;

        [Tooltip("How many character widths to add on each side when letter padding is enabled.")]
        [SerializeField] private float letterPaddingCount = 1f;

        [Tooltip("Fixed extra horizontal space on each side when letter padding is disabled.")]
        [SerializeField] private float horizontalPadding = 24f;

        [Header("Font size")]
        [Tooltip("Upper bound for the font size. Automatically reduced so every label fits within its button.")]
        [SerializeField] private float maxFontSize = 28f;

        [Tooltip("Lower bound for the font size.")]
        [SerializeField] private float minFontSize = 8f;

        private void OnValidate() => Apply();
        private void OnEnable() => Apply();

        public void SetReferenceLabel(TMP_Text label)
        {
            referenceLabel = label;
            Apply();
        }

        /// <summary>Computes button widths and a uniform font size for all child labels.</summary>
        public void Apply()
        {
            TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(includeInactive: true);
            if (labels.Length == 0) return;

            foreach (TMP_Text label in labels)
            {
                label.enableAutoSizing = false;
                label.fontSize = maxFontSize;
                label.overflowMode = TextOverflowModes.Overflow;
            }

            Canvas.ForceUpdateCanvases();

            if (syncButtonWidths)
                ApplyUniformButtonWidth(labels);

            Canvas.ForceUpdateCanvases();

            float minScaleFactor = 1f;
            foreach (TMP_Text label in labels)
            {
                float containerWidth = label.rectTransform.rect.width;
                if (containerWidth <= 0f) continue;

                float textWidth = label.GetPreferredValues().x;
                if (textWidth > containerWidth)
                {
                    float scale = containerWidth / textWidth;
                    if (scale < minScaleFactor)
                        minScaleFactor = scale;
                }
            }

            float finalSize = Mathf.Max(minFontSize, maxFontSize * minScaleFactor);
            foreach (TMP_Text label in labels)
                label.fontSize = finalSize;
        }

        private void ApplyUniformButtonWidth(TMP_Text[] labels)
        {
            TMP_Text sizingLabel = GetSizingLabel(labels);
            float textWidth = sizingLabel.GetPreferredValues().x;
            float sidePadding = GetSidePadding(sizingLabel);
            float buttonWidth = textWidth + sidePadding * 2f;

            foreach (Transform child in transform)
            {
                if (child is not RectTransform rt) continue;
                rt.sizeDelta = new Vector2(buttonWidth, rt.sizeDelta.y);
            }
        }

        private TMP_Text GetSizingLabel(TMP_Text[] labels)
        {
            if (referenceLabel != null)
                return referenceLabel;

            TMP_Text widest = labels[0];
            float widestWidth = 0f;
            foreach (TMP_Text label in labels)
            {
                float width = label.GetPreferredValues().x;
                if (width > widestWidth)
                {
                    widestWidth = width;
                    widest = label;
                }
            }

            return widest;
        }

        private float GetSidePadding(TMP_Text sizingLabel)
        {
            if (!useLetterPadding)
                return horizontalPadding;

            return sizingLabel.GetPreferredValues("M").x * letterPaddingCount;
        }
    }
}
