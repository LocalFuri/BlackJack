using UnityEngine;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Sort orders for UI that must draw above world-space card meshes.
    /// </summary>
    public static class UiOverlaySorting
    {
        public const int MartingalePopup = 300;
        public const int DeviationPopup = 350;
        public const int Menu = 400;

        public static void Apply(GameObject root, int sortingOrder)
        {
            if (root == null)
                return;

            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null)
                canvas = root.AddComponent<Canvas>();

            Canvas parentCanvas = root.transform.parent != null
                ? root.transform.parent.GetComponentInParent<Canvas>()
                : null;
            if (parentCanvas != null && parentCanvas != canvas)
            {
                canvas.renderMode = parentCanvas.renderMode;
                canvas.worldCamera = parentCanvas.worldCamera;
                canvas.planeDistance = parentCanvas.planeDistance;
                canvas.sortingLayerID = parentCanvas.sortingLayerID;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            if (root.GetComponent<GraphicRaycaster>() == null)
                root.AddComponent<GraphicRaycaster>();
        }
    }
}
