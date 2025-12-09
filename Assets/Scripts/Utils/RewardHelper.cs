using UnityEngine;

/// <summary>
/// 보상 시스템용 Modifier 계산 헬퍼
/// BookmarkOptionTable.csv Option_Type 10, 11, 12 처리
/// - 10: 아이템 획득 확률 증가
/// - 11: 골드 획득량 증가
/// - 12: 파견 시간 감소
/// </summary>
public static class RewardHelper
{
    /// <summary>
    /// 아이템 획득 확률 계산 (Option_Type 10)
    /// </summary>
    /// <param name="baseProbability">기본 확률 (0.0 ~ 1.0)</param>
    /// <returns>modifier 적용된 확률 (최대 1.0)</returns>
    public static float CalculateItemDropRate(float baseProbability)
    {
        float modifier = BookMarkManager.Instance?.GetItemDropModifier() ?? 0f;
        float result = baseProbability * (1f + modifier);
        return Mathf.Clamp01(result);
    }

    /// <summary>
    /// 골드 획득량 계산 (Option_Type 11)
    /// </summary>
    /// <param name="baseAmount">기본 골드량</param>
    /// <returns>modifier 적용된 골드량</returns>
    public static int CalculateGoldAmount(int baseAmount)
    {
        float modifier = BookMarkManager.Instance?.GetGoldModifier() ?? 0f;
        return Mathf.FloorToInt(baseAmount * (1f + modifier));
    }

    /// <summary>
    /// 파견 시간 계산 (Option_Type 12)
    /// </summary>
    /// <param name="baseHours">기본 파견 시간 (시간 단위)</param>
    /// <returns>modifier 적용된 파견 시간 (감소)</returns>
    public static float CalculateDispatchTime(float baseHours)
    {
        float modifier = BookMarkManager.Instance?.GetDispatchTimeModifier() ?? 0f;
        // 감소이므로 빼기 (최소 10% 시간은 보장)
        float result = baseHours * (1f - modifier);
        return Mathf.Max(result, baseHours * 0.1f);
    }

    /// <summary>
    /// 보상 수량 계산 (아이템 타입에 따라 modifier 적용)
    /// 골드(1601)인 경우에만 골드 modifier 적용
    /// </summary>
    /// <param name="baseAmount">기본 수량</param>
    /// <param name="itemId">아이템 ID</param>
    /// <returns>modifier 적용된 수량</returns>
    public static int CalculateRewardAmount(int baseAmount, int itemId)
    {
        // 골드(1601)인 경우 골드 modifier 적용
        if (itemId == CurrencyManager.GOLD_ID)
        {
            return CalculateGoldAmount(baseAmount);
        }
        return baseAmount;
    }

    /// <summary>
    /// 보상 수량 계산 (float multiplier 포함)
    /// 파견 보상처럼 Reward_Multiplier가 있는 경우 사용
    /// </summary>
    /// <param name="baseAmount">기본 수량</param>
    /// <param name="multiplier">보상 배율</param>
    /// <param name="itemId">아이템 ID</param>
    /// <returns>modifier 적용된 수량</returns>
    public static int CalculateRewardAmount(int baseAmount, float multiplier, int itemId)
    {
        int multipliedAmount = Mathf.FloorToInt(baseAmount * multiplier);
        return CalculateRewardAmount(multipliedAmount, itemId);
    }
}
