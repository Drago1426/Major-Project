using System.Collections;
using UnityEngine;

public class PortalSummon : MonoBehaviour
{
    [Header("References")]
    public Transform creatureRoot;        // Drag CreatureRoot here
    public GameObject summonCircle;       // Drag SummonCircle (Quad) here
    public Renderer circleRenderer;       // Drag SummonCircle (Mesh Renderer) here
    public ParticleSystem fireVfx;        // Drag FireVFX here (child of CreatureRoot)
    public AudioSource summonSfx;         // Optional

    [Header("Creature Movement")]
    public Vector3 creatureStartLocalPos = new Vector3(0f, -0.05f, 0f);
    public Vector3 creatureEndLocalPos   = new Vector3(0f,  0.00f, 0f);

    [Header("Timing")]
    public float circleOpenTime   = 0.25f;
    public float creatureRiseTime = 0.70f;
    public float circleFadeTime   = 0.40f;

    [Header("Circle Size")]
    public float circleStartScale = 0.001f;
    public float circleEndScale   = 0.055f;   // Good for ~6.3cm target width

    [Header("Circle Spin (optional)")]
    public bool spinCircle = true;
    public float spinSpeed = 90f;

    Coroutine routine;
    bool hasSummoned;

    Material _circleMat; // cached material instance

    void Awake()
    {
        // Cache ONE material instance so we don't create lots of copies
        if (circleRenderer != null)
        {
            _circleMat = circleRenderer.material;
        }

        if (creatureRoot != null)
        {
            creatureRoot.localPosition = creatureStartLocalPos;
            creatureRoot.gameObject.SetActive(false);
        }

        if (summonCircle != null)
        {
            summonCircle.transform.localScale = Vector3.one * circleStartScale;
            summonCircle.SetActive(false);
        }
    }

    // Called by Vuforia "On Target Found"
    public void OnFound()
    {
        if (hasSummoned || creatureRoot == null) return;
        hasSummoned = true;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SummonRoutine());
    }

    // Called by Vuforia "On Target Lost" (optional)
    public void OnLost()
    {
        hasSummoned = false;

        if (routine != null) StopCoroutine(routine);
        routine = null;

        if (fireVfx != null)
            fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (summonCircle != null)
            summonCircle.SetActive(false);

        if (creatureRoot != null)
        {
            creatureRoot.gameObject.SetActive(false);
            creatureRoot.localPosition = creatureStartLocalPos;
        }
    }

    IEnumerator SummonRoutine()
    {
        // Show circle and reset alpha
        if (summonCircle != null)
            summonCircle.SetActive(true);

        SetCircleAlpha(1f);

        // Open circle (scale up)
        yield return ScaleCircle(circleStartScale, circleEndScale, circleOpenTime);

        // Start fire + raise creature
        if (fireVfx != null) fireVfx.Play();
        if (summonSfx != null) summonSfx.Play();

        creatureRoot.localPosition = creatureStartLocalPos;
        creatureRoot.gameObject.SetActive(true);

        yield return MoveCreatureUp(creatureStartLocalPos, creatureEndLocalPos, creatureRiseTime);

        // Stop fire smoothly (let particles finish)
        if (fireVfx != null)
            fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Fade out circle
        yield return FadeCircle(1f, 0f, circleFadeTime);

        if (summonCircle != null)
            summonCircle.SetActive(false);

        routine = null;
    }

    IEnumerator MoveCreatureUp(Vector3 from, Vector3 to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);

            // Smoothstep
            float smooth = p * p * (3f - 2f * p);

            creatureRoot.localPosition = Vector3.Lerp(from, to, smooth);
            yield return null;
        }

        creatureRoot.localPosition = to;
    }

    IEnumerator ScaleCircle(float from, float to, float dur)
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

            if (spinCircle && summonCircle != null)
            {
                rot += Time.deltaTime * spinSpeed;
                // Keep it flat (90 deg) and rotate around Z
                summonCircle.transform.localRotation = Quaternion.Euler(90f, 0f, rot);
            }

            yield return null;
        }

        if (summonCircle != null)
            summonCircle.transform.localScale = Vector3.one * to;
    }

    IEnumerator FadeCircle(float from, float to, float dur)
    {
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            SetCircleAlpha(Mathf.Lerp(from, to, p));
            yield return null;
        }

        SetCircleAlpha(to);
    }

    void SetCircleAlpha(float a)
    {
        if (_circleMat == null) return;

        Color c = _circleMat.color;
        c.a = a;
        _circleMat.color = c;
    }

#if UNITY_EDITOR
    // Right-click the component header in Inspector -> TEST Summon
    [ContextMenu("TEST Summon")]
    void TestSummon()
    {
        OnFound();
    }
#endif
}
