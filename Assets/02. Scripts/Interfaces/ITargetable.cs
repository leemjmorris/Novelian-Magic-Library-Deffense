using UnityEngine;

public interface ITargetable
{
    Transform GetTransform();
    Vector3 GetPosition();
    bool IsAlive();
    float Weight { get; }
    void TakeDamage(float damage);

    /// <summary>
    /// Check if this target has a Focus Mark (for focus targeting)
    /// </summary>
    bool HasFocusMark();

    /// <summary>
    /// Get remaining mark duration in seconds (for priority targeting)
    /// Returns float.MaxValue if no mark or dead
    /// </summary>
    float GetMarkRemainingTime();
}
