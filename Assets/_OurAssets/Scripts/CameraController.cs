using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Vector2 boundsX;
    public Vector2 boundsY;

    public Vector2 minMaxZoom;
    public float dragSpeed = 2;
    public Rigidbody2D myBody;

    Vector3 dragOrigin;

    float zoom = 10;
    bool dragging = false;
    bool zooming = false;

    Vector3 prev;
    Vector3 vel;

    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0 && zoom > minMaxZoom.x)
        {
            zoom -= 1;
            zooming = true;
        }

        if (Input.GetAxis("Mouse ScrollWheel") < 0 && zoom < minMaxZoom.y)
        {
            zoom += 1;
            zooming = true;
        }

        if (zooming)
        {
            Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, zoom, Time.deltaTime * 2);
        }

        vel = (transform.position - prev) / Time.deltaTime;
        prev = transform.position;
    }

    private void FixedUpdate()
    {
        if(transform.position.x < boundsX.x)
        {
            transform.position = new Vector3(boundsX.x, transform.position.y, transform.position.z);
            myBody.velocity = Vector3.zero;
        }

        if (transform.position.x > boundsX.y)
        {
            transform.position = new Vector3(boundsX.y, transform.position.y, transform.position.z);
            myBody.velocity = Vector3.zero;
        }

        if (transform.position.y < boundsY.x)
        {
            transform.position = new Vector3(transform.position.x, boundsY.x, transform.position.z);
            myBody.velocity = Vector3.zero;
        }

        if (transform.position.y > boundsY.y)
        {
            transform.position = new Vector3(transform.position.x, boundsY.y, transform.position.z);
            myBody.velocity = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0) && GridInteraction.singleton.currentSelectionMode == GridInteraction.SelectionMode.Select)
        {
            dragOrigin = Input.mousePosition;
            dragging = true;
            myBody.velocity = Vector3.zero;
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
            myBody.AddForce(vel, ForceMode2D.Impulse);
        }

        if (dragging)
        {
            Vector2 movePos = Input.mousePosition - dragOrigin;
            Vector3 move = new Vector3(movePos.x * dragSpeed, movePos.y * dragSpeed) * Time.deltaTime;

            if(transform.position.x - move.x > boundsX.x && transform.position.x - move.x < boundsX.y && transform.position.y - move.y > boundsY.x && transform.position.y - move.y < boundsY.y)
            {
                transform.position -= (move);
            }
            dragOrigin = Input.mousePosition;
        }
    }
}
