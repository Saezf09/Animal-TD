using UnityEngine;
using TMPro;

/// <summary>
/// A Singleton manager that oversees the core gameplay state for the player's base.
/// This includes managing the base's health, tracking the player's in-game currency (Fur),
/// and updating the relevant user interface elements.
/// </summary>
public class BaseManager : MonoBehaviour
{
    // --------------------------------------------------------
    // SINGLETON INSTANCE
    // --------------------------------------------------------
    public static BaseManager Instance { get; private set; }

    // --------------------------------------------------------
    // HEALTH METRICS
    // --------------------------------------------------------
    [Header("Base Stats")]
    [SerializeField] private int maxHealth = 20; // The maximum structural integrity of the base.
    private int currentHealth; // The current health value during runtime.

    // --------------------------------------------------------
    // ECONOMY METRICS
    // --------------------------------------------------------
    [Header("Economy")]
    [SerializeField] private int startingFur = 50; // The initial amount of currency provided to the player.
    public int currentFur { get; private set; } // The player's current balance, readable publicly but set privately.

    // --------------------------------------------------------
    // USER INTERFACE REFERENCES
    // --------------------------------------------------------
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI healthText; // Text component displaying current and max health.
    [SerializeField] private TextMeshProUGUI furText; // Text component displaying the current currency balance.

    /// <summary>
    /// Establishes the Singleton instance on script awake.
    /// Destroys any duplicate instances to ensure a single point of access.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initializes the base's health and economy values at the start of the simulation,
    /// and performs an initial UI update.
    /// </summary>
    private void Start()
    {
        currentHealth = maxHealth;
        currentFur = startingFur;
        UpdateUI();
    }

    /// <summary>
    /// Reduces the base's health by the specified amount and evaluates game over conditions.
    /// </summary>
    /// <param name="damageAmount">The integer value of damage inflicted by an enemy.</param>
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        // Prevent health from dropping below zero for UI consistency.
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        UpdateUI();

        if (currentHealth <= 0)
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// Increments the player's currency balance, typically called when an enemy is defeated.
    /// </summary>
    /// <param name="amount">The amount of Fur to add to the balance.</param>
    public void AddFur(int amount)
    {
        currentFur += amount;
        UpdateUI();
    }

    /// <summary>
    /// Attempts to deduct a specified cost from the player's currency balance.
    /// Evaluates whether the transaction is valid based on current funds.
    /// </summary>
    /// <param name="amount">The cost of the intended purchase.</param>
    /// <returns>True if the player has sufficient funds and the transaction succeeds, false otherwise.</returns>
    public bool TrySpendFur(int amount)
    {
        if (currentFur >= amount)
        {
            currentFur -= amount;
            UpdateUI();
            return true;
        }

        Debug.Log("Insufficient Fur for this transaction.");
        return false;
    }

    /// <summary>
    /// Refreshes the on-screen text elements to reflect the most accurate data.
    /// </summary>
    private void UpdateUI()
    {
        if (healthText != null)
        {
            healthText.text = $"Base Health: {currentHealth} / {maxHealth}";
        }

        if (furText != null)
        {
            furText.text = $"Fur: {currentFur}";
        }
    }

    /// <summary>
    /// Handles the end-of-game sequence by freezing time and updating the central UI.
    /// </summary>
    private void TriggerGameOver()
    {
        Debug.Log("Game over. The base structural integrity has been compromised.");

        if (healthText != null)
        {
            healthText.text = "GAME OVER";
            healthText.color = Color.red;
        }

        // Halt all physics and update cycles.
        Time.timeScale = 0f;
    }
}