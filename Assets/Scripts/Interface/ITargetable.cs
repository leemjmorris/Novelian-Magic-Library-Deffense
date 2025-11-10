using UnityEngine;

public interface ITargetable
{
    Transform GetTransform();
    Vector3 GetPosition();
    bool IsAlive();
    void TakeDamage(float damage);
}
