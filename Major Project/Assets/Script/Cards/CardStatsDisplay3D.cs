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
    [SerializeField] Vector3 localOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] float iconSize = 0.01f;
    [SerializeField] float iconSpacing = 0.1f;
    [SerializeField] int fontSize = 30;
    [SerializeField] Color textColor = Color.white;

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

        GameObject textObject = new GameObject(label + " Text");
        textObject.transform.SetParent(iconGroup.transform, false);

        // Slightly in front of the icon so the text renders on top.
        textObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        textObject.transform.localScale = Vector3.one * 0.01f;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = value.ToString();
        textMesh.fontSize = fontSize;
        textMesh.color = textColor;

        // This centers the text in the middle of the icon.
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        textRenderer.sortingOrder = 11;
    }
}