using UnityEngine;
using UnityEngine.UI;

public class BookMarkTest : MonoBehaviour
{
    [SerializeField] private Button test;
    [SerializeField] private Button add;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        add.onClick.AddListener(OnAddClick);
    }

    private void OnAddClick()
    {
        // ==================== 북마크 제작 재료 (Use_Type 1) ====================
        IngredientManager.Instance.AddIngredient(10101, 100); // 희미한 마력의 종이
        IngredientManager.Instance.AddIngredient(10102, 100); // 응축된 마력의 종이
        IngredientManager.Instance.AddIngredient(10103, 100); // 비범한 마력의 종이
        IngredientManager.Instance.AddIngredient(10104, 100); // 고대 마력의 종이
        IngredientManager.Instance.AddIngredient(10105, 100); // 신성한 마력의 종이
        IngredientManager.Instance.AddIngredient(10106, 100); // 잉크
        IngredientManager.Instance.AddIngredient(10114, 100); // 룬석

        // ==================== 캐릭터 강화 재료 (Use_Type 2) ====================
        // 장르별 정수
        IngredientManager.Instance.AddIngredient(10207, 1000); // 장르 정수 1
        IngredientManager.Instance.AddIngredient(10208, 1000); // 장르 정수 2
        IngredientManager.Instance.AddIngredient(10209, 1000); // 장르 정수 3
        IngredientManager.Instance.AddIngredient(10210, 1000); // 장르 정수 4
        IngredientManager.Instance.AddIngredient(10211, 1000); // 장르 정수 5
        IngredientManager.Instance.AddIngredient(10212, 100);  // 승진의 인장

        // 캐릭터별 정수 (20명)
        IngredientManager.Instance.AddIngredient(10215, 500); // 그믐의 정수
        IngredientManager.Instance.AddIngredient(10216, 500); // 테네브라의 정수
        IngredientManager.Instance.AddIngredient(10217, 500); // 위스퍼의 정수
        IngredientManager.Instance.AddIngredient(10218, 500); // 념령의 정수
        IngredientManager.Instance.AddIngredient(10219, 500); // 미르의 정수
        IngredientManager.Instance.AddIngredient(10220, 500); // 코르의 정수
        IngredientManager.Instance.AddIngredient(10221, 500); // 세린의 정수
        IngredientManager.Instance.AddIngredient(10222, 500); // 리브라의 정수
        IngredientManager.Instance.AddIngredient(10223, 500); // 누리의 정수
        IngredientManager.Instance.AddIngredient(10224, 500); // 이테르의 정수
        IngredientManager.Instance.AddIngredient(10225, 500); // 트레일의 정수
        IngredientManager.Instance.AddIngredient(10226, 500); // 루멘의 정수
        IngredientManager.Instance.AddIngredient(10227, 500); // 도담의 정수
        IngredientManager.Instance.AddIngredient(10228, 500); // 루도의 정수
        IngredientManager.Instance.AddIngredient(10229, 500); // 펀치의 정수
        IngredientManager.Instance.AddIngredient(10230, 500); // 토비의 정수
        IngredientManager.Instance.AddIngredient(10231, 500); // 이오의 정수
        IngredientManager.Instance.AddIngredient(10232, 500); // 라티오의 정수
        IngredientManager.Instance.AddIngredient(10233, 500); // 키의 정수
        IngredientManager.Instance.AddIngredient(10234, 500); // 베리타의 정수

        // ==================== 파티 시너지 강화 재료 (Use_Type 3) ====================
        IngredientManager.Instance.AddIngredient(10313, 100); // 파티 시너지 강화 재료

        // ==================== 재화 ====================
        CurrencyManager.Instance.AddGold(10000000);                                      // 골드 (1000만)
        CurrencyManager.Instance.AddCurrency(CurrencyManager.EXP_ID, 1000);              // 경험치
        CurrencyManager.Instance.AddCurrency(CurrencyManager.APPLICATION_ID, 1000);     // 지원서
        CurrencyManager.Instance.AddCurrency(CurrencyManager.RECOMMENDATION_ID, 1000);  // 추천서
        CurrencyManager.Instance.AddCurrency(CurrencyManager.MAGIC_STONE_ID, 1000);     // 마석
        CurrencyManager.Instance.AddCurrency(CurrencyManager.DUNGEON_PASS_ID, 100);    // 던전 출입증

        Debug.Log("모든 재료 및 재화 지급 완료!");
    }
}
