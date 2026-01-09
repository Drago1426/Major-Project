using UnityEngine;

public enum ElementType
{
    Fire, Water, Ice, Earth, Air, Light, Dark
}

[CreateAssetMenu(fileName = "NewCardData", menuName = "AR Card Game/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Card Info")]
    public string cardName;
    public ElementType element;

    [Header("Summon Content")]
    public GameObject creaturePrefab;

    [Header("VFX / SFX")]
    public Color runeColor = Color.white;
    public Color fireColor = new Color(1f, 0.5f, 0f);
    public AudioClip summonSfx;

    [Header("Timing")]
    public float waitBeforeFire = 0.5f;
    public float waitBeforeCreature = 0.1f;
}
