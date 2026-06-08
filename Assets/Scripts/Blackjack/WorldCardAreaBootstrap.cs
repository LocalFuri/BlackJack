using UnityEngine;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Creates world-space card row parents aligned to UI card areas (not under Canvas scale).
    /// </summary>
    public static class WorldCardAreaBootstrap
    {
        private const float ReferenceCardWidthPx = 120f;
        private const float ZOffsetTowardCamera = 0.05f;

        private static Transform _worldRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetWorldRoot()
        {
            _worldRoot = null;
        }

        public static Transform EnsureWorldArea(Transform uiArea, string worldAreaName)
        {
            if (uiArea == null)
                return null;

            Canvas canvas = uiArea.GetComponentInParent<Canvas>();
            Transform root = GetWorldRoot(canvas);

            if (canvas != null)
            {
                Transform legacy = canvas.transform.Find(worldAreaName);
                if (legacy != null)
                    Object.Destroy(legacy.gameObject);
            }

            Transform existing = root.Find(worldAreaName);
            WorldCardRowLayout layout;
            if (existing != null)
            {
                layout = existing.GetComponent<WorldCardRowLayout>();
                if (uiArea is RectTransform existingRt)
                {
                    if (layout != null)
                        layout.ConfigureFromUiArea(existingRt);
                    AlignWorldAreaToUi(existing, existingRt, canvas, layout);
                }

                return existing;
            }

            var go = new GameObject(worldAreaName);
            go.transform.SetParent(root, true);
            go.transform.localScale = Vector3.one;

            layout = go.GetComponent<WorldCardRowLayout>();
            if (layout == null)
                layout = go.AddComponent<WorldCardRowLayout>();

            if (uiArea is RectTransform areaRt)
            {
                layout.ConfigureFromUiArea(areaRt);
                AlignWorldAreaToUi(go.transform, areaRt, canvas, layout);
            }
            else
            {
                go.transform.position = uiArea.position;
                go.transform.rotation = uiArea.rotation;
            }

            return go.transform;
        }

        /// <summary>
        /// Matches UI HorizontalLayoutGroup Middle-Left: first card center on the area's left edge.
        /// </summary>
        private static void AlignWorldAreaToUi(
            Transform worldArea, RectTransform uiArea, Canvas canvas, WorldCardRowLayout layout)
        {
            Vector3[] corners = new Vector3[4];
            uiArea.GetWorldCorners(corners);

            // Left edge, vertical center (Unity corner order: 0 BL, 1 TL, 2 TR, 3 BR).
            Vector3 anchor = (corners[0] + corners[1]) * 0.5f;

            float halfCard = layout != null ? layout.CardWorldWidth * 0.5f : 0f;
            Vector3 position = anchor + uiArea.right * halfCard;

            Camera cam = canvas != null ? canvas.worldCamera : Camera.main;
            if (cam != null)
                position -= cam.transform.forward * ZOffsetTowardCamera;

            worldArea.position = position;
            worldArea.rotation = uiArea.rotation;
        }

        public static float GetCardWorldWidthFromCanvas(Canvas canvas)
        {
            if (canvas == null)
                return ReferenceCardWidthPx / 100f;

            float ppu = 100f;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
                ppu = Mathf.Max(1f, scaler.referencePixelsPerUnit);

            // Screen Space - Camera canvas world size is resolution-independent,
            // so card world width must not scale with canvas.scaleFactor.
            return ReferenceCardWidthPx / ppu;
        }

        private static Transform GetWorldRoot(Canvas canvas)
        {
            if (_worldRoot != null)
                return _worldRoot;

            var go = new GameObject("WorldCardRoot");
            Camera cam = canvas != null ? canvas.worldCamera : Camera.main;
            if (cam != null)
                go.transform.SetParent(cam.transform, false);
            else
                go.transform.SetParent(null, false);

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            _worldRoot = go.transform;
            return _worldRoot;
        }
    }
}
