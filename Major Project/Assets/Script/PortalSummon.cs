using System.Collections;
using UnityEngine;
using Vuforia;

public class PortalSummon : MonoBehaviour
{
    [Header("References")]
    public Transform creatureRoot;          // CreatureRoot (enable/disable this)
    public GameObject summonCircle;         // SummonCircle Quad
    public Renderer circleRenderer;         // Mesh Renderer on SummonCircle
    public ParticleSystem fireVfx;          // FireVFX (child of CreatureRoot)
    public FireTornadoSummonVfx fireTornadoVfx;

    [Header("Creature Movement")]
    public Vector3 creatureStartLocalPos = new Vector3(0f, -0.05f, 0f);
    public Vector3 creatureEndLocalPos   = new Vector3(0f,  0.00f, 0f);
    public float creatureRiseTime = 0.7f;

    [Header("Timing")]
    public float runeOpenTime = 0.25f;
    public float waitBeforeFire = 1.0f;
    public float waitBeforeDragon = 0.1f;
    public float runeFadeTime = 0.4f;
    public float fireStopDelay = 0.2f;

    [Header("Rune Scale")]
    public float runeStartScale = 0.001f;
    public float runeEndScale = 0.055f; // good for ~6.3cm target

    [Header("Rune Spin (optional)")]
    public bool spinRune = true;
    public float spinSpeed = 90f;

    [Header("Behaviour")]
    public bool useSingleVisibleCreatureLock = true;
    public bool createFireTornadoIfMissing = true;
    public bool treatExtendedTrackedAsFound = false;

    Coroutine routine;
    bool hasSummoned;
    bool wasTracked;
    ObserverBehaviour observerBehaviour;

    // URP base color property (works for URP Unlit/Lit)
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    Material _circleMat;

    void Awake()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();

        if (circleRenderer != null)
            _circleMat = circleRenderer.material; // one instance

        if (creatureRoot != null)
        {
            creatureRoot.localPosition = creatureStartLocalPos;
            creatureRoot.gameObject.SetActive(false);
        }

        if (summonCircle != null)
        {
            summonCircle.transform.localScale = Vector3.one * runeStartScale;
            summonCircle.SetActive(false);
        }

        if (fireVfx != null)
            fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (createFireTornadoIfMissing && fireTornadoVfx == null)
            fireTornadoVfx = FireTornadoSummonVfx.Ensure(transform);

        PositionFireTornado();
        if (fireTornadoVfx != null)
            fireTornadoVfx.Stop(true);
    }

    void OnEnable()
    {
        if (observerBehaviour != null)
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    public void OnFound()
    {
        if (hasSummoned || creatureRoot == null) return;
        if (useSingleVisibleCreatureLock && !SummonVisibilityLock.TryClaim(this))
            return;

        hasSummoned = true;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SummonRoutine());
    }

    public void OnLost()
    {
        if (useSingleVisibleCreatureLock)
            SummonVisibilityLock.Release(this);

        hasSummoned = false;
        if (routine != null) StopCoroutine(routine);
        routine = null;

        if (fireVfx != null)
            fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (fireTornadoVfx != null)
            fireTornadoVfx.Stop(true);

        if (summonCircle != null)
            summonCircle.SetActive(false);

        if (creatureRoot != null)
        {
            creatureRoot.gameObject.SetActive(false);
            creatureRoot.localPosition = creatureStartLocalPos;
        }
    }

    void OnDisable()
    {
        if (observerBehaviour != null)
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;

        if (useSingleVisibleCreatureLock)
            SummonVisibilityLock.Release(this);
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isTracked = status.Status == Status.TRACKED ||
            (treatExtendedTrackedAsFound && status.Status == Status.EXTENDED_TRACKED);

        if (isTracked && !wasTracked)
        {
            wasTracked = true;
            OnFound();
        }
        else if (!isTracked && wasTracked)
        {
            wasTracked = false;
            OnLost();
        }
    }

    IEnumerator SummonRoutine()
    {
        // 1) Rune appears + opens
        if (summonCircle != null)
        {
            summonCircle.SetActive(true);
            summonCircle.transform.localScale = Vector3.one * runeStartScale;
        }

        SetRuneAlpha(1f);
        yield return ScaleRune(runeStartScale, runeEndScale, runeOpenTime);

        // 2) Wait 1 second (dramatic pause)
        yield return new WaitForSeconds(waitBeforeFire);

        // 3) Fire starts
        PositionFireTornado();
        if (fireTornadoVfx != null) fireTornadoVfx.Play();
        if (fireVfx != null) fireVfx.Play();

        // 4) After a short delay, dragon appears and rises
        yield return new WaitForSeconds(waitBeforeDragon);

        creatureRoot.localPosition = creatureStartLocalPos;
        creatureRoot.gameObject.SetActive(true);

        yield return MoveCreature(creatureStartLocalPos, creatureEndLocalPos, creatureRiseTime);

        // 5) Stop fire + fade rune out
        yield return new WaitForSeconds(fireStopDelay);

        if (fireVfx != null)
            fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (fireTornadoVfx != null)
            fireTornadoVfx.Stop();

        yield return FadeRune(1f, 0f, runeFadeTime);

        if (summonCircle != null) summonCircle.SetActive(false);
        routine = null;
    }

    IEnumerator MoveCreature(Vector3 from, Vector3 to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float smooth = p * p * (3f - 2f * p);
            creatureRoot.localPosition = Vector3.Lerp(from, to, smooth);
            yield return null;
        }
        creatureRoot.localPosition = to;
    }

    void PositionFireTornado()
    {
        if (fireTornadoVfx == null)
            return;

        fireTornadoVfx.transform.SetParent(transform, false);
        fireTornadoVfx.transform.localPosition = new Vector3(creatureEndLocalPos.x, 0f, creatureEndLocalPos.z);
        fireTornadoVfx.transform.localRotation = Quaternion.identity;
    }

    IEnumerator ScaleRune(float from, float to, float dur)
    {
        float t = 0f;
        float rot = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float smooth = p * p * (3f - 2f * p);

            float s = Mathf.Lerp(from, to, smooth);
            if (summonCircle != null)
                summonCircle.transform.localScale = Vector3.one * s;

            if (spinRune && summonCircle != null)
            {
                rot += Time.deltaTime * spinSpeed;
                summonCircle.transform.localRotation = Quaternion.Euler(90f, 0f, rot);
            }

            yield return null;
        }

        if (summonCircle != null)
            summonCircle.transform.localScale = Vector3.one * to;
    }

    IEnumerator FadeRune(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            SetRuneAlpha(Mathf.Lerp(from, to, p));
            yield return null;
        }
        SetRuneAlpha(to);
    }

    void SetRuneAlpha(float a)
    {
        if (_circleMat == null) return;

        // Works for URP shaders that use _BaseColor
        if (_circleMat.HasProperty(BaseColorId))
        {
            Color c = _circleMat.GetColor(BaseColorId);
            c.a = a;
            _circleMat.SetColor(BaseColorId, c);
        }
        else
        {
            // Fallback
            Color c = _circleMat.color;
            c.a = a;
            _circleMat.color = c;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("TEST Summon")]
    void TestSummon() => OnFound();
#endif
}
