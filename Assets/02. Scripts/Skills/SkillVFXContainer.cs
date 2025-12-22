using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// VFX 컨테이너 - 외부 VFX 에셋을 감싸서 우리 코드에서 제어
    /// 외부 에셋 수정 없이 다양한 VFX 타입 지원
    /// 프리팹에 이 컴포넌트를 추가하고 자식으로 외부 VFX 에셋 배치
    /// </summary>
    public class SkillVFXContainer : MonoBehaviour
    {
        #region VFX Slots
        [Header("Required VFX")]
        [Tooltip("메인 활성 VFX (투사체, 빔, AOE 이펙트 등)")]
        [SerializeField] private GameObject activeVFX;

        [Header("Optional VFX")]
        [Tooltip("스킬 시작 시 재생되는 VFX (캐스팅 이펙트)")]
        [SerializeField] private GameObject spawnVFX;

        [Tooltip("피격/충돌 시 재생되는 VFX")]
        [SerializeField] private GameObject impactVFX;

        [Tooltip("스킬 종료 시 재생되는 VFX (소멸 이펙트)")]
        [SerializeField] private GameObject expireVFX;

        [Tooltip("이동 중 남기는 트레일 VFX")]
        [SerializeField] private GameObject trailVFX;

        [Tooltip("스킬 경고 표시 VFX (AOE 착지점 등)")]
        [SerializeField] private GameObject warningVFX;

        [Tooltip("지속 효과 루프 VFX (DOT, 버프 등)")]
        [SerializeField] private GameObject loopVFX;
        #endregion

        #region Settings
        [Header("Auto Cleanup")]
        [Tooltip("자동 파괴 시간 (0 = 수동 파괴)")]
        [SerializeField] private float autoDestroyTime = 0f;

        [Tooltip("스폰 VFX 자동 파괴 시간")]
        [SerializeField] private float spawnVFXLifetime = 1f;

        [Tooltip("피격 VFX 자동 파괴 시간")]
        [SerializeField] private float impactVFXLifetime = 2f;

        [Tooltip("소멸 VFX 자동 파괴 시간")]
        [SerializeField] private float expireVFXLifetime = 2f;
        #endregion

        #region Runtime State
        private bool isInitialized;
        private GameObject spawnedSpawnVFX;
        private GameObject spawnedImpactVFX;
        private GameObject spawnedExpireVFX;
        #endregion

        #region Public Properties
        public GameObject ActiveVFX => activeVFX;
        public GameObject SpawnVFX => spawnVFX;
        public GameObject ImpactVFX => impactVFX;
        public GameObject ExpireVFX => expireVFX;
        public GameObject TrailVFX => trailVFX;
        public GameObject WarningVFX => warningVFX;
        public GameObject LoopVFX => loopVFX;
        public bool IsInitialized => isInitialized;
        #endregion

        #region Initialization

        private void Start()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 컨테이너 초기화 - 외부에서 호출 가능
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            // Active VFX 활성화
            if (activeVFX != null)
            {
                activeVFX.SetActive(true);
            }

            // 자동 파괴 설정
            if (autoDestroyTime > 0)
            {
                Destroy(gameObject, autoDestroyTime);
            }

            isInitialized = true;
        }

        #endregion

        #region VFX Playback

        /// <summary>
        /// 스폰 VFX 재생 (위치 지정)
        /// </summary>
        public void PlaySpawnVFX(Vector3 position)
        {
            if (spawnVFX == null) return;

            spawnedSpawnVFX = Instantiate(spawnVFX, position, Quaternion.identity);
            Destroy(spawnedSpawnVFX, spawnVFXLifetime);
        }

        /// <summary>
        /// 스폰 VFX 재생 (현재 위치)
        /// </summary>
        public void PlaySpawnVFX()
        {
            PlaySpawnVFX(transform.position);
        }

        /// <summary>
        /// 피격 VFX 재생 (위치 지정)
        /// </summary>
        public void PlayImpactVFX(Vector3 position)
        {
            if (impactVFX == null) return;

            spawnedImpactVFX = Instantiate(impactVFX, position, Quaternion.identity);
            Destroy(spawnedImpactVFX, impactVFXLifetime);
        }

        /// <summary>
        /// 피격 VFX 재생 (현재 위치)
        /// </summary>
        public void PlayImpactVFX()
        {
            PlayImpactVFX(transform.position);
        }

        /// <summary>
        /// 소멸 VFX 재생 (위치 지정)
        /// </summary>
        public void PlayExpireVFX(Vector3 position)
        {
            if (expireVFX == null) return;

            spawnedExpireVFX = Instantiate(expireVFX, position, Quaternion.identity);
            Destroy(spawnedExpireVFX, expireVFXLifetime);
        }

        /// <summary>
        /// 소멸 VFX 재생 (현재 위치)
        /// </summary>
        public void PlayExpireVFX()
        {
            PlayExpireVFX(transform.position);
        }

        /// <summary>
        /// 경고 VFX 표시 (위치 지정)
        /// </summary>
        public GameObject ShowWarningVFX(Vector3 position)
        {
            if (warningVFX == null) return null;

            return Instantiate(warningVFX, position, Quaternion.identity);
        }

        /// <summary>
        /// 루프 VFX 활성화/비활성화
        /// </summary>
        public void SetLoopVFXActive(bool active)
        {
            if (loopVFX != null)
            {
                loopVFX.SetActive(active);
            }
        }

        /// <summary>
        /// 트레일 VFX 활성화/비활성화
        /// </summary>
        public void SetTrailVFXActive(bool active)
        {
            if (trailVFX != null)
            {
                trailVFX.SetActive(active);
            }
        }

        #endregion

        #region Active VFX Control

        /// <summary>
        /// Active VFX 활성화/비활성화
        /// </summary>
        public void SetActiveVFX(bool active)
        {
            if (activeVFX != null)
            {
                activeVFX.SetActive(active);
            }
        }

        /// <summary>
        /// 모든 VFX 비활성화
        /// </summary>
        public void DisableAllVFX()
        {
            if (activeVFX != null) activeVFX.SetActive(false);
            if (loopVFX != null) loopVFX.SetActive(false);
            if (trailVFX != null) trailVFX.SetActive(false);
        }

        #endregion

        #region Beam/Laser Support

        /// <summary>
        /// Hovl_Laser 컴포넌트 MaxLength 설정
        /// 외부 에셋 수정 없이 런타임에서 설정
        /// </summary>
        public void ConfigureLaserMaxLength(float maxLength)
        {
            var hovlLaser = GetComponentInChildren<Hovl_Laser>();
            var hovlLaser2 = GetComponentInChildren<Hovl_Laser2>();

            if (hovlLaser != null) hovlLaser.MaxLength = maxLength;
            if (hovlLaser2 != null) hovlLaser2.MaxLength = maxLength;
        }

        /// <summary>
        /// Hovl_Laser 종료 준비 호출
        /// </summary>
        public void PrepareLaserDisable()
        {
            var hovlLaser = GetComponentInChildren<Hovl_Laser>();
            var hovlLaser2 = GetComponentInChildren<Hovl_Laser2>();

            if (hovlLaser != null) hovlLaser.DisablePrepare();
            if (hovlLaser2 != null) hovlLaser2.DisablePrepare();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 컨테이너와 모든 VFX 정리
        /// 소멸 VFX가 있으면 재생 후 파괴
        /// </summary>
        public void Cleanup()
        {
            // 소멸 VFX 재생
            PlayExpireVFX();

            // 메인 VFX 비활성화
            DisableAllVFX();

            // 본체 파괴
            Destroy(gameObject);
        }

        /// <summary>
        /// 즉시 파괴 (VFX 없이)
        /// </summary>
        public void DestroyImmediate()
        {
            Destroy(gameObject);
        }

        #endregion
    }
}
