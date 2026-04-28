using UnityEngine;
using UnityEngine.EventSystems;


/// Handles the falling mechanics for towers dropping from the sky.
/// Also manages the merging logic if it lands on a tower of the same level and type.

public class FallingTower : MonoBehaviour
{
    
    // TOWER DATA & IDENTITY
    
    [Header("Identity")]
    public TowerData myData; // Reference to the ScriptableObject containing stats
    public int currentLevel = 0; // Tracks current upgrade tier (0-indexed)

    [HideInInspector]
    public bool isMerging = false; // Flag set by TowerNode if a valid merge target is found

    
    // MOVEMENT SETTINGS
    
    [Header("Fall Settings")]
    [SerializeField] private float fallSpeed = 5f; // Vertical gravity speed
    [SerializeField] private float horizontalSpeed = 15f; // How fast it slides to the target node

    
    // STATE TRACKING
    
    public TowerNode targetNode; // The tile we are currently aiming for
    private bool isLanded = false; // Prevents Update logic from running after touchdown

    [Header("Highlight Colors")]
    public Color activeColor;   // Cyan
    public Color inactiveColor; // Orange
    private TowerNode currentlyHighlightedNode;

    //  Forcefully override the Unity Inspector's cached memory 
    private void Awake()
    {
        activeColor = new Color(0f, 1f, 1f, 1f);     // Solid Cyan
        inactiveColor = new Color(1f, 0.5f, 0f, 1f); // Solid Orange
    }

    
    /// Assigns a new landing zone and updates the visual highlights on the tiles.
    
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
            Vector3 targetPos = targetNode.transform.position;
            targetPos.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, horizontalSpeed * Time.deltaTime);

            if (transform.position.y <= targetNode.transform.position.y + targetNode.yOffset)
            {
                Land(); // Tower landed on tile
                return;
            }
        }
        else
        {
            // 3. Penalty Logic
            if (transform.position.y <= 0)
            {
                if (TowerDropManager.Instance.activeFallingTower == this)
                {
                    TowerDropManager.Instance.activeFallingTower = null;
                }
                gameObject.SetActive(false);
                Destroy(gameObject);

                return;
            }
        }

        UpdateHighlight();

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    TowerDropManager.Instance.activeFallingTower = this;
                    return;
                }
            }
        }
    }

    private void Land()
    {
        isLanded = true;
        targetNode.isTargeted = false;

        if (TowerDropManager.Instance != null && TowerDropManager.Instance.activeFallingTower == this)
        {
            TowerDropManager.Instance.activeFallingTower = null;
        }

        if (currentlyHighlightedNode != null)
        {
            currentlyHighlightedNode.SetHighlight(false);
            currentlyHighlightedNode = null;
        }

        // Branch A: Merging
        if (isMerging && targetNode.currentTower != null)
        {
            int upgradedLevel = currentLevel + 1;

            targetNode.currentTower.SetActive(false);
            Destroy(targetNode.currentTower);

            GameObject upgradedPrefab = myData.tiers[upgradedLevel].towerPrefab;
            Vector3 spawnPos = targetNode.transform.position + new Vector3(0, targetNode.yOffset, 0);
            GameObject newUpgradedTower = Instantiate(upgradedPrefab, spawnPos, Quaternion.identity);

            FallingTower newFallingScript = newUpgradedTower.GetComponent<FallingTower>();
            if (newFallingScript != null)
            {
                newFallingScript.enabled = false;
            }

            TowerController newController = newUpgradedTower.GetComponent<TowerController>();
            if (newController != null)
            {
                newController.myData = myData;
                newController.currentLevel = upgradedLevel;
                newController.myNode = targetNode;
                newController.ActivateTower();
            }

            targetNode.currentTower = newUpgradedTower;

            gameObject.SetActive(false);
            Destroy(gameObject);

            Debug.Log("MERGE SUCCESS! Tower upgraded to Level " + (upgradedLevel + 1));
        }
        // Branch B: Standard placement
        else
        {
            transform.position = targetNode.transform.position + new Vector3(0, targetNode.yOffset, 0);
            targetNode.currentTower = this.gameObject;

            TowerController myController = GetComponent<TowerController>();
            if (myController != null)
            {
                myController.myData = myData;
                myController.currentLevel = currentLevel;
                myController.myNode = targetNode;
                myController.ActivateTower();
            }
        }
    }

    // Change tile colour depending on active or inactive
    private void UpdateHighlight()
    {
        bool isActive = (TowerDropManager.Instance != null && TowerDropManager.Instance.activeFallingTower == this);

        Color desiredColor = isActive ? activeColor : inactiveColor;

        if (targetNode != null)
        {
            if (currentlyHighlightedNode != targetNode)
            {
                if (currentlyHighlightedNode != null) currentlyHighlightedNode.SetHighlight(false);

                targetNode.SetHighlight(true, desiredColor);
                currentlyHighlightedNode = targetNode;
            }
            else
            {
                currentlyHighlightedNode.SetHighlight(true, desiredColor);
            }
        }
    }
}