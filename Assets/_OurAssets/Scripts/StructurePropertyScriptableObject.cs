using UnityEngine;

[CreateAssetMenu(fileName = "StructureProperty", menuName = "ScriptableObjects/StructureProperty", order = 1)]
public class StructurePropertyScriptableObject : ScriptableObject
{
    public enum LocationRestrictions { None, Land, Water}

    public string structureName;
    public bool canSell = true;
    public int cost;
    public int sellPrice;
    public float range;
    [Range(1, 3)]
    public int size = 1;
    public int pollution = 0;

    [Header("Attack Properties")]
    public AttackProperties attackProperties;

    [Header("Restrictions")]
    public bool powerStructure = false;
    public LocationRestrictions buildRestrictions;

    [Header("Bonuses")]
    public BonusProperties bonusProperties;

    [Header("References")]
    public Sprite currentSprite;
    public GameObject structureObject;

    [TextArea(3, 5)]
    public string[] tooltip;
}

[System.Serializable]
public struct AttackProperties
{
    public float attackSpeed;
    public float satisfaction;
    public GameObject projectile;
}

[System.Serializable]
public struct BonusProperties
{
    [Header("Slow")]
    public bool slow;
    [Range(0, 1)]
    public float slowPercent;
    public float slowDurationInSeconds;

    [Header("AoE")]
    public bool AoE;
    public float radius;

    [Header("Emit From Tower")]
    public bool emitFromTower;
}