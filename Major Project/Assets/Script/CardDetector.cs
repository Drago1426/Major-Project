using UnityEngine;
using Vuforia;

public class CardDetector : MonoBehaviour
{
    [SerializeField] bool treatExtendedTrackedAsFound = false;

    private ObserverBehaviour observerBehaviour;
    private SummonOnTargetFound summonOnTargetFound;
    private bool wasTracked;

    private void Awake()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();
        FindTargetScripts();
    }

    private void OnEnable()
    {
        if (observerBehaviour != null)
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    private void OnDisable()
    {
        if (observerBehaviour != null)
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    private void Start()
    {
        if (observerBehaviour == null)
            Debug.LogWarning($"[CardDetector] Missing ObserverBehaviour on '{gameObject.name}'.", this);

        if (summonOnTargetFound == null)
            Debug.LogWarning($"[CardDetector] No SummonOnTargetFound found yet for '{gameObject.name}'. Will try again when card is detected.", this);
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isTracked = status.Status == Status.TRACKED ||
            (treatExtendedTrackedAsFound && status.Status == Status.EXTENDED_TRACKED);

        if (isTracked && !wasTracked)
        {
            wasTracked = true;

            string cardName = behaviour.TargetName;
            Debug.Log("[CardDetector] Detected card: " + cardName, this);

            Speak(cardName);

            if (summonOnTargetFound == null)
                FindTargetScripts();

            if (summonOnTargetFound == null)
            {
                Debug.LogWarning($"[CardDetector] Card was detected, but no SummonOnTargetFound exists on '{gameObject.name}'.", this);
                return;
            }

            summonOnTargetFound.OnFound();
        }
        else if (!isTracked && wasTracked)
        {
            wasTracked = false;
            Debug.Log("[CardDetector] Lost card: " + behaviour.TargetName, this);

            if (summonOnTargetFound == null)
                FindTargetScripts();

            summonOnTargetFound?.OnLost();
        }
    }

    private void FindTargetScripts()
    {
        summonOnTargetFound = GetComponent<SummonOnTargetFound>();

        if (summonOnTargetFound == null)
            summonOnTargetFound = GetComponentInChildren<SummonOnTargetFound>(true);

        if (summonOnTargetFound == null)
            summonOnTargetFound = GetComponentInParent<SummonOnTargetFound>();

    }

    private void Speak(string text)
    {
        Debug.Log("[CardDetector] Speaking: " + text, this);
    }
}
