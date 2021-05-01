using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public GameObject startGameMenu;
    public GameObject gameplayMenu;
    public GameObject cheatsMenu;
    public Vector3[] zoomDestinations;
    public Vector2 boundsX;
    public Vector2 boundsY;
    public Vector2 minMaxZoom;
    public float dragSpeed = 2;
    public float dragSpeedMaxZoom = 15;
    public Rigidbody2D myBody;


    Vector3 dragOrigin;

    float zoom = 10;
    bool dragging = false;
    bool zooming = false;

    Vector3 prev;
    Vector3 vel;

    Vector3 destination;

    private void Start()
    {
        transform.position = new Vector3(20, 20, -10);
        Camera.main.orthographicSize = 25;
        gameplayMenu.SetActive(false);
        cheatsMenu.SetActive(false);
        startGameMenu.SetActive(true);
    }

    public void PlayIntialGameStart()
    {
        MoveToDestination(0);
        gameplayMenu.SetActive(true);
        startGameMenu.SetActive(false);
        GameManager.cheating = false;
        GameManager.singleton.StartNewGame();
    }

    public void PlayIntialGameStartWtihCheats()
    {
        MoveToDestination(0);
        gameplayMenu.SetActive(true);
        startGameMenu.SetActive(false);
        cheatsMenu.SetActive(true);
        GameManager.cheating = true;
        GameManager.singleton.StartNewGame();
    }

    public void MoveToOverview()
    {
        destination = new Vector3(20, 20, -10);
        zoom = 25;
        zooming = true;
        vel = Vector3.zero;
        dragging = false;
    }

    public void MoveToDestination(int destinationID)
    {
        destination = zoomDestinations[destinationID];
        zoom = 10;
        zooming = true;
        dragging = false;
    }

    void Update()
    {
        if(destination != Vector3.zero)
        {
            transform.position = Vector3.Lerp(transform.position, destination, Time.deltaTime * 5);

            if(Vector3.Distance(transform.position, destination) < .1f)
            {
                destination = Vector3.zero;
            }
        }

        if (Input.GetAxis("Mouse ScrollWheel") > 0 && zoom > minMaxZoom.x && destination == Vector3.zero)
        {
            zoom -= 1;
            zooming = true;
        }

        if (Input.GetAxis("Mouse ScrollWheel") < 0 && zoom < minMaxZoom.y && destination == Vector3.zero)
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
        if (destination != Vector3.zero) return;

        if (Input.GetMouseButtonDown(0) && GridInteraction.singleton.currentSelectionMode == GridInteraction.SelectionMode.Select && !EventSystem.current.IsPointerOverGameObject())
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
            float moveLerp = Mathf.Lerp(1, dragSpeedMaxZoom, (zoom + minMaxZoom.x) / (minMaxZoom.y + minMaxZoom.y));

            Vector3 move = new Vector3(movePos.x * dragSpeed * moveLerp, movePos.y * dragSpeed * moveLerp);

            if(transform.position.x - move.x > boundsX.x && transform.position.x - move.x < boundsX.y && transform.position.y - move.y > boundsY.x && transform.position.y - move.y < boundsY.y)
            {
                transform.position -= (move);
            }
            dragOrigin = Input.mousePosition;
        }
    }
}
