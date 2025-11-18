using System;

/// <summary>
/// Skill data class matching SkillTable.csv structure
/// </summary>
[Serializable]
public class SkillData
{
    public int Skill_ID { get; set; }
    public string Skill_Name { get; set; }
    public SkillType Skill_Type { get; set; }
    public AttackRange Attack_Range { get; set; }
    public float Cooldown { get; set; }
    public float Cast_Time { get; set; }
    public int Effect_ID { get; set; }
    public bool Equipable { get; set; }
    public string Description { get; set; }
    public int Table_Num { get; set; }
    public int Order2 { get; set; }
}

/// <summary>
/// Skill Type Enum: 1=Attack, 2=Buff, 3=Debuff
/// </summary>
public enum SkillType
{
    Attack = 1,
    Buff = 2,
    Debuff = 3
}

/// <summary>
/// Attack Range Enum: 1=Single, 2=Area, 3=Wide
/// </summary>
public enum AttackRange
{
    Single = 1,
    Area = 2,
    Wide = 3
}
