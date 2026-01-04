using UnityEngine;

/// <summary>
/// 서버 시간 통합 테스트용 스크립트
/// Hierarchy에 GameObject 생성 후 이 컴포넌트 추가
/// </summary>
public class ServerTimeTest : MonoBehaviour
{
    [Header("AP 테스트")]
    [SerializeField] private int testAPAmount = 20;

    [Header("시간 점프 테스트")]
    [SerializeField] private int jumpHours = 1;

    [Header("파견 시스템")]
    [SerializeField] private Dispatch.DispatchManager dispatchManager;

    [ContextMenu("1. AP를 20으로 설정")]
    private void SetAPTo20()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.SetCurrency(CurrencyManager.AP_ID, testAPAmount, true);
            Debug.Log($"<color=green>AP를 {testAPAmount}으로 설정했습니다.</color>");
        }
    }

    [ContextMenu("2. 현재 AP 및 회복 시간 확인")]
    private void CheckAPStatus()
    {
        if (CurrencyManager.Instance == null) return;

        int currentAP = CurrencyManager.Instance.GetCurrency(CurrencyManager.AP_ID);
        int maxAP = CurrencyManager.Instance.GetMaxAP();
        float remainingTime = CurrencyManager.Instance.GetAPRecoveryRemainingTime();

        Debug.Log($"<color=cyan>=== AP 상태 ===</color>");
        Debug.Log($"현재 AP: {currentAP}/{maxAP}");
        Debug.Log($"다음 회복까지: {remainingTime:F1}초");
    }

    [ContextMenu("3. 서버 시간 1시간 점프 (디버그 모드 필요)")]
    private void Jump1Hour()
    {
        if (ServerTimeManager.Instance != null)
        {
            ServerTimeManager.Instance.DebugAddHours(jumpHours);
            Debug.Log($"<color=yellow>시간을 {jumpHours}시간 앞으로 점프했습니다! (AP +{jumpHours * 4} 예상)</color>");
            Debug.Log($"<color=yellow>약 1초 후 AP가 회복됩니다.</color>");
        }
        else
        {
            Debug.LogWarning("ServerTimeManager가 없습니다!");
        }
    }

    [ContextMenu("4. 시간 점프 초기화")]
    private void ResetTimeJump()
    {
        if (ServerTimeManager.Instance != null)
        {
            ServerTimeManager.Instance.DebugResetTimeOffset();
            Debug.Log("<color=green>시간 오프셋 초기화 완료!</color>");
        }
    }

    [ContextMenu("5. 서버 시간 확인")]
    private void CheckServerTime()
    {
        if (ServerTimeManager.Instance != null && ServerTimeManager.Instance.IsSynced)
        {
            var serverTime = ServerTimeManager.Instance.GetServerDateTime();
            Debug.Log($"<color=cyan>=== 서버 시간 ===</color>");
            Debug.Log($"현재 서버 시간: {serverTime:yyyy-MM-dd HH:mm:ss} (UTC)");
            Debug.Log($"밀리초: {ServerTimeManager.Instance.GetServerTimeMs()}");
        }
        else
        {
            Debug.LogWarning("ServerTimeManager가 동기화되지 않았습니다!");
        }
    }

    [ContextMenu("6. 파견 시스템 모드 확인")]
    private void CheckDispatchMode()
    {
        if (dispatchManager != null)
        {
            Debug.Log("<color=cyan>=== 파견 시스템 상태 ===</color>");
            Debug.Log("DispatchManager가 활성화되어 있습니다.");
            Debug.Log("Inspector에서 Use Server Time 확인하세요.");
        }
        else
        {
            Debug.LogWarning("DispatchManager가 Inspector에서 연결되지 않았습니다!");
        }
    }

    [ContextMenu("7. 종합 테스트 실행")]
    private void RunFullTest()
    {
        Debug.Log("<color=yellow>========== 서버 시간 통합 테스트 시작 ==========</color>");

        CheckServerTime();
        CheckAPStatus();
        CheckDispatchMode();

        Debug.Log("<color=yellow>========== 테스트 완료 ==========</color>");
        Debug.Log("<color=green>ContextMenu에서 개별 테스트를 실행하세요:</color>");
        Debug.Log("1. AP를 20으로 설정");
        Debug.Log("2. 현재 AP 및 회복 시간 확인");
        Debug.Log("3. 서버 시간 1시간 점프");
    }
}
