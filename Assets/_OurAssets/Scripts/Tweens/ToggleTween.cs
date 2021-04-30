using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleTween : MonoBehaviour
{
    public float slideDistance = 10;
    public bool toggled = false;
    Toggle myToggle;
    Vector3 startPosition;

    private void Awake()
    {
        myToggle = GetComponent<Toggle>();
        startPosition = transform.localPosition;
        if (myToggle.isOn)
        {
            transform.localPosition = startPosition + Vector3.right * slideDistance;
        }
    }

    public void OnToggle(bool toggle)
    {
        if (toggle == toggled) return;
        toggled = toggle;
        if (toggle)
        {
            transform.localPosition = startPosition + Vector3.right * slideDistance;
        }
        else
        {
            transform.localPosition = startPosition;
        }
    }
}
