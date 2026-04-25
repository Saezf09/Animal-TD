using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapGenerator mapGen;
    [SerializeField] private List<EnemyData> allowedEnemyTypes = new List<EnemyData>();

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Wave Budget Settings")]
    [SerializeField] private int totalWaves = 10;
    [SerializeField] private int startingWaveWeight = 10;
    [SerializeField] private int weightIncreasePerWave = 5;
    [SerializeField] private float timeBetweenWaves = 5f;

    [Header("Chunk Settings (Squads)")]
    [SerializeField] private int minChunkSize = 1;
    [SerializeField] private int baseMaxChunkSize = 3;
    [SerializeField] private float chunkGrowthPerWave = 0.5f;
    [SerializeField] private float timeBetweenChunks = 2.0f;

    [Header("Spacing & Scaling")]
    [SerializeField] private float baseDistanceBetweenEnemies = 2.5f;
    [SerializeField] private float enemyScaleFactor = 1f;

    private int currentWave = 0;
    private bool isSpawning = false;

    // --- NEW: A data structure to hold our "Shopping Cart" items ---
    private struct EnemyChunk
    {
        public EnemyData enemyData;
        public int count;
    }

    private void Start()
    {
        if (mapGen == null) mapGen = FindObjectOfType<MapGenerator>();

        if (waveText != null) waveText.text = $"Wave: 0 / {totalWaves}";
        if (timerText != null) timerText.text = "Press 'Enter' to Start!";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !isSpawning)
        {
            if (mapGen.PathWaypoints.Count == 0 || mapGen.SpawnPoint == null) return;
            if (allowedEnemyTypes.Count == 0) return;

            StartCoroutine(SpawnWavesRoutine());
        }
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
            if (timerText != null) timerText.text = "Wave in Progress...";

            // --- NEW: Create the Shopping Cart for this wave ---
            List<EnemyChunk> plannedWave = new List<EnemyChunk>();

            // Phase 1: Planning (Fill the cart until budget is empty)
            while (currentWaveBudget > 0)
            {
                List<EnemyData> affordableEnemies = new List<EnemyData>();
                foreach (var enemy in allowedEnemyTypes)
                {
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

                // Add this squad to our planned list instead of spawning immediately
                plannedWave.Add(new EnemyChunk { enemyData = chosenEnemy, count = chunkSize });
            }

            // --- NEW: Phase 2: Sorting ---
            // Sort the cart from lowest spawnWeight to highest spawnWeight
            plannedWave.Sort((chunkA, chunkB) => chunkA.enemyData.spawnWeight.CompareTo(chunkB.enemyData.spawnWeight));

            // --- NEW: Phase 3: Spawning ---
            // Now we actually spawn the pre-planned, sorted squads
            for (int c = 0; c < plannedWave.Count; c++)
            {
                EnemyChunk chunk = plannedWave[c];
                Debug.Log($"Spawning Squad: {chunk.count}x {chunk.enemyData.enemyName}");

                for (int i = 0; i < chunk.count; i++)
                {
                    SpawnSingleEnemy(chunk.enemyData);

                    float scaledDistance = baseDistanceBetweenEnemies * enemyScaleFactor;
                    float waitTime = scaledDistance / Mathf.Max(0.1f, chunk.enemyData.moveSpeed);
                    yield return new WaitForSeconds(waitTime);
                }

                // Wait between chunks, but don't wait after the very last chunk is finished
                if (c < plannedWave.Count - 1)
                {
                    yield return new WaitForSeconds(timeBetweenChunks);
                }
            }

            // Phase 4: Wave Cooldown Timer
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

        if (timerText != null) timerText.text = "All Waves Cleared!";
        isSpawning = false;
    }

    private void SpawnSingleEnemy(EnemyData dataToSpawn)
    {
        GameObject newEnemy = Instantiate(dataToSpawn.enemyPrefab, mapGen.SpawnPoint.position, Quaternion.identity);
        newEnemy.transform.localScale = Vector3.one * enemyScaleFactor;

        EnemyMovement movement = newEnemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.Initialize(mapGen.PathWaypoints, dataToSpawn);
        }
    }
}