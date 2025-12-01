using NovelianMagicLibraryDefense.Events;
using UnityEngine;

public class Wall : MonoBehaviour, IEntity
{
    [Header("Event Channels")]
    [SerializeField] private WallEvents wallEvents;

    [SerializeField] private float maxHealth = 200f;
    private float health;

    private void Awake()
    {
        health = maxHealth;
    }

    private void Start()
    {
        // LMJ: Raise initial health after all managers are initialized
        if (wallEvents != null)
        {
            wallEvents.RaiseHealthChanged(health, maxHealth);
        }
    }

    // IEntity Implementation
    public void TakeDamage(float damage)
    {
        health -= damage;
        // Debug.Log($"Wall took {damage} damage. current Health: {health}");

        // LMJ: Use EventChannel instead of static event
        if (wallEvents != null)
        {
            wallEvents.RaiseHealthChanged(health, maxHealth);
        }

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

    /// <summary>
    /// CSV 데이터 기반으로 Wall의 최대 체력 설정 (StageData.Barrier_HP)
    /// Start() 전에 호출되어야 함
    /// </summary>
    public void SetMaxHealth(float hp)
    {
        maxHealth = hp;
        health = maxHealth;
        Debug.Log($"[Wall] MaxHealth set to {maxHealth} from CSV");
    }

    public void Die()
    {
        GameOver();
    }

    private void GameOver()
    {
        // Debug.Log("Game Over!");

        // LMJ: Use EventChannel instead of static event
        if (wallEvents != null)
        {
            wallEvents.RaiseWallDestroyed();
        }
    }
}
