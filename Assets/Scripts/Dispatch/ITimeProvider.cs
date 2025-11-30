using System;

namespace Dispatch
{
    /// <summary>
    /// 시간 제공 인터페이스
    /// 실시간과 테스트용 시간을 추상화
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// 현재 시간 반환
        /// </summary>
        DateTime GetCurrentTime();

        /// <summary>
        /// 시작 시간으로부터 경과된 시간 반환
        /// </summary>
        TimeSpan GetElapsedTime(DateTime startTime);
    }
}
