using UnityEngine;

/// <summary>
/// Handles the falling mechanics for towers dropping from the sky.
/// Also manages the merging logic if it lands on a tower of the same level and type.
/// </summary>
public class FallingTower : MonoBehaviour
{
    // --------------------------------------------------------
    // TOWER DATA & IDENTITY
    // --------------------------------------------------------
    [Header("Identity")]
    public TowerData myData; // Reference to the ScriptableObject containing stats
    public int currentLevel = 0; // Tracks current upgrade tier (0-indexed)

    [HideInInspector]
    public bool isMerging = false; // Flag set by TowerNode if a valid merge target is found

    // --------------------------------------------------------
    // MOVEMENT SETTINGS
    // --------------------------------------------------------
    [Header("Fall Settings")]
    [SerializeField] private float fallSpeed = 5f; // Vertical gravity speed
    [SerializeField] private float horizontalSpeed = 15f; // How fast it slides to the target node

    // --------------------------------------------------------
    // STATE TRACKING
    // --------------------------------------------------------
    public TowerNode targetNode; // The tile we are currently aiming for
    private bool isLanded = false; // Prevents Update logic from running after touchdown

    /// <summary>
    /// Assigns a new landing zone and updates the visual highlights on the tiles.
    /// </summary>
    /// <param name="newNode">The new TowerNode to steer towards.</param>
    public void SetTarget(TowerNode newNode)
    {
        // Turn off highlight on the old node if we change our minds mid-fall
        if (targetNode != null)
        {
            targetNode.SetTargetedState(false);
        }

        targetNode = newNode;

        // Turn on highlight for the new target
        if (targetNode != null)
        {
            targetNode.SetTargetedState(true);
        }
    }

    void Update()
    {
        // Stop calculating fall physics if we already hit the ground
        if (isLanded) return;

        // 1. Apply constant downward force (Gravity)
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        // 2. Handle horizontal steering if the player clicked a valid node
        if (targetNode != null)
        {
            // Create a target position that matches our current Y height so we only lerp X and Z
            Vector3 targetPos = targetNode.transform.position;
            targetPos.y = transform.position.y;

            // Move horizontally towards the tile
            transform.position = Vector3.MoveTowards(transform.position, targetPos, horizontalSpeed * Time.deltaTime);

            // Check collision with the node's placement height
            if (transform.position.y <= targetNode.transform.position.y + targetNode.yOffset)
            {
                Land(); // Touchdown!
            }
        }
        else
        {
            // 3. Penalty Logic: If no node was selected and it hits the ground (y <= 0), destroy it
            if (transform.position.y <= 0)
            {
                // Clear it from the manager so the player can drop a new one
                if (TowerDropManager.Instance.activeFallingTower == this)
                {
                    TowerDropManager.Instance.activeFallingTower = null;
                }
                gameObject.SetActive(false);
                Destroy(gameObject); // Garbage collection / Cleanup
            }
        }
    }

    /// <summary>
    /// Executes the final placement logic. 
    /// Handles either merging into a new upgraded prefab or snapping to the grid normally.
    /// </summary>
    private void Land()
    {
        isLanded = true;
        targetNode.SetTargetedState(false); // Turn off the yellow UI highlight
        TowerDropManager.Instance.activeFallingTower = null; // Free up the manager

        // Branch A: We landed on an identical tower and need to merge
        if (isMerging && targetNode.currentTower != null)
        {
            int upgradedLevel = currentLevel + 1; // Calculate next tier

            // Safely disable and destroy the old tower sitting on the node to prevent Unity Inspector errors
            targetNode.currentTower.SetActive(false);
            Destroy(targetNode.currentTower);

            // Fetch the new prefab from our ScriptableObject and spawn it
            GameObject upgradedPrefab = myData.tiers[upgradedLevel].towerPrefab;
            Vector3 spawnPos = targetNode.transform.position + new Vector3(0, targetNode.yOffset, 0);
            GameObject newUpgradedTower = Instantiate(upgradedPrefab, spawnPos, Quaternion.identity);

            // Hacky fix: The new prefab also has a FallingTower script on it. 
            // We need to disable it immediately so it doesn't think it's falling and destroy itself!
            FallingTower newFallingScript = newUpgradedTower.GetComponent<FallingTower>();
            if (newFallingScript != null)
            {
                newFallingScript.enabled = false;
            }

            // Inject the data and level into the new upgraded tower so it functions correctly
            TowerController newController = newUpgradedTower.GetComponent<TowerController>();
            if (newController != null)
            {
                newController.myData = myData;
                newController.currentLevel = upgradedLevel;
                newController.myNode = targetNode; // Link the tower back to its floor tile

                newController.ActivateTower(); // Boot up the tower's logic
            }

            // Update the node's reference to point to the newly spawned Level 2 tower
            targetNode.currentTower = newUpgradedTower;

            // Destroy this falling tower (the one we steered into the old one)
            gameObject.SetActive(false);
            Destroy(gameObject);

            Debug.Log("MERGE SUCCESS! Tower upgraded to Level " + (upgradedLevel + 1));
        }
        // Branch B: Standard landing on an empty tile
        else
        {
            // Snap perfectly to the grid center using the node's offset
            transform.position = targetNode.transform.position + new Vector3(0, targetNode.yOffset, 0);

            // Occupy the tile
            targetNode.currentTower = this.gameObject;

            // Initialize this tower's combat script
            TowerController myController = GetComponent<TowerController>();
            if (myController != null)
            {
                myController.myData = myData;
                myController.currentLevel = currentLevel;
                myController.myNode = targetNode;

                myController.ActivateTower(); // Start shooting
            }
        }
    }
}