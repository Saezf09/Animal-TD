using System.Collections.Generic;
using UnityEngine;

public class TowerDropManager : MonoBehaviour
{
    public static TowerDropManager Instance { get; private set; }

    [Header("Drop Settings")]
    [Tooltip("Index 0 = Key 1, Index 1 = Key 2, Index 2 = Key 3")]
    public TowerData[] availableTowers;
    public float spawnHeight = 25f;

    [Header("Spawn Probabilities")]
    [Range(0f, 100f)]
    public float level2SpawnChance = 15f;

    [HideInInspector] public FallingTower activeFallingTower;
    private MapGenerator mapGen;

    public float buyCooldown = 4.0f;
    private float lastBuyTime = -99f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        mapGen = FindObjectOfType<MapGenerator>();
    }

    private void Update()
    {
        // Selective purchasing using the number row 
        if (Input.GetKeyDown(KeyCode.Alpha1)) RequestTowerDrop(0); // Buys the 1st tower
        if (Input.GetKeyDown(KeyCode.Alpha2)) RequestTowerDrop(1); // Buys the 2nd tower
        if (Input.GetKeyDown(KeyCode.Alpha3)) RequestTowerDrop(2); // Buys the 3rd tower
    }

    // accepts an index parameter 
    public void RequestTowerDrop(int towerIndex)
    {
        if (Time.time < lastBuyTime + buyCooldown)
        {
            Debug.Log("Shop is cooling down!");
            return;
        }


        // Failsafe: Ensure they pressed a valid key for the towers we have
        if (availableTowers == null || towerIndex >= availableTowers.Length) return;

        // 1. Pick the SPECIFIC tower data requested by the player
        TowerData chosenData = availableTowers[towerIndex];

        TowerNode[] allNodes = FindObjectsOfType<TowerNode>();
        List<TowerNode> validNodes = new List<TowerNode>();

        foreach (TowerNode node in allNodes)
        {
            if (node.currentTower == null && !node.isTargeted)
            {
                validNodes.Add(node);
            }
        }

        if (validNodes.Count == 0)
        {
            Debug.LogWarning("No safe empty space left to drop a tower.");
            return;
        }

        // 3. Attempt to buy the tower
        if (BaseManager.Instance != null && !BaseManager.Instance.TrySpendFur(chosenData.dropCost))
        {
            return; // Not enough Fur
        }

        lastBuyTime = Time.time;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LockRegeneration();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPurchaseSound();
        }

        // 4. Spawn logic
        int startingLevel = 0;
        if (Random.Range(0f, 100f) <= level2SpawnChance && chosenData.tiers.Length > 1)
        {
            startingLevel = 1;
        }

        GameObject prefabToSpawn = chosenData.tiers[startingLevel].towerPrefab;

        float startX = (mapGen.MapWidth * mapGen.TileSize) / 2f;
        float startZ = (mapGen.MapHeight * mapGen.TileSize) / 2f;
        Vector3 spawnPos = new Vector3(startX, spawnHeight, startZ);

        GameObject newTower = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        activeFallingTower = newTower.GetComponent<FallingTower>();
        activeFallingTower.myData = chosenData;
        activeFallingTower.currentLevel = startingLevel;

        TowerNode randomTarget = validNodes[Random.Range(0, validNodes.Count)];
        activeFallingTower.SetTarget(randomTarget);
    }
}