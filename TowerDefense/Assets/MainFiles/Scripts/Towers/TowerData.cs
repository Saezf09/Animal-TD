using UnityEngine;

[System.Serializable]
public struct TowerTier
{
    [Tooltip("The specific 3D prefab for this level of the tower")]
    public GameObject towerPrefab;
    public float damage;
    public float attackRange;
    public float fireRate;

    // --- NEW: Defines how large the explosion is. 0 means single-target. ---
    [Tooltip("Set to 0 for single-target. Set higher than 0 for AoE explosion radius.")]
    public float splashRadius;
}

[CreateAssetMenu(fileName = "New Tower Data", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("Tower Identity")]
    public string towerName = "Basic Tower";

    [Header("Economy")]
    public int dropCost = 10;

    [Header("Audio")]
    public AudioClip attackSound;

    [Header("Upgrade Tiers")]
    public TowerTier[] tiers;
}