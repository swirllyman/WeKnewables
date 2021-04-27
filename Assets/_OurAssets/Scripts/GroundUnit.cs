using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GroundUnit : MonoBehaviour
{
    public float minDistanceToNextNode = .5f;
    public Image heartImage;
    public Image fullySatisfiedHeartImage;
    public GameObject smileFace;
    public Collider2D myCollider;
    internal GamePath currentPath;
    internal bool satisfied = false;
    internal bool fullySatisfied = false;

    bool runningPath = false;
    int currentWaypointID = 0;
    float currentSpeed = 5.0f;
    float currentSatisfiedPercent = .1f;
    float currentFullySatisfiedPercent = .1f;
    Vector3 destination;

    public delegate void FinishedPathCallback(GroundUnit thisUnit);
    public event FinishedPathCallback onFinishedPath;

    public delegate void SatisfiedCallback(GroundUnit thisUnit);
    public event SatisfiedCallback onSatisfied;

    public delegate void FullySatisfiedCallback(GroundUnit thisUnit);
    public event FullySatisfiedCallback onFullySatisfied;

    internal void Satisfy(float satisfyAmount)
    {
        if (satisfied)
        {
            currentFullySatisfiedPercent = Mathf.Clamp01(currentFullySatisfiedPercent + (satisfyAmount / GameManager.currentGeneration.unitFullHappiness));
            fullySatisfiedHeartImage.fillAmount = currentFullySatisfiedPercent;

            if (currentFullySatisfiedPercent >= 1.0f)
            {
                onFullySatisfied?.Invoke(this);
                fullySatisfied = true;
            }
        }
        else
        {
            currentSatisfiedPercent = Mathf.Clamp01(currentSatisfiedPercent + (satisfyAmount / GameManager.currentGeneration.unitHappiness));
            heartImage.fillAmount = currentSatisfiedPercent;

            if (currentSatisfiedPercent >= 1.0f)
            {
                onSatisfied?.Invoke(this);
                satisfied = true;
                fullySatisfiedHeartImage.gameObject.SetActive(true);
                smileFace.SetActive(true);
                smileFace.transform.parent = null;
                LeanTween.scale(smileFace, smileFace.transform.localScale * 1.15f, .25f).setLoopPingPong().setEaseInExpo();
                LeanTween.move(smileFace, smileFace.transform.position + Vector3.up * 1.15f, 2.0f).setEasePunch();
                Destroy(smileFace, 2.5f);
            }
        }
    }

    internal void SetPath(GamePath newPath)
    {
        currentSpeed = GameManager.currentGeneration.unitSpeed * Random.Range(.95f, 1.05f);
        currentPath = newPath;
        runningPath = true;
        SetNextWaypoint();
        heartImage.fillAmount = currentSatisfiedPercent;
        fullySatisfiedHeartImage.fillAmount = 0.0f;
        LeanTween.scale(gameObject, transform.localScale * 1.15f, .25f).setLoopPingPong().setEaseSpring();
    }

    void SetNextWaypoint()
    {
        currentWaypointID++;

        if(currentWaypointID >= currentPath.waypoints.Length)
        {
            runningPath = false;
            onFinishedPath?.Invoke(this);
        }
        else
        {
            destination = currentPath.transform.position + currentPath.waypoints[currentWaypointID] + (Vector3)Random.insideUnitCircle * currentPath.waypointRadius;
        }
    }

    private void FixedUpdate()
    {
        if (runningPath)
        {
            transform.position += (destination - transform.position).normalized * Time.fixedDeltaTime * currentSpeed * 2;

            if(Vector3.Distance(transform.position, destination) < minDistanceToNextNode)
            {
                SetNextWaypoint();
            }
        }
    }
}
