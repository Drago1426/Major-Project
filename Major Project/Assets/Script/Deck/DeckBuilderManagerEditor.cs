#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeckBuilderManager))]
public class DeckBuilderManagerEditor : Editor
{
    SerializedProperty deckDatabase;
    SerializedProperty runtimeTargetLoader;
    SerializedProperty reloadRuntimeTargetsAfterSave;
    SerializedProperty deckName;
    SerializedProperty editingDeckId;
    SerializedProperty selectedSavedDeckId;
    SerializedProperty cardName;
    SerializedProperty quantity;
    SerializedProperty cardType;
    SerializedProperty cardImage;
    SerializedProperty targetWidthMeters;
    SerializedProperty cardModelPrefab;
    SerializedProperty cardModelResourcePath;
    SerializedProperty cardCustomModelPath;
    SerializedProperty cardModelScale;
    SerializedProperty cardModelTint;
    SerializedProperty cardSummonSfxClip;
    SerializedProperty cardSummonSfxPath;
    SerializedProperty cardEffectSfxClip;
    SerializedProperty cardEffectSfxPath;
    SerializedProperty cardEffectType;
    SerializedProperty cardEffectTarget;
    SerializedProperty cardEffectAmount;
    SerializedProperty cardEffectDurationTurns;
    SerializedProperty cardEffectManaCost;
    SerializedProperty health;
    SerializedProperty damage;
    SerializedProperty mana;
    SerializedProperty workingCards;

    static bool showModelOptions;
    static bool showAudioPaths;
    static bool showRuntimeReload;
    static bool showWorkingCards = true;

