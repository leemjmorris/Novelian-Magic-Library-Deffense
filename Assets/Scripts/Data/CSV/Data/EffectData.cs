using System;

/// <summary>
/// Effect data class matching EffectTable.csv structure
/// </summary>
[Serializable]
public class EffectData
{
    public int Effect_ID { get; set; }
    public string Effect_Name { get; set; }
    public int Effect_Level { get; set; }
    public EffectCategory Effect_Category { get; set; }
    public TargetType Target_Type { get; set; }
    public EffectApplyMode Effect_Apply_Mode { get; set; }
    public float Effect_Value { get; set; }
    public float? Duration { get; set; }  // Nullable float to handle empty values and decimal durations in CSV
    public bool Stackble { get; set; }
    public bool Is_Upgrade { get; set; }
    public string Description { get; set; }
    public int Table_Num { get; set; }
    public int Order2 { get; set; }
}

/// <summary>
/// Effect Category Enum: 1=Damage, 2=Buff, 3=Debuff, 4=Special
/// </summary>
public enum EffectCategory
{
    Damage = 1,
    Buff = 2,
    Debuff = 3,
    Special = 4
}

/// <summary>
/// Target Type Enum: 1=Ally, 2=Enemy, 3=All
/// </summary>
public enum TargetType
{
    Ally = 1,
    Enemy = 2,
    All = 3
}

/// <summary>
/// Effect Apply Mode Enum: 1=Instant, 2=Duration, 3=DOT
/// </summary>
public enum EffectApplyMode
{
    Instant = 1,
    Duration = 2,
    DOT = 3
}
