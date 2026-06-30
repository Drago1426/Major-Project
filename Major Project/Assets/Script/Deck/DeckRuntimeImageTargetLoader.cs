using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Vuforia;

[Serializable]
public class RuntimeCardModelOverride
{
    public string cardName;
    public GameObject modelPrefab;
}

public class DeckRuntimeImageTargetLoader : MonoBehaviour
{
    [SerializeField] DeckDatabase deckDatabase;
    [SerializeField] string deckIdToLoad;
    [SerializeField] bool autoLoadOnStart = true;
    [SerializeField] bool reloadDatabaseBeforeLoading = true;
    [SerializeField] string runtimeTargetNamePrefix = "runtime_deck_";
    [SerializeField] bool addCardDetectorToRuntimeTargets = true;
    [SerializeField] bool addSummonOnTargetFoundToRuntimeTargets = true;
    [SerializeField] bool disableSceneTargetsMatchingLoadedDeck = true;
    [SerializeField] GameObject defaultRuntimeCreaturePrefab;

    [Header("Runtime Model Overrides")]
    [Tooltip("Optional card-name-to-prefab mapping for cards loaded from the deck. Use this when a saved deck card has no modelResourcePath.")]
    [SerializeField] List<RuntimeCardModelOverride> cardModelOverrides = new();

    [Header("Runtime Summon Defaults")]
    [SerializeField] bool useSingleVisibleRuntimeCreatureLock = true;
    [SerializeField] bool hideRuntimeCreatureOnTargetLost = false;
    [SerializeField] bool createRuntimeFireTornado = true;
    [SerializeField] Vector3 runtimeCreatureStartLocalPos = new(0f, -0.05f, 0f);
    [SerializeField] Vector3 runtimeCreatureEndLocalPos = new(0f, 0f, 0f);
    [SerializeField] float runtimeCreatureRiseDuration = 0.6f;
    [SerializeField] float runtimeFireTornadoStopDelay = 0.15f;

    [Header("Runtime Stats Display")]
    [SerializeField] Sprite healthIcon;
    [SerializeField] Sprite manaIcon;
    [SerializeField] Sprite damageIcon;
    [SerializeField] bool addStatsDisplayToRuntimeModels = true;

    readonly HashSet<string> loadedTargetNames = new();
    readonly List<GameObject> createdRuntimeTargetObjects = new();
    Coroutine reloadRoutine;

    public DeckDatabase DeckDatabase => deckDatabase;

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

    public void SetDeckIdToLoad(string deckId, bool reloadAfterChange = false)
    {
        deckIdToLoad = string.IsNullOrWhiteSpace(deckId) ? string.Empty : deckId.Trim();

        if (reloadAfterChange)
            ReloadRuntimeDeck();
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

        if (reloadDatabaseBeforeLoading)
            deckDatabase.LoadDecks();

        var deck = deckDatabase.GetDeck(deckIdToLoad);
        if (deck == null)
        {
            Debug.LogWarning($"Deck '{deckIdToLoad}' not found.");
            return;
        }

        if (disableSceneTargetsMatchingLoadedDeck)
            DisableSceneTargetsMatching(deck);

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
        createdRuntimeTargetObjects.Add(createdObserver.gameObject);

        Debug.Log(
            $"Created runtime Vuforia target '{runtimeTargetName}' from card '{card.cardName}' " +
            $"(width: {Mathf.Max(0.01f, card.targetWidthMeters):0.###}m).");
        return true;
    }

    [ContextMenu("Reload Runtime Deck")]
    public void ReloadRuntimeDeck()
    {
        if (!Application.isPlaying)
        {
            ClearRuntimeTargets();

            if (deckDatabase != null)
                deckDatabase.LoadDecks();

            LoadRuntimeImageTargets();
            return;
        }

        if (reloadRoutine != null)
            StopCoroutine(reloadRoutine);

        reloadRoutine = StartCoroutine(ReloadRuntimeDeckRoutine());
    }

    IEnumerator ReloadRuntimeDeckRoutine()
    {
        ClearRuntimeTargets();
        yield return null;

        if (deckDatabase != null)
            deckDatabase.LoadDecks();

        LoadRuntimeImageTargets();
        reloadRoutine = null;
    }

