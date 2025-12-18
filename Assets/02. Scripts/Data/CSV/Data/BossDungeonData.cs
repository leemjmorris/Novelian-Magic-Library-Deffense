/// <summary>
/// 도전던전(보스던전) 스테이지 정보 데이터
/// Issue #476 - 도전던전 콘텐츠 구현
/// </summary>
public class BossDungeonData
{
    /// <summary>
    /// 던전 고유 ID (24001~24100)
    /// </summary>
    public int Dungeon_ID { get; set; }

    /// <summary>
    /// 스테이지 번호 (1~100)
    /// </summary>
    public int Floor_Index { get; set; }

    /// <summary>
    /// 보스 몬스터 ID (MonsterTable 참조)
    /// </summary>
    public int Boss_ID { get; set; }

    /// <summary>
    /// 보스 레벨 ID (MonsterLevelTable 참조)
    /// </summary>
    public int Boss_Level_ID { get; set; }

    /// <summary>
    /// 스턴 전 공격 횟수
    /// </summary>
    public int Attack_Count { get; set; }

    /// <summary>
    /// 보스 공격 주기 (초)
    /// </summary>
    public float Attack_Period { get; set; }

    /// <summary>
    /// 보스가 Wall 공격 시 스턴 게이지 감소량
    /// </summary>
    public int Stun_Damage { get; set; }

    /// <summary>
    /// 스턴 게이지 최대값
    /// </summary>
    public float Stun_Gauge { get; set; }

    /// <summary>
    /// 스턴 지속 시간 (초)
    /// </summary>
    public float Stun_Duration { get; set; }

    /// <summary>
    /// 제한 시간 (초)
    /// </summary>
    public float Time_Limit { get; set; }

    /// <summary>
    /// 보상 그룹 ID (RewardGroupTable 참조)
    /// </summary>
    public int Reward_Group_ID { get; set; }

    /// <summary>
    /// 구현 여부 (1=구현됨, 0=미구현)
    /// </summary>
    public int Is_Implemented { get; set; }

    /// <summary>
    /// 구현된 스테이지인지 확인
    /// </summary>
    public bool IsImplemented => Is_Implemented == 1;
}
