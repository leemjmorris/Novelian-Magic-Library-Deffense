using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 실행기 - 메인 스킬 + 서포트 스킬 조합으로 이펙트 실행
    /// 프리팹은 SkillVFXDatabase에서 인스펙터 직접 참조
    /// 스탯 데이터는 CSV에서 관리
    /// </summary>
    public class SkillExecutor : MonoBehaviour
    {
        #region Singleton
        private static SkillExecutor _instance;
        public static SkillExecutor Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 씬에서 찾기 (Tag 사용)
                    GameObject obj = GameObject.FindWithTag("SkillExecutor");
                    if (obj != null)
                    {
                        _instance = obj.GetComponent<SkillExecutor>();
                    }

                    if (_instance == null)
                    {
                        Debug.LogError("[SkillExecutor] Instance not found in scene! SkillExecutor 태그가 있는 GameObject에 SkillExecutor 컴포넌트를 추가하세요.");
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        #endregion

        #region Inspector References
        [Header("VFX Database (인스펙터에서 직접 참조)")]
        [SerializeField] private SkillVFXDatabase vfxDatabase;
        #endregion

        #region Helper Methods
        /// <summary>
        /// 타겟의 조준점 위치 반환 (Collider 중심 또는 위치 + 1m)
        /// </summary>
        private Vector3 GetTargetAimPosition(ITargetable target)
        {
            if (target == null) return Vector3.zero;

            Vector3 pos = target.GetTransform().position;
            Collider col = target.GetTransform().GetComponent<Collider>();
            if (col != null)
            {
                return col.bounds.center;
            }
            return pos + Vector3.up * 1f;
        }
        #endregion

        #region Public API

        /// <summary>
        /// 스킬 실행 - 메인 스킬 데이터와 서포트 스킬 데이터 기반
        /// </summary>
        public void ExecuteSkill(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill = null)
        {
            ExecuteSkillAsync(caster, target, mainSkill, supportSkill).Forget();
        }

        /// <summary>
        /// 스킬 실행 (Async 버전)
        /// </summary>
        public async UniTask ExecuteSkillAsync(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill = null)
        {
            if (mainSkill == null)
            {
                Debug.LogError("[SkillExecutor] MainSkillData is null");
                return;
            }

            if (vfxDatabase == null)
            {
                Debug.LogError("[SkillExecutor] VFXDatabase is not assigned!");
                return;
            }

            // VFX Database에서 프리팹 가져오기
            GameObject prefab = vfxDatabase.GetVFXPrefab(mainSkill.skill_id);
            if (prefab == null)
            {
                Debug.LogError($"[SkillExecutor] VFX prefab not found for skill_id: {mainSkill.skill_id}");
                return;
            }

            GameObject hitPrefab = vfxDatabase.GetHitPrefab(mainSkill.skill_id);

            // behavior_type에 따라 분기
            switch (mainSkill.behavior_type)
            {
                case "SingleProjectile":
                    ExecuteProjectile(caster, target, mainSkill, supportSkill, prefab, hitPrefab, false);
                    break;

                case "ExplosiveProjectile":
                    ExecuteProjectile(caster, target, mainSkill, supportSkill, prefab, hitPrefab, true);
                    break;

                case "BeamRay":
                    await ExecuteBeamAsync(caster, target, mainSkill, supportSkill, prefab);
                    break;

                case "TargetAOE":
                    await ExecuteTargetAOEAsync(caster, target, mainSkill, supportSkill, prefab, hitPrefab);
                    break;

                case "LinearAOE":
                    ExecuteLinearAOE(caster, target, mainSkill, supportSkill, prefab);
                    break;

                case "GroundAOE":
                    ExecuteGroundAOE(caster, target, mainSkill, supportSkill, prefab);
                    break;

                case "Barrier":
                    await ExecuteBarrierAsync(caster, mainSkill, supportSkill, prefab);
                    break;

                case "Buff":
                    await ExecuteBuffAsync(caster, mainSkill, supportSkill, prefab);
                    break;

                case "Debuff":
                    await ExecuteDebuffAsync(caster, target, mainSkill, supportSkill, prefab);
                    break;

                case "Trap":
                    ExecuteTrap(caster, mainSkill, supportSkill, prefab);
                    break;

                case "Instant":
                    await ExecuteInstantAsync(caster, target, mainSkill, supportSkill, prefab);
                    break;

                default:
                    Debug.LogWarning($"[SkillExecutor] Unknown behavior_type: {mainSkill.behavior_type}");
                    break;
            }
        }

        #endregion

        #region Skill Execution Methods

        /// <summary>
        /// 투사체 스킬 실행
        /// </summary>
        private void ExecuteProjectile(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab,
            GameObject hitPrefab,
            bool isExplosive)
        {
            if (target == null || !target.IsAlive())
            {
                Debug.LogWarning("[SkillExecutor] Invalid target for projectile");
                return;
            }

            Vector3 spawnPos = caster.position + Vector3.up * 1f;
            Vector3 targetPos = GetTargetAimPosition(target);
            Vector3 direction = (targetPos - spawnPos).normalized;

            // 서포트 스킬에 따른 발사 수 계산
            int projectileCount = 1;
            float spreadAngle = 0f;

            if (supportSkill != null && supportSkill.IsMultiShotSupport)
            {
                projectileCount = supportSkill.count;
                spreadAngle = supportSkill.spread_angle;
            }

            // 투사체 발사
            for (int i = 0; i < projectileCount; i++)
            {
                Vector3 fireDir = direction;

                if (projectileCount > 1 && spreadAngle > 0)
                {
                    float angleOffset = Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (projectileCount - 1));
                    fireDir = Quaternion.Euler(0, angleOffset, 0) * direction;
                }

                SpawnProjectile(spawnPos, fireDir, mainSkill, supportSkill, prefab, isExplosive, target, hitPrefab);
            }
        }

        private void SpawnProjectile(
            Vector3 position,
            Vector3 direction,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab,
            bool isExplosive,
            ITargetable target,
            GameObject hitPrefab)
        {
            GameObject projectileObj = Instantiate(prefab, position, Quaternion.LookRotation(direction));

            // Collider 설정
            Collider col = projectileObj.GetComponent<Collider>();
            if (col == null)
            {
                var sphereCol = projectileObj.AddComponent<SphereCollider>();
                sphereCol.radius = 0.5f;
                sphereCol.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }

            // Rigidbody 설정
            Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = projectileObj.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            // 레이어 설정
            projectileObj.layer = LayerMask.NameToLayer("Projectile");

            // SkillProjectile 컴포넌트
            SkillProjectile projectile = projectileObj.GetComponent<SkillProjectile>();
            if (projectile == null)
            {
                projectile = projectileObj.AddComponent<SkillProjectile>();
            }

            projectile.Initialize(mainSkill, supportSkill, target, isExplosive, hitPrefab);
        }

        /// <summary>
        /// 빔 스킬 실행 - Monster 레이어에서만 멈춤
        /// </summary>
        private async UniTask ExecuteBeamAsync(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab)
        {
            Vector3 spawnPos = caster.position + Vector3.up * 1f;
            Vector3 targetPos = target != null ? GetTargetAimPosition(target) : caster.position + caster.forward * 10f;
            Vector3 direction = (targetPos - spawnPos).normalized;

            // 빔 생성
            GameObject beamObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction));
            beamObj.transform.SetParent(caster);
            beamObj.transform.localPosition = Vector3.up * 1f;

            float maxRange = mainSkill.range > 0 ? mainSkill.range : 15f;

            // Hovl_Laser 비활성화하고 우리 컨트롤러 사용
            var hovlLaser = beamObj.GetComponent<Hovl_Laser>();
            var hovlLaser2 = beamObj.GetComponent<Hovl_Laser2>();

            if (hovlLaser != null)
            {
                hovlLaser.MaxLength = maxRange;
            }
            if (hovlLaser2 != null)
            {
                hovlLaser2.MaxLength = maxRange;
            }

            // 빔 설정
            float duration = mainSkill.duration > 0 ? mainSkill.duration : 1.5f;
            float damage = CalculateDamage(mainSkill, supportSkill);
            float tickInterval = 0.5f;
            int monsterLayerMask = LayerMask.GetMask("Monster");

            // 지속 데미지 + 타겟 추적
            float elapsed = 0f;
            while (elapsed < duration && beamObj != null)
            {
                if (target != null && target.IsAlive())
                {
                    Vector3 currentTargetPos = GetTargetAimPosition(target);
                    Vector3 beamStartPos = beamObj.transform.position;
                    direction = (currentTargetPos - beamStartPos).normalized;
                    beamObj.transform.rotation = Quaternion.LookRotation(direction);
                }

                // Monster 레이어만 Raycast
                RaycastHit hit;
                if (Physics.Raycast(beamObj.transform.position, beamObj.transform.forward, out hit, maxRange, monsterLayerMask))
                {
                    ITargetable hitTarget = hit.collider.GetComponent<Monster>();
                    if (hitTarget == null)
                    {
                        hitTarget = hit.collider.GetComponent<BossMonster>();
                    }

                    if (hitTarget != null && hitTarget.IsAlive())
                    {
                        float tickDamage = damage * tickInterval;
                        hitTarget.TakeDamage(tickDamage);
                    }
                }

                await UniTask.Delay((int)(tickInterval * 1000));
                elapsed += tickInterval;
            }

            // 정리
            if (beamObj != null)
            {
                beamObj.transform.SetParent(null);

                if (hovlLaser != null) hovlLaser.DisablePrepare();
                if (hovlLaser2 != null) hovlLaser2.DisablePrepare();

                await UniTask.Delay(500);
                if (beamObj != null) Destroy(beamObj);
            }
        }

        /// <summary>
        /// 타겟 AOE 실행
        /// </summary>
        private async UniTask ExecuteTargetAOEAsync(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab,
            GameObject hitPrefab)
        {
            Vector3 targetPos = target != null ? target.GetTransform().position : caster.position + caster.forward * 5f;

            GameObject aoeObj = Instantiate(prefab, targetPos, Quaternion.identity);

            // 이펙트 재생 대기
            float fallDuration = 1.0f;
            await UniTask.Delay((int)(fallDuration * 1000));

            // Hit 이펙트
            if (hitPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitPrefab, targetPos, Quaternion.identity);
                Destroy(hitEffect, 3f);
            }

            // 범위 데미지
            float radius = mainSkill.aoe_radius > 0 ? mainSkill.aoe_radius : 3f;
            float damage = CalculateDamage(mainSkill, supportSkill);

            Collider[] colliders = Physics.OverlapSphere(targetPos, radius);
            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (!col.CompareTag(Tag.Monster) && !col.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable hitTarget = col.GetComponent<Monster>();
                if (hitTarget == null)
                {
                    hitTarget = col.GetComponent<BossMonster>();
                }

                if (hitTarget != null && hitTarget.IsAlive())
                {
                    hitTarget.TakeDamage(damage);
                }
            }

            if (aoeObj != null) Destroy(aoeObj, 2f);
        }

        /// <summary>
        /// 직선 AOE 실행
        /// </summary>
        private void ExecuteLinearAOE(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab)
        {
            Vector3 spawnPos = caster.position;
            Vector3 direction = target != null
                ? (target.GetTransform().position - spawnPos).normalized
                : caster.forward;

            GameObject aoeObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction));

            SkillAOE aoe = aoeObj.GetComponent<SkillAOE>();
            if (aoe == null)
            {
                aoe = aoeObj.AddComponent<SkillAOE>();
            }

            aoe.Initialize(mainSkill, supportSkill, isLinear: true, moveDirection: direction);
        }

        /// <summary>
        /// 지속 장판 AOE 실행
        /// </summary>
        private void ExecuteGroundAOE(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab)
        {
            Vector3 targetPos = target != null
                ? target.GetTransform().position
                : caster.position + caster.forward * 5f;

            GameObject aoeObj = Instantiate(prefab, targetPos, Quaternion.identity);

            SkillAOE aoe = aoeObj.GetComponent<SkillAOE>();
            if (aoe == null)
            {
                aoe = aoeObj.AddComponent<SkillAOE>();
            }

            aoe.Initialize(mainSkill, supportSkill, isGround: true);
        }

        /// <summary>
        /// 방어막 실행
        /// </summary>
        private async UniTask ExecuteBarrierAsync(
            Transform caster,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab)
        {
            GameObject barrierObj = Instantiate(prefab, caster.position, Quaternion.identity);
            barrierObj.transform.SetParent(caster);

            float duration = mainSkill.duration > 0 ? mainSkill.duration : 5f;
            await UniTask.Delay((int)(duration * 1000));

            if (barrierObj != null) Destroy(barrierObj);
        }

        /// <summary>
        /// 버프 실행
        /// </summary>
        private async UniTask ExecuteBuffAsync(
            Transform caster,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab)
        {
            GameObject buffObj = Instantiate(prefab, caster.position, Quaternion.identity);
            buffObj.transform.SetParent(caster);

            float duration = mainSkill.duration > 0 ? mainSkill.duration : 8f;
            await UniTask.Delay((int)(duration * 1000));

            if (buffObj != null) Destroy(buffObj);
        }

        /// <summary>
        /// 디버프 실행
        /// </summary>
        private async UniTask ExecuteDebuffAsync(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab)
        {
            Vector3 targetPos = target != null
                ? target.GetTransform().position
                : caster.position + caster.forward * 5f;

            GameObject debuffObj = Instantiate(prefab, targetPos, Quaternion.identity);

            float duration = mainSkill.duration > 0 ? mainSkill.duration : 5f;
            await UniTask.Delay((int)(duration * 1000));

            if (debuffObj != null) Destroy(debuffObj);
        }

        /// <summary>
        /// 트랩 실행
        /// </summary>
        private void ExecuteTrap(
            Transform caster,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab)
        {
            Vector3 spawnPos = caster.position + caster.forward * 3f;
            GameObject trapObj = Instantiate(prefab, spawnPos, Quaternion.identity);

            SkillTrap trap = trapObj.GetComponent<SkillTrap>();
            if (trap == null)
            {
                trap = trapObj.AddComponent<SkillTrap>();
            }

            trap.Initialize(mainSkill, supportSkill);
        }

        /// <summary>
        /// 즉발 스킬 실행
        /// </summary>
        private async UniTask ExecuteInstantAsync(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab)
        {
            Vector3 targetPos = target != null ? target.GetTransform().position : caster.position;

            GameObject effectObj = Instantiate(prefab, targetPos, Quaternion.identity);

            if (target != null && target.IsAlive())
            {
                float damage = CalculateDamage(mainSkill, supportSkill);
                target.TakeDamage(damage);
            }

            float lifetime = mainSkill.duration > 0 ? mainSkill.duration : 2f;
            await UniTask.Delay((int)(lifetime * 1000));

            if (effectObj != null) Destroy(effectObj);
        }

        #endregion

        #region Damage Calculation

        /// <summary>
        /// 데미지 계산
        /// </summary>
        public static float CalculateDamage(MainSkillData mainSkill, SupportSkillData supportSkill)
        {
            float damage = mainSkill.base_damage;

            if (supportSkill != null && supportSkill.IsDamageUpSupport && supportSkill.explosion_ratio > 0)
            {
                damage *= supportSkill.explosion_ratio;
            }

            return damage;
        }

        #endregion
    }
}