    [ContextMenu("Clear Runtime Targets")]
    public void ClearRuntimeTargets()
    {
        for (int i = createdRuntimeTargetObjects.Count - 1; i >= 0; i--)
        {
            var targetObject = createdRuntimeTargetObjects[i];
            if (targetObject == null)
                continue;

            if (Application.isPlaying)
                Destroy(targetObject);
            else
                DestroyImmediate(targetObject);
        }

        createdRuntimeTargetObjects.Clear();
        loadedTargetNames.Clear();
        SummonVisibilityLock.ReleaseAll();
        Debug.Log("Cleared runtime Vuforia targets loaded from the deck.");
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

        if (addSummonOnTargetFoundToRuntimeTargets && ShouldCreateSummonForCard(card))
        {
            summonComponent = runtimeObject.GetComponent<SummonOnTargetFound>();

            if (summonComponent == null)
            {
                summonComponent = runtimeObject.AddComponent<SummonOnTargetFound>();
                Debug.Log($"Added SummonOnTargetFound to runtime target '{createdObserver.TargetName}'.");
            }

            ConfigureRuntimeSummon(summonComponent, runtimeObject.transform, card);

            if (summonComponent.creature == null)
            {
                GameObject spawnedCreature = CreateRuntimeCreatureForCard(card, runtimeObject.transform);

                if (spawnedCreature == null)
                {
                    Debug.LogWarning($"Summon is enabled for '{createdObserver.TargetName}' but no per-card modelResourcePath, customModelPath, or default runtime prefab was found.");
                }
                else
                {
                    spawnedCreature.transform.localPosition = summonComponent.startLocalPos;
                    spawnedCreature.transform.localRotation = Quaternion.identity;
                    ApplyRuntimeModelCustomization(spawnedCreature, card);
                    EnsureRuntimeCreatureInteraction(spawnedCreature, card);
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

                    Debug.Log($"Assigned runtime creature '{spawnedCreature.name}' to target '{createdObserver.TargetName}' with summon VFX and interaction.");
                }
            }
            else
            {
                EnsureRuntimeCreatureInteraction(summonComponent.creature, card);
            }
        }

        if (addCardDetectorToRuntimeTargets && runtimeObject.GetComponent<CardDetector>() == null)
        {
            runtimeObject.AddComponent<CardDetector>();
            Debug.Log($"Added CardDetector to runtime target '{createdObserver.TargetName}'.");
        }
    }

    bool ShouldCreateSummonForCard(DeckCardEntry card)
    {
        if (card == null)
            return false;

        if (card.HasCustomModelPath())
            return true;

        if (card.NeedsCombatStats() || card.HasModelResourcePath())
            return true;

        if (ResolveModelOverrideForCard(card) != null)
            return true;

        if (ResolveModelPrefabByConvention(card) != null)
            return true;

        return false;
    }

    void ConfigureRuntimeSummon(SummonOnTargetFound summonComponent, Transform runtimeTarget, DeckCardEntry card)
    {
        if (summonComponent == null)
            return;

        summonComponent.hideOnTargetLost = hideRuntimeCreatureOnTargetLost;
        summonComponent.useSingleVisibleCreatureLock = useSingleVisibleRuntimeCreatureLock;
        summonComponent.createFireTornadoIfMissing = createRuntimeFireTornado;
        summonComponent.startLocalPos = runtimeCreatureStartLocalPos;
        summonComponent.endLocalPos = runtimeCreatureEndLocalPos;
        summonComponent.riseDuration = Mathf.Max(0.01f, runtimeCreatureRiseDuration);
        summonComponent.fireTornadoStopDelay = Mathf.Max(0f, runtimeFireTornadoStopDelay);

        if (createRuntimeFireTornado && summonComponent.fireTornadoVfx == null && runtimeTarget != null)
            summonComponent.fireTornadoVfx = FireTornadoSummonVfx.Ensure(runtimeTarget);

        if (card != null)
            summonComponent.SetSummonSfxPath(card.summonSfxPath);
    }

    void DisableSceneTargetsMatching(DeckData deck)
    {
        if (deck?.cards == null || deck.cards.Count == 0)
            return;

        var deckCardNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < deck.cards.Count; i++)
        {
            var card = deck.cards[i];
            if (card != null && !string.IsNullOrWhiteSpace(card.cardName))
                deckCardNames.Add(NormalizeTargetName(card.cardName));
        }

        if (deckCardNames.Count == 0)
            return;

