using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{

    [SerializeField]
    private int Health;
    [SerializeField]
    private EnemyType Config;

    private void Start()
    {
        Health = Config.Health;
        Config.SetupNavMeshAgent(GetComponent<NavMeshAgent>());
    }
}
