using UnityEngine;

public class CardStatsDisplay3D : MonoBehaviour
{
    [Header("Icons")]
    [SerializeField] Sprite healthIcon;
    [SerializeField] Sprite manaIcon;
    [SerializeField] Sprite damageIcon;

    [Header("Values")]
    [SerializeField] int health;
    [SerializeField] int mana;
    [SerializeField] int damage;

    [Header("Layout")]
    [SerializeField] Vector3 localOffset = new Vector3(0f, 0.15f, 0f);
    [SerializeField] float iconSize = 0.01f;
    [SerializeField] float iconSpacing = 0.075f;
    [SerializeField] int fontSize = 30;
    [SerializeField] Color textColor = Color.white;
    [SerializeField] Color outlineColor = Color.black;
    [SerializeField] float outlineOffset = 0.0015f;

    [Header("Billboard")]
    [SerializeField] bool faceCamera = true;

    GameObject statsRoot;

    public void Initialize(
        int newHealth,
        int newMana,
        int newDamage,
        Sprite newHealthIcon,
        Sprite newManaIcon,
        Sprite newDamageIcon)
    {
        health = newHealth;
        mana = newMana;
        damage = newDamage;

        healthIcon = newHealthIcon;
        manaIcon = newManaIcon;
        damageIcon = newDamageIcon;

        BuildDisplay();
    }

    void Start()
    {
        if (statsRoot == null)
            BuildDisplay();
    }

    void LateUpdate()
    {
        if (!faceCamera || statsRoot == null || Camera.main == null)
            return;

        Vector3 directionToCamera = statsRoot.transform.position - Camera.main.transform.position;
        statsRoot.transform.rotation = Quaternion.LookRotation(directionToCamera);
    }

    void BuildDisplay()
    {
        if (statsRoot != null)
            Destroy(statsRoot);

        statsRoot = new GameObject("Card Stats Display");
        statsRoot.transform.SetParent(transform, false);
        statsRoot.transform.localPosition = localOffset;
        statsRoot.transform.localRotation = Quaternion.identity;
        statsRoot.transform.localScale = Vector3.one;

        CreateStatIcon("Health", healthIcon, health, -iconSpacing);
        CreateStatIcon("Mana", manaIcon, mana, 0f);
        CreateStatIcon("Damage", damageIcon, damage, iconSpacing);
    }

    void CreateStatIcon(string label, Sprite icon, int value, float xPosition)
    {
        GameObject iconGroup = new GameObject(label + " Icon Group");
        iconGroup.transform.SetParent(statsRoot.transform, false);
        iconGroup.transform.localPosition = new Vector3(xPosition, 0f, 0f);

        GameObject iconObject = new GameObject(label + " Icon");
        iconObject.transform.SetParent(iconGroup.transform, false);
        iconObject.transform.localPosition = Vector3.zero;
        iconObject.transform.localScale = Vector3.one * iconSize;

        SpriteRenderer iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = icon;
        iconRenderer.sortingOrder = 10;

        string valueText = value.ToString();
        Vector3 textPosition = new Vector3(0f, 0f, -0.01f);

        CreateTextMesh(label + " Outline Left", iconGroup.transform, valueText, textPosition + new Vector3(-outlineOffset, 0f, 0f), outlineColor, 11);
        CreateTextMesh(label + " Outline Right", iconGroup.transform, valueText, textPosition + new Vector3(outlineOffset, 0f, 0f), outlineColor, 11);
        CreateTextMesh(label + " Outline Up", iconGroup.transform, valueText, textPosition + new Vector3(0f, outlineOffset, 0f), outlineColor, 11);
        CreateTextMesh(label + " Outline Down", iconGroup.transform, valueText, textPosition + new Vector3(0f, -outlineOffset, 0f), outlineColor, 11);
        CreateTextMesh(label + " Outline Up Left", iconGroup.transform, valueText, textPosition + new Vector3(-outlineOffset, outlineOffset, 0f), outlineColor, 11);
        CreateTextMesh(label + " Outline Up Right", iconGroup.transform, valueText, textPosition + new Vector3(outlineOffset, outlineOffset, 0f), outlineColor, 11);
        CreateTextMesh(label + " Outline Down Left", iconGroup.transform, valueText, textPosition + new Vector3(-outlineOffset, -outlineOffset, 0f), outlineColor, 11);
        CreateTextMesh(label + " Outline Down Right", iconGroup.transform, valueText, textPosition + new Vector3(outlineOffset, -outlineOffset, 0f), outlineColor, 11);
        CreateTextMesh(label + " Text", iconGroup.transform, valueText, textPosition, textColor, 12);
    }

    void CreateTextMesh(string objectName, Transform parent, string value, Vector3 localPosition, Color color, int sortingOrder)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localScale = Vector3.one * 0.01f;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = value;
        textMesh.fontSize = fontSize;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        textRenderer.sortingOrder = sortingOrder;
    }
}
