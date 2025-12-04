using System;
using CsvHelper.Configuration.Attributes;

/// <summary>
/// SupportCompatibilityTable.csv 데이터 클래스
/// 서포트 스킬과 메인 스킬 타입 간의 호환성 정의
/// </summary>
[Serializable]
public class SupportCompatibilityData
{
    [Name("Support_ID")]
    public int support_id { get; set; }

    [Name("//Support_name")]
    [Optional]
    public string support_name { get; set; }

    [Name("Projectile")]
    public int projectile { get; set; }

    [Name("Instant_Single")]
    public int instant_single { get; set; }

    [Name("AoE")]
    public int aoe { get; set; }

    [Name("DOT")]
    public int dot { get; set; }

    [Name("Buff")]
    public int buff { get; set; }

    [Name("Debuff")]
    public int debuff { get; set; }

    [Name("Channeling")]
    public int channeling { get; set; }

    [Name("Trap")]
    public int trap { get; set; }

    [Name("Mine")]
    public int mine { get; set; }

    [Name("//Description")]
    [Optional]
    public string description { get; set; }

    /// <summary>
    /// 특정 스킬 타입과 호환되는지 확인
    /// </summary>
    public bool IsCompatibleWith(SkillAssetType skillType)
    {
        return skillType switch
        {
            SkillAssetType.Projectile => projectile == 1,
            SkillAssetType.InstantSingle => instant_single == 1,
            SkillAssetType.AOE => aoe == 1,
            SkillAssetType.DOT => dot == 1,
            SkillAssetType.Buff => buff == 1,
            SkillAssetType.Debuff => debuff == 1,
            SkillAssetType.Channeling => channeling == 1,
            SkillAssetType.Trap => trap == 1,
            SkillAssetType.Mine => mine == 1,
            _ => false
        };
    }
}
