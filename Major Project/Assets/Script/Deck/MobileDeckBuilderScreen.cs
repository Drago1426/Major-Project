using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

public class MobileDeckBuilderScreen : MonoBehaviour
{
    [SerializeField] MobileDeckBuilderController controller;
    [SerializeField] bool buildOnStart = true;

    Canvas canvas;
    GameObject fullPanel;
    GameObject collapsedButton;
    GameObject menuPanel;
    GameObject deckPickerPanel;
    GameObject settingsPanel;
    Text statusText;
    Font uiFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreateForRuntimeDeckScene()
    {
        if (FindFirstObjectByType<MobileDeckBuilderScreen>() != null)
            return;

        if (FindFirstObjectByType<DeckRuntimeImageTargetLoader>() == null)
            return;

        var screenObject = new GameObject("Mobile Deck Builder Screen");
        screenObject.AddComponent<MobileDeckBuilderScreen>();
    }

    void Start()
    {
        if (buildOnStart)
            Build();
    }

    public void Build()
    {
        if (canvas != null)
            return;

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ResolveController();
        EnsureEventSystem();

        canvas = CreateCanvas();
        fullPanel = CreateFullPanel(canvas.transform);

        RectTransform content = CreateScrollContent(fullPanel.transform);
        BuildDeckSection(content);
        BuildCardSection(content);
        BuildImageSection(content);
        BuildModelSection(content);
        BuildAudioSection(content);
        BuildActions(content);
        fullPanel.SetActive(false);

        deckPickerPanel = CreateFullPanel(canvas.transform);
        deckPickerPanel.name = "Deck Picker Panel";
        deckPickerPanel.SetActive(false);

        settingsPanel = CreateFullPanel(canvas.transform);
        settingsPanel.name = "Settings Panel";
        BuildSettingsPanel(settingsPanel.transform);
        settingsPanel.SetActive(false);

        menuPanel = CreateMenuPanel(canvas.transform);
        menuPanel.SetActive(false);

        collapsedButton = CreateCollapsedButton(canvas.transform);
        collapsedButton.SetActive(true);
        collapsedButton.transform.SetAsLastSibling();

        UpdateStatus();
    }

    void BuildDeckSection(Transform parent)
    {
        AddTitle(parent, "AR Card Deck Builder");
        AddInput(parent, "Deck Name", "My Mobile Deck", controller.SetDeckName);
        AddDropdown(parent, "Game", new List<string> { "MTG", "Pokemon TCG" }, controller.SetGameType);
    }

    void BuildCardSection(Transform parent)
    {
        AddSection(parent, "Card");
        AddInput(parent, "Card Name", "Cinder", controller.SetCardName);
        AddInput(parent, "Quantity", "1", controller.SetQuantityFromText, InputField.ContentType.IntegerNumber);

        AddDropdown(parent, "MTG Type", EnumNames<MtgCardType>(), controller.SetMtgCardType);
        AddDropdown(parent, "Pokemon Type", EnumNames<PokemonCardType>(), controller.SetPokemonCardType);

        AddInput(parent, "Health", "5", controller.SetHealthFromText, InputField.ContentType.IntegerNumber);
        AddInput(parent, "Damage", "5", controller.SetDamageFromText, InputField.ContentType.IntegerNumber);
        AddInput(parent, "Mana", "2", controller.SetManaFromText, InputField.ContentType.IntegerNumber);
        AddInput(parent, "Target Width (m)", "0.06", controller.SetTargetWidthFromText, InputField.ContentType.DecimalNumber);
    }

    void BuildImageSection(Transform parent)
    {
        AddSection(parent, "Card Image Target");
        AddButtonRow(parent,
            ("Gallery", () => Run(controller.PickImageFromGallery)),
            ("Camera", () => Run(controller.TakePhotoForImageTarget)));

        InputField imagePath = AddInput(parent, "Image File Path", "", null);
        AddButton(parent, "Use Image Path", () => Run(() => controller.UseImageFilePath(imagePath.text)));

        InputField imageUrl = AddInput(parent, "Image URL", "https://...", null, InputField.ContentType.Standard);
        AddButton(parent, "Download Image", () => Run(() => controller.DownloadImageFromUrl(imageUrl.text)));
    }

