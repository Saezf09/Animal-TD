using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{

    [SerializeField] public EnemyType Config;
    [HideInInspector] public float Health;
    [HideInInspector] public int enemyWeight;

    //sets values on prefab for referencing
    private void Start()
    {
        
        Config.SetupNavMeshAgent(GetComponent<NavMeshAgent>());
        enemyWeight = Config.weight;
    }

    public void setHealth(float healthScale)
    {
       Health = Mathf.RoundToInt(Config.Health * healthScale);
    }

    public void DIE()
    {
        Destroy(this.gameObject);
    }
}
