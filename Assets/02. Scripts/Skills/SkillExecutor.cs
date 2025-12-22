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

        #region Constants
        private const float DEFAULT_SPAWN_HEIGHT = 1f;
        private const float DEFAULT_BEAM_RANGE = 15f;
        private const float DEFAULT_BEAM_DURATION = 1.5f;
        private const float DEFAULT_AOE_FALL_DURATION = 1.0f;
        private const float DEFAULT_AOE_RADIUS = 3f;
        private const float DEFAULT_BUFF_DURATION = 8f;
        private const float DEFAULT_BARRIER_DURATION = 5f;
        private const float DEFAULT_DEBUFF_DURATION = 5f;
        private const float DEFAULT_INSTANT_DURATION = 2f;
        private const float DEFAULT_CHAIN_RANGE = 5f;
        private const float BEAM_TICK_INTERVAL = 0.5f;
        private const int BEAM_CLEANUP_DELAY_MS = 500;
        private const float FORWARD_OFFSET = 5f;
        private const float TRAP_SPAWN_OFFSET = 3f;

        // Falling Projectile Constants
        private const float DEFAULT_FALL_HEIGHT = 15f;
        private const float DEFAULT_FALL_DURATION = 0.5f;
        private const float DEFAULT_WARNING_DURATION = 0.5f;
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
            if (!ValidateSkillExecution(mainSkill, out GameObject prefab, out GameObject hitPrefab))
            {
                return;
            }

            await ExecuteByBehaviorType(caster, target, mainSkill, supportSkill, prefab, hitPrefab);
        }

        #endregion

        #region Validation

        private bool ValidateSkillExecution(MainSkillData mainSkill, out GameObject prefab, out GameObject hitPrefab)
        {
            prefab = null;
            hitPrefab = null;

            if (mainSkill == null)
            {
                Debug.LogError("[SkillExecutor] MainSkillData is null");
                return false;
            }

            if (vfxDatabase == null)
            {
                Debug.LogError("[SkillExecutor] VFXDatabase is not assigned!");
                return false;
            }

            prefab = vfxDatabase.GetVFXPrefab(mainSkill.skill_id);
            if (prefab == null)
            {
                Debug.LogError($"[SkillExecutor] VFX prefab not found for skill_id: {mainSkill.skill_id}");
                return false;
            }

            hitPrefab = vfxDatabase.GetHitPrefab(mainSkill.skill_id);
            return true;
        }

        private async UniTask ExecuteByBehaviorType(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab,
            GameObject hitPrefab)
        {
            switch (mainSkill.behavior_type)
            {
                case "SingleProjectile":
                    ExecuteProjectile(caster, target, mainSkill, supportSkill, prefab, hitPrefab, false);
                    break;

                case "ExplosiveProjectile":
                    ExecuteProjectile(caster, target, mainSkill, supportSkill, prefab, hitPrefab, true);
                    break;

                case "FallingProjectile":
                    await ExecuteFallingProjectileAsync(caster, target, mainSkill, supportSkill, prefab, hitPrefab);
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

                case "MovingAOE":
                    ExecuteMovingAOE(caster, target, mainSkill, supportSkill, prefab);
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

            Vector3 spawnPos = caster.position + Vector3.up * DEFAULT_SPAWN_HEIGHT;
            Vector3 targetPos = TargetableUtils.GetAimPosition(target);
            Vector3 direction = (targetPos - spawnPos).normalized;

            int projectileCount = 1;
            float spreadAngle = 0f;

            if (supportSkill != null && supportSkill.IsMultiShotSupport)
            {
                projectileCount = supportSkill.count;
                spreadAngle = supportSkill.spread_angle;
            }

            for (int i = 0; i < projectileCount; i++)
            {
                Vector3 fireDir = CalculateSpreadDirection(direction, i, projectileCount, spreadAngle);
                SpawnProjectile(spawnPos, fireDir, mainSkill, supportSkill, prefab, isExplosive, target, hitPrefab);
            }
        }

        private Vector3 CalculateSpreadDirection(Vector3 baseDirection, int index, int totalCount, float spreadAngle)
        {
            if (totalCount <= 1 || spreadAngle <= 0)
            {
                return baseDirection;
            }

            float angleOffset = Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)index / (totalCount - 1));
            return Quaternion.Euler(0, angleOffset, 0) * baseDirection;
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
        /// 낙하 투사체 실행 (하늘에서 떨어지는 타입)
        /// 경고 VFX → 낙하 → 착지 데미지
        /// </summary>
        private async UniTask ExecuteFallingProjectileAsync(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab,
            GameObject hitPrefab)
        {
            // 착지 위치 결정
            Vector3 landingPos = target != null
                ? TargetableUtils.GetAimPosition(target)
                : caster.position + caster.forward * FORWARD_OFFSET;

            // 지면 높이로 조정
            landingPos.y = 0f;

            // 멀티샷 서포트 처리
            int projectileCount = 1;
            float spreadRadius = 0f;

            if (supportSkill != null && supportSkill.IsMultiShotSupport)
            {
                projectileCount = supportSkill.count;
                spreadRadius = supportSkill.spread_angle * 0.1f; // spread_angle을 범위로 사용
            }

            // 각 투사체 스폰
            for (int i = 0; i < projectileCount; i++)
            {
                Vector3 targetLandingPos = landingPos;

                // 멀티샷 시 착지점 분산
                if (projectileCount > 1)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
                    targetLandingPos += new Vector3(offset.x, 0, offset.y);
                }

                SpawnFallingProjectile(targetLandingPos, mainSkill, supportSkill, prefab, hitPrefab).Forget();
            }

            await UniTask.Yield();
        }

        /// <summary>
        /// 단일 낙하 투사체 스폰
        /// </summary>
        private async UniTaskVoid SpawnFallingProjectile(
            Vector3 landingPos,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab,
            GameObject hitPrefab)
        {
            // 경고 VFX 표시
            GameObject warningVFX = vfxDatabase.GetWarningPrefab(mainSkill.skill_id);
            GameObject warningObj = null;

            if (warningVFX != null)
            {
                warningObj = Instantiate(warningVFX, landingPos, Quaternion.identity);
            }

            // 경고 시간 대기 (duration 필드를 경고 시간으로 활용)
            float warningDuration = mainSkill.duration > 0 ? mainSkill.duration : DEFAULT_WARNING_DURATION;
            await UniTask.Delay((int)(warningDuration * 1000));

            // 경고 VFX 제거
            if (warningObj != null)
            {
                Destroy(warningObj);
            }

            // 시작 위치 (착지점 위 공중)
            float fallHeight = DEFAULT_FALL_HEIGHT;
            Vector3 startPos = landingPos + Vector3.up * fallHeight;

            // 투사체 생성
            GameObject projectileObj = Instantiate(prefab, startPos, Quaternion.LookRotation(Vector3.down));

            // 낙하 애니메이션
            float fallDuration = DEFAULT_FALL_DURATION;
            float elapsed = 0f;

            while (elapsed < fallDuration && projectileObj != null)
            {
                float t = elapsed / fallDuration;
                // 가속 곡선 적용 (더 자연스러운 낙하)
                float curvedT = t * t;
                projectileObj.transform.position = Vector3.Lerp(startPos, landingPos, curvedT);

                await UniTask.Yield();
                elapsed += Time.deltaTime;
            }

            // 착지 위치로 확정
            if (projectileObj != null)
            {
                projectileObj.transform.position = landingPos;
            }

            // 피격 이펙트 스폰
            SpawnHitEffect(hitPrefab, landingPos);

            // 데미지 적용
            float damage = CalculateDamage(mainSkill, supportSkill);
            float radius = mainSkill.aoe_radius > 0 ? mainSkill.aoe_radius : DEFAULT_AOE_RADIUS;

            // 폭발형인지 확인
            if (mainSkill.aoe_radius > 0)
            {
                TargetableUtils.ApplyDamageInRadius(landingPos, radius, damage);
            }
            else
            {
                // 단일 타겟 데미지 (착지점 근처의 가장 가까운 적)
                ITargetable nearestTarget = TargetableUtils.FindNearestTarget(landingPos, 1f);
                if (nearestTarget != null && nearestTarget.IsAlive())
                {
                    nearestTarget.TakeDamage(damage);
                }
            }

            // 투사체 제거
            if (projectileObj != null)
            {
                Destroy(projectileObj, 0.1f);
            }
        }

        /// <summary>
        /// 빔 스킬 실행 - Monster 레이어에서만 멈춤
        /// SkillExecutor가 직접 위치/방향 제어 (외부 에셋 수정 없이)
        /// </summary>
        private async UniTask ExecuteBeamAsync(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab)
        {
            Vector3 spawnPos = GetCasterAimPosition(caster);
            Vector3 targetPos = target != null
                ? TargetableUtils.GetAimPosition(target)
                : caster.position + caster.forward * 10f;
            Vector3 direction = (targetPos - spawnPos).normalized;

            GameObject beamObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction));

            float maxRange = mainSkill.range > 0 ? mainSkill.range : DEFAULT_BEAM_RANGE;
            ConfigureHovlLaser(beamObj, maxRange);

            float duration = mainSkill.duration > 0 ? mainSkill.duration : DEFAULT_BEAM_DURATION;
            float damage = CalculateDamage(mainSkill, supportSkill);
            int monsterLayerMask = LayerMask.GetMask("Monster");

            // 연쇄 레이저 설정
            int chainCount = 0;
            float chainRange = DEFAULT_CHAIN_RANGE;
            float chainDecay = 0.8f;
            if (supportSkill != null && supportSkill.IsChainSupport)
            {
                chainCount = supportSkill.count;
                chainRange = supportSkill.range > 0 ? supportSkill.range : DEFAULT_CHAIN_RANGE;
                chainDecay = supportSkill.chain_decay > 0 ? supportSkill.chain_decay : 0.8f;
            }

            // 연쇄 레이저 오브젝트 리스트
            var chainBeams = new System.Collections.Generic.List<GameObject>();
            var chainTargetIds = new System.Collections.Generic.HashSet<int>();

            float elapsed = 0f;
            while (elapsed < duration && beamObj != null)
            {
                // 메인 레이저 위치/방향 업데이트 (SkillExecutor가 직접 제어)
                UpdateBeamTransform(beamObj, caster, target);

                // 메인 레이저 데미지 적용 및 연쇄 처리
                ITargetable hitTarget = ApplyBeamDamageAndGetTarget(beamObj, maxRange, damage, monsterLayerMask);

                // 연쇄 레이저 처리
                if (chainCount > 0 && hitTarget != null)
                {
                    UpdateChainBeams(hitTarget, prefab, chainCount, chainRange, chainDecay, damage, monsterLayerMask, chainBeams, chainTargetIds);
                }

                await UniTask.Delay((int)(BEAM_TICK_INTERVAL * 1000));
                elapsed += BEAM_TICK_INTERVAL;
            }

            // 순차적 종료: 메인 레이저 → 연쇄 레이저 순서대로
            await CleanupBeam(beamObj);

            foreach (var chainBeam in chainBeams)
            {
                if (chainBeam != null)
                {
                    await CleanupBeam(chainBeam);
                }
            }
        }

        /// <summary>
        /// 캐스터의 조준 위치 (Collider 중심 또는 position + offset)
        /// </summary>
        private Vector3 GetCasterAimPosition(Transform caster)
        {
            Collider col = caster.GetComponent<Collider>();
            if (col != null)
            {
                return col.bounds.center;
            }
            return caster.position + Vector3.up * DEFAULT_SPAWN_HEIGHT;
        }

        /// <summary>
        /// 레이저 위치/방향 업데이트 (매 틱마다 호출)
        /// </summary>
        private void UpdateBeamTransform(GameObject beamObj, Transform source, ITargetable target)
        {
            if (beamObj == null) return;

            // 시작점: 캐스터 위치
            Vector3 startPos = GetCasterAimPosition(source);
            beamObj.transform.position = startPos;

            // 끝점: 타겟 방향으로 LookAt
            if (target != null && target.IsAlive())
            {
                Vector3 targetPos = TargetableUtils.GetAimPosition(target);
                beamObj.transform.LookAt(targetPos);
            }
        }

        private void UpdateChainBeams(
            ITargetable firstTarget,
            GameObject prefab,
            int chainCount,
            float chainRange,
            float chainDecay,
            float baseDamage,
            int layerMask,
            System.Collections.Generic.List<GameObject> chainBeams,
            System.Collections.Generic.HashSet<int> chainTargetIds)
        {
            // 이전 연쇄 타겟 초기화 (매 틱마다 새로 계산)
            chainTargetIds.Clear();
            chainTargetIds.Add(firstTarget.GetTransform().GetInstanceID());

            // 현재 틱의 연쇄 타겟들 (위치 계산용)
            var currentChainTargets = new System.Collections.Generic.List<ITargetable>();
            currentChainTargets.Add(firstTarget);

            float currentDamage = baseDamage;
            int activeChainCount = 0;

            for (int i = 0; i < chainCount; i++)
            {
                // 마지막 타겟에서 다음 타겟 찾기
                ITargetable lastTarget = currentChainTargets[currentChainTargets.Count - 1];
                if (lastTarget == null || !lastTarget.IsAlive()) break;

                Vector3 lastPos = TargetableUtils.GetAimPosition(lastTarget);

                // 이미 맞은 타겟 제외하고 가장 가까운 적 찾기
                ITargetable nextTarget = FindNextChainTargetById(lastPos, chainRange, chainTargetIds);
                if (nextTarget == null || !nextTarget.IsAlive()) break;

                // 타겟 추가
                chainTargetIds.Add(nextTarget.GetTransform().GetInstanceID());
                currentChainTargets.Add(nextTarget);
                currentDamage *= chainDecay;
                activeChainCount++;

                // 시작점(lastTarget) → 끝점(nextTarget)
                Vector3 startPos = TargetableUtils.GetAimPosition(lastTarget);
                Vector3 endPos = TargetableUtils.GetAimPosition(nextTarget);
                Vector3 dir = (endPos - startPos).normalized;
                if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;

                // 연쇄 레이저 생성 또는 업데이트
                if (i >= chainBeams.Count)
                {
                    // 새 연쇄 레이저 생성
                    GameObject chainBeam = Instantiate(prefab, startPos, Quaternion.LookRotation(dir));
                    ConfigureHovlLaser(chainBeam, chainRange);
                    chainBeams.Add(chainBeam);
                }
                else if (chainBeams[i] != null)
                {
                    // 비활성화된 레이저 재활성화
                    if (!chainBeams[i].activeSelf)
                    {
                        chainBeams[i].SetActive(true);
                    }
                }

                // 연쇄 레이저 위치/방향 직접 업데이트
                if (i < chainBeams.Count && chainBeams[i] != null)
                {
                    UpdateChainBeamTransform(chainBeams[i], lastTarget, nextTarget);
                }

                // 연쇄 데미지 적용
                float tickDamage = currentDamage * BEAM_TICK_INTERVAL;
                nextTarget.TakeDamage(tickDamage);
            }

            // 사용하지 않는 연쇄 레이저 비활성화
            for (int i = activeChainCount; i < chainBeams.Count; i++)
            {
                if (chainBeams[i] != null)
                {
                    chainBeams[i].SetActive(false);
                }
            }
        }

        /// <summary>
        /// 연쇄 레이저 위치/방향 업데이트 (매 틱마다 호출)
        /// </summary>
        private void UpdateChainBeamTransform(GameObject beamObj, ITargetable source, ITargetable target)
        {
            if (beamObj == null) return;

            // 시작점: source의 Collider 중심
            Vector3 startPos = TargetableUtils.GetAimPosition(source);
            beamObj.transform.position = startPos;

            // 끝점: target 방향으로 LookAt
            if (target != null && target.IsAlive())
            {
                Vector3 targetPos = TargetableUtils.GetAimPosition(target);
                beamObj.transform.LookAt(targetPos);
            }
        }

        private ITargetable FindNextChainTargetById(Vector3 position, float range, System.Collections.Generic.HashSet<int> excludeTargetIds)
        {
            var allTargets = TargetableUtils.GetTargetsInRadius(position, range);

            ITargetable nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var target in allTargets)
            {
                if (!target.IsAlive()) continue;

                int targetId = target.GetTransform().GetInstanceID();
                if (excludeTargetIds.Contains(targetId)) continue;

                float dist = Vector3.Distance(position, TargetableUtils.GetAimPosition(target));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = target;
                }
            }

            return nearest;
        }

        private ITargetable ApplyBeamDamageAndGetTarget(GameObject beamObj, float maxRange, float damage, int layerMask)
        {
            if (Physics.Raycast(beamObj.transform.position, beamObj.transform.forward, out RaycastHit hit, maxRange, layerMask))
            {
                ITargetable hitTarget = TargetableUtils.GetTargetable(hit.collider);
                if (hitTarget != null && hitTarget.IsAlive())
                {
                    float tickDamage = damage * BEAM_TICK_INTERVAL;
                    hitTarget.TakeDamage(tickDamage);
                    return hitTarget;
                }
            }
            return null;
        }

        private void ConfigureHovlLaser(GameObject beamObj, float maxRange)
        {
            var hovlLaser = beamObj.GetComponent<Hovl_Laser>();
            var hovlLaser2 = beamObj.GetComponent<Hovl_Laser2>();

            if (hovlLaser != null) hovlLaser.MaxLength = maxRange;
            if (hovlLaser2 != null) hovlLaser2.MaxLength = maxRange;
        }

        private async UniTask CleanupBeam(GameObject beamObj)
        {
            if (beamObj == null) return;

            beamObj.transform.SetParent(null);

            var hovlLaser = beamObj.GetComponent<Hovl_Laser>();
            var hovlLaser2 = beamObj.GetComponent<Hovl_Laser2>();

            if (hovlLaser != null) hovlLaser.DisablePrepare();
            if (hovlLaser2 != null) hovlLaser2.DisablePrepare();

            await UniTask.Delay(BEAM_CLEANUP_DELAY_MS);
            if (beamObj != null) Destroy(beamObj);
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
            Vector3 targetPos = GetTargetOrForwardPosition(caster, target);

            GameObject aoeObj = Instantiate(prefab, targetPos, Quaternion.identity);

            await UniTask.Delay((int)(DEFAULT_AOE_FALL_DURATION * 1000));

            SpawnHitEffect(hitPrefab, targetPos);

            float radius = mainSkill.aoe_radius > 0 ? mainSkill.aoe_radius : DEFAULT_AOE_RADIUS;
            float damage = CalculateDamage(mainSkill, supportSkill);

            TargetableUtils.ApplyDamageInRadius(targetPos, radius, damage);

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
            Vector3 direction = GetDirectionToTarget(caster, target);

            GameObject aoeObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction));
            GetOrAddComponent<SkillAOE>(aoeObj).Initialize(mainSkill, supportSkill, isLinear: true, moveDirection: direction);
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
            Vector3 targetPos = GetTargetOrForwardPosition(caster, target);

            GameObject aoeObj = Instantiate(prefab, targetPos, Quaternion.identity);
            GetOrAddComponent<SkillAOE>(aoeObj).Initialize(mainSkill, supportSkill, isGround: true);
        }

        /// <summary>
        /// 이동하는 AOE 실행
        /// 소환된 위치에서 타겟 방향으로 천천히 이동하며 데미지
        /// </summary>
        private void ExecuteMovingAOE(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab)
        {
            // 소환 위치 결정 (타겟이 있으면 타겟 위치, 없으면 캐스터 앞)
            Vector3 spawnPos = target != null
                ? target.GetTransform().position
                : caster.position + caster.forward * FORWARD_OFFSET;

            // 이동 방향 (캐스터 → 타겟 방향, 없으면 캐스터 forward)
            Vector3 moveDirection = target != null
                ? (target.GetTransform().position - caster.position).normalized
                : caster.forward;

            GameObject aoeObj = Instantiate(prefab, spawnPos, Quaternion.identity);

            // AOE 컴포넌트 초기화 - isMoving: true
            GetOrAddComponent<SkillAOE>(aoeObj).Initialize(
                mainSkill,
                supportSkill,
                isGround: false,
                isLinear: false,
                moveDirection: moveDirection,
                isFalling: false,
                fallTarget: default,
                isMoving: true
            );
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
            GameObject barrierObj = SpawnAttachedEffect(prefab, caster);

            float duration = mainSkill.duration > 0 ? mainSkill.duration : DEFAULT_BARRIER_DURATION;
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
            GameObject buffObj = SpawnAttachedEffect(prefab, caster);

            // 캐릭터에게 버프 효과 적용
            Character casterCharacter = caster.GetComponent<Character>();
            if (casterCharacter != null)
            {
                SkillBuff buff = buffObj.GetComponent<SkillBuff>();
                if (buff == null)
                {
                    buff = buffObj.AddComponent<SkillBuff>();
                }
                buff.Initialize(mainSkill, supportSkill, casterCharacter);
            }
            else
            {
                // 캐릭터가 없으면 VFX만 표시
                float duration = mainSkill.duration > 0 ? mainSkill.duration : DEFAULT_BUFF_DURATION;
                await UniTask.Delay((int)(duration * 1000));
                if (buffObj != null) Destroy(buffObj);
            }
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
            Vector3 targetPos = GetTargetOrForwardPosition(caster, target);

            GameObject debuffObj = Instantiate(prefab, targetPos, Quaternion.identity);

            // 디버프 효과 적용
            SkillDebuff debuff = debuffObj.GetComponent<SkillDebuff>();
            if (debuff == null)
            {
                debuff = debuffObj.AddComponent<SkillDebuff>();
            }
            debuff.Initialize(mainSkill, supportSkill, target);

            await UniTask.Yield();
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
            Vector3 spawnPos = caster.position + caster.forward * TRAP_SPAWN_OFFSET;
            GameObject trapObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            GetOrAddComponent<SkillTrap>(trapObj).Initialize(mainSkill, supportSkill);
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

            float lifetime = mainSkill.duration > 0 ? mainSkill.duration : DEFAULT_INSTANT_DURATION;
            await UniTask.Delay((int)(lifetime * 1000));

            if (effectObj != null) Destroy(effectObj);
        }

        #endregion

        #region Helper Methods

        private Vector3 GetTargetOrForwardPosition(Transform caster, ITargetable target)
        {
            return target != null
                ? target.GetTransform().position
                : caster.position + caster.forward * FORWARD_OFFSET;
        }

        private Vector3 GetDirectionToTarget(Transform caster, ITargetable target)
        {
            return target != null
                ? (target.GetTransform().position - caster.position).normalized
                : caster.forward;
        }

        private GameObject SpawnAttachedEffect(GameObject prefab, Transform parent)
        {
            GameObject obj = Instantiate(prefab, parent.position, Quaternion.identity);
            obj.transform.SetParent(parent);
            return obj;
        }

        private void SpawnHitEffect(GameObject hitPrefab, Vector3 position)
        {
            if (hitPrefab == null) return;

            GameObject hitEffect = Instantiate(hitPrefab, position, Quaternion.identity);
            Destroy(hitEffect, 3f);
        }

        private T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            return component != null ? component : obj.AddComponent<T>();
        }

        #endregion

        #region Public Accessors

        /// <summary>
        /// VFX 프리팹 가져오기 (연쇄 투사체 생성용)
        /// </summary>
        public GameObject GetVFXPrefab(int skillId)
        {
            if (vfxDatabase == null) return null;
            return vfxDatabase.GetVFXPrefab(skillId);
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
