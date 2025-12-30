using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Audio;
using NovelianMagicLibraryDefense.Managers;
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

        [Header("조합 규칙 데이터")]
        [SerializeField] private SkillCombinationRuleData combinationRuleData;
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
        private const float BEAM_TICK_INTERVAL = 0.5f;
        private const int BEAM_CLEANUP_DELAY_MS = 500;
        private const float FORWARD_OFFSET = 5f;
        private const float TRAP_SPAWN_OFFSET = 3f;

        // Beam 서포트 스킬 기본값
        private const float DEFAULT_DOT_DAMAGE_RATIO = 0.1f;
        private const float DEFAULT_DOT_DURATION = 5f;
        private const float DEFAULT_DOT_TICK_INTERVAL = 0.5f;
        private const float DEFAULT_CC_DURATION = 3f;

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

            // 스킬 발동 사운드 재생
            PlaySkillSound(mainSkill.skill_id);

            // 조합 규칙 검증 - 유효하지 않으면 서포트 스킬 없이 실행
            SupportSkillData validSupport = GetValidSupportSkill(mainSkill, supportSkill);

            // 캐스터에서 Character 컴포넌트 추출 (스탯 적용용)
            Character casterCharacter = caster.GetComponent<Character>();

            await ExecuteByBehaviorType(caster, target, mainSkill, validSupport, prefab, hitPrefab, casterCharacter);
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

        /// <summary>
        /// 메인 스킬과 서포트 스킬의 조합이 유효한지 검증
        /// </summary>
        /// <param name="mainSkill">메인 스킬 데이터</param>
        /// <param name="supportSkill">서포트 스킬 데이터 (null이면 검증 통과)</param>
        /// <returns>조합이 유효하면 true, 아니면 false</returns>
        public bool IsValidCombination(MainSkillData mainSkill, SupportSkillData supportSkill)
        {
            // 서포트 스킬이 없으면 검증 통과
            if (supportSkill == null) return true;

            // 조합 규칙 데이터가 없으면 모든 조합 허용 (경고 출력)
            if (combinationRuleData == null)
            {
                Debug.LogWarning("[SkillExecutor] CombinationRuleData가 할당되지 않았습니다. 모든 조합이 허용됩니다.");
                return true;
            }

            bool isValid = combinationRuleData.IsValidCombination(mainSkill.behavior_type, supportSkill.support_type);

            if (!isValid)
            {
                Debug.LogWarning($"[SkillExecutor] 유효하지 않은 스킬 조합: {mainSkill.behavior_type} + {supportSkill.support_type}");
            }

            return isValid;
        }

        /// <summary>
        /// 조합 규칙에 따라 유효한 서포트 스킬만 필터링하여 반환
        /// </summary>
        public SupportSkillData GetValidSupportSkill(MainSkillData mainSkill, SupportSkillData supportSkill)
        {
            if (IsValidCombination(mainSkill, supportSkill))
            {
                return supportSkill;
            }

            // 유효하지 않은 조합이면 null 반환 (서포트 없이 메인만 실행)
            return null;
        }

        private async UniTask ExecuteByBehaviorType(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab,
            GameObject hitPrefab,
            Character casterCharacter = null)
        {
            switch (mainSkill.behavior_type)
            {
                // ============================================
                // 신규 시스템: 3개 behavior_type
                // ============================================
                case "Projectile":
                    // 폭발 여부는 aoe_radius로 판단
                    bool isExplosive = mainSkill.aoe_radius > 0;
                    ExecuteProjectile(caster, target, mainSkill, supportSkill, prefab, hitPrefab, isExplosive, casterCharacter);
                    break;

                case "BeamRay":
                    await ExecuteBeamAsync(caster, target, mainSkill, supportSkill, prefab, casterCharacter);
                    break;

                case "AOE":
                    await ExecuteAOEAsync(caster, target, mainSkill, supportSkill, prefab, hitPrefab, casterCharacter);
                    break;

                // ============================================
                // Legacy 호환성 (이전 타입들도 지원)
                // ============================================
                case "SingleProjectile":
                    ExecuteProjectile(caster, target, mainSkill, supportSkill, prefab, hitPrefab, false, casterCharacter);
                    break;

                case "ExplosiveProjectile":
                    ExecuteProjectile(caster, target, mainSkill, supportSkill, prefab, hitPrefab, true, casterCharacter);
                    break;

                case "FallingProjectile":
                    await ExecuteFallingProjectileAsync(caster, target, mainSkill, supportSkill, prefab, hitPrefab, casterCharacter);
                    break;

                case "TargetAOE":
                    await ExecuteTargetAOEAsync(caster, target, mainSkill, supportSkill, prefab, hitPrefab, casterCharacter);
                    break;

                case "LinearAOE":
                    ExecuteLinearAOE(caster, target, mainSkill, supportSkill, prefab, casterCharacter);
                    break;

                case "GroundAOE":
                    ExecuteGroundAOE(caster, target, mainSkill, supportSkill, prefab, casterCharacter);
                    break;

                case "MovingAOE":
                    ExecuteMovingAOE(caster, target, mainSkill, supportSkill, prefab, casterCharacter);
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
                    await ExecuteInstantAsync(caster, target, mainSkill, supportSkill, prefab, casterCharacter);
                    break;

                default:
                    Debug.LogWarning($"[SkillExecutor] Unknown behavior_type: {mainSkill.behavior_type}");
                    break;
            }
        }

        /// <summary>
        /// 신규 통합 AOE 실행 - 파라미터 기반으로 동작 결정
        /// duration > 0: 지속형 장판 (기존 GroundAOE)
        /// duration == 0: 즉발형 AOE (기존 TargetAOE/Instant)
        /// </summary>
        private async UniTask ExecuteAOEAsync(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab,
            GameObject hitPrefab,
            Character casterCharacter = null)
        {
            Vector3 targetPos = GetTargetOrForwardPosition(caster, target);
            float radius = mainSkill.aoe_radius > 0 ? mainSkill.aoe_radius : DEFAULT_AOE_RADIUS;

            // 지속형 장판인지 즉발형인지 판단
            if (mainSkill.duration > 0)
            {
                // 지속형 장판 AOE (범위 표시 후 지속)
                AOERangeIndicator.Show(targetPos, radius, AOERangeIndicator.IndicatorType.Damage);
                GameObject aoeObj = Instantiate(prefab, targetPos, Quaternion.identity);
                GetOrAddComponent<SkillAOE>(aoeObj).Initialize(mainSkill, supportSkill, isGround: true);
            }
            else
            {
                // 즉발형 AOE (경고 → 폭발)
                AOERangeIndicator.ShowWithWarning(targetPos, radius, DEFAULT_AOE_FALL_DURATION);
                GameObject aoeObj = Instantiate(prefab, targetPos, Quaternion.identity);

                await UniTask.Delay((int)(DEFAULT_AOE_FALL_DURATION * 1000));

                float hitScale = vfxDatabase != null ? vfxDatabase.GetHitScale(mainSkill.skill_id) : 1f;
                SpawnHitEffect(hitPrefab, targetPos, hitScale);

                float damage = CalculateDamage(mainSkill, supportSkill, casterCharacter, out _);
                // 상성 시스템 적용 (Issue #531)
                if (casterCharacter != null)
                {
                    TargetableUtils.ApplyDamageInRadius(targetPos, radius, damage, casterCharacter.GetGenre(), hitTarget =>
                    {
                        ApplyAOEStatusEffects(hitTarget, supportSkill, damage);
                    });
                }
                else
                {
                    TargetableUtils.ApplyDamageInRadius(targetPos, radius, damage, hitTarget =>
                    {
                        ApplyAOEStatusEffects(hitTarget, supportSkill, damage);
                    });
                }

                if (aoeObj != null) Destroy(aoeObj, 2f);
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
            bool isExplosive,
            Character casterCharacter = null)
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
                SpawnProjectile(spawnPos, fireDir, mainSkill, supportSkill, prefab, isExplosive, target, hitPrefab, casterCharacter);
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
            GameObject hitPrefab,
            Character casterCharacter = null)
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

            // Hit 스케일 가져오기
            float hitScale = vfxDatabase != null ? vfxDatabase.GetHitScale(mainSkill.skill_id) : 1f;

            projectile.Initialize(mainSkill, supportSkill, target, isExplosive, hitPrefab, hitScale, casterCharacter);
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
            GameObject hitPrefab,
            Character casterCharacter = null)
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

                SpawnFallingProjectile(targetLandingPos, mainSkill, supportSkill, prefab, hitPrefab, casterCharacter).Forget();
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
            GameObject hitPrefab,
            Character casterCharacter = null)
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

            // 범위가 있으면 경고 범위 표시
            float radius = mainSkill.aoe_radius > 0 ? mainSkill.aoe_radius : DEFAULT_AOE_RADIUS;
            if (mainSkill.aoe_radius > 0)
            {
                AOERangeIndicator.ShowWithWarning(landingPos, radius, warningDuration);
            }

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

            // Hit 스케일 가져오기
            float hitScale = vfxDatabase != null ? vfxDatabase.GetHitScale(mainSkill.skill_id) : 1f;

            // 피격 이펙트 스폰
            SpawnHitEffect(hitPrefab, landingPos, hitScale);

            // 데미지 적용
            float damage = CalculateDamage(mainSkill, supportSkill, casterCharacter, out bool isCritical);

            // 폭발형인지 확인 - 상성 시스템 적용 (Issue #531)
            if (mainSkill.aoe_radius > 0)
            {
                if (casterCharacter != null)
                {
                    TargetableUtils.ApplyDamageInRadius(landingPos, radius, damage, isCritical, casterCharacter.GetGenre());
                }
                else
                {
                    TargetableUtils.ApplyDamageInRadius(landingPos, radius, damage, isCritical);
                }
            }
            else
            {
                // 단일 타겟 데미지 (착지점 근처의 가장 가까운 적)
                ITargetable nearestTarget = TargetableUtils.FindNearestTarget(landingPos, 1f);
                if (nearestTarget != null && nearestTarget.IsAlive())
                {
                    if (casterCharacter != null)
                    {
                        nearestTarget.TakeDamage(damage, isCritical, casterCharacter.GetGenre());
                    }
                    else
                    {
                        nearestTarget.TakeDamage(damage, isCritical);
                    }
                }
            }

            // 투사체 제거
            if (projectileObj != null)
            {
                Destroy(projectileObj, 0.1f);
            }
        }

        /// <summary>
        /// 빔 스킬 실행 - 관통형 (화면 경계까지 발사, 모든 몬스터에 틱 데미지)
        /// 서포트 스킬: MultiShot(다중 빔), CC, DOT, Enhance
        /// (Bounce는 BeamRay에서 지원하지 않음 - Projectile만 지원)
        /// </summary>
        private async UniTask ExecuteBeamAsync(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab,
            Character casterCharacter = null)
        {
            // 프로젝타일과 동일한 발사 위치 (캐스터 위치 + 높이 오프셋)
            Vector3 spawnPos = caster.position + Vector3.up * DEFAULT_SPAWN_HEIGHT;

            // 빔 방향 계산 (타겟 방향, Y축 고정)
            Vector3 baseDirection = CalculateBeamDirection(caster, target, spawnPos);

            // MultiShot: 여러 방향으로 빔 발사
            int beamCount = 1;
            float spreadAngle = 15f;
            if (supportSkill != null && supportSkill.IsMultiShotSupport)
            {
                beamCount = supportSkill.count > 0 ? supportSkill.count : 3;
                spreadAngle = supportSkill.spread_angle > 0 ? supportSkill.spread_angle : 15f;
            }

            float duration = mainSkill.duration > 0 ? mainSkill.duration : DEFAULT_BEAM_DURATION;
            float damage = CalculateDamage(mainSkill, supportSkill, casterCharacter, out _);

            // 각 빔 방향 계산 및 실행
            var beamTasks = new System.Collections.Generic.List<UniTask>();
            for (int i = 0; i < beamCount; i++)
            {
                Vector3 direction = baseDirection;
                if (beamCount > 1)
                {
                    float angleOffset = spreadAngle * (i - (beamCount - 1) / 2f);
                    direction = Quaternion.Euler(0, angleOffset, 0) * baseDirection;
                }

                beamTasks.Add(ExecuteSingleBeamAsync(caster, spawnPos, direction, prefab, duration, damage, supportSkill, casterCharacter));
            }

            await UniTask.WhenAll(beamTasks);
        }

        /// <summary>
        /// 단일 빔 실행
        /// </summary>
        private async UniTask ExecuteSingleBeamAsync(
            Transform caster,
            Vector3 spawnPos,
            Vector3 direction,
            GameObject prefab,
            float duration,
            float damage,
            SupportSkillData supportSkill,
            Character casterCharacter = null)
        {
            GameObject beamObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction));
            int monsterLayerMask = LayerMask.GetMask("Monster");

            float beamLength = CalculateBeamLengthToScreenBoundary(spawnPos, direction);
            ConfigureHovlLaser(beamObj, beamLength);

            float elapsed = 0f;
            while (elapsed < duration && beamObj != null)
            {
                // 빔 시작 위치 업데이트 (캐스터 따라감)
                Vector3 currentStartPos = caster.position + Vector3.up * DEFAULT_SPAWN_HEIGHT;
                beamObj.transform.position = currentStartPos;
                beamObj.transform.rotation = Quaternion.LookRotation(direction);

                // 빔 길이 재계산
                beamLength = CalculateBeamLengthToScreenBoundary(currentStartPos, direction);
                ConfigureHovlLaser(beamObj, beamLength);

                // 관통형 데미지 적용 (RaycastAll) - 상성 시스템 적용 (Issue #531)
                ApplyPenetratingBeamDamage(beamObj, beamLength, damage, monsterLayerMask, supportSkill, casterCharacter);

                await UniTask.Delay((int)(BEAM_TICK_INTERVAL * 1000));
                elapsed += BEAM_TICK_INTERVAL;
            }

            // 레이저 종료
            await CleanupBeam(beamObj);
        }

        /// <summary>
        /// 관통형 빔 데미지 적용 (RaycastAll로 경로상 모든 몬스터)
        /// </summary>
        private void ApplyPenetratingBeamDamage(GameObject beamObj, float beamLength, float damage, int layerMask, SupportSkillData supportSkill, Character casterCharacter = null)
        {
            if (beamObj == null) return;

            RaycastHit[] hits = Physics.RaycastAll(
                beamObj.transform.position,
                beamObj.transform.forward,
                beamLength,
                layerMask
            );

            float tickDamage = damage * BEAM_TICK_INTERVAL;

            foreach (RaycastHit hit in hits)
            {
                ITargetable hitTarget = TargetableUtils.GetTargetable(hit.collider);
                if (hitTarget != null && hitTarget.IsAlive())
                {
                    // 상성 시스템 적용 (Issue #531)
                    if (casterCharacter != null)
                    {
                        hitTarget.TakeDamage(tickDamage, false, casterCharacter.GetGenre());
                    }
                    else
                    {
                        hitTarget.TakeDamage(tickDamage);
                    }

                    if (supportSkill != null)
                    {
                        ApplyBeamStatusEffects(hitTarget, supportSkill, tickDamage);
                    }
                }
            }
        }

        /// <summary>
        /// 빔 방향 계산 (타겟 기준, XZ 평면에서 수평 발사)
        /// </summary>
        private Vector3 CalculateBeamDirection(Transform caster, ITargetable target, Vector3 spawnPos)
        {
            Vector3 targetPos;
            if (target != null && target.IsAlive())
            {
                targetPos = TargetableUtils.GetAimPosition(target);
            }
            else
            {
                targetPos = caster.position + caster.forward * 10f;
            }

            // XZ 평면에서만 방향 계산 (Y축 무시하여 수평 빔)
            Vector3 direction = new Vector3(targetPos.x - spawnPos.x, 0f, targetPos.z - spawnPos.z);
            return direction.normalized;
        }

        /// <summary>
        /// 화면 경계까지의 빔 길이 계산
        /// </summary>
        private float CalculateBeamLengthToScreenBoundary(Vector3 startPos, Vector3 direction)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject camObj = GameObject.FindWithTag("MainCamera");
                if (camObj != null)
                {
                    mainCamera = camObj.GetComponent<Camera>();
                }
            }

            if (mainCamera == null)
            {
                return DEFAULT_BEAM_RANGE * 3f; // 카메라 없으면 기본값 * 3
            }

            const float maxSearchDistance = 100f;
            const float stepSize = 2f;
            const float margin = 0.05f;

            float distance = 0f;
            while (distance < maxSearchDistance)
            {
                Vector3 checkPos = startPos + direction * distance;
                Vector3 viewportPos = mainCamera.WorldToViewportPoint(checkPos);

                // 화면 밖으로 나갔는지 체크
                if (viewportPos.x < -margin || viewportPos.x > 1f + margin ||
                    viewportPos.y < -margin || viewportPos.y > 1f + margin ||
                    viewportPos.z < 0)
                {
                    return distance;
                }

                distance += stepSize;
            }

            return maxSearchDistance;
        }

        /// <summary>
        /// 빔 위치만 업데이트 (방향은 고정)
        /// </summary>
        private void UpdateBeamPositionOnly(GameObject beamObj, Transform caster, Vector3 direction)
        {
            if (beamObj == null || caster == null) return;

            // 프로젝타일과 동일한 발사 위치 (캐스터 위치 + 높이 오프셋)
            Vector3 startPos = caster.position + Vector3.up * DEFAULT_SPAWN_HEIGHT;
            beamObj.transform.position = startPos;
            beamObj.transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>
        /// 빔 스킬의 서포트 스킬 상태효과 적용 (CC, DOT)
        /// </summary>
        private void ApplyBeamStatusEffects(ITargetable target, SupportSkillData supportSkill, float baseDamage)
        {
            if (supportSkill == null || target == null) return;

            Transform targetTransform = target.GetTransform();
            if (targetTransform == null) return;

            // CC 효과 (Stun/Slow)
            if (supportSkill.IsCCSupport)
            {
                ApplyBeamCCEffect(targetTransform, supportSkill);
            }

            // DOT 효과
            if (supportSkill.IsDOTSupport)
            {
                ApplyBeamDOTEffect(targetTransform, supportSkill, baseDamage);
            }
        }

        /// <summary>
        /// 빔 CC 효과 적용
        /// </summary>
        private void ApplyBeamCCEffect(Transform targetTransform, SupportSkillData supportSkill)
        {
            float ccDuration = supportSkill.duration > 0 ? supportSkill.duration : DEFAULT_CC_DURATION;
            CCType ccType = supportSkill.GetCCType();

            if (targetTransform.CompareTag("Monster"))
            {
                Monster monster = targetTransform.GetComponent<Monster>();
                if (monster != null)
                {
                    if (ccType == CCType.Slow)
                    {
                        monster.ApplySlow(supportSkill.slow_rate, ccDuration);
                    }
                    else
                    {
                        monster.ApplyDizzy(ccDuration);
                    }
                }
            }
            else if (targetTransform.CompareTag("Boss"))
            {
                BossMonster boss = targetTransform.GetComponent<BossMonster>();
                if (boss != null)
                {
                    boss.ApplyStun(ccDuration);
                }
            }
        }

        /// <summary>
        /// 빔 DOT 효과 적용
        /// </summary>
        private void ApplyBeamDOTEffect(Transform targetTransform, SupportSkillData supportSkill, float baseDamage)
        {
            float dotDamage = supportSkill.tick_damage > 0
                ? supportSkill.tick_damage
                : baseDamage * DEFAULT_DOT_DAMAGE_RATIO;
            float dotInterval = supportSkill.tick_interval > 0
                ? supportSkill.tick_interval
                : DEFAULT_DOT_TICK_INTERVAL;
            float dotDuration = supportSkill.duration > 0
                ? supportSkill.duration
                : DEFAULT_DOT_DURATION;

            if (targetTransform.CompareTag("Monster"))
            {
                Monster monster = targetTransform.GetComponent<Monster>();
                monster?.ApplyDOT(DOTType.Poison, dotDamage, dotInterval, dotDuration, null);
            }
            else if (targetTransform.CompareTag("Boss"))
            {
                BossMonster boss = targetTransform.GetComponent<BossMonster>();
                boss?.ApplyDOT(DOTType.Poison, dotDamage, dotInterval, dotDuration, null);
            }
        }

        #endregion

        #region AOE Status Effects

        /// <summary>
        /// AOE 스킬의 상태효과 적용 (CC, DOT)
        /// </summary>
        private void ApplyAOEStatusEffects(ITargetable target, SupportSkillData supportSkill, float baseDamage)
        {
            if (supportSkill == null || target == null) return;

            Transform targetTransform = target.GetTransform();
            if (targetTransform == null) return;

            // CC 효과 (Stun/Slow)
            if (supportSkill.IsCCSupport)
            {
                ApplyAOECCEffect(targetTransform, supportSkill);
            }

            // DOT 효과
            if (supportSkill.IsDOTSupport)
            {
                ApplyAOEDOTEffect(targetTransform, supportSkill, baseDamage);
            }
        }

        /// <summary>
        /// AOE CC 효과 적용
        /// </summary>
        private void ApplyAOECCEffect(Transform targetTransform, SupportSkillData supportSkill)
        {
            float ccDuration = supportSkill.duration > 0 ? supportSkill.duration : DEFAULT_CC_DURATION;
            CCType ccType = supportSkill.GetCCType();

            if (targetTransform.CompareTag("Monster"))
            {
                Monster monster = targetTransform.GetComponent<Monster>();
                if (monster != null)
                {
                    if (ccType == CCType.Slow)
                    {
                        monster.ApplySlow(supportSkill.slow_rate, ccDuration);
                    }
                    else
                    {
                        monster.ApplyDizzy(ccDuration);
                    }
                }
            }
            else if (targetTransform.CompareTag("Boss"))
            {
                BossMonster boss = targetTransform.GetComponent<BossMonster>();
                if (boss != null)
                {
                    boss.ApplyStun(ccDuration);
                }
            }
        }

        /// <summary>
        /// AOE DOT 효과 적용
        /// </summary>
        private void ApplyAOEDOTEffect(Transform targetTransform, SupportSkillData supportSkill, float baseDamage)
        {
            float dotDamage = supportSkill.tick_damage > 0
                ? supportSkill.tick_damage
                : baseDamage * DEFAULT_DOT_DAMAGE_RATIO;
            float dotInterval = supportSkill.tick_interval > 0
                ? supportSkill.tick_interval
                : DEFAULT_DOT_TICK_INTERVAL;
            float dotDuration = supportSkill.duration > 0
                ? supportSkill.duration
                : DEFAULT_DOT_DURATION;

            if (targetTransform.CompareTag("Monster"))
            {
                Monster monster = targetTransform.GetComponent<Monster>();
                monster?.ApplyDOT(DOTType.Poison, dotDamage, dotInterval, dotDuration, null);
            }
            else if (targetTransform.CompareTag("Boss"))
            {
                BossMonster boss = targetTransform.GetComponent<BossMonster>();
                boss?.ApplyDOT(DOTType.Poison, dotDamage, dotInterval, dotDuration, null);
            }
        }

        #endregion

        #region Beam Utils

        private void ConfigureHovlLaser(GameObject beamObj, float maxRange)
        {
            var hovlLaser = beamObj.GetComponent<Hovl_Laser>();
            var hovlLaser2 = beamObj.GetComponent<Hovl_Laser2>();

            // Monster 레이어 마스크 (관통용)
            int monsterLayerMask = LayerMask.GetMask("Monster");

            if (hovlLaser != null)
            {
                hovlLaser.MaxLength = maxRange;
                hovlLaser.ignoreLayers = monsterLayerMask; // Monster 레이어 무시 (관통)
            }
            if (hovlLaser2 != null)
            {
                hovlLaser2.MaxLength = maxRange;
                hovlLaser2.ignoreLayers = monsterLayerMask; // Monster 레이어 무시 (관통)
            }
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
            GameObject hitPrefab,
            Character casterCharacter = null)
        {
            Vector3 targetPos = GetTargetOrForwardPosition(caster, target);

            float radius = mainSkill.aoe_radius > 0 ? mainSkill.aoe_radius : DEFAULT_AOE_RADIUS;

            // 경고 범위 표시 (낙하 전)
            AOERangeIndicator.ShowWithWarning(targetPos, radius, DEFAULT_AOE_FALL_DURATION);

            GameObject aoeObj = Instantiate(prefab, targetPos, Quaternion.identity);

            await UniTask.Delay((int)(DEFAULT_AOE_FALL_DURATION * 1000));

            // Hit 스케일 가져오기
            float hitScale = vfxDatabase != null ? vfxDatabase.GetHitScale(mainSkill.skill_id) : 1f;
            SpawnHitEffect(hitPrefab, targetPos, hitScale);

            float damage = CalculateDamage(mainSkill, supportSkill, casterCharacter, out bool isCritical);

            // 상성 시스템 적용 (Issue #531)
            if (casterCharacter != null)
            {
                TargetableUtils.ApplyDamageInRadius(targetPos, radius, damage, isCritical, casterCharacter.GetGenre());
            }
            else
            {
                TargetableUtils.ApplyDamageInRadius(targetPos, radius, damage, isCritical);
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
            GameObject prefab,
            Character casterCharacter = null)
        {
            Vector3 spawnPos = caster.position;
            Vector3 direction = GetDirectionToTarget(caster, target);

            GameObject aoeObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction));
            GetOrAddComponent<SkillAOE>(aoeObj).Initialize(mainSkill, supportSkill, isLinear: true, moveDirection: direction, caster: casterCharacter);
        }

        /// <summary>
        /// 지속 장판 AOE 실행
        /// </summary>
        private void ExecuteGroundAOE(
            Transform caster,
            ITargetable target,
            MainSkillData mainSkill,
            SupportSkillData supportSkill,
            GameObject prefab,
            Character casterCharacter = null)
        {
            Vector3 targetPos = GetTargetOrForwardPosition(caster, target);

            // 범위 표시
            if (mainSkill.aoe_radius > 0)
            {
                AOERangeIndicator.Show(targetPos, mainSkill.aoe_radius, AOERangeIndicator.IndicatorType.Damage);
            }

            GameObject aoeObj = Instantiate(prefab, targetPos, Quaternion.identity);
            GetOrAddComponent<SkillAOE>(aoeObj).Initialize(mainSkill, supportSkill, isGround: true, caster: casterCharacter);
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
            GameObject prefab,
            Character casterCharacter = null)
        {
            // 소환 위치 결정 (타겟이 있으면 타겟 위치, 없으면 캐스터 앞)
            Vector3 spawnPos = target != null
                ? target.GetTransform().position
                : caster.position + caster.forward * FORWARD_OFFSET;

            // 이동 방향 (캐스터 → 타겟 방향, 없으면 캐스터 forward)
            Vector3 moveDirection = target != null
                ? (target.GetTransform().position - caster.position).normalized
                : caster.forward;

            // 범위 표시
            if (mainSkill.aoe_radius > 0)
            {
                AOERangeIndicator.Show(spawnPos, mainSkill.aoe_radius, AOERangeIndicator.IndicatorType.Damage);
            }

            GameObject aoeObj = Instantiate(prefab, spawnPos, Quaternion.identity);

            // AOE 컴포넌트 초기화 - Linear로 대체 (이동하며 데미지)
            GetOrAddComponent<SkillAOE>(aoeObj).Initialize(
                mainSkill,
                supportSkill,
                isGround: false,
                isLinear: true,
                moveDirection: moveDirection,
                isFalling: false,
                fallTarget: default,
                caster: casterCharacter
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
            GameObject prefab,
            Character casterCharacter = null)
        {
            Vector3 targetPos = target != null ? target.GetTransform().position : caster.position;

            GameObject effectObj = Instantiate(prefab, targetPos, Quaternion.identity);

            if (target != null && target.IsAlive())
            {
                float damage = CalculateDamage(mainSkill, supportSkill, casterCharacter, out bool isCritical);
                // 상성 시스템 적용 (Issue #531)
                if (casterCharacter != null)
                {
                    target.TakeDamage(damage, isCritical, casterCharacter.GetGenre());
                }
                else
                {
                    target.TakeDamage(damage, isCritical);
                }
            }

            float lifetime = mainSkill.duration > 0 ? mainSkill.duration : DEFAULT_INSTANT_DURATION;
            await UniTask.Delay((int)(lifetime * 1000));

            if (effectObj != null) Destroy(effectObj);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 스킬 발동 사운드 재생
        /// </summary>
        private void PlaySkillSound(int skillId)
        {
            string soundKey = SkillSoundHelper.GetSkillSoundKey(skillId);
            Debug.Log($"[SkillSound] skillId={skillId}, soundKey={soundKey ?? "null"}, AudioManager={AudioManager.Instance != null}");
            if (!string.IsNullOrEmpty(soundKey))
            {
                AudioManager.Instance?.PlaySFX(soundKey);
            }
        }

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

        private void SpawnHitEffect(GameObject hitPrefab, Vector3 position, float scale = 1f)
        {
            if (hitPrefab == null) return;

            GameObject hitEffect = Instantiate(hitPrefab, position, Quaternion.identity);

            // 스케일 적용
            if (scale != 1f && scale > 0f)
            {
                hitEffect.transform.localScale = Vector3.one * scale;
            }

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
        /// 데미지 계산 (Character 스탯 적용)
        /// </summary>
        /// <param name="mainSkill">메인 스킬 데이터</param>
        /// <param name="supportSkill">서포트 스킬 데이터</param>
        /// <param name="casterCharacter">캐스터 캐릭터 (null이면 기본값 사용)</param>
        /// <param name="isCritical">치명타 여부 (out)</param>
        /// <returns>최종 데미지</returns>
        public static float CalculateDamage(MainSkillData mainSkill, SupportSkillData supportSkill, Character casterCharacter, out bool isCritical)
        {
            isCritical = false;
            float damage;

            // Character가 있으면 FinalDamage 사용 (스탯 카드 버프 적용됨)
            if (casterCharacter != null)
            {
                damage = casterCharacter.FinalDamage;

                // BonusDamage 적용 (추가 데미지)
                float bonusDamagePercent = casterCharacter.GetBonusDamageModifier();
                if (bonusDamagePercent > 0)
                {
                    damage *= (1f + bonusDamagePercent / 100f);
                }

                // 치명타 판정
                float critChance = casterCharacter.GetDisplayCritChance(); // 기본 5% + modifier
                if (UnityEngine.Random.Range(0f, 100f) < critChance)
                {
                    isCritical = true;
                    float critMultiplier = casterCharacter.GetDisplayCritMultiplier(); // 기본 150% + modifier
                    damage *= (critMultiplier / 100f);
                }
            }
            else
            {
                // Character가 없으면 기본값 사용 (하위 호환성)
                damage = mainSkill.base_damage;
            }

            // Enhance 서포트의 explosion_ratio를 데미지 배율로 사용
            if (supportSkill != null && supportSkill.IsEnhanceSupport && supportSkill.explosion_ratio > 0)
            {
                damage *= supportSkill.explosion_ratio;
            }

            return damage;
        }

        /// <summary>
        /// 데미지 계산 (하위 호환성용 오버로드)
        /// </summary>
        public static float CalculateDamage(MainSkillData mainSkill, SupportSkillData supportSkill)
        {
            return CalculateDamage(mainSkill, supportSkill, null, out _);
        }

        #endregion
    }
}
