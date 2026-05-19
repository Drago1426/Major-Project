using System.Collections;
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
    public float fireballDistance = 0.38f;
    public float fireballTravelTime = 0.32f;

    [Header("Bounce")]
    public float bounceScale = 1.15f;
    public float bounceTime = 0.12f;

    Vector3 _baseScale;
    Coroutine _bounceRoutine;
    Coroutine _audioLoadRoutine;
    ParticleSystem[] _defaultTapVfx;
    AudioClip[] _proceduralTapSfx;

    void Awake()
    {
        _baseScale = transform.localScale;

        if (createDefaultFeedbackIfMissing)
            EnsureDefaultFeedback();

        LoadConfiguredFireballClip();
    }

    void OnEnable()
    {
        LoadConfiguredFireballClip();
    }

    public void OnTapped()
    {
        OnTapped(transform.position);
    }

    public void OnTapped(Vector3 hitWorldPosition)
    {
        if (shootFireballOnTap)
            FireballProjectileVfx.Play(transform, hitWorldPosition, fireballDistance, fireballTravelTime);

        PlayRandomParticle(hitWorldPosition);
        PlayRandomSound();

        if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
        _bounceRoutine = StartCoroutine(Bounce());

        var combat = GetComponent<CreatureCombat>();
        if (combat != null)
            combat.TryAttack();
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

        AudioClip clip = Pick(randomFireballSfx);

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
}
