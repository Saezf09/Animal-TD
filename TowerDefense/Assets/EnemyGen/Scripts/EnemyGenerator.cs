using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyGenerator : MonoBehaviour
{   
    //List of enemy prefabs to spawn and spawn position
    public List<GameObject> enemies = new List<GameObject>();
    public GameObject spawnObjectRef;
    private Vector3 spawnPoint;
    
    private Enemy enemyScript;  //Reference to recently spawned enemy Script
    [SerializeField]
    private Adapt_Diff difficultyScript;

    
    //Starts the spawning of enemies with an interval, prefab and starting weight.
    private void Start()
    {
        spawnPoint = spawnObjectRef.transform.position;
        //StartCoroutine(spawnEnemies(1, enemies[Random.Range(0, enemies.Count - 1)], totalWeight));
    }

    //splits weight into segments, and spawns enemies depending on weight
    public void spawnEnemyChunk(int weight,float healthScaler, string name)
    {
        
        StartCoroutine(spawnEnemies(1, name, healthScaler, weight));
        
    }

    //Spawn enemy method until weight has been depleted. 
    private IEnumerator spawnEnemies(float interval, string name, float scaler, int weight)
    {
        //Stops spawning when weight is 0 or less
        if (weight <= 0) { StartCoroutine(difficultyScript.Calculate_Next_Wave_Difficulty()); yield break; }
        
        //interval between spawning
        yield return new WaitForSeconds(interval);



        GameObject newEnemy = SpawnSingleEnemy(name);//Spawns one enemy in the wave

        //Sets temp ref to script, calculates new weight
        enemyScript = newEnemy.GetComponent<Enemy>();
        weight -= enemyScript.Config.weight;

        enemyScript.setHealth(scaler);//sets base health of enemy
        

        print(weight.ToString());
        //print(enemyScript.Config.weight.ToString());
        
        //restarts method with same enemy type          
        StartCoroutine(spawnEnemies(interval, name,scaler, weight));        
    }

    //Pass a string of enemy needed to spawn
    private GameObject SpawnSingleEnemy(string name)
    {
        if (name == "Bull") { GameObject newEnemy = Instantiate(enemies[0], spawnPoint, Quaternion.identity); print("Bull"); return newEnemy; }
        else if (name == "Sheep") { GameObject newEnemy = Instantiate(enemies[1], spawnPoint, Quaternion.identity); print("Sheep"); return newEnemy; }
        else if (name == "Cat") { GameObject newEnemy = Instantiate(enemies[2], spawnPoint, Quaternion.identity); print("Cat"); return newEnemy; }
        else if (name == "Horse") { GameObject newEnemy = Instantiate(enemies[3], spawnPoint, Quaternion.identity); print("Horse"); return newEnemy; }
        else if (name == "Dog") { GameObject newEnemy = Instantiate(enemies[4], spawnPoint, Quaternion.identity); print("Dog"); return newEnemy; }
        else if (name == "Cow") { GameObject newEnemy = Instantiate(enemies[5], spawnPoint, Quaternion.identity); print("Cow"); return newEnemy; }
        else if (name == "Goat") { GameObject newEnemy = Instantiate(enemies[6], spawnPoint, Quaternion.identity); print("Goat"); return newEnemy; }
        else { return null; }
    }

}
