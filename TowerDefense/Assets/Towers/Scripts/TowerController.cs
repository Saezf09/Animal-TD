using UnityEngine;

/// <summary>
/// Controls the combat logic and visual representation of a stationary tower.
/// Handles target acquisition, firing mechanics, and dynamic range visualization.
/// </summary>
public class TowerController : MonoBehaviour
{
    // --------------------------------------------------------
    // TOWER DATA & STATE
    // --------------------------------------------------------
    [Header("Data")]
    public TowerData myData; // Reference to the ScriptableObject containing upgrade tiers and stats.
    public int currentLevel = 0; // The current upgrade index for accessing tier data.

    [HideInInspector]
    public TowerNode myNode; // The grid tile this tower is anchored to.

    [HideInInspector]
    public bool isPlaced = false; // Prevents the tower from firing or updating while falling.

    // --------------------------------------------------------
    // VISUAL REFERENCES & TIMERS
    // --------------------------------------------------------
    [Header("Visuals")]
    public Transform firePoint; // The transform position where projectiles or lasers originate.
    public LineRenderer laserVisual; // The line renderer used to draw the attack beam.

    private float fireCountdown = 0f; // Tracks the cooldown time between shots.
    private EnemyMovement currentTarget; // The enemy currently locked on by the tower.
    private LineRenderer rangeVisual; // A dynamically generated circle indicating attack radius.

    /// <summary>
    /// Initializes the tower's active state. Called externally once the tower has safely landed.
    /// </summary>
    public void ActivateTower()
    {
        // Ensure data is valid before attempting to read tier statistics.
        if (myData != null && myData.tiers.Length > currentLevel)
        {
            SetupRangeVisual();
            isPlaced = true; // Enable combat and update logic.
        }
    }

    /// <summary>
    /// Programmatically generates a LineRenderer to display a smooth circle representing the tower's attack range.
    /// </summary>
    private void SetupRangeVisual()
    {
        // Create a child object to hold the visual component.
        GameObject rangeObj = new GameObject("RangeVisual");
        rangeObj.transform.SetParent(transform);
        rangeObj.transform.localPosition = new Vector3(0, 0.2f, 0); // Offset slightly on the Y axis to prevent ground clipping.

        rangeVisual = rangeObj.AddComponent<LineRenderer>();
        rangeVisual.useWorldSpace = false; // Keep the circle relative to the tower's position.
        rangeVisual.loop = true; // Connect the end of the line back to the beginning.

        // 50 segments provide a sufficiently smooth circle without heavy performance costs.
        int segments = 50;
        rangeVisual.positionCount = segments;
        rangeVisual.startWidth = 0.1f;
        rangeVisual.endWidth = 0.1f;

        // Apply a basic transparent grey material.
        rangeVisual.material = new Material(Shader.Find("Sprites/Default"));
        Color transparentGrey = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        rangeVisual.startColor = transparentGrey;
        rangeVisual.endColor = transparentGrey;

        // Retrieve the specific range value for the current upgrade tier.
        float range = myData.tiers[currentLevel].attackRange;

        // Calculate the local coordinates for each point along the circumference using trigonometry.
        float angle = 0f;
        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * range;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * range;
            rangeVisual.SetPosition(i, new Vector3(x, 0, z));
            angle += (360f / segments);
        }

        rangeVisual.enabled = false; // Hide by default until the user hovers over the tower.
    }

    // Toggle the range visualization based on mouse interaction, provided the tower is fully placed.
    private void OnMouseEnter() { if (rangeVisual != null && isPlaced) rangeVisual.enabled = true; }
    private void OnMouseExit() { if (rangeVisual != null) rangeVisual.enabled = false; }

    /// <summary>
    /// Delegates mouse click events down to the underlying grid node to facilitate merging logic.
    /// </summary>
    private void OnMouseDown()
    {
        if (myNode != null) myNode.OnNodeClicked();
    }

    private void Update()
    {
        // Halt execution if the tower is still falling or missing critical data.
        if (!isPlaced || myData == null || myData.tiers.Length == 0) return;

        UpdateTarget();

        // Decrement the firing cooldown based on elapsed frame time.
        fireCountdown -= Time.deltaTime;

        // If an enemy is within range and the weapon is off cooldown, execute an attack.
        if (currentTarget != null && fireCountdown <= 0f)
        {
            Shoot();
            // Reset the cooldown timer using the inverse of the fire rate.
            fireCountdown = 1f / myData.tiers[currentLevel].fireRate;
        }

        // Handle the gradual fading of the laser visual effect.
        if (laserVisual != null && laserVisual.enabled)
        {
            // Linearly interpolate the alpha channel towards clear to simulate beam dissipation.
            laserVisual.startColor = Color.Lerp(laserVisual.startColor, Color.clear, Time.deltaTime * 4f);
            laserVisual.endColor = Color.Lerp(laserVisual.endColor, Color.clear, Time.deltaTime * 4f);

            // Disable the renderer entirely once it is sufficiently transparent.
            if (laserVisual.startColor.a <= 0.1f) laserVisual.enabled = false;
        }
    }

    /// <summary>
    /// Scans the surrounding area for enemies and selects the one furthest along the path.
    /// </summary>
    private void UpdateTarget()
    {
        float range = myData.tiers[currentLevel].attackRange;

        // Retrieve all colliders within the tower's current attack radius.
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, range);

        EnemyMovement bestTarget = null;
        float furthestProgress = Mathf.NegativeInfinity;

        // Iterate through all detected objects to find the optimal valid target.
        foreach (Collider col in collidersInRange)
        {
            EnemyMovement enemy = col.GetComponent<EnemyMovement>();
            if (enemy != null)
            {
                // Request the pathing score from the enemy script.
                float enemyProgress = enemy.GetPathProgress();

                // Compare scores to identify the enemy closest to the base.
                if (enemyProgress > furthestProgress)
                {
                    furthestProgress = enemyProgress;
                    bestTarget = enemy;
                }
            }
        }
        currentTarget = bestTarget;
    }

    /// <summary>
    /// Applies damage to the current target and triggers the laser visual effects.
    /// </summary>
    private void Shoot()
    {
        if (currentTarget == null) return;

        // Extract the current tier's stats for cleaner reading
        float dmg = myData.tiers[currentLevel].damage;
        float splash = myData.tiers[currentLevel].splashRadius;

        // --- NEW: Area of Effect (AoE) Logic ---
        if (splash > 0f)
        {
            // Find all physical colliders within the splash radius centered on the target
            Collider[] caughtInBlast = Physics.OverlapSphere(currentTarget.transform.position, splash);

            foreach (Collider col in caughtInBlast)
            {
                EnemyMovement caughtEnemy = col.GetComponent<EnemyMovement>();
                if (caughtEnemy != null)
                {
                    caughtEnemy.TakeDamage(dmg);
                }
            }

            StartCoroutine(ShowExplosionRadius(currentTarget.transform.position, splash));
        }
        else
        {
            // Standard Single-Target Logic (Basic & Mage Towers)
            currentTarget.TakeDamage(dmg);
            
            // --- Visuals ---
            if (laserVisual != null && firePoint != null)
            {
                laserVisual.enabled = true;
                Color brightLaser = new Color(1f, 0.2f, 0.2f, 1f);

                // Optional visual tweak: Make the laser thicker for the Mage tower, 
                // or maybe change the color based on the tower type later!

                laserVisual.startColor = brightLaser;
                laserVisual.endColor = brightLaser;
                laserVisual.SetPosition(0, firePoint.position);
                laserVisual.SetPosition(1, currentTarget.transform.position + new Vector3(0, 0.5f, 0));
            }
        }

        
    }

    /// <summary>
    /// Dynamically generates a primitive sphere to visually represent the AoE blast radius.
    /// Fades the sphere out over a short duration to simulate a shockwave.
    /// </summary>
    private System.Collections.IEnumerator ShowExplosionRadius(Vector3 center, float radius)
    {
        // Generate a basic Unity sphere
        GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosion.transform.position = center;

        // Strip the physical collider so it does not interfere with the game's physics or targeting
        Destroy(explosion.GetComponent<Collider>());

        // A sphere's default scale is 1 (radius 0.5). To make its radius match our splash radius, 
        // we must multiply the splash radius by 2 to get the full diameter.
        explosion.transform.localScale = Vector3.one * (radius * 2f);

        // Apply the same transparent sprite shader we used for the range circle
        Renderer rend = explosion.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Sprites/Default"));
        rend.material = mat;

        // Define a vibrant, semi-transparent orange
        Color explosionColor = new Color(1f, 0.4f, 0f, 0.6f);
        rend.material.color = explosionColor;

        // Fade animation loop
        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Linearly interpolate the alpha channel from 0.6 down to 0
            float alpha = Mathf.Lerp(0.6f, 0f, elapsed / duration);
            rend.material.color = new Color(1f, 0.4f, 0f, alpha);

            // Optional: Slightly expand the sphere as it fades for a shockwave effect
            float expandScale = Mathf.Lerp(radius * 2f, radius * 2.5f, elapsed / duration);
            explosion.transform.localScale = Vector3.one * expandScale;

            yield return null;
        }

        // Clean up the temporary object
        Destroy(explosion);
    }


    /// <summary>
    /// Draws a wireframe sphere in the Unity Editor to help visualize the tower's range during design.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (myData != null && myData.tiers.Length > currentLevel)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, myData.tiers[currentLevel].attackRange);
        }
    }
}