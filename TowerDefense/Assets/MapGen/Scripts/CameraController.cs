using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float panSpeed = 15f;
    [Tooltip("How high the camera sits above the map.")]
    [SerializeField] private float cameraHeight = 100f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    private Camera cam;

    [Header("References")]
    [SerializeField] private MapGenerator mapGen;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (mapGen == null) mapGen = FindObjectOfType<MapGenerator>();

        transform.rotation = Quaternion.Euler(30f, 45f, 0f);

        if (mapGen != null)
        {
            // Start the camera looking at the exact center of the map
            float centerX = (mapGen.MapWidth * mapGen.TileSize) / 2f;
            float centerZ = (mapGen.MapHeight * mapGen.TileSize) / 2f;

            // Calculate where the physical camera needs to sit to look at the center
            Vector3 centerLookPoint = new Vector3(centerX, 0, centerZ);
            float distanceToGround = cameraHeight / -transform.forward.y;
            transform.position = centerLookPoint - transform.forward * distanceToGround;
        }
    }

    private void LateUpdate()
    {
        // Notice we handle zoom BEFORE movement so the movement bounds are perfectly accurate!
        HandleZoom();
        HandleMovement();
    }

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

        if (mapGen != null && cam != null && cam.orthographic)
        {
            float distanceToGround = targetPosition.y / -transform.forward.y;
            Vector3 focalPoint = targetPosition + transform.forward * distanceToGround;

            float paddingX = cam.orthographicSize * cam.aspect;
            float paddingZ = cam.orthographicSize;

            // --- NEW: Calculate exactly half of the border's physical thickness ---
            float halfBorder = (mapGen.BorderThickness / 2f) * mapGen.TileSize;

            // --- NEW: Apply the halfBorder to restrict the camera deeper inside the tree line ---
            float minX = -halfBorder + paddingX;
            float maxX = (mapGen.MapWidth * mapGen.TileSize) + halfBorder - paddingX;

            float minZ = -halfBorder + paddingZ;
            float maxZ = (mapGen.MapHeight * mapGen.TileSize) + halfBorder - paddingZ;

            if (maxX < minX) maxX = minX = (minX + maxX) / 2f;
            if (maxZ < minZ) maxZ = minZ = (minZ + maxZ) / 2f;

            focalPoint.x = Mathf.Clamp(focalPoint.x, minX, maxX);
            focalPoint.z = Mathf.Clamp(focalPoint.z, minZ, maxZ);

            targetPosition = focalPoint - transform.forward * distanceToGround;
        }

        transform.position = targetPosition;
    }
}