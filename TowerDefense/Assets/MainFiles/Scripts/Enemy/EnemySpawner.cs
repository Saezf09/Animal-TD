using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


/// Orchestrates the procedural generation and deployment of enemy waves.
/// Utilizes a dynamic budget-allocation algorithm to plan, sort, and spawn clusters of enemies.

public class EnemySpawner : MonoBehaviour
{
    
    // DEPENDENCIES & REFERENCES
    
    [Header("References")]
    [SerializeField] private MapGenerator mapGen;
    [SerializeField] private List<EnemyData> allowedEnemyTypes = new List<EnemyData>();

    
    // USER INTERFACE
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;

    
    // WAVE ECONOMY & TIMING
    
    [Header("Wave Budget Settings")]
    [SerializeField] private int totalWaves = 10;
    [SerializeField] private int startingWaveWeight = 10;
    [SerializeField] private int weightIncreasePerWave = 5;
    [SerializeField] private float timeBetweenWaves = 5f;

    
    // SQUAD GENERATION RULES
    
    [Header("Chunk Settings (Squads)")]
    [SerializeField] private int minChunkSize = 1;
    [SerializeField] private int baseMaxChunkSize = 3;
    [SerializeField] private float chunkGrowthPerWave = 0.5f;
    [SerializeField] private float timeBetweenChunks = 2.0f;
    [Tooltip("Health Percentage increase per wave")]
    [SerializeField] private float healthMultiplierScaling = 0.25f;

    
    // SPATIAL CONFIGURATION
    
    [Header("Spacing & Scaling")]
    [SerializeField] private float enemyScaleFactor = 1f;


    
    // STATE TRACKING
    
    private int currentWave = 0;
    private bool isSpawning = false;

    private struct EnemyChunk
    {
        public EnemyData enemyData;
        public int count;
    }

    private void Start()
    {
        if (waveText != null) waveText.text = $"Wave: 0 / {totalWaves}";
        if (timerText != null) timerText.text = "Awaiting Combat Initialization.";
    }

    public void StartSpawningWaves()
    {
        if (isSpawning) return;

        if (mapGen.PathWaypoints.Count == 0 || mapGen.SpawnPoint == null) return;
        if (allowedEnemyTypes.Count == 0) return;

        StartCoroutine(SpawnWavesRoutine());
    }

    private IEnumerator SpawnWavesRoutine()
    {
        isSpawning = true;
        currentWave = 0;

        while (currentWave < totalWaves)
        {
            currentWave++;

            int currentWaveBudget = startingWaveWeight + (weightIncreasePerWave * (currentWave - 1));

            if (waveText != null) waveText.text = $"Wave: {currentWave} / {totalWaves}";
            if (timerText != null) timerText.text = "Wave in Progress.";

            List<EnemyChunk> plannedWave = new List<EnemyChunk>();

            // Phase 1: Budget Allocation
            while (currentWaveBudget > 0)
            {
                List<EnemyData> affordableEnemies = new List<EnemyData>();

                foreach (var enemy in allowedEnemyTypes)
                {
                    //  Reads minWaveRequirement from EnemyData 
                    if (enemy.spawnWeight <= currentWaveBudget && currentWave >= enemy.minWaveRequirement)
                    {
                        affordableEnemies.Add(enemy);
                    }
                }

                if (affordableEnemies.Count == 0) break;

                EnemyData chosenEnemy = affordableEnemies[Random.Range(0, affordableEnemies.Count)];

                int maxAffordable = currentWaveBudget / chosenEnemy.spawnWeight;
                int dynamicMaxChunkSize = baseMaxChunkSize + Mathf.FloorToInt(currentWave * chunkGrowthPerWave);
                int chunkSize = Random.Range(minChunkSize, Mathf.Min(maxAffordable, dynamicMaxChunkSize) + 1);

                currentWaveBudget -= (chunkSize * chosenEnemy.spawnWeight);

                plannedWave.Add(new EnemyChunk { enemyData = chosenEnemy, count = chunkSize });
            }

            // Phase 2: Threat Sorting 
            // sorting Lowest -> Highest means Tanks spawn FIRST, and Swarms spawn LAST! 
            plannedWave.Sort((chunkA, chunkB) => chunkB.enemyData.maxHealth.CompareTo(chunkA.enemyData.maxHealth));

            // Phase 3: Deployment 
            for (int c = 0; c < plannedWave.Count; c++)
            {
                EnemyChunk chunk = plannedWave[c];
                Debug.Log($"Spawning Squad: {chunk.count}x {chunk.enemyData.enemyName}");

                for (int i = 0; i < chunk.count; i++)
                {
                    SpawnSingleEnemy(chunk.enemyData);

                    // Read the specific delay from the enemy data!
                    // E.g., Dogs wait 0.15s, Cows wait 1.5s
                    yield return new WaitForSeconds(chunk.enemyData.timeToNextSpawn);
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
                    yield return null;
                }
            }
        }

        if (timerText != null) timerText.text = "All Waves Cleared.";
        isSpawning = false;
    }

    private void SpawnSingleEnemy(EnemyData dataToSpawn)
    {
        GameObject newEnemy = Instantiate(dataToSpawn.enemyPrefab, mapGen.SpawnPoint.position, Quaternion.identity);
        newEnemy.transform.localScale = Vector3.one * enemyScaleFactor;

        EnemyMovement movement = newEnemy.GetComponent<EnemyMovement>();

        if (movement != null)
        {
            //  cast it to an int/float depending on health variable type
            movement.currentHealth = dataToSpawn.maxHealth * (1.0f + (healthMultiplierScaling * currentWave));

            movement.Initialize(mapGen.PathWaypoints, dataToSpawn);
        }
    }
}