using UnityEngine;

//Config file for enemies attacking (dont change)

[CreateAssetMenu(menuName = "Attack Configuration/ Base Config", order = 2)]
public class AttackConfigurationScriptableObject : ScriptableObject
{
    public float AttackDelay = 0.5f;
    public int Damage = 1;
    public float StoppingDistance = 2.5f;
}
