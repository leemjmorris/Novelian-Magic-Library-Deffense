using UnityEngine;

namespace Dispatch
{
    /// <summary>
    /// 파견 상태 확인을 위한 유틸리티 클래스
    /// </summary>
    public static class DispatchStateHelper
    {
        private const string DISPATCH_SAVE_KEY = "DispatchTestPanel_SaveData";

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
        /// 파견 완료 여부 확인
        /// </summary>
        /// <returns>파견 완료 상태면 true, 아니면 false</returns>
        public static bool IsDispatchCompleted()
        {
            // 저장된 파견 상태가 없으면 false
            if (!PlayerPrefs.HasKey(DISPATCH_SAVE_KEY))
            {
                return false;
            }

            // 파견 상태 데이터 로드
            string json = PlayerPrefs.GetString(DISPATCH_SAVE_KEY);
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
        /// 남은 파견 시간 계산 (초 단위)
        /// </summary>
        /// <returns>남은 시간 (초), 파견 중이 아니면 -1</returns>
        public static float GetRemainingTime()
        {
            if (!PlayerPrefs.HasKey(DISPATCH_SAVE_KEY))
            {
                return -1f;
            }

            string json = PlayerPrefs.GetString(DISPATCH_SAVE_KEY);
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
        /// 파견 중인지 확인
        /// </summary>
        /// <returns>파견 중이면 true, 아니면 false</returns>
        public static bool IsDispatching()
        {
            if (!PlayerPrefs.HasKey(DISPATCH_SAVE_KEY))
            {
                return false;
            }

            string json = PlayerPrefs.GetString(DISPATCH_SAVE_KEY);
            var saveData = JsonUtility.FromJson<DispatchSaveData>(json);

            return saveData != null && saveData.isDispatching;
        }
    }
}
