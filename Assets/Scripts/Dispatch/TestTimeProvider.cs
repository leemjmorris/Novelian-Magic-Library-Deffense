using System;
using UnityEngine;

namespace Dispatch
{
    /// <summary>
    /// 테스트용 시간 제공자 (시간 배속 조절 가능)
    /// </summary>
    public class TestTimeProvider : ITimeProvider
    {
        /// <summary>
        /// 시간 배율 (1.0 = 정상속도, 3600.0 = 1시간을 1초로)
        /// </summary>
        public float timeScale = 1.0f;

        private DateTime startRealTime;
        private DateTime startVirtualTime;

        public TestTimeProvider(float scale = 1.0f)
        {
            timeScale = scale;
            startRealTime = DateTime.Now;
            startVirtualTime = DateTime.Now;
        }

        public DateTime GetCurrentTime()
        {
            // 실제 경과 시간
            TimeSpan realElapsed = DateTime.Now - startRealTime;

            // 배속 적용
            TimeSpan virtualElapsed = TimeSpan.FromSeconds(realElapsed.TotalSeconds * timeScale);

            return startVirtualTime + virtualElapsed;
        }

        public TimeSpan GetElapsedTime(DateTime startTime)
        {
            return GetCurrentTime() - startTime;
        }

        /// <summary>
        /// 시간 배율 설정 (Inspector에서 조절 가능하도록)
        /// </summary>
        public void SetTimeScale(float scale)
        {
            timeScale = scale;
            Debug.Log($"[TestTimeProvider] 시간 배율 변경: x{scale}");
        }
    }
}
