using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GroundUnit : MonoBehaviour
{
    public float minDistanceToNextNode = .5f;
    public SpriteRenderer myRend;
    public Color slowedColor;
    public Image heartImage;
    public Image fullySatisfiedHeartImage;
    public Image completedHeartImage;
    public GameObject smileFace;
    public Collider2D myCollider;
    internal GamePath currentPath;
    internal bool satisfied = false;
    internal bool fullySatisfied = false;

    bool runningPath = false;
    int currentWaypointID = 0;
    float mySpeed = 5.0f;
    float currentSpeed = 5.0f;
    float currentSatisfiedPercent = .1f;
    float currentFullySatisfiedPercent = .1f;

    float currentSatisfaction = 0.0f;

    //float 
    Vector3 destination;

    Coroutine currentSlowRoutine;

    public delegate void FinishedPathCallback(GroundUnit thisUnit);
    public event FinishedPathCallback onFinishedPath;

    public delegate void SatisfiedCallback(GroundUnit thisUnit);
    public event SatisfiedCallback onSatisfied;

    public delegate void FullySatisfiedCallback(GroundUnit thisUnit);
    public event FullySatisfiedCallback onFullySatisfied;

    internal void Satisfy(float satisfyAmount)
    {

        if(GameManager.singleton.pollution.pollutionPercent > .4f)
        {
            satisfyAmount = satisfyAmount * (1 - GameManager.singleton.pollution.pollutionPercent);
        }

        currentSatisfaction += satisfyAmount;

        if (satisfied)
        {

            currentSatisfiedPercent = (currentSatisfaction - GameManager.CurrentGenHappiness()) / GameManager.CurrentGenFullHappiness();
            fullySatisfiedHeartImage.fillAmount = currentSatisfiedPercent;

            if (currentSatisfiedPercent >= 1.0f)
            {
                completedHeartImage.enabled = true;
                fullySatisfied = true;
                onFullySatisfied?.Invoke(this);
            }
        }
        else
        {
            currentSatisfiedPercent = currentSatisfaction / GameManager.CurrentGenHappiness();
            heartImage.fillAmount = currentSatisfiedPercent;

            if (currentSatisfiedPercent >= 1.0f)
            {
                satisfied = true;
                fullySatisfiedHeartImage.gameObject.SetActive(true);
                smileFace.SetActive(true);
                smileFace.transform.parent = null;
                LeanTween.scale(smileFace, smileFace.transform.localScale * 1.15f, .25f).setLoopPingPong().setEaseInExpo();
                LeanTween.move(smileFace, smileFace.transform.position + Vector3.up * 1.15f, 2.0f).setEasePunch();
                Destroy(smileFace, 2.5f);
                onSatisfied?.Invoke(this);
                currentSatisfiedPercent = 0.0f;
            }
        }
    }

    internal void Slow(float slowPerc, float slowDuration)
    {
        if (currentSlowRoutine != null) StopCoroutine(currentSlowRoutine);
        currentSlowRoutine = StartCoroutine(SlowRoutine(slowDuration));
        currentSpeed = mySpeed * (1 - slowPerc);
        myRend.color = slowedColor;
    }

    IEnumerator SlowRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        currentSpeed = mySpeed;
        myRend.color = Color.white;
    }

    internal void SetPath(GamePath newPath)
    {
        mySpeed = GameManager.currentGeneration.unitSpeed * Random.Range(.95f, 1.05f) * GameManager.GetCurrentGenSpeed();
        currentSpeed = mySpeed;
        currentPath = newPath;
        runningPath = true;
        SetNextWaypoint();
        heartImage.fillAmount = currentSatisfiedPercent;
        fullySatisfiedHeartImage.fillAmount = 0.0f;
        LeanTween.scale(gameObject, transform.localScale * 1.15f, .25f).setLoopPingPong().setEaseSpring();

        if(currentSpeed > 3.0f)
        {
            minDistanceToNextNode = Mathf.Lerp(.25f, .75f, currentSpeed / 10);
        }
    }

    void SetNextWaypoint()
    {
        currentWaypointID++;
        if (currentWaypointID >= currentPath.waypoints.Length)
        {
            runningPath = false;
            onFinishedPath?.Invoke(this);
        }
        else
        {
            destination = currentPath.transform.position + currentPath.waypoints[currentWaypointID] + (Vector3)Random.insideUnitCircle * currentPath.waypointRadius;
            myRend.flipX = transform.position.x > destination.x;
        }
    }

    private void FixedUpdate()
    {
        if (runningPath &! GameManager.failed)
        {
            transform.position += (destination - transform.position).normalized * Time.fixedDeltaTime * currentSpeed * 2;

            if(Vector3.Distance(transform.position, destination) < minDistanceToNextNode)
            {
                SetNextWaypoint();
            }
        }
    }
}
