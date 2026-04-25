using System.Collections;
using TMPro;
using UnityEngine;

public class Adapt_Diff : MonoBehaviour
{
    public Player_Results player;
    [Range(0f, 1f)]
    public float difficulty = 0.5f; //start in middle/ normal
    public float smoothing = 0.3f; //change speed
    public EnemyGenerator enemyGenerator;
    public float weight;
    private float healthScaleMultiplier = 1f;


    public IEnumerator Calculate_Next_Wave_Difficulty()
    {
        yield return new WaitForSeconds(5);
        //float performance = Calculate_Performance(); //player score

        //if (performance < difficulty) //player struggling -- help them
        //{ 
        //    difficulty = Mathf.Lerp(difficulty, performance, smoothing * 1.5f);
        //}
        //else
        //{
        //    difficulty = Mathf.Lerp(difficulty, performance, smoothing * 0.5f); //if doing well increase more
        //}
        
        difficulty = Mathf.Clamp(difficulty, 0.1f, 0.9f); //reasonable range       

        
        CalculateNewWeight(difficulty); //Passes difficulty to calculate new weight
        

        print("Total weight for wave is: "+weight);
        enemyGenerator.spawnEnemyChunk(Mathf.RoundToInt(weight),healthScaleMultiplier, "Cow"); //If wanting to spawn specific enemy types, Split weight and change "Cow"
        
        //player.ResetStats(); //clear stats
    }
    float Calculate_Performance()
    {
        float timeAlive = Mathf.Max(player.timeAlive, 1f); //avoid 0, cant divide

        float killRate = player.enemiesKilled / timeAlive; //kills per socond
        float deathPenalty = player.deaths * 0.3f; //lower diff dependent deaths

        float rawScore = (killRate * 2f) - deathPenalty;

        return Mathf.InverseLerp(-1f, 5f, rawScore);// 1 or 0
    }

    private void CalculateNewWeight(float difficulty)
    {
        //UPDATE WITH WEIGHTING CALCULATIONS
         weight += weight * difficulty;
    }

    private void Start()
    {
        enemyGenerator.spawnEnemyChunk(Mathf.RoundToInt(weight),healthScaleMultiplier, "Horse");
    }
}
