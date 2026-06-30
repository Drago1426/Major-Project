#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ARDeckBuilderWindow : EditorWindow
{
    const string PreviewScenePath = "Assets/Scenes/Card Detection Test.unity";
    const string ImportedModelFolder = "Assets/Resources/Card Models/Imported";
    const string GeneratedOutputFolder = "Assets/Generated/AR Deck Builder";

    DeckBuilderManager manager;
    SerializedObject managerObject;
    SerializedProperty deckDatabase;
    SerializedProperty runtimeTargetLoader;
    SerializedProperty reloadRuntimeTargetsAfterSave;
    SerializedProperty deckName;
    SerializedProperty editingDeckId;
    SerializedProperty selectedSavedDeckId;
    SerializedProperty workingCards;

    Vector2 scroll;
    string statusMessage = "Ready.";
    bool showWorkingDeck = true;
    bool showAdvanced;

    string cardName = string.Empty;
    int quantity = 1;
    DeckCardType cardType = DeckCardType.Creature;
    Texture2D cardImage;
    float targetWidthMeters = 0.06f;
    int health = 5;
    int damage = 2;
    int mana = 1;
    GameObject modelAsset;
    string modelResourcePath = string.Empty;
    string customModelPath = string.Empty;
    Vector3 modelScale = Vector3.one;
    Color modelTint = Color.white;
    AudioClip summonSfxClip;
    AudioClip effectSfxClip;
    string summonSfxPath = string.Empty;
    string effectSfxPath = string.Empty;
    CardEffectType effectType = CardEffectType.Attack;
    CardEffectTarget effectTarget = CardEffectTarget.EnemyCreature;
    int effectAmount = 1;
    int effectDurationTurns = 0;
    int effectManaCost = 0;

    [MenuItem("Tools/AR Deck Builder")]
    public static void Open()
    {
        GetWindow<ARDeckBuilderWindow>("AR Deck Builder");
    }

    void OnEnable()
    {
        RefreshSceneReferences();
    }

    void OnGUI()
    {
        DrawHeader();
        DrawManagerPicker();

        if (manager == null)
        {
            DrawMissingManager();
            return;
        }

        RefreshSerializedObject();
        managerObject.Update();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawDeckSetup();
        DrawCardCreator();
        DrawValidation();
        DrawOutputActions();
        DrawWorkingDeck();
        DrawPreviewActions();
        EditorGUILayout.EndScrollView();

        managerObject.ApplyModifiedProperties();
    }

    void DrawHeader()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("AR Deck Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Build an AR-ready deck from card images, model assets, sounds, and stats. This editor tool keeps the four card categories simple: Summoner, Creature, Spell, and Land.",
            MessageType.Info);
    }

    void DrawManagerPicker()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUI.BeginChangeCheck();
        manager = (DeckBuilderManager)EditorGUILayout.ObjectField("Deck Builder", manager, typeof(DeckBuilderManager), true);
        if (EditorGUI.EndChangeCheck())
            RefreshSerializedObject();

        if (GUILayout.Button("Find", GUILayout.Width(70)))
            RefreshSceneReferences();
        EditorGUILayout.EndHorizontal();
    }

    void DrawMissingManager()
    {
        EditorGUILayout.HelpBox("No DeckBuilderManager was found in the current scene.", MessageType.Warning);
        if (GUILayout.Button("Create Deck Builder In Scene", GUILayout.Height(32)))
            CreateManagerInScene();
    }

    void DrawDeckSetup()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("1. Deck Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(deckDatabase);
        EditorGUILayout.PropertyField(deckName, new GUIContent("Deck Name"));
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(editingDeckId, new GUIContent("Editing Deck Id"));
        EditorGUI.EndDisabledGroup();

        DrawSavedDeckEditor();

        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Runtime Setup", true);
        if (showAdvanced)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(runtimeTargetLoader);
            EditorGUILayout.PropertyField(reloadRuntimeTargetsAfterSave);
            EditorGUI.indentLevel--;

            if (GUILayout.Button("Auto Fill Scene References"))
                AutoAssignSceneReferences();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Deck", GUILayout.Height(30)))
            SaveDeck();

        if (GUILayout.Button("Clear Working Deck", GUILayout.Height(30)))
            ClearWorkingDeck();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    void DrawSavedDeckEditor()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Edit Existing Deck", EditorStyles.miniBoldLabel);

        var database = deckDatabase.objectReferenceValue as DeckDatabase;
        if (database == null)
        {
            EditorGUILayout.HelpBox("Assign a DeckDatabase before loading saved decks.", MessageType.Info);
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
        if (GUILayout.Button("Refresh Saved Decks", GUILayout.Height(26)))
        {
            managerObject.ApplyModifiedProperties();
            manager.RefreshSavedDecks();
            managerObject.Update();
            statusMessage = "Saved deck list refreshed.";
        }

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(selectedSavedDeckId.stringValue)))
        {
            if (GUILayout.Button("Load Selected For Editing", GUILayout.Height(26)))
                LoadSelectedDeckForEditing();
        }

        if (GUILayout.Button("New Deck", GUILayout.Height(26), GUILayout.Width(100)))
            StartNewDeck();

        EditorGUILayout.EndHorizontal();
    }

    void DrawCardCreator()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. Card Creator", EditorStyles.boldLabel);

        cardName = EditorGUILayout.TextField("Card Name", cardName);
        quantity = Mathf.Max(1, EditorGUILayout.IntField("Quantity", quantity));
        cardType = (DeckCardType)EditorGUILayout.EnumPopup("Card Type", cardType);
        cardImage = (Texture2D)EditorGUILayout.ObjectField("Card Image", cardImage, typeof(Texture2D), false);
        targetWidthMeters = Mathf.Max(0.01f, EditorGUILayout.FloatField("Target Width (m)", targetWidthMeters));

        if (cardType == DeckCardType.Creature)
            DrawCreatureFields();

        if (cardType == DeckCardType.Spell)
            DrawSpellRuleFields();

        DrawModelFields();
        DrawAudioFields();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Card To Working Deck", GUILayout.Height(32)))
            AddCardToWorkingDeck();

        if (GUILayout.Button("Reset Card Form", GUILayout.Height(32), GUILayout.Width(150)))
            ResetCardForm();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(statusMessage, MessageType.None);
        EditorGUILayout.EndVertical();
    }

    void DrawCreatureFields()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Creature Stats", EditorStyles.miniBoldLabel);
        health = Mathf.Max(0, EditorGUILayout.IntField("Health", health));
        damage = Mathf.Max(0, EditorGUILayout.IntField("Damage", damage));
        mana = Mathf.Max(0, EditorGUILayout.IntField("Mana", mana));
    }

    void DrawModelFields()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("3D Model", EditorStyles.miniBoldLabel);

        modelAsset = (GameObject)EditorGUILayout.ObjectField("Model / Prefab", modelAsset, typeof(GameObject), false);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use Selected Model"))
            UseSelectedModelAsset();

        if (GUILayout.Button("Copy Selected To Resources"))
            CopySelectedModelToResources();

        if (GUILayout.Button("Import FBX/OBJ"))
            ImportExternalModel();
        EditorGUILayout.EndHorizontal();

        modelResourcePath = EditorGUILayout.TextField("Resources Path", modelResourcePath);
        customModelPath = EditorGUILayout.TextField("Runtime OBJ Path", customModelPath);
        modelScale = EditorGUILayout.Vector3Field("Model Scale", modelScale);
        modelTint = EditorGUILayout.ColorField("Model Tint", modelTint);
    }

    void DrawSpellRuleFields()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Spell / Rule Effect", EditorStyles.miniBoldLabel);
        effectType = (CardEffectType)EditorGUILayout.EnumPopup("Effect Type", effectType);
        effectTarget = (CardEffectTarget)EditorGUILayout.EnumPopup("Target", effectTarget);
        effectAmount = Mathf.Max(0, EditorGUILayout.IntField("Effect Amount", effectAmount));
        effectDurationTurns = Mathf.Max(0, EditorGUILayout.IntField("Duration (Turns)", effectDurationTurns));
        effectManaCost = Mathf.Max(0, EditorGUILayout.IntField("Mana Cost", effectManaCost));
    }

    void DrawAudioFields()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Sound Effects", EditorStyles.miniBoldLabel);

        summonSfxClip = (AudioClip)EditorGUILayout.ObjectField("Summon Sound", summonSfxClip, typeof(AudioClip), false);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use Summon Clip"))
            PrepareAudioClip(summonSfxClip, "summon", ref summonSfxPath);
        if (GUILayout.Button("Import Summon Audio"))
            ImportExternalAudio("summon", ref summonSfxPath);
        EditorGUILayout.EndHorizontal();
        summonSfxPath = EditorGUILayout.TextField("Summon Path", summonSfxPath);

        if (cardType == DeckCardType.Spell)
        {
            effectSfxClip = (AudioClip)EditorGUILayout.ObjectField("Effect Sound", effectSfxClip, typeof(AudioClip), false);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Effect Clip"))
                PrepareAudioClip(effectSfxClip, "effect", ref effectSfxPath);
            if (GUILayout.Button("Import Effect Audio"))
                ImportExternalAudio("effect", ref effectSfxPath);
            EditorGUILayout.EndHorizontal();
            effectSfxPath = EditorGUILayout.TextField("Effect Path", effectSfxPath);
        }
    }

    void DrawValidation()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        string currentCardIssue = ValidateCurrentCard();
        EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(currentCardIssue) ? "Current card looks ready to add." : currentCardIssue,
            string.IsNullOrWhiteSpace(currentCardIssue) ? MessageType.Info : MessageType.Warning);

        string deckIssue = ValidateWorkingDeck();
        EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(deckIssue) ? "Working deck has no blocking issues." : deckIssue,
            string.IsNullOrWhiteSpace(deckIssue) ? MessageType.Info : MessageType.Warning);
        EditorGUILayout.EndVertical();
    }

    void DrawWorkingDeck()
    {
        int count = workingCards != null && workingCards.isArray ? workingCards.arraySize : 0;
        showWorkingDeck = EditorGUILayout.Foldout(showWorkingDeck, $"Working Deck ({count})", true);
        if (!showWorkingDeck)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(workingCards, true);
        EditorGUILayout.EndVertical();
    }

    void DrawOutputActions()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Creator Outputs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Generate files that make the deck easier to test, share, and present: a deck JSON, a printable card sheet, and a readable deck report.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Export Deck JSON", GUILayout.Height(30)))
            ExportWorkingDeckJson();

        if (GUILayout.Button("Generate Print Sheet", GUILayout.Height(30)))
            GeneratePrintableCardSheet();

        if (GUILayout.Button("Generate Deck Report", GUILayout.Height(30)))
            GenerateDeckReport();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Reveal Generated Files Folder", GUILayout.Height(26)))
            RevealGeneratedFolder();

        EditorGUILayout.EndVertical();
    }

    void DrawPreviewActions()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("AR Preview", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open AR Preview Scene", GUILayout.Height(30)))
            OpenPreviewScene();

        if (GUILayout.Button("Set Runtime Loader Deck", GUILayout.Height(30)))
            SetRuntimeLoaderDeck();
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Runtime Targets", GUILayout.Height(28)))
                RunRuntimeLoaderAction(loader => loader.LoadRuntimeImageTargets());

            if (GUILayout.Button("Clear Runtime Targets", GUILayout.Height(28)))
                RunRuntimeLoaderAction(loader => loader.ClearRuntimeTargets());
            EditorGUILayout.EndHorizontal();
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Runtime target loading needs Play Mode because Vuforia must be initialized.", MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    void AddCardToWorkingDeck()
    {
        string issue = ValidateCurrentCard();
        if (!string.IsNullOrWhiteSpace(issue))
        {
            statusMessage = issue;
            return;
        }

        if (!RuntimeDeckAssetStore.TrySaveTextureAsPng(cardImage, cardName, out string storedImagePath, out string imageError))
        {
            statusMessage = imageError;
            return;
        }

        managerObject.Update();
        int index = workingCards.arraySize;
        workingCards.InsertArrayElementAtIndex(index);
        SerializedProperty card = workingCards.GetArrayElementAtIndex(index);

        SetString(card, "cardName", cardName.Trim());
        SetInt(card, "quantity", quantity);
        SetString(card, "cardType", cardType.ToString());
        SetInt(card, "health", cardType == DeckCardType.Creature ? health : 0);
        SetInt(card, "damage", cardType == DeckCardType.Creature ? damage : 0);
        SetInt(card, "mana", cardType == DeckCardType.Creature ? mana : 0);
        SetString(card, "imagePath", storedImagePath);
        SetFloat(card, "targetWidthMeters", targetWidthMeters);
        SetString(card, "modelResourcePath", modelResourcePath.Trim());
        SetString(card, "customModelPath", customModelPath.Trim());
        SetVector3(card, "modelScale", SafeScale(modelScale));
        SetColor(card, "modelTint", SafeTint(modelTint));
        SetString(card, "summonSfxPath", summonSfxPath.Trim());
        SetEnum(card, "effectType", cardType == DeckCardType.Spell ? effectType : CardEffectType.None);
        SetEnum(card, "effectTarget", cardType == DeckCardType.Spell ? effectTarget : CardEffectTarget.None);
        SetInt(card, "effectAmount", cardType == DeckCardType.Spell ? Mathf.Max(0, effectAmount) : 0);
        SetInt(card, "effectDurationTurns", cardType == DeckCardType.Spell ? Mathf.Max(0, effectDurationTurns) : 0);
        SetInt(card, "effectManaCost", cardType == DeckCardType.Spell ? Mathf.Max(0, effectManaCost) : 0);
        SetString(card, "effectSfxPath", cardType == DeckCardType.Spell ? effectSfxPath.Trim() : string.Empty);

        managerObject.ApplyModifiedProperties();
        MarkManagerDirty();

        statusMessage = $"Added {quantity}x {cardName.Trim()} to the working deck.";
        ResetCardForm();
    }

    void SaveDeck()
    {
        managerObject.ApplyModifiedProperties();
        if (manager.TrySaveWorkingDeck())
        {
            SetRuntimeLoaderDeck();
            statusMessage = "Deck saved.";
        }
        else
        {
            statusMessage = "Deck could not be saved. Check the Console for details.";
        }

        MarkManagerDirty();
    }

    void LoadSelectedDeckForEditing()
    {
        managerObject.ApplyModifiedProperties();
        if (manager.LoadDeckIntoEditor(selectedSavedDeckId.stringValue))
        {
            managerObject.Update();
            statusMessage = "Loaded saved deck into the working editor.";
            MarkManagerDirty();
            return;
        }

        statusMessage = "Saved deck could not be loaded. Check the Console for details.";
    }

    void StartNewDeck()
    {
        managerObject.ApplyModifiedProperties();
        manager.StartNewDeck();
        managerObject.Update();
        statusMessage = "Started a new empty deck.";
        MarkManagerDirty();
    }

    void ExportWorkingDeckJson()
    {
        DeckData deck = BuildWorkingDeckData();
        if (deck.cards.Count == 0)
        {
            statusMessage = "Add at least one card before exporting.";
            return;
        }

        string folder = EnsureGeneratedSubfolder("Deck Exports");
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{SanitizeFileName(deck.deckName)}.json");
        File.WriteAllText(ToAbsoluteProjectPath(path), JsonUtility.ToJson(deck, true));
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(ToAbsoluteProjectPath(path));
        statusMessage = $"Exported deck JSON: {path}";
    }

    void GeneratePrintableCardSheet()
    {
        DeckData deck = BuildWorkingDeckData();
        if (deck.cards.Count == 0)
        {
            statusMessage = "Add at least one card before generating a print sheet.";
            return;
        }

        string folder = EnsureGeneratedSubfolder("Print Sheets");
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{SanitizeFileName(deck.deckName)}_print_sheet.html");
        string absolutePath = ToAbsoluteProjectPath(path);
        File.WriteAllText(absolutePath, BuildPrintSheetHtml(deck));
        AssetDatabase.Refresh();
        Application.OpenURL(new Uri(absolutePath).AbsoluteUri);
        statusMessage = $"Generated printable card sheet: {path}";
    }

    void GenerateDeckReport()
    {
        DeckData deck = BuildWorkingDeckData();
        if (deck.cards.Count == 0)
        {
            statusMessage = "Add at least one card before generating a report.";
            return;
        }

        string folder = EnsureGeneratedSubfolder("Deck Reports");
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{SanitizeFileName(deck.deckName)}_report.md");
        File.WriteAllText(ToAbsoluteProjectPath(path), BuildDeckReport(deck));
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(ToAbsoluteProjectPath(path));
        statusMessage = $"Generated deck report: {path}";
    }

    void RevealGeneratedFolder()
    {
        EnsureAssetFolder(GeneratedOutputFolder);
        EditorUtility.RevealInFinder(ToAbsoluteProjectPath(GeneratedOutputFolder));
    }

    void ClearWorkingDeck()
    {
        managerObject.ApplyModifiedProperties();
        manager.ClearWorkingDeck();
        managerObject.Update();
        MarkManagerDirty();
        statusMessage = "Working deck cleared.";
    }

    void ResetCardForm()
    {
        cardName = string.Empty;
        quantity = 1;
        cardType = DeckCardType.Creature;
        cardImage = null;
        targetWidthMeters = 0.06f;
        health = 5;
        damage = 2;
        mana = 1;
        modelAsset = null;
        modelResourcePath = string.Empty;
        customModelPath = string.Empty;
        modelScale = Vector3.one;
        modelTint = Color.white;
        summonSfxClip = null;
        effectSfxClip = null;
        summonSfxPath = string.Empty;
        effectSfxPath = string.Empty;
        effectType = CardEffectType.Attack;
        effectTarget = CardEffectTarget.EnemyCreature;
        effectAmount = 1;
        effectDurationTurns = 0;
        effectManaCost = 0;
    }

    void UseSelectedModelAsset()
    {
        if (modelAsset == null)
        {
            statusMessage = "Select a model or prefab first.";
            return;
        }

        string resourcePath = TryGetResourcesPath(AssetDatabase.GetAssetPath(modelAsset));
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            statusMessage = "That model is not inside a Resources folder. Use 'Copy Selected To Resources' first.";
            return;
        }

        modelResourcePath = resourcePath;
        customModelPath = string.Empty;
        statusMessage = $"Using model Resources path: {modelResourcePath}";
    }

    void CopySelectedModelToResources()
    {
        if (modelAsset == null)
        {
            statusMessage = "Select a model or prefab first.";
            return;
        }

        string sourceAssetPath = AssetDatabase.GetAssetPath(modelAsset);
        if (string.IsNullOrWhiteSpace(sourceAssetPath))
        {
            statusMessage = "Selected model has no asset path.";
            return;
        }

        string copiedAssetPath = CopyProjectAssetToImportedModels(sourceAssetPath);
        if (string.IsNullOrWhiteSpace(copiedAssetPath))
            return;

        modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(copiedAssetPath);
        modelResourcePath = TryGetResourcesPath(copiedAssetPath);
        customModelPath = string.Empty;
        statusMessage = $"Copied model to {copiedAssetPath}.";
    }

    void ImportExternalModel()
    {
        string sourcePath = EditorUtility.OpenFilePanel("Import model", string.Empty, "fbx,obj");
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        EnsureAssetFolder(ImportedModelFolder);

        string safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(sourcePath));
        string extension = Path.GetExtension(sourcePath);
        string destinationAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{ImportedModelFolder}/{safeName}{extension}");
        string absoluteDestination = ToAbsoluteProjectPath(destinationAssetPath);

        Directory.CreateDirectory(Path.GetDirectoryName(absoluteDestination));
        File.Copy(sourcePath, absoluteDestination, false);
        AssetDatabase.ImportAsset(destinationAssetPath);
        AssetDatabase.Refresh();

        modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(destinationAssetPath);
        modelResourcePath = TryGetResourcesPath(destinationAssetPath);
        customModelPath = string.Empty;
        statusMessage = modelAsset == null
            ? $"Imported file, but Unity did not load it as a model: {destinationAssetPath}"
            : $"Imported model: {destinationAssetPath}";
    }

    void PrepareAudioClip(AudioClip clip, string slot, ref string targetPath)
    {
        if (clip == null)
        {
            statusMessage = $"Select a {slot} audio clip first.";
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(clip);
        string resourcePath = TryGetResourcesPath(assetPath);
        if (!string.IsNullOrWhiteSpace(resourcePath))
        {
            targetPath = resourcePath;
            statusMessage = $"Using {slot} Resources path: {resourcePath}";
            return;
        }

        string sourcePath = ToAbsoluteProjectPath(assetPath);
        if (RuntimeDeckAssetStore.TryCopyAudioFile(sourcePath, SafeCardNameForAssets(), slot, out string storedPath, out string error))
        {
            targetPath = storedPath;
            statusMessage = $"Copied {slot} audio for runtime use.";
            return;
        }

        statusMessage = error;
    }

    void ImportExternalAudio(string slot, ref string targetPath)
    {
        string sourcePath = EditorUtility.OpenFilePanel($"Import {slot} audio", string.Empty, "wav,mp3,ogg,aif,aiff");
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        if (RuntimeDeckAssetStore.TryCopyAudioFile(sourcePath, SafeCardNameForAssets(), slot, out string storedPath, out string error))
        {
            targetPath = storedPath;
            statusMessage = $"Imported {slot} audio.";
            return;
        }

        statusMessage = error;
    }

    void OpenPreviewScene()
    {
        if (!File.Exists(ToAbsoluteProjectPath(PreviewScenePath)))
        {
            statusMessage = $"Preview scene not found: {PreviewScenePath}";
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(PreviewScenePath);
    }

    void SetRuntimeLoaderDeck()
    {
        DeckRuntimeImageTargetLoader loader = GetRuntimeLoader();
        if (loader == null)
        {
            statusMessage = "No DeckRuntimeImageTargetLoader is assigned or present in the scene.";
            return;
        }

        string id = manager.CurrentDeckId;
        loader.SetDeckIdToLoad(id);
        MarkLoaderDirty(loader);
        statusMessage = $"Runtime loader set to deck id '{id}'.";
    }

    void RunRuntimeLoaderAction(Action<DeckRuntimeImageTargetLoader> action)
    {
        DeckRuntimeImageTargetLoader loader = GetRuntimeLoader();
        if (loader == null)
        {
            statusMessage = "No DeckRuntimeImageTargetLoader is assigned or present in the scene.";
            return;
        }

        action?.Invoke(loader);
        MarkLoaderDirty(loader);
    }

    string ValidateCurrentCard()
    {
        if (string.IsNullOrWhiteSpace(cardName))
            return "Card name is required.";

        if (quantity < 1)
            return "Quantity must be at least 1.";

        if (cardImage == null)
            return "Card image is required so Vuforia has something to scan.";

        if (cardType == DeckCardType.Creature)
        {
            if (health <= 0 || damage <= 0)
                return "Creature cards need health and damage above 0.";

            if (string.IsNullOrWhiteSpace(modelResourcePath) && string.IsNullOrWhiteSpace(customModelPath))
                return "Creature cards need a model Resources path or runtime OBJ path.";
        }

        if (cardType == DeckCardType.Spell)
        {
            if (effectType == CardEffectType.None)
                return "Spell cards need an effect type.";

            if (effectTarget == CardEffectTarget.None)
                return "Spell cards need an effect target.";

            if (EffectUsuallyNeedsAmount(effectType) && effectAmount <= 0)
                return "This spell effect needs an amount above 0.";
        }

        return string.Empty;
    }

    string ValidateWorkingDeck()
    {
        if (workingCards == null || !workingCards.isArray || workingCards.arraySize == 0)
            return "Working deck is empty.";

        int missingImages = 0;
        int missingCreatureModels = 0;
        int badCreatureStats = 0;
        int missingSpellRules = 0;

        for (int i = 0; i < workingCards.arraySize; i++)
        {
            SerializedProperty card = workingCards.GetArrayElementAtIndex(i);
            string type = GetString(card, "cardType");

            if (string.IsNullOrWhiteSpace(GetString(card, "imagePath")))
                missingImages++;

            if (string.Equals(type, DeckCardType.Creature.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (GetInt(card, "health") <= 0 || GetInt(card, "damage") <= 0)
                    badCreatureStats++;

                if (string.IsNullOrWhiteSpace(GetString(card, "modelResourcePath")) &&
                    string.IsNullOrWhiteSpace(GetString(card, "customModelPath")))
                {
                    missingCreatureModels++;
                }
            }

            if (string.Equals(type, DeckCardType.Spell.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var ruleType = (CardEffectType)card.FindPropertyRelative("effectType").enumValueIndex;
                var target = (CardEffectTarget)card.FindPropertyRelative("effectTarget").enumValueIndex;
                int amount = GetInt(card, "effectAmount");
                if (ruleType == CardEffectType.None ||
                    target == CardEffectTarget.None ||
                    (EffectUsuallyNeedsAmount(ruleType) && amount <= 0))
                {
                    missingSpellRules++;
                }
            }
        }

        string message = string.Empty;
        if (missingImages > 0)
            message += $"{missingImages} card(s) are missing image targets.\n";
        if (missingCreatureModels > 0)
            message += $"{missingCreatureModels} creature card(s) are missing models.\n";
        if (badCreatureStats > 0)
            message += $"{badCreatureStats} creature card(s) need health and damage above 0.\n";
        if (missingSpellRules > 0)
            message += $"{missingSpellRules} spell card(s) need a complete rule effect.\n";

        return message.Trim();
    }

    DeckData BuildWorkingDeckData()
    {
        var deck = new DeckData
        {
            deckId = manager != null ? manager.CurrentDeckId : BuildDeckId(deckName.stringValue),
            deckName = string.IsNullOrWhiteSpace(deckName.stringValue) ? "Untitled Deck" : deckName.stringValue.Trim(),
            cards = new List<DeckCardEntry>()
        };

        if (workingCards == null || !workingCards.isArray)
            return deck;

        for (int i = 0; i < workingCards.arraySize; i++)
            deck.cards.Add(ReadCardEntry(workingCards.GetArrayElementAtIndex(i)));

        return deck;
    }

    DeckCardEntry ReadCardEntry(SerializedProperty card)
    {
        return new DeckCardEntry
        {
            cardName = GetString(card, "cardName"),
            quantity = GetInt(card, "quantity"),
            cardType = GetString(card, "cardType"),
            health = GetInt(card, "health"),
            damage = GetInt(card, "damage"),
            mana = GetInt(card, "mana"),
            imagePath = GetString(card, "imagePath"),
            targetWidthMeters = card.FindPropertyRelative("targetWidthMeters").floatValue,
            modelResourcePath = GetString(card, "modelResourcePath"),
            customModelPath = GetString(card, "customModelPath"),
            modelScale = card.FindPropertyRelative("modelScale").vector3Value,
            modelTint = card.FindPropertyRelative("modelTint").colorValue,
            summonSfxPath = GetString(card, "summonSfxPath"),
            effectType = (CardEffectType)card.FindPropertyRelative("effectType").enumValueIndex,
            effectTarget = (CardEffectTarget)card.FindPropertyRelative("effectTarget").enumValueIndex,
            effectAmount = GetInt(card, "effectAmount"),
            effectDurationTurns = GetInt(card, "effectDurationTurns"),
            effectManaCost = GetInt(card, "effectManaCost"),
            effectSfxPath = GetString(card, "effectSfxPath")
        };
    }

    string BuildDeckReport(DeckData deck)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {deck.deckName}");
        builder.AppendLine();
        builder.AppendLine($"- Deck ID: `{deck.deckId}`");
        builder.AppendLine($"- Cards: `{deck.cards.Count}`");
        builder.AppendLine($"- Generated: `{DateTime.Now:yyyy-MM-dd HH:mm}`");
        builder.AppendLine();
        builder.AppendLine("## Validation");
        string validation = ValidateWorkingDeck();
        builder.AppendLine(string.IsNullOrWhiteSpace(validation) ? "No blocking issues found." : validation);
        builder.AppendLine();
        builder.AppendLine("## Card List");
        builder.AppendLine();

        for (int i = 0; i < deck.cards.Count; i++)
        {
            DeckCardEntry card = deck.cards[i];
            builder.AppendLine($"### {i + 1}. {card.cardName}");
            builder.AppendLine($"- Quantity: `{card.quantity}`");
            builder.AppendLine($"- Type: `{card.cardType}`");
            if (string.Equals(card.cardType, DeckCardType.Creature.ToString(), StringComparison.OrdinalIgnoreCase))
                builder.AppendLine($"- Stats: `{card.health} HP / {card.damage} DMG / {card.mana} Mana`");
            if (string.Equals(card.cardType, DeckCardType.Spell.ToString(), StringComparison.OrdinalIgnoreCase))
                builder.AppendLine($"- Effect: `{DescribeEffect(card)}`");
            builder.AppendLine($"- Image Target: `{card.imagePath}`");
            builder.AppendLine($"- Model: `{FirstNonEmpty(card.modelResourcePath, card.customModelPath, "None")}`");
            builder.AppendLine($"- Summon Sound: `{FirstNonEmpty(card.summonSfxPath, "None")}`");
            builder.AppendLine($"- Effect Sound: `{FirstNonEmpty(card.effectSfxPath, "None")}`");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    string BuildPrintSheetHtml(DeckData deck)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html><head><meta charset=\"utf-8\">");
        builder.AppendLine($"<title>{EscapeHtml(deck.deckName)} Print Sheet</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("body{font-family:Arial,sans-serif;margin:18px;color:#111}h1{font-size:22px;margin:0 0 14px}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}.card{break-inside:avoid;border:1px solid #bbb;padding:8px;text-align:center}.card img{width:100%;aspect-ratio:2.5/3.5;object-fit:contain}.name{font-weight:bold;margin-top:6px}.meta{font-size:12px;color:#555}@media print{body{margin:10mm}.card{page-break-inside:avoid}}");
        builder.AppendLine("</style></head><body>");
        builder.AppendLine($"<h1>{EscapeHtml(deck.deckName)} - Printable AR Cards</h1>");
        builder.AppendLine("<div class=\"grid\">");

        foreach (DeckCardEntry card in deck.cards)
        {
            int copies = Mathf.Max(1, card.quantity);
            for (int i = 0; i < copies; i++)
            {
                builder.AppendLine("<div class=\"card\">");
                if (!string.IsNullOrWhiteSpace(card.imagePath) && File.Exists(card.imagePath))
                    builder.AppendLine($"<img src=\"{EscapeHtml(new Uri(card.imagePath).AbsoluteUri)}\" alt=\"{EscapeHtml(card.cardName)}\">");
                else
                    builder.AppendLine("<div style=\"height:260px;display:flex;align-items:center;justify-content:center;background:#eee\">Missing image</div>");

                builder.AppendLine($"<div class=\"name\">{EscapeHtml(card.cardName)}</div>");
                builder.AppendLine($"<div class=\"meta\">{EscapeHtml(card.cardType)}");
                if (string.Equals(card.cardType, DeckCardType.Creature.ToString(), StringComparison.OrdinalIgnoreCase))
                    builder.AppendLine($" | HP {card.health} | DMG {card.damage} | Mana {card.mana}");
                if (string.Equals(card.cardType, DeckCardType.Spell.ToString(), StringComparison.OrdinalIgnoreCase))
                    builder.AppendLine($" | {EscapeHtml(DescribeEffect(card))}");
                builder.AppendLine("</div></div>");
            }
        }

        builder.AppendLine("</div></body></html>");
        return builder.ToString();
    }

    void RefreshSceneReferences()
    {
        manager = FindFirstObjectByType<DeckBuilderManager>(FindObjectsInactive.Include);
        RefreshSerializedObject();
    }

    void RefreshSerializedObject()
    {
        if (manager == null)
        {
            managerObject = null;
            return;
        }

        if (manager.ResolveSceneReferences())
            EditorUtility.SetDirty(manager);

        managerObject = new SerializedObject(manager);
        deckDatabase = managerObject.FindProperty("deckDatabase");
        runtimeTargetLoader = managerObject.FindProperty("runtimeTargetLoader");
        reloadRuntimeTargetsAfterSave = managerObject.FindProperty("reloadRuntimeTargetsAfterSave");
        deckName = managerObject.FindProperty("deckName");
        editingDeckId = managerObject.FindProperty("editingDeckId");
        selectedSavedDeckId = managerObject.FindProperty("selectedSavedDeckId");
        workingCards = managerObject.FindProperty("workingCards");
    }

    void CreateManagerInScene()
    {
        var managerObjectInScene = new GameObject("AR Deck Builder Manager");
        Undo.RegisterCreatedObjectUndo(managerObjectInScene, "Create AR Deck Builder Manager");
        manager = managerObjectInScene.AddComponent<DeckBuilderManager>();
        RefreshSerializedObject();
        AutoAssignSceneReferences();
        Selection.activeObject = managerObjectInScene;
    }

    void AutoAssignSceneReferences()
    {
        if (managerObject == null)
            return;

        managerObject.ApplyModifiedProperties();
        if (manager != null && manager.ResolveSceneReferences())
        {
            RefreshSerializedObject();
            MarkManagerDirty();
            statusMessage = "Scene references auto-filled.";
            return;
        }

        statusMessage = "No missing scene references were found.";
    }

    DeckRuntimeImageTargetLoader GetRuntimeLoader()
    {
        if (runtimeTargetLoader != null && runtimeTargetLoader.objectReferenceValue is DeckRuntimeImageTargetLoader assignedLoader)
            return assignedLoader;

        return FindFirstObjectByType<DeckRuntimeImageTargetLoader>(FindObjectsInactive.Include);
    }

    string CopyProjectAssetToImportedModels(string sourceAssetPath)
    {
        EnsureAssetFolder(ImportedModelFolder);

        string extension = Path.GetExtension(sourceAssetPath);
        string safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(sourceAssetPath));
        string destinationAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{ImportedModelFolder}/{safeName}{extension}");

        if (!AssetDatabase.CopyAsset(sourceAssetPath, destinationAssetPath))
        {
            statusMessage = $"Could not copy model asset: {sourceAssetPath}";
            return string.Empty;
        }

        AssetDatabase.ImportAsset(destinationAssetPath);
        AssetDatabase.Refresh();
        return destinationAssetPath;
    }

    static void EnsureAssetFolder(string folderPath)
    {
        string normalized = folderPath.Replace("\\", "/");
        string[] parts = normalized.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    static string EnsureGeneratedSubfolder(string subfolder)
    {
        string path = $"{GeneratedOutputFolder}/{subfolder}";
        EnsureAssetFolder(path);
        return path;
    }

    static string TryGetResourcesPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return string.Empty;

        string normalized = assetPath.Replace("\\", "/");
        const string rootMarker = "Assets/Resources/";
        const string nestedMarker = "/Resources/";

        int startIndex;
        if (normalized.StartsWith(rootMarker, StringComparison.OrdinalIgnoreCase))
            startIndex = rootMarker.Length;
        else
        {
            int markerIndex = normalized.IndexOf(nestedMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                return string.Empty;

            startIndex = markerIndex + nestedMarker.Length;
        }

        string resourcePath = normalized.Substring(startIndex);
        return Path.ChangeExtension(resourcePath, null).Replace("\\", "/");
    }

    static string BuildDeckId(string name)
    {
        string safeName = string.IsNullOrWhiteSpace(name) ? "deck" : name.Trim();
        return safeName.ToLowerInvariant().Replace(" ", "-");
    }

    static string ToAbsoluteProjectPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return string.IsNullOrWhiteSpace(projectRoot) ? assetPath : Path.GetFullPath(Path.Combine(projectRoot, assetPath));
    }

    static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "asset";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        string safe = value.Trim();
        for (int i = 0; i < invalidChars.Length; i++)
            safe = safe.Replace(invalidChars[i].ToString(), string.Empty);

        return string.IsNullOrWhiteSpace(safe) ? "asset" : safe.Replace(" ", "_");
    }

    string SafeCardNameForAssets()
    {
        return string.IsNullOrWhiteSpace(cardName) ? "card" : cardName.Trim();
    }

    static string FirstNonEmpty(params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return string.Empty;
    }

    static bool EffectUsuallyNeedsAmount(CardEffectType ruleType)
    {
        return ruleType == CardEffectType.Attack ||
            ruleType == CardEffectType.BuffDamage ||
            ruleType == CardEffectType.BuffHealth ||
            ruleType == CardEffectType.Heal ||
            ruleType == CardEffectType.ManaGain ||
            ruleType == CardEffectType.DrawCard ||
            ruleType == CardEffectType.Shield;
    }

    static string DescribeEffect(DeckCardEntry card)
    {
        if (card == null || card.effectType == CardEffectType.None)
            return "No effect";

        string amount = EffectUsuallyNeedsAmount(card.effectType) ? $" {card.effectAmount}" : string.Empty;
        string duration = card.effectDurationTurns > 0 ? $" for {card.effectDurationTurns} turn(s)" : string.Empty;
        string cost = card.effectManaCost > 0 ? $" costing {card.effectManaCost} mana" : string.Empty;
        return $"{card.effectType}{amount} on {card.effectTarget}{duration}{cost}";
    }

    static string EscapeHtml(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    static Vector3 SafeScale(Vector3 value)
    {
        return new Vector3(
            Mathf.Max(0.01f, value.x),
            Mathf.Max(0.01f, value.y),
            Mathf.Max(0.01f, value.z));
    }

    static Color SafeTint(Color value)
    {
        return new Color(value.r, value.g, value.b, Mathf.Max(0.01f, value.a));
    }

    void MarkManagerDirty()
    {
        if (manager == null)
            return;

        EditorUtility.SetDirty(manager);
        if (!Application.isPlaying && manager.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }

    void MarkLoaderDirty(DeckRuntimeImageTargetLoader loader)
    {
        if (loader == null)
            return;

        EditorUtility.SetDirty(loader);
        if (!Application.isPlaying && loader.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
    }

    static void SetString(SerializedProperty root, string propertyName, string value)
    {
        root.FindPropertyRelative(propertyName).stringValue = value;
    }

    static string GetString(SerializedProperty root, string propertyName)
    {
        return root.FindPropertyRelative(propertyName).stringValue;
    }

    static void SetInt(SerializedProperty root, string propertyName, int value)
    {
        root.FindPropertyRelative(propertyName).intValue = value;
    }

    static int GetInt(SerializedProperty root, string propertyName)
    {
        return root.FindPropertyRelative(propertyName).intValue;
    }

    static void SetFloat(SerializedProperty root, string propertyName, float value)
    {
        root.FindPropertyRelative(propertyName).floatValue = value;
    }

    static void SetVector3(SerializedProperty root, string propertyName, Vector3 value)
    {
        root.FindPropertyRelative(propertyName).vector3Value = value;
    }

    static void SetColor(SerializedProperty root, string propertyName, Color value)
    {
        root.FindPropertyRelative(propertyName).colorValue = value;
    }

    static void SetEnum<T>(SerializedProperty root, string propertyName, T value) where T : Enum
    {
        root.FindPropertyRelative(propertyName).enumValueIndex = Convert.ToInt32(value);
    }
}
#endif
