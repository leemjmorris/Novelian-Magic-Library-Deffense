// 훈련소 DPS 계산기 (Issue #458)
// 실시간 DPS 및 누적 데미지 측정
namespace Novelian.Training
{
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// DPS 계산기 - 슬라이딩 윈도우 방식으로 실시간 DPS 계산
    /// </summary>
    public class DPSCalculator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("DPS 계산 윈도우 크기 (초)")]
        private float windowSize = 5f;

        // 데미지 기록
        private struct DamageRecord
        {
            public float time;
            public float damage;
        }

        private List<DamageRecord> damageRecords = new List<DamageRecord>();
        private float totalDamage = 0f;
        private float startTime = 0f;
        private bool isRunning = false;

        // 이벤트
        public event System.Action<float, float, float> OnStatsUpdated; // dps, totalDamage, elapsedTime

        #region Lifecycle

        private void OnEnable()
        {
            DummyTarget.OnDamageTaken += RecordDamage;
        }

        private void OnDisable()
        {
            DummyTarget.OnDamageTaken -= RecordDamage;
        }

        private void Update()
        {
            if (!isRunning) return;

            // 오래된 기록 제거 (슬라이딩 윈도우)
            float cutoffTime = Time.time - windowSize;
            damageRecords.RemoveAll(r => r.time < cutoffTime);

            // 통계 업데이트 이벤트 발생
            float dps = CalculateWindowDPS();
            float elapsed = Time.time - startTime;
            OnStatsUpdated?.Invoke(dps, totalDamage, elapsed);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 측정 시작
        /// </summary>
        public void StartMeasurement()
        {
            if (isRunning) return;

            isRunning = true;
            if (startTime == 0f)
            {
                startTime = Time.time;
            }
            GameLog.Log("[DPSCalculator] 측정 시작");
        }

        /// <summary>
        /// 측정 일시정지
        /// </summary>
        public void PauseMeasurement()
        {
            isRunning = false;
            GameLog.Log("[DPSCalculator] 측정 일시정지");
        }

        /// <summary>
        /// 측정 재시작 (일시정지 후)
        /// </summary>
        public void ResumeMeasurement()
        {
            if (isRunning) return;
            isRunning = true;
            GameLog.Log("[DPSCalculator] 측정 재시작");
        }

        /// <summary>
        /// 모든 데이터 초기화
        /// </summary>
        public void Reset()
        {
            damageRecords.Clear();
            totalDamage = 0f;
            startTime = 0f;

            // 리셋 후 통계 업데이트
            OnStatsUpdated?.Invoke(0f, 0f, 0f);
            GameLog.Log("[DPSCalculator] 리셋 완료");
        }

        /// <summary>
        /// 현재 실시간 DPS (슬라이딩 윈도우)
        /// </summary>
        public float GetCurrentDPS()
        {
            return CalculateWindowDPS();
        }

        /// <summary>
        /// 평균 DPS (전체 시간 기준)
        /// </summary>
        public float GetAverageDPS()
        {
            if (startTime == 0f) return 0f;
            float elapsed = Time.time - startTime;
            if (elapsed <= 0f) return 0f;
            return totalDamage / elapsed;
        }

        /// <summary>
        /// 누적 데미지
        /// </summary>
        public float GetTotalDamage()
        {
            return totalDamage;
        }

        /// <summary>
        /// 경과 시간
        /// </summary>
        public float GetElapsedTime()
        {
            if (startTime == 0f) return 0f;
            return Time.time - startTime;
        }

        /// <summary>
        /// 측정 중인지 여부
        /// </summary>
        public bool IsRunning => isRunning;

        #endregion

        #region Private Methods

        /// <summary>
        /// 데미지 기록 (DummyTarget.OnDamageTaken 이벤트 핸들러)
        /// </summary>
        private void RecordDamage(float damage)
        {
            if (!isRunning) return;

            totalDamage += damage;
            damageRecords.Add(new DamageRecord
            {
                time = Time.time,
                damage = damage
            });
        }

        /// <summary>
        /// 슬라이딩 윈도우 DPS 계산
        /// </summary>
        private float CalculateWindowDPS()
        {
            if (damageRecords.Count == 0) return 0f;

            float windowDamage = 0f;
            float cutoffTime = Time.time - windowSize;

            for (int i = 0; i < damageRecords.Count; i++)
            {
                if (damageRecords[i].time >= cutoffTime)
                {
                    windowDamage += damageRecords[i].damage;
                }
            }

            // 실제 윈도우 시간 계산 (시작 직후에는 windowSize보다 작을 수 있음)
            float actualWindow = Mathf.Min(windowSize, Time.time - startTime);
            if (actualWindow <= 0f) return 0f;

            return windowDamage / actualWindow;
        }

        #endregion
    }
}
