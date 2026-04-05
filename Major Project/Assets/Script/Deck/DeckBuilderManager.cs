using System;
using System.Collections.Generic;
using UnityEngine;

public class DeckBuilderManager : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] DeckDatabase deckDatabase;

    [Header("Current Deck")]
    [SerializeField] string deckName = "My Deck";
    [SerializeField] CardGameType selectedGameType = CardGameType.MTG;

    [Header("Card Input")]
    [SerializeField] string cardName = "";
    [SerializeField] int quantity = 1;
    [SerializeField] MtgCardType mtgCardType = MtgCardType.Creature;
    [SerializeField] PokemonCardType pokemonCardType = PokemonCardType.Pokemon;

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

        var card = new DeckCardEntry
        {
            cardName = cardName.Trim(),
            quantity = quantity,
            cardType = CurrentSelectedCardType(),
            health = health,
            damage = damage,
            mana = mana
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
}