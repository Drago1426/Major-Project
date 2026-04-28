using UnityEngine;
using Vuforia;

public class CardDetector : MonoBehaviour
{
    ObserverBehaviour observerBehaviour;
    SummonOnTargetFound summonOnTargetFound;

    void Start()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();
        summonOnTargetFound = GetComponent<SummonOnTargetFound>();

        if (observerBehaviour)
        {
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
        }
    }

    void OnDestroy()
    {
        if (observerBehaviour)
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
        {
            string cardName = behaviour.TargetName;
            Debug.Log("Detected card: " + cardName);

            Speak(cardName);
            summonOnTargetFound?.OnFound();
            return;
        }

        summonOnTargetFound?.OnLost();
    }

    void Speak(string text)
    {
        // placeholder for speech
        Debug.Log("Speaking: " + text);
    }
}
