/// <summary>
/// CardLevelTable.csv 데이터 클래스
/// 인게임 카드 레벨별 효과값 (1~3 Tier)
/// </summary>
public class CardLevelData
{
    public int Card_Level_ID { get; set; }
    public int Tier { get; set; }
    public float value_change { get; set; }
    public int Is_Final_Level { get; set; }  // 0 = false, 1 = true (BOOL in CSV)
}
