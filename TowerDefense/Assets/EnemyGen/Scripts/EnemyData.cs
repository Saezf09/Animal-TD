using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Tower Defense/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Visuals")]
    public string enemyName = "Basic Enemy";
    public GameObject enemyPrefab;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3f;
    public float turnSpeed = 10f;

    [Header("Wave Spawning")]
    public int spawnWeight = 1;
    public int minWaveRequirement = 1;

    // --- NEW: How much health the base loses when this enemy reaches it ---
    [Tooltip("How much damage this enemy deals to the player's base")]
    public int baseDamage = 1;
}