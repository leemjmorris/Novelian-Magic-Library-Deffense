/// <summary>
/// 장르 상성 계산 유틸리티 (Issue #531)
/// 상성 순환: 로맨스(2)→공포(1)→추리(5)→모험(3)→코믹(4)→로맨스(2)
/// 상성 = 1.2x, 역상성 = 0.6x, 중립 = 1.0x
/// </summary>
public static class AffinityCalculator
{
    // 상성 배율
    public const float ADVANTAGE_MULTIPLIER = 1.2f;
    public const float DISADVANTAGE_MULTIPLIER = 0.6f;
    public const float NEUTRAL_MULTIPLIER = 1.0f;

    /// <summary>
    /// 공격자 장르와 타겟 장르를 비교하여 상성 배율 반환
    /// </summary>
    /// <param name="attackerGenre">공격자 장르</param>
    /// <param name="targetGenre">타겟 장르</param>
    /// <returns>데미지 배율 (1.2x, 0.6x, 1.0x)</returns>
    public static float GetMultiplier(Genre attackerGenre, Genre targetGenre)
    {
        // 동일 장르 = 중립
        if (attackerGenre == targetGenre)
            return NEUTRAL_MULTIPLIER;

        // 상성 관계 체크 (시계방향: 로맨스→공포→추리→모험→코믹→로맨스)
        if (IsAdvantage(attackerGenre, targetGenre))
            return ADVANTAGE_MULTIPLIER;

        // 역상성 관계 체크 (반시계방향)
        if (IsDisadvantage(attackerGenre, targetGenre))
            return DISADVANTAGE_MULTIPLIER;

        // 비인접 = 중립
        return NEUTRAL_MULTIPLIER;
    }

    /// <summary>
    /// 공격자가 타겟에게 상성인지 확인
    /// 상성 순환: Romance(2)→Horror(1)→Mystery(5)→Adventure(3)→Comedy(4)→Romance(2)
    /// </summary>
    private static bool IsAdvantage(Genre attacker, Genre target)
    {
        return attacker switch
        {
            Genre.Romance => target == Genre.Horror,      // 로맨스 → 공포
            Genre.Horror => target == Genre.Mystery,      // 공포 → 추리
            Genre.Mystery => target == Genre.Adventure,   // 추리 → 모험
            Genre.Adventure => target == Genre.Comedy,    // 모험 → 코믹
            Genre.Comedy => target == Genre.Romance,      // 코믹 → 로맨스
            _ => false
        };
    }

    /// <summary>
    /// 공격자가 타겟에게 역상성인지 확인
    /// 역상성 순환: Romance(2)←Horror(1)←Mystery(5)←Adventure(3)←Comedy(4)←Romance(2)
    /// </summary>
    private static bool IsDisadvantage(Genre attacker, Genre target)
    {
        return attacker switch
        {
            Genre.Romance => target == Genre.Comedy,      // 로맨스 ← 코믹
            Genre.Horror => target == Genre.Romance,      // 공포 ← 로맨스
            Genre.Mystery => target == Genre.Horror,      // 추리 ← 공포
            Genre.Adventure => target == Genre.Mystery,   // 모험 ← 추리
            Genre.Comedy => target == Genre.Adventure,    // 코믹 ← 모험
            _ => false
        };
    }

    /// <summary>
    /// 상성 관계 문자열 반환 (디버그용)
    /// </summary>
    public static string GetRelationship(Genre attacker, Genre target)
    {
        float multiplier = GetMultiplier(attacker, target);
        return multiplier switch
        {
            ADVANTAGE_MULTIPLIER => "상성",
            DISADVANTAGE_MULTIPLIER => "역상성",
            _ => "중립"
        };
    }
}
