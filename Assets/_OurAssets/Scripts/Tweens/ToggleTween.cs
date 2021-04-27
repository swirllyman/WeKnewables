using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleTween : MonoBehaviour
{
    public float slideDistance = 10;
    public bool toggled = false;

    public void OnToggle(bool toggle)
    {
        if (toggle == toggled) return;
        toggled = toggle;
        if (toggle)
        {
            transform.position += Vector3.right * slideDistance;
        }
        else
        {
            transform.position -= Vector3.right * slideDistance;
        }
    }
}
