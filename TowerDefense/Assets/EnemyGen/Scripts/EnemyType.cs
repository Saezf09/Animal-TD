using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Enemy Types/ newType", order = 1)]
public class EnemyType : ScriptableObject
{
    //Enemy Stats
    public int Health = 100;
    public float MoveSpeed = 2f;
    public bool Flying;
    public AttackConfigurationScriptableObject AttackConfig;

    public void SetupNavMeshAgent(NavMeshAgent Agent)
    {
        Agent.speed = MoveSpeed;
        Agent.stoppingDistance = AttackConfig.StoppingDistance;
    }
}
