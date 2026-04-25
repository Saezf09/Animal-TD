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
    private int totalPathLength = 0;

    [Header("Prefabs")]
    [SerializeField] private GameObject emptyTile;
    [SerializeField] private GameObject straightPrefab;
    [Tooltip("L-shape connecting Forward(+Z) and Right(+X) at 0 rotation")]
    [SerializeField] private GameObject turnPrefab;
    [SerializeField] private GameObject[] foliageObjects;
    [SerializeField] private int foliageNum;
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

    void Awake()
    {
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
        pathCoroutine = StartCoroutine(GeneratePath());
    }

    void GenerateFoliage()
    {
        
        // 1. Create a list of all potential coordinates
        List<Vector2Int> availablePositions = new List<Vector2Int>();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                availablePositions.Add(new Vector2Int(x, z));
            }
        }

        // 2. Fisher-Yates Shuffle
        for (int i = 0; i < availablePositions.Count; i++)
        {
            Vector2Int temp = availablePositions[i];
            int randomIndex = Random.Range(i, availablePositions.Count);
            availablePositions[i] = availablePositions[randomIndex];
            availablePositions[randomIndex] = temp;
        }

        // 3. Place foliage on the first 'foliageNum' shuffled positions
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
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                CreateTileAt(x, z, emptyTile, 0, 0f, 0f);
            }
        }
        GenerateFoliage();
        pathCoroutine = StartCoroutine(GeneratePath());
    }

    void CreateTileAt(int x, int z, GameObject prefab, int id, float yOffset, float yRotation)
    {
        if (x < 0 || x >= mapWidth || z < 0 || z >= mapHeight) return;

        if (tileData[x, z].tileObject != null)
        {
            Destroy(tileData[x, z].tileObject);
        }

        // ADDED OFFSET: (x * tileSize) + (tileSize * 0.5f)
        // This centers the tile on the grid cell
        float xPos = (x * tileSize) + (tileSize);
        float zPos = (z * tileSize) + (tileSize);

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

        // 1. Reset the length BEFORE the loop starts
        totalPathLength = 0;

        while (curZ < mapHeight && totalPathLength < maxPathLength)
        {
            Direction oldDir = curDirection;
            EvaluateMovement();

            // (Removed the totalPathLength = 0; that was here)

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

            CreateTileAt(curX, curZ, prefabToUse, 1, pathHeightOffset, rotation);
            MoveCursor();

            // 2. Increment the length AFTER placing a tile
            totalPathLength++;

            yield return new WaitForSeconds(0.05f);
        }
    }

    private void EvaluateMovement()
    {
        int directStepsToExit = (mapHeight - 1) - curZ;

        // 1. Boundary Safety Check
        bool hittingLeftWall = (curDirection == Direction.LEFT && curX <= 0);
        bool hittingRightWall = (curDirection == Direction.RIGHT && curX >= mapWidth - 1);

        if (hittingLeftWall || hittingRightWall)
        {
            curDirection = Direction.DOWN; // Explicitly forcing DOWN is safer here
            currentCount = 0;
            return;
        }

        // 2. MAX LENGTH LOGIC: The "Rush Tactic" (Locked)
        int remainingAllowedSteps = maxPathLength - totalPathLength;

        // If we only have exactly enough (or fewer) steps to reach the bottom...
        if (remainingAllowedSteps <= directStepsToExit)
        {
            // Force direction to DOWN if it isn't already
            if (curDirection != Direction.DOWN)
            {
                curDirection = Direction.DOWN;
                currentCount = 0;
            }

            // RETURN EARLY: This is the crucial fix. It skips the stall tactic 
            // and the random turn logic, permanently locking the path downward.
            return;
        }

        // 3. MINIMUM LENGTH LOGIC: The "Stall Tactic"
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

        // 4. Normal random turn logic
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