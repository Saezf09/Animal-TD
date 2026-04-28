using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public NavMeshAgent agent;
    [HideInInspector] public GameObject dest;
    public Animator animator;
    private bool isMoving;
    private float speed;
    float stoppingDistance;
    
    //Set enemy destination, animation speed and stopping distance
    void Start()
    {
        dest = GameObject.FindWithTag("Gate"); 
        stoppingDistance = agent.stoppingDistance;
        animator.SetFloat("AnimSpeed", agent.speed/3);
        StartCoroutine(setDestination((float)0.5));
    }

    //Coroutine to set destination every interval period in case of change
    private IEnumerator setDestination(float interval)
    {
        yield return new WaitForSeconds(interval);
        agent.SetDestination(dest.transform.position);
        StartCoroutine(setDestination(interval));
    }
    private void Update()
    {
        float dist = agent.remainingDistance;
        speed = agent.velocity.magnitude / agent.speed;

        if (agent.velocity != Vector3.zero && agent.remainingDistance > stoppingDistance)
        {
            animator.SetFloat("Speed", speed);
            animator.SetBool("isAttacking", false);
            animator.SetBool("isRunning", true);
        }
        else if (agent.velocity == Vector3.zero)
        {
            animator.SetFloat("Speed", 0);
           
        }
        
        else if (dist != Mathf.Infinity && agent.remainingDistance <= stoppingDistance)
        {
            animator.SetBool("isAttacking", true);
            animator.SetBool("isRunning", false);
        }
    }

}
