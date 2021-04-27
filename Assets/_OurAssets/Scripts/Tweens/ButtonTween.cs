using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonTween : MonoBehaviour
{
    public float tweenOffsetTime = 0.0f;
    public float hoverScale = 1.15f;
    public float enableScale = 1.25f;
    public Vector3 startScale = Vector3.one;
    Coroutine enableRoutine;

    // Update is called once per frame
    void OnEnable()
    {

        transform.localScale = Vector3.zero;

        if (enableRoutine != null)
            StopCoroutine(enableRoutine);

        enableRoutine = StartCoroutine(EnableRoutine(startScale));
    }

    IEnumerator EnableRoutine(Vector3 startScale)
    {
        yield return new WaitForSeconds(tweenOffsetTime);
        transform.localScale = startScale;
        LeanTween.scale(gameObject, startScale * enableScale, .5f).setEasePunch();
    }

    public void PlayHoverTween()
    {
        transform.localScale = startScale;
        LeanTween.scale(gameObject, startScale * hoverScale, .5f).setEasePunch();
    }
}
