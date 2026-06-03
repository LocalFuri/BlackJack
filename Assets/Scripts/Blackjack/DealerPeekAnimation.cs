using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Brief lift and slight tilt on the dealer hole card when checking for natural blackjack.
    /// Does not reveal the card face or change sprites.
    /// </summary>
    [DisallowMultipleComponent]
    public class DealerPeekAnimation : MonoBehaviour
    {
        private const string LegacyPeekCornerName = "PeekCorner";

        [Header("Motion")]
        [SerializeField] private float liftPixels = 4f;
        [SerializeField] private float peekRotationZ = -2f;

        [Header("Timing")]
        [SerializeField] private float moveOutDuration = 0.05f;
        [SerializeField] private float holdDuration = 0.05f;
        [SerializeField] private float returnDuration = 0.05f;

        private RectTransform _rect;
        private LayoutElement _layoutElement;
        private bool _savedIgnoreLayout;
        private bool _isPeeking;

        private void Awake()
        {
            _rect = transform as RectTransform;
            RemoveLegacyPeekCorner();
        }

        public IEnumerator PlayPeekRoutine()
        {
            yield return DealerPeekHoleCardAnimation();
        }

        public IEnumerator DealerPeekHoleCardAnimation()
        {
            if (_isPeeking || _rect == null)
                yield break;

            if (_rect.parent is RectTransform layoutRoot)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
                Canvas.ForceUpdateCanvases();
            }

            yield return null;

            _isPeeking = true;

            Vector3 restPos = _rect.localPosition;
            Quaternion restRot = _rect.localRotation;
            Vector3 peakPos = restPos + new Vector3(0f, liftPixels, 0f);
            Quaternion peakRot = restRot * Quaternion.Euler(0f, 0f, peekRotationZ);

            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
                _layoutElement = gameObject.AddComponent<LayoutElement>();

            _savedIgnoreLayout = _layoutElement.ignoreLayout;
            _layoutElement.ignoreLayout = true;

            _rect.localPosition = restPos;
            _rect.localRotation = restRot;

            yield return LerpPose(restPos, restRot, peakPos, peakRot, moveOutDuration);
            yield return new WaitForSeconds(holdDuration);
            yield return LerpPose(peakPos, peakRot, restPos, restRot, returnDuration);

            _rect.localPosition = restPos;
            _rect.localRotation = restRot;

            _layoutElement.ignoreLayout = _savedIgnoreLayout;
            _isPeeking = false;
        }

        public IEnumerator DealerPeekRoutine()
        {
            yield return DealerPeekHoleCardAnimation();
        }

        private IEnumerator LerpPose(
            Vector3 fromPos, Quaternion fromRot,
            Vector3 toPos, Quaternion toRot,
            float duration)
        {
            if (duration <= 0f)
            {
                _rect.localPosition = toPos;
                _rect.localRotation = toRot;
                yield break;
            }

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = SmoothStep(Mathf.Clamp01(t / duration));
                _rect.localPosition = Vector3.Lerp(fromPos, toPos, p);
                _rect.localRotation = Quaternion.Lerp(fromRot, toRot, p);
                yield return null;
            }

            _rect.localPosition = toPos;
            _rect.localRotation = toRot;
        }

        private static float SmoothStep(float t) => t * t * (3f - 2f * t);

        private void RemoveLegacyPeekCorner()
        {
            Transform legacy = transform.Find(LegacyPeekCornerName);
            if (legacy != null)
                Destroy(legacy.gameObject);
        }
    }
}
