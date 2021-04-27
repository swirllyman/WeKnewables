using UnityEngine;

[CreateAssetMenu(fileName = "StructureProperty", menuName = "ScriptableObjects/StructureProperty", order = 1)]
public class StructurePropertyScriptableObject : ScriptableObject
{
    public enum LocationRestrictions { None, Land, Water}

    public string structureName;
    public int currentLevel = 0;
    public int size = 1;
    public bool powerStructure = false;
    public LocationRestrictions buildRestrictions;

    //public bool requireLand = false;
    //public bool requireWater = false;
    public TowerProperties towerProperties;
    public int[] cost;
    public Sprite[] currentSprites;
    public GameObject[] structureObjects;

    [TextArea(3, 5)]
    public string[] tooltip;

    internal float GetAttackSpeed()
    {
        return towerProperties.attackSpeed[currentLevel];
    }

    internal float GetRange()
    {
        return powerStructure ? towerProperties.range[currentLevel] : towerProperties.range[currentLevel] * 25;
    }

    internal float GetSatisfaction()
    {
        return towerProperties.satisfaction[currentLevel];
    }

    internal float GetPollution()
    {
        return towerProperties.pollution[currentLevel];
    }

    internal GameObject GetProjectile()
    {
        return towerProperties.projectiles[currentLevel];
    }
}

[System.Serializable]
public struct TowerProperties
{
    public float[] attackSpeed;
    public float[] range;
    public float[] satisfaction;
    public float[] pollution;
    public GameObject[] projectiles;
}