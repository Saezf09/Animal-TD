using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerShopUI : MonoBehaviour
{
    [Header("Button Mapping")]
    [SerializeField] private Button[] towerButtons;
    [SerializeField] private TextMeshProUGUI[] costTexts;

    //  Inspector Reference 
    [Header("System References")]
    [Tooltip("Drag the object holding your MapGenerator script here.")]
    [SerializeField] private MapGenerator mapGen;

    
    private void Start()
    {

        for (int i = 0; i < towerButtons.Length; i++)
        {
            int buttonIndex = i;
            towerButtons[i].onClick.AddListener(() => OnTowerButtonClicked(buttonIndex));

            if (TowerDropManager.Instance != null && TowerDropManager.Instance.availableTowers.Length > i)
            {
                TowerData data = TowerDropManager.Instance.availableTowers[i];
                if (costTexts.Length > i && costTexts[i] != null)
                {
                    costTexts[i].text = $"{data.dropCost} Fur";
                }
            }
        }
    }

    private void Update()
    {
        if (BaseManager.Instance == null || TowerDropManager.Instance == null) return;

        // Check our locked reference 
        bool isMapReady = (mapGen != null && mapGen.isMapGenerated);

        for (int i = 0; i < towerButtons.Length; i++)
        {
            if (i < TowerDropManager.Instance.availableTowers.Length)
            {
                int cost = TowerDropManager.Instance.availableTowers[i].dropCost;

                // Button requires BOTH enough Fur AND a finished map
                towerButtons[i].interactable = (BaseManager.Instance.currentFur >= cost) && isMapReady;
            }
        }
    }

    private void OnTowerButtonClicked(int index)
    {
        if (TowerDropManager.Instance != null)
        {
            TowerDropManager.Instance.RequestTowerDrop(index);
        }
    }
}