    void BuildModelSection(Transform parent)
    {
        AddSection(parent, "3D Model");
        AddDropdown(parent, "Built-in Model", ModelOptionNames(), index => Run(() => controller.SelectBuiltInModel(index)));
        if (controller.BuiltInModels.Count > 0)
            controller.SelectBuiltInModel(0);

        AddInput(parent, "Uniform Scale", "1", controller.SetUniformModelScaleFromText, InputField.ContentType.DecimalNumber);
        AddInput(parent, "Tint Hex", "#FFFFFF", controller.SetModelTintFromHtml);

        InputField modelPath = AddInput(parent, "Custom OBJ Path", "", null);
        AddButton(parent, "Use OBJ Path", () => Run(() => controller.UseCustomModelFilePath(modelPath.text)));
        AddButton(parent, "Pick OBJ File", () => Run(controller.PickCustomModelFile));

        InputField modelUrl = AddInput(parent, "OBJ URL", "https://...", null);
        AddButton(parent, "Download OBJ", () => Run(() => controller.DownloadModelFromUrl(modelUrl.text)));
    }

    void BuildAudioSection(Transform parent)
    {
        AddSection(parent, "Sound Effects");
        AddDropdown(parent, "Summon Sound", SoundOptionNames(controller.BuiltInSummonSounds), index => Run(() => controller.SelectBuiltInSummonSound(index)));
        if (controller.BuiltInSummonSounds.Count > 0)
            controller.SelectBuiltInSummonSound(0);

        InputField summonPath = AddInput(parent, "Summon Audio Path", "", null);
        AddButtonRow(parent,
            ("Pick Summon Audio", () => Run(controller.PickSummonAudioFile)),
            ("Use Summon Path", () => Run(() => controller.UseSummonAudioFilePath(summonPath.text))));

        InputField summonUrl = AddInput(parent, "Summon Audio URL", "https://...", null);
        AddButton(parent, "Download Summon Audio", () => Run(() => controller.DownloadSummonAudioFromUrl(summonUrl.text)));

        AddDropdown(parent, "Fireball Sound", SoundOptionNames(controller.BuiltInFireballSounds), index => Run(() => controller.SelectBuiltInFireballSound(index)));
        if (controller.BuiltInFireballSounds.Count > 0)
            controller.SelectBuiltInFireballSound(0);

        InputField fireballPath = AddInput(parent, "Fireball Audio Path", "", null);
        AddButtonRow(parent,
            ("Pick Fireball Audio", () => Run(controller.PickFireballAudioFile)),
            ("Use Fireball Path", () => Run(() => controller.UseFireballAudioFilePath(fireballPath.text))));

        InputField fireballUrl = AddInput(parent, "Fireball Audio URL", "https://...", null);
        AddButton(parent, "Download Fireball Audio", () => Run(() => controller.DownloadFireballAudioFromUrl(fireballUrl.text)));
    }

    void BuildActions(Transform parent)
    {
        AddSection(parent, "Deck Actions");
        AddButtonRow(parent,
            ("Add Card", () => Run(() => controller.TryAddCurrentCard())),
            ("Save Deck", () => Run(() => controller.TrySaveWorkingDeck())));
        AddButtonRow(parent,
            ("Clear Deck", () => Run(controller.ClearWorkingDeck)),
            ("Back To Camera", HideBuilder));

        statusText = AddText(parent, "Ready.", 26, FontStyle.Normal, TextAnchor.MiddleLeft);
    }

