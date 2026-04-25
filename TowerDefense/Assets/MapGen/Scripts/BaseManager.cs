using UnityEngine;
using TMPro;

public class BaseManager : MonoBehaviour
{
    // --- The Singleton Instance ---
    // This allows ANY script to access BaseManager.Instance from anywhere!
    public static BaseManager Instance { get; private set; }

    [Header("Base Stats")]
    [SerializeField] private int maxHealth = 20;
    private int currentHealth;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI healthText;

    private void Awake()
    {
        // Set up the Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    // Enemies will call this method when they reach the end of the path
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        // Prevent health from going into negative numbers
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            TriggerGameOver();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = $"Base Health: {currentHealth} / {maxHealth}";
        }
    }

    private void TriggerGameOver()
    {
        Debug.Log("GAME OVER! The base was destroyed.");
        if (healthText != null)
        {
            healthText.text = "GAME OVER";
            healthText.color = Color.red;
        }

        // Here you can pause the game, stop the Spawner, or show a Restart screen!
        Time.timeScale = 0f; // Freezes the game
    }
}