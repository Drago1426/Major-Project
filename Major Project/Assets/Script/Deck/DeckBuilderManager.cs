using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DeckBuilderManager : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] DeckDatabase deckDatabase;
    [SerializeField] DeckRuntimeImageTargetLoader runtimeTargetLoader;
    [SerializeField] bool reloadRuntimeTargetsAfterSave = true;

    [Header("Current Deck")]
    [SerializeField] string deckName = "My Deck";
    [SerializeField] CardGameType selectedGameType = CardGameType.MTG;

    [Header("Card Input")]
    [SerializeField] string cardName = "";
    [SerializeField] int quantity = 1;
    [SerializeField] MtgCardType mtgCardType = MtgCardType.Creature;
    [SerializeField] PokemonCardType pokemonCardType = PokemonCardType.Pokemon;
    [SerializeField] Texture2D cardImage;
    [SerializeField] float targetWidthMeters = 0.06f;
    [SerializeField] GameObject cardModelPrefab;
    [Tooltip("Optional Resources path for this card's summon prefab, e.g. Creatures/Dragon.")]
    [SerializeField] string cardModelResourcePath = "";
    [Tooltip("Optional absolute path to a runtime-imported model file. Runtime OBJ files are supported.")]
    [SerializeField] string cardCustomModelPath = "";
    [SerializeField] Vector3 cardModelScale = Vector3.one;
    [SerializeField] Color cardModelTint = Color.white;

    [Header("Card Audio")]
    [SerializeField] AudioClip cardSummonSfxClip;
    [Tooltip("Optional absolute file path or Resources path for this card's summon sound.")]
    [SerializeField] string cardSummonSfxPath = "";
    [SerializeField] AudioClip cardFireballSfxClip;
    [Tooltip("Optional absolute file path or Resources path for this card's interaction fireball sound.")]
    [SerializeField] string cardFireballSfxPath = "";

    [Header("Only used for Creature / Pokemon cards")]
    [SerializeField] int health = 0;
    [SerializeField] int damage = 0;
    [SerializeField] int mana = 0;

    [Header("Working deck before save")]
    [SerializeField] List<DeckCardEntry> workingCards = new();

    void Awake()
    {
        if (deckDatabase != null)
            deckDatabase.LoadDecks();
    }

    public void SetGameType(int gameTypeIndex)
    {
        selectedGameType = (CardGameType)Mathf.Clamp(gameTypeIndex, 0, Enum.GetValues(typeof(CardGameType)).Length - 1);
    }

    public void SetDeckName(string newDeckName)
    {
        deckName = newDeckName;
    }

    public void SetCardName(string newCardName)
    {
        cardName = newCardName;
    }

    public void SetQuantity(int newQuantity)
    {
        quantity = Mathf.Max(1, newQuantity);
    }

    public void SetCardImage(Texture2D image)
    {
        cardImage = image;
    }

    public void SetTargetWidthMeters(float widthMeters)
    {
        targetWidthMeters = Mathf.Max(0.01f, widthMeters);
    }

    public void SetCardModelResourcePath(string resourcePath)
    {
        cardModelResourcePath = resourcePath;
    }

    public void SetCardCustomModelPath(string modelPath)
    {
        cardCustomModelPath = modelPath;
    }

    public void SetCardModelScale(Vector3 modelScale)
    {
        cardModelScale = new Vector3(
            Mathf.Max(0.01f, modelScale.x),
            Mathf.Max(0.01f, modelScale.y),
            Mathf.Max(0.01f, modelScale.z));
    }

    public void SetCardModelTint(Color modelTint)
    {
        modelTint.a = Mathf.Max(0.01f, modelTint.a);
        cardModelTint = modelTint;
    }

    public void SetCardModelPrefab(GameObject modelPrefab)
    {
        cardModelPrefab = modelPrefab;
    }

    public void SetCardSummonSfxPath(string soundPath)
    {
        cardSummonSfxPath = soundPath;
    }

    public void SetCardFireballSfxPath(string soundPath)
    {
        cardFireballSfxPath = soundPath;
    }

    public void SetCardSummonSfxClip(AudioClip clip)
    {
        cardSummonSfxClip = clip;
    }

    public void SetCardFireballSfxClip(AudioClip clip)
    {
        cardFireballSfxClip = clip;
    }

    public void SetMtgCardType(int cardTypeIndex)
    {
        mtgCardType = (MtgCardType)Mathf.Clamp(cardTypeIndex, 0, Enum.GetValues(typeof(MtgCardType)).Length - 1);
    }

    public void SetPokemonCardType(int cardTypeIndex)
    {
        pokemonCardType = (PokemonCardType)Mathf.Clamp(cardTypeIndex, 0, Enum.GetValues(typeof(PokemonCardType)).Length - 1);
    }

    public void SetCombatStats(int newHealth, int newDamage, int newMana)
    {
        health = Mathf.Max(0, newHealth);
        damage = Mathf.Max(0, newDamage);
        mana = Mathf.Max(0, newMana);
    }

    public bool CurrentCardNeedsCombatStats()
    {
        if (selectedGameType == CardGameType.MTG)
            return mtgCardType == MtgCardType.Creature;

        return pokemonCardType == PokemonCardType.Pokemon;
    }

    public string CurrentSelectedCardType()
    {
        if (selectedGameType == CardGameType.MTG)
            return mtgCardType.ToString();

        return pokemonCardType.ToString();
    }

    [ContextMenu("Add Card To Working Deck")]
    public void AddCardToWorkingDeck()
    {
        TryAddCardToWorkingDeck();
    }

    public bool TryAddCardToWorkingDeck()
    {
        if (string.IsNullOrWhiteSpace(cardName))
        {
            Debug.LogWarning("Card name is required.");
            return false;
        }

        if (quantity <= 0)
        {
            Debug.LogWarning("Quantity must be at least 1.");
            return false;
        }

        string resolvedModelResourcePath = ResolveModelResourcePathForCurrentCard();
        string resolvedSummonSfxPath = ResolveAudioPathForCurrentCard(cardSummonSfxClip, cardSummonSfxPath, cardName, "summon");
        string resolvedFireballSfxPath = ResolveAudioPathForCurrentCard(cardFireballSfxClip, cardFireballSfxPath, cardName, "fireball");
        var card = new DeckCardEntry
        {
            cardName = cardName.Trim(),
            quantity = quantity,
            cardType = CurrentSelectedCardType(),
            health = health,
            damage = damage,
            mana = mana,
            targetWidthMeters = targetWidthMeters,
            modelResourcePath = resolvedModelResourcePath,
            customModelPath = string.IsNullOrWhiteSpace(cardCustomModelPath) ? string.Empty : cardCustomModelPath.Trim(),
            modelScale = SafeModelScale(cardModelScale),
            modelTint = SafeModelTint(cardModelTint),
            summonSfxPath = resolvedSummonSfxPath,
            fireballSfxPath = resolvedFireballSfxPath
        };

        if (card.NeedsCombatStats(selectedGameType) && (health <= 0 || damage <= 0 || mana < 0))
        {
            Debug.LogWarning("Creature / Pokemon cards need valid health and damage values. Mana must be 0 or higher.");
            return false;
        }

        if (!card.NeedsCombatStats(selectedGameType))
        {
            card.health = 0;
            card.damage = 0;
            card.mana = 0;
        }

        if (cardImage == null)
        {
            Debug.LogWarning("Card image is required to build a Vuforia image target.");
            return false;
        }

        card.imagePath = SaveCardImage(card.cardName, cardImage);
        if (string.IsNullOrWhiteSpace(card.imagePath))
            return false;

        workingCards.Add(card);
        Debug.Log($"Added {card.quantity}x {card.cardName} to working deck.");
        return true;
    }

    [ContextMenu("Clear Working Deck")]
    public void ClearWorkingDeck()
    {
        workingCards.Clear();
        Debug.Log("Working deck cleared.");
    }

    [ContextMenu("Save Working Deck")]
    public void SaveWorkingDeck()
    {
        TrySaveWorkingDeck();
    }

    public bool TrySaveWorkingDeck()
    {
        if (deckDatabase == null)
        {
            Debug.LogWarning("DeckDatabase reference is missing.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(deckName))
        {
            Debug.LogWarning("Deck name is required.");
            return false;
        }

        var deck = new DeckData
        {
            deckId = BuildDeckId(deckName, selectedGameType),
            deckName = deckName.Trim(),
            gameType = selectedGameType,
            cards = new List<DeckCardEntry>(workingCards)
        };

        deckDatabase.AddOrUpdateDeck(deck);
        Debug.Log($"Saved deck '{deck.deckName}' with {deck.cards.Count} card entries.");

        if (runtimeTargetLoader != null)
        {
            runtimeTargetLoader.SetDeckIdToLoad(deck.deckId);

            if (reloadRuntimeTargetsAfterSave && Application.isPlaying)
                runtimeTargetLoader.ReloadRuntimeDeck();
        }

        return true;
    }

    public DeckData LoadDeck(string deckId)
    {
        if (deckDatabase == null)
            return null;

        return deckDatabase.GetDeck(deckId);
    }

    string BuildDeckId(string name, CardGameType gameType)
    {
        return $"{gameType}-{name}".ToLowerInvariant().Replace(" ", "-");
    }

    string SaveCardImage(string rawCardName, Texture2D image)
    {
        if (image == null)
            return null;

        bool createdReadableCopy = false;
        Texture2D sourceTexture = EnsureTextureIsReadable(image, ref createdReadableCopy);
        if (sourceTexture == null)
        {
            Debug.LogWarning("Failed to prepare a readable card image.");
            return null;
        }

        byte[] pngData = sourceTexture.EncodeToPNG();
        if (createdReadableCopy)
            Destroy(sourceTexture);

        if (pngData == null || pngData.Length == 0)
        {
            Debug.LogWarning("Failed to encode card image as PNG.");
            return null;
        }

        string folderPath = Path.Combine(Application.persistentDataPath, "deck_card_images");
        Directory.CreateDirectory(folderPath);

        string safeName = SanitizeFileName(rawCardName);
        string filePath = Path.Combine(folderPath, $"{safeName}_{Guid.NewGuid():N}.png");
        File.WriteAllBytes(filePath, pngData);
        return filePath;
    }

    Texture2D EnsureTextureIsReadable(Texture2D source, ref bool createdReadableCopy)
    {
        if (source.isReadable)
            return source;

        RenderTexture temporaryRenderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;

        Graphics.Blit(source, temporaryRenderTexture);
        RenderTexture.active = temporaryRenderTexture;

        var readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readableTexture.ReadPixels(new Rect(0, 0, temporaryRenderTexture.width, temporaryRenderTexture.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporaryRenderTexture);

        createdReadableCopy = true;
        return readableTexture;
    }

    static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "card";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        string safe = value.Trim();
        for (int i = 0; i < invalidChars.Length; i++)
            safe = safe.Replace(invalidChars[i].ToString(), string.Empty);

        return string.IsNullOrWhiteSpace(safe) ? "card" : safe.Replace(" ", "_");
    }

    string ResolveModelResourcePathForCurrentCard()
    {
        if (cardModelPrefab != null)
        {
#if UNITY_EDITOR
            string prefabResourcePath = ConvertPrefabToResourcesPath(cardModelPrefab);
            if (!string.IsNullOrWhiteSpace(prefabResourcePath))
                return prefabResourcePath;
#else
            Debug.LogWarning("Card model prefab drag-and-drop path conversion is only available in the Unity Editor. Use cardModelResourcePath on runtime builds.");
#endif
        }

        return string.IsNullOrWhiteSpace(cardModelResourcePath) ? string.Empty : cardModelResourcePath.Trim();
    }

    string ResolveAudioPathForCurrentCard(AudioClip clip, string explicitPath, string rawCardName, string soundSlot)
    {
        if (clip != null)
        {
#if UNITY_EDITOR
            string copiedPath = CopyAudioClipAssetToPersistentPath(clip, rawCardName, soundSlot);
            if (!string.IsNullOrWhiteSpace(copiedPath))
                return copiedPath;
#else
            Debug.LogWarning("AudioClip drag-and-drop is only available in the Unity Editor. Use an audio file path in runtime builds.");
#endif
        }

        return string.IsNullOrWhiteSpace(explicitPath) ? string.Empty : explicitPath.Trim();
    }

    static Vector3 SafeModelScale(Vector3 modelScale)
    {
        return new Vector3(
            Mathf.Max(0.01f, modelScale.x),
            Mathf.Max(0.01f, modelScale.y),
            Mathf.Max(0.01f, modelScale.z));
    }

    static Color SafeModelTint(Color modelTint)
    {
        if (modelTint.a <= 0f &&
            Mathf.Approximately(modelTint.r, 0f) &&
            Mathf.Approximately(modelTint.g, 0f) &&
            Mathf.Approximately(modelTint.b, 0f))
        {
            return Color.white;
        }

        return new Color(modelTint.r, modelTint.g, modelTint.b, Mathf.Max(0.01f, modelTint.a));
    }

#if UNITY_EDITOR
    string ConvertPrefabToResourcesPath(GameObject prefab)
    {
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            Debug.LogWarning("Selected model prefab does not have a valid asset path.");
            return string.Empty;
        }

        const string resourcesToken = "/Resources/";
        int resourcesIndex = assetPath.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex < 0)
        {
            Debug.LogWarning($"Model prefab '{prefab.name}' is not inside a Resources folder. Move it under Assets/**/Resources/ to use drag-and-drop.");
            return string.Empty;
        }

        int pathStart = resourcesIndex + resourcesToken.Length;
        string pathWithoutExtension = assetPath.Substring(pathStart);
        return Path.ChangeExtension(pathWithoutExtension, null).Replace("\\", "/");
    }

    string CopyAudioClipAssetToPersistentPath(AudioClip clip, string rawCardName, string soundSlot)
    {
        string assetPath = AssetDatabase.GetAssetPath(clip);
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            Debug.LogWarning($"Selected {soundSlot} audio clip does not have a valid asset path.");
            return string.Empty;
        }

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            Debug.LogWarning("Could not resolve the Unity project root to copy the audio file.");
            return string.Empty;
        }

        string sourcePath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"Selected {soundSlot} audio file does not exist on disk: {sourcePath}");
            return string.Empty;
        }

        string extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            Debug.LogWarning($"Selected {soundSlot} audio file needs an extension Unity can load, such as .wav, .mp3, or .ogg.");
            return string.Empty;
        }

        string folderPath = Path.Combine(Application.persistentDataPath, "deck_card_audio");
        Directory.CreateDirectory(folderPath);

        string safeCardName = SanitizeFileName(rawCardName);
        string safeSlot = SanitizeFileName(soundSlot);
        string destinationPath = Path.Combine(folderPath, $"{safeCardName}_{safeSlot}_{Guid.NewGuid():N}{extension}");
        File.Copy(sourcePath, destinationPath, true);
        return destinationPath;
    }
#endif
}
