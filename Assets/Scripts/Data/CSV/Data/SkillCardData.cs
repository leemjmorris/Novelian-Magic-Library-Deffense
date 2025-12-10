public class SkillCardData
{
    public int Skill_Card_Level_ID { get; set; }
    public int Support_id { get; set; }
    public Tier Tier { get; set; }
}

public enum Tier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3
}