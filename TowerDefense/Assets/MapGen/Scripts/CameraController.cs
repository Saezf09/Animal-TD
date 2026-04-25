using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float panSpeed = 15f;
    [Tooltip("How high the camera sits above the map. Does not affect zoom in Orthographic mode.")]
    [SerializeField] private float cameraHeight = 200f;

    // --- NEW: Zoom Settings ---
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    private Camera cam; // Reference to the camera component

    [Header("References")]
    [Tooltip("Drag your MapGenerator object here. If left blank, it will find it automatically.")]
    [SerializeField] private MapGenerator mapGen;

    private void Start()
    {
        // --- NEW: Get the camera component ---
        cam = GetComponent<Camera>();

        if (mapGen == null)
            mapGen = FindObjectOfType<MapGenerator>();

        transform.rotation = Quaternion.Euler(30f, 45f, 0f);

        if (mapGen != null)
        {
            float centerX = (mapGen.MapWidth * mapGen.TileSize) / 2f;
            float centerZ = (mapGen.MapHeight * mapGen.TileSize) / 2f;
            transform.position = new Vector3(centerX, cameraHeight, centerZ);
        }
    }

    private void LateUpdate()
    {
        HandleMovement();
        HandleZoom(); // --- NEW: Call the zoom function ---
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        Vector3 moveDir = (forward * z + right * x).normalized;
        Vector3 targetPosition = transform.position + (moveDir * panSpeed * Time.deltaTime);

        if (mapGen != null)
        {
            float minX = -mapGen.BorderThickness * mapGen.TileSize;
            float maxX = (mapGen.MapWidth + mapGen.BorderThickness) * mapGen.TileSize;

            float minZ = -mapGen.BorderThickness * mapGen.TileSize;
            float maxZ = (mapGen.MapHeight + mapGen.BorderThickness) * mapGen.TileSize;

            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
        }

        transform.position = targetPosition;
    }

    // --- NEW: The Zoom Logic ---
    private void HandleZoom()
    {
        // Only run this if we actually have a Camera component and it is Orthographic
        if (cam != null && cam.orthographic)
        {
            // Read the scroll wheel input
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll != 0f)
            {
                // Subtract the scroll value (scrolling up is positive, which should make size smaller to zoom in)
                cam.orthographicSize -= scroll * zoomSpeed;

                // Clamp the size so the player can't zoom in to microscopic levels or zoom out into space
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
        }
    }
}