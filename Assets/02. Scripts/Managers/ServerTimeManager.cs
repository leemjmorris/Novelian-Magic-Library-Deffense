using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Firebase.Database;

/// <summary>
/// Firebase 서버 시간을 관리하는 싱글톤 매니저
/// 클라이언트 시간 조작 방지를 위해 모든 시간 기반 시스템은 이 매니저를 통해 시간을 조회해야 함
/// </summary>
public class ServerTimeManager : MonoBehaviour
{
    private const string LOG_PREFIX = "<color=#FFD700>[ServerTime]</color>";
    private const float SYNC_TIMEOUT_SECONDS = 10f;
    private const long MAX_OFFLINE_MS = 24 * 60 * 60 * 1000; // 24시간 (밀리초)

    private static ServerTimeManager instance;
    public static ServerTimeManager Instance => instance;

    /// <summary>
    /// 서버-클라이언트 시간 오프셋 (밀리초)
    /// serverTime = clientTime + offset
    /// </summary>
    private long serverTimeOffsetMs = 0;

    /// <summary>
    /// 동기화 완료 여부
    /// </summary>
    private bool isSynced = false;

    /// <summary>
    /// 동기화 성공 여부
    /// </summary>
    public bool IsSynced => isSynced;

    /// <summary>
    /// 마지막 동기화 시간 (로컬 기준)
    /// </summary>
    private DateTime lastSyncTime;

    /// <summary>
    /// 디버그 모드에서 시간 오프셋 (테스트용)
    /// </summary>
    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private long debugTimeOffsetMs = 0;

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

