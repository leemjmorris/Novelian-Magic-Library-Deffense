using System;
using System.Collections.Generic;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    private static CurrencyManager instance;
    public static CurrencyManager Instance => instance;

    // 모든 재화를 관리하는 Dictionary (Key: Currency_ID, Value: 보유량)
    private Dictionary<int, int> currencies = new Dictionary<int, int>();

    // 재화 변경 이벤트 (UI 갱신용)
    public event Action<int, int> OnCurrencyChanged; // (currencyId, newAmount)

    // 재화 ID 상수
    public const int GOLD_ID = 1601;        // 골드
    public const int EXP_ID = 1602;         // 경험치
    public const int APPLICATION_ID = 1603; // 지원서
    public const int RECOMMENDATION_ID = 1604; // 추천서
    public const int MAGIC_STONE_ID = 1605; // 마석
    public const int AP_ID = 1607;          // AP (행동력)

    // AP 회복 설정
    public const float AP_RECOVERY_INTERVAL_SECONDS = 900f; // 15분 = 900초
    private float apRecoveryTimer = 0f;
    private int maxAP = 30;

    // 기존 Gold 호환용
    public int Gold => GetCurrency(GOLD_ID);

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        // 모든 재화 초기화 (CurrencyTable 기준: 1601~1606)
        // TODO: 세이브/로드 시스템 구현 시 저장된 값으로 덮어쓰기
        currencies[GOLD_ID] = 1000;         // 테스트용 초기 골드
        currencies[EXP_ID] = 0;             // 경험치
        currencies[APPLICATION_ID] = 0;     // 지원서
        currencies[RECOMMENDATION_ID] = 0;  // 추천서
        currencies[MAGIC_STONE_ID] = 0;     // 마석
        currencies[1606] = 0;               // 추가 재화 (StringTable 미등록)
        currencies[AP_ID] = 30;             // AP (테스트용 최대치 30)

        // CurrencyTable에서 최대 AP 조회
        if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            var currencyData = CSVLoader.Instance.GetData<CurrencyData>(AP_ID);
            if (currencyData != null && currencyData.Currency_Max_Count > 0)
            {
                maxAP = currencyData.Currency_Max_Count;
            }
        }

        Debug.Log($"[CurrencyManager] 초기화 완료. 재화 종류: {currencies.Count}개, 최대 AP: {maxAP}");
    }

    private void Update()
    {
        UpdateAPRecovery();
    }

    private void UpdateAPRecovery()
    {
        int currentAP = GetCurrency(AP_ID);

        // AP가 최대치면 회복 불필요 - 타이머 리셋
        if (currentAP >= maxAP)
        {
            apRecoveryTimer = 0f;
            return;
        }

        // AP가 최대치 미만이면 회복 타이머 작동
        apRecoveryTimer += Time.deltaTime;

        if (apRecoveryTimer >= AP_RECOVERY_INTERVAL_SECONDS)
        {
            apRecoveryTimer = 0f;
            AddCurrency(AP_ID, 1);
            Debug.Log($"[CurrencyManager] AP 회복! 현재 AP: {GetCurrency(AP_ID)}/{maxAP}");
        }
    }

    /// <summary>
    /// 다음 AP 회복까지 남은 시간 (초)
    /// AP가 최대치면 0 반환
    /// </summary>
    public float GetAPRecoveryRemainingTime()
    {
        if (GetCurrency(AP_ID) >= maxAP)
        {
            return 0f;
        }
        return AP_RECOVERY_INTERVAL_SECONDS - apRecoveryTimer;
    }

    /// <summary>
    /// 최대 AP 값 반환
    /// </summary>
    public int GetMaxAP()
    {
        return maxAP;
    }

    #region 범용 재화 API

    /// <summary>
    /// 재화 보유량 조회
    /// </summary>
    public int GetCurrency(int currencyId)
    {
        if (currencies.TryGetValue(currencyId, out int amount))
        {
            return amount;
        }
        Debug.LogWarning($"[CurrencyManager] 존재하지 않는 재화 ID: {currencyId}");
        return 0;
    }

    /// <summary>
    /// 재화 추가
    /// </summary>
    public void AddCurrency(int currencyId, int amount)
    {
        if (amount < 0)
        {
            Debug.LogError($"[CurrencyManager] 음수 값을 추가할 수 없습니다: {amount}");
            return;
        }

        if (!currencies.ContainsKey(currencyId))
        {
            Debug.LogWarning($"[CurrencyManager] 존재하지 않는 재화 ID: {currencyId}. 새로 생성합니다.");
            currencies[currencyId] = 0;
        }

        currencies[currencyId] += amount;

        string currencyName = GetCurrencyName(currencyId);
        Debug.Log($"[CurrencyManager] {currencyName} +{amount}. 현재: {currencies[currencyId]}");

        OnCurrencyChanged?.Invoke(currencyId, currencies[currencyId]);
    }

    /// <summary>
    /// 재화 소비 (충분한지 체크 후 차감)
    /// </summary>
    public bool SpendCurrency(int currencyId, int amount)
    {
        if (amount < 0)
        {
            Debug.LogError($"[CurrencyManager] 음수 값을 소비할 수 없습니다: {amount}");
            return false;
        }

        if (!HasEnoughCurrency(currencyId, amount))
        {
            string currencyName = GetCurrencyName(currencyId);
            Debug.LogWarning($"[CurrencyManager] {currencyName}이(가) 부족합니다. 필요: {amount}, 보유: {GetCurrency(currencyId)}");
            return false;
        }

        currencies[currencyId] -= amount;

        string name = GetCurrencyName(currencyId);
        Debug.Log($"[CurrencyManager] {name} -{amount}. 현재: {currencies[currencyId]}");

        OnCurrencyChanged?.Invoke(currencyId, currencies[currencyId]);
        return true;
    }

    /// <summary>
    /// 재화가 충분한지 확인
    /// </summary>
    public bool HasEnoughCurrency(int currencyId, int amount)
    {
        return GetCurrency(currencyId) >= amount;
    }

    /// <summary>
    /// 재화 직접 설정 (세이브/로드용)
    /// </summary>
    public void SetCurrency(int currencyId, int amount)
    {
        if (amount < 0)
        {
            Debug.LogError($"[CurrencyManager] 음수 값으로 설정할 수 없습니다: {amount}");
            return;
        }

        currencies[currencyId] = amount;
        OnCurrencyChanged?.Invoke(currencyId, amount);
    }

    /// <summary>
    /// 재화 이름 조회 (StringTable 연동)
    /// </summary>
    public string GetCurrencyName(int currencyId)
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            return $"Currency_{currencyId}";
        }

        var currencyData = CSVLoader.Instance.GetData<CurrencyData>(currencyId);
        if (currencyData == null)
        {
            return $"Currency_{currencyId}";
        }

        var stringData = CSVLoader.Instance.GetData<StringTable>(currencyData.Currency_Name_ID);
        return stringData?.Text ?? $"Currency_{currencyId}";
    }

    /// <summary>
    /// 모든 재화 정보 반환 (디버그/UI용)
    /// </summary>
    public Dictionary<int, int> GetAllCurrencies()
    {
        return new Dictionary<int, int>(currencies);
    }

    #endregion

    #region 기존 Gold API 호환

    public void AddGold(int amount)
    {
        AddCurrency(GOLD_ID, amount);
    }

    public bool SpendGold(int amount)
    {
        return SpendCurrency(GOLD_ID, amount);
    }

    #endregion

    #region Debug

    [ContextMenu("모든 재화 출력")]
    private void PrintAllCurrencies()
    {
        Debug.Log("=== 보유 재화 목록 ===");
        foreach (var kvp in currencies)
        {
            string name = GetCurrencyName(kvp.Key);
            Debug.Log($"{name} (ID: {kvp.Key}): {kvp.Value}");
        }
    }

    [ContextMenu("테스트 재화 지급")]
    private void AddTestCurrencies()
    {
        AddCurrency(GOLD_ID, 1000);
        AddCurrency(EXP_ID, 500);
        AddCurrency(APPLICATION_ID, 10);
        AddCurrency(RECOMMENDATION_ID, 5);
        AddCurrency(MAGIC_STONE_ID, 100);
    }

    #endregion
}