    void BuildSettingsPanel(Transform parent)
    {
        RectTransform content = CreateScrollContent(parent);
        AddTitle(content, "Settings");
        AddText(content, "Runtime deck controls", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddButton(content, "Load Selected Deck", () => Run(controller.LoadSelectedRuntimeDeck));
        AddButton(content, "Reload Selected Deck", () => Run(controller.ReloadSelectedRuntimeDeck));
        AddButton(content, "Clear AR Targets", () => Run(controller.ClearRuntimeTargets));
        AddButton(content, "Back To Camera", HideAllPanels);
    }

    void HideBuilder()
    {
        HideAllPanels();
    }

    void ShowBuilder()
    {
        HideAllPanels();
        fullPanel.SetActive(true);
        fullPanel.transform.SetAsLastSibling();
        if (collapsedButton != null)
            collapsedButton.SetActive(false);
        UpdateStatus();
    }

    void ShowCreateDeck()
    {
        controller.ClearWorkingDeck();
        controller.ResetCurrentCardSelection();
        ShowBuilder();
    }

    void ShowMenu()
    {
        if (menuPanel == null)
            return;

        bool showMenu = !menuPanel.activeSelf;
        menuPanel.SetActive(showMenu);
        if (collapsedButton != null)
            collapsedButton.SetActive(!showMenu);

        if (showMenu)
            menuPanel.transform.SetAsLastSibling();
    }

    void HideMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (collapsedButton != null)
            collapsedButton.SetActive(true);
    }

    void HideAllPanels()
    {
        if (fullPanel != null)
            fullPanel.SetActive(false);

        if (deckPickerPanel != null)
            deckPickerPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        HideMenu();
        if (collapsedButton != null)
            collapsedButton.SetActive(true);
    }

    void ShowSettings()
    {
        HideAllPanels();
        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();
        if (collapsedButton != null)
            collapsedButton.SetActive(false);
    }

    void ShowDeckPicker(bool editMode)
    {
        HideAllPanels();
        RebuildDeckPicker(editMode);
        deckPickerPanel.SetActive(true);
        deckPickerPanel.transform.SetAsLastSibling();
        if (collapsedButton != null)
            collapsedButton.SetActive(false);
    }

    void RebuildDeckPicker(bool editMode)
    {
        ClearChildren(deckPickerPanel.transform);
        RectTransform content = CreateScrollContent(deckPickerPanel.transform);

        AddTitle(content, editMode ? "Edit Deck" : "Pick Deck");
        controller.RefreshSavedDecks();

        IReadOnlyList<DeckData> decks = controller.SavedDecks;
        if (decks.Count == 0)
        {
            AddText(content, "No saved decks yet.", 28, FontStyle.Normal, TextAnchor.MiddleLeft);
            AddButton(content, "Create Deck", ShowCreateDeck);
            AddButton(content, "Back To Camera", HideAllPanels);
            return;
        }

        for (int i = 0; i < decks.Count; i++)
        {
            DeckData deck = decks[i];
            if (deck == null)
                continue;

            string deckId = deck.deckId;
            string label = $"{deck.deckName} ({deck.cards?.Count ?? 0} cards)";
            AddButton(content, label, () =>
            {
                if (editMode)
                {
                    controller.LoadDeckIntoEditor(deckId);
                    ShowBuilder();
                    return;
                }

                controller.SelectDeckForPlay(deckId);
                HideAllPanels();
            });
        }

        AddButton(content, "Back To Camera", HideAllPanels);
    }

    void Run(Action action)
    {
        action?.Invoke();
        UpdateStatus();
    }

    void UpdateStatus()
    {
        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(controller.StatusMessage) ? "Ready." : controller.StatusMessage;
    }

