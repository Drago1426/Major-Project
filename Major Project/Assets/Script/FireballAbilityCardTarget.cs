using UnityEngine;
using Vuforia;

[DisallowMultipleComponent]
public class FireballAbilityCardTarget : MonoBehaviour
{
    public string cardName = "Fireball";
    public string fireballSfxPath;
    public bool treatExtendedTrackedAsFound = false;

    ObserverBehaviour observerBehaviour;
    bool wasTracked;

    void Awake()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();

        if (string.IsNullOrWhiteSpace(cardName) && observerBehaviour != null)
            cardName = observerBehaviour.TargetName;
    }

    void OnEnable()
    {
        if (observerBehaviour == null)
            observerBehaviour = GetComponent<ObserverBehaviour>();

        if (observerBehaviour != null)
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    void OnDisable()
    {
        if (observerBehaviour != null)
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;

        wasTracked = false;
    }

    public void Configure(DeckCardEntry card)
    {
        if (card == null)
            return;

        cardName = string.IsNullOrWhiteSpace(card.cardName) ? "Fireball" : card.cardName.Trim();
        fireballSfxPath = string.IsNullOrWhiteSpace(card.fireballSfxPath) ? string.Empty : card.fireballSfxPath.Trim();
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isTracked = status.Status == Status.TRACKED ||
            (treatExtendedTrackedAsFound && status.Status == Status.EXTENDED_TRACKED);

        if (isTracked && !wasTracked)
        {
            wasTracked = true;
            ArmCreature();
        }
        else if (!isTracked && wasTracked)
        {
            wasTracked = false;
        }
    }

    void ArmCreature()
    {
        string sourceName = string.IsNullOrWhiteSpace(cardName) ? "Fireball" : cardName.Trim();
        if (CreatureInteractable.TryArmFirstActiveFireball(sourceName, fireballSfxPath))
        {
            Debug.Log($"Fireball card '{sourceName}' armed the active creature.", this);
            return;
        }

        Debug.LogWarning($"Fireball card '{sourceName}' was scanned, but no active creature is visible to arm.", this);
    }
}
