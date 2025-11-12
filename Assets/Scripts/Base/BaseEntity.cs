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
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public virtual bool IsAlive() => gameObject.activeInHierarchy && currentHealth > 0;
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
