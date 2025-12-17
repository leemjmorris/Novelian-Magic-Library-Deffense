using System;
using CsvHelper.Configuration.Attributes;

/// <summary>
/// SkillLevelTable.csv 데이터 클래스
/// 스킬 레벨별 성장 데이터를 파싱합니다.
/// </summary>
[Serializable]
public class SkillLevelData
{
    [Name("skill_id")]
    public int skill_id { get; set; }

    [Name("level")]
    public int level { get; set; }

    [Name("damage_mult")]
    public float damage_mult { get; set; }

    [Name("cooldown_mult")]
    public float cooldown_mult { get; set; }

    [Name("range_mult")]
    public float range_mult { get; set; }

    [Name("aoe_mult")]
    public float aoe_mult { get; set; }

    [Name("projectile_add")]
    public int projectile_add { get; set; }

    [Name("pierce_add")]
    public int pierce_add { get; set; }

    [Name("duration_mult")]
    public float duration_mult { get; set; }

    [Name("unlock_condition")]
    public string unlock_condition { get; set; }

    /// <summary>
    /// 복합 키 생성 (skill_id * 100 + level)
    /// 예: skill_id=39002, level=3 → 3900203
    /// </summary>
    public int GetCompositeKey()
    {
        return skill_id * 100 + level;
    }
}
