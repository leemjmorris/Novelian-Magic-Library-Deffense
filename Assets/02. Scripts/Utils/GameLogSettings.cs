using UnityEngine;

/// <summary>
/// 인스펙터에서 GameLog 활성화/비활성화를 제어하는 싱글톤 컴포넌트
/// 씬 전환 시에도 유지되며, 런타임에서 로그 출력 여부를 토글할 수 있음
///
/// 사용법:
/// 1. 빈 GameObject에 이 컴포넌트 추가 (첫 씬에서)
/// 2. 인스펙터에서 Enable Logging 체크박스로 on/off
/// 3. 씬 전환 시에도 설정이 유지됨
/// 4. 릴리즈 빌드에서는 Conditional로 인해 어차피 모든 로그가 제거됨
/// </summary>
public class GameLogSettings : MonoBehaviour
{
    public static GameLogSettings Instance { get; private set; }

    [Header("Debug Log Settings")]
    [Tooltip("체크 해제 시 모든 GameLog 출력이 비활성화됩니다.\n릴리즈 빌드에서는 이 설정과 무관하게 로그가 완전히 제거됩니다.")]
    [SerializeField] private bool enableLogging = true;

    [Header("Info (Read Only)")]
    [Tooltip("현재 빌드 타입")]
    [SerializeField] private string buildType = "Unknown";

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        UpdateBuildTypeInfo();
        ApplyLoggingSetting();
    }

    private void OnValidate()
    {
        // 인스펙터에서 값 변경 시 즉시 적용
        ApplyLoggingSetting();
        UpdateBuildTypeInfo();
    }

    private void ApplyLoggingSetting()
    {
        GameLog.EnableLogging = enableLogging;
    }

    private void UpdateBuildTypeInfo()
    {
#if UNITY_EDITOR
        buildType = "Unity Editor (로그 활성화)";
#elif DEVELOPMENT_BUILD
        buildType = "Development Build (로그 활성화)";
#else
        buildType = "Release Build (로그 완전 제거됨)";
#endif
    }

    /// <summary>
    /// 런타임에서 로깅 설정 변경
    /// </summary>
    public void SetLogging(bool enabled)
    {
        enableLogging = enabled;
        ApplyLoggingSetting();
    }

#if UNITY_EDITOR
    [ContextMenu("Toggle Logging")]
    private void ToggleLogging()
    {
        enableLogging = !enableLogging;
        ApplyLoggingSetting();
        UnityEngine.Debug.Log($"[GameLogSettings] Logging {(enableLogging ? "ENABLED" : "DISABLED")}");
    }

    [ContextMenu("Test Log Output")]
    private void TestLogOutput()
    {
        GameLog.Log("[GameLogSettings] Test Log - 이 메시지가 보이면 로그가 활성화된 상태입니다.");
        GameLog.LogWarning("[GameLogSettings] Test Warning");
        GameLog.LogError("[GameLogSettings] Test Error");
    }
#endif
}
