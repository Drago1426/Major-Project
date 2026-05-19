using System.Collections;
using UnityEngine;

public class SummonOnTargetFound : MonoBehaviour
{
    [Header("References")]
    public GameObject creature;          // your 3D model root
    public ParticleSystem summonVfx;
    public FireTornadoSummonVfx fireTornadoVfx;
    public AudioSource summonSfx;         // optional sound
    public AudioClip summonSfxClip;
    public string summonSfxPath;
    public Animator creatureAnimator;

    [Header("Animation Trigger")]
    public bool triggerCreatureAnimation = true;
    public string summonTriggerName = "Summon";

    [Header("Motion")]
    public Vector3 startLocalPos = new Vector3(0f, -0.05f, 0f);
    public Vector3 endLocalPos   = new Vector3(0f,  0.00f, 0f);
    public float riseDuration = 0.6f;

    [Header("Behaviour")]
    public bool hideOnTargetLost = false;
    public bool useSingleVisibleCreatureLock = true;
    public bool createFireTornadoIfMissing = true;
    public float fireTornadoStopDelay = 0.15f;

    Coroutine _routine;
    Coroutine _audioLoadRoutine;
    bool _hasSummoned;

    void Awake()
    {
        if (creature != null)
        {
            creature.transform.localPosition = startLocalPos;
            creature.SetActive(false);
        }

        if (createFireTornadoIfMissing && fireTornadoVfx == null)
            fireTornadoVfx = FireTornadoSummonVfx.Ensure(transform);

        PositionFireTornado();
        if (fireTornadoVfx != null)
            fireTornadoVfx.Stop(true);

        LoadConfiguredSummonClip();
    }

    // Call this from Vuforia "On Target Found"
    public void OnFound()
    {
        if (creature == null) return;

        // Prevent re-triggering every frame while tracked
        if (_hasSummoned) return;
        if (useSingleVisibleCreatureLock && !SummonVisibilityLock.TryClaim(this))
            return;

        _hasSummoned = true;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(SummonRoutine());
    }

    // Call this from Vuforia "On Target Lost"
    public void OnLost()
    {
        if (useSingleVisibleCreatureLock)
            SummonVisibilityLock.Release(this);

        if (_routine != null) StopCoroutine(_routine);
        _routine = null;

        if (fireTornadoVfx != null)
            fireTornadoVfx.Stop(true);

        if (!hideOnTargetLost)
        {
            if (creature != null && creature.activeInHierarchy)
            {
                creature.transform.localPosition = endLocalPos;
                _hasSummoned = true;
            }
            else
            {
                _hasSummoned = false;
            }

            return;
        }

        _hasSummoned = false;
        if (creature == null) return;

        creature.SetActive(false);
        creature.transform.localPosition = startLocalPos;
    }

    void OnDisable()
    {
        if (useSingleVisibleCreatureLock)
            SummonVisibilityLock.Release(this);
    }

    public void SetSummonSfxPath(string soundPath)
    {
        summonSfxPath = string.IsNullOrWhiteSpace(soundPath) ? string.Empty : soundPath.Trim();
        LoadConfiguredSummonClip();
    }

    void LoadConfiguredSummonClip()
    {
        if (string.IsNullOrWhiteSpace(summonSfxPath) || !isActiveAndEnabled)
            return;

        if (_audioLoadRoutine != null)
            StopCoroutine(_audioLoadRoutine);

        _audioLoadRoutine = StartCoroutine(RuntimeAudioClipLoader.Load(summonSfxPath, clip =>
        {
            summonSfxClip = clip;
            _audioLoadRoutine = null;
        }));
    }

    void PlaySummonSound()
    {
        if (summonSfxClip != null)
        {
            AudioSource source = EnsureSummonAudioSource();
            if (source != null)
                source.PlayOneShot(summonSfxClip);

            return;
        }

        if (summonSfx != null)
            summonSfx.Play();
    }

    AudioSource EnsureSummonAudioSource()
    {
        if (summonSfx == null)
        {
            summonSfx = gameObject.AddComponent<AudioSource>();
            summonSfx.playOnAwake = false;
            summonSfx.spatialBlend = 1f;
            summonSfx.volume = 0.85f;
        }

        return summonSfx;
    }

    IEnumerator SummonRoutine()
    {
        // VFX + SFX first
        PositionFireTornado();
        if (fireTornadoVfx != null) fireTornadoVfx.Play();
        if (summonVfx != null) summonVfx.Play();
        PlaySummonSound();

        creature.transform.localPosition = startLocalPos;
        creature.SetActive(true);
        if (triggerCreatureAnimation && creatureAnimator != null && !string.IsNullOrWhiteSpace(summonTriggerName))
            creatureAnimator.SetTrigger(summonTriggerName);

        float t = 0f;
        while (t < riseDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / riseDuration);

            // Smooth rise
            float smooth = p * p * (3f - 2f * p);

            creature.transform.localPosition = Vector3.Lerp(startLocalPos, endLocalPos, smooth);
            yield return null;
        }

        creature.transform.localPosition = endLocalPos;
        if (fireTornadoVfx != null)
        {
            yield return new WaitForSeconds(fireTornadoStopDelay);
            fireTornadoVfx.Stop();
        }

        _routine = null;
    }

    void PositionFireTornado()
    {
        if (fireTornadoVfx == null)
            return;

        fireTornadoVfx.transform.SetParent(transform, false);
        fireTornadoVfx.transform.localPosition = new Vector3(endLocalPos.x, 0f, endLocalPos.z);
        fireTornadoVfx.transform.localRotation = Quaternion.identity;
    }
}
