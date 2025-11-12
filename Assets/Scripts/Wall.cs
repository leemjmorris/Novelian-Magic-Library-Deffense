using System;
using UnityEngine;

public class Wall : MonoBehaviour
{
    private float health = 200f;

    public static event Action OnWallDestroyed;
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
        OnWallDestroyed?.Invoke();
    }
}
