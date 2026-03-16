using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public NavMeshAgent agent;
    public GameObject dest;
    public Animator animator;
    private bool isMoving;
    private float speed;
    float stoppingDistance;
    
    void Start()
    {
        agent.SetDestination(dest.transform.position);
        stoppingDistance = agent.stoppingDistance;
        animator.SetFloat("AnimSpeed", agent.speed/3);

    }
    private void Update()
    {
        float dist = agent.remainingDistance;
        speed = agent.velocity.magnitude / agent.speed;

        if (agent.velocity != Vector3.zero)
        {
            animator.SetFloat("Speed", speed);
            animator.SetBool("isAttacking", false);
        }
        else if (agent.velocity == Vector3.zero)
        {
            animator.SetFloat("Speed", 0);
           
        }
        
        if (dist != Mathf.Infinity && agent.remainingDistance <= stoppingDistance)
        {
            animator.SetBool("isAttacking", true);
        }
    }

}
