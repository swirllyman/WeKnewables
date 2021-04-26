using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GroundUnit : MonoBehaviour
{
    public float currentSpeed = 5.0f;
    public float minDistanceToNextNode = .5f;
    public float decayRate = .1f;
    public Image heartImage;
    public GameObject filledHeartObject;
    public GameObject smileFace;
    public Collider2D myCollider;
    internal GamePath currentPath;
    internal bool satisfied = false;

    bool runningPath = false;
    int currentWaypointID = 0;
    float currentSatisfactionPercent = .1f;
    Vector3 destination;

    public delegate void FinishedPathCallback(GroundUnit thisUnit);
    public event FinishedPathCallback onFinishedPath;

    public delegate void SatisfiedCallback(GroundUnit thisUnit);
    public event SatisfiedCallback onSatisfied;

    internal void Satisfy(float satisfyAmount)
    {
        if (satisfied) return;
        currentSatisfactionPercent = Mathf.Clamp01(currentSatisfactionPercent + (satisfyAmount / 100));
        heartImage.fillAmount = currentSatisfactionPercent;

        if(currentSatisfactionPercent >= 1.0f)
        {
            onSatisfied?.Invoke(this);
            satisfied = true;
            filledHeartObject.SetActive(true);
            smileFace.SetActive(true);
            smileFace.transform.parent = null;
            LeanTween.scale(smileFace, smileFace.transform.localScale * 1.15f, .25f).setLoopPingPong().setEaseInExpo();
            LeanTween.move(smileFace, smileFace.transform.position + Vector3.up * 1.15f, 2.0f).setEasePunch();
            Destroy(smileFace, 2.5f);
        }
    }

    internal void SetPath(GamePath newPath)
    {
        currentPath = newPath;
        runningPath = true;
        SetNextWaypoint();
        heartImage.fillAmount = currentSatisfactionPercent;
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
            transform.position += (destination - transform.position).normalized * Time.fixedDeltaTime * currentSpeed;

            if(Vector3.Distance(transform.position, destination) < minDistanceToNextNode)
            {
                SetNextWaypoint();
            }
        }
    }
}
