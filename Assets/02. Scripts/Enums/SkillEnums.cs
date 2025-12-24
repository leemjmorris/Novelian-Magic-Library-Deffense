// 스킬 관련 Enum 정의
// 스킬 시스템뿐만 아니라 Monster, StageManager 등에서도 사용됨
// 리팩토링됨: 장르 기반 속성 추가

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 행동 타입 (CSV behavior_type 컬럼)
    /// </summary>
    public enum SkillBehaviorType
    {
        Unknown,
        Projectile,          // 단일 투사체
        Spawner_Projectile,  // 다중 투사체 스폰
        Spawner_AOE,         // 다중 AOE 스폰
        Visual_AOE,          // 시각적 AOE (즉시 데미지)
        Field,               // 지속 필드
        Shield,              // 방어막
        Beam                 // 빔/레이저
    }

    /// <summary>
    /// 스킬 데미지 타입 (CSV damage_type 컬럼)
    /// </summary>
    public enum SkillDamageType
    {
        None,
        OnHit,      // 충돌 시 데미지
        OnSpawn,    // 스폰 시 데미지
        Instant,    // 즉시 데미지
        Periodic    // 주기적 데미지
    }

    /// <summary>
    /// 스킬 에셋 타입 (스킬 실행 분기용)
    /// </summary>
    public enum SkillAssetType
    {
        Unknown,
        Projectile,      // 투사체
        AOE,             // 범위 공격
        InstantSingle,   // 즉발 단일 대상
        DOT,             // 지속 데미지
        Channeling,      // 채널링
        Buff,            // 버프
        Debuff,          // 디버프
        Trap,            // 트랩
        Mine             // 지뢰
    }

    /// <summary>
    /// 속성 타입 (장르 기반 - 소설 테마)
    /// </summary>
    public enum ElementType
    {
        None,
        Fire,
        Ice,
        Lightning,
        Poison,
        Holy,
        Dark,
        Physical,
        // 장르 기반 속성 (CSV element_type_ID 매핑용)
        Romance,    // 로맨스 (3101101)
        Comedy,     // 코미디 (3101202)
        Adventure,  // 모험 (3101303)
        Mystery,    // 추리 (3101404)
        Fear        // 공포 (3101505)
    }

    /// <summary>
    /// CC (군중 제어) 타입
    /// </summary>
    public enum CCType
    {
        None,
        Stun,       // 기절
        Slow,       // 둔화
        Root,       // 속박
        Silence,    // 침묵
        Knockback,  // 넉백
        Fear,       // 공포
        Taunt,      // 도발
        Freeze      // 빙결
    }

    /// <summary>
    /// 버프 타입
    /// </summary>
    public enum BuffType
    {
        None,
        ATK_Damage_UP,      // 공격력 증가
        ATK_Speed_UP,       // 공격 속도 증가
        ATK_Range_UP,       // 사거리 증가
        Critical_Damage_UP, // 치명타 데미지 증가
        Critical_Chance_UP, // 치명타 확률 증가
        Move_Speed_UP,      // 이동 속도 증가
        Defense_UP,         // 방어력 증가
        Battle_Exp_UP,      // 전투 경험치 증가
        HP_Regen,           // 체력 재생
        Shield              // 보호막
    }

    /// <summary>
    /// 디버프 타입
    /// </summary>
    public enum DeBuffType
    {
        None,
        ATK_Damage_DOWN,    // 공격력 감소
        ATK_Speed_DOWN,     // 공격 속도 감소
        Defense_DOWN,       // 방어력 감소
        Move_Speed_DOWN,    // 이동 속도 감소
        Vulnerability,      // 받는 피해 증가
        Blind,              // 실명 (명중률 감소)
        Weaken,             // 약화
        // CSV ID 매핑용 별칭
        ATK_Damage_Down = ATK_Damage_DOWN,
        ATK_Speed_Down = ATK_Speed_DOWN,
        Take_Damage_UP = Vulnerability  // 받는 피해 증가
    }

    /// <summary>
    /// DOT (지속 데미지) 타입
    /// </summary>
    public enum DOTType
    {
        None,
        Burn,       // 화상
        Poison,     // 독
        Bleed,      // 출혈
        Curse,      // 저주
        Corruption  // 부식
    }

    /// <summary>
    /// 마크 타입 (타겟팅 우선순위용)
    /// </summary>
    public enum MarkType
    {
        None,
        Target,         // 타겟 마크 (우선 공격)
        Weakness,       // 약점 마크 (추가 데미지)
        Execute,        // 처형 마크 (체력 낮으면 즉사)
        Bounty,         // 현상금 마크 (처치 시 보너스)
        Chain,          // 체인 마크 (연쇄 데미지)
        Focus,          // 포커스 마크 (집중 공격)
        // 장르 기반 마크 (CSV element_type_ID 매핑용)
        Romance,        // 로맨스 마크
        Comedy,         // 코미디 마크
        Adventure,      // 모험 마크
        Mystery,        // 추리 마크
        Fear            // 공포 마크
    }

    /// <summary>
    /// 상태 효과 타입 (서포트 스킬용)
    /// </summary>
    public enum StatusEffectType
    {
        None,
        Chain,          // 체이닝
        Pierce,         // 관통
        Splash,         // 스플래시
        Bounce,         // 바운스
        Homing,         // 유도
        Fragmentation,  // 파편화
        Vampiric,       // 흡혈
        Explosive,      // 폭발
        // CSV status_effect ID 매핑용
        CC,             // 군중 제어 (3201701)
        DOT,            // 지속 데미지 (3201802)
        Mark            // 마크 (3201903)
    }
}
