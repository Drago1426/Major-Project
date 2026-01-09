using System.Collections;
using UnityEngine;

public class CreatureInteractable : MonoBehaviour
{
    [Header("Feedback")]
    public ParticleSystem tapVfx;   // optional burst
    public AudioSource audioSource; // optional
    public AudioClip tapSfx;        // optional

    [Header("Bounce")]
    public float bounceScale = 1.15f;
    public float bounceTime = 0.12f;

    Vector3 _baseScale;
    Coroutine _bounceRoutine;

    void Awake()
    {
        _baseScale = transform.localScale;
    }

    public void OnTapped()
    {
        // VFX
        if (tapVfx != null) tapVfx.Play();

        // SFX
        if (audioSource != null && tapSfx != null)
            audioSource.PlayOneShot(tapSfx);

        // Bounce
        if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
        _bounceRoutine = StartCoroutine(Bounce());
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
