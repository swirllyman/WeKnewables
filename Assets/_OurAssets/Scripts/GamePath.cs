using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePath : MonoBehaviour
{
    [Header("Path")]
    public Vector3[] waypoints;
    public float spawnRadius = .7f;
    public float waypointRadius = .5f;

    internal int totalSpawnCount = 30;
    internal int satisfiedCount = 0;
    internal int fullySatisfiedCount = 0;

    List<GroundUnit> currentUnits;
    
    internal bool roundActive = false;
    float spawnRate = 1.0f;
    float spawnTimer = 0.0f;
    int currentSpawnCount = 0;

    [ContextMenu("Send Wave")]
    public void SendWave()
    {
        if (!roundActive)
        {
            currentUnits = new List<GroundUnit>();
            totalSpawnCount = GameManager.GetCurrentGenSpawnAmount();
            spawnRate = GameManager.currentGeneration.unitSpawnSpeed;
            fullySatisfiedCount = 0;
            satisfiedCount = 0;
            spawnTimer = 0.0f;
            currentSpawnCount = 0;
            roundActive = true;
        }
    }

    public void EndWaveEarly()
    {
        roundActive = false;
        EndWave();
    }

    private void Update()
    {
        if (roundActive && totalSpawnCount > currentSpawnCount)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0.0f)
            {
                Spawn();
            }
        }
    }

    void Spawn()
    {
        spawnTimer = spawnRate;
        currentSpawnCount++;

        GameObject unitObject = Instantiate(GameManager.currentGeneration.unitObjects[Random.Range(0, GameManager.currentGeneration.unitObjects.Length)], transform.position
            + ((Vector3)Random.insideUnitCircle * spawnRadius), Quaternion.identity);
        GroundUnit newUnit = unitObject.GetComponent<GroundUnit>();
        newUnit.SetPath(this);
        newUnit.onFinishedPath += OnUnitFinishedPath;
        newUnit.onSatisfied += OnUnitSatisfied;
        newUnit.onFullySatisfied += OnUnitFullySatisfied;
        currentUnits.Add(newUnit);
    }

    private void OnUnitFullySatisfied(GroundUnit unit)
    {
        unit.onFullySatisfied -= OnUnitFullySatisfied;
        fullySatisfiedCount++;
        GameManager.singleton.UnitFullySatisfied();
        if (currentUnits.Contains(unit))
        {
            currentUnits.Remove(unit);
            if (currentSpawnCount == totalSpawnCount && currentUnits.Count <= 0 & !GameManager.failed)
            {
                roundActive = false;
                EndWave();
            }
        }
    }

    private void OnUnitSatisfied(GroundUnit unit)
    {
        unit.onSatisfied -= OnUnitSatisfied;
        satisfiedCount++;

        GameManager.singleton.UnitSatisfied();
    }

    private void OnUnitFinishedPath(GroundUnit finishedUnit)
    {
        finishedUnit.onFinishedPath -= OnUnitFinishedPath;
        finishedUnit.onFullySatisfied -= OnUnitFullySatisfied;
        finishedUnit.onSatisfied -= OnUnitSatisfied;
        if (currentUnits.Contains(finishedUnit))
        {
            currentUnits.Remove(finishedUnit);
            if (currentUnits.Count <= 0 && roundActive &! GameManager.failed)
            {
                Debug.Log("Wave Finished");
                roundActive = false;
                EndWave();
            }
        }

        if (!finishedUnit.satisfied && roundActive)
        {
            GameManager.singleton.pathManager.Leak();
        }

        Destroy(finishedUnit.gameObject);
    }

    void EndWave()
    {
        GameManager.singleton.WaveFinished();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position + waypoints[i], transform.position + waypoints[i + 1]);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + waypoints[i + 1], waypointRadius);
        }
    }
}
