using UnityEngine;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Lays out child WorldCardView objects in a horizontal row in world space.
    /// </summary>
    public class WorldCardRowLayout : MonoBehaviour
    {
        [SerializeField] private float cardSpacing = 1.25f;

        private float _cardWorldWidth = 1.2f;

        public float CardWorldWidth => _cardWorldWidth > 0f ? _cardWorldWidth : 1.2f;

        public void ConfigureFromUiArea(RectTransform uiArea)
        {
            Canvas canvas = uiArea != null ? uiArea.GetComponentInParent<Canvas>() : null;
            _cardWorldWidth = WorldCardAreaBootstrap.GetCardWorldWidthFromCanvas(canvas);

            float scale = canvas != null ? canvas.scaleFactor : 1f;
            float ppu = 100f;
            CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            if (scaler != null)
                ppu = Mathf.Max(1f, scaler.referencePixelsPerUnit);

            float uiSpacingPx = 10f;
            HorizontalLayoutGroup layoutGroup = uiArea != null ? uiArea.GetComponent<HorizontalLayoutGroup>() : null;
            if (layoutGroup != null)
                uiSpacingPx = layoutGroup.spacing;

            cardSpacing = _cardWorldWidth + uiSpacingPx / ppu * scale;
        }

        public void RefreshLayout()
        {
            int index = 0;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf)
                    continue;

                WorldCardView card = child.GetComponent<WorldCardView>();
                float spacing = card != null ? card.CardWorldWidth + (cardSpacing - CardWorldWidth) : cardSpacing;
                child.localPosition = new Vector3(index * spacing, 0f, 0f);
                child.localRotation = Quaternion.identity;
                index++;
            }
        }
    }
}
