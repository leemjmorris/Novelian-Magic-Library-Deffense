// LMJ: 스킬 시스템 리팩토링 - SkillEffectManager/SkillEffectDatabase 제거됨
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 스킬 이펙트 브릿지 (스텁)
/// 스킬 시스템 리팩토링 대기 중 - 새 시스템 구현 시 교체 예정
/// </summary>
public static class SkillEffectBridge
{
    // 새 시스템 사용 여부 (현재 비활성화)
    public static bool UseNewEffectSystem { get; set; } = false;

    /// <summary>
    /// 스킬 이펙트 스폰 시도 (스텁)
    /// TODO: 새 스킬 시스템 구현 시 구현
    /// </summary>
    public static UniTask<bool> TrySpawnSkillEffect(
        int skillId,
        Vector3 position,
        Quaternion rotation,
        Transform target = null,
        float damageMultiplier = 1f)
    {
        return UniTask.FromResult(false);
    }

    /// <summary>
    /// 타겟 방향으로 스킬 이펙트 스폰 시도 (스텁)
    /// </summary>
    public static UniTask<bool> TrySpawnSkillEffectToward(
        int skillId,
        Vector3 position,
        Transform target,
        float damageMultiplier = 1f)
    {
        return UniTask.FromResult(false);
    }

    /// <summary>
    /// 히트 이펙트 스폰 시도 (스텁)
    /// </summary>
    public static GameObject TrySpawnHitEffect(int skillId, Vector3 position)
    {
        return null;
    }

    /// <summary>
    /// 시전 이펙트 스폰 시도 (스텁)
    /// </summary>
    public static GameObject TrySpawnCastEffect(int skillId, Vector3 position, Transform parent = null)
    {
        return null;
    }

    /// <summary>
    /// 스킬이 새 이펙트 시스템에 매핑되어 있는지 확인 (스텁)
    /// </summary>
    public static bool IsSkillMapped(int skillId)
    {
        return false;
    }

    /// <summary>
    /// 스킬 이펙트 풀 사전 초기화 (스텁)
    /// </summary>
    public static async UniTask PrewarmSkillEffects(params int[] skillIds)
    {
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// 모든 스킬 이펙트 회수 (스텁)
    /// </summary>
    public static void DespawnAllEffects()
    {
        // no-op
    }

    /// <summary>
    /// 전역 스케일 설정 (스텁)
    /// </summary>
    public static void SetGlobalScale(float scale)
    {
        // no-op
    }

    /// <summary>
    /// 디버그 정보 출력 (스텁)
    /// </summary>
    public static void LogDebugInfo()
    {
    }
}
