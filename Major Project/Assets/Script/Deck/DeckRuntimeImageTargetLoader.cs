using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Vuforia;

public class DeckRuntimeImageTargetLoader : MonoBehaviour
{
    [SerializeField] DeckDatabase deckDatabase;
    [SerializeField] string deckIdToLoad;
    [SerializeField] bool autoLoadOnStart = true;
    [SerializeField] string runtimeTargetNamePrefix = "runtime_deck_";
    [SerializeField] bool addCardDetectorToRuntimeTargets = true;
    [SerializeField] bool addSummonOnTargetFoundToRuntimeTargets = true;
    [SerializeField] GameObject defaultRuntimeCreaturePrefab;

    [Header("Runtime Stats Display")]
    [SerializeField] Sprite healthIcon;
    [SerializeField] Sprite manaIcon;
    [SerializeField] Sprite damageIcon;
    [SerializeField] bool addStatsDisplayToRuntimeModels = true;

    readonly HashSet<string> loadedTargetNames = new();

    void Start()
    {
        Debug.Log($"[DeckRuntimeImageTargetLoader] Start on '{gameObject.name}'. autoLoadOnStart={autoLoadOnStart}");

        if (autoLoadOnStart)
            LoadRuntimeImageTargets();
    }

    [ContextMenu("Test Loader Log")]
    public void TestLoaderLog()
    {
        Debug.Log($"[DeckRuntimeImageTargetLoader] Test log from '{gameObject.name}'.");
    }

    [ContextMenu("Load Runtime Image Targets")]
    public void LoadRuntimeImageTargets()
    {
        Debug.Log("[DeckRuntimeImageTargetLoader] LoadRuntimeImageTargets() called.");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("Load Runtime Image Targets must run in Play Mode so Vuforia is initialized.");
            return;
        }

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

        int createdCount = 0;
        int skippedCount = 0;
        int failedCount = 0;

        for (int i = 0; i < deck.cards.Count; i++)
        {
            var card = deck.cards[i];
            if (card == null || !card.HasImageTarget())
            {
                skippedCount++;
                continue;
            }

            string runtimeTargetName = BuildRuntimeTargetName(card.cardName);
            if (loadedTargetNames.Contains(runtimeTargetName))
            {
                skippedCount++;
                Debug.Log($"Skipping duplicate runtime target '{runtimeTargetName}'.");
                continue;
            }

            if (TryCreateVuforiaImageTarget(card, runtimeTargetName))
            {
                createdCount++;
                loadedTargetNames.Add(runtimeTargetName);
                continue;
            }

            failedCount++;
        }

