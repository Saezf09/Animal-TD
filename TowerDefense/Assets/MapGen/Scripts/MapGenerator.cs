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

    [Header("Prefabs")]
    [SerializeField] private GameObject emptyTile;
    [SerializeField] private GameObject straightPrefab;
    [Tooltip("L-shape connecting Forward(+Z) and Right(+X) at 0 rotation")]
    [SerializeField] private GameObject turnPrefab;
    [SerializeField] private GameObject[] foliageObjects;
    private int curX, curZ;
    private int currentCount = 0;
    private bool forceDirectionChange = false;

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
        int foliageNum = 180;
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                if (tileData[x, z].tileObject != null)
                {
                    if (foliageNum <= 0) { return; }

                    if(Random.Range(0, 100) < 25) 
                    { 
                        CreateTileAt(x, z, foliageObjects[Random.Range(0, foliageObjects.Length - 1)], 0, 0f, 0f); 
                        foliageNum -= 1; 
                    }                    

                    
                }
            }
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

        while (curZ < mapHeight)
        {
            Direction oldDir = curDirection;
            EvaluateMovement();

            GameObject prefabToUse;
            float rotation = 0f;

            if (oldDir == curDirection)
            {
                prefabToUse = straightPrefab;
                rotation = (curDirection == Direction.LEFT || curDirection == Direction.RIGHT) ? 90f : 0f;
            }
            else
            {
                prefabToUse = turnPrefab;
                rotation = CalculateTurnRotation(oldDir, curDirection);
            }

            CreateTileAt(curX, curZ, prefabToUse, 1, pathHeightOffset, rotation);
            MoveCursor();
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void EvaluateMovement()
    {
        if (curDirection == Direction.LEFT && curX <= 0) forceDirectionChange = true;
        else if (curDirection == Direction.RIGHT && curX >= mapWidth - 1) forceDirectionChange = true;

        if (currentCount > 3 && (Random.value > 0.7f || forceDirectionChange))
        {
            ChangeDirection();
            currentCount = 0;
            forceDirectionChange = false;
        }
        else { currentCount++; }
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
            if (to == Direction.LEFT) return 180f;
            if (to == Direction.RIGHT) return 90f;
        }
        if (from == Direction.LEFT) return 0f;
        if (from == Direction.RIGHT) return 270f;
        return 0f;
    }

    private void MoveCursor()
    {
        if (curDirection == Direction.DOWN) curZ++;
        else if (curDirection == Direction.LEFT) curX--;
        else if (curDirection == Direction.RIGHT) curX++;
    }
}