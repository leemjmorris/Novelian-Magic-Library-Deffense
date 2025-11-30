using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dispatch
{
    /// <summary>
    /// 파견 시스템 매니저
    /// </summary>
    public class DispatchManager : MonoBehaviour
    {
        [Header("시간 설정")]
        [SerializeField] private DispatchTimeSettings timeSettings;

        [Header("테스트 모드")]
        [SerializeField] private bool useTestMode = true;
        [SerializeField] private float testTimeScale = 900f; // 4시간(14400초) → 16초로 테스트

        private ITimeProvider timeProvider;
        private Dictionary<int, ActiveDispatchInfo> activeDispatches = new Dictionary<int, ActiveDispatchInfo>();

        private void Awake()
        {
            InitializeTimeProvider();
        }

        private void Update()
        {
            CheckDispatchProgress();
        }

        /// <summary>
        /// 타임 프로바이더 초기화
        /// </summary>
        private void InitializeTimeProvider()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (useTestMode)
            {
                var testProvider = new TestTimeProvider(testTimeScale);
                timeProvider = testProvider;
                Debug.Log($"<color=yellow>[DispatchManager] 테스트 모드 활성화 - 시간 배율: x{testTimeScale}</color>");
                Debug.Log($"<color=yellow>[DispatchManager] 4시간 파견 → 약 {14400f / testTimeScale:F1}초로 테스트</color>");
            }
            else
            {
                timeProvider = new RealTimeProvider();
                Debug.Log("[DispatchManager] 실시간 모드");
            }
#else
            timeProvider = new RealTimeProvider();
            Debug.Log("[DispatchManager] 실시간 모드 (빌드)");
#endif
        }

        /// <summary>
        /// 파견 시작
        /// </summary>
        public void StartDispatch(int locationId, string locationName, DispatchType type, int hours)
        {
            // timeSettings null 체크
            if (timeSettings == null)
            {
                Debug.LogError("[DispatchManager] DispatchTimeSettings가 할당되지 않았습니다! Inspector에서 할당해주세요.");
                return;
            }

            // 이미 해당 슬롯에 파견이 있는지 확인
            if (activeDispatches.ContainsKey(locationId))
            {
                Debug.LogWarning($"[DispatchManager] 이미 {locationName}에 파견이 진행 중입니다.");
                return;
            }

            DateTime startTime = timeProvider.GetCurrentTime();
            DateTime endTime = startTime.AddHours(hours);

            var dispatchInfo = new ActiveDispatchInfo(
                locationId,
                locationName,
                type,
                hours,
                startTime,
                endTime
            );

            activeDispatches[locationId] = dispatchInfo;

            var timeData = timeSettings.GetTimeData(hours);
            Debug.Log($"<color=cyan>[DispatchManager] 파견 시작!</color>\n" +
                      $"장소: {locationName}\n" +
                      $"타입: {(type == DispatchType.Collection ? "채집형" : "전투형")}\n" +
                      $"시간: {hours}시간 (배율: x{timeData.rewardMultiplier})\n" +
                      $"시작: {startTime:yyyy-MM-dd HH:mm:ss}\n" +
                      $"완료 예정: {endTime:yyyy-MM-dd HH:mm:ss}");

            if (useTestMode)
            {
                float realSeconds = (hours * 3600f) / testTimeScale;
                Debug.Log($"<color=yellow>[테스트] 실제 대기 시간: 약 {realSeconds:F1}초</color>");
            }
        }

        /// <summary>
        /// 파견 진행 상황 체크 (Update에서 호출)
        /// </summary>
        private void CheckDispatchProgress()
        {
            if (activeDispatches.Count == 0 || timeSettings == null) return;

            DateTime currentTime = timeProvider.GetCurrentTime();
            List<int> completedIds = new List<int>();

            foreach (var kvp in activeDispatches)
            {
                var dispatch = kvp.Value;

                if (dispatch.status == DispatchStatus.InProgress && dispatch.IsCompleted(currentTime))
                {
                    // 파견 완료!
                    dispatch.status = DispatchStatus.Completed;
                    completedIds.Add(kvp.Key);

                    var timeData = timeSettings.GetTimeData(dispatch.durationHours);
                    //Debug.Log($"<color=green>====================================</color>");
                    Debug.Log($"<color=green>[파견 완료!] {dispatch.durationHours}시간 파견 완료!</color>");
                    //Debug.Log($"<color=green>====================================</color>");
                    Debug.Log($"장소: {dispatch.locationName}\n" +
                              $"타입: {(dispatch.dispatchType == DispatchType.Collection ? "채집형" : "전투형")}\n" +
                              $"소요 시간: {dispatch.durationHours}시간\n" +
                              $"보상 배율: x{timeData.rewardMultiplier}\n" +
                              $"완료 시간: {currentTime:yyyy-MM-dd HH:mm:ss}");
                }
            }

            // 완료된 파견 제거 (보상 획득 후)
            foreach (var id in completedIds)
            {
                activeDispatches.Remove(id);
            }
        }

        /// <summary>
        /// 남은 시간 가져오기 (UI 업데이트용)
        /// </summary>
        public TimeSpan GetRemainingTime(int locationId)
        {
            if (activeDispatches.TryGetValue(locationId, out var dispatch))
            {
                DateTime currentTime = timeProvider.GetCurrentTime();
                return dispatch.GetRemainingTime(currentTime);
            }
            return TimeSpan.Zero;
        }

        /// <summary>
        /// 파견 상태 확인
        /// </summary>
        public DispatchStatus GetDispatchStatus(int locationId)
        {
            if (activeDispatches.TryGetValue(locationId, out var dispatch))
            {
                return dispatch.status;
            }
            return DispatchStatus.None;
        }

        /// <summary>
        /// 테스트: 시간 배율 변경 (런타임에서 조절 가능)
        /// </summary>
        public void SetTestTimeScale(float scale)
        {
            if (timeProvider is TestTimeProvider testProvider)
            {
                testProvider.SetTimeScale(scale);
                testTimeScale = scale;
            }
        }
    }
}
