using UnityEngine;

public interface ITargetable
{
    Transform GetTransform();
    Vector3 GetPosition();
    bool IsAlive();
    float Weight { get; }
    void TakeDamage(float damage);
}
