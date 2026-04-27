using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Handles the procedural generation of the game map, including grid initialization,
/// path routing, waypoint caching, and the placement of environmental assets such as foliage and the player base.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    // --------------------------------------------------------
    // DIMENSIONAL SETTINGS
    // --------------------------------------------------------
    [Header("Map Settings")]
    [SerializeField] private int mapWidth = 10; // The total width of the playable grid.
    [SerializeField] private int mapHeight = 10; // The total height of the playable grid.
    [SerializeField] private float tileSize = 1.0f; // The physical world-space size of a single grid unit.
    [SerializeField] private float pathHeightOffset = 0.1f; // Vertical offset to prevent z-fighting with the ground plane.

    [SerializeField] private int minPathLength; // The minimum required steps for a valid enemy path.
    [SerializeField] private int maxPathLength; // The absolute maximum steps allowed for the enemy path.
    [SerializeField] private int borderThickness = 2; // The depth of the decorative perimeter foliage.

    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public float TileSize => tileSize;
    public int BorderThickness => borderThickness;

    private int totalPathLength = 0; // Tracks the current length of the generated path.

    // --------------------------------------------------------
    // ASSET REFERENCES
    // --------------------------------------------------------
    [Header("Prefabs")]
    [SerializeField] private GameObject emptyTile; // The default ground tile.
    [SerializeField] private GameObject straightPrefab; // The linear path segment.
    [Tooltip("L-shape connecting Forward(+Z) and Right(+X) at 0 rotation")]
    [SerializeField] private GameObject turnPrefab; // The corner path segment.
    [SerializeField] private GameObject[] foliageObjects; // Array of decorative environmental meshes.
    [SerializeField] private int foliageNum; // The target quantity of randomized foliage to scatter.

    [Header("Path Markers & Base")]
    [SerializeField] private GameObject spawnPointPrefab; // The specific tile indicating the enemy origin.
    [SerializeField] private GameObject basePrefab; // The core objective structure.
    [Tooltip("Add 90, 180, 270, etc., to rotate the base if it faces the incorrect axis")]
    [SerializeField] private float baseRotationModifier = 0f;
    [Tooltip("Automatically aligns the base to face the terminal path direction")]
    [SerializeField] private bool autoRotateBase = true;

    // --------------------------------------------------------
    // EXPOSED STATE VARIABLES
    // --------------------------------------------------------
    public Transform SpawnPoint { get; private set; } // The localized origin for enemy entities.
    public Transform EndPoint { get; private set; } // The target destination for enemy entities.
    public List<Vector3> PathWaypoints { get; private set; } = new List<Vector3>(); // Cached spatial coordinates for navigation.

    // --------------------------------------------------------
    // INTERNAL STATE TRACKING
    // --------------------------------------------------------
    private int curX, curZ; // The current operational coordinates during path generation.
    private int currentCount = 0; // Tracks consecutive steps in a single direction.

    private enum Direction { LEFT, RIGHT, DOWN, UP };
    private Direction curDirection = Direction.DOWN; // The current trajectory of the path generator.

    public struct TileData
    {
        public GameObject tileObject; // The instantiated physical mesh.
        public int tileID; // Numeric identifier for tile type (0 = empty, 1 = path, etc.).
    }

    private TileData[,] tileData; // 2D array caching the state of every grid coordinate.
    private Coroutine pathCoroutine; // Reference to the active generation sequence.
    private Transform borderContainer; // Organizational parent for decorative boundary objects.

    /// <summary>
    /// Initializes internal structures and begins the procedural generation sequence.
    /// </summary>
    void Awake()
    {
        borderContainer = new GameObject("Border").transform;
        borderContainer.SetParent(transform);        
    }

    /// <summary>
    /// Populates the coordinate matrix with default empty tiles and initiates the sequential generation phases.
    /// </summary>
    public void GenerateInitialGrid()
    {
        tileData = new TileData[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                CreateTileAt(x, z, emptyTile, 0, 0f, 0f);
            }
        }
        GenerateFoliage();
        GenerateBorder();
        pathCoroutine = StartCoroutine(GeneratePath());
    }

    /// <summary>
    /// Populates the perimeter of the playable grid with impenetrable decorative objects to define the boundary.
    /// </summary>
    void GenerateBorder()
    {
        for (int x = -borderThickness; x < mapWidth + borderThickness; x++)
        {
            for (int z = -borderThickness; z < mapHeight + borderThickness; z++)
            {
                // Only instantiate assets if the coordinate falls strictly outside the defined playable dimensions.
                if (x < 0 || x >= mapWidth || z < 0 || z >= mapHeight)
                {
                    float xPos = (x * tileSize) + (tileSize * 0.5f);
                    float zPos = (z * tileSize) + (tileSize * 0.5f);
                    Vector3 pos = new Vector3(xPos, 0, zPos);

                    GameObject prefab = foliageObjects[Random.Range(0, foliageObjects.Length)];
                    Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0), borderContainer);
                }
            }
        }
    }

    /// <summary>
    /// Implements a Fisher-Yates shuffle algorithm to randomly distribute foliage across available empty coordinates.
    /// </summary>
    void GenerateFoliage()
    {
        List<Vector2Int> availablePositions = new List<Vector2Int>();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                availablePositions.Add(new Vector2Int(x, z));
            }
        }

        // Shuffle the list to randomize selection order.
        for (int i = 0; i < availablePositions.Count; i++)
        {
            Vector2Int temp = availablePositions[i];
            int randomIndex = Random.Range(i, availablePositions.Count);
            availablePositions[i] = availablePositions[randomIndex];
            availablePositions[randomIndex] = temp;
        }

        // Distribute the requested number of foliage assets, constrained by total available spaces.
        int count = Mathf.Min(foliageNum, availablePositions.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = availablePositions[i];
            GameObject prefab = foliageObjects[Random.Range(0, foliageObjects.Length)];
            CreateTileAt(pos.x, pos.y, prefab, 0, 0f, Random.Range(0, 4) * 90f);
        }
    }

    /// <summary>
    /// Clears all existing procedural data, destroys associated GameObjects, and restarts the generation sequence.
    /// </summary>
    public void RegenerateMap()
    {
        if (pathCoroutine != null) StopCoroutine(pathCoroutine);

        if (SpawnPoint != null) Destroy(SpawnPoint.gameObject);
        if (EndPoint != null) Destroy(EndPoint.gameObject);
        foreach (Transform child in borderContainer) Destroy(child.gameObject);

        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                CreateTileAt(x, z, emptyTile, 0, 0f, 0f);
            }
        }
        GenerateFoliage();
        GenerateBorder();
        pathCoroutine = StartCoroutine(GeneratePath());
    }

    /// <summary>
    /// Instantiates a given prefab at a specific grid coordinate, replacing any pre-existing object at that location.
    /// </summary>
    void CreateTileAt(int x, int z, GameObject prefab, int id, float yOffset, float yRotation)
    {
        // Enforce strict dimensional boundaries.
        if (x < 0 || x >= mapWidth || z < 0 || z >= mapHeight) return;

        if (tileData[x, z].tileObject != null)
        {
            Destroy(tileData[x, z].tileObject);
        }

        float xPos = (x * tileSize) + (tileSize * 0.5f);
        float zPos = (z * tileSize) + (tileSize * 0.5f);

        Vector3 pos = new Vector3(xPos, yOffset, zPos);
        Quaternion rot = Quaternion.Euler(0, yRotation, 0);

        GameObject newTile = Instantiate(prefab, pos, rot, transform);

        tileData[x, z].tileObject = newTile;
        tileData[x, z].tileID = id;
    }

    /// <summary>
    /// Executes a controlled random-walk algorithm to define the enemy traversal path and terminal base location.
    /// </summary>
    IEnumerator GeneratePath()
    {
        // Initialize algorithm starting parameters along the top edge of the matrix.
        curX = Random.Range(0, mapWidth);
        curZ = 0;
        curDirection = Direction.DOWN;
        currentCount = 0;
        totalPathLength = 0;

        PathWaypoints.Clear();

        int lastPathX = curX;
        int lastPathZ = curZ;

        // Iterate until the path reaches the bottom edge or exceeds the maximum length constraint.
        while (curZ < mapHeight && totalPathLength < maxPathLength)
        {
            Direction oldDir = curDirection;
            EvaluateMovement();

            GameObject prefabToUse;
            float rotation = 0f;

            // Determine the appropriate spatial prefab based on the trajectory delta.
            if (oldDir == curDirection)
            {
                prefabToUse = straightPrefab;
                rotation = (curDirection == Direction.LEFT || curDirection == Direction.RIGHT) ? 0f : 90f;
            }
            else
            {
                prefabToUse = turnPrefab;
                rotation = CalculateTurnRotation(oldDir, curDirection);
            }

            // Substitute the default path mesh with the designated origin marker for the initial node.
            if (totalPathLength == 0 && spawnPointPrefab != null)
            {
                prefabToUse = spawnPointPrefab;
                rotation = 0f;
            }

            lastPathX = curX;
            lastPathZ = curZ;

            CreateTileAt(curX, curZ, prefabToUse, 1, pathHeightOffset, rotation);

            // Cache the reference to the localized origin for the enemy spawning sub-system.
            if (totalPathLength == 0)
            {
                SpawnPoint = tileData[curX, curZ].tileObject.transform;
            }

            // Record the mathematical world coordinate for subsequent nav-mesh or waypoint routing.
            float wpX = (curX * tileSize) + (tileSize * 0.5f);
            float wpZ = (curZ * tileSize) + (tileSize * 0.5f);
            PathWaypoints.Add(new Vector3(wpX, pathHeightOffset + 0.5f, wpZ));

            MoveCursor();

            totalPathLength++;
            yield return new WaitForSeconds(0.05f); // Intentional delay for visual sequencing.
        }

        // Establish the final base coordinates, pushing the structure slightly past the final path node.
        int baseCenterX = lastPathX;
        int baseCenterZ = lastPathZ;

        if (curDirection == Direction.DOWN) baseCenterZ += 2;
        else if (curDirection == Direction.LEFT) baseCenterX -= 2;
        else if (curDirection == Direction.RIGHT) baseCenterX += 2;

        ClearAreaForBase(baseCenterX, baseCenterZ);

        float endX = (baseCenterX * tileSize) + (tileSize * 0.5f);
        float endZ = (baseCenterZ * tileSize) + (tileSize * 0.5f);

        if (basePrefab != null)
        {
            Vector3 basePos = new Vector3(endX, pathHeightOffset, endZ);
            float finalRotation = baseRotationModifier;

            if (autoRotateBase)
            {
                if (curDirection == Direction.LEFT) finalRotation += 90f;
                else if (curDirection == Direction.RIGHT) finalRotation -= 90f;
            }

            Quaternion baseRot = Quaternion.Euler(0, finalRotation, 0);

            GameObject playerBase = Instantiate(basePrefab, basePos, baseRot, transform);
            EndPoint = playerBase.transform;
        }
    }

    /// <summary>
    /// Excavates a 3x3 volumetric area to ensure the player base structure does not collide 
    /// with decorative assets or grid remnants.
    /// </summary>
    private void ClearAreaForBase(int centerX, int centerZ)
    {
        // 1. Eradicate any overlapping standard grid elements.
        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            for (int z = centerZ - 1; z <= centerZ + 1; z++)
            {
                if (x >= 0 && x < mapWidth && z >= 0 && z < mapHeight)
                {
                    if (tileData[x, z].tileObject != null)
                    {
                        tileData[x, z].tileObject.SetActive(false);
                        Destroy(tileData[x, z].tileObject);
                        tileData[x, z].tileObject = null;
                        tileData[x, z].tileID = -1;
                    }
                }
            }
        }

        // 2. Eradicate intersecting boundary foliage.
        // Calculate the absolute spatial footprint of the clearing.
        float minX = (centerX - 1) * tileSize;
        float maxX = (centerX + 2) * tileSize;
        float minZ = (centerZ - 1) * tileSize;
        float maxZ = (centerZ + 2) * tileSize;

        // Execute a reverse iteration to safely perform destructive operations on the active list.
        for (int i = borderContainer.childCount - 1; i >= 0; i--)
        {
            Transform tree = borderContainer.GetChild(i);

            if (tree.position.x >= minX && tree.position.x <= maxX &&
                tree.position.z >= minZ && tree.position.z <= maxZ)
            {
                tree.gameObject.SetActive(false);
                Destroy(tree.gameObject);
            }
        }
    }

    /// <summary>
    /// Analyzes the remaining path distance and proximity to grid edges to enforce structural constraints on the random walk.
    /// </summary>
    private void EvaluateMovement()
    {
        int directStepsToExit = (mapHeight - 1) - curZ;

        bool hittingLeftWall = (curDirection == Direction.LEFT && curX <= 0);
        bool hittingRightWall = (curDirection == Direction.RIGHT && curX >= mapWidth - 1);

        // Terminate lateral movement upon boundary intersection.
        if (hittingLeftWall || hittingRightWall)
        {
            curDirection = Direction.DOWN;
            currentCount = 0;
            return;
        }

        int remainingAllowedSteps = maxPathLength - totalPathLength;

        // Force downward progression if maximum length limits are approaching.
        if (remainingAllowedSteps <= directStepsToExit)
        {
            if (curDirection != Direction.DOWN)
            {
                curDirection = Direction.DOWN;
                currentCount = 0;
            }
            return;
        }

        // Force lateral progression if minimum length limits demand it.
        if (curDirection == Direction.DOWN)
        {
            int remainingRequiredSteps = minPathLength - totalPathLength;

            if (remainingRequiredSteps > directStepsToExit)
            {
                ChangeDirection();
                currentCount = 0;
                return;
            }
        }

        // Introduce stochastic turning logic to create organic path structures.
        if (currentCount > 3 && Random.value > 0.7f)
        {
            ChangeDirection();
            currentCount = 0;
        }
        else
        {
            currentCount++;
        }
    }

    /// <summary>
    /// Alters the current traversal vector based on lateral availability within the grid matrix.
    /// </summary>
    private void ChangeDirection()
    {
        if (curDirection == Direction.DOWN)
        {
            // Evaluate lateral viability to prevent recursive loops.
            bool canL = curX > 0 && tileData[curX - 1, curZ].tileID == 0;
            bool canR = curX < mapWidth - 1 && tileData[curX + 1, curZ].tileID == 0;

            if (canL && canR) curDirection = Random.value > 0.5f ? Direction.LEFT : Direction.RIGHT;
            else if (canL) curDirection = Direction.LEFT;
            else if (canR) curDirection = Direction.RIGHT;
        }
        else
        {
            curDirection = Direction.DOWN;
        }
    }

    /// <summary>
    /// Calculates the required rotational quaternion logic for a corner prefab given its entry and exit vectors.
    /// </summary>
    private float CalculateTurnRotation(Direction from, Direction to)
    {
        if (from == Direction.DOWN)
        {
            if (to == Direction.LEFT) return 0f;
            if (to == Direction.RIGHT) return 270f;
        }
        if (from == Direction.LEFT) return 180f;
        if (from == Direction.RIGHT) return 90f;
        return 0f;
    }

    /// <summary>
    /// Updates the internal coordinate tracker according to the active traversal vector.
    /// </summary>
    private void MoveCursor()
    {
        if (curDirection == Direction.DOWN) curZ++;
        else if (curDirection == Direction.LEFT) curX--;
        else if (curDirection == Direction.RIGHT) curX++;
    }
}