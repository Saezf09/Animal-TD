using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int mapWidth = 10;
    [SerializeField] private int mapHeight = 10;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float pathHeightOffset = 0.1f;

    [SerializeField] private int minPathLength;
    [SerializeField] private int maxPathLength;

    [SerializeField] private int borderThickness = 2;

    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public float TileSize => tileSize;
    public int BorderThickness => borderThickness;   

    private int totalPathLength = 0;

    [Header("Prefabs")]
    [SerializeField] private GameObject emptyTile;
    [SerializeField] private GameObject straightPrefab;
    [Tooltip("L-shape connecting Forward(+Z) and Right(+X) at 0 rotation")]
    [SerializeField] private GameObject turnPrefab;
    [SerializeField] private GameObject[] foliageObjects;
    [SerializeField] private int foliageNum;

    [Header("Path Markers & Base")]
    [SerializeField] private GameObject spawnPointPrefab;
    [SerializeField] private GameObject basePrefab;
    [Tooltip("Add 90, 180, 270, etc., to rotate the base if it faces the wrong way")]
    [SerializeField] private float baseRotationModifier = 0f;
    [Tooltip("Automatically spins the base to face the direction the path entered from")]
    [SerializeField] private bool autoRotateBase = true;

    public Transform SpawnPoint { get; private set; }
    public Transform EndPoint { get; private set; }

    private int curX, curZ;
    private int currentCount = 0;

    private enum Direction { LEFT, RIGHT, DOWN, UP };
    private Direction curDirection = Direction.DOWN;

    public struct TileData
    {
        public GameObject tileObject;
        public int tileID;
    }

    private TileData[,] tileData;
    private Coroutine pathCoroutine;

    private Transform borderContainer;

    void Awake()
    {
        borderContainer = new GameObject("Border").transform;
        borderContainer.SetParent(transform);
        GenerateInitialGrid();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) RegenerateMap();
    }

    void GenerateInitialGrid()
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

    void GenerateBorder()
    {
        for (int x = -borderThickness; x < mapWidth + borderThickness; x++)
        {
            for (int z = -borderThickness; z < mapHeight + borderThickness; z++)
            {
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

        for (int i = 0; i < availablePositions.Count; i++)
        {
            Vector2Int temp = availablePositions[i];
            int randomIndex = Random.Range(i, availablePositions.Count);
            availablePositions[i] = availablePositions[randomIndex];
            availablePositions[randomIndex] = temp;
        }

        int count = Mathf.Min(foliageNum, availablePositions.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = availablePositions[i];
            GameObject prefab = foliageObjects[Random.Range(0, foliageObjects.Length)];
            CreateTileAt(pos.x, pos.y, prefab, 0, 0f, Random.Range(0, 4) * 90f);
        }
    }

    void RegenerateMap()
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

    void CreateTileAt(int x, int z, GameObject prefab, int id, float yOffset, float yRotation)
    {
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

    IEnumerator GeneratePath()
    {
        curX = Random.Range(0, mapWidth);
        curZ = 0;
        curDirection = Direction.DOWN;
        currentCount = 0;
        totalPathLength = 0;

        float startX = (curX * tileSize) + (tileSize * 0.5f);
        float startZ = (curZ * tileSize) + (tileSize * 0.5f);
        if (spawnPointPrefab != null)
        {
            GameObject sp = Instantiate(spawnPointPrefab, new Vector3(startX, pathHeightOffset + 0.1f, startZ), Quaternion.identity, transform);
            SpawnPoint = sp.transform;
        }

        int lastPathX = curX;
        int lastPathZ = curZ;

        while (curZ < mapHeight && totalPathLength < maxPathLength)
        {
            Direction oldDir = curDirection;
            EvaluateMovement();

            GameObject prefabToUse;
            float rotation = 0f;

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

            lastPathX = curX;
            lastPathZ = curZ;

            CreateTileAt(curX, curZ, prefabToUse, 1, pathHeightOffset, rotation);
            MoveCursor();

            totalPathLength++;
            yield return new WaitForSeconds(0.05f);
        }

        // --- NEW: Dynamically offset the base 2 tiles FORWARD ---
        int baseCenterX = lastPathX;
        int baseCenterZ = lastPathZ;

        if (curDirection == Direction.DOWN) baseCenterZ += 2;
        else if (curDirection == Direction.LEFT) baseCenterX -= 2;
        else if (curDirection == Direction.RIGHT) baseCenterX += 2;

        // Clear the 3x3 footprint
        ClearAreaForBase(baseCenterX, baseCenterZ);

        float endX = (baseCenterX * tileSize) + (tileSize * 0.5f);
        float endZ = (baseCenterZ * tileSize) + (tileSize * 0.5f);

        if (basePrefab != null)
        {
            Vector3 basePos = new Vector3(endX, pathHeightOffset + 0.1f, endZ);

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


    // --- UPDATED: Destroys map tiles AND overlapping border foliage ---
    // --- UPDATED: Clears normal tiles and border trees without needing path protection ---
    private void ClearAreaForBase(int centerX, int centerZ)
    {
        // 1. Clear any normal grid tiles (in case the base generated near an edge)
        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            for (int z = centerZ - 1; z <= centerZ + 1; z++)
            {
                if (x >= 0 && x < mapWidth && z >= 0 && z < mapHeight)
                {
                    if (tileData[x, z].tileObject != null)
                    {
                        Destroy(tileData[x, z].tileObject);
                        tileData[x, z].tileObject = null;
                        tileData[x, z].tileID = -1;
                    }
                }
            }
        }

        // 2. Clear the Border Foliage
        // Calculate the exact physical world boundaries of the 3x3 footprint
        float minX = (centerX - 1) * tileSize;
        float maxX = (centerX + 2) * tileSize;
        float minZ = (centerZ - 1) * tileSize;
        float maxZ = (centerZ + 2) * tileSize;

        // Iterate backwards to safely destroy blocking trees
        for (int i = borderContainer.childCount - 1; i >= 0; i--)
        {
            Transform tree = borderContainer.GetChild(i);

            if (tree.position.x >= minX && tree.position.x <= maxX &&
                tree.position.z >= minZ && tree.position.z <= maxZ)
            {
                Destroy(tree.gameObject);
            }
        }
    }

    private void EvaluateMovement()
    {
        int directStepsToExit = (mapHeight - 1) - curZ;

        bool hittingLeftWall = (curDirection == Direction.LEFT && curX <= 0);
        bool hittingRightWall = (curDirection == Direction.RIGHT && curX >= mapWidth - 1);

        if (hittingLeftWall || hittingRightWall)
        {
            curDirection = Direction.DOWN;
            currentCount = 0;
            return;
        }

        int remainingAllowedSteps = maxPathLength - totalPathLength;

        if (remainingAllowedSteps <= directStepsToExit)
        {
            if (curDirection != Direction.DOWN)
            {
                curDirection = Direction.DOWN;
                currentCount = 0;
            }
            return;
        }

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

    private void ChangeDirection()
    {
        if (curDirection == Direction.DOWN)
        {
            bool canL = curX > 0 && tileData[curX - 1, curZ].tileID == 0;
            bool canR = curX < mapWidth - 1 && tileData[curX + 1, curZ].tileID == 0;
            if (canL && canR) curDirection = Random.value > 0.5f ? Direction.LEFT : Direction.RIGHT;
            else if (canL) curDirection = Direction.LEFT;
            else if (canR) curDirection = Direction.RIGHT;
        }
        else { curDirection = Direction.DOWN; }
    }

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

    private void MoveCursor()
    {
        if (curDirection == Direction.DOWN) curZ++;
        else if (curDirection == Direction.LEFT) curX--;
        else if (curDirection == Direction.RIGHT) curX++;
    }
}