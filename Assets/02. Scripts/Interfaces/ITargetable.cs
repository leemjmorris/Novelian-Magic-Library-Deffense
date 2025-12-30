using UnityEngine;

public interface ITargetable
{
    Transform GetTransform();
    Vector3 GetPosition();
    bool IsAlive();
    float Weight { get; }
    void TakeDamage(float damage);

    /// <summary>
    /// 크리티컬 데미지 여부를 포함한 데미지 처리
    /// </summary>
    void TakeDamage(float damage, bool isCritical);

    /// <summary>
    /// 상성 시스템 적용 데미지 처리 (Issue #531)
    /// 공격자 장르에 따라 상성 배율 적용
    /// </summary>
    /// <param name="damage">기본 데미지</param>
    /// <param name="isCritical">치명타 여부</param>
    /// <param name="attackerGenre">공격자 장르</param>
    void TakeDamage(float damage, bool isCritical, Genre attackerGenre);

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
