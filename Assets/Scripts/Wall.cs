using UnityEngine;

public class Wall : MonoBehaviour
{
    private float health = 200f;
    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"Wall took {damage} damage. current Health: {health}");
        if (health <= 0)
        {
            GameOver();
        }
    }
    private void GameOver()
    {
        Debug.Log("Game Over!");
        Time.timeScale = 0f; // JML: Stop the game
    }
}