        var sceneObservers = FindObjectsByType<ObserverBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneObservers.Length; i++)
        {
            var observer = sceneObservers[i];
            if (observer == null)
                continue;

            if (!string.IsNullOrWhiteSpace(observer.TargetName) &&
                observer.TargetName.StartsWith(runtimeTargetNamePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            string normalizedTargetName = NormalizeTargetName(observer.TargetName);
            string normalizedObjectName = NormalizeTargetName(observer.gameObject.name);
            if (!deckCardNames.Contains(normalizedTargetName) && !deckCardNames.Contains(normalizedObjectName))
                continue;

            observer.enabled = false;
            observer.gameObject.SetActive(false);
            Debug.Log($"Disabled scene image target '{observer.gameObject.name}' because runtime deck target loading will handle that card.");
        }
    }

    void EnsureRuntimeCreatureInteraction(GameObject creature, DeckCardEntry card)
    {
        if (creature == null)
            return;

        var interactable = creature.GetComponent<CreatureInteractable>();
        if (interactable == null)
            interactable = creature.AddComponent<CreatureInteractable>();

        if (creature.GetComponentInChildren<Collider>(true) == null)
            AddFittedCollider(creature);
    }

    void AddFittedCollider(GameObject creature)
    {
        var collider = creature.AddComponent<BoxCollider>();
        var renderers = creature.GetComponentsInChildren<Renderer>(true);

        bool hasBounds = false;
        Bounds bounds = default;

        foreach (var renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            collider.center = new Vector3(0f, 0.05f, 0f);
            collider.size = new Vector3(0.06f, 0.1f, 0.06f);
            return;
        }

        Vector3 localSize = creature.transform.InverseTransformVector(bounds.size);
        collider.center = creature.transform.InverseTransformPoint(bounds.center);
        collider.size = new Vector3(
            Mathf.Max(0.02f, Mathf.Abs(localSize.x) * 1.15f),
            Mathf.Max(0.02f, Mathf.Abs(localSize.y) * 1.15f),
            Mathf.Max(0.02f, Mathf.Abs(localSize.z) * 1.15f));
    }

    GameObject CreateRuntimeCreatureForCard(DeckCardEntry card, Transform parent)
    {
        if (card != null && card.HasCustomModelPath())
        {
            GameObject runtimeModel = RuntimeObjModelLoader.Load(card.customModelPath, card.SafeModelTint());
            if (runtimeModel != null)
            {
                runtimeModel.name = $"{SanitizeObjectName(card.cardName)}_custom_runtime";
                runtimeModel.transform.SetParent(parent, false);
                return runtimeModel;
            }
        }

        GameObject modelPrefab = ResolveModelPrefabForCard(card);
        if (modelPrefab == null)
            return null;

        var spawnedCreature = Instantiate(modelPrefab, parent);
        spawnedCreature.name = $"{modelPrefab.name}_runtime";
        return spawnedCreature;
    }

    void ApplyRuntimeModelCustomization(GameObject creature, DeckCardEntry card)
    {
        if (creature == null || card == null)
            return;

        creature.transform.localScale = card.SafeModelScale();
        ApplyModelTint(creature, card.SafeModelTint());
    }

    void ApplyModelTint(GameObject creature, Color tint)
    {
        if (creature == null)
            return;

        var renderers = creature.GetComponentsInChildren<Renderer>(true);
        var propertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", tint);
            propertyBlock.SetColor("_Color", tint);
            renderer.SetPropertyBlock(propertyBlock);
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

        var overridePrefab = ResolveModelOverrideForCard(card);
        if (overridePrefab != null)
            return overridePrefab;

        var conventionPrefab = ResolveModelPrefabByConvention(card);
        if (conventionPrefab != null)
            return conventionPrefab;

        return defaultRuntimeCreaturePrefab;
    }

    string SanitizeObjectName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "runtime_model";

        string safe = value.Trim().Replace(" ", "_");
        char[] invalidChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidChars.Length; i++)
            safe = safe.Replace(invalidChars[i].ToString(), string.Empty);

        return string.IsNullOrWhiteSpace(safe) ? "runtime_model" : safe;
    }

    GameObject ResolveModelOverrideForCard(DeckCardEntry card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.cardName) || cardModelOverrides == null)
            return null;

        string cardName = card.cardName.Trim();
        for (int i = 0; i < cardModelOverrides.Count; i++)
        {
            var modelOverride = cardModelOverrides[i];
            if (modelOverride == null || modelOverride.modelPrefab == null || string.IsNullOrWhiteSpace(modelOverride.cardName))
                continue;

            if (string.Equals(modelOverride.cardName.Trim(), cardName, StringComparison.OrdinalIgnoreCase))
                return modelOverride.modelPrefab;
        }

        return null;
    }

    GameObject ResolveModelPrefabByConvention(DeckCardEntry card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.cardName))
            return null;

        string safeName = SanitizeResourceName(card.cardName);
        string[] candidatePaths =
        {
            $"Card Models/{safeName}_Prefab",
            $"Card Models/{safeName}",
            $"Card Models/{safeName}_PrefabRoot"
        };

        for (int i = 0; i < candidatePaths.Length; i++)
        {
            var prefab = Resources.Load<GameObject>(candidatePaths[i]);
            if (prefab != null)
                return prefab;
        }

        return null;
    }

    string SanitizeResourceName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string safe = value.Trim().Replace(" ", "_");
        char[] invalidChars = Path.GetInvalidFileNameChars();

        for (int i = 0; i < invalidChars.Length; i++)
            safe = safe.Replace(invalidChars[i].ToString(), string.Empty);

        return safe;
    }

    string NormalizeTargetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized = value.Trim();
        if (normalized.StartsWith(runtimeTargetNamePrefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(runtimeTargetNamePrefix.Length);

        const string imageTargetPrefix = "ImageTarget_";
        if (normalized.StartsWith(imageTargetPrefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(imageTargetPrefix.Length);

        normalized = normalized.Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);

        return normalized;
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