    void ResolveController()
    {
        if (controller != null)
            return;

        controller = FindFirstObjectByType<MobileDeckBuilderController>();
        if (controller != null)
            return;

        var controllerObject = new GameObject("Mobile Deck Builder Controller");
        controller = controllerObject.AddComponent<MobileDeckBuilderController>();
    }

    Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Mobile Deck Builder Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var createdCanvas = canvasObject.GetComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        createdCanvas.sortingOrder = 1000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return createdCanvas;
    }

    GameObject CreateFullPanel(Transform parent)
    {
        var panel = CreateUiObject("Builder Panel", parent);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panel.AddComponent<Image>();
        image.color = new Color(0.06f, 0.07f, 0.08f, 0.95f);
        return panel;
    }

    GameObject CreateCollapsedButton(Transform parent)
    {
        GameObject button = CreateButtonObject("Menu", parent, ShowMenu);
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(190f, 74f);
        return button;
    }

    GameObject CreateMenuPanel(Transform parent)
    {
        var panel = CreateUiObject("App Menu Panel", parent);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(560f, 0f);

        var image = panel.AddComponent<Image>();
        image.color = new Color(0.07f, 0.08f, 0.09f, 0.96f);

        var title = AddText(panel.transform, "Menu", 52, FontStyle.Bold, TextAnchor.MiddleLeft);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.offsetMin = new Vector2(42f, -128f);
        title.rectTransform.offsetMax = new Vector2(-42f, -42f);

        CreateMenuButton(panel.transform, "Pick Deck", new Vector2(42f, -178f), () => ShowDeckPicker(false));
        CreateMenuButton(panel.transform, "Create Deck", new Vector2(42f, -306f), ShowCreateDeck);
        CreateMenuButton(panel.transform, "Edit Deck", new Vector2(42f, -434f), () => ShowDeckPicker(true));
        CreateMenuButton(panel.transform, "Settings", new Vector2(42f, -562f), ShowSettings);
        CreateMenuButton(panel.transform, "Close", new Vector2(42f, -690f), HideMenu, new Color(0.25f, 0.27f, 0.3f, 1f));
        return panel;
    }

    GameObject CreateMenuButton(Transform parent, string text, Vector2 topLeft, Action onClick, Color? colorOverride = null)
    {
        var buttonObject = CreateButtonObject(text, parent, () =>
        {
            Debug.Log($"Menu button tapped: {text}", this);
            onClick?.Invoke();
        });

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = topLeft;
        rect.sizeDelta = new Vector2(476f, 104f);

        var layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement != null)
            Destroy(layoutElement);

        var image = buttonObject.GetComponent<Image>();
        if (image != null)
            image.color = colorOverride ?? new Color(0.93f, 0.35f, 0.18f, 1f);

        return buttonObject;
    }

    RectTransform CreateScrollContent(Transform parent)
    {
        var scrollObject = CreateUiObject("Scroll View", parent);
        var scrollRect = scrollObject.AddComponent<ScrollRect>();
        var scrollTransform = scrollObject.GetComponent<RectTransform>();
        scrollTransform.anchorMin = Vector2.zero;
        scrollTransform.anchorMax = Vector2.one;
        scrollTransform.offsetMin = new Vector2(28f, 28f);
        scrollTransform.offsetMax = new Vector2(-28f, -28f);

        var viewport = CreateUiObject("Viewport", scrollObject.transform);
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        var content = CreateUiObject("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.padding = new RectOffset(0, 0, 0, 28);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        return contentRect;
    }

    void AddTitle(Transform parent, string text)
    {
        Text title = AddText(parent, text, 46, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0f, 72f));
        SetLayoutSize(title.gameObject, 72f, 72f, -1f);
    }

    void AddSection(Transform parent, string text)
    {
        Text label = AddText(parent, text, 34, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0f, 58f));
        label.color = new Color(1f, 0.72f, 0.28f, 1f);
        SetLayoutSize(label.gameObject, 58f, 58f, -1f);
    }

    InputField AddInput(Transform parent, string label, string placeholder, Action<string> onEndEdit, InputField.ContentType contentType = InputField.ContentType.Standard)
    {
        var row = CreateRow(parent);
        AddText(row.transform, label, 24, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(330f, 64f));

        var inputObject = CreateUiObject($"{label} Input", row.transform);
        var inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.sizeDelta = new Vector2(0f, 64f);
        inputObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        SetLayoutSize(inputObject, 64f, 64f, -1f);

        var input = inputObject.AddComponent<InputField>();
        input.contentType = contentType;

        Text textComponent = AddText(inputObject.transform, "", 24, FontStyle.Normal, TextAnchor.MiddleLeft);
        textComponent.color = Color.white;
        Stretch(textComponent.rectTransform, new Vector2(18f, 0f), new Vector2(-18f, 0f));

        Text placeholderText = AddText(inputObject.transform, placeholder, 24, FontStyle.Italic, TextAnchor.MiddleLeft);
        placeholderText.color = new Color(1f, 1f, 1f, 0.45f);
        Stretch(placeholderText.rectTransform, new Vector2(18f, 0f), new Vector2(-18f, 0f));

        input.textComponent = textComponent;
        input.placeholder = placeholderText;
        if (onEndEdit != null)
            input.onEndEdit.AddListener(value => Run(() => onEndEdit(value)));

        return input;
    }

    void AddDropdown(Transform parent, string label, List<string> options, Action<int> onValueChanged)
    {
        var row = CreateRow(parent);
        AddText(row.transform, label, 24, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(330f, 64f));

        var dropdownObject = CreateUiObject($"{label} Dropdown", row.transform);
        dropdownObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 64f);
        dropdownObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
        SetLayoutSize(dropdownObject, 64f, 64f, -1f);

        var labelText = AddText(dropdownObject.transform, "", 24, FontStyle.Normal, TextAnchor.MiddleLeft);
        Stretch(labelText.rectTransform, new Vector2(18f, 0f), new Vector2(-54f, 0f));

        var arrow = AddText(dropdownObject.transform, "v", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(arrow.rectTransform, new Vector2(0f, 0f), new Vector2(-14f, 0f));
        arrow.rectTransform.anchorMin = new Vector2(1f, 0f);
        arrow.rectTransform.anchorMax = new Vector2(1f, 1f);
        arrow.rectTransform.sizeDelta = new Vector2(40f, 0f);

        var template = CreateDropdownTemplate(dropdownObject.transform);
        var dropdown = dropdownObject.AddComponent<Dropdown>();
        dropdown.captionText = labelText;
        dropdown.template = template;
        dropdown.itemText = template.GetComponentInChildren<Toggle>(true).GetComponentInChildren<Text>(true);
        dropdown.options = new List<Dropdown.OptionData>();
        for (int i = 0; i < options.Count; i++)
            dropdown.options.Add(new Dropdown.OptionData(options[i]));

        dropdown.onValueChanged.AddListener(value => onValueChanged?.Invoke(value));
    }

    RectTransform CreateDropdownTemplate(Transform parent)
    {
        var template = CreateUiObject("Template", parent);
        template.SetActive(false);
        var templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0f, 320f);
        template.AddComponent<Image>().color = new Color(0.12f, 0.13f, 0.15f, 1f);
        template.AddComponent<ScrollRect>();

        var viewport = CreateUiObject("Viewport", template.transform);
        var viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, Vector2.zero, Vector2.zero);
        viewport.AddComponent<RectMask2D>();

        var content = CreateUiObject("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 320f);
        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;

        var item = CreateUiObject("Item", content.transform);
        item.AddComponent<Toggle>();
        item.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
        item.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 58f);
        SetLayoutSize(item, 58f, 58f, -1f);
        var itemText = AddText(item.transform, "Option", 23, FontStyle.Normal, TextAnchor.MiddleLeft);
        Stretch(itemText.rectTransform, new Vector2(18f, 0f), new Vector2(-18f, 0f));

        var itemToggle = item.GetComponent<Toggle>();
        itemToggle.targetGraphic = item.GetComponent<Image>();

        var scroll = template.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;

        return templateRect;
    }

    void AddButton(Transform parent, string text, Action onClick)
    {
        CreateButtonObject(text, parent, onClick);
    }

    void AddButtonRow(Transform parent, params (string label, Action action)[] buttons)
    {
        var row = CreateRow(parent);
        for (int i = 0; i < buttons.Length; i++)
            CreateButtonObject(buttons[i].label, row.transform, buttons[i].action);
    }

    GameObject CreateButtonObject(string text, Transform parent, Action onClick)
    {
        var buttonObject = CreateUiObject($"{text} Button", parent);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 78f);
        SetLayoutSize(buttonObject, 78f, 78f, -1f);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.93f, 0.35f, 0.18f, 1f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());

        Text buttonText = AddText(buttonObject.transform, text, 25, FontStyle.Bold, TextAnchor.MiddleCenter);
        buttonText.raycastTarget = false;
        Stretch(buttonText.rectTransform, new Vector2(16f, 0f), new Vector2(-16f, 0f));
        return buttonObject;
    }

    GameObject CreateRow(Transform parent)
    {
        var row = CreateUiObject("Row", parent);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 72f);
        SetLayoutSize(row, 72f, 72f, -1f);

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;
        return row;
    }

    Text AddText(Transform parent, string text, int size, FontStyle style, TextAnchor alignment, Vector2? fixedSize = null)
    {
        var textObject = CreateUiObject($"{text} Text", parent);
        var rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = fixedSize ?? new Vector2(0f, 52f);
        SetLayoutSize(textObject, rect.sizeDelta.y, rect.sizeDelta.y, fixedSize.HasValue ? fixedSize.Value.x : -1f);

        var textComponent = textObject.AddComponent<Text>();
        textComponent.font = uiFont;
        textComponent.text = text;
        textComponent.fontSize = size;
        textComponent.fontStyle = style;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    void SetLayoutSize(GameObject target, float minHeight, float preferredHeight, float preferredWidth)
    {
        if (target == null)
            return;

        var layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = target.AddComponent<LayoutElement>();

        layoutElement.minHeight = minHeight;
        layoutElement.preferredHeight = preferredHeight;

        if (preferredWidth > 0f)
            layoutElement.preferredWidth = preferredWidth;
        else
            layoutElement.flexibleWidth = 1f;
    }

    GameObject CreateUiObject(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    void EnsureEventSystem()
    {
        var existingEventSystem = FindFirstObjectByType<EventSystem>();
        if (existingEventSystem != null)
        {
            EnsureInputModule(existingEventSystem.gameObject);
            return;
        }

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        EnsureInputModule(eventSystemObject);
        eventSystemObject.transform.SetParent(transform, false);
    }

    void EnsureInputModule(GameObject eventSystemObject)
    {
        if (eventSystemObject == null)
            return;

#if ENABLE_INPUT_SYSTEM
        var oldInputModule = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (oldInputModule != null)
        {
            if (Application.isPlaying)
                Destroy(oldInputModule);
            else
                DestroyImmediate(oldInputModule);
        }

        var inputSystemModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
            inputSystemModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();

        inputSystemModule.AssignDefaultActions();
#else
        if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    static List<string> EnumNames<T>() where T : Enum
    {
        return new List<string>(Enum.GetNames(typeof(T)));
    }

    List<string> ModelOptionNames()
    {
        var options = new List<string>();
        foreach (BuiltInModelOption option in controller.BuiltInModels)
            options.Add(string.IsNullOrWhiteSpace(option.displayName) ? option.resourcesPath : option.displayName);

        return options;
    }

    List<string> SoundOptionNames(IReadOnlyList<BuiltInSoundOption> sounds)
    {
        var options = new List<string>();
        for (int i = 0; i < sounds.Count; i++)
        {
            BuiltInSoundOption option = sounds[i];
            options.Add(string.IsNullOrWhiteSpace(option.displayName) ? option.resourcesPath : option.displayName);
        }

        return options;
    }
}
