using UnityEngine;

/// <summary>
/// Manages the isometric camera system, including orthographic zooming and boundary-constrained panning.
/// Calculates a dynamic focal point to ensure the viewport remains strictly within the generated map limits.
/// </summary>
public class CameraController : MonoBehaviour
{
    // --------------------------------------------------------
    // MOVEMENT PARAMETERS
    // --------------------------------------------------------
    [Header("Movement Settings")]
    [SerializeField] private float panSpeed = 15f; // The translation speed of the camera across the XZ plane.

    [Tooltip("How high the camera sits above the map.")]
    [SerializeField] private float cameraHeight = 100f; // The fixed Y-axis altitude of the camera body.

    // --------------------------------------------------------
    // ZOOM PARAMETERS
    // --------------------------------------------------------
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 10f; // The rate at which scroll input affects orthographic size.
    [SerializeField] private float minZoom = 5f; // The minimum allowed orthographic size (closest zoom).
    [SerializeField] private float maxZoom = 20f; // The maximum allowed orthographic size (furthest zoom).

    private Camera cam; // Cached reference to the Camera component.

    // --------------------------------------------------------
    // ENVIRONMENT REFERENCES
    // --------------------------------------------------------
    [Header("References")]
    [SerializeField] private MapGenerator mapGen; // Reference to the map generator for reading grid dimensions.

    /// <summary>
    /// Initializes camera references, sets the designated isometric rotation, and positions 
    /// the camera body to focus exactly on the geometric center of the generated grid.
    /// </summary>
    private void Start()
    {
        cam = GetComponent<Camera>();
        if (mapGen == null) mapGen = FindObjectOfType<MapGenerator>();

        // Establish the fixed isometric viewing angle.
        transform.rotation = Quaternion.Euler(30f, 45f, 0f);

        if (mapGen != null)
        {
            // Determine the mathematical center coordinate of the grid.
            float centerX = (mapGen.MapWidth * mapGen.TileSize) / 2f;
            float centerZ = (mapGen.MapHeight * mapGen.TileSize) / 2f;

            // Define the ground-level focal point the camera should look at.
            Vector3 centerLookPoint = new Vector3(centerX, 0, centerZ);

            // Calculate the required hypotenuse distance to push the camera back along its local Z axis 
            // so that it rests at the specified cameraHeight while looking at the center point.
            float distanceToGround = cameraHeight / -transform.forward.y;
            transform.position = centerLookPoint - transform.forward * distanceToGround;
        }
    }

    /// <summary>
    /// Executes camera logic after all primary game logic has concluded for the frame.
    /// Zoom is processed prior to movement to ensure boundary clamping utilizes the updated viewport dimensions.
    /// </summary>
    private void LateUpdate()
    {
        HandleZoom();
        HandleMovement();
    }

    /// <summary>
    /// Modifies the orthographic size of the camera based on user scroll input, constraining 
    /// the value within the predefined minimum and maximum zoom thresholds.
    /// </summary>
    private void HandleZoom()
    {
        if (cam != null && cam.orthographic)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                cam.orthographicSize -= scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
        }
    }

    /// <summary>
    /// Processes keyboard input to translate the camera. Computes a ground-level focal point 
    /// and clamps it within the map boundaries, accounting for the dynamic viewport padding.
    /// </summary>
    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // Flatten the forward vector to ensure movement occurs purely on the XZ plane.
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        // Flatten the right vector similarly.
        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        // Calculate the normalized direction and apply speed to find the provisional target position.
        Vector3 moveDir = (forward * z + right * x).normalized;
        Vector3 targetPosition = transform.position + (moveDir * panSpeed * Time.deltaTime);

        if (mapGen != null && cam != null && cam.orthographic)
        {
            // Project a ray from the camera's provisional position to the ground to find the central focal point.
            float distanceToGround = targetPosition.y / -transform.forward.y;
            Vector3 focalPoint = targetPosition + transform.forward * distanceToGround;

            // Calculate viewport padding dynamically based on the current orthographic size and aspect ratio.
            float paddingX = cam.orthographicSize * cam.aspect;
            float paddingZ = cam.orthographicSize;

            // Determine half of the physical border thickness to allow the camera to pan slightly into the tree line.
            float halfBorder = (mapGen.BorderThickness / 2f) * mapGen.TileSize;

            // Define the strict coordinate boundaries, offset by the viewport padding and half-border.
            float minX = -halfBorder + paddingX;
            float maxX = (mapGen.MapWidth * mapGen.TileSize) + halfBorder - paddingX;

            float minZ = -halfBorder + paddingZ;
            float maxZ = (mapGen.MapHeight * mapGen.TileSize) + halfBorder - paddingZ;

            // Fallback evaluation: if the viewport is physically larger than the map, lock the axis to the midpoint.
            if (maxX < minX) maxX = minX = (minX + maxX) / 2f;
            if (maxZ < minZ) maxZ = minZ = (minZ + maxZ) / 2f;

            // Clamp the ground focal point to ensure it never exceeds the calculated boundaries.
            focalPoint.x = Mathf.Clamp(focalPoint.x, minX, maxX);
            focalPoint.z = Mathf.Clamp(focalPoint.z, minZ, maxZ);

            // Translate the clamped focal point back up the camera's forward vector to determine the final body position.
            targetPosition = focalPoint - transform.forward * distanceToGround;
        }

        transform.position = targetPosition;
    }
}