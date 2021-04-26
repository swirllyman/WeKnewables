using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float projectileSpeed = .5f;
    public GameObject hitEffect;
    public GameObject rendObject;

    GroundUnit target;
    StructurePropertyScriptableObject myProperty;
    //Vector3 startPosition;

    internal void Fire(GroundUnit newTarget, StructurePropertyScriptableObject property, Vector3 startPos)
    {
        target = newTarget;
        myProperty = property;
        //startPosition = startPos;
        transform.position = startPos;
        //LeanTween.rotate(rendObject, new Vector3(0, 0, 360), .25f).setLoopPingPong();
    }

    private void Update()
    {
        if(target != null)
        {
            rendObject.transform.Rotate(new Vector3(0, 0, 720 * Time.deltaTime));
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, Time.deltaTime * projectileSpeed);

            if(Vector3.Distance(transform.position, target.transform.position) < .25f)
            {
                HitTarget();
            }
        }
    }

    //IEnumerator LerpToTarget()
    //{

    //    for(float i = 0; i <= bulletTravelTime; i += Time.deltaTime)
    //    {
    //        float perc = i / bulletTravelTime;
    //        transform.position = Vector3.Lerp(startPosition, target.transform.position, perc);

    //        yield return null;
    //    }

    //    HitTarget();
    //}

    //Override this for different attack types (slow, aoe, etc...)
    protected virtual void HitTarget()
    {
        target.Satisfy(myProperty.GetSatisfaction());

        target = null;
        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
