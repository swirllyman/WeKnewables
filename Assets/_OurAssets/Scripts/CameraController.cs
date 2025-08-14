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
    public Color[] backgroundColors;
    public float colorChangeSpeed = 0.1f;
    public float dragSpeed = 2;
    public float dragSpeedMaxZoom = 15;
    public Rigidbody2D myBody;


    Vector3 dragOrigin;

    float zoom = 10;
    bool dragging = false;
    bool zooming = false;

    Vector3 prev;
    Vector3 vel;

    int currentColorIndex = 0;
    float colorLerpTime = 0f;

    Vector3 destination;

    private void Start()
    {
        transform.position = new Vector3(20, 20, -10);
        Camera.main.orthographicSize = 25;
        gameplayMenu.SetActive(false);
        cheatsMenu.SetActive(false);
        startGameMenu.SetActive(true);

        if (backgroundColors != null && backgroundColors.Length > 0)
        {
            Camera.main.backgroundColor = backgroundColors[0];
        }
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
        if (backgroundColors != null && backgroundColors.Length > 1)
        {
            colorLerpTime += Time.deltaTime * colorChangeSpeed;

            int nextColorIndex = (currentColorIndex + 1) % backgroundColors.Length;
            Color startColor = backgroundColors[currentColorIndex];
            Color endColor = backgroundColors[nextColorIndex];

            Camera.main.backgroundColor = Color.Lerp(startColor, endColor, colorLerpTime);

            if (colorLerpTime >= 1f)
            {
                colorLerpTime = 0f;
                currentColorIndex = nextColorIndex;
            }
        }

        if(destination != Vector3.zero)
        {
            // Using this formula for framerate-independent smoothing
            transform.position = Vector3.Lerp(transform.position, destination, 1 - Mathf.Exp(-5 * Time.deltaTime));

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
            // Using this formula for framerate-independent smoothing
            Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, zoom, 1 - Mathf.Exp(-2 * Time.deltaTime));
        }

        if (Time.deltaTime > 0)
        {
            vel = (transform.position - prev) / Time.deltaTime;
        }
        else
        {
            vel = Vector3.zero;
        }
        prev = transform.position;
    }

    private void FixedUpdate()
    {
        if(transform.position.x < boundsX.x)
        {
            transform.position = new Vector3(boundsX.x, transform.position.y, transform.position.z);
            myBody.linearVelocity = Vector3.zero;
        }

        if (transform.position.x > boundsX.y)
        {
            transform.position = new Vector3(boundsX.y, transform.position.y, transform.position.z);
            myBody.linearVelocity = Vector3.zero;
        }

        if (transform.position.y < boundsY.x)
        {
            transform.position = new Vector3(transform.position.x, boundsY.x, transform.position.z);
            myBody.linearVelocity = Vector3.zero;
        }

        if (transform.position.y > boundsY.y)
        {
            transform.position = new Vector3(transform.position.x, boundsY.y, transform.position.z);
            myBody.linearVelocity = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        if (destination != Vector3.zero) return;

        if (Input.GetMouseButtonDown(0) && GridInteraction.singleton.currentSelectionMode == GridInteraction.SelectionMode.Select && !EventSystem.current.IsPointerOverGameObject())
        {
            dragOrigin = Input.mousePosition;
            dragging = true;
            myBody.linearVelocity = Vector3.zero;
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
            if(vel != Vector3.zero)
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
