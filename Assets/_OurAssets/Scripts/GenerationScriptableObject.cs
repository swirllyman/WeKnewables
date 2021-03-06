using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Generation", menuName = "ScriptableObjects/Generation", order = 1)]
public class GenerationScriptableObject : ScriptableObject
{
    public int unitsPerWave = 5;
    public int waveEndBonus = 100;
    public float buildTime = 30.0f;
    public float unitSpawnSpeed = .25f;
    public float generationEndBonus = 350;

    public float unitHappiness = 100;
    public float unitFullHappiness = 100;
    
    public float unitSpeed = 1;
    public int unitMoney = 10;

    public GameObject[] unitObjects;
}
