//LMJ : Main skill prefab entry for skill effect mapping
//      Compatibility layer between SkillEffectEntry and legacy systems
//      Used by Character, Projectile, TrapObject, MineObject
using UnityEngine;

/// <summary>
/// 메인 스킬 프리팹 엔트리
/// SkillEffectEntry와 레거시 시스템 간의 호환성 레이어
/// </summary>
[System.Serializable]
public class MainSkillPrefabEntry
{
    [Tooltip("스킬 ID (CSV skill_id와 매칭)")]
    public int skillId;

    [Tooltip("스킬 이름 (디버그용)")]
    public string skillName;

    [Header("Effect Prefabs")]
    [Tooltip("메인 발사체/이펙트 프리팹")]
    public GameObject projectilePrefab;

    [Tooltip("피격 이펙트 프리팹")]
    public GameObject hitEffectPrefab;

    [Tooltip("시전 이펙트 프리팹")]
    public GameObject castEffectPrefab;

    [Tooltip("트레일 이펙트 프리팹")]
    public GameObject trailEffectPrefab;

    [Tooltip("영역 이펙트 프리팹 (채널링/빔 스킬용)")]
    public GameObject areaEffectPrefab;

    [Header("Effect Scales")]
    [Tooltip("메인 이펙트 스케일 (-1 = 전역값, 0 이상 = 개별값)")]
    public float mainEffectScale = -1f;

    [Tooltip("피격 이펙트 스케일 (-1 = 전역값, 0 이상 = 개별값)")]
    public float hitEffectScale = -1f;

    [Tooltip("시전 이펙트 스케일 (-1 = 전역값, 0 이상 = 개별값)")]
    public float castEffectScale = -1f;

    [Tooltip("트레일 이펙트 스케일 (-1 = 전역값, 0 이상 = 개별값)")]
    public float trailEffectScale = -1f;

    [Tooltip("영역 이펙트 스케일 (-1 = 전역값, 0 이상 = 개별값)")]
    public float areaEffectScale = -1f;

    /// <summary>
    /// 메인 이펙트 존재 여부
    /// </summary>
    public bool HasMainEffect() => projectilePrefab != null;

    /// <summary>
    /// 히트 이펙트 존재 여부
    /// </summary>
    public bool HasHitEffect() => hitEffectPrefab != null;

    /// <summary>
    /// 시전 이펙트 존재 여부
    /// </summary>
    public bool HasCastEffect() => castEffectPrefab != null;

    /// <summary>
    /// 트레일 이펙트 존재 여부
    /// </summary>
    public bool HasTrailEffect() => trailEffectPrefab != null;

    /// <summary>
    /// 메인 이펙트 스케일 반환 (-1이면 기본값 1, 0 이상이면 해당값)
    /// </summary>
    public float GetMainScale() => mainEffectScale >= 0f ? mainEffectScale : 1f;

    /// <summary>
    /// 피격 이펙트 스케일 반환 (-1이면 기본값 1, 0 이상이면 해당값)
    /// </summary>
    public float GetHitScale() => hitEffectScale >= 0f ? hitEffectScale : 1f;

    /// <summary>
    /// 시전 이펙트 스케일 반환 (-1이면 기본값 1, 0 이상이면 해당값)
    /// </summary>
    public float GetCastScale() => castEffectScale >= 0f ? castEffectScale : 1f;

    /// <summary>
    /// 트레일 이펙트 스케일 반환 (-1이면 기본값 1, 0 이상이면 해당값)
    /// </summary>
    public float GetTrailScale() => trailEffectScale >= 0f ? trailEffectScale : 1f;

    /// <summary>
    /// 영역 이펙트 스케일 반환 (-1이면 기본값 1, 0 이상이면 해당값)
    /// </summary>
    public float GetAreaScale() => areaEffectScale >= 0f ? areaEffectScale : 1f;
}
