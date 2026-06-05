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

        [Tooltip("Extra horizontal space added on each side of the label text.")]
        [SerializeField] private float horizontalPadding = 24f;

        [Header("Font size")]
        [Tooltip("Upper bound for the font size. Automatically reduced so every label fits within its button.")]
        [SerializeField] private float maxFontSize = 28f;

        [Tooltip("Lower bound for the font size.")]
        [SerializeField] private float minFontSize = 8f;

        private void OnValidate() => Apply();
        private void OnEnable() => Apply();

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
            float textWidth = MeasureReferenceTextWidth(labels);
            float buttonWidth = textWidth + horizontalPadding * 2f;

            foreach (Transform child in transform)
            {
                if (child is not RectTransform rt) continue;
                rt.sizeDelta = new Vector2(buttonWidth, rt.sizeDelta.y);
            }
        }

        private float MeasureReferenceTextWidth(TMP_Text[] labels)
        {
            if (referenceLabel != null)
                return referenceLabel.GetPreferredValues().x;

            float widest = 0f;
            foreach (TMP_Text label in labels)
                widest = Mathf.Max(widest, label.GetPreferredValues().x);

            return widest;
        }
    }
}
