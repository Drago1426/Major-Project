using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureInteractable : MonoBehaviour
{
    [Header("Feedback")]
    public ParticleSystem tapVfx;       // optional fallback burst
    public ParticleSystem[] randomTapVfx;
    public AudioSource audioSource;     // optional
    public AudioClip tapSfx;            // optional fallback sound
    public AudioClip[] randomTapSfx;
    public AudioClip fireballSfx;
    public AudioClip[] randomFireballSfx;
    public string fireballSfxPath;
    public bool createDefaultFeedbackIfMissing = true;
    public bool useProceduralTapSfx = true;

    [Header("Fireball")]
    public bool shootFireballOnTap = true;
    public bool requireFireballCardToInteract = true;
    public float fireballDistance = 0.38f;
    public float fireballTravelTime = 0.32f;
    public Color armedGlowColor = new Color(1f, 0.82f, 0.05f, 1f);
    public float armedGlowIntensity = 2.5f;
    [Range(0f, 1f)] public float armedGlowTint = 0.35f;

    [Header("Bounce")]
    public float bounceScale = 1.15f;
    public float bounceTime = 0.12f;

    Vector3 _baseScale;
    Coroutine _bounceRoutine;
    Coroutine _audioLoadRoutine;
    Coroutine _glowOffRoutine;
    GameObject _armedGlowObject;
    ParticleSystem _armedGlowParticles;
    Light _armedGlowLight;
    ParticleSystem[] _defaultTapVfx;
    AudioClip[] _proceduralTapSfx;
    AudioClip _armedFireballSfx;
    bool _fireballArmed;
    string _armedByCardName;
    readonly List<MaterialGlowState> _glowStates = new();
    static readonly List<CreatureInteractable> ActiveCreatures = new();

    void Awake()
    {
        _baseScale = transform.localScale;

        if (createDefaultFeedbackIfMissing)
            EnsureDefaultFeedback();

        LoadConfiguredFireballClip();
    }

    void OnEnable()
    {
        RegisterActiveCreature(this);
        LoadConfiguredFireballClip();

        if (_fireballArmed)
            SetArmedGlow(true);
    }

    void OnDisable()
    {
        UnregisterActiveCreature(this);
        DisarmFireballInteraction(false);
    }

    public void OnTapped()
    {
        OnTapped(transform.position);
    }

    public void OnTapped(Vector3 hitWorldPosition)
    {
        if (requireFireballCardToInteract && !_fireballArmed)
            return;

        bool consumedArmedFireball = _fireballArmed;
        _fireballArmed = false;

        if (shootFireballOnTap)
            FireballProjectileVfx.Play(transform, hitWorldPosition, fireballDistance, fireballTravelTime);

        PlayRandomParticle(hitWorldPosition);
        PlayRandomSound();

        if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
        _bounceRoutine = StartCoroutine(Bounce());

        var combat = GetComponent<CreatureCombat>();
        if (combat != null)
            combat.TryAttack();

        if (consumedArmedFireball)
            DisableGlowAfterShot();
    }

    void PlayRandomParticle(Vector3 hitWorldPosition)
    {
        ParticleSystem effect = Pick(randomTapVfx);

        if (effect == null)
            effect = Pick(_defaultTapVfx);

        if (effect == null)
            effect = tapVfx;

        if (effect == null)
            return;

        effect.transform.position = hitWorldPosition;
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
    }

    void PlayRandomSound()
    {
        if (audioSource == null)
            return;

        AudioClip clip = _armedFireballSfx;

        if (clip == null)
            clip = Pick(randomFireballSfx);

        if (clip == null)
            clip = fireballSfx;

        if (clip == null)
            clip = Pick(randomTapSfx);

        if (clip == null)
            clip = Pick(_proceduralTapSfx);

        if (clip == null)
            clip = tapSfx;

        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void SetFireballSfxPath(string soundPath)
    {
        fireballSfxPath = string.IsNullOrWhiteSpace(soundPath) ? string.Empty : soundPath.Trim();
        LoadConfiguredFireballClip();
    }

    public void ArmFireballInteraction(string sourceCardName, string overrideSfxPath)
    {
        _fireballArmed = true;
        _armedByCardName = string.IsNullOrWhiteSpace(sourceCardName) ? "Fireball" : sourceCardName.Trim();
        SetArmedGlow(true);

        _armedFireballSfx = null;
        string armedFireballSfxPath = string.IsNullOrWhiteSpace(overrideSfxPath) ? string.Empty : overrideSfxPath.Trim();
        if (!string.IsNullOrWhiteSpace(armedFireballSfxPath))
            StartCoroutine(RuntimeAudioClipLoader.Load(armedFireballSfxPath, clip => _armedFireballSfx = clip));

        Debug.Log($"'{gameObject.name}' armed by {_armedByCardName}. Click the model to fire.", this);
    }

    public void DisarmFireballInteraction(bool keepGlowUntilShotEnds)
    {
        _fireballArmed = false;
        _armedByCardName = string.Empty;
        _armedFireballSfx = null;
        if (keepGlowUntilShotEnds)
            DisableGlowAfterShot();
        else
            SetArmedGlow(false);
    }

    public static bool TryArmFirstActiveFireball(string sourceCardName, string overrideSfxPath)
    {
        for (int i = ActiveCreatures.Count - 1; i >= 0; i--)
        {
            var creature = ActiveCreatures[i];
            if (creature == null)
            {
                ActiveCreatures.RemoveAt(i);
                continue;
            }

            if (!creature.isActiveAndEnabled || !creature.gameObject.activeInHierarchy)
                continue;

            creature.ArmFireballInteraction(sourceCardName, overrideSfxPath);
            return true;
        }

        return false;
    }

    static void RegisterActiveCreature(CreatureInteractable creature)
    {
        if (creature != null && !ActiveCreatures.Contains(creature))
            ActiveCreatures.Add(creature);
    }

    static void UnregisterActiveCreature(CreatureInteractable creature)
    {
        ActiveCreatures.Remove(creature);
    }

    void LoadConfiguredFireballClip()
    {
        if (string.IsNullOrWhiteSpace(fireballSfxPath) || !isActiveAndEnabled)
            return;

        if (_audioLoadRoutine != null)
            StopCoroutine(_audioLoadRoutine);

        _audioLoadRoutine = StartCoroutine(RuntimeAudioClipLoader.Load(fireballSfxPath, clip =>
        {
            fireballSfx = clip;
            _audioLoadRoutine = null;
        }));
    }

    void DisableGlowAfterShot()
    {
        if (_glowOffRoutine != null)
            StopCoroutine(_glowOffRoutine);

        _glowOffRoutine = StartCoroutine(DisableGlowAfterDelay(Mathf.Max(0.05f, fireballTravelTime)));
    }

    IEnumerator DisableGlowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetArmedGlow(false);
        _glowOffRoutine = null;
    }

    void SetArmedGlow(bool enabled)
    {
        if (enabled)
        {
            ApplyArmedGlow();
            return;
        }

        RestoreGlow();
    }

    void ApplyArmedGlow()
    {
        EnsureArmedGlowObject();
        SetVisibleArmedGlow(true);

        if (_glowStates.Count > 0)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer || IsStatsDisplayRenderer(renderer))
                continue;

            var materials = renderer.materials;
            for (int j = 0; j < materials.Length; j++)
            {
                var material = materials[j];
                if (material == null)
                    continue;

                var state = new MaterialGlowState(material);
                _glowStates.Add(state);

                Color baseColor = state.CurrentBaseColor;
                state.SetBaseColor(Color.Lerp(baseColor, armedGlowColor, armedGlowTint));
                state.SetEmission(armedGlowColor * Mathf.Max(0f, armedGlowIntensity));
            }
        }
    }

    void RestoreGlow()
    {
        if (_glowOffRoutine != null)
        {
            StopCoroutine(_glowOffRoutine);
            _glowOffRoutine = null;
        }

        for (int i = 0; i < _glowStates.Count; i++)
            _glowStates[i].Restore();

        _glowStates.Clear();
        SetVisibleArmedGlow(false);
    }

    void EnsureArmedGlowObject()
    {
        if (_armedGlowObject != null)
            return;

        Bounds bounds = CalculateVisualBounds();
        float radius = Mathf.Max(0.035f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.15f);
        float height = Mathf.Max(0.04f, bounds.size.y);

        _armedGlowObject = new GameObject("Fireball Armed Glow");
        _armedGlowObject.transform.SetParent(transform, false);
        _armedGlowObject.transform.localPosition = transform.InverseTransformPoint(bounds.center);
        _armedGlowObject.transform.localRotation = Quaternion.identity;
        _armedGlowObject.transform.localScale = Vector3.one;

        _armedGlowLight = _armedGlowObject.AddComponent<Light>();
        _armedGlowLight.type = LightType.Point;
        _armedGlowLight.color = armedGlowColor;
        _armedGlowLight.intensity = Mathf.Max(0.25f, armedGlowIntensity * 0.65f);
        _armedGlowLight.range = Mathf.Max(0.18f, height * 2.2f);

        _armedGlowParticles = CreateArmedGlowParticles(_armedGlowObject.transform, radius, height);
        SetVisibleArmedGlow(false);
    }

    ParticleSystem CreateArmedGlowParticles(Transform parent, float radius, float height)
    {
        var system = parent.gameObject.AddComponent<ParticleSystem>();
        var main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.018f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.08f, 0.55f),
            new Color(1f, 0.98f, 0.28f, 0.85f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 80f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)32) });

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = radius;
        shape.donutRadius = Mathf.Max(0.004f, radius * 0.18f);
        shape.randomDirectionAmount = 0.35f;

        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = Mathf.Max(0.025f, height * 0.18f);
        velocity.z = 0f;
        velocity.orbitalX = 0f;
        velocity.orbitalY = 4.5f;
        velocity.orbitalZ = 0f;
        velocity.radial = 0.01f;

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(MakeArmedGlowGradient());

        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.25f, 1.2f),
            new Keyframe(1f, 0f)));

        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.material = RuntimeParticleAssets.CreateParticleMaterial(
            "Runtime Fireball Armed Glow Particle",
            RuntimeParticleAssets.SoftDiscTexture,
            Color.white);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 2f;

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return system;
    }

    void SetVisibleArmedGlow(bool visible)
    {
        if (_armedGlowObject == null)
            return;

        _armedGlowObject.SetActive(visible);

        if (_armedGlowLight != null)
            _armedGlowLight.enabled = visible;

        if (_armedGlowParticles == null)
            return;

        if (visible)
        {
            _armedGlowParticles.Clear(true);
            _armedGlowParticles.Play(true);
        }
        else
        {
            _armedGlowParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    Bounds CalculateVisualBounds()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one * 0.08f);

        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer || IsStatsDisplayRenderer(renderer))
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    bool IsStatsDisplayRenderer(Renderer renderer)
    {
        if (renderer == null)
            return false;

        if (renderer.GetComponent<TextMesh>() != null)
            return true;

        if (renderer is SpriteRenderer && HasParentNamed(renderer.transform, "Card Stats Display"))
            return true;

        return false;
    }

    bool HasParentNamed(Transform current, string objectName)
    {
        while (current != null)
        {
            if (current.name == objectName)
                return true;

            current = current.parent;
        }

        return false;
    }

    Gradient MakeArmedGlowGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.92f, 0.12f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.02f), 0.7f),
                new GradientColorKey(new Color(1f, 0.15f, 0.01f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.18f),
                new GradientAlphaKey(0.65f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    T Pick<T>(T[] options) where T : Object
    {
        if (options == null || options.Length == 0)
            return null;

        int startIndex = Random.Range(0, options.Length);
        for (int i = 0; i < options.Length; i++)
        {
            T option = options[(startIndex + i) % options.Length];
            if (option != null)
                return option;
        }

        return null;
    }

    void EnsureDefaultFeedback()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = 0.8f;
        }

        if (useProceduralTapSfx && NoRandomSoundsConfigured())
        {
            var whooshA = CreateProceduralWhoosh("Creature Fireball Whoosh A", 0.42f, 150f, 660f);
            var whooshB = CreateProceduralWhoosh("Creature Fireball Whoosh B", 0.32f, 220f, 920f);
            var crackle = CreateProceduralWhoosh("Creature Fireball Crackle", 0.24f, 380f, 1100f);

            _proceduralTapSfx = tapSfx == null
                ? new[] { whooshA, whooshB, crackle }
                : new[] { tapSfx, whooshA, whooshB, crackle };
        }

        if (NoRandomParticlesConfigured())
        {
            var fireBurst = CreateBurstSystem("Fireball Impact Burst", new Color(1f, 0.26f, 0.04f, 1f), new Color(1f, 0.85f, 0.18f, 1f), 28, 0.65f, 0.08f);
            var emberRing = CreateBurstSystem("Fireball Ember Ring", new Color(1f, 0.55f, 0.05f, 1f), new Color(1f, 0.12f, 0.02f, 1f), 38, 0.45f, 0.12f);
            var smokePuff = CreateBurstSystem("Fireball Smoke Puff", new Color(0.22f, 0.18f, 0.14f, 0.35f), new Color(0.45f, 0.32f, 0.22f, 0.28f), 18, 0.8f, 0.05f);

            _defaultTapVfx = tapVfx == null
                ? new[] { fireBurst, emberRing, smokePuff }
                : new[] { tapVfx, fireBurst, emberRing, smokePuff };
        }
    }

    bool NoRandomSoundsConfigured()
    {
        return randomTapSfx == null || randomTapSfx.Length == 0;
    }

    bool NoRandomParticlesConfigured()
    {
        return randomTapVfx == null || randomTapVfx.Length == 0;
    }

    ParticleSystem CreateBurstSystem(string systemName, Color startColor, Color endColor, short burstCount, float lifetime, float radius)
    {
        var go = new GameObject(systemName);
        go.transform.SetParent(transform, false);

        var system = go.AddComponent<ParticleSystem>();
        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.25f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.02f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(startColor, endColor);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.randomDirectionAmount = 0.75f;

        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = 0f;
        velocity.y = 0.08f;
        velocity.z = 0f;
        velocity.radial = 0.12f;

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(MakeFadeGradient(startColor, endColor));

        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial();
        renderer.sortingFudge = 1f;

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return system;
    }

    Material CreateParticleMaterial()
    {
        return RuntimeParticleAssets.CreateParticleMaterial(
            "Runtime Creature Interaction Particle",
            RuntimeParticleAssets.SoftDiscTexture,
            Color.white,
            false);
    }

    Gradient MakeFadeGradient(Color startColor, Color endColor)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(endColor, 0.65f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(startColor.a, 0.12f),
                new GradientAlphaKey(endColor.a, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    AudioClip CreateProceduralWhoosh(string clipName, float duration, float startFrequency, float endFrequency)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        var samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float p = i / (float)sampleCount;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, 1f - p);
            phase += frequency * Mathf.PI * 2f / sampleRate;

            float envelope = Mathf.Sin(Mathf.Clamp01(p) * Mathf.PI);
            float crackle = Random.value > 0.82f ? Random.Range(-0.18f, 0.18f) : 0f;
            samples[i] = (Mathf.Sin(phase) * 0.2f + crackle) * envelope * 0.55f;
        }

        var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    IEnumerator Bounce()
    {
        Vector3 up = _baseScale * bounceScale;

        // scale up
        float t = 0f;
        while (t < bounceTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / bounceTime);
            transform.localScale = Vector3.Lerp(_baseScale, up, p);
            yield return null;
        }

        // scale back
        t = 0f;
        while (t < bounceTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / bounceTime);
            transform.localScale = Vector3.Lerp(up, _baseScale, p);
            yield return null;
        }

        transform.localScale = _baseScale;
        _bounceRoutine = null;
    }

    readonly struct MaterialGlowState
    {
        readonly Material material;
        readonly bool hasBaseColor;
        readonly bool hasColor;
        readonly bool hasEmission;
        readonly Color baseColor;
        readonly Color color;
        readonly Color emissionColor;

        public Color CurrentBaseColor
        {
            get
            {
                if (material == null)
                    return Color.white;

                if (hasBaseColor)
                    return material.GetColor("_BaseColor");

                return hasColor ? material.color : Color.white;
            }
        }

        public MaterialGlowState(Material sourceMaterial)
        {
            material = sourceMaterial;
            hasBaseColor = material != null && material.HasProperty("_BaseColor");
            hasColor = material != null && material.HasProperty("_Color");
            hasEmission = material != null && material.HasProperty("_EmissionColor");
            baseColor = hasBaseColor ? material.GetColor("_BaseColor") : Color.white;
            color = hasColor ? material.color : Color.white;
            emissionColor = hasEmission ? material.GetColor("_EmissionColor") : Color.black;
        }

        public void SetBaseColor(Color newColor)
        {
            if (material == null)
                return;

            if (hasBaseColor)
                material.SetColor("_BaseColor", newColor);
            else if (hasColor)
                material.color = newColor;
        }

        public void SetEmission(Color newEmission)
        {
            if (material == null || !hasEmission)
                return;

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", newEmission);
        }

        public void Restore()
        {
            if (material == null)
                return;

            if (hasBaseColor)
                material.SetColor("_BaseColor", baseColor);

            if (hasColor)
                material.color = color;

            if (hasEmission)
                material.SetColor("_EmissionColor", emissionColor);
        }
    }
}
