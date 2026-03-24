using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{

    [SerializeField]
    private int Health;
    [SerializeField]
    public EnemyType Config;
    public int enemyWeight;

    //sets values on prefab for referencing
    private void Start()
    {
        Health = Config.Health;
        Config.SetupNavMeshAgent(GetComponent<NavMeshAgent>());
        enemyWeight = Config.weight;
    }
}
