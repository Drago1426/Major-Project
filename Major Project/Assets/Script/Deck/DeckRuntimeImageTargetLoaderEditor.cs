#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeckRuntimeImageTargetLoader))]
public class DeckRuntimeImageTargetLoaderEditor : Editor
{
    SerializedProperty deckDatabase;
    SerializedProperty deckIdToLoad;
    SerializedProperty autoLoadOnStart;
    SerializedProperty reloadDatabaseBeforeLoading;
    SerializedProperty runtimeTargetNamePrefix;
    SerializedProperty addCardDetectorToRuntimeTargets;
    SerializedProperty addSummonOnTargetFoundToRuntimeTargets;
    SerializedProperty disableSceneTargetsMatchingLoadedDeck;
    SerializedProperty defaultRuntimeCreaturePrefab;
    SerializedProperty cardModelOverrides;
    SerializedProperty useSingleVisibleRuntimeCreatureLock;
    SerializedProperty hideRuntimeCreatureOnTargetLost;
    SerializedProperty createRuntimeFireTornado;
    SerializedProperty runtimeCreatureStartLocalPos;
    SerializedProperty runtimeCreatureEndLocalPos;
    SerializedProperty runtimeCreatureRiseDuration;
    SerializedProperty runtimeFireTornadoStopDelay;
    SerializedProperty healthIcon;
    SerializedProperty manaIcon;
    SerializedProperty damageIcon;
    SerializedProperty addStatsDisplayToRuntimeModels;

    static bool showModelOverrides;
    static bool showSummonSettings;
    static bool showStatsSettings;
    static bool showAdvanced;

