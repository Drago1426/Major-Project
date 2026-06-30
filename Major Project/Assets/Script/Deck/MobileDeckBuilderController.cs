using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class BuiltInModelOption
{
    public string displayName;
    [Tooltip("Resources path without file extension, e.g. Card Models/FireDragon_Prefab.")]
    public string resourcesPath;
}

[Serializable]
public class BuiltInSoundOption
{
    public string displayName;
    [Tooltip("Resources path without file extension, e.g. Effects/Sound/spellEffect.")]
    public string resourcesPath;
}

public enum MobileDeckCardType
{
    Summoner,
    Creature,
    Spell,
    Land
}

public class MobileDeckBuilderController : MonoBehaviour
{
    static readonly DeckData[] EmptyDecks = Array.Empty<DeckData>();

    [Header("Database")]
    [SerializeField] DeckDatabase deckDatabase;
    [SerializeField] DeckRuntimeImageTargetLoader runtimeTargetLoader;
    [SerializeField] bool reloadRuntimeDeckAfterSave = true;

    [Header("Built-in Models")]
    [SerializeField] List<BuiltInModelOption> builtInModels = new();

    [Header("Built-in Sounds")]
    [SerializeField] List<BuiltInSoundOption> builtInSummonSounds = new();

    [Header("Current Deck")]
    [SerializeField] string deckName = "My Mobile Deck";

    [Header("Current Card")]
    [SerializeField] string cardName = "";
    [SerializeField] int quantity = 1;
    [SerializeField] MobileDeckCardType selectedCardType = MobileDeckCardType.Summoner;
    [SerializeField] int health;
    [SerializeField] int damage;
    [SerializeField] int mana;
    [SerializeField] float targetWidthMeters = 0.06f;
    [SerializeField] Vector3 modelScale = Vector3.one;
    [SerializeField] Color modelTint = Color.white;

    [Header("Selected Runtime Assets")]
    [SerializeField] string selectedImagePath = "";
    [SerializeField] string selectedModelResourcePath = "";
    [SerializeField] string selectedCustomModelPath = "";
    [SerializeField] string selectedSummonSfxPath = "";

    [Header("Working Deck")]
    [SerializeField] List<DeckCardEntry> workingCards = new();
    [SerializeField] string statusMessage = "";

    public IReadOnlyList<BuiltInModelOption> BuiltInModels => builtInModels;
    public IReadOnlyList<BuiltInSoundOption> BuiltInSummonSounds => builtInSummonSounds;
    public IReadOnlyList<DeckCardEntry> WorkingCards => workingCards;
    public IReadOnlyList<DeckData> SavedDecks => deckDatabase != null ? deckDatabase.SavedDecks : EmptyDecks;
    public string StatusMessage => statusMessage;
    public string SelectedImagePath => selectedImagePath;
    public string SelectedModelResourcePath => selectedModelResourcePath;
    public string SelectedCustomModelPath => selectedCustomModelPath;
    public string SelectedSummonSfxPath => selectedSummonSfxPath;
    public bool CurrentCardNeedsStats => selectedCardType == MobileDeckCardType.Creature;

