using UnityEngine;

namespace Dispatch
{
    /// <summary>
    /// 파견 상태 확인을 위한 유틸리티 클래스
    /// </summary>
    public static class DispatchStateHelper
    {
        // 전투형/채집형 파견 저장 키
        private const string COMBAT_DISPATCH_SAVE_KEY = "CombatDispatch_SaveData";
        private const string GATHERING_DISPATCH_SAVE_KEY = "GatheringDispatch_SaveData";

        /// <summary>
        /// 파견 상태 저장 데이터 구조
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
        /// <returns>파견 완료 상태면 true, 아니면 false</returns>
        public static bool IsDispatchCompleted()
        {
            return IsDispatchCompleted(COMBAT_DISPATCH_SAVE_KEY) || IsDispatchCompleted(GATHERING_DISPATCH_SAVE_KEY);
        }

        /// <summary>
        /// 전투형 파견 완료 여부 확인
        /// </summary>
        public static bool IsCombatDispatchCompleted()
        {
            return IsDispatchCompleted(COMBAT_DISPATCH_SAVE_KEY);
        }

        /// <summary>
        /// 채집형 파견 완료 여부 확인
        /// </summary>
        public static bool IsGatheringDispatchCompleted()
        {
            return IsDispatchCompleted(GATHERING_DISPATCH_SAVE_KEY);
        }

        /// <summary>
        /// 파견 완료 여부 확인 (커스텀 키)
        /// </summary>
        /// <param name="saveKey">확인할 파견 저장 키</param>
        /// <returns>파견 완료 상태면 true, 아니면 false</returns>
        public static bool IsDispatchCompleted(string saveKey)
        {
            // 저장된 파견 상태가 없으면 false
            if (!PlayerPrefs.HasKey(saveKey))
            {
                return false;
            }

            // 파견 상태 데이터 로드
            string json = PlayerPrefs.GetString(saveKey);
            var saveData = JsonUtility.FromJson<DispatchSaveData>(json);

            if (saveData == null || !saveData.isDispatching)
            {
                return false;
            }

            // 시작 시간 파싱
            if (!System.DateTime.TryParse(saveData.startTimeString, out System.DateTime startTime))
            {
                return false;
            }

            // 경과 시간 계산
            System.TimeSpan elapsed = System.DateTime.Now - startTime;
            float elapsedSeconds = (float)elapsed.TotalSeconds;
            float remainingTime = saveData.totalDispatchTime - elapsedSeconds;

            // 파견 완료 여부 반환
            return remainingTime <= 0f;
        }

        /// <summary>
        /// 남은 파견 시간 계산 (초 단위) - 전투형/채집형 중 파견 중인 것의 남은 시간 반환
        /// </summary>
        /// <returns>남은 시간 (초), 파견 중이 아니면 -1</returns>
        public static float GetRemainingTime()
        {
            float combatRemaining = GetRemainingTime(COMBAT_DISPATCH_SAVE_KEY);
            float gatheringRemaining = GetRemainingTime(GATHERING_DISPATCH_SAVE_KEY);

            // 둘 다 파견 중이면 더 짧은 남은 시간 반환
            if (combatRemaining >= 0f && gatheringRemaining >= 0f)
            {
                return Mathf.Min(combatRemaining, gatheringRemaining);
            }
            // 하나만 파견 중이면 해당 시간 반환
            if (combatRemaining >= 0f) return combatRemaining;
            if (gatheringRemaining >= 0f) return gatheringRemaining;

            return -1f;
        }

        /// <summary>
        /// 특정 키의 남은 파견 시간 계산 (초 단위)
        /// </summary>
        private static float GetRemainingTime(string saveKey)
        {
            if (!PlayerPrefs.HasKey(saveKey))
            {
                return -1f;
            }

            string json = PlayerPrefs.GetString(saveKey);
            var saveData = JsonUtility.FromJson<DispatchSaveData>(json);

            if (saveData == null || !saveData.isDispatching)
            {
                return -1f;
            }

            if (!System.DateTime.TryParse(saveData.startTimeString, out System.DateTime startTime))
            {
                return -1f;
            }

            System.TimeSpan elapsed = System.DateTime.Now - startTime;
            float elapsedSeconds = (float)elapsed.TotalSeconds;
            float remainingTime = saveData.totalDispatchTime - elapsedSeconds;

            return Mathf.Max(0f, remainingTime);
        }

        /// <summary>
        /// 파견 중인지 확인 (전투형 또는 채집형 중 하나라도 파견 중이면 true)
        /// </summary>
        /// <returns>파견 중이면 true, 아니면 false</returns>
        public static bool IsDispatching()
        {
            return IsDispatchingByKey(COMBAT_DISPATCH_SAVE_KEY) || IsDispatchingByKey(GATHERING_DISPATCH_SAVE_KEY);
        }

        /// <summary>
        /// 특정 키의 파견 중 여부 확인
        /// </summary>
        private static bool IsDispatchingByKey(string saveKey)
        {
            if (!PlayerPrefs.HasKey(saveKey))
            {
                return false;
            }

            string json = PlayerPrefs.GetString(saveKey);
            var saveData = JsonUtility.FromJson<DispatchSaveData>(json);

            return saveData != null && saveData.isDispatching;
        }
    }
}
