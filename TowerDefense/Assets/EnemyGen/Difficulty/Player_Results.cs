using UnityEngine;

public class Player_Results : MonoBehaviour
{
    public int deaths;
    public int enemiesKilled;
    public float timeAlive;

    private float startTime;
    void Start()
    {
        ResetTimer(); //timer begins when game does
    }
    void Update()
    {
        timeAlive = Time.time - startTime; //updates time alive
    }
    public void RecordDeath()
    {
        deaths++; //player loss
        ResetTimer(); //restart timer
    }
    public void RecordKill()
    {
        enemiesKilled++; //add kill
    }
    void ResetTimer()
    {
        startTime = Time.time;
        timeAlive = 0f; //reset time
    }
    public void ResetStats()
    {
        deaths = 0;
        enemiesKilled = 0;
        ResetTimer();
    }
}