        Debug.Log(
            $"Runtime target load finished for deck '{deckIdToLoad}'. " +
            $"Created: {createdCount}, Skipped: {skippedCount}, Failed: {failedCount}. " +
            $"Runtime target names use prefix '{runtimeTargetNamePrefix}'.");
    }

    string BuildRuntimeTargetName(string cardName)
    {
        string trimmedCardName = string.IsNullOrWhiteSpace(cardName) ? "unknown_card" : cardName.Trim();
        return $"{runtimeTargetNamePrefix}{trimmedCardName}";
    }

    bool TryCreateVuforiaImageTarget(DeckCardEntry card, string runtimeTargetName)
    {
        if (card == null)
            return false;

        if (string.IsNullOrWhiteSpace(card.imagePath))
        {
            Debug.LogWarning($"Card '{card.cardName}' has no image path.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(runtimeTargetName))
        {
            Debug.LogWarning($"Card '{card.cardName}' has an invalid runtime target name.");
            return false;
        }

        if (!File.Exists(card.imagePath))
        {
            Debug.LogWarning($"Image file missing for card '{card.cardName}': {card.imagePath}");
            return false;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(card.imagePath)))
        {
            SafeDestroy(texture);
            Debug.LogWarning($"Failed to load image bytes for card '{card.cardName}'.");
            return false;
        }

        if (!TryCreateWithVuforia(texture, card.targetWidthMeters, runtimeTargetName, out ObserverBehaviour createdObserver, out string createError))
        {
            SafeDestroy(texture);
            Debug.LogWarning($"Could not create Vuforia image target for '{card.cardName}'. Reason: {createError}");
            return false;
        }

        TryAttachRuntimeComponents(createdObserver, card);

        Debug.Log(
            $"Created runtime Vuforia target '{runtimeTargetName}' from card '{card.cardName}' " +
            $"(width: {Mathf.Max(0.01f, card.targetWidthMeters):0.###}m).");
        return true;
    }

    [ContextMenu("Clear Loaded Runtime Target Cache")]
    public void ClearLoadedRuntimeTargetCache()
    {
        loadedTargetNames.Clear();
        Debug.Log("Cleared runtime target cache in DeckRuntimeImageTargetLoader.");
    }

    [ContextMenu("Print Loaded Runtime Targets")]
    public void PrintLoadedRuntimeTargets()
    {
        if (loadedTargetNames.Count == 0)
        {
            Debug.Log("No runtime targets have been created in this session.");
            return;
        }

        Debug.Log("Loaded runtime targets: " + string.Join(", ", loadedTargetNames));
    }

    void TryAttachRuntimeComponents(ObserverBehaviour createdObserver, DeckCardEntry card)
    {
        if (createdObserver == null)
            return;

        var runtimeObject = createdObserver.gameObject;

        SummonOnTargetFound summonComponent = null;

        if (addSummonOnTargetFoundToRuntimeTargets)
        {
            summonComponent = runtimeObject.GetComponent<SummonOnTargetFound>();

            if (summonComponent == null)
            {
                summonComponent = runtimeObject.AddComponent<SummonOnTargetFound>();
                Debug.Log($"Added SummonOnTargetFound to runtime target '{createdObserver.TargetName}'.");
            }

            if (summonComponent.creature == null)
            {
                GameObject modelPrefab = ResolveModelPrefabForCard(card);

                if (modelPrefab == null)
                {
                    Debug.LogWarning($"Summon is enabled for '{createdObserver.TargetName}' but no per-card modelResourcePath or default runtime prefab was found.");
                }
                else
                {
                    var spawnedCreature = Instantiate(modelPrefab, runtimeObject.transform);
                    spawnedCreature.name = $"{modelPrefab.name}_runtime";
                    spawnedCreature.transform.localPosition = summonComponent.startLocalPos;
                    spawnedCreature.transform.localRotation = Quaternion.identity;
                    spawnedCreature.transform.localScale = Vector3.one;
                    spawnedCreature.SetActive(false);
                    summonComponent.creature = spawnedCreature;

                    if (addStatsDisplayToRuntimeModels && card != null)
                    {
                        var statsDisplay = spawnedCreature.GetComponent<CardStatsDisplay3D>();

                        if (statsDisplay == null)
                            statsDisplay = spawnedCreature.AddComponent<CardStatsDisplay3D>();

                        statsDisplay.Initialize(
                            card.health,
                            card.mana,
                            card.damage,
                            healthIcon,
                            manaIcon,
                            damageIcon
                        );
                    }

                    Debug.Log($"Assigned runtime creature '{spawnedCreature.name}' to target '{createdObserver.TargetName}'.");
                }
            }
        }

        if (addCardDetectorToRuntimeTargets && runtimeObject.GetComponent<CardDetector>() == null)
        {
            runtimeObject.AddComponent<CardDetector>();
            Debug.Log($"Added CardDetector to runtime target '{createdObserver.TargetName}'.");
        }
    }

    GameObject ResolveModelPrefabForCard(DeckCardEntry card)
    {
        if (card != null && card.HasModelResourcePath())
        {
            var perCardPrefab = Resources.Load<GameObject>(card.modelResourcePath);
            if (perCardPrefab != null)
                return perCardPrefab;

            Debug.LogWarning($"Could not load model prefab from Resources path '{card.modelResourcePath}' for card '{card.cardName}'.");
        }

        return defaultRuntimeCreaturePrefab;
    }

    bool TryCreateWithVuforia(Texture2D texture, float widthMeters, string targetName, out ObserverBehaviour createdObserver, out string error)
    {
        createdObserver = null;
        error = string.Empty;

        var vuforiaBehaviour = VuforiaBehaviour.Instance;
        if (vuforiaBehaviour == null)
        {
            error = "VuforiaBehaviour.Instance is null (Vuforia may not be initialized yet).";
            return false;
        }

        var observerFactory = vuforiaBehaviour.ObserverFactory;
        if (observerFactory == null)
        {
            error = "ObserverFactory is null on VuforiaBehaviour.Instance.";
            return false;
        }

        var safeWidth = Mathf.Max(0.01f, widthMeters);
        try
        {
            createdObserver = observerFactory.CreateImageTarget(texture, safeWidth, targetName);
            if (createdObserver == null)
            {
                error = "CreateImageTarget returned null.";
                return false;
            }
        }
        catch (Exception e)
        {
            error = $"CreateImageTarget failed: {e.Message}";
            return false;
        }

        return true;
    }

    void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}