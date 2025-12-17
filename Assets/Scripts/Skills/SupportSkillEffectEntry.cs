//LMJ : Support skill effect entry for status effect icons (Mark, DOT, CC, etc.)
//      Icons are displayed above the target's head when status effect is applied
using System;
using UnityEngine;

/// <summary>
/// 서포트 스킬 이펙트 엔트리
/// 표식, DOT, CC 등 상태이상 아이콘을 대상 머리 위에 표시
/// </summary>
[Serializable]
public class SupportSkillEffectEntry
{
    [Header("서포트 스킬 정보 (CSV 자동 로드)")]
    [Tooltip("SupportSkillTable의 support_id")]
    public int supportId;

    [Tooltip("서포트 스킬 이름 (표시용)")]
    public string supportName;

    [Tooltip("상태이상 타입")]
    public StatusEffectType effectType;

    [Header("상태이상 아이콘 프리팹")]
    [Tooltip("대상 머리 위에 표시할 아이콘 프리팹")]
    public GameObject statusIconPrefab;

    [Tooltip("상태이상 적용 시 재생되는 이펙트")]
    public GameObject applyEffectPrefab;

    [Tooltip("상태이상 해제 시 재생되는 이펙트")]
    public GameObject removeEffectPrefab;

    [Header("표식(Mark) 전용 설정")]
    [Tooltip("표식 타입별 아이콘 (Romance, Comedy, Adventure, Mystery, Fear)")]
    public MarkIconSet markIcons;

    [Header("DOT 전용 설정")]
    [Tooltip("DOT 틱마다 재생되는 이펙트")]
    public GameObject dotTickEffectPrefab;

    [Header("CC 전용 설정")]
    [Tooltip("스턴 아이콘 프리팹")]
    public GameObject stunIconPrefab;

    [Tooltip("슬로우 아이콘 프리팹")]
    public GameObject slowIconPrefab;

    [Header("위치 설정")]
    [Tooltip("아이콘 Y 오프셋 (대상 머리 위 높이)")]
    public float iconYOffset = 2f;

    [Tooltip("아이콘 스케일")]
    public float iconScale = 1f;

    /// <summary>
    /// 아이콘 프리팹이 설정되어 있는지 확인
    /// </summary>
    public bool HasStatusIcon()
    {
        return statusIconPrefab != null ||
               (markIcons != null && markIcons.HasAnyIcon()) ||
               stunIconPrefab != null ||
               slowIconPrefab != null;
    }

    /// <summary>
    /// 상태이상 타입에 맞는 아이콘 프리팹 반환
    /// </summary>
    public GameObject GetIconPrefab(StatusEffectType type, MarkType markType = MarkType.None, CCType ccType = CCType.None)
    {
        switch (type)
        {
            case StatusEffectType.Mark:
                return markIcons?.GetIcon(markType) ?? statusIconPrefab;

            case StatusEffectType.CC:
                if (ccType == CCType.Stun && stunIconPrefab != null)
                    return stunIconPrefab;
                if (ccType == CCType.Slow && slowIconPrefab != null)
                    return slowIconPrefab;
                return statusIconPrefab;

            case StatusEffectType.DOT:
            case StatusEffectType.Chain:
            default:
                return statusIconPrefab;
        }
    }
}

/// <summary>
/// 표식 타입별 아이콘 세트
/// </summary>
[Serializable]
public class MarkIconSet
{
    [Tooltip("로맨스 표식 아이콘")]
    public GameObject romanceIcon;

    [Tooltip("코미디 표식 아이콘")]
    public GameObject comedyIcon;

    [Tooltip("모험 표식 아이콘")]
    public GameObject adventureIcon;

    [Tooltip("추리 표식 아이콘")]
    public GameObject mysteryIcon;

    [Tooltip("공포 표식 아이콘")]
    public GameObject fearIcon;

    /// <summary>
    /// 표식 타입에 맞는 아이콘 반환
    /// </summary>
    public GameObject GetIcon(MarkType markType)
    {
        return markType switch
        {
            MarkType.Romance => romanceIcon,
            MarkType.Comedy => comedyIcon,
            MarkType.Adventure => adventureIcon,
            MarkType.Mystery => mysteryIcon,
            MarkType.Fear => fearIcon,
            _ => null
        };
    }

    /// <summary>
    /// 아이콘이 하나라도 설정되어 있는지 확인
    /// </summary>
    public bool HasAnyIcon()
    {
        return romanceIcon != null ||
               comedyIcon != null ||
               adventureIcon != null ||
               mysteryIcon != null ||
               fearIcon != null;
    }
}
