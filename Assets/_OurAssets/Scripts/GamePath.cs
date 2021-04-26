using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePath : MonoBehaviour
{
    [Header("Path")]
    public Vector3[] waypoints;
    public float spawnRadius = .7f;
    public float waypointRadius = .5f;

    [Header("Units")]
    public GameObject unitPrefab;
    public List<GroundUnit> currentUnits;
    public float spawnRate = 1.0f;
    public int totalSpawnCount = 30;

    bool roundActive = false;

    float spawnTimer = 0.0f;
    int currentSpawnCount = 0;
    int satisfiedCount = 0;

    [ContextMenu("Send Wave")]
    public void SendWave()
    {
        if (!roundActive)
        {
            satisfiedCount = 0;
            spawnTimer = 0.0f;
            currentSpawnCount = 0;
            roundActive = true;
        }
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

        GameObject unitObject = Instantiate(unitPrefab, transform.position + ((Vector3)Random.insideUnitCircle * spawnRadius), Quaternion.identity);
        GroundUnit newUnit = unitObject.GetComponent<GroundUnit>();
        newUnit.SetPath(this);
        newUnit.onFinishedPath += OnUnitFinishedPath;
        newUnit.onSatisfied += OnUnitSatisfied;
        currentUnits.Add(newUnit);
        //GameManager.singleton.pathManager.UpdateCount(currentUnits.Count);
    }

    private void OnUnitSatisfied(GroundUnit unit)
    {
        unit.onSatisfied -= OnUnitSatisfied;
        if (currentUnits.Contains(unit))
        {
            satisfiedCount++;
            currentUnits.Remove(unit);
            GameManager.singleton.pathManager.UpdateCount(totalSpawnCount - satisfiedCount);
            if (currentSpawnCount == totalSpawnCount && currentUnits.Count <= 0)
            {
                Debug.Log("Wave Finished");
                roundActive = false;
                StartCoroutine(StartNextWave());
            }
        }

        //TODO: Give Points Here
    }

    private void OnUnitFinishedPath(GroundUnit finishedUnit)
    {
        finishedUnit.onFinishedPath -= OnUnitFinishedPath;
        if (currentUnits.Contains(finishedUnit))
        {
            currentUnits.Remove(finishedUnit);
            GameManager.singleton.pathManager.UpdateCount(currentUnits.Count);

            if (currentUnits.Count <= 0 && roundActive)
            {
                Debug.Log("Wave Finished");
                roundActive = false;
                StartCoroutine(StartNextWave());
            }
        }

        if (!finishedUnit.satisfied)
        {
            //Leak Here
        }

        Destroy(finishedUnit.gameObject);
    }

    IEnumerator StartNextWave()
    {
        yield return new WaitForSeconds(5.0f);
        GameManager.singleton.ShowNextPath();
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
