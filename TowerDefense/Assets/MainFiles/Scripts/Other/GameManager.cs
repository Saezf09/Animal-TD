using UnityEngine;
using UnityEngine.UI; // --- NEW: Required for the Button component ---

public class GameManager : MonoBehaviour
{
    // --- NEW: Singleton Instance ---
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject startScreenPanel;
    [SerializeField] private GameObject mapPrepPanel;
    [SerializeField] private GameObject combatHUDPanel;
    [SerializeField] private GameObject furWalletUI;

    // --- NEW: Direct reference to the Regenerate Button ---
    [Header("UI Controls")]
    [SerializeField] private Button regenerateButton;

    [Header("System References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private EnemySpawner enemySpawner;

    private void Awake()
    {
        // Initialize the Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        startScreenPanel.SetActive(true);
        mapPrepPanel.SetActive(false);
        combatHUDPanel.SetActive(false);
        furWalletUI.SetActive(false);
    }

    public void OnPlayButtonClicked()
    {
        startScreenPanel.SetActive(false);
        mapPrepPanel.SetActive(true);
        furWalletUI.SetActive(true);
        combatHUDPanel.SetActive(false);

        // --- NEW: Ensure the regenerate button is clickable when a new game starts ---
        if (regenerateButton != null) regenerateButton.interactable = true;

        if (mapGenerator != null) mapGenerator.GenerateInitialGrid();
    }

    public void OnRegenerateMapClicked()
    {
        if (mapGenerator != null) mapGenerator.RegenerateMap();
    }

    public void OnStartWavesClicked()
    {
        mapPrepPanel.SetActive(false);
        combatHUDPanel.SetActive(true);

        if (enemySpawner != null) enemySpawner.StartSpawningWaves();
    }

    // --- NEW: Locks the regenerate button so the player can't wipe their towers ---
    public void LockRegeneration()
    {
        if (regenerateButton != null)
        {
            regenerateButton.interactable = false;
        }
    }
}