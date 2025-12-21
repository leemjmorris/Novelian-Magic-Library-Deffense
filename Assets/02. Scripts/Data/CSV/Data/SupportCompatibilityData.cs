using System;
using CsvHelper.Configuration.Attributes;

/// <summary>
/// SupportCompatibilityTable.csv 데이터 클래스
/// 서포트 스킬과 메인 스킬 behavior_type 간의 호환성 정의
/// </summary>
[Serializable]
public class SupportCompatibilityData
{
    [Name("support_id")]
    public int support_id { get; set; }

    [Name("//support_name")]
    [Optional]
    public string support_name { get; set; }

    [Name("SingleProjectile")]
    public bool single_projectile { get; set; }

    [Name("ExplosiveProjectile")]
    public bool explosive_projectile { get; set; }

    [Name("BeamRay")]
    public bool beam_ray { get; set; }

    [Name("TargetAOE")]
    public bool target_aoe { get; set; }

    [Name("LinearAOE")]
    public bool linear_aoe { get; set; }

    [Name("GroundAOE")]
    public bool ground_aoe { get; set; }

    [Name("Barrier")]
    public bool barrier { get; set; }

    [Name("Buff")]
    public bool buff { get; set; }

    [Name("Debuff")]
    public bool debuff { get; set; }

    [Name("Trap")]
    public bool trap { get; set; }

    [Name("Instant")]
    public bool instant { get; set; }

    [Name("//description")]
    [Optional]
    public string description { get; set; }

    /// <summary>
    /// 특정 behavior_type과 호환되는지 확인
    /// </summary>
    public bool IsCompatibleWith(string behaviorType)
    {
        if (string.IsNullOrEmpty(behaviorType))
            return false;

        return behaviorType switch
        {
            "SingleProjectile" => single_projectile,
            "ExplosiveProjectile" => explosive_projectile,
            "BeamRay" => beam_ray,
            "TargetAOE" => target_aoe,
            "LinearAOE" => linear_aoe,
            "GroundAOE" => ground_aoe,
            "Barrier" => barrier,
            "Buff" => buff,
            "Debuff" => debuff,
            "Trap" => trap,
            "Instant" => instant,
            _ => false
        };
    }

    /// <summary>
    /// 투사체 계열 스킬과 호환되는지 확인
    /// </summary>
    public bool IsCompatibleWithProjectile => single_projectile || explosive_projectile;

    /// <summary>
    /// AOE 계열 스킬과 호환되는지 확인
    /// </summary>
    public bool IsCompatibleWithAOE => target_aoe || linear_aoe || ground_aoe;

    /// <summary>
    /// 공격 스킬과 호환되는지 확인
    /// </summary>
    public bool IsCompatibleWithDamageSkill =>
        single_projectile || explosive_projectile || beam_ray ||
        target_aoe || linear_aoe || ground_aoe || trap || instant;
}
