using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blackjack
{
    /// <summary>
    /// Realistic fireworks built entirely with Unity ParticleSystem components.
    /// Call Play() with the two player card world positions; the effect will
    /// confine its bursts to the bounding area of those cards.
    /// Public API: Play / Stop.
    /// </summary>
    public class FireworksEffect : MonoBehaviour
    {
        [Header("Sequence")]
        [SerializeField] private int   burstCount    = 7;
        [SerializeField] private float burstInterval = 0.30f;
        [SerializeField] private float spreadPadding = 1.2f;  // multiplier on card-span to widen area slightly

        [Header("Shell Rise")]
        [SerializeField] private float shellRiseFraction = 0.5f;  // rise as fraction of card-area half-height
        [SerializeField] private float shellRiseDuration = 0.38f;

        [Header("Burst")]
        [SerializeField] private int   particlesPerBurst  = 80;
        [SerializeField] private float burstSpeed         = 2.5f;
        [SerializeField] private float burstSpeedVariance = 0.8f;
        [SerializeField] private float burstLifetime      = 1.6f;
        [SerializeField] private float gravityModifier    = 0.4f;

        [Header("Glitter Trail")]
        [SerializeField] private int   trailParticles = 6;
        [SerializeField] private float trailLifetime  = 0.5f;
        [SerializeField] private float trailSize      = 0.004f;

        [Header("Rendering")]
        [SerializeField] private int sortingOrder = 200;

        // Camera is at Z=-10, canvas planeDistance=1 so canvas is at Z=-9.
        // Particles at Z=-8 are in front of the canvas (closer to camera),
        // within the frustum (near clip is at Z=-9.7).
        private const float ParticleZ = -8f;

        private static readonly Color[] BurstColors =
        {
            new Color(1.00f, 0.92f, 0.10f),
            new Color(1.00f, 0.25f, 0.25f),
            new Color(0.20f, 1.00f, 0.30f),
            new Color(0.20f, 0.55f, 1.00f),
            new Color(1.00f, 0.50f, 0.05f),
            new Color(0.85f, 0.20f, 1.00f),
            new Color(0.10f, 1.00f, 0.95f),
            new Color(1.00f, 1.00f, 1.00f),
        };

        private readonly List<GameObject> _activeSystems = new List<GameObject>();
        private Material _cachedMaterial;

        // ──────────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _cachedMaterial = BuildMaterial();
        }

        private void OnDestroy()
        {
            if (_cachedMaterial != null)
                Destroy(_cachedMaterial);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Plays a multi-burst firework sequence confined to the bounding area of the two card positions.
        /// cardWorldPos0 and cardWorldPos1 are the world-space positions of the player's first two cards.
        /// </summary>
        public void Play(Vector3 cardWorldPos0, Vector3 cardWorldPos1)
        {
            Vector3 center       = (cardWorldPos0 + cardWorldPos1) * 0.5f;
            float   cardSpan     = Mathf.Abs(cardWorldPos1.x - cardWorldPos0.x);
            float   halfSpread   = cardSpan * spreadPadding * 0.5f;
            float   riseHeight   = cardSpan * shellRiseFraction;
            StartCoroutine(FireworksSequence(center, halfSpread, riseHeight));
        }

        /// <summary>Stops all running fireworks and destroys any remaining particle systems.</summary>
        public void Stop()
        {
            StopAllCoroutines();
            foreach (GameObject go in _activeSystems)
            {
                if (go != null)
                    Destroy(go);
            }
            _activeSystems.Clear();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Sequence
        // ──────────────────────────────────────────────────────────────────────────

        private IEnumerator FireworksSequence(Vector3 worldCenter, float halfSpread, float riseHeight)
        {
            for (int i = 0; i < burstCount; i++)
            {
                Vector3 launchPos = new Vector3(
                    worldCenter.x + Random.Range(-halfSpread, halfSpread),
                    worldCenter.y + Random.Range(-halfSpread * 0.4f, halfSpread * 0.1f),
                    ParticleZ);

                StartCoroutine(FireShell(launchPos, riseHeight));
                yield return new WaitForSeconds(burstInterval);
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Shell rise
        // ──────────────────────────────────────────────────────────────────────────

        private IEnumerator FireShell(Vector3 launchPos, float worldRiseHeight)
        {
            Vector3 burstPos = launchPos + new Vector3(0f, worldRiseHeight, 0f);

            GameObject shellGO = CreateParticleSystemObject("FW_Shell");
            ParticleSystem shellPS = shellGO.GetComponent<ParticleSystem>();
            ConfigureShellPS(shellPS, launchPos, burstPos);
            Track(shellGO);

            yield return new WaitForSeconds(shellRiseDuration + 0.05f);

            _activeSystems.Remove(shellGO);
            Destroy(shellGO);

            SpawnBurst(burstPos);
        }

        private void ConfigureShellPS(ParticleSystem ps, Vector3 from, Vector3 to)
        {
            float worldRiseH = (to - from).magnitude;
            ps.gameObject.transform.position = from;

            var main = ps.main;
            main.loop            = false;
            main.playOnAwake     = true;
            main.maxParticles    = 40;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(shellRiseDuration * 0.55f, shellRiseDuration * 0.75f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.005f, 0.012f);
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.5f, 0.9f),
                new Color(1f, 0.7f, 0.2f, 0.6f));
            main.gravityModifier = -0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 80;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 5f;
            shape.radius    = 0.02f;

            Vector3 dir = (to - from).normalized;
            ps.gameObject.transform.rotation =
                Quaternion.LookRotation(Vector3.forward, dir) * Quaternion.Euler(-90f, 0f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.World;
            vel.y       = new ParticleSystem.MinMaxCurve(
                worldRiseH / shellRiseDuration * 0.6f,
                worldRiseH / shellRiseDuration * 1.0f);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size    = new ParticleSystem.MinMaxCurve(1f, CurveDecelerate());

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white,               0f),
                    new GradientColorKey(new Color(1f, 0.6f, 0.1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f,   0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0f,   1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            ApplyRenderer(ps, sortingOrder - 1);
            ps.Play();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Burst explosion
        // ──────────────────────────────────────────────────────────────────────────

        private void SpawnBurst(Vector3 position)
        {
            Color colorA = BurstColors[Random.Range(0, BurstColors.Length)];
            Color colorB = BurstColors[Random.Range(0, BurstColors.Length)];

            StartCoroutine(SpawnFlash(position, colorA));

            GameObject burstGO = CreateParticleSystemObject("FW_Burst");
            burstGO.transform.position = position;
            ParticleSystem burstPS = burstGO.GetComponent<ParticleSystem>();
            ConfigureBurstPS(burstPS, colorA, colorB);
            Track(burstGO);

            GameObject glitterGO = new GameObject("FW_Glitter");
            glitterGO.transform.SetParent(burstGO.transform, false);
            glitterGO.transform.localPosition = Vector3.zero;
            ParticleSystem glitterPS = glitterGO.AddComponent<ParticleSystem>();
            ConfigureGlitterPS(glitterPS, colorA);

            var sub = burstPS.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(
                glitterPS,
                ParticleSystemSubEmitterType.Birth,
                ParticleSystemSubEmitterProperties.InheritColor);

            burstPS.Play();

            StartCoroutine(DestroyAfter(burstGO, burstLifetime + trailLifetime + 0.5f));
        }

        private void ConfigureBurstPS(ParticleSystem ps, Color colorA, Color colorB)
        {
            var main = ps.main;
            main.loop            = false;
            main.playOnAwake     = false;
            main.maxParticles    = particlesPerBurst + 20;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(burstLifetime * 0.7f, burstLifetime);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(burstSpeed - burstSpeedVariance, burstSpeed + burstSpeedVariance);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.008f, 0.018f);
            main.startColor      = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = gravityModifier;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (short)particlesPerBurst)
            });

            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Sphere;
            shape.radius          = 0.05f;
            shape.radiusThickness = 0f;

            var limitVel = ps.limitVelocityOverLifetime;
            limitVel.enabled = true;
            limitVel.limit   = new ParticleSystem.MinMaxCurve(1f, CurveDecelerate());
            limitVel.dampen  = 0.15f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size    = new ParticleSystem.MinMaxCurve(1f, CurveDecelerate());

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color   = new ParticleSystem.MinMaxGradient(BuildBurstGradient(colorA));

            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = new ParticleSystem.MinMaxCurve(0.15f);
            noise.frequency   = 0.8f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.2f);
            noise.quality     = ParticleSystemNoiseQuality.Medium;
            noise.damping     = true;

            ApplyRenderer(ps, sortingOrder);
        }

        private void ConfigureGlitterPS(ParticleSystem ps, Color baseColor)
        {
            var main = ps.main;
            main.loop            = false;
            main.playOnAwake     = false;
            main.maxParticles    = particlesPerBurst * trailParticles;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(trailLifetime * 0.5f, trailLifetime);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.startSize       = new ParticleSystem.MinMaxCurve(trailSize * 0.5f, trailSize);
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 0.8f, 1f),
                new Color(baseColor.r, baseColor.g, baseColor.b, 0.85f));
            main.gravityModifier = gravityModifier * 0.7f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (short)trailParticles)
            });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.01f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white,               0f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0.6f),
                    new GradientColorKey(new Color(0.5f, 0.2f, 0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f,   0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0f,   1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size    = new ParticleSystem.MinMaxCurve(1f, CurveDecelerate());

            ApplyRenderer(ps, sortingOrder + 1);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Flash
        // ──────────────────────────────────────────────────────────────────────────

        private IEnumerator SpawnFlash(Vector3 position, Color color)
        {
            GameObject flashGO = CreateParticleSystemObject("FW_Flash");
            flashGO.transform.position = position;
            Track(flashGO);

            ParticleSystem flashPS = flashGO.GetComponent<ParticleSystem>();

            var main = flashPS.main;
            main.loop            = false;
            main.playOnAwake     = false;
            main.maxParticles    = 1;
            main.startLifetime   = 0.18f;
            main.startSpeed      = 0f;
            main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
            main.startColor      = Color.white;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = flashPS.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var shape = flashPS.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0f;

            var col = flashPS.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(color,       0.5f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var sol = flashPS.sizeOverLifetime;
            sol.enabled = true;
            sol.size    = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f,   0.3f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f,   1.8f)));

            ApplyRenderer(flashPS, sortingOrder + 2);
            flashPS.Play();

            yield return new WaitForSeconds(0.25f);
            _activeSystems.Remove(flashGO);
            Destroy(flashGO);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private GameObject CreateParticleSystemObject(string goName)
        {
            var go = new GameObject(goName);
            go.AddComponent<ParticleSystem>();
            return go;
        }

        /// <summary>
        /// Builds the shared particle material once in Awake.
        /// Uses Sprites/Default which is always available in URP and Built-in.
        /// </summary>
        private static Material BuildMaterial()
        {
            return new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.white
            };
        }

        private void ApplyRenderer(ParticleSystem ps, int order)
        {
            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode       = ParticleSystemRenderMode.Billboard;
            r.sortingLayerName = "Default";
            r.sortingOrder     = order;
            r.material         = _cachedMaterial;
        }

        private void Track(GameObject go) => _activeSystems.Add(go);

        private IEnumerator DestroyAfter(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (go != null)
            {
                _activeSystems.Remove(go);
                Destroy(go);
            }
        }

        private static AnimationCurve CurveDecelerate()
        {
            return new AnimationCurve(
                new Keyframe(0f,   1f,  0f,   -2f),
                new Keyframe(0.6f, 0.4f),
                new Keyframe(1f,   0f, -0.5f,  0f));
        }

        private static Gradient BuildBurstGradient(Color color)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white,  0f),
                    new GradientColorKey(color,        0.15f),
                    new GradientColorKey(color * 0.6f, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                });
            return g;
        }
    }
}
