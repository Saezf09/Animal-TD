using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGenerator : MonoBehaviour
{
    public List<GameObject> enemies = new List<GameObject>();

    private void Start()
    {
        Instantiate(enemies[Random.Range(0, enemies.Count - 1)]);
        
    }

}
