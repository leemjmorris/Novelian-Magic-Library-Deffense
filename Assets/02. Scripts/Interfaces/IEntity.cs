using UnityEngine;

/// <summary>
/// Interface for all entities that have health, position, and can take damage.
/// Implemented by Monster, BossMonster, and Wall.
/// </summary>
public interface IEntity
{
    // Health Management
    void TakeDamage(float damage);
    float GetHealth();
    float GetMaxHealth();
    bool IsAlive();

    // Position & Transform
    Vector3 GetPosition();
    Transform GetTransform();

    // Lifecycle
    void Die();
}
