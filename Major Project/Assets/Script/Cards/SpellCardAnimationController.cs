using UnityEngine;

public class SpellCardAnimationController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string animationTriggerName = "Cast";

    [Header("Optional Spell VFX")]
    [SerializeField] private GameObject spellVfxPrefab;
    [SerializeField] private Vector3 vfxLocalOffset = new Vector3(0f, 0.04f, 0f);
    [SerializeField] private bool destroySpawnedVfxAfterSeconds = true;
    [SerializeField] private float vfxLifetimeSeconds = 2.5f;

    private bool isConfiguredSpellCard;

    public void ConfigureForRuntimeCard(
        bool shouldPlaySpellAnimation,
        Animator runtimeAnimator,
        string triggerName,
        GameObject runtimeSpellVfxPrefab)
    {
        isConfiguredSpellCard = shouldPlaySpellAnimation;

        if (runtimeAnimator != null)
            targetAnimator = runtimeAnimator;

        if (!string.IsNullOrWhiteSpace(triggerName))
            animationTriggerName = triggerName.Trim();

        if (runtimeSpellVfxPrefab != null)
            spellVfxPrefab = runtimeSpellVfxPrefab;
    }

    public void OnCardFound()
    {
        if (!isConfiguredSpellCard)
            return;

        if (targetAnimator != null && !string.IsNullOrWhiteSpace(animationTriggerName))
            targetAnimator.SetTrigger(animationTriggerName);

        if (spellVfxPrefab != null)
        {
            GameObject spawnedVfx = Instantiate(spellVfxPrefab, transform);
            spawnedVfx.transform.localPosition = vfxLocalOffset;
            spawnedVfx.transform.localRotation = Quaternion.identity;

            if (destroySpawnedVfxAfterSeconds)
                Destroy(spawnedVfx, Mathf.Max(0.1f, vfxLifetimeSeconds));
        }
    }
}