using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player or object to follow. Automatically set to object tagged 'Player'.")]
    public Transform target;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0, 0, -10f);
    [Tooltip("Speed at which the camera follows the target.")]
    public float followSpeed = 5f;

    [Header("Edge Panning Settings")]
    [Tooltip("Thickness in pixels from screen edge to start panning.")]
    public float panBorderThickness = 10f;
    [Tooltip("Speed at which the camera pans when mouse at screen edge.")]
    public float panSpeed = 5f;
    [Tooltip("Multiplier to amplify pan movement.")]
    public float panMultiplier = 3f;
    private Vector3 panOffset;

    [Header("Zoom Settings")]
    [Tooltip("Scroll wheel zoom speed.")]
    public float zoomSpeed = 10f;
    [Tooltip("Minimum orthographic size or field of view.")]
    public float minZoom = 1f;
    [Tooltip("Maximum orthographic size or field of view.")]
    public float maxZoom = 10f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        HandleEdgePanning();
        HandleZoom();
    }

    private void LateUpdate()
    {
        // If we haven't found the player yet, try again
        if (target == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
            else
                return;    // still no player? bail out this frame
        }

        // Calculate desired position
        Vector3 desiredPosition = target.position + offset + panOffset;
        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Moves the camera offset when mouse is at screen edges.
    /// </summary>
    private void HandleEdgePanning()
    {
        panOffset = Vector3.zero;
        Vector3 mousePos = Input.mousePosition;

        float moveAmount = panSpeed * panMultiplier * Time.deltaTime;
        if (mousePos.x <= panBorderThickness)        // Left edge
            panOffset += Vector3.left * panSpeed * moveAmount;
        else if (mousePos.x >= Screen.width - panBorderThickness) // Right edge
            panOffset += Vector3.right * panSpeed * moveAmount;

        if (mousePos.y <= panBorderThickness)        // Bottom edge
            panOffset += Vector3.down * panSpeed * moveAmount;
        else if (mousePos.y >= Screen.height - panBorderThickness) // Top edge
            panOffset += Vector3.up * panSpeed * moveAmount;
    }

    /// <summary>
    /// Zooms camera in/out using scroll wheel input.
    /// Supports both orthographic and perspective cameras.
    /// </summary>
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        if (cam.orthographic)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
        else
        {
            cam.fieldOfView -= scroll * zoomSpeed;
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minZoom, maxZoom);
        }
    }

    /// <summary>
    /// Allows setting a new follow target at runtime.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
