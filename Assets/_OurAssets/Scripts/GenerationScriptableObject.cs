using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Generation", menuName = "ScriptableObjects/Generation", order = 1)]
public class GenerationScriptableObject : ScriptableObject
{
    public int unitsPerWave = 5;
    public int waveEndBonus = 100;
    public float unitSpawnSpeed = .25f;

    public float unitHappiness = 100;
    public float unitFullHappiness = 100;
    
    public int unitSpeed = 1;
    public int unitMoney = 10;

    public GameObject unitObject;
}