    /// <summary>
    /// 서버 시간 동기화 초기화
    /// BootScene에서 FirebaseSaveManager 초기화 직후에 호출
    /// </summary>
    /// <returns>동기화 성공 여부</returns>
    public async UniTask<bool> InitializeAsync()
    {
        GameLog.Log($"{LOG_PREFIX} 서버 시간 동기화 시작...");

        try
        {
            // Firebase .info/serverTimeOffset을 통해 서버-클라이언트 시간차 계산
            var offsetRef = FirebaseDatabase.DefaultInstance.GetReference(".info/serverTimeOffset");

            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(SYNC_TIMEOUT_SECONDS));
            var syncTask = GetServerTimeOffsetAsync(offsetRef);

            // 타임아웃과 동기화 작업 중 먼저 완료되는 것 확인
            var result = await UniTask.WhenAny(syncTask, timeoutTask);

            if (result == 0) // syncTask가 먼저 완료
            {
                isSynced = true;
                lastSyncTime = DateTime.UtcNow;
                GameLog.Log($"{LOG_PREFIX} 서버 시간 동기화 성공! 오프셋: {serverTimeOffsetMs}ms");
                GameLog.Log($"{LOG_PREFIX} 현재 서버 시간: {GetServerDateTime():yyyy-MM-dd HH:mm:ss} (UTC)");
                return true;
            }
            else // 타임아웃
            {
                GameLog.LogError($"{LOG_PREFIX} 서버 시간 동기화 타임아웃 ({SYNC_TIMEOUT_SECONDS}초)");
                return false;
            }
        }
        catch (Exception e)
        {
            GameLog.LogError($"{LOG_PREFIX} 서버 시간 동기화 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Firebase에서 서버 시간 오프셋 가져오기
    /// </summary>
    private async UniTask GetServerTimeOffsetAsync(DatabaseReference offsetRef)
    {
        var snapshot = await offsetRef.GetValueAsync();

        if (snapshot.Exists && snapshot.Value != null)
        {
            serverTimeOffsetMs = Convert.ToInt64(snapshot.Value);
        }
        else
        {
            // 오프셋을 가져오지 못한 경우 0으로 설정 (클라이언트 시간 사용)
            serverTimeOffsetMs = 0;
            GameLog.LogWarning($"{LOG_PREFIX} 서버 오프셋을 가져오지 못함. 클라이언트 시간 사용.");
        }
    }

    /// <summary>
    /// 현재 서버 시간 반환 (밀리초, Unix Epoch 기준)
    /// </summary>
    public long GetServerTimeMs()
    {
        if (!isSynced)
        {
            GameLog.LogWarning($"{LOG_PREFIX} 서버 시간이 동기화되지 않았습니다!");
            return 0;
        }

        long clientTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long serverTimeMs = clientTimeMs + serverTimeOffsetMs;

        // 디버그 모드에서 시간 오프셋 적용
        if (debugMode)
        {
            serverTimeMs += debugTimeOffsetMs;
        }

        return serverTimeMs;
    }

    /// <summary>
    /// 현재 서버 시간 반환 (DateTime)
    /// </summary>
    public DateTime GetServerDateTime()
    {
        long serverTimeMs = GetServerTimeMs();
        return DateTimeOffset.FromUnixTimeMilliseconds(serverTimeMs).UtcDateTime;
    }

    /// <summary>
    /// 특정 시간부터 현재까지의 경과 시간 계산 (밀리초)
    /// </summary>
    /// <param name="fromTimeMs">시작 시간 (밀리초)</param>
    /// <returns>경과 시간 (밀리초), 음수인 경우 0 반환</returns>
    public long GetElapsedMs(long fromTimeMs)
    {
        if (!isSynced || fromTimeMs <= 0)
        {
            return 0;
        }

        long currentTimeMs = GetServerTimeMs();
        long elapsedMs = currentTimeMs - fromTimeMs;

        // 시간이 역행한 경우 (시간 조작 의심) 0 반환
        if (elapsedMs < 0)
        {
            GameLog.LogWarning($"{LOG_PREFIX} 시간 역행 감지! from: {fromTimeMs}, current: {currentTimeMs}");
            return 0;
        }

        return elapsedMs;
    }

    /// <summary>
    /// 오프라인 경과 시간 계산 (최대 24시간 제한)
    /// </summary>
    /// <param name="lastSyncTimeMs">마지막 동기화 시간 (밀리초)</param>
    /// <returns>오프라인 경과 시간 (밀리초), 최대 24시간</returns>
    public long GetOfflineElapsedMs(long lastSyncTimeMs)
    {
        long elapsedMs = GetElapsedMs(lastSyncTimeMs);

        // 최대 24시간 제한
        if (elapsedMs > MAX_OFFLINE_MS)
        {
            GameLog.Log($"{LOG_PREFIX} 오프라인 시간 24시간 초과 ({elapsedMs / 3600000f:F1}시간). 24시간으로 제한.");
            return MAX_OFFLINE_MS;
        }

        return elapsedMs;
    }

    /// <summary>
    /// 특정 시간이 현재 서버 시간보다 과거인지 확인
    /// </summary>
    /// <param name="targetTimeMs">확인할 시간 (밀리초)</param>
    /// <returns>과거이면 true</returns>
    public bool IsPast(long targetTimeMs)
    {
        if (!isSynced || targetTimeMs <= 0)
        {
            return false;
        }

        return GetServerTimeMs() >= targetTimeMs;
    }

    /// <summary>
    /// 특정 시간까지 남은 시간 계산 (밀리초)
    /// </summary>
    /// <param name="targetTimeMs">목표 시간 (밀리초)</param>
    /// <returns>남은 시간 (밀리초), 이미 지났으면 0</returns>
    public long GetRemainingMs(long targetTimeMs)
    {
        if (!isSynced || targetTimeMs <= 0)
        {
            return 0;
        }

        long remainingMs = targetTimeMs - GetServerTimeMs();
        return remainingMs > 0 ? remainingMs : 0;
    }

    /// <summary>
    /// 시간 유효성 검증 (저장된 시간이 현재보다 미래면 false)
    /// </summary>
    /// <param name="savedTimeMs">저장된 시간 (밀리초)</param>
    /// <returns>유효하면 true</returns>
    public bool IsValidSavedTime(long savedTimeMs)
    {
        if (!isSynced)
        {
            return false;
        }

        // 저장된 시간이 0이면 유효 (초기값)
        if (savedTimeMs == 0)
        {
            return true;
        }

        long currentTimeMs = GetServerTimeMs();

        // 저장된 시간이 현재보다 미래면 무효 (시간 조작 의심)
        if (savedTimeMs > currentTimeMs)
        {
            GameLog.LogWarning($"{LOG_PREFIX} 시간 조작 감지! 저장된 시간({savedTimeMs})이 현재({currentTimeMs})보다 미래입니다.");
            return false;
        }

        return true;
    }

    #region Debug Methods

    /// <summary>
    /// [디버그] 시간 오프셋 설정 (테스트용)
    /// </summary>
    public void SetDebugTimeOffset(long offsetMs)
    {
        if (!debugMode)
        {
            GameLog.LogWarning($"{LOG_PREFIX} 디버그 모드가 아닙니다.");
            return;
        }

        debugTimeOffsetMs = offsetMs;
        GameLog.Log($"{LOG_PREFIX} 디버그 시간 오프셋 설정: {offsetMs}ms ({offsetMs / 3600000f:F1}시간)");
    }

    /// <summary>
    /// [디버그] 시간을 N시간 뒤로 이동
    /// </summary>
    public void DebugAddHours(int hours)
    {
        SetDebugTimeOffset(debugTimeOffsetMs + (hours * 60 * 60 * 1000L));
    }

    /// <summary>
    /// [디버그] 시간 오프셋 초기화
    /// </summary>
    public void DebugResetTimeOffset()
    {
        debugTimeOffsetMs = 0;
        GameLog.Log($"{LOG_PREFIX} 디버그 시간 오프셋 초기화");
    }

    [ContextMenu("현재 서버 시간 출력")]
    private void PrintServerTime()
    {
        if (isSynced)
        {
            GameLog.Log($"{LOG_PREFIX} 현재 서버 시간: {GetServerDateTime():yyyy-MM-dd HH:mm:ss} (UTC)");
            GameLog.Log($"{LOG_PREFIX} 서버 시간 (ms): {GetServerTimeMs()}");
        }
        else
        {
            GameLog.Log($"{LOG_PREFIX} 서버 시간이 동기화되지 않았습니다.");
        }
    }

    #endregion

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
