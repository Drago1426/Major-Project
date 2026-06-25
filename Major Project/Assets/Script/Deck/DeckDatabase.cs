using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum CardGameType
{
    MTG,
    PokemonTCG
}

public enum MtgCardType
{
    Creature,
    Spell,
    Land,
    Artifact,
    Enchantment,
    Planeswalker,
    Commander,
    Elemental
}

public enum PokemonCardType
{
    Pokemon,
    Trainer,
    Energy,
    Item,
    Supporter,
    Stadium,
    Tool
}

[Serializable]
public class DeckCardEntry
{
    public string cardName;
    public int quantity = 1;

    [Header("Card Type (stored as text for selected game)")]
    public string cardType;

    [Header("Only required for creatures / pokemon")]
    public int health;
    public int damage;
    public int mana;

    [Header("AR Image Target")]
    [Tooltip("Absolute path to the card image saved on device.")]
    public string imagePath;
    [Tooltip("Physical width in meters used when creating a runtime image target.")]
    public float targetWidthMeters = 0.06f;
    [Tooltip("Resources path to load this card's summon prefab (without file extension).")]
    public string modelResourcePath;
    [Tooltip("Absolute path to a runtime-imported model file. Runtime OBJ files are supported by the mobile deck builder.")]
    public string customModelPath;
    [Tooltip("Local scale applied to the spawned model.")]
    public Vector3 modelScale = Vector3.one;
    [Tooltip("Tint applied to the spawned model renderers.")]
    public Color modelTint = Color.white;

    [Header("Audio")]
    [Tooltip("Absolute file path or Resources path for the sound played when this card summons.")]
    public string summonSfxPath;
    [Tooltip("Absolute file path or Resources path for the sound played when this card fires on interaction.")]
    public string fireballSfxPath;

    public bool NeedsCombatStats(CardGameType gameType)
    {
        if (gameType == CardGameType.MTG)
            return string.Equals(cardType, MtgCardType.Creature.ToString(), StringComparison.OrdinalIgnoreCase);

        return string.Equals(cardType, PokemonCardType.Pokemon.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public bool HasImageTarget()
    {
        return !string.IsNullOrWhiteSpace(imagePath);
    }

    public bool HasModelResourcePath()
    {
        return !string.IsNullOrWhiteSpace(modelResourcePath);
    }

    public bool HasCustomModelPath()
    {
        return !string.IsNullOrWhiteSpace(customModelPath);
    }

    public bool HasSummonSfxPath()
    {
        return !string.IsNullOrWhiteSpace(summonSfxPath);
    }

    public bool HasFireballSfxPath()
    {
        return !string.IsNullOrWhiteSpace(fireballSfxPath);
    }

    public Vector3 SafeModelScale()
    {
        bool hasScale = Mathf.Abs(modelScale.x) > Mathf.Epsilon ||
            Mathf.Abs(modelScale.y) > Mathf.Epsilon ||
            Mathf.Abs(modelScale.z) > Mathf.Epsilon;

        if (!hasScale)
            return Vector3.one;

        return new Vector3(
            Mathf.Max(0.01f, modelScale.x),
            Mathf.Max(0.01f, modelScale.y),
            Mathf.Max(0.01f, modelScale.z));
    }

    public Color SafeModelTint()
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
}

[Serializable]
public class DeckData
{
    public string deckId;
    public string deckName;
    public CardGameType gameType;
    public List<DeckCardEntry> cards = new();
}

[Serializable]
class DeckDataList
{
    public List<DeckData> decks = new();
}

[CreateAssetMenu(fileName = "DeckDatabase", menuName = "AR Card Game/Deck Database")]
public class DeckDatabase : ScriptableObject
{
    [SerializeField] List<DeckData> savedDecks = new();

    public IReadOnlyList<DeckData> SavedDecks => savedDecks;

    string SavePath => Path.Combine(Application.persistentDataPath, "saved_decks.json");

    void OnEnable()
    {
        LoadDecks();
    }

    public void LoadDecks()
    {
        if (!File.Exists(SavePath))
        {
            savedDecks.Clear();
            return;
        }

        var json = File.ReadAllText(SavePath);
        var loaded = JsonUtility.FromJson<DeckDataList>(json);
        savedDecks = loaded?.decks ?? new List<DeckData>();
    }

    public void SaveDecks()
    {
        var payload = new DeckDataList { decks = savedDecks };
        var json = JsonUtility.ToJson(payload, true);
        File.WriteAllText(SavePath, json);
    }

    public void AddOrUpdateDeck(DeckData deck)
    {
        if (deck == null || string.IsNullOrWhiteSpace(deck.deckId))
        {
            Debug.LogWarning("Cannot save deck. deckId is required.");
            return;
        }

        if (deck.cards == null)
            deck.cards = new List<DeckCardEntry>();

        for (int i = 0; i < savedDecks.Count; i++)
        {
            if (savedDecks[i].deckId.Equals(deck.deckId, StringComparison.OrdinalIgnoreCase))
            {
                savedDecks[i] = deck;
                SaveDecks();
                return;
            }
        }

        savedDecks.Add(deck);
        SaveDecks();
    }

    public DeckData GetDeck(string deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId))
            return null;

        for (int i = 0; i < savedDecks.Count; i++)
        {
            if (savedDecks[i].deckId.Equals(deckId, StringComparison.OrdinalIgnoreCase))
                return savedDecks[i];
        }

        return null;
    }

    public bool RemoveDeck(string deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId))
            return false;

        var removed = savedDecks.RemoveAll(deck =>
            !string.IsNullOrWhiteSpace(deck?.deckId) &&
            deck.deckId.Equals(deckId, StringComparison.OrdinalIgnoreCase)) > 0;

        if (removed)
            SaveDecks();

        return removed;
    }
}
