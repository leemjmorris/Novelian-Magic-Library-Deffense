// 훈련소 허수아비 타겟 (Issue #458)
// 무한 체력, 피격 시 데미지 기록
namespace Novelian.Training
{
    using UnityEngine;

    /// <summary>
    /// 훈련소 허수아비 - 무한 체력의 타겟
    /// ITargetable 구현하여 캐릭터가 공격 가능
    /// </summary>
    public class DummyTarget : MonoBehaviour, ITargetable
    {
        [Header("Settings")]
        [SerializeField] private float _weight = 1f;

        // 데미지 기록용 이벤트
        public static event System.Action<float> OnDamageTaken;

        // 상태
        private bool isActive = true;
        private bool hasFocusMark = false;
        private float markRemainingTime = 0f;

        #region ITargetable Implementation

        public Transform GetTransform() => transform;

        public Vector3 GetPosition() => transform.position;

        public bool IsAlive() => isActive && gameObject.activeInHierarchy;

        public float Weight => _weight;

        public void TakeDamage(float damage)
        {
            if (!isActive) return;

            // 데미지 기록 이벤트 발생 (DPSCalculator가 구독)
            OnDamageTaken?.Invoke(damage);

            // 무한 체력이므로 죽지 않음
            // Debug.Log($"[DummyTarget] Took {damage} damage");
        }

        public bool HasFocusMark() => hasFocusMark;

        public float GetMarkRemainingTime() => hasFocusMark ? markRemainingTime : float.MaxValue;

        #endregion

        #region Public API

        /// <summary>
        /// 허수아비 활성화/비활성화
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);
        }

        /// <summary>
        /// 포커스 마크 설정
        /// </summary>
        public void SetFocusMark(bool marked, float duration = 0f)
        {
            hasFocusMark = marked;
            markRemainingTime = duration;
        }

        /// <summary>
        /// TargetRegistry에 등록
        /// </summary>
        public void Register()
        {
            if (TargetRegistry.Instance != null)
            {
                TargetRegistry.Instance.RegisterTarget(this);
            }
        }

        /// <summary>
        /// TargetRegistry에서 해제
        /// </summary>
        public void Unregister()
        {
            if (TargetRegistry.Instance != null)
            {
                TargetRegistry.Instance.UnregisterTarget(this);
            }
        }

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void Update()
        {
            // 마크 시간 감소
            if (hasFocusMark && markRemainingTime > 0f)
            {
                markRemainingTime -= Time.deltaTime;
                if (markRemainingTime <= 0f)
                {
                    hasFocusMark = false;
                    markRemainingTime = 0f;
                }
            }
        }

        #endregion
    }
}
