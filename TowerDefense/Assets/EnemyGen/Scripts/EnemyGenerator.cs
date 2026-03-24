using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyGenerator : MonoBehaviour
{   
    //List of enemy prefabs to spawn and spawn position
    public List<GameObject> enemies = new List<GameObject>();
    public Vector3 SpawnPoint;
    
    //Difficulty weight scaling
    public int totalWeight = 40;
    
    //Reference to recently spawned enemy Scrtipt
    private Enemy enemyScript;
    
    //Starts the spawning of enemies with an interval, prefab and starting weight.
    private void Start()
    {
        spawnEnemyChunk(totalWeight);
        StartCoroutine(spawnEnemies(1, enemies[Random.Range(0, enemies.Count - 1)], totalWeight));
    }

    //splits weight into segments, and spawns enemies depending on weight
    private void spawnEnemyChunk(int weight)
    {
        
        //StartCoroutine(spawnEnemies(3, enemies[Random.Range(0, enemies.Count - 1)], weight1));

    }

    //Spawn enemy method until weight has been depleted. 
    private IEnumerator spawnEnemies(float interval, GameObject enemy, int weight)
    {
        //Stops spawning when weight is 0 or less
        if (weight <= 0) { yield break; }
        
        //interval between spawning
        yield return new WaitForSeconds(interval);
        
        //Spawns enemy, sets temp ref to script, calculates new weight
        GameObject newEnemy = Instantiate(enemy, SpawnPoint, Quaternion.identity);        
        enemyScript = newEnemy.GetComponent<Enemy>();
        weight -= enemyScript.Config.weight;

        print(weight.ToString());
        //print(enemyScript.Config.weight.ToString());
        
        //restarts method with same enemy type          
        StartCoroutine(spawnEnemies(interval, enemy, weight));        
    }
}
