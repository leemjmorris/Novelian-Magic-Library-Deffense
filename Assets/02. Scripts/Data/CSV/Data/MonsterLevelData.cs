public class MonsterLevelData
{
    public int Mon_Level_ID { get; set; }
    public int Level_Type { get; set; }  // 1~10 레벨 (INT로 변경)
    public MonsterGrade Monster_Grade { get; set; }
    public float Monster_Weight { get; set; }
    public float Endurance { get; set; }
    public float HP { get; set; }
    public float Power { get; set; }
    public float ATK { get; set; }
    public float Move_Speed { get; set; }
    public float Attack_Speed { get; set; }
    public int Exp_Value { get; set; }
}

public enum MonsterGrade
{
    Normal = 1,   // 일반 몬스터
    Elite = 2,    // 정예 몬스터
    Boss = 3      // 보스 몬스터
}