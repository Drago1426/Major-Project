using System.Collections;
using UnityEngine;

public class SummonOnTargetFound : MonoBehaviour
{
    [Header("References")]
    public GameObject creature;          // your 3D model root
    public ParticleSystem summonVfx;
    public AudioSource summonSfx;         // optional sound
    public Animator creatureAnimator;

    [Header("Animation Trigger")]
    public bool triggerCreatureAnimation = true;
    public string summonTriggerName = "Summon";

    [Header("Motion")]
    public Vector3 startLocalPos = new Vector3(0f, -0.05f, 0f);
    public Vector3 endLocalPos   = new Vector3(0f,  0.00f, 0f);
    public float riseDuration = 0.6f;

    [Header("Behaviour")]
    public bool hideOnTargetLost = true;

    Coroutine _routine;
    bool _hasSummoned;

    void Awake()
    {
        if (creature != null)
        {
            creature.transform.localPosition = startLocalPos;
            creature.SetActive(false);
        }
    }

    // Call this from Vuforia "On Target Found"
    public void OnFound()
    {
        if (creature == null) return;

        // Prevent re-triggering every frame while tracked
        if (_hasSummoned) return;
        _hasSummoned = true;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(SummonRoutine());
    }

    // Call this from Vuforia "On Target Lost"
    public void OnLost()
    {
        if (!hideOnTargetLost || creature == null) return;

        if (_routine != null) StopCoroutine(_routine);

        _hasSummoned = false;
        creature.SetActive(false);
        creature.transform.localPosition = startLocalPos;
    }

    IEnumerator SummonRoutine()
    {
        // VFX + SFX first
        if (summonVfx != null) summonVfx.Play();
        if (summonSfx != null) summonSfx.Play();

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
        _routine = null;
    }
}