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
        [SerializeField] private GameObject damageTextPrefab;  // 훈련소 전용 데미지 폰트 (Inspector에서 할당)

        // 데미지 기록용 이벤트
        public static event System.Action<float> OnDamageTaken;

        // 상태
        private bool isActive = true;
        private bool hasFocusMark = false;
        private float markRemainingTime = 0f;

        // Animator 캐시
        private Animator _animator;
        private static readonly int PUSHED_HASH = Animator.StringToHash("pushed");

        #region ITargetable Implementation

        public Transform GetTransform() => transform;

        public Vector3 GetPosition() => transform.position;

        public bool IsAlive() => isActive && gameObject.activeInHierarchy;

        public float Weight => _weight;

        public void TakeDamage(float damage)
        {
            TakeDamage(damage, false);
        }

        public void TakeDamage(float damage, bool isCritical)
        {
            TakeDamageInternal(damage, isCritical);
        }

        /// <summary>
        /// 상성 시스템 적용 데미지 처리 (Issue #531)
        /// 훈련용 허수아비는 상성 무시 - 기본 데미지 그대로 적용
        /// </summary>
        public void TakeDamage(float damage, bool isCritical, Genre attackerGenre)
        {
            // 훈련용이므로 상성 무시, 기본 데미지 그대로 적용
            TakeDamageInternal(damage, isCritical);
        }

        private void TakeDamageInternal(float damage, bool isCritical)
        {
            if (!isActive) return;

            // 데미지 기록 이벤트 발생 (DPSCalculator가 구독)
            OnDamageTaken?.Invoke(damage);

            // 데미지 폰트 표시 (훈련소 전용 - 직접 Instantiate)
            if (damageTextPrefab != null)
            {
                Collider col = GetComponent<Collider>();
                Vector3 textPosition = col != null ? col.bounds.center : transform.position;
                textPosition += Vector3.up * 0.5f; // 약간 위로

                GameObject textObj = Instantiate(damageTextPrefab, textPosition, Quaternion.identity);
                var floatingText = textObj.GetComponent<FloatingDamageText>();
                if (floatingText != null)
                {
                    floatingText.OnSpawn();
                    floatingText.Initialize(damage, isCritical, false);
                }
            }

            // 피격 애니메이션 재생 (처음부터 재생하여 Exit Time 리셋)
            if (_animator != null && _animator.enabled)
            {
                _animator.Play(PUSHED_HASH, 0, 0f);
            }

            // 무한 체력이므로 죽지 않음
            string critText = isCritical ? " (CRITICAL!)" : "";
            Debug.Log($"[DummyTarget] Took {damage:F1} damage{critText} (subscribers: {OnDamageTaken?.GetInvocationList()?.Length ?? 0})");
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

        private void Awake()
        {
            // Monster 태그 설정 (타겟팅을 위해 필요)
            gameObject.tag = "Monster";

            // Monster 레이어 설정 (Projectile과 충돌 감지를 위해 필요)
            int monsterLayer = LayerMask.NameToLayer("Monster");
            if (monsterLayer >= 0)
            {
                gameObject.layer = monsterLayer;
                Debug.Log($"[DummyTarget] Layer set to Monster ({monsterLayer})");
            }
            else
            {
                Debug.LogWarning("[DummyTarget] Monster layer not found! Projectile collision may not work.");
            }

            // Monster/BossMonster 컴포넌트가 있으면 즉시 제거 (더미는 죽으면 안 됨)
            // DestroyImmediate 사용 - Destroy()는 프레임 끝에 제거되므로 그 전에 데미지 받아 죽을 수 있음
            var monster = GetComponent<Monster>();
            if (monster != null)
            {
                DestroyImmediate(monster);
                Debug.Log("[DummyTarget] Monster component removed immediately to prevent death");
            }

            var bossMonster = GetComponent<BossMonster>();
            if (bossMonster != null)
            {
                DestroyImmediate(bossMonster);
                Debug.Log("[DummyTarget] BossMonster component removed immediately to prevent death");
            }

            // Animator 캐시 (died 상태 전환 방지용)
            _animator = GetComponent<Animator>();
        }

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
