using UnityEngine;

/// <summary>
/// Base class for all poolable entities that have health and can take damage.
/// Implements IEntity and IPoolable interfaces.
/// Used by Monster and BossMonster.
/// </summary>
public abstract class BaseEntity : MonoBehaviour, IEntity, IPoolable
{
    [SerializeField] protected float maxHealth;
    protected float currentHealth;

    // IEntity Implementation
    public virtual void TakeDamage(float damage)
    {
        // Prevent damage to dead entities (also checks if already despawned)
        if (!IsAlive()) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;

    /// <summary>
    /// CSV 데이터 기반으로 최대 체력 설정
    /// </summary>
    public void SetMaxHealth(float hp)
    {
        maxHealth = hp;
        currentHealth = maxHealth;
    }
    public virtual bool IsAlive()
    {
        // JML: Check if Unity object is destroyed before accessing properties
        if (this == null || gameObject == null)
            return false;

        return gameObject.activeInHierarchy && currentHealth > 0;
    }
    public Vector3 GetPosition() => transform.position;
    public Transform GetTransform() => transform;
    public abstract void Die();

    // IPoolable Implementation
    public virtual void OnSpawn()
    {
        currentHealth = maxHealth;
    }

    public virtual void OnDespawn()
    {
        // Override in derived classes if needed
    }
}
