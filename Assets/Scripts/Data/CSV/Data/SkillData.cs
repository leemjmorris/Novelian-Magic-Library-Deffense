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