    void Awake()
    {
        ResolveSceneReferences();
        EnsureDefaultBuiltInOptions();

        if (deckDatabase != null)
            deckDatabase.LoadDecks();
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

    public void SetQuantityFromText(string value)
    {
        SetQuantity(ParseInt(value, quantity));
    }

    public void SetCardType(int cardTypeIndex)
    {
        selectedCardType = (MobileDeckCardType)Mathf.Clamp(cardTypeIndex, 0, Enum.GetValues(typeof(MobileDeckCardType)).Length - 1);
        SetStatus($"Card type set to {selectedCardType}.");
    }

    public void SetHealthFromText(string value)
    {
        health = Mathf.Max(0, ParseInt(value, health));
    }

    public void SetDamageFromText(string value)
    {
        damage = Mathf.Max(0, ParseInt(value, damage));
    }

    public void SetManaFromText(string value)
    {
        mana = Mathf.Max(0, ParseInt(value, mana));
    }

    public void SetTargetWidthFromText(string value)
    {
        targetWidthMeters = Mathf.Max(0.01f, ParseFloat(value, targetWidthMeters));
    }

    public void SetUniformModelScale(float uniformScale)
    {
        float safeScale = Mathf.Max(0.01f, uniformScale);
        modelScale = new Vector3(safeScale, safeScale, safeScale);
    }

    public void SetUniformModelScaleFromText(string value)
    {
        SetUniformModelScale(ParseFloat(value, modelScale.x));
    }

    public void SetModelTintFromHtml(string htmlColor)
    {
        if (string.IsNullOrWhiteSpace(htmlColor))
        {
            SetStatus("Model color is empty.", true);
            return;
        }

        string colorText = htmlColor.Trim();
        if (!colorText.StartsWith("#", StringComparison.Ordinal))
            colorText = $"#{colorText}";

        if (!ColorUtility.TryParseHtmlString(colorText, out Color parsedColor))
        {
            SetStatus($"Could not read color '{htmlColor}'. Use a hex color like #FFAA00.", true);
            return;
        }

        parsedColor.a = Mathf.Max(0.01f, parsedColor.a);
        modelTint = parsedColor;
        SetStatus($"Model tint set to {colorText}.");
    }

    public void SetModelTint(Color tint)
    {
        tint.a = Mathf.Max(0.01f, tint.a);
        modelTint = tint;
    }

    public void SelectBuiltInModel(int optionIndex)
    {
        if (!IsValidIndex(optionIndex, builtInModels))
        {
            SetStatus("Selected built-in model does not exist.", true);
            return;
        }

        selectedModelResourcePath = builtInModels[optionIndex].resourcesPath?.Trim() ?? string.Empty;
        selectedCustomModelPath = string.Empty;
        SetStatus($"Selected model: {builtInModels[optionIndex].displayName}");
    }

    public void SetModelResourcePath(string resourcePath)
    {
        selectedModelResourcePath = string.IsNullOrWhiteSpace(resourcePath) ? string.Empty : resourcePath.Trim();
        selectedCustomModelPath = string.Empty;
    }

    public void SelectBuiltInSummonSound(int optionIndex)
    {
        if (!IsValidIndex(optionIndex, builtInSummonSounds))
        {
            SetStatus("Selected summon sound does not exist.", true);
            return;
        }

        selectedSummonSfxPath = builtInSummonSounds[optionIndex].resourcesPath?.Trim() ?? string.Empty;
        SetStatus($"Selected summon sound: {builtInSummonSounds[optionIndex].displayName}");
    }

    public void UseImageFilePath(string sourcePath)
    {
        if (RuntimeDeckAssetStore.TryCopyImageFile(sourcePath, CurrentCardFileStem(), out string storedPath, out string error))
        {
            selectedImagePath = storedPath;
            SetStatus("Card image selected.");
            return;
        }

        SetStatus(error, true);
    }

    public void UseImageTexture(Texture2D texture)
    {
        if (RuntimeDeckAssetStore.TrySaveTextureAsPng(texture, CurrentCardFileStem(), out string storedPath, out string error))
        {
            selectedImagePath = storedPath;
            SetStatus("Card image saved.");
            return;
        }

        SetStatus(error, true);
    }

    public void UseCustomModelFilePath(string sourcePath)
    {
        if (RuntimeDeckAssetStore.TryCopyModelFile(sourcePath, CurrentCardFileStem(), out string storedPath, out string error))
        {
            selectedCustomModelPath = storedPath;
            selectedModelResourcePath = string.Empty;
            SetStatus("Custom OBJ model selected.");
            return;
        }

        SetStatus(error, true);
    }

    public void UseSummonAudioFilePath(string sourcePath)
    {
        if (RuntimeDeckAssetStore.TryCopyAudioFile(sourcePath, CurrentCardFileStem(), "summon", out string storedPath, out string error))
        {
            selectedSummonSfxPath = storedPath;
            SetStatus("Summon sound selected.");
            return;
        }

        SetStatus(error, true);
    }

    public void PickImageFromGallery()
    {
        if (!MobileNativeAssetPicker.TryPickImageFromGallery(UseImageFilePath, out string error))
            SetStatus(error, true);
    }

    public void TakePhotoForImageTarget()
    {
        if (!MobileNativeAssetPicker.TryTakePhoto(UseImageFilePath, out string error))
            SetStatus(error, true);
    }

    public void PickSummonAudioFile()
    {
        if (!MobileNativeAssetPicker.TryPickAudioFile(UseSummonAudioFilePath, out string error))
            SetStatus(error, true);
    }

    public void PickCustomModelFile()
    {
        if (!MobileNativeAssetPicker.TryPickModelFile(UseCustomModelFilePath, out string error))
            SetStatus(error, true);
    }

    public void DownloadImageFromUrl(string url)
    {
        StartCoroutine(RuntimeDeckAssetStore.DownloadImage(url, CurrentCardFileStem(), path =>
        {
            selectedImagePath = path;
            SetStatus("Downloaded card image.");
        }, error => SetStatus(error, true)));
    }

    public void DownloadSummonAudioFromUrl(string url)
    {
        StartCoroutine(RuntimeDeckAssetStore.DownloadSummonAudio(url, CurrentCardFileStem(), path =>
        {
            selectedSummonSfxPath = path;
            SetStatus("Downloaded summon sound.");
        }, error => SetStatus(error, true)));
    }

    public void DownloadModelFromUrl(string url)
    {
        StartCoroutine(RuntimeDeckAssetStore.DownloadModel(url, CurrentCardFileStem(), path =>
        {
            selectedCustomModelPath = path;
            selectedModelResourcePath = string.Empty;
            SetStatus("Downloaded custom OBJ model.");
        }, error => SetStatus(error, true)));
    }

    public bool TryAddCurrentCard()
    {
        if (string.IsNullOrWhiteSpace(cardName))
        {
            SetStatus("Card name is required.", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(selectedImagePath) || !File.Exists(selectedImagePath))
        {
            SetStatus("Select or download a card image before adding the card.", true);
            return false;
        }

        var card = new DeckCardEntry
        {
            cardName = cardName.Trim(),
            quantity = Mathf.Max(1, quantity),
            cardType = selectedCardType.ToString(),
            health = health,
            damage = damage,
            mana = mana,
            imagePath = selectedImagePath,
            targetWidthMeters = Mathf.Max(0.01f, targetWidthMeters),
            modelResourcePath = selectedModelResourcePath,
            customModelPath = selectedCustomModelPath,
            modelScale = SafeModelScale(modelScale),
            modelTint = SafeModelTint(modelTint),
            summonSfxPath = selectedSummonSfxPath,
            effectType = CardEffectType.None,
            effectTarget = CardEffectTarget.None,
            effectAmount = 0,
            effectDurationTurns = 0,
            effectManaCost = 0,
            effectSfxPath = string.Empty
        };

        if (selectedCardType == MobileDeckCardType.Creature)
        {
            if (health <= 0 || damage <= 0)
            {
                SetStatus("Creature cards need health and damage values above 0.", true);
                return false;
            }

            if (!card.HasModelResourcePath() && !card.HasCustomModelPath())
            {
                SetStatus("Creature cards need a built-in model or custom OBJ model.", true);
                return false;
            }
        }
        else
        {
            card.health = 0;
            card.damage = 0;
            card.mana = 0;
        }

        workingCards.Add(card);
        SetStatus($"Added {card.quantity}x {card.cardName}.");
        ResetCurrentCardSelection();
        return true;
    }

    public bool TrySaveWorkingDeck()
    {
        if (deckDatabase == null)
        {
            SetStatus("DeckDatabase reference is missing.", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(deckName))
        {
            SetStatus("Deck name is required.", true);
            return false;
        }

        if (workingCards.Count == 0)
        {
            SetStatus("Add at least one card before saving the deck.", true);
            return false;
        }

        var deck = new DeckData
        {
            deckId = BuildDeckId(deckName, CardGameType.MTG),
            deckName = deckName.Trim(),
            gameType = CardGameType.MTG,
            cards = new List<DeckCardEntry>(workingCards)
        };

        deckDatabase.AddOrUpdateDeck(deck);

        if (runtimeTargetLoader != null)
        {
            runtimeTargetLoader.SetDeckIdToLoad(deck.deckId);
            if (reloadRuntimeDeckAfterSave && Application.isPlaying)
                runtimeTargetLoader.ReloadRuntimeDeck();
        }

        SetStatus($"Saved deck '{deck.deckName}' with {deck.cards.Count} cards.");
        return true;
    }

    public void LoadDeckIntoEditor(string deckId)
    {
        if (deckDatabase == null)
        {
            SetStatus("DeckDatabase reference is missing.", true);
            return;
        }

        deckDatabase.LoadDecks();
        DeckData deck = deckDatabase.GetDeck(deckId);
        if (deck == null)
        {
            SetStatus($"Deck '{deckId}' was not found.", true);
            return;
        }

        deckName = deck.deckName;
        workingCards = CloneCards(deck.cards);
        SetStatus($"Loaded deck '{deck.deckName}' for editing.");
    }

    public void RefreshSavedDecks()
    {
        if (deckDatabase == null)
        {
            SetStatus("DeckDatabase reference is missing.", true);
            return;
        }

        deckDatabase.LoadDecks();
        SetStatus($"Loaded {deckDatabase.SavedDecks.Count} saved deck(s).");
    }

    public void SelectDeckForPlay(string deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            SetStatus("Deck id is required.", true);
            return;
        }

        if (runtimeTargetLoader == null)
        {
            SetStatus("DeckRuntimeImageTargetLoader reference is missing.", true);
            return;
        }

        runtimeTargetLoader.SetDeckIdToLoad(deckId, Application.isPlaying);
        SetStatus($"Selected runtime deck '{deckId}'.");
    }

    public void LoadSelectedRuntimeDeck()
    {
        if (runtimeTargetLoader == null)
        {
            SetStatus("DeckRuntimeImageTargetLoader reference is missing.", true);
            return;
        }

        runtimeTargetLoader.LoadRuntimeImageTargets();
        SetStatus("Runtime deck load requested.");
    }

    public void ReloadSelectedRuntimeDeck()
    {
        if (runtimeTargetLoader == null)
        {
            SetStatus("DeckRuntimeImageTargetLoader reference is missing.", true);
            return;
        }

        runtimeTargetLoader.ReloadRuntimeDeck();
        SetStatus("Runtime deck reload requested.");
    }

    public void ClearRuntimeTargets()
    {
        if (runtimeTargetLoader == null)
        {
            SetStatus("DeckRuntimeImageTargetLoader reference is missing.", true);
            return;
        }

        runtimeTargetLoader.ClearRuntimeTargets();
        SetStatus("Runtime AR targets cleared.");
    }

    public void ClearWorkingDeck()
    {
        workingCards.Clear();
        SetStatus("Working deck cleared.");
    }

    public void ResetCurrentCardSelection()
    {
        cardName = string.Empty;
        quantity = 1;
        selectedCardType = MobileDeckCardType.Summoner;
        health = 0;
        damage = 0;
        mana = 0;
        targetWidthMeters = 0.06f;
        modelScale = Vector3.one;
        modelTint = Color.white;
        selectedImagePath = string.Empty;
        selectedModelResourcePath = string.Empty;
        selectedCustomModelPath = string.Empty;
        selectedSummonSfxPath = string.Empty;
    }

    string CurrentCardFileStem()
    {
        return string.IsNullOrWhiteSpace(cardName) ? "card" : cardName.Trim();
    }

    static string BuildDeckId(string name, CardGameType gameType)
    {
        return $"{gameType}-{name}".ToLowerInvariant().Replace(" ", "-");
    }

    void ResolveSceneReferences()
    {
        if (runtimeTargetLoader == null)
            runtimeTargetLoader = FindFirstObjectByType<DeckRuntimeImageTargetLoader>();

        if (deckDatabase == null && runtimeTargetLoader != null)
            deckDatabase = runtimeTargetLoader.DeckDatabase;
    }

    void EnsureDefaultBuiltInOptions()
    {
        if (builtInModels.Count == 0)
        {
            builtInModels.Add(new BuiltInModelOption { displayName = "Fire Dragon", resourcesPath = "Card Models/FireDragon_Prefab" });
            builtInModels.Add(new BuiltInModelOption { displayName = "Fire Mermaid", resourcesPath = "Card Models/FireMermaid_Prefab" });
            builtInModels.Add(new BuiltInModelOption { displayName = "Fire Troll", resourcesPath = "Card Models/FireTroll_Prefab" });
        }

        if (builtInSummonSounds.Count == 0)
        {
            builtInSummonSounds.Add(new BuiltInSoundOption { displayName = "Fire Spawn", resourcesPath = "Effects/Sound/fireSpawn" });
            builtInSummonSounds.Add(new BuiltInSoundOption { displayName = "Dragon Growl", resourcesPath = "Effects/Sound/dragonGrowl" });
            builtInSummonSounds.Add(new BuiltInSoundOption { displayName = "Siren", resourcesPath = "Effects/Sound/sirenSound" });
        }

    }

    List<DeckCardEntry> CloneCards(List<DeckCardEntry> sourceCards)
    {
        var clone = new List<DeckCardEntry>();
        if (sourceCards == null)
            return clone;

        for (int i = 0; i < sourceCards.Count; i++)
        {
            DeckCardEntry source = sourceCards[i];
            if (source == null)
                continue;

            clone.Add(new DeckCardEntry
            {
                cardName = source.cardName,
                quantity = source.quantity,
                cardType = source.cardType,
                health = source.health,
                damage = source.damage,
                mana = source.mana,
                imagePath = source.imagePath,
                targetWidthMeters = source.targetWidthMeters,
                modelResourcePath = source.modelResourcePath,
                customModelPath = source.customModelPath,
                modelScale = source.SafeModelScale(),
                modelTint = source.SafeModelTint(),
                summonSfxPath = source.summonSfxPath,
                effectType = source.effectType,
                effectTarget = source.effectTarget,
                effectAmount = source.effectAmount,
                effectDurationTurns = source.effectDurationTurns,
                effectManaCost = source.effectManaCost,
                effectSfxPath = source.effectSfxPath
            });
        }

        return clone;
    }

    void SetStatus(string message, bool warning = false)
    {
        statusMessage = message;
        if (warning)
            Debug.LogWarning(message, this);
        else
            Debug.Log(message, this);
    }

    static bool IsValidIndex<T>(int index, List<T> list)
    {
        return list != null && index >= 0 && index < list.Count;
    }

    static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, out int parsed) ? parsed : fallback;
    }

    static float ParseFloat(string value, float fallback)
    {
        return float.TryParse(value, out float parsed) ? parsed : fallback;
    }

    static Vector3 SafeModelScale(Vector3 value)
    {
        return new Vector3(
            Mathf.Max(0.01f, value.x),
            Mathf.Max(0.01f, value.y),
            Mathf.Max(0.01f, value.z));
    }

    static Color SafeModelTint(Color value)
    {
        if (value.a <= 0f &&
            Mathf.Approximately(value.r, 0f) &&
            Mathf.Approximately(value.g, 0f) &&
            Mathf.Approximately(value.b, 0f))
        {
            return Color.white;
        }

        value.a = Mathf.Max(0.01f, value.a);
        return value;
    }
}
