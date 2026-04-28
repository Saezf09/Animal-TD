using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Represents a single interactable grid tile within the game world.
/// Manages visual feedback for player interaction and acts as the spatial anchor for tower placement and merging.
/// </summary>
public class TowerNode : MonoBehaviour
{
    // STATE & TRACKING
    [Header("State")]
    public GameObject currentTower;
    public bool isTargeted = false;

    // VISUAL FEEDBACK
    [Header("Visuals")]
    [SerializeField] private Color hoverColor = Color.gray;

    private Renderer rend;
    private Color originalColor;

    // GRID POSITIONING
    [Header("Placement Settings")]
    public float yOffset = 0.5f;

    private void Start()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }

    public void SetHighlight(bool isHighlighted, Color colorToSet = default)
    {
        if (rend != null)
        {
            rend.material.color = isHighlighted ? colorToSet : originalColor;
        }
    }

    private void OnMouseEnter()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (currentTower == null && rend != null && !isTargeted)
        {
            rend.material.color = hoverColor;
        }
    }

    private void OnMouseExit()
    {
        if (currentTower == null && rend != null && !isTargeted)
        {
            rend.material.color = originalColor;
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        OnNodeClicked();
    }

    public void OnNodeClicked()
    {
        FallingTower falling = TowerDropManager.Instance?.activeFallingTower;
        if (falling == null) return;

        if (isTargeted && falling.targetNode != this)
        {
            Debug.Log("Tile is already reserved by another falling tower.");
            return;
        }

        if (currentTower != null)
        {
            TowerController existingTower = currentTower.GetComponent<TowerController>();

            if (existingTower != null && falling.myData == existingTower.myData && falling.currentLevel == existingTower.currentLevel)
            {
                if (falling.currentLevel < falling.myData.tiers.Length - 1)
                {
                    Debug.Log("Merge target locked.");
                    falling.isMerging = true;
                    falling.SetTarget(this);
                    return;
                }
            }
            return;
        }

        falling.isMerging = false;
        falling.SetTarget(this);
    }

    public void SetTargetedState(bool state)
    {
        isTargeted = state;

        // --- PATCHED: Removed the material color overwrite here. 
        // We now let FallingTower.UpdateHighlight() handle 100% of the colors!
    }
}