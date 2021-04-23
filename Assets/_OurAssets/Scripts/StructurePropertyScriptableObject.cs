using UnityEngine;

[CreateAssetMenu(fileName = "StructureProperty", menuName = "ScriptableObjects/StructureProperty", order = 1)]
public class StructurePropertyScriptableObject : ScriptableObject
{
    public string structureName;
    public Sprite[] structureSprite;
    public float[] cost;
    public float[] smog;
}