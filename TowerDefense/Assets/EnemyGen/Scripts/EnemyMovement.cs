using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private EnemyData myData;
    private float currentHealth;

    private List<Vector3> waypoints;
    private int currentTargetIndex = 0;

    public void Initialize(List<Vector3> pathList, EnemyData data)
    {
        waypoints = pathList;
        currentTargetIndex = 0;

        myData = data;
        currentHealth = myData.maxHealth;

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
        // Later on, can add code here to spawn particle effects, play a death sound,
        // or add money before destroying the object!       
        Destroy(gameObject);
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