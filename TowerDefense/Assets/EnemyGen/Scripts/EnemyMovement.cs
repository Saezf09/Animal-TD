using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private EnemyData myData;
    private float currentHealth;

    private List<Vector3> waypoints;
    private int currentTargetIndex = 0;

    // --- NEW: Animator Reference ---
    private Animator anim;

    public void Initialize(List<Vector3> pathList, EnemyData data)
    {
        waypoints = pathList;
        currentTargetIndex = 0;

        myData = data;
        currentHealth = myData.maxHealth;

        // --- NEW: Get the animator and sync the speed ---
        anim = GetComponentInChildren<Animator>();

        if (anim != null)
        {
            // Calculate the multiplier. 
            // If the enemy moves at 6 speed, but the animation was built for 3 speed,
            // the multiplier becomes 2.0 (The animation plays twice as fast!)
            float speedMultiplier = myData.moveSpeed / myData.baseAnimationWalkSpeed;
            anim.speed = speedMultiplier;

            // Tell the animator to start walking
            anim.SetBool("IsWalking", true);
        }

        if (waypoints != null && waypoints.Count > 0)
        {
            transform.position = waypoints[0];
        }
    }

    void Update()
    {
        if (waypoints == null || currentTargetIndex >= waypoints.Count || myData == null) return;

        Vector3 targetPosition = waypoints[currentTargetIndex];

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, myData.moveSpeed * Time.deltaTime);

        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, myData.turnSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            currentTargetIndex++;
            if (currentTargetIndex >= waypoints.Count)
            {
                ReachBase();
            }
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Stop the enemy from moving any further while the death animation plays
        myData.moveSpeed = 0f;

        if (anim != null)
        {
            // --- CRITICAL FIX: Reset the animator speed to normal! ---
            // If we don't do this, a fast goblin will play its death animation in fast-forward.
            anim.speed = 1f;

            anim.SetBool("IsWalking", false);
            anim.SetTrigger("Die");

            // Destroy the object after a delay to let the animation finish (e.g., 2 seconds)
            Destroy(gameObject, 2f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ReachBase()
    {
        Debug.Log($"{myData.enemyName} reached the base! Dealing {myData.baseDamage} damage!");

        if (BaseManager.Instance != null)
        {
            BaseManager.Instance.TakeDamage(myData.baseDamage);
        }

        Destroy(gameObject);
    }
}