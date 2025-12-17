using System;

namespace Dispatch
{
    /// <summary>
    /// 실제 시간 제공자 (실서비스용)
    /// </summary>
    public class RealTimeProvider : ITimeProvider
    {
        public DateTime GetCurrentTime()
        {
            // 실제 서비스에서는 서버 시간을 받아옴
            // 지금은 로컬 시간 사용
            return DateTime.Now;
        }

        public TimeSpan GetElapsedTime(DateTime startTime)
        {
            return GetCurrentTime() - startTime;
        }
    }
}
