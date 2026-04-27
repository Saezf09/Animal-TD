using UnityEngine;

/// <summary>
/// Orchestrates the high-level flow of the game states.
/// Manages transitions between the Start Menu, the Map Preparation phase, and the Combat phase.
/// </summary>
public class GameManager : MonoBehaviour
{
    // --------------------------------------------------------
    // UI REFERENCES
    // --------------------------------------------------------
    [Header("UI Panels")]
    [Tooltip("The initial menu containing instructions.")]
    [SerializeField] private GameObject startScreenPanel;

    [Tooltip("The UI visible only while configuring the map layout (Regenerate, Start Waves).")]
    [SerializeField] private GameObject mapPrepPanel;

    [Tooltip("The HUD visible during active gameplay (Base Health, Wave Counter, Timer).")]
    [SerializeField] private GameObject combatHUDPanel;

    [Tooltip("The container or text element for the Fur economy.")]
    [SerializeField] private GameObject furWalletUI;

    // --------------------------------------------------------
    // SYSTEM REFERENCES
    // --------------------------------------------------------
    [Header("System References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private EnemySpawner enemySpawner;

    /// <summary>
    /// Initializes the game state. Ensures only the Start Screen is visible, hiding all gameplay UI.
    /// </summary>
    private void Start()
    {
        startScreenPanel.SetActive(true);
        mapPrepPanel.SetActive(false);
        combatHUDPanel.SetActive(false);
        furWalletUI.SetActive(false);
    }

    /// <summary>
    /// Triggered by the "Play" button on the Start Screen.
    /// Hides the menu, reveals the map preparation UI and Fur economy, and generates the initial grid.
    /// </summary>
    public void OnPlayButtonClicked()
    {
        startScreenPanel.SetActive(false);

        mapPrepPanel.SetActive(true);
        furWalletUI.SetActive(true);
        combatHUDPanel.SetActive(false); // Ensure combat stats remain hidden

        if (mapGenerator != null)
        {
            mapGenerator.GenerateInitialGrid();
        }
    }

    /// <summary>
    /// Triggered by the "Regenerate" button during the preparation phase.
    /// Commands the map generator to wipe and rebuild the grid.
    /// </summary>
    public void OnRegenerateMapClicked()
    {
        if (mapGenerator != null)
        {
            mapGenerator.RegenerateMap();
        }
    }

    /// <summary>
    /// Triggered by the "Start Waves" button.
    /// Transitions the game into the combat phase, hiding prep tools and revealing combat metrics.
    /// </summary>
    public void OnStartWavesClicked()
    {
        mapPrepPanel.SetActive(false);
        combatHUDPanel.SetActive(true);

        if (enemySpawner != null)
        {
            enemySpawner.StartSpawningWaves();
        }
    }
}