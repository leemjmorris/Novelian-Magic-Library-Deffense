using System;
using UnityEngine;

/// <summary>
/// 개별 스킬 이펙트 설정 데이터
/// SpecialSkillsEffectsPack 에셋 프리팹과 게임 스킬을 연결합니다
/// </summary>
[Serializable]
public class SkillEffectEntry
{
    [Header("스킬 정보 (CSV 자동 로드)")]
    [Tooltip("MainSkillTable의 skill_id")]
    public int skillId;

    [Tooltip("스킬 이름 (표시용)")]
    public string skillName;

    [Tooltip("스킬 타입")]
    public SkillAssetType skillType;

    [Header("이펙트 프리팹 (SpecialSkillsEffectsPack)")]
    [Tooltip("메인 이펙트 프리팹 (투사체/AOE/버프 등)")]
    public GameObject mainEffectPrefab;

    [Tooltip("피격 이펙트 프리팹 (선택)")]
    public GameObject hitEffectPrefab;

    [Tooltip("시전 이펙트 프리팹 (선택)")]
    public GameObject castEffectPrefab;

    [Tooltip("트레일 이펙트 프리팹 (선택)")]
    public GameObject trailEffectPrefab;

    [Tooltip("영역 이펙트 프리팹 (채널링/빔 스킬용)")]
    public GameObject areaEffectPrefab;

    [Header("스케일 설정")]
    [Tooltip("메인 이펙트 스케일 오버라이드 (-1 = 전역값 사용, 0 이상 = 개별값)")]
    public float scaleOverride = -1f;

    [Tooltip("피격 이펙트 스케일 (-1 = 전역값 사용, 0 이상 = 개별값)")]
    public float hitEffectScale = -1f;

    [Tooltip("시전 이펙트 스케일 (-1 = 전역값 사용, 0 이상 = 개별값)")]
    public float castEffectScale = -1f;

    [Tooltip("트레일 이펙트 스케일 (-1 = 전역값 사용, 0 이상 = 개별값)")]
    public float trailEffectScale = -1f;

    [Tooltip("영역 이펙트 스케일 (-1 = 전역값 사용, 0 이상 = 개별값)")]
    public float areaEffectScale = -1f;

    [Header("AOE 이펙트 설정")]
    [Tooltip("이펙트 프리팹의 기본 반경 (scale=1일 때 시각적 반경)\n" +
             "AOE Preview에서 'Measure Effect' 버튼으로 측정 가능")]
    [Min(0.1f)]
    public float baseEffectRadius = 1f;

    [Header("이펙트 지속시간 설정")]
    [Tooltip("이펙트의 재생 지속시간 (초)\n" +
             "ParticleSystem Duration 또는 수동 측정값\n" +
             "AOE 스킬의 DOT 지속시간으로 사용됨")]
    [Min(0f)]
    public float effectDuration = 0f;

    [Header("동작 설정")]
    [Tooltip("에셋의 이동 로직 사용 여부 (ObjectMove 등)")]
    public bool useAssetMovement = true;

    [Tooltip("CSV의 속도값으로 에셋 속도 오버라이드")]
    public bool overrideSpeed = false;

    [Tooltip("래퍼 프리팹 경로 (자동 생성됨)")]
    [HideInInspector]
    public string wrapperPrefabPath;

    /// <summary>
    /// 최소 하나의 이펙트 프리팹이 연결되어 있는지 확인
    /// </summary>
    public bool HasAnyEffect()
    {
        return mainEffectPrefab != null ||
               hitEffectPrefab != null ||
               castEffectPrefab != null ||
               trailEffectPrefab != null ||
               areaEffectPrefab != null;
    }

    /// <summary>
    /// 메인 이펙트가 연결되어 있는지 확인
    /// </summary>
    public bool HasMainEffect()
    {
        return mainEffectPrefab != null;
    }

    /// <summary>
    /// 메인 이펙트 스케일 계산 (-1 = 전역값, 0 이상 = 개별값)
    /// </summary>
    /// <param name="globalScale">전역 스케일 값</param>
    /// <returns>적용할 스케일</returns>
    public float GetEffectiveScale(float globalScale)
    {
        return scaleOverride >= 0f ? scaleOverride : globalScale;
    }

    /// <summary>
    /// 피격 이펙트 스케일 계산 (-1 = 전역값, 0 이상 = 개별값)
    /// </summary>
    /// <param name="globalScale">전역 스케일 값</param>
    /// <returns>적용할 스케일</returns>
    public float GetHitEffectScale(float globalScale)
    {
        return hitEffectScale >= 0f ? hitEffectScale : globalScale;
    }

    /// <summary>
    /// 시전 이펙트 스케일 계산 (-1 = 전역값, 0 이상 = 개별값)
    /// </summary>
    /// <param name="globalScale">전역 스케일 값</param>
    /// <returns>적용할 스케일</returns>
    public float GetCastEffectScale(float globalScale)
    {
        return castEffectScale >= 0f ? castEffectScale : globalScale;
    }

    /// <summary>
    /// 트레일 이펙트 스케일 계산 (-1 = 전역값, 0 이상 = 개별값)
    /// </summary>
    /// <param name="globalScale">전역 스케일 값</param>
    /// <returns>적용할 스케일</returns>
    public float GetTrailEffectScale(float globalScale)
    {
        return trailEffectScale >= 0f ? trailEffectScale : globalScale;
    }

    /// <summary>
    /// 영역 이펙트 스케일 계산 (-1 = 전역값, 0 이상 = 개별값)
    /// </summary>
    /// <param name="globalScale">전역 스케일 값</param>
    /// <returns>적용할 스케일</returns>
    public float GetAreaEffectScale(float globalScale)
    {
        return areaEffectScale >= 0f ? areaEffectScale : globalScale;
    }
}
