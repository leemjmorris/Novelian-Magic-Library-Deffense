using UnityEngine;
using Firebase.Data;

namespace Dispatch
{
    /// <summary>
    /// 파견 상태 확인을 위한 유틸리티 클래스
    /// Firebase 데이터 기반으로 동작
    /// </summary>
    public static class DispatchStateHelper
    {
        /// <summary>
        /// 파견 상태 저장 데이터 구조 (레거시 호환용)
        /// </summary>
        [System.Serializable]
        public class DispatchSaveData
        {
            public bool isDispatching;
            public float totalDispatchTime;
            public string startTimeString;
            public int selectedLocation;
            public int selectedHours;
            public int selectedTimeID;
        }

        /// <summary>
        /// 파견 완료 여부 확인 (전투형 또는 채집형 중 하나라도 완료되면 true)
        /// </summary>
        public static bool IsDispatchCompleted()
        {
            return IsCombatDispatchCompleted() || IsGatheringDispatchCompleted();
        }

        /// <summary>
        /// 전투형 파견 완료 여부 확인
        /// </summary>
        public static bool IsCombatDispatchCompleted()
        {
            var state = GetCombatDispatchState();
            return IsDispatchStateCompleted(state);
        }

        /// <summary>
        /// 채집형 파견 완료 여부 확인
        /// </summary>
        public static bool IsGatheringDispatchCompleted()
        {
            var state = GetGatheringDispatchState();
            return IsDispatchStateCompleted(state);
        }

        /// <summary>
        /// 파견 상태가 완료되었는지 확인
        /// </summary>
        private static bool IsDispatchStateCompleted(DispatchStateData state)
        {
            if (state == null || !state.isActive)
                return false;

            if (string.IsNullOrEmpty(state.endTime))
                return false;

            if (!System.DateTime.TryParse(state.endTime, out System.DateTime endTime))
                return false;

            return System.DateTime.UtcNow >= endTime;
        }

        /// <summary>
        /// 남은 파견 시간 계산 (초 단위)
        /// </summary>
        public static float GetRemainingTime()
        {
            float combatRemaining = GetRemainingTimeForState(GetCombatDispatchState());
            float gatheringRemaining = GetRemainingTimeForState(GetGatheringDispatchState());

            if (combatRemaining >= 0f && gatheringRemaining >= 0f)
                return Mathf.Min(combatRemaining, gatheringRemaining);
            if (combatRemaining >= 0f) return combatRemaining;
            if (gatheringRemaining >= 0f) return gatheringRemaining;

            return -1f;
        }

        /// <summary>
        /// 전투형 파견 남은 시간
        /// </summary>
        public static float GetCombatRemainingTime()
        {
            return GetRemainingTimeForState(GetCombatDispatchState());
        }

        /// <summary>
        /// 채집형 파견 남은 시간
        /// </summary>
        public static float GetGatheringRemainingTime()
        {
            return GetRemainingTimeForState(GetGatheringDispatchState());
        }

        /// <summary>
        /// 특정 상태의 남은 파견 시간 계산 (초 단위)
        /// </summary>
        private static float GetRemainingTimeForState(DispatchStateData state)
        {
            if (state == null || !state.isActive)
                return -1f;

            if (string.IsNullOrEmpty(state.endTime))
                return -1f;

            if (!System.DateTime.TryParse(state.endTime, out System.DateTime endTime))
                return -1f;

            System.TimeSpan remaining = endTime - System.DateTime.UtcNow;
            return Mathf.Max(0f, (float)remaining.TotalSeconds);
        }

        /// <summary>
        /// 파견 중인지 확인
        /// </summary>
        public static bool IsDispatching()
        {
            return IsCombatDispatching() || IsGatheringDispatching();
        }

        /// <summary>
        /// 전투형 파견 중인지 확인
        /// </summary>
        public static bool IsCombatDispatching()
        {
            var state = GetCombatDispatchState();
            return state != null && state.isActive;
        }

        /// <summary>
        /// 채집형 파견 중인지 확인
        /// </summary>
        public static bool IsGatheringDispatching()
        {
            var state = GetGatheringDispatchState();
            return state != null && state.isActive;
        }

        /// <summary>
        /// 전투형 파견 상태 가져오기
        /// </summary>
        public static DispatchStateData GetCombatDispatchState()
        {
            return FirebaseSaveManager.Instance?.CachedData?.dispatch?.combat;
        }

        /// <summary>
        /// 채집형 파견 상태 가져오기
        /// </summary>
        public static DispatchStateData GetGatheringDispatchState()
        {
            return FirebaseSaveManager.Instance?.CachedData?.dispatch?.gathering;
        }

        /// <summary>
        /// Firebase 데이터를 레거시 DispatchSaveData 형식으로 변환
        /// </summary>
        public static DispatchSaveData ConvertToLegacyFormat(DispatchStateData state)
        {
            if (state == null)
                return null;

            return new DispatchSaveData
            {
                isDispatching = state.isActive,
                totalDispatchTime = state.hours * 3600f,
                startTimeString = state.startTime,
                selectedLocation = state.locationId,
                selectedHours = state.hours,
                selectedTimeID = state.hours // 시간 ID는 hours와 동일하게 사용
            };
        }
    }
}
