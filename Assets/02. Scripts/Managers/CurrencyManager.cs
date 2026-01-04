using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Data;
using Cysharp.Threading.Tasks;

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
    public const int DUNGEON_PASS_ID = 1606; // 던전 출입증
    public const int AP_ID = 1607;          // AP (행동력)

    // AP 회복 설정
    public const float AP_RECOVERY_INTERVAL_SECONDS = 900f; // 15분 = 900초
    public const long AP_RECOVERY_INTERVAL_MS = 900 * 1000; // 15분 (밀리초)
    private float apRecoveryTimer = 0f;
    private int maxAP = 30;

    // 마지막 AP 동기화 시간 (서버 시간, 밀리초)
    private long lastAPSyncTimeMs = 0;

    // 초기화 완료 여부 (중복 초기화 방지)
    private bool isInitialized = false;

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
        // 이미 초기화되었으면 스킵 (타이틀씬에서 재시작 시 기존 데이터 유지)
        if (isInitialized)
        {
            Debug.Log($"[CurrencyManager] 이미 초기화됨 - 기존 데이터 유지. 골드: {currencies[GOLD_ID]}, 던전출입증: {currencies[DUNGEON_PASS_ID]}");
            return;
        }

        // 모든 재화 초기화 (CurrencyTable 기준: 1601~1606)
        // TODO: 세이브/로드 시스템 구현 시 저장된 값으로 덮어쓰기
        currencies[GOLD_ID] = 0;         // 테스트용 초기 골드
        currencies[EXP_ID] = 0;             // 경험치
        currencies[APPLICATION_ID] = 0;     // 지원서
        currencies[RECOMMENDATION_ID] = 0;  // 추천서
        currencies[MAGIC_STONE_ID] = 0;     // 마석
        currencies[DUNGEON_PASS_ID] = 0;    // 던전 출입증
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

        isInitialized = true;
        Debug.Log($"[CurrencyManager] 초기화 완료. 재화 종류: {currencies.Count}개, 최대 AP: {maxAP}");
    }

    private void Update()
    {
        // 초기화 전이면 AP 회복 스킵
        if (currencies.Count == 0) return;

        UpdateAPRecovery();
    }

    private void UpdateAPRecovery()
    {
        // AP가 초기화되지 않았으면 스킵
        if (!currencies.ContainsKey(AP_ID)) return;

        int currentAP = currencies[AP_ID];

        // AP가 최대치면 회복 불필요
        if (currentAP >= maxAP)
        {
            apRecoveryTimer = 0f;
            // 서버 시간 사용 가능하면 동기화 시간 업데이트
            if (CanUseServerTime())
            {
                lastAPSyncTimeMs = ServerTimeManager.Instance.GetServerTimeMs();
            }
            return;
        }

        // 서버 시간 사용 가능 여부 확인
        if (CanUseServerTime())
        {
            UpdateAPRecoveryWithServerTime();
        }
        else
        {
            // Fallback: 기존 로컬 시간 방식 사용 (안전 모드)
            UpdateAPRecoveryWithLocalTime();
        }
    }

    /// <summary>
    /// 서버 시간 사용 가능 여부 확인
    /// </summary>
    private bool CanUseServerTime()
    {
        return ServerTimeManager.Instance != null && ServerTimeManager.Instance.IsSynced;
    }

    /// <summary>
    /// 서버 시간 기반 AP 회복 (메인 로직)
    /// </summary>
    private void UpdateAPRecoveryWithServerTime()
    {
        long currentServerTimeMs = ServerTimeManager.Instance.GetServerTimeMs();

        // 마지막 동기화 시간이 없으면 현재 시간으로 초기화
        if (lastAPSyncTimeMs <= 0)
        {
            lastAPSyncTimeMs = currentServerTimeMs;
            return;
        }

        // 시간 유효성 검증 (시간 조작 방지)
        if (currentServerTimeMs < lastAPSyncTimeMs)
        {
            Debug.LogWarning("[CurrencyManager] 서버 시간 역행 감지! Fallback으로 전환");
            UpdateAPRecoveryWithLocalTime();
            return;
        }

        // 경과 시간 계산
        long elapsedMs = currentServerTimeMs - lastAPSyncTimeMs;
        int recoveryCount = (int)(elapsedMs / AP_RECOVERY_INTERVAL_MS);

        if (recoveryCount > 0)
        {
            // AP 회복 (최대치 초과 방지)
            int actualRecovery = Mathf.Min(recoveryCount, maxAP - currencies[AP_ID]);
            if (actualRecovery > 0)
            {
                AddCurrency(AP_ID, actualRecovery);
                Debug.Log($"<color=cyan>[CurrencyManager]</color> AP 회복 (서버 시간)! +{actualRecovery}, 현재: {GetCurrency(AP_ID)}/{maxAP}");
            }

            // 마지막 동기화 시간 업데이트 (정확한 회복 시간으로)
            lastAPSyncTimeMs += recoveryCount * AP_RECOVERY_INTERVAL_MS;
        }
    }

    /// <summary>
    /// 로컬 시간 기반 AP 회복 (Fallback - 기존 로직 보존)
    /// ServerTimeManager 사용 불가 시 자동으로 이 로직 사용
    /// </summary>
    private void UpdateAPRecoveryWithLocalTime()
    {
        apRecoveryTimer += Time.deltaTime;

        if (apRecoveryTimer >= AP_RECOVERY_INTERVAL_SECONDS)
        {
            apRecoveryTimer = 0f;
            AddCurrency(AP_ID, 1);
            Debug.Log($"<color=yellow>[CurrencyManager]</color> AP 회복 (로컬 시간 Fallback)! 현재 AP: {GetCurrency(AP_ID)}/{maxAP}");
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

        // 서버 시간 사용 가능하면 서버 시간 기준 계산
        if (CanUseServerTime() && lastAPSyncTimeMs > 0)
        {
            long currentServerTimeMs = ServerTimeManager.Instance.GetServerTimeMs();
            long elapsedMs = currentServerTimeMs - lastAPSyncTimeMs;
            long remainingMs = AP_RECOVERY_INTERVAL_MS - (elapsedMs % AP_RECOVERY_INTERVAL_MS);
            return remainingMs / 1000f; // 밀리초 → 초 변환
        }

        // Fallback: 기존 로컬 시간 방식
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
        SaveToFirebase();

        // 경험치인 경우 레벨업 체크
        if (currencyId == EXP_ID)
        {
            CheckAndProcessLevelUp();
        }
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
        SaveToFirebase();
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
    public void SetCurrency(int currencyId, int amount, bool saveToFirebase = true)
    {
        if (amount < 0)
        {
            Debug.LogError($"[CurrencyManager] 음수 값으로 설정할 수 없습니다: {amount}");
            return;
        }

        currencies[currencyId] = amount;
        OnCurrencyChanged?.Invoke(currencyId, amount);

        if (saveToFirebase)
        {
            SaveToFirebase();
        }
    }

    /// <summary>
    /// Firebase 데이터로 재화 설정 (BootScene에서 호출)
    /// 오프라인 AP 회복 계산 포함
    /// </summary>
    public void SetCurrenciesFromFirebase(CurrencySaveData data)
    {
        if (data == null) return;

        currencies[GOLD_ID] = data.gold;
        currencies[EXP_ID] = data.exp;
        currencies[APPLICATION_ID] = data.application;
        currencies[RECOMMENDATION_ID] = data.recommendation;
        currencies[MAGIC_STONE_ID] = data.magicStone;
        currencies[DUNGEON_PASS_ID] = data.dungeonPass;

        // AP 오프라인 회복 계산
        int loadedAP = data.ap;
        lastAPSyncTimeMs = data.apLastSyncTimeMs;

        int offlineRecoveredAP = CalculateOfflineAPRecovery(loadedAP, lastAPSyncTimeMs);
        int finalAP = Mathf.Min(loadedAP + offlineRecoveredAP, maxAP); // 최대치 제한
        currencies[AP_ID] = finalAP;

        // 현재 서버 시간으로 동기화 시간 업데이트
        if (ServerTimeManager.Instance != null && ServerTimeManager.Instance.IsSynced)
        {
            lastAPSyncTimeMs = ServerTimeManager.Instance.GetServerTimeMs();
        }

        Debug.Log($"<color=#3EB489>[CurrencyManager]</color> Firebase에서 재화 로드 완료 - 골드: {data.gold}, AP: {loadedAP}→{finalAP} (오프라인 +{offlineRecoveredAP}), 던전출입증: {data.dungeonPass}");

        // 오프라인 회복이 있었거나 동기화 시간이 변경되었으면 Firebase에 저장
        if (offlineRecoveredAP > 0 || lastAPSyncTimeMs != data.apLastSyncTimeMs)
        {
            SaveToFirebase();
            Debug.Log($"<color=#3EB489>[CurrencyManager]</color> 동기화 시간 업데이트 저장: {lastAPSyncTimeMs}");
        }
    }

    /// <summary>
    /// 오프라인 AP 회복량 계산
    /// </summary>
    /// <param name="currentAP">현재 AP</param>
    /// <param name="lastSyncTimeMs">마지막 동기화 시간 (서버 시간, 밀리초)</param>
    /// <returns>회복할 AP 양</returns>
    private int CalculateOfflineAPRecovery(int currentAP, long lastSyncTimeMs)
    {
        // ServerTimeManager 확인
        if (ServerTimeManager.Instance == null || !ServerTimeManager.Instance.IsSynced)
        {
            Debug.LogWarning("[CurrencyManager] ServerTimeManager가 초기화되지 않아 오프라인 회복 계산 불가");
            return 0;
        }

        // 마지막 동기화 시간이 없으면 회복 없음
        if (lastSyncTimeMs <= 0)
        {
            Debug.Log("[CurrencyManager] 마지막 동기화 시간 없음 - 오프라인 회복 0");
            return 0;
        }

        // 시간 유효성 검증 (미래 시간이면 무효)
        if (!ServerTimeManager.Instance.IsValidSavedTime(lastSyncTimeMs))
        {
            Debug.LogWarning("[CurrencyManager] 저장된 시간이 유효하지 않음 - 오프라인 회복 0 (시간 조작 의심)");
            return 0;
        }

        // 이미 최대치면 회복 불필요
        if (currentAP >= maxAP)
        {
            return 0;
        }

        // 오프라인 경과 시간 계산 (최대 24시간)
        long offlineElapsedMs = ServerTimeManager.Instance.GetOfflineElapsedMs(lastSyncTimeMs);

        // 회복량 계산 (15분당 1 AP)
        int recoveryCount = (int)(offlineElapsedMs / AP_RECOVERY_INTERVAL_MS);

        // 최대치까지만 회복
        int maxRecovery = maxAP - currentAP;
        int actualRecovery = Mathf.Min(recoveryCount, maxRecovery);

        if (actualRecovery > 0)
        {
            float offlineHours = offlineElapsedMs / 3600000f;
            Debug.Log($"[CurrencyManager] 오프라인 AP 회복: {offlineHours:F1}시간 경과, {actualRecovery} AP 회복");
        }

        return actualRecovery;
    }

    /// <summary>
    /// 현재 재화 상태를 Firebase에 저장
    /// </summary>
    public void SaveToFirebase()
    {
        if (FirebaseSaveManager.Instance == null || !FirebaseSaveManager.Instance.IsInitialized)
        {
            return;
        }

        if (FirebaseManager.Instance == null || string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserId))
        {
            return;
        }

        // 서버 시간으로 동기화 시간 업데이트
        if (ServerTimeManager.Instance != null && ServerTimeManager.Instance.IsSynced)
        {
            lastAPSyncTimeMs = ServerTimeManager.Instance.GetServerTimeMs();
        }

        var data = new CurrencySaveData
        {
            gold = currencies[GOLD_ID],
            exp = currencies[EXP_ID],
            application = currencies[APPLICATION_ID],
            recommendation = currencies[RECOMMENDATION_ID],
            magicStone = currencies[MAGIC_STONE_ID],
            dungeonPass = currencies[DUNGEON_PASS_ID],
            ap = currencies[AP_ID],
            apLastSyncTimeMs = lastAPSyncTimeMs,
            apRecoveryTime = "" // 레거시 필드 (더 이상 사용 안 함)
        };

        FirebaseSaveManager.Instance.SaveCurrenciesAsync(FirebaseManager.Instance.CurrentUserId, data).Forget();
    }

    /// <summary>
    /// 재화 K/M 단위 축약 표기
    /// </summary>
    public static string FormatCurrency(int amount)
    {
        if (amount >= 1000000)
            return $"{amount / 1000000f:0.#}M";
        else if (amount >= 1000)
            return $"{amount / 1000f:0.#}K";
        else if(amount>=1000000000)
                return $"{amount / 1000000000f:0.#}B";
        else
            return amount.ToString();
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

    #region 레벨업 시스템

    // 레벨업 이벤트
    public event Action<int> OnLevelUp; // (newLevel)

    private const int PLAYER_LEVEL_TABLE_BASE = 700; // PlayerLevelTable ID 기준 (0701, 0702...)
    private const int MAX_PLAYER_LEVEL = 50;

    /// <summary>
    /// 경험치 획득 시 레벨업 체크 및 처리
    /// </summary>
    private void CheckAndProcessLevelUp()
    {
        if (FirebaseSaveManager.Instance?.CachedData?.progression == null)
        {
            Debug.LogWarning("[CurrencyManager] 캐시 데이터 없음 - 레벨업 체크 스킵");
            return;
        }

        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            Debug.LogWarning("[CurrencyManager] CSVLoader 미초기화 - 레벨업 체크 스킵");
            return;
        }

        var progression = FirebaseSaveManager.Instance.CachedData.progression;
        int currentLevel = progression.playerLevel;
        int totalExp = GetCurrency(EXP_ID); // 누적 경험치

        var levelTable = CSVLoader.Instance.GetTable<PlayerLevelData>();
        if (levelTable == null)
        {
            Debug.LogWarning("[CurrencyManager] PlayerLevelTable 로드 실패");
            return;
        }

        bool leveledUp = false;

        // 다중 레벨업 처리 (한번에 많은 경험치 획득 시)
        while (currentLevel < MAX_PLAYER_LEVEL)
        {
            int nextLevelId = PLAYER_LEVEL_TABLE_BASE + currentLevel + 1; // 702, 703...
            var nextLevelData = levelTable.GetId(nextLevelId);

            if (nextLevelData == null)
            {
                Debug.Log($"[CurrencyManager] 최대 레벨 도달 또는 테이블 데이터 없음 (ID: {nextLevelId})");
                break;
            }

            // 누적 경험치가 다음 레벨 필요량보다 적으면 중단
            if (totalExp < nextLevelData.Tot_EXP)
            {
                break;
            }

            // 레벨업!
            currentLevel++;
            leveledUp = true;
            Debug.Log($"[CurrencyManager] 레벨업! Lv{currentLevel - 1} → Lv{currentLevel} (누적 EXP: {totalExp})");

            OnLevelUp?.Invoke(currentLevel);
        }

        // 레벨이 변경되었으면 저장
        if (leveledUp)
        {
            progression.playerLevel = currentLevel;
            SavePlayerLevelAsync(progression).Forget();
        }
    }

    /// <summary>
    /// 플레이어 레벨 저장
    /// </summary>
    private async UniTaskVoid SavePlayerLevelAsync(ProgressionData progression)
    {
        if (FirebaseSaveManager.Instance == null) return;
        if (FirebaseManager.Instance == null || string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserId)) return;

        await FirebaseSaveManager.Instance.SaveProgressionAsync(FirebaseManager.Instance.CurrentUserId, progression);
    }

    /// <summary>
    /// 현재 플레이어 레벨 반환
    /// </summary>
    public int GetPlayerLevel()
    {
        return FirebaseSaveManager.Instance?.CachedData?.progression?.playerLevel ?? 1;
    }

    /// <summary>
    /// 다음 레벨까지 필요한 경험치 반환
    /// </summary>
    public int GetExpToNextLevel()
    {
        int currentLevel = GetPlayerLevel();
        if (currentLevel >= MAX_PLAYER_LEVEL) return 0;

        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit) return 0;

        int nextLevelId = PLAYER_LEVEL_TABLE_BASE + currentLevel + 1;
        var nextLevelData = CSVLoader.Instance.GetData<PlayerLevelData>(nextLevelId);
        if (nextLevelData == null) return 0;

        int currentExp = GetCurrency(EXP_ID);
        return Mathf.Max(0, (int)nextLevelData.Tot_EXP - currentExp);
    }

    /// <summary>
    /// 현재 레벨 진행률 반환 (0.0 ~ 1.0)
    /// </summary>
    public float GetLevelProgress()
    {
        int currentLevel = GetPlayerLevel();
        if (currentLevel >= MAX_PLAYER_LEVEL) return 1f;

        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit) return 0f;

        int currentLevelId = PLAYER_LEVEL_TABLE_BASE + currentLevel;
        int nextLevelId = PLAYER_LEVEL_TABLE_BASE + currentLevel + 1;

        var currentLevelData = CSVLoader.Instance.GetData<PlayerLevelData>(currentLevelId);
        var nextLevelData = CSVLoader.Instance.GetData<PlayerLevelData>(nextLevelId);

        if (currentLevelData == null || nextLevelData == null) return 0f;

        int currentExp = GetCurrency(EXP_ID);
        float prevTotExp = currentLevelData.Tot_EXP;
        float nextTotExp = nextLevelData.Tot_EXP;
        float reqExp = nextTotExp - prevTotExp;

        if (reqExp <= 0) return 1f;

        float progress = (currentExp - prevTotExp) / reqExp;
        return Mathf.Clamp01(progress);
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
