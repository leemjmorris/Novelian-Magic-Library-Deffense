using System;

namespace Dispatch
{
    /// <summary>
    /// Firebase 서버 시간 기반 시간 제공자 (실서비스용)
    /// ServerTimeManager를 통해 시간 조작 방지
    /// </summary>
    public class ServerTimeProvider : ITimeProvider
    {
        private const string LOG_PREFIX = "<color=#00CED1>[ServerTimeProvider]</color>";

        /// <summary>
        /// 현재 서버 시간 반환
        /// ServerTimeManager가 초기화되지 않았으면 로컬 시간 반환 (Fallback)
        /// </summary>
        public DateTime GetCurrentTime()
        {
            // ServerTimeManager 사용 가능 여부 확인
            if (ServerTimeManager.Instance != null && ServerTimeManager.Instance.IsSynced)
            {
                return ServerTimeManager.Instance.GetServerDateTime();
            }
            else
            {
                // Fallback: 로컬 시간 사용 (안전 모드)
                UnityEngine.Debug.LogWarning($"{LOG_PREFIX} ServerTimeManager 미동기화. 로컬 시간 사용 (Fallback)");
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 시작 시간으로부터 경과된 시간 반환
        /// </summary>
        public TimeSpan GetElapsedTime(DateTime startTime)
        {
            DateTime currentTime = GetCurrentTime();
            TimeSpan elapsed = currentTime - startTime;

            // 시간 역행 방지 (시간 조작 의심)
            if (elapsed.TotalSeconds < 0)
            {
                UnityEngine.Debug.LogWarning($"{LOG_PREFIX} 시간 역행 감지! 0으로 반환");
                return TimeSpan.Zero;
            }

            return elapsed;
        }

        /// <summary>
        /// ServerTimeManager가 사용 가능한지 확인
        /// </summary>
        public bool IsServerTimeSynced()
        {
            return ServerTimeManager.Instance != null && ServerTimeManager.Instance.IsSynced;
        }
    }
}
