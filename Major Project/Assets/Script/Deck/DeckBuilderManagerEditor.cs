using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeckBuilderManager))]
public class DeckBuilderManagerEditor : Editor
{
    SerializedProperty deckDatabase;
    SerializedProperty deckName;
    SerializedProperty selectedGameType;
    SerializedProperty cardName;
    SerializedProperty quantity;
    SerializedProperty mtgCardType;
    SerializedProperty pokemonCardType;
    SerializedProperty health;
    SerializedProperty damage;
    SerializedProperty mana;
    SerializedProperty workingCards;

    void OnEnable()
    {
        deckDatabase = serializedObject.FindProperty("deckDatabase");
        deckName = serializedObject.FindProperty("deckName");
        selectedGameType = serializedObject.FindProperty("selectedGameType");
        cardName = serializedObject.FindProperty("cardName");
        quantity = serializedObject.FindProperty("quantity");
        mtgCardType = serializedObject.FindProperty("mtgCardType");
        pokemonCardType = serializedObject.FindProperty("pokemonCardType");
        health = serializedObject.FindProperty("health");
        damage = serializedObject.FindProperty("damage");
        mana = serializedObject.FindProperty("mana");
        workingCards = serializedObject.FindProperty("workingCards");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(deckDatabase);
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Current Deck", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(deckName);
        EditorGUILayout.PropertyField(selectedGameType);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Card Input", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cardName);
        EditorGUILayout.PropertyField(quantity);

        var gameType = (CardGameType)selectedGameType.enumValueIndex;
        if (gameType == CardGameType.MTG)
            EditorGUILayout.PropertyField(mtgCardType);
        else
            EditorGUILayout.PropertyField(pokemonCardType);

        var manager = (DeckBuilderManager)target;
        if (manager.CurrentCardNeedsCombatStats())
        {
            EditorGUILayout.HelpBox("This card type needs Health, Damage, and Mana.", MessageType.Info);
            EditorGUILayout.PropertyField(health);
            EditorGUILayout.PropertyField(damage);
            EditorGUILayout.PropertyField(mana);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.PropertyField(workingCards, true);

        EditorGUILayout.Space(10);
        DrawActionButtons();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawActionButtons()
    {
        var manager = (DeckBuilderManager)target;

        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Card"))
        {
            serializedObject.ApplyModifiedProperties();
            bool added = manager.TryAddCardToWorkingDeck();
            if (added)
                EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("Clear Working Deck"))
        {
            serializedObject.ApplyModifiedProperties();
            manager.ClearWorkingDeck();
            EditorUtility.SetDirty(manager);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Save Working Deck"))
        {
            serializedObject.ApplyModifiedProperties();
            manager.TrySaveWorkingDeck();
            EditorUtility.SetDirty(manager);
        }
    }
}