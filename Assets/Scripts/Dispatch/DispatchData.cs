using System;
using UnityEngine;

namespace Dispatch
{
    /// <summary>
    /// 파견 타입 (채집형/전투형)
    /// </summary>
    public enum DispatchType
    {
        Collection,  // 채집형 (지상 파견)
        Combat       // 전투형 (지하 파견)
    }

    /// <summary>
    /// 파견 상태
    /// </summary>
    public enum DispatchStatus
    {
        None,        // 파견 없음
        InProgress,  // 파견 진행 중
        Completed    // 파견 완료 (보상 획득 대기)
    }

    /// <summary>
    /// 진행 중인 파견 정보
    /// </summary>
    [Serializable]
    public class ActiveDispatchInfo
    {
        public int locationId;                  // 파견 장소 ID
        public string locationName;             // 파견 장소 이름
        public DispatchType dispatchType;       // 파견 타입
        public int durationHours;               // 파견 시간
        public DateTime startTime;              // 시작 시간
        public DateTime endTime;                // 완료 예정 시간
        public DispatchStatus status;           // 현재 상태

        public ActiveDispatchInfo(int id, string name, DispatchType type, int hours, DateTime start, DateTime end)
        {
            locationId = id;
            locationName = name;
            dispatchType = type;
            durationHours = hours;
            startTime = start;
            endTime = end;
            status = DispatchStatus.InProgress;
        }

        /// <summary>
        /// 남은 시간 계산
        /// </summary>
        public TimeSpan GetRemainingTime(DateTime currentTime)
        {
            TimeSpan remaining = endTime - currentTime;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        /// <summary>
        /// 파견이 완료되었는지 확인
        /// </summary>
        public bool IsCompleted(DateTime currentTime)
        {
            return currentTime >= endTime;
        }
    }
}
