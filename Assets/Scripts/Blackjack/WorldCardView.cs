using System;
using System.Collections;
using UnityEngine;

namespace Blackjack
{
    /// <summary>
    /// World-space card rendered with MeshRenderer and deformable mesh for dealer peek.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class WorldCardView : MonoBehaviour, ICardDisplay
    {
        private static Shader _cachedShader;

        [Header("Card Size (world units)")]
        [SerializeField] private float cardWorldWidth = 1.2f;

        [Header("Mesh Grid")]
        [SerializeField] private int meshColumns = 16;
        [SerializeField] private int meshRows = 20;

        [Header("Corner Peek")]
        [SerializeField] private float peekDuration = 0.25f;
        [SerializeField] private float peekHoldDuration = 0.1f;
        [SerializeField] private float returnDuration = 0.25f;
        [SerializeField] private float cornerBendAmount = 0.14f;
        [SerializeField] private AnimationCurve peekEase;
        [SerializeField] private float cornerRegionStart = 0.68f;

        [Header("Flip")]
        [SerializeField] private float flipDuration = 0.35f;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _cardMaterial;

        private Sprite _faceSprite;
        private Sprite _backSprite;
        private bool _isFaceUp;
        private float _cardWorldHeight;

        private Mesh _cardMesh;
        private Vector3[] _restVertices;
        private Vector3[] _workVertices;
        private bool _isPeeking;
        private Coroutine _flipCoroutine;

        public float CardWorldWidth => cardWorldWidth;

        public void SetCardWorldWidth(float width)
        {
            if (width > 0f)
                cardWorldWidth = width;
        }

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _meshRenderer.sortingOrder = 200;
            EnsurePeekEaseCurve();
        }

