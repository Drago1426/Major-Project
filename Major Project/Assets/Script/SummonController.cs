using System.Collections;
using UnityEngine;

public class SummonController : MonoBehaviour
{
    [Header("Card Data")]
    public CardData cardData;

    [Header("Scene References")]
    public Transform creatureSpawnRoot;       // Empty object where creature appears (child of ImageTarget)
    public GameObject summonCircle;           // Your rune quad
    public Renderer circleRenderer;           // Mesh Renderer on rune quad
    public ParticleSystem fireVfx;            // Your fire particles
    public AudioSource audioSource;           // Add AudioSource to ImageTarget or ARCamera

    [Header("Movement")]
    public Vector3 creatureStartLocalPos = new Vector3(0f, -0.05f, 0f);
    public Vector3 creatureEndLocalPos   = new Vector3(0f,  0.00f, 0f);
    public float creatureRiseTime = 0.7f;

    [Header("Rune")]
    public float runeOpenTime = 0.25f;
    public float runeStartScale = 0.001f;
    public float runeEndScale = 0.055f;
    public float runeFadeTime = 0.4f;

    GameObject _spawnedCreature;
    Material _circleMat;
    Coroutine _routine;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        if (circleRenderer != null)
            _circleMat = circleRenderer.material;

        ResetState();
    }

    public void OnFound()
    {
        if (cardData == null) return;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(SummonRoutine());
    }

    public void OnLost()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        ResetState();
    }

    void ResetState()
    {
        if (fireVfx != null)
            fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (summonCircle != null)
        {
            summonCircle.SetActive(false);
            summonCircle.transform.localScale = Vector3.one * runeStartScale;
        }

        if (_spawnedCreature != null)
        {
            Destroy(_spawnedCreature);
            _spawnedCreature = null;
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

        SetRuneColor(cardData.runeColor, 1f);
        yield return ScaleRune(runeStartScale, runeEndScale, runeOpenTime);

        // 2) Wait → Fire
        yield return new WaitForSeconds(cardData.waitBeforeFire);

        if (fireVfx != null)
        {
            // Optional: tint particle material
            var r = fireVfx.GetComponent<ParticleSystemRenderer>();
            if (r != null && r.material != null)
                r.material.color = cardData.fireColor;

            fireVfx.Play();
        }

        // 3) Wait → Creature spawns + rises
        yield return new WaitForSeconds(cardData.waitBeforeCreature);

        if (cardData.creaturePrefab != null && creatureSpawnRoot != null)
        {
            _spawnedCreature = Instantiate(cardData.creaturePrefab, creatureSpawnRoot);
            _spawnedCreature.transform.localPosition = creatureStartLocalPos;
            _spawnedCreature.transform.localRotation = Quaternion.identity;
        }

        // Sound
        if (audioSource != null && cardData.summonSfx != null)
            audioSource.PlayOneShot(cardData.summonSfx);

        yield return MoveCreature(creatureStartLocalPos, creatureEndLocalPos, creatureRiseTime);

        // 4) Fade out rune + stop fire
        if (fireVfx != null)
            fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        yield return FadeRune(1f, 0f, runeFadeTime);

        if (summonCircle != null)
            summonCircle.SetActive(false);

        _routine = null;
    }

    IEnumerator MoveCreature(Vector3 from, Vector3 to, float dur)
    {
        if (_spawnedCreature == null) yield break;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float smooth = p * p * (3f - 2f * p);
            _spawnedCreature.transform.localPosition = Vector3.Lerp(from, to, smooth);
            yield return null;
        }
        _spawnedCreature.transform.localPosition = to;
    }

    IEnumerator ScaleRune(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float smooth = p * p * (3f - 2f * p);

            float s = Mathf.Lerp(from, to, smooth);
            if (summonCircle != null)
                summonCircle.transform.localScale = Vector3.one * s;

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
            SetRuneColor(cardData.runeColor, Mathf.Lerp(from, to, p));
            yield return null;
        }

        SetRuneColor(cardData.runeColor, to);
    }

    void SetRuneColor(Color col, float alpha)
    {
        if (_circleMat == null) return;

        col.a = alpha;

        if (_circleMat.HasProperty(BaseColorId))
            _circleMat.SetColor(BaseColorId, col);
        else
            _circleMat.color = col;
    }
}
