using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 스킬 이펙트 브릿지
/// 기존 Character 스킬 시스템과 새로운 SkillEffectManager를 연결하는 브릿지 클래스
///
/// SkillEffectDatabase를 통해 이펙트를 스폰합니다.
/// (레거시 SkillPrefabDatabase는 완전히 제거됨)
///
/// 사용법:
/// - 기존 코드에서 프리팹 직접 Instantiate 대신 이 브릿지 호출
/// - Character에서 LaunchProjectile 등에서 사용 가능
/// </summary>
public static class SkillEffectBridge
{
    // 새 시스템 사용 여부 (에디터에서 토글 가능)
    public static bool UseNewEffectSystem { get; set; } = true;

    /// <summary>
    /// 스킬 이펙트 스폰 시도
    /// </summary>
    /// <param name="skillId">스킬 ID</param>
    /// <param name="position">스폰 위치</param>
    /// <param name="rotation">스폰 회전</param>
    /// <param name="target">타겟 (옵션)</param>
    /// <param name="damageMultiplier">데미지 배율</param>
    /// <returns>성공 시 true (새 시스템 사용됨)</returns>
    public static async UniTask<bool> TrySpawnSkillEffect(
        int skillId,
        Vector3 position,
        Quaternion rotation,
        Transform target = null,
        float damageMultiplier = 1f)
    {
        if (!UseNewEffectSystem)
        {
            return false;
        }

        var manager = SkillEffectManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[SkillEffectBridge] SkillEffectManager not found");
            return false;
        }

        // 데이터베이스 확인
        var database = SkillEffectDatabase.Instance;
        if (database == null)
        {
            return false;
        }

        // 이펙트 매핑 확인
        var entry = database.GetEntry(skillId);
        if (entry == null || !entry.HasMainEffect())
        {
            // 매핑되지 않음 - 기존 시스템 사용
            return false;
        }

        // 새 시스템으로 스폰
        var wrapper = await manager.SpawnSkillEffect(skillId, position, rotation, target, damageMultiplier);
        return wrapper != null;
    }

    /// <summary>
    /// 타겟 방향으로 스킬 이펙트 스폰 시도
    /// </summary>
    public static async UniTask<bool> TrySpawnSkillEffectToward(
        int skillId,
        Vector3 position,
        Transform target,
        float damageMultiplier = 1f)
    {
        if (!UseNewEffectSystem)
        {
            return false;
        }

        var manager = SkillEffectManager.Instance;
        if (manager == null)
        {
            return false;
        }

        var database = SkillEffectDatabase.Instance;
        if (database == null)
        {
            return false;
        }

        var entry = database.GetEntry(skillId);
        if (entry == null || !entry.HasMainEffect())
        {
            return false;
        }

        var wrapper = await manager.SpawnSkillEffectToward(skillId, position, target, damageMultiplier);
        return wrapper != null;
    }

    /// <summary>
    /// 히트 이펙트 스폰 시도
    /// </summary>
    /// <param name="skillId">스킬 ID</param>
    /// <param name="position">히트 위치</param>
    /// <returns>스폰된 이펙트 (null이면 매핑 없음)</returns>
    public static GameObject TrySpawnHitEffect(int skillId, Vector3 position)
    {
        if (!UseNewEffectSystem)
        {
            return null;
        }

        var manager = SkillEffectManager.Instance;
        if (manager == null)
        {
            return null;
        }

        return manager.SpawnHitEffect(skillId, position);
    }

    /// <summary>
    /// 시전 이펙트 스폰 시도
    /// </summary>
    /// <param name="skillId">스킬 ID</param>
    /// <param name="position">시전 위치</param>
    /// <param name="parent">부모 Transform (옵션)</param>
    /// <returns>스폰된 이펙트 (null이면 매핑 없음)</returns>
    public static GameObject TrySpawnCastEffect(int skillId, Vector3 position, Transform parent = null)
    {
        if (!UseNewEffectSystem)
        {
            return null;
        }

        var manager = SkillEffectManager.Instance;
        if (manager == null)
        {
            return null;
        }

        return manager.SpawnCastEffect(skillId, position, parent);
    }

    /// <summary>
    /// 스킬이 새 이펙트 시스템에 매핑되어 있는지 확인
    /// </summary>
    /// <param name="skillId">스킬 ID</param>
    /// <returns>매핑 여부</returns>
    public static bool IsSkillMapped(int skillId)
    {
        var database = SkillEffectDatabase.Instance;
        if (database == null)
        {
            return false;
        }

        var entry = database.GetEntry(skillId);
        return entry != null && entry.HasMainEffect();
    }

    /// <summary>
    /// 스킬 이펙트 풀 사전 초기화
    /// 게임 시작 시 또는 씬 로드 시 호출하여 로딩 시간 최소화
    /// </summary>
    /// <param name="skillIds">초기화할 스킬 ID 목록</param>
    public static async UniTask PrewarmSkillEffects(params int[] skillIds)
    {
        var manager = SkillEffectManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[SkillEffectBridge] Cannot prewarm: SkillEffectManager not found");
            return;
        }

        foreach (int skillId in skillIds)
        {
            await manager.InitializePoolForSkill(skillId);
        }

        Debug.Log($"[SkillEffectBridge] Prewarmed {skillIds.Length} skill effect pools");
    }

    /// <summary>
    /// 모든 스킬 이펙트 회수
    /// 씬 전환 시 호출
    /// </summary>
    public static void DespawnAllEffects()
    {
        var manager = SkillEffectManager.Instance;
        if (manager != null)
        {
            manager.DespawnAllEffects();
        }
    }

    /// <summary>
    /// 전역 스케일 설정
    /// </summary>
    /// <param name="scale">스케일 팩터</param>
    public static void SetGlobalScale(float scale)
    {
        SkillEffectManager.GlobalScaleFactor = scale;

        var manager = SkillEffectManager.Instance;
        if (manager != null)
        {
            manager.SetGlobalScale(scale);
        }
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public static void LogDebugInfo()
    {
        var manager = SkillEffectManager.Instance;
        var database = SkillEffectDatabase.Instance;

        Debug.Log($"[SkillEffectBridge] Debug Info:\n" +
                  $"  UseNewEffectSystem: {UseNewEffectSystem}\n" +
                  $"  Manager Instance: {(manager != null ? "OK" : "NULL")}\n" +
                  $"  Database Instance: {(database != null ? "OK" : "NULL")}\n" +
                  $"  Total Active Effects: {(manager != null ? manager.GetTotalActiveEffectCount() : 0)}\n" +
                  $"  Global Scale: {SkillEffectManager.GlobalScaleFactor}");
    }
}
