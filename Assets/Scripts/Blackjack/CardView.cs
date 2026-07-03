using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Controls a single card UI element: face/back display, flip animation, and bloom glow effect.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CardView : MonoBehaviour, ICardDisplay
    {
        [Header("References")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Image glowImage;

        [Header("Flip Animation")]
        [SerializeField] private float flipDuration = 0.35f;

        [Header("Bloom Pulse")]
        [SerializeField] private float breatheSpeed  = 0.7f;
        [SerializeField] private float flickerSpeed  = 4.5f;
        [SerializeField] private float flickerWeight = 0.35f;
        [SerializeField] private float pulseMinAlpha = 0.15f;
        [SerializeField] private float pulseMaxAlpha = 0.88f;
        [SerializeField] private Color glowColorMin  = new Color(0.95f, 0.72f, 0.08f);
        [SerializeField] private Color glowColorMax  = new Color(1.35f, 1.05f, 0.25f);

        private Sprite _faceSprite;
        private Sprite _backSprite;
        private bool   _isFaceUp;

        private Coroutine _flipCoroutine;
        private Coroutine _glowCoroutine;

        private float _glowNoiseOffset;

        private void Awake()
        {
            if (cardImage == null) cardImage = GetComponent<Image>();
            _glowNoiseOffset = Random.Range(0f, 100f);
            EnsureRectMask();
        }

        private void EnsureRectMask()
        {
            if (GetComponent<RectMask2D>() == null)
                gameObject.AddComponent<RectMask2D>();
        }

        public void Setup(Sprite faceSprite, Sprite backSprite, bool faceUp = true)
        {
            _faceSprite      = faceSprite;
            _backSprite      = backSprite;
            _isFaceUp        = faceUp;
            cardImage.sprite = faceUp ? _faceSprite : _backSprite;
            SyncGlowSprite();
            SetGlow(false);
        }

        private void SyncGlowSprite()
        {
            if (glowImage == null || cardImage == null)
                return;

            glowImage.sprite         = cardImage.sprite;
            glowImage.preserveAspect = true;
        }

        public void Flip(bool toFaceUp, System.Action onComplete = null, System.Action onFaceRevealed = null)
        {
            if (_flipCoroutine != null) StopCoroutine(_flipCoroutine);
            _flipCoroutine = StartCoroutine(FlipRoutine(toFaceUp, onComplete, onFaceRevealed));
        }

        private IEnumerator FlipRoutine(bool toFaceUp, System.Action onComplete, System.Action onFaceRevealed)
        {
            RectTransform rt        = GetComponent<RectTransform>();
            Vector3       origScale = rt.localScale;
            float         half      = flipDuration * 0.5f;
            float         elapsed   = 0f;

            while (elapsed < half)
            {
                elapsed       += Time.deltaTime;
                float t        = Mathf.Clamp01(elapsed / half);
                rt.localScale  = new Vector3(origScale.x * (1f - t), origScale.y, 1f);
                yield return null;
            }
            rt.localScale = new Vector3(0f, origScale.y, 1f);

            _isFaceUp        = toFaceUp;
            cardImage.sprite = toFaceUp ? _faceSprite : _backSprite;
            SyncGlowSprite();
            onFaceRevealed?.Invoke();

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed       += Time.deltaTime;
                float t        = Mathf.Clamp01(elapsed / half);
                rt.localScale  = new Vector3(origScale.x * t, origScale.y, 1f);
                yield return null;
            }
            rt.localScale = origScale;

            onComplete?.Invoke();
        }

        public void SetFaceUpImmediate(bool faceUp)
        {
            if (_flipCoroutine != null)
            {
                StopCoroutine(_flipCoroutine);
                _flipCoroutine = null;
            }

            _isFaceUp        = faceUp;
            cardImage.sprite = faceUp ? _faceSprite : _backSprite;
            SyncGlowSprite();

            RectTransform rt = GetComponent<RectTransform>();
            Vector3 origScale = rt.localScale;
            if (origScale.x == 0f)
                rt.localScale = Vector3.one;
        }

        public void SetGlow(bool enabled)
        {
            StopGlowPulse();
            if (glowImage == null) return;
            SyncGlowSprite();
            glowImage.enabled = enabled;
            glowImage.color   = new Color(1.15f, 0.92f, 0.2f, enabled ? pulseMaxAlpha : 0f);
        }

        public void StartGlowPulse()
        {
            if (glowImage == null) return;
            StopGlowPulse();
            SyncGlowSprite();
            glowImage.enabled = true;
            _glowCoroutine    = StartCoroutine(GlowPulseRoutine());
        }

        public void StopGlowPulse()
        {
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
                _glowCoroutine = null;
            }
            if (glowImage != null)
                glowImage.enabled = false;
        }

        private IEnumerator GlowPulseRoutine()
        {
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime;

                float breathe = (Mathf.Sin(time * breatheSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                float flicker = Mathf.PerlinNoise(time * flickerSpeed + _glowNoiseOffset, 0f);
                float t       = Mathf.Lerp(breathe, flicker, flickerWeight);
                float alpha   = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);

                Color color = Color.Lerp(
                    new Color(glowColorMin.r, glowColorMin.g, glowColorMin.b, alpha),
                    new Color(glowColorMax.r, glowColorMax.g, glowColorMax.b, alpha),
                    t);

                glowImage.color = color;
                yield return null;
            }
        }

        public bool IsFaceUp => _isFaceUp;
    }
}
