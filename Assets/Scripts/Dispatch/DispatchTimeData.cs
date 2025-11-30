using System;
using UnityEngine;

namespace Dispatch
{
    /// <summary>
    /// 파견 시간 데이터 (PDF 4페이지 참조)
    /// </summary>
    [Serializable]
    public class DispatchTimeData
    {
        [Header("파견 시간 정보")]
        public int hours;                    // 파견 시간 (4, 8, 12, 23)
        public string description;           // 설명
        public float rewardMultiplier;       // 보상 배율
        public int dailyLimit;               // 하루 횟수 제한

        public DispatchTimeData(int hours, string description, float multiplier, int dailyLimit)
        {
            this.hours = hours;
            this.description = description;
            this.rewardMultiplier = multiplier;
            this.dailyLimit = dailyLimit;
        }
    }

    /// <summary>
    /// 파견 시간 설정 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "DispatchTimeSettings", menuName = "Dispatch/Time Settings")]
    public class DispatchTimeSettings : ScriptableObject
    {
        [Header("파견 시간 옵션")]
        public DispatchTimeData[] timeOptions = new DispatchTimeData[]
        {
            new DispatchTimeData(4, "잠깐 보내도 얻을 건 있다", 1.0f, 6),
            new DispatchTimeData(8, "생활 루틴과 함께 돌리기 좋다", 1.8f, 3),
            new DispatchTimeData(12, "접속 간격이 길어도 손해 없음", 2.6f, 2),
            new DispatchTimeData(23, "하루 한 번은 꼭 돌리게 만드는 약한 압박", 5.0f, 1)
        };

        /// <summary>
        /// 시간별 데이터 가져오기
        /// </summary>
        public DispatchTimeData GetTimeData(int hours)
        {
            foreach (var data in timeOptions)
            {
                if (data.hours == hours)
                    return data;
            }
            Debug.LogWarning($"[DispatchTimeSettings] {hours}시간 데이터를 찾을 수 없습니다.");
            return timeOptions[0]; // 기본값: 4시간
        }
    }
}
