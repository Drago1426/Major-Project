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
    SerializedProperty enableFireballAbilityCards;
    SerializedProperty fireballAbilityCardKeyword;
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
        enableFireballAbilityCards = serializedObject.FindProperty("enableFireballAbilityCards");
        fireballAbilityCardKeyword = serializedObject.FindProperty("fireballAbilityCardKeyword");
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
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawActions();
        DrawBasicSetup();
        DrawAbilityCards();
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
        EditorGUILayout.PropertyField(deckIdToLoad);
        EditorGUILayout.PropertyField(autoLoadOnStart);
        EditorGUILayout.PropertyField(defaultRuntimeCreaturePrefab);
        EditorGUILayout.Space(8);
    }

    void DrawAbilityCards()
    {
        EditorGUILayout.LabelField("Ability Cards", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableFireballAbilityCards);
        if (enableFireballAbilityCards.boolValue)
            EditorGUILayout.PropertyField(fireballAbilityCardKeyword);

        EditorGUILayout.Space(8);
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
