using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Orchestrates the procedural generation and deployment of enemy waves.
/// Utilizes a dynamic budget-allocation algorithm to plan, sort, and spawn clusters of enemies.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // --------------------------------------------------------
    // DEPENDENCIES & REFERENCES
    // --------------------------------------------------------
    [Header("References")]
    [SerializeField] private MapGenerator mapGen; // Reference to the procedural grid to obtain spawn coordinates and waypoints.
    [SerializeField] private List<EnemyData> allowedEnemyTypes = new List<EnemyData>(); // The pool of valid enemy blueprints.

    // --------------------------------------------------------
    // USER INTERFACE
    // --------------------------------------------------------
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI waveText; // Text component indicating the current progression.
    [SerializeField] private TextMeshProUGUI timerText; // Text component displaying phase status and countdowns.

    // --------------------------------------------------------
    // WAVE ECONOMY & TIMING
    // --------------------------------------------------------
    [Header("Wave Budget Settings")]
    [SerializeField] private int totalWaves = 10; // Total number of discrete waves to generate.
    [SerializeField] private int startingWaveWeight = 10; // The base currency available for the algorithm in wave 1.
    [SerializeField] private int weightIncreasePerWave = 5; // Linear increment of the budget per subsequent wave.
    [SerializeField] private float timeBetweenWaves = 5f; // Duration of the interstitial resting phase between waves.

    // --------------------------------------------------------
    // SQUAD GENERATION RULES
    // --------------------------------------------------------
    [Header("Chunk Settings (Squads)")]
    [SerializeField] private int minChunkSize = 1; // Minimum identical enemies spawned consecutively.
    [SerializeField] private int baseMaxChunkSize = 3; // Maximum baseline consecutive identical enemies.
    [SerializeField] private float chunkGrowthPerWave = 0.5f; // Scaling factor to expand maximum squad size over time.
    [SerializeField] private float timeBetweenChunks = 2.0f; // Delay inserted between distinct enemy squads.

    // --------------------------------------------------------
    // SPATIAL CONFIGURATION
    // --------------------------------------------------------
    [Header("Spacing & Scaling")]
    [SerializeField] private float baseDistanceBetweenEnemies = 2.5f; // Standard physical distance between units in a squad.
    [SerializeField] private float enemyScaleFactor = 1f; // Global scale multiplier for instantiated enemy prefabs.

    // --------------------------------------------------------
    // STATE TRACKING
    // --------------------------------------------------------
    private int currentWave = 0; // The active wave index.
    private bool isSpawning = false; // Mutex flag preventing concurrent wave initialization.

    /// <summary>
    /// A localized data structure representing a planned cluster of identical enemies.
    /// Used to cache generation intent before physical instantiation.
    /// </summary>
    private struct EnemyChunk
    {
        public EnemyData enemyData;
        public int count;
    }

    /// <summary>
    /// Initializes default interface text values upon script awake.
    /// </summary>
    private void Start()
    {
        if (waveText != null) waveText.text = $"Wave: 0 / {totalWaves}";
        if (timerText != null) timerText.text = "Awaiting Combat Initialization.";
    }

    /// <summary>
    /// Initiates the wave generation sequence. Called externally by the GameManager.
    /// </summary>
    public void StartSpawningWaves()
    {
        if (isSpawning) return;

        if (mapGen.PathWaypoints.Count == 0 || mapGen.SpawnPoint == null) return;
        if (allowedEnemyTypes.Count == 0) return;

        StartCoroutine(SpawnWavesRoutine());
    }

    /// <summary>
    /// The primary sequential logic for wave generation. Manages budget allocation, entity sorting, 
    /// physical deployment, and interstitial cooldowns.
    /// </summary>
    private IEnumerator SpawnWavesRoutine()
    {
        isSpawning = true;
        currentWave = 0;

        while (currentWave < totalWaves)
        {
            currentWave++;

            // Calculate total permitted threat budget for the active sequence.
            int currentWaveBudget = startingWaveWeight + (weightIncreasePerWave * (currentWave - 1));

            if (waveText != null) waveText.text = $"Wave: {currentWave} / {totalWaves}";
            if (timerText != null) timerText.text = "Wave in Progress.";

            // Initialize a list to stage the generated squads prior to instantiation.
            List<EnemyChunk> plannedWave = new List<EnemyChunk>();

            // Phase 1: Budget Allocation (Determine entity composition until resources are depleted).
            while (currentWaveBudget > 0)
            {
                List<EnemyData> affordableEnemies = new List<EnemyData>();

                // Filter the global pool for entities that satisfy current economic and chronological constraints.
                foreach (var enemy in allowedEnemyTypes)
                {
                    if (enemy.spawnWeight <= currentWaveBudget && currentWave >= enemy.minWaveRequirement)
                    {
                        affordableEnemies.Add(enemy);
                    }
                }

                // Terminate allocation if no valid options remain.
                if (affordableEnemies.Count == 0) break;

                // Select a randomized viable entity profile.
                EnemyData chosenEnemy = affordableEnemies[Random.Range(0, affordableEnemies.Count)];

                // Calculate local batch constraints.
                int maxAffordable = currentWaveBudget / chosenEnemy.spawnWeight;
                int dynamicMaxChunkSize = baseMaxChunkSize + Mathf.FloorToInt(currentWave * chunkGrowthPerWave);
                int chunkSize = Random.Range(minChunkSize, Mathf.Min(maxAffordable, dynamicMaxChunkSize) + 1);

                // Deduct the aggregate cost from the current wave budget.
                currentWaveBudget -= (chunkSize * chosenEnemy.spawnWeight);

                // Cache the planned entity cluster.
                plannedWave.Add(new EnemyChunk { enemyData = chosenEnemy, count = chunkSize });
            }

            // Phase 2: Threat Sorting (Organize the deployment sequence from weakest to strongest).
            plannedWave.Sort((chunkA, chunkB) => chunkA.enemyData.spawnWeight.CompareTo(chunkB.enemyData.spawnWeight));

            // Phase 3: Deployment (Execute the physical instantiation based on the sorted plan).
            for (int c = 0; c < plannedWave.Count; c++)
            {
                EnemyChunk chunk = plannedWave[c];
                Debug.Log($"Spawning Squad: {chunk.count}x {chunk.enemyData.enemyName}");

                for (int i = 0; i < chunk.count; i++)
                {
                    SpawnSingleEnemy(chunk.enemyData);

                    // Calculate the necessary chronological delay based on the entity's traversal velocity.
                    float scaledDistance = baseDistanceBetweenEnemies * enemyScaleFactor;
                    float waitTime = scaledDistance / Mathf.Max(0.1f, chunk.enemyData.moveSpeed);
                    yield return new WaitForSeconds(waitTime);
                }

                // Append the squad interlude delay, excluding the final iteration.
                if (c < plannedWave.Count - 1)
                {
                    yield return new WaitForSeconds(timeBetweenChunks);
                }
            }

            // Phase 4: Interstitial Cooldown.
            if (currentWave < totalWaves)
            {
                float countdown = timeBetweenWaves;
                while (countdown > 0)
                {
                    if (timerText != null) timerText.text = $"Next Wave In: {Mathf.CeilToInt(countdown)}s";
                    countdown -= Time.deltaTime;
                    yield return null; // Yield execution to the engine for one frame.
                }
            }
        }

        // Finalize state upon exhaustion of the configured wave limit.
        if (timerText != null) timerText.text = "All Waves Cleared.";
        isSpawning = false;
    }

    /// <summary>
    /// Instantiates a given entity blueprint at the map origin and applies initialization parameters.
    /// </summary>
    /// <param name="dataToSpawn">The ScriptableObject defining the enemy attributes.</param>
    private void SpawnSingleEnemy(EnemyData dataToSpawn)
    {
        GameObject newEnemy = Instantiate(dataToSpawn.enemyPrefab, mapGen.SpawnPoint.position, Quaternion.identity);
        newEnemy.transform.localScale = Vector3.one * enemyScaleFactor;

        EnemyMovement movement = newEnemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            // Inject the calculated pathing coordinates and behavior definitions.
            movement.Initialize(mapGen.PathWaypoints, dataToSpawn);
        }
    }
}