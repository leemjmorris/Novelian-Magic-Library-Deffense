public class MonsterLevelData
{
    public int Mon_Level_ID { get; set; }
    public LevelType Level_Type { get; set; }
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

public enum LevelType
{
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
    Level4 = 4,
    Level5 = 5,
    Level6 = 6,
    Level7 = 7,
    Level8 = 8,
    Level9 = 9,
    Level10 = 10
}
public enum MonsterGrade
{
    Normal = 1,       // 일반 몬스터
    MidBoss = 2,      // 중간 보스 몬스터
    FinalBoss = 3     // 최종 보스 몬스터
}