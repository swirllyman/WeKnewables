using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubTrigger : MonoBehaviour
{
    public delegate void TriggerCallback(Collider2D collider, bool entered);
    public event TriggerCallback onTrigger;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        onTrigger?.Invoke(collision, true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        onTrigger?.Invoke(collision, false);
    }
}
