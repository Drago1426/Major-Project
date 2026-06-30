#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeckBuilderManager))]
public class DeckBuilderManagerEditor : Editor
{
    SerializedProperty deckDatabase;
    SerializedProperty runtimeTargetLoader;
    SerializedProperty reloadRuntimeTargetsAfterSave;
    SerializedProperty deckName;
    SerializedProperty selectedGameType;
    SerializedProperty cardName;
    SerializedProperty quantity;
    SerializedProperty mtgCardType;
    SerializedProperty pokemonCardType;
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
        selectedGameType = serializedObject.FindProperty("selectedGameType");
        cardName = serializedObject.FindProperty("cardName");
        quantity = serializedObject.FindProperty("quantity");
        mtgCardType = serializedObject.FindProperty("mtgCardType");
        pokemonCardType = serializedObject.FindProperty("pokemonCardType");
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
        EditorGUILayout.PropertyField(selectedGameType);

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

    void DrawCardSection()
    {
        EditorGUILayout.LabelField("Card", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cardName);
        EditorGUILayout.PropertyField(quantity);

        var gameType = (CardGameType)selectedGameType.enumValueIndex;
        EditorGUILayout.PropertyField(gameType == CardGameType.MTG ? mtgCardType : pokemonCardType);
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
        EditorGUILayout.PropertyField(cardEffectSfxClip);

        showAudioPaths = EditorGUILayout.Foldout(showAudioPaths, "Sound File Paths", true);
        if (showAudioPaths)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cardSummonSfxPath);
            EditorGUILayout.PropertyField(cardEffectSfxPath);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Spell / Rule Effect", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(cardEffectType);
        EditorGUILayout.PropertyField(cardEffectTarget);
        EditorGUILayout.PropertyField(cardEffectAmount);
        EditorGUILayout.PropertyField(cardEffectDurationTurns);
        EditorGUILayout.PropertyField(cardEffectManaCost);

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

        if (GUILayout.Button("Clear", GUILayout.Height(28)))
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
