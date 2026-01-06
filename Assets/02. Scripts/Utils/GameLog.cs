using System.Diagnostics;
using UnityEngine;

/// <summary>
/// 릴리즈 빌드에서 Debug.Log를 완전히 제거하는 래퍼 클래스
/// - UNITY_EDITOR 또는 DEVELOPMENT_BUILD에서만 동작
/// - Conditional 속성으로 IL 레벨에서 호출부 완전 제거
/// - 인스펙터에서 EnableLogging으로 런타임 토글 가능
/// </summary>
public static class GameLog
{
    /// <summary>
    /// 런타임에서 로깅 활성화/비활성화 (에디터/개발빌드 전용)
    /// GameLogSettings 컴포넌트에서 인스펙터로 제어
    /// </summary>
    public static bool EnableLogging { get; set; } = true;

    #region Log

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        if (!EnableLogging) return;
        UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, Object context)
    {
        if (!EnableLogging) return;
        UnityEngine.Debug.Log(message, context);
    }

    #endregion

    #region LogWarning

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        if (!EnableLogging) return;
        UnityEngine.Debug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, Object context)
    {
        if (!EnableLogging) return;
        UnityEngine.Debug.LogWarning(message, context);
    }

    #endregion

    #region LogError

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message)
    {
        if (!EnableLogging) return;
        UnityEngine.Debug.LogError(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message, Object context)
    {
        if (!EnableLogging) return;
        UnityEngine.Debug.LogError(message, context);
    }

    #endregion
}