    void OnEnable()
    {
        deckDatabase = serializedObject.FindProperty("deckDatabase");
        runtimeTargetLoader = serializedObject.FindProperty("runtimeTargetLoader");
        reloadRuntimeTargetsAfterSave = serializedObject.FindProperty("reloadRuntimeTargetsAfterSave");
        deckName = serializedObject.FindProperty("deckName");
        editingDeckId = serializedObject.FindProperty("editingDeckId");
        selectedSavedDeckId = serializedObject.FindProperty("selectedSavedDeckId");
        cardName = serializedObject.FindProperty("cardName");
        quantity = serializedObject.FindProperty("quantity");
        cardType = serializedObject.FindProperty("cardType");
        cardImage = serializedObject.FindProperty("cardImage");
        targetWidthMeters = serializedObject.FindProperty("targetWidthMeters");
        cardModelPrefab = serializedObject.FindProperty("cardModelPrefab");
        cardModelResourcePath = serializedObject.FindProperty("cardModelResourcePath");
        cardCustomModelPath = serializedObject.FindProperty("cardCustomModelPath");
        cardModelScale = serializedObject.FindProperty("cardModelScale");
        cardModelTint = serializedObject.FindProperty("cardModelTint");
        cardSummonSfxClip = serializedObject.FindProperty("cardSummonSfxClip");
        cardSummonSfxPath = serializedObject.FindProperty("cardSummonSfxPath");
        cardEffectSfxClip = serializedObject.FindProperty("cardEffectSfxClip");
        cardEffectSfxPath = serializedObject.FindProperty("cardEffectSfxPath");
        cardEffectType = serializedObject.FindProperty("cardEffectType");
        cardEffectTarget = serializedObject.FindProperty("cardEffectTarget");
        cardEffectAmount = serializedObject.FindProperty("cardEffectAmount");
        cardEffectDurationTurns = serializedObject.FindProperty("cardEffectDurationTurns");
        cardEffectManaCost = serializedObject.FindProperty("cardEffectManaCost");
        health = serializedObject.FindProperty("health");
        damage = serializedObject.FindProperty("damage");
        mana = serializedObject.FindProperty("mana");
        workingCards = serializedObject.FindProperty("workingCards");

        var manager = (DeckBuilderManager)target;
        if (manager.ResolveSceneReferences())
        {
            EditorUtility.SetDirty(manager);
            serializedObject.Update();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDeckSection();
        DrawCardSection();
        DrawWorkingDeckSection();
        DrawActionButtons();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawDeckSection()
    {
        EditorGUILayout.LabelField("Deck", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(deckDatabase);
        EditorGUILayout.PropertyField(deckName);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(editingDeckId, new GUIContent("Editing Deck Id"));
        EditorGUI.EndDisabledGroup();

        DrawSavedDeckPicker();

        showRuntimeReload = EditorGUILayout.Foldout(showRuntimeReload, "Runtime Reload", true);
        if (showRuntimeReload)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(runtimeTargetLoader);
            EditorGUILayout.PropertyField(reloadRuntimeTargetsAfterSave);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);
    }

    void DrawSavedDeckPicker()
    {
        var manager = (DeckBuilderManager)target;
        var database = deckDatabase.objectReferenceValue as DeckDatabase;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Edit Saved Deck", EditorStyles.miniBoldLabel);

        if (database == null)
        {
            EditorGUILayout.HelpBox("Assign a DeckDatabase to load and edit saved decks.", MessageType.Info);
            return;
        }

        var decks = database.SavedDecks;
        if (decks.Count == 0)
        {
            EditorGUILayout.HelpBox("No saved decks are loaded. Press Refresh Saved Decks if you created one during Play Mode.", MessageType.Info);
        }
        else
        {
            string[] options = new string[decks.Count];
            int selectedIndex = 0;
            string currentDeckId = selectedSavedDeckId.stringValue;

            for (int i = 0; i < decks.Count; i++)
            {
                DeckData deck = decks[i];
                string id = deck?.deckId ?? string.Empty;
                string name = string.IsNullOrWhiteSpace(deck?.deckName) ? id : deck.deckName;
                options[i] = $"{name} ({id})";

                if (!string.IsNullOrWhiteSpace(currentDeckId) &&
                    string.Equals(currentDeckId, id, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                }
            }

            int nextIndex = EditorGUILayout.Popup("Saved Deck", selectedIndex, options);
            if (nextIndex >= 0 && nextIndex < decks.Count)
                selectedSavedDeckId.stringValue = decks[nextIndex].deckId;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Saved Decks"))
        {
            serializedObject.ApplyModifiedProperties();
            manager.RefreshSavedDecks();
            serializedObject.Update();
        }

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(selectedSavedDeckId.stringValue)))
        {
            if (GUILayout.Button("Load Selected For Editing"))
            {
                serializedObject.ApplyModifiedProperties();
                if (manager.LoadDeckIntoEditor(selectedSavedDeckId.stringValue))
                {
                    EditorUtility.SetDirty(manager);
                    serializedObject.Update();
                }
            }
        }

        if (GUILayout.Button("New Deck"))
        {
            serializedObject.ApplyModifiedProperties();
            manager.StartNewDeck();
            EditorUtility.SetDirty(manager);
            serializedObject.Update();
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawCardSection()
    {
        EditorGUILayout.LabelField("Card", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cardName);
        EditorGUILayout.PropertyField(quantity);
        EditorGUILayout.PropertyField(cardType, new GUIContent("Card Type"));
        EditorGUILayout.PropertyField(cardImage);

        var manager = (DeckBuilderManager)target;
        if (manager.CurrentCardNeedsCombatStats())
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Stats", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(health);
            EditorGUILayout.PropertyField(damage);
            EditorGUILayout.PropertyField(mana);
        }

        showModelOptions = EditorGUILayout.Foldout(showModelOptions, "Model And Target", true);
        if (showModelOptions)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(targetWidthMeters);
            EditorGUILayout.PropertyField(cardModelPrefab);
            EditorGUILayout.PropertyField(cardModelResourcePath);
            EditorGUILayout.PropertyField(cardCustomModelPath);
            EditorGUILayout.PropertyField(cardModelScale);
            EditorGUILayout.PropertyField(cardModelTint);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Sounds", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(cardSummonSfxClip);
        if ((DeckCardType)cardType.enumValueIndex == DeckCardType.Spell)
            EditorGUILayout.PropertyField(cardEffectSfxClip);

        showAudioPaths = EditorGUILayout.Foldout(showAudioPaths, "Sound File Paths", true);
        if (showAudioPaths)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cardSummonSfxPath);
            if ((DeckCardType)cardType.enumValueIndex == DeckCardType.Spell)
                EditorGUILayout.PropertyField(cardEffectSfxPath);
            EditorGUI.indentLevel--;
        }

        if ((DeckCardType)cardType.enumValueIndex == DeckCardType.Spell)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Spell / Rule Effect", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(cardEffectType);
            EditorGUILayout.PropertyField(cardEffectTarget);
            EditorGUILayout.PropertyField(cardEffectAmount);
            EditorGUILayout.PropertyField(cardEffectDurationTurns);
            EditorGUILayout.PropertyField(cardEffectManaCost);
        }

        EditorGUILayout.Space(8);
    }

    void DrawWorkingDeckSection()
    {
        int count = workingCards != null && workingCards.isArray ? workingCards.arraySize : 0;
        showWorkingCards = EditorGUILayout.Foldout(showWorkingCards, $"Working Deck ({count})", true);
        if (showWorkingCards)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(workingCards, true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);
    }

    void DrawActionButtons()
    {
        var manager = (DeckBuilderManager)target;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Card", GUILayout.Height(28)))
        {
            serializedObject.ApplyModifiedProperties();
            if (manager.TryAddCardToWorkingDeck())
                EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("Clear Working Deck", GUILayout.Height(28)))
        {
            serializedObject.ApplyModifiedProperties();
            manager.ClearWorkingDeck();
            EditorUtility.SetDirty(manager);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Save Deck", GUILayout.Height(32)))
        {
            serializedObject.ApplyModifiedProperties();
            manager.TrySaveWorkingDeck();
            EditorUtility.SetDirty(manager);
        }
    }
}
#endif