    void OnEnable()
    {
        deckDatabase = serializedObject.FindProperty("deckDatabase");
        deckIdToLoad = serializedObject.FindProperty("deckIdToLoad");
        autoLoadOnStart = serializedObject.FindProperty("autoLoadOnStart");
        reloadDatabaseBeforeLoading = serializedObject.FindProperty("reloadDatabaseBeforeLoading");
        runtimeTargetNamePrefix = serializedObject.FindProperty("runtimeTargetNamePrefix");
        addCardDetectorToRuntimeTargets = serializedObject.FindProperty("addCardDetectorToRuntimeTargets");
        addSummonOnTargetFoundToRuntimeTargets = serializedObject.FindProperty("addSummonOnTargetFoundToRuntimeTargets");
        disableSceneTargetsMatchingLoadedDeck = serializedObject.FindProperty("disableSceneTargetsMatchingLoadedDeck");
        defaultRuntimeCreaturePrefab = serializedObject.FindProperty("defaultRuntimeCreaturePrefab");
        cardModelOverrides = serializedObject.FindProperty("cardModelOverrides");
        useSingleVisibleRuntimeCreatureLock = serializedObject.FindProperty("useSingleVisibleRuntimeCreatureLock");
        hideRuntimeCreatureOnTargetLost = serializedObject.FindProperty("hideRuntimeCreatureOnTargetLost");
        createRuntimeFireTornado = serializedObject.FindProperty("createRuntimeFireTornado");
        runtimeCreatureStartLocalPos = serializedObject.FindProperty("runtimeCreatureStartLocalPos");
        runtimeCreatureEndLocalPos = serializedObject.FindProperty("runtimeCreatureEndLocalPos");
        runtimeCreatureRiseDuration = serializedObject.FindProperty("runtimeCreatureRiseDuration");
        runtimeFireTornadoStopDelay = serializedObject.FindProperty("runtimeFireTornadoStopDelay");
        healthIcon = serializedObject.FindProperty("healthIcon");
        manaIcon = serializedObject.FindProperty("manaIcon");
        damageIcon = serializedObject.FindProperty("damageIcon");
        addStatsDisplayToRuntimeModels = serializedObject.FindProperty("addStatsDisplayToRuntimeModels");

        var loader = (DeckRuntimeImageTargetLoader)target;
        if (loader.DeckDatabase != null)
            loader.DeckDatabase.LoadDecks();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawBasicSetup();
        DrawActions();
        DrawOptionalSections();
        DrawAdvanced();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawActions()
    {
        var loader = (DeckRuntimeImageTargetLoader)target;

        EditorGUILayout.LabelField("Runtime Deck Actions", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load", GUILayout.Height(28)))
            RunAction(loader.LoadRuntimeImageTargets);

        if (GUILayout.Button("Reload", GUILayout.Height(28)))
            RunAction(loader.ReloadRuntimeDeck);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Targets"))
            RunAction(loader.ClearRuntimeTargets);

        if (GUILayout.Button("Print Loaded"))
            RunAction(loader.PrintLoadedRuntimeTargets);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Clear Loaded Cache"))
            RunAction(loader.ClearLoadedRuntimeTargetCache);

        EditorGUILayout.Space(8);
    }

    void DrawBasicSetup()
    {
        EditorGUILayout.LabelField("Deck Source", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(deckDatabase);
        DrawSavedDeckSelector();
        EditorGUILayout.PropertyField(deckIdToLoad);
        EditorGUILayout.PropertyField(autoLoadOnStart);
        EditorGUILayout.PropertyField(defaultRuntimeCreaturePrefab);
        EditorGUILayout.Space(8);
    }

    void DrawSavedDeckSelector()
    {
        var database = deckDatabase.objectReferenceValue as DeckDatabase;
        if (database == null)
        {
            EditorGUILayout.HelpBox("Assign a DeckDatabase to select saved decks.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Saved Decks", GUILayout.Height(24)))
        {
            database.LoadDecks();
            EditorUtility.SetDirty(database);
        }
        EditorGUILayout.EndHorizontal();

        var decks = database.SavedDecks;
        if (decks.Count == 0)
        {
            EditorGUILayout.HelpBox("No saved decks found. Save a deck first, then refresh this list.", MessageType.Info);
            return;
        }

        string currentDeckId = deckIdToLoad.stringValue;
        string[] options = new string[decks.Count + 1];
        options[0] = "Select a deck...";
        int selectedIndex = 0;

        for (int i = 0; i < decks.Count; i++)
        {
            DeckData deck = decks[i];
            string deckId = deck?.deckId ?? string.Empty;
            string deckName = string.IsNullOrWhiteSpace(deck?.deckName) ? deckId : deck.deckName;
            options[i + 1] = $"{deckName} ({deckId})";

            if (!string.IsNullOrWhiteSpace(currentDeckId) &&
                string.Equals(currentDeckId, deckId, System.StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i + 1;
            }
        }

        EditorGUI.BeginChangeCheck();
        int nextIndex = EditorGUILayout.Popup("Saved Deck", selectedIndex, options);
        if (EditorGUI.EndChangeCheck())
        {
            deckIdToLoad.stringValue = nextIndex > 0 && nextIndex <= decks.Count
                ? decks[nextIndex - 1].deckId
                : string.Empty;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }

    void DrawOptionalSections()
    {
        showModelOverrides = EditorGUILayout.Foldout(showModelOverrides, "Model Overrides", true);
        if (showModelOverrides)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cardModelOverrides, true);
            EditorGUI.indentLevel--;
        }

        showSummonSettings = EditorGUILayout.Foldout(showSummonSettings, "Summon Settings", true);
        if (showSummonSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useSingleVisibleRuntimeCreatureLock);
            EditorGUILayout.PropertyField(hideRuntimeCreatureOnTargetLost);
            EditorGUILayout.PropertyField(createRuntimeFireTornado);
            EditorGUILayout.PropertyField(runtimeCreatureStartLocalPos);
            EditorGUILayout.PropertyField(runtimeCreatureEndLocalPos);
            EditorGUILayout.PropertyField(runtimeCreatureRiseDuration);
            EditorGUILayout.PropertyField(runtimeFireTornadoStopDelay);
            EditorGUI.indentLevel--;
        }

        showStatsSettings = EditorGUILayout.Foldout(showStatsSettings, "Stats Display", true);
        if (showStatsSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(addStatsDisplayToRuntimeModels);
            EditorGUILayout.PropertyField(healthIcon);
            EditorGUILayout.PropertyField(manaIcon);
            EditorGUILayout.PropertyField(damageIcon);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);
    }

    void DrawAdvanced()
    {
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
        if (!showAdvanced)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(reloadDatabaseBeforeLoading);
        EditorGUILayout.PropertyField(runtimeTargetNamePrefix);
        EditorGUILayout.PropertyField(addCardDetectorToRuntimeTargets);
        EditorGUILayout.PropertyField(addSummonOnTargetFoundToRuntimeTargets);
        EditorGUILayout.PropertyField(disableSceneTargetsMatchingLoadedDeck);
        EditorGUI.indentLevel--;
    }

    void RunAction(System.Action action)
    {
        serializedObject.ApplyModifiedProperties();
        action?.Invoke();
        EditorUtility.SetDirty(target);
    }
}
#endif
