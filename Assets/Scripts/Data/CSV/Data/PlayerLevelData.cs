/// <summary>
/// PlayerLevelTable.csv 데이터 클래스
/// 플레이어 레벨업 시 카드 선택 규칙 정의
/// 테이블 번호: 07
/// </summary>
public class PlayerLevelData
{
    /// <summary>
    /// 레벨 ID (0701 ~ 0750)
    /// </summary>
    public int Level_ID { get; set; }

    /// <summary>
    /// 필요 경험치 (Req_EXP)
    /// </summary>
    public float Req_EXP { get; set; }

    /// <summary>
    /// 누적 경험치 (Tot_EXP)
    /// </summary>
    public float Tot_EXP { get; set; }

    /// <summary>
    /// 카드 목록 ID (CardListTable 참조)
    /// N/A인 경우 0으로 저장됨
    /// </summary>
    public int Card_List_ID { get; set; }

    /// <summary>
    /// 캐릭터 카드 출현 여부
    /// 1 = 캐릭터 카드 2장 표시
    /// 0 = Card_List_ID의 스탯 카드 2장 표시
    /// </summary>
    public int Character_Card_Appear { get; set; }

    /// <summary>
    /// Level_ID에서 실제 레벨 추출 (0701 → 1, 0750 → 50)
    /// </summary>
    public int GetLevel()
    {
        return Level_ID % 100;
    }
}
