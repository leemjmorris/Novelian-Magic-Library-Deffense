using System;
using UnityEngine;

public class Wall : MonoBehaviour, IEntity
{
    public static event Action OnWallDestroyed;
    public static event Action<float, float> OnHealthChanged; // (currentHP, maxHP)

    [SerializeField] private float maxHealth = 200f;
    private float health;

    private void Awake()
    {
        health = maxHealth;
        OnHealthChanged?.Invoke(health, maxHealth);
    }

    // IEntity Implementation
    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"Wall took {damage} damage. current Health: {health}");
        OnHealthChanged?.Invoke(health, maxHealth);

        if (health <= 0)
        {
            Die();
        }
    }

    public float GetHealth() => health;
    public float GetMaxHealth() => maxHealth;
    public bool IsAlive() => health > 0;
    public Vector3 GetPosition() => transform.position;
    public Transform GetTransform() => transform;

    public void Die()
    {
        GameOver();
    }

    private void GameOver()
    {
        Debug.Log("Game Over!");
        OnWallDestroyed?.Invoke();
    }
}