        private void EnsurePeekEaseCurve()
        {
            if (peekEase != null && peekEase.length > 0)
                return;

            peekEase = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f));
            for (int i = 0; i < peekEase.length; i++)
            {
                peekEase.SmoothTangents(i, 0.5f);
            }
        }

        private void Start()
        {
            ApplyWidthFromLayout();
        }

        private void ApplyWidthFromLayout()
        {
            WorldCardRowLayout layout = GetComponentInParent<WorldCardRowLayout>();
            if (layout != null)
                SetCardWorldWidth(layout.CardWorldWidth);
        }

        public void Setup(Sprite faceSprite, Sprite backSprite, bool faceUp = true)
        {
            ApplyWidthFromLayout();

            _faceSprite = faceSprite;
            _backSprite = backSprite;
            _isFaceUp = faceUp;

            Sprite active = faceUp ? _faceSprite : _backSprite;
            if (active == null)
            {
                Debug.LogWarning("[WorldCardView] Setup called with null sprite.");
                return;
            }

            float aspect = active.rect.height / active.rect.width;
            _cardWorldHeight = cardWorldWidth * aspect;

            if (_cardMesh != null)
                Destroy(_cardMesh);

            _cardMesh = SpriteCardMeshBuilder.CreateGridMesh(
                active, meshColumns, meshRows, cardWorldWidth, _cardWorldHeight);

            _meshFilter.sharedMesh = _cardMesh;
            _restVertices = _cardMesh.vertices;
            _workVertices = new Vector3[_restVertices.Length];

            ApplySpriteToMaterial(active);
            _meshRenderer.enabled = true;
        }

        private static Shader GetCardShader()
        {
            if (_cachedShader != null)
                return _cachedShader;

            _cachedShader = Shader.Find("Blackjack/WorldCard");
            if (_cachedShader == null)
                _cachedShader = Shader.Find("Sprites/Default");
            if (_cachedShader == null)
                _cachedShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (_cachedShader == null)
                _cachedShader = Shader.Find("Unlit/Transparent");

            return _cachedShader;
        }

        private void ApplySpriteToMaterial(Sprite sprite)
        {
            if (sprite == null)
                return;

            if (_cardMaterial == null)
            {
                Shader shader = GetCardShader();
                if (shader == null)
                {
                    Debug.LogError("[WorldCardView] No compatible shader found for world cards.");
                    return;
                }

                _cardMaterial = new Material(shader);
                _cardMaterial.renderQueue = 4000;
                _meshRenderer.material = _cardMaterial;
            }

            _cardMaterial.mainTexture = sprite.texture;
            _cardMaterial.color = Color.white;
        }

        public void Flip(bool toFaceUp, Action onComplete = null)
        {
            if (_flipCoroutine != null)
                StopCoroutine(_flipCoroutine);
            _flipCoroutine = StartCoroutine(FlipRoutine(toFaceUp, onComplete));
        }

        private IEnumerator FlipRoutine(bool toFaceUp, Action onComplete)
        {
            Vector3 scale = transform.localScale;
            if (scale.x == 0f)
                scale = Vector3.one;

            float half = flipDuration * 0.5f;

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float p = t / half;
                transform.localScale = new Vector3(scale.x * (1f - p), scale.y, scale.z);
                yield return null;
            }

            transform.localScale = new Vector3(0f, scale.y, scale.z);
            _isFaceUp = toFaceUp;
            Setup(_faceSprite, _backSprite, toFaceUp);

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float p = t / half;
                transform.localScale = new Vector3(scale.x * p, scale.y, scale.z);
                yield return null;
            }

            transform.localScale = scale;
            onComplete?.Invoke();
        }

        public bool IsFaceUp => _isFaceUp;

        public void SetGlow(bool enabled)
        {
            if (_meshRenderer == null || _cardMaterial == null)
                return;

            _cardMaterial.color = enabled ? new Color(1.15f, 1.05f, 0.75f, 1f) : Color.white;
        }

        public void StartGlowPulse() => SetGlow(true);

        public void StopGlowPulse() => SetGlow(false);

        public IEnumerator DealerPeekHoleCardAnimation()
        {
            yield return DealerPeekRoutine();
        }

        public IEnumerator DealerPeekRoutine()
        {
            if (_isPeeking || _cardMesh == null || _restVertices == null)
                yield break;

            if (_isFaceUp)
                yield break;

            _isPeeking = true;

            yield return AnimateCornerBend(0f, 1f, peekDuration);
            if (peekHoldDuration > 0f)
                yield return new WaitForSeconds(peekHoldDuration);
            yield return AnimateCornerBend(1f, 0f, returnDuration);

            RestoreRestMesh();
            _isPeeking = false;
        }

        private IEnumerator AnimateCornerBend(float fromT, float toT, float duration)
        {
            if (duration <= 0f)
            {
                ApplyCornerBend(toT);
                yield break;
            }

            EnsurePeekEaseCurve();

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float p = peekEase.Evaluate(Mathf.Clamp01(elapsed / duration));
                float t = Mathf.Lerp(fromT, toT, p);
                ApplyCornerBend(t);
                yield return null;
            }

            ApplyCornerBend(toT);
        }

        private void ApplyCornerBend(float bendT)
        {
            if (_cardMesh == null || _restVertices == null || _workVertices == null)
                return;

            float halfW = cardWorldWidth * 0.5f;
            float halfH = _cardWorldHeight * 0.5f;
            float maxOffset = cornerBendAmount * cardWorldWidth;

            for (int i = 0; i < _restVertices.Length; i++)
            {
                Vector3 v = _restVertices[i];
                float u = (v.x + halfW) / cardWorldWidth;
                float vv = (v.y + halfH) / _cardWorldHeight;

                float wx = Mathf.InverseLerp(cornerRegionStart, 1f, u);
                float wy = Mathf.InverseLerp(cornerRegionStart, 1f, vv);
                float w = Mathf.Clamp01(wx * wy);

                Vector3 inward = new Vector3(-1f, -1f, 0f).normalized;
                _workVertices[i] = v + inward * (maxOffset * w * bendT);
            }

            SpriteCardMeshBuilder.ApplyVertices(_cardMesh, _workVertices);
        }

        private void RestoreRestMesh()
        {
            if (_cardMesh == null || _restVertices == null)
                return;

            SpriteCardMeshBuilder.ApplyVertices(_cardMesh, _restVertices);
        }

        private void OnDestroy()
        {
            if (_cardMesh != null)
                Destroy(_cardMesh);
            if (_cardMaterial != null)
                Destroy(_cardMaterial);
        }
    }
}
