using System;
using CsvHelper.Configuration.Attributes;

/// <summary>
/// SupportCompatibilityTable.csv 데이터 클래스
/// 서포트 스킬과 메인 스킬 타입 간의 호환성 정의
/// </summary>
[Serializable]
public class SupportCompatibilityData
{
    [Name("support_id")]
    public int support_id { get; set; }

    [Name("//support_name")]
    [Optional]
    public string support_name { get; set; }

    [Name("projectile")]
    public int projectile { get; set; }

    [Name("instant_single")]
    public int instant_single { get; set; }

    [Name("aoe")]
    public int aoe { get; set; }

    [Name("dot")]
    public int dot { get; set; }

    [Name("buff")]
    public int buff { get; set; }

    [Name("debuff")]
    public int debuff { get; set; }

    [Name("channeling")]
    public int channeling { get; set; }

    [Name("trap")]
    public int trap { get; set; }

    [Name("mine")]
    public int mine { get; set; }

    [Name("//description")]
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
