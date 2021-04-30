using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

public class Projectile : MonoBehaviour
{
    public float projectileSpeed = .5f;
    public GameObject hitEffect;
    public GameObject aoeEffect;
    public GameObject rendObject;


    bool aoe = false;
    bool slow = false;
    bool explodeOnStart = false;

    float aoeRadius = 1;
    float slowAmount = .75f;
    float slowDuration = 2.0f;

    GroundUnit target;
    StructurePropertyScriptableObject myProperty;

    internal void Fire(GroundUnit newTarget, StructurePropertyScriptableObject property, Vector3 startPos, BonusProperties bonusProperties)
    {
        aoe = bonusProperties.AoE;
        aoeRadius = bonusProperties.radius;

        slow = bonusProperties.slow;
        slowAmount = bonusProperties.slowPercent;
        slowDuration = bonusProperties.slowDurationInSeconds;

        explodeOnStart = bonusProperties.emitFromTower;


        target = newTarget;
        myProperty = property;
        transform.position = startPos;
        if(explodeOnStart)
            HitTarget();
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
        else
        {
            Destroy(gameObject);
        }
    }

    //Override this for different attack types (slow, aoe, etc...)
    protected virtual void HitTarget()
    {
        if (aoe)
        {
            List<Collider2D> nearbyColliders = new List<Collider2D>();
            ContactFilter2D contactFilter = new ContactFilter2D();
            if (Physics2D.OverlapCircle(transform.position, aoeRadius, contactFilter, nearbyColliders) > 0)
            {
                for (int i = 0; i < nearbyColliders.Count; i++)
                {
                    if (nearbyColliders[i].CompareTag("GroundUnit"))
                    {
                        GroundUnit unit = nearbyColliders[i].GetComponent<GroundUnit>();
                        unit.Satisfy(myProperty.attackProperties.satisfaction);

                        if (slow)
                        {
                            unit.Slow(slowAmount, slowDuration);
                        }
                    }
                }
            }
        }
        else
        {
            if (slow)
            {
                target.Slow(slowAmount, slowDuration);
            }
            target.Satisfy(myProperty.attackProperties.satisfaction);
        }

        target = null;

        if (aoe)
        {
            if (aoeEffect != null)
                Instantiate(aoeEffect, transform.position, Quaternion.identity).transform.localScale *= aoeRadius;
        }
        else
        {
            if (hitEffect != null)
                Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}

#region Custom Editor
//#if UNITY_EDITOR
//[CustomEditor(typeof(Projectile))]
//public class Projectile_Editor : Editor
//{
//    Projectile thisProjectile;
//    SerializedProperty projectileSpeed;
//    SerializedProperty hitEffect;
//    SerializedProperty rendObject;
//    SerializedProperty aoeEffect;

//    //SerializedProperty slowAmount;

//    private void OnEnable()
//    {
//        thisProjectile = (Projectile)target;
//        projectileSpeed = serializedObject.FindProperty("projectileSpeed");
//        hitEffect = serializedObject.FindProperty("hitEffect");
//        rendObject = serializedObject.FindProperty("rendObject");
//        aoeEffect = serializedObject.FindProperty("aoeEffect");

//    }

//    public override void OnInspectorGUI()
//    {
//        serializedObject.Update();
//        EditorGUILayout.PropertyField(projectileSpeed, true);
//        EditorGUILayout.PropertyField(rendObject);

//        EditorGUILayout.Space();
//        EditorGUILayout.PropertyField(hitEffect);
//        EditorGUILayout.PropertyField(aoeEffect);

//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("AoE", EditorStyles.boldLabel);
//        thisProjectile.aoe = EditorGUILayout.Toggle(new GUIContent("AoE", "Check this for AoE"), thisProjectile.aoe);
//        if (thisProjectile.aoe)
//        {
//            thisProjectile.aoeRadius = EditorGUILayout.FloatField(new GUIContent("AoE Radius", "AoE Radius"), thisProjectile.aoeRadius);
//        }

//        EditorGUILayout.LabelField("Slow", EditorStyles.boldLabel);
//        thisProjectile.slow = EditorGUILayout.Toggle(new GUIContent("Slow", "Check this for Slow"), thisProjectile.slow);
//        if (thisProjectile.slow)
//        {
//            thisProjectile.slowAmount = EditorGUILayout.Slider(thisProjectile.slowAmount, 0.0f, 1.0f);
//            thisProjectile.slowDuration = EditorGUILayout.FloatField(new GUIContent("Slow Duration", "Slow Percent"), thisProjectile.slowDuration);
//        }


//        serializedObject.ApplyModifiedProperties();
//    }
//}
//#endif
#endregion