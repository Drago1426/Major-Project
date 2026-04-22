using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public class DeckRuntimeImageTargetLoader : MonoBehaviour
{
    [SerializeField] DeckDatabase deckDatabase;
    [SerializeField] string deckIdToLoad;

    [ContextMenu("Load Runtime Image Targets")]
    public void LoadRuntimeImageTargets()
    {
        if (deckDatabase == null)
        {
            Debug.LogWarning("DeckDatabase reference is missing.");
            return;
        }

        var deck = deckDatabase.GetDeck(deckIdToLoad);
        if (deck == null)
        {
            Debug.LogWarning($"Deck '{deckIdToLoad}' not found.");
            return;
        }

        for (int i = 0; i < deck.cards.Count; i++)
        {
            var card = deck.cards[i];
            if (card == null || !card.HasImageTarget())
                continue;

            TryCreateVuforiaImageTarget(card);
        }
    }

    void TryCreateVuforiaImageTarget(DeckCardEntry card)
    {
        if (!File.Exists(card.imagePath))
        {
            Debug.LogWarning($"Image file missing for card '{card.cardName}': {card.imagePath}");
            return;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(card.imagePath)))
        {
            Destroy(texture);
            Debug.LogWarning($"Failed to load image bytes for card '{card.cardName}'.");
            return;
        }

        if (!InvokeVuforiaObserverFactory(texture, card.targetWidthMeters, card.cardName))
        {
            Destroy(texture);
            Debug.LogWarning($"Could not create Vuforia image target for '{card.cardName}'.");
        }
    }

    bool InvokeVuforiaObserverFactory(Texture2D texture, float widthMeters, string targetName)
    {
        var vuforiaBehaviourType = Type.GetType("Vuforia.VuforiaBehaviour, Vuforia.Unity.Wrapper");
        if (vuforiaBehaviourType == null)
            return false;

        var instanceProperty = vuforiaBehaviourType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        var instance = instanceProperty?.GetValue(null);
        if (instance == null)
            return false;

        var observerFactoryProperty = vuforiaBehaviourType.GetProperty("ObserverFactory", BindingFlags.Public | BindingFlags.Instance);
        var observerFactory = observerFactoryProperty?.GetValue(instance);
        if (observerFactory == null)
            return false;

        var factoryType = observerFactory.GetType();
        var createImageTargetMethod = factoryType.GetMethod("CreateImageTarget", new[] { typeof(Texture2D), typeof(float), typeof(string) });
        if (createImageTargetMethod == null)
            return false;

        var safeWidth = Mathf.Max(0.01f, widthMeters);
        createImageTargetMethod.Invoke(observerFactory, new object[] { texture, safeWidth, targetName });
        return true;
    }
}