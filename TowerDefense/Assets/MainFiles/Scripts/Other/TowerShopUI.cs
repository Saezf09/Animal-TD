using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the visual interface for tower purchasing during the combat phase.
/// Automatically binds UI buttons to the deployment manager and evaluates economic affordability.
/// </summary>
public class TowerShopUI : MonoBehaviour
{
    // --------------------------------------------------------
    // UI REFERENCES
    // --------------------------------------------------------
    [Header("Button Mapping")]
    [Tooltip("Drag the buttons here in order: Basic (0), Bomb (1), Mage (2).")]
    [SerializeField] private Button[] towerButtons;

    [Tooltip("Drag the TextMeshPro elements that display the cost for each respective button.")]
    [SerializeField] private TextMeshProUGUI[] costTexts;

    /// <summary>
    /// Establishes button event listeners and populates the initial cost text arrays 
    /// by reading directly from the central TowerDropManager data.
    /// </summary>
    private void Start()
    {
        for (int i = 0; i < towerButtons.Length; i++)
        {
            // Cache the index locally to ensure the delegate captures the correct integer value.
            int buttonIndex = i;

            // Programmatically assign the click event.
            towerButtons[i].onClick.AddListener(() => OnTowerButtonClicked(buttonIndex));

            // Pull the static cost data from the manager and project it onto the UI text.
            if (TowerDropManager.Instance != null && TowerDropManager.Instance.availableTowers.Length > i)
            {
                TowerData data = TowerDropManager.Instance.availableTowers[i];
                if (costTexts.Length > i && costTexts[i] != null)
                {
                    costTexts[i].text = $"{data.towerName}\n{data.dropCost} Fur";
                }
            }
        }
    }

    /// <summary>
    /// Continuously evaluates the player's economy and toggles button interactability 
    /// to prevent invalid purchases.
    /// </summary>
    private void Update()
    {
        if (BaseManager.Instance == null || TowerDropManager.Instance == null) return;

        for (int i = 0; i < towerButtons.Length; i++)
        {
            if (i < TowerDropManager.Instance.availableTowers.Length)
            {
                int cost = TowerDropManager.Instance.availableTowers[i].dropCost;

                // The button becomes unclickable if the current balance falls below the cost threshold.
                towerButtons[i].interactable = BaseManager.Instance.currentFur >= cost;
            }
        }
    }

    /// <summary>
    /// Delegates the purchase request to the central manager when a valid button is pressed.
    /// </summary>
    /// <param name="index">The array index corresponding to the specific tower type.</param>
    private void OnTowerButtonClicked(int index)
    {
        if (TowerDropManager.Instance != null)
        {
            TowerDropManager.Instance.RequestTowerDrop(index);
        }
    }
}