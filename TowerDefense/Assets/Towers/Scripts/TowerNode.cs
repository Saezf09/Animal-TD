using UnityEngine;

/// <summary>
/// Represents a single interactable grid tile within the game world.
/// Manages visual feedback for player interaction and acts as the spatial anchor for tower placement and merging.
/// </summary>
public class TowerNode : MonoBehaviour
{
    // --------------------------------------------------------
    // STATE & TRACKING
    // --------------------------------------------------------
    [Header("State")]
    public GameObject currentTower; // The physical tower object currently occupying this tile.

    public bool isTargeted = false; // Indicates whether an active falling tower has reserved this node.

    // --------------------------------------------------------
    // VISUAL FEEDBACK
    // --------------------------------------------------------
    [Header("Visuals")]
    [SerializeField] private Color hoverColor = Color.gray; // Color applied when the cursor rests on an empty tile.
    [SerializeField] private Color targetedColor = Color.yellow; // Color applied when a tower is actively routing here.

    private Color startColor; // Caches the original material color for reversion.
    private Renderer rend; // Reference to the tile's mesh renderer.

    // --------------------------------------------------------
    // GRID POSITIONING
    // --------------------------------------------------------
    [Header("Placement Settings")]
    public float yOffset = 0.5f; // Vertical adjustment to ensure the tower sits flush on the surface.

    /// <summary>
    /// Initializes renderer references and caches the default material state.
    /// </summary>
    private void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            startColor = rend.material.color;
        }
    }

    /// <summary>
    /// Processes hover visuals. Only applies the hover color if the tile is both empty and unreserved.
    /// </summary>
    private void OnMouseEnter()
    {
        if (currentTower == null && rend != null && !isTargeted)
        {
            rend.material.color = hoverColor;
        }
    }

    /// <summary>
    /// Restores the default color upon cursor exit, provided the tile is not currently reserved by a falling tower.
    /// </summary>
    private void OnMouseExit()
    {
        if (currentTower == null && rend != null && !isTargeted)
        {
            rend.material.color = startColor;
        }
    }

    /// <summary>
    /// Captures direct mouse clicks on the tile collider.
    /// </summary>
    private void OnMouseDown()
    {
        OnNodeClicked();
    }

    /// <summary>
    /// Core interaction logic for the node. Evaluates whether the incoming tower should merge with an 
    /// existing occupant or execute a standard placement on an empty tile.
    /// </summary>
    public void OnNodeClicked()
    {
        // Retrieve the tower currently being steered by the player.
        FallingTower falling = TowerDropManager.Instance?.activeFallingTower;

        // Abort interaction if the manager has no active drops.
        if (falling == null) return;

        // Concurrency safeguard: Reject the click if this tile is already reserved by a different falling tower.
        if (isTargeted && falling.targetNode != this)
        {
            Debug.Log("Tile is already reserved by another falling tower.");
            return;
        }

        // Branch A: The tile is currently occupied. Attempt to initiate a merge sequence.
        if (currentTower != null)
        {
            TowerController existingTower = currentTower.GetComponent<TowerController>();

            // Validate that both towers share the exact same blueprint and upgrade tier.
            if (existingTower != null && falling.myData == existingTower.myData && falling.currentLevel == existingTower.currentLevel)
            {
                // Verify that the blueprint contains a subsequent tier to upgrade into.
                if (falling.currentLevel < falling.myData.tiers.Length - 1)
                {
                    Debug.Log("Merge target locked.");
                    falling.isMerging = true;
                    falling.SetTarget(this);
                    return;
                }
            }

            Debug.Log("Cannot merge. Invalid type, mismatched level, or maximum tier reached.");
            return;
        }

        // Branch B: The tile is completely empty. Execute standard landing protocol.
        falling.isMerging = false;
        falling.SetTarget(this);
    }

    /// <summary>
    /// Toggles the visual state to indicate that a falling tower has locked onto this specific coordinate.
    /// </summary>
    /// <param name="state">True to apply the targeted color, false to revert to default.</param>
    public void SetTargetedState(bool state)
    {
        isTargeted = state;
        if (rend != null)
        {
            rend.material.color = state ? targetedColor : startColor;
        }
    }
}