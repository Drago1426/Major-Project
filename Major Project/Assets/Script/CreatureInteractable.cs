using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureInteractable : MonoBehaviour
{
    [Header("Feedback")]
    public ParticleSystem tapVfx;
    public ParticleSystem[] randomTapVfx;
    public AudioSource audioSource;
    public AudioClip tapSfx;
    public AudioClip[] randomTapSfx;
    public AudioClip effectSfx;
    public AudioClip[] randomEffectSfx;
    public string effectSfxPath;
    public bool useProceduralTapSfx = true;

    [Header("Rule Interaction")]
    public bool requireSpellCardToInteract = false;
    public Color armedGlowColor = new(1f, 0.82f, 0.05f, 1f);
    public float armedGlowIntensity = 2.5f;

    [Header("Bounce")]
    public float bounceScale = 1.15f;
    public float bounceTime = 0.12f;

    Vector3 baseScale;
    Coroutine bounceRoutine;
    Coroutine audioLoadRoutine;
    AudioClip armedEffectSfx;
    bool spellArmed;
    string armedByCardName;
    Renderer[] renderers;
    Color[] originalColors;
    AudioClip[] proceduralTapSfx;

    static readonly List<CreatureInteractable> ActiveCreatures = new();

    void Awake()
    {
        baseScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null && renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }

        LoadConfiguredEffectClip();
    }

    void OnEnable()
    {
        RegisterActiveCreature(this);
        LoadConfiguredEffectClip();
        SetArmedGlow(spellArmed);
    }

    void OnDisable()
    {
        UnregisterActiveCreature(this);
        DisarmSpellInteraction();
    }

    public void OnTapped()
    {
        OnTapped(transform.position);
    }

    public void OnTapped(Vector3 hitWorldPosition)
    {
        if (requireSpellCardToInteract && !spellArmed)
            return;

        bool consumedSpell = spellArmed;
        spellArmed = false;

        PlayParticle(hitWorldPosition);
        PlaySound();

        if (bounceRoutine != null)
            StopCoroutine(bounceRoutine);
        bounceRoutine = StartCoroutine(Bounce());

        var combat = GetComponent<CreatureCombat>();
        if (combat != null)
            combat.TryAttack();

        if (consumedSpell)
            SetArmedGlow(false);
    }

    public void SetEffectSfxPath(string soundPath)
    {
        effectSfxPath = string.IsNullOrWhiteSpace(soundPath) ? string.Empty : soundPath.Trim();
        LoadConfiguredEffectClip();
    }

    public void ArmSpellInteraction(string sourceCardName, string overrideSfxPath)
    {
        spellArmed = true;
        armedByCardName = string.IsNullOrWhiteSpace(sourceCardName) ? "Spell" : sourceCardName.Trim();
        armedEffectSfx = null;

        string armedSfxPath = string.IsNullOrWhiteSpace(overrideSfxPath) ? string.Empty : overrideSfxPath.Trim();
        if (!string.IsNullOrWhiteSpace(armedSfxPath))
            StartCoroutine(RuntimeAudioClipLoader.Load(armedSfxPath, clip => armedEffectSfx = clip));

        SetArmedGlow(true);
        Debug.Log($"Creature '{name}' armed by spell '{armedByCardName}'.", this);
    }

    public void DisarmSpellInteraction()
    {
        spellArmed = false;
        armedByCardName = string.Empty;
        armedEffectSfx = null;
        SetArmedGlow(false);
    }

    public static bool TryArmFirstActiveSpell(string sourceCardName, string overrideSfxPath)
    {
        CleanupActiveCreatures();

        for (int i = ActiveCreatures.Count - 1; i >= 0; i--)
        {
            var creature = ActiveCreatures[i];
            if (creature == null || !creature.isActiveAndEnabled || !creature.gameObject.activeInHierarchy)
                continue;

            creature.ArmSpellInteraction(sourceCardName, overrideSfxPath);
            return true;
        }

        return false;
    }

    void PlayParticle(Vector3 hitWorldPosition)
    {
        ParticleSystem effect = Pick(randomTapVfx);
        if (effect == null)
            effect = tapVfx;
        if (effect == null)
            return;

        effect.transform.position = hitWorldPosition;
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
    }

    void PlaySound()
    {
        AudioClip clip = armedEffectSfx;
        if (clip == null)
            clip = Pick(randomEffectSfx);
        if (clip == null)
            clip = effectSfx;
        if (clip == null)
            clip = Pick(randomTapSfx);
        if (clip == null)
            clip = tapSfx;
        if (clip == null && useProceduralTapSfx)
            clip = Pick(ProceduralTapSfx());
        if (clip == null)
            return;

        AudioSource source = EnsureAudioSource();
        source.PlayOneShot(clip);
    }

    void LoadConfiguredEffectClip()
    {
        if (string.IsNullOrWhiteSpace(effectSfxPath) || !isActiveAndEnabled)
            return;

        if (audioLoadRoutine != null)
            StopCoroutine(audioLoadRoutine);

        audioLoadRoutine = StartCoroutine(RuntimeAudioClipLoader.Load(effectSfxPath, clip =>
        {
            effectSfx = clip;
            audioLoadRoutine = null;
        }));
    }

    AudioSource EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = 0.85f;
        }

        return audioSource;
    }

    IEnumerator Bounce()
    {
        float halfTime = Mathf.Max(0.01f, bounceTime);
        Vector3 targetScale = baseScale * Mathf.Max(1f, bounceScale);

        yield return ScaleOverTime(baseScale, targetScale, halfTime);
        yield return ScaleOverTime(targetScale, baseScale, halfTime);
    }

    IEnumerator ScaleOverTime(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.localScale = to;
    }

    void SetArmedGlow(bool enabled)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.material == null || !renderer.material.HasProperty("_Color"))
                continue;

            renderer.material.color = enabled
                ? Color.Lerp(originalColors[i], armedGlowColor * Mathf.Max(1f, armedGlowIntensity), 0.35f)
                : originalColors[i];
        }
    }

    AudioClip[] ProceduralTapSfx()
    {
        if (proceduralTapSfx != null && proceduralTapSfx.Length > 0)
            return proceduralTapSfx;

        proceduralTapSfx = new[]
        {
            CreateProceduralClick("Creature Tap A", 0.08f, 220f),
            CreateProceduralClick("Creature Tap B", 0.1f, 300f)
        };
        return proceduralTapSfx;
    }

    AudioClip CreateProceduralClick(string clipName, float duration, float frequency)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float fade = 1f - (i / (float)sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * fade * 0.35f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static T Pick<T>(IReadOnlyList<T> options) where T : class
    {
        if (options == null || options.Count == 0)
            return null;

        return options[Random.Range(0, options.Count)];
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

    static void CleanupActiveCreatures()
    {
        for (int i = ActiveCreatures.Count - 1; i >= 0; i--)
        {
            if (ActiveCreatures[i] == null)
                ActiveCreatures.RemoveAt(i);
        }
    }
}
