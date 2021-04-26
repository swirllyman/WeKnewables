using UnityEngine;

[CreateAssetMenu(fileName = "StructureProperty", menuName = "ScriptableObjects/StructureProperty", order = 1)]
public class StructurePropertyScriptableObject : ScriptableObject
{
    public string structureName;
    public int currentLevel = 0;
    public int size = 1;
    public bool powerStructure = false;
    public bool waterStructure = false;
    public Sprite[] currentSprites;
    public GameObject[] structureObjects;
    public float[] cost;
    public TowerProperties towerProperties;

    [TextArea(3, 5)]
    public string[] tooltip;

    internal float GetAttackSpeed()
    {
        return towerProperties.attackSpeed[currentLevel];
    }

    internal float GetRange()
    {
        return towerProperties.range[currentLevel];
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