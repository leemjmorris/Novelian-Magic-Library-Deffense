// SkillProjectile.cs - 새로운 래퍼 기반 투사체 시스템
// 에셋 VFX를 감싸는 래퍼 구조로, CSV 데이터 기반으로 동작
// 기존 Projectile.cs의 모든 로직 이식 + 새 구조 적용

namespace Novelian.Combat
{
    using NovelianMagicLibraryDefense.Managers;
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;

    public enum SkillProjectileMode
    {
        Physics,    // Rigidbody 기반 충돌 감지
        Effect      // Visual-only lerp 이동
    }

    /// <summary>
    /// 새로운 래퍼 기반 투사체 시스템
    /// - 에셋 VFX는 자식으로 배치 (Inspector에서 참조)
    /// - CSV 데이터 기반으로 동작
    /// - 서포트 스킬 시스템 완벽 지원
    /// </summary>
    public class SkillProjectile : MonoBehaviour, IPoolable
    {
        private const float OUT_OF_BOUNDS_DISTANCE = 100f;

        #region Inspector References
        [Header("Components - Inspector 참조")]
        [SerializeField, Tooltip("Rigidbody (Physics 모드용)")]
        private Rigidbody rb;

        [SerializeField, Tooltip("Collider (충돌 감지용)")]
        private Collider col;

        [Header("VFX References - 자식 이펙트")]
        [SerializeField, Tooltip("메인 VFX (투사체 본체)")]
        private GameObject vfxMain;

        [SerializeField, Tooltip("꼬리 VFX (옵션)")]
        private GameObject vfxTail;

        [SerializeField, Tooltip("히트 VFX 프리팹")]
        private GameObject hitEffectPrefab;

        [SerializeField, Tooltip("시전 VFX 프리팹")]
        private GameObject castEffectPrefab;

        [Header("Movement Settings")]
        [SerializeField, Tooltip("에셋 스크립트(ObjectMoveDestroy 등)가 이동을 담당하면 true")]
        private bool useAssetMovement = false;
        #endregion

        #region Runtime State
        [Header("Damage")]
        [SerializeField, Tooltip("투사체 데미지")]
        private float damage = 10f;

        // 이동 모드
        private SkillProjectileMode mode = SkillProjectileMode.Physics;
        private System.Action<Vector3> onHitCallback;

        // 스킬 데이터 (CSV 기반)
        private int skillId;
        private MainSkillData skillData;
        private int supportSkillId;
        private SupportSkillData supportSkillData;

        // 체이닝 상태
        private int currentChainCount = 0;
        private int maxChainCount = 0;
        private HashSet<ITargetable> chainHitTargets;
        private float currentChainDamage = 0f;

        // 관통 상태
        private int currentPierceCount = 0;
        private int maxPierceCount = 0;
        private float baseDamageForPierce = 0f;

        // 부메랑 상태
        private bool isBoomerang = false;
        private bool isReturning = false;
        private Vector3 ownerPosition;
        private float boomerangMaxDistance = 0f;
        private float boomerangTraveledDistance = 0f;
        private Dictionary<int, int> boomerangHitCounts;

        // 다이너마이트 상태
        private bool isDynamite = false;
        private float dynamiteFuseTime = 0f;
        private float dynamiteAoeRadius = 0f;
        private bool dynamiteExploded = false;
        private bool dynamiteStopped = false;
        private int dynamiteBounceCount = 0;
        private const int DYNAMITE_MAX_BOUNCES = 3;
        private float dynamiteVerticalVelocity = 0f;
        private float dynamiteHorizontalSpeed = 0f;
        private float dynamiteGravity = 0f;
        private Vector3 dynamiteTargetPosition;

        // 전설의 지팡이 상태
        private bool isLegendaryStaff = false;
        private float legendaryStaffAoeRadius = 0f;
        private float legendaryStaffMaxRange = 0f;
        private float legendaryStaffTraveledDistance = 0f;
        private float legendaryStaffTickInterval = 0.1f;
        private float legendaryStaffLastTickTime = 0f;
        private HashSet<int> legendaryStaffHitTargets;

        // 시한폭탄 상태
        private bool isTimeBomb = false;
        private float timeBombFuseTime = 0f;
        private bool timeBombExploded = false;
        private bool timeBombAttached = false;
        private Transform timeBombAttachTarget = null;
        private GameObject timeBombEffectInstance = null;

        // 유도 상태
        private bool isHoming = false;
        private ITargetable homingTarget = null;
        private float homingTurnSpeed = 5f;

        // 이동 상태
        private Vector3 fixedDirection;
        private float speed;
        private float lifetime;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float elapsedTime;
        private bool isInitialized = false;

        // 치명타 상태
        private float critChance = 5f;
        private float critMultiplier = 150f;

        // 상성 시스템
        private Genre attackerGenre = Genre.Horror;

        // 수명 추적
        private CancellationTokenSource lifetimeCts;
        #endregion

        #region Launch Methods
        /// <summary>
        /// 기본 발사 (하위 호환용)
        /// </summary>
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime)
        {
            Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime, this.damage, 0, 0, 5f, 150f);
        }

        /// <summary>
        /// 스킬 ID 포함 발사 (하위 호환용)
        /// </summary>
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, int mainSkillId, int supportId)
        {
            Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime, damageAmount, mainSkillId, supportId, 5f, 150f);
        }

        /// <summary>
        /// 전체 발사 (치명타 + 상성)
        /// </summary>
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, int mainSkillId, int supportId, float criticalChance, float criticalMultiplier, Genre genre)
        {
            attackerGenre = genre;
            Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime, damageAmount, mainSkillId, supportId, criticalChance, criticalMultiplier);
        }

        /// <summary>
        /// 메인 발사 로직
        /// </summary>
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, int mainSkillId, int supportId, float criticalChance, float criticalMultiplier)
        {
            mode = SkillProjectileMode.Physics;
            transform.position = spawnPos;
            startPosition = spawnPos;
            targetPosition = targetPos;

            fixedDirection = (targetPos - spawnPos).normalized;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            damage = damageAmount;
            elapsedTime = 0f;
            isInitialized = true;

            critChance = criticalChance;
            critMultiplier = criticalMultiplier;

            // Rigidbody 자동 설정
            EnsureRigidbody();

            // Collider 자동 설정
            EnsureCollider();

            // 레이어 설정
            int projectileLayer = LayerMask.NameToLayer("Projectile");
            if (projectileLayer >= 0)
            {
                gameObject.layer = projectileLayer;
            }

            // CSV에서 스킬 데이터 로드
            LoadSkillData(mainSkillId, supportId);

            // VFX 활성화 및 방향 설정
            ActivateVFX(fixedDirection);

            // 특수 상태 초기화
            InitializeSpecialStates(spawnPos, targetPos);

            // 수명 추적 시작
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();
            TrackLifetimeAsync(lifetimeCts.Token).Forget();
        }

        /// <summary>
        /// Effect 모드 발사 (물리 없이 비주얼만)
        /// </summary>
        public void LaunchEffect(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, System.Action<Vector3> onHit = null, int supportId = 0)
        {
            mode = SkillProjectileMode.Effect;
            transform.position = spawnPos;
            startPosition = spawnPos;
            targetPosition = targetPos;

            fixedDirection = (targetPos - spawnPos).normalized;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            damage = damageAmount;
            onHitCallback = onHit;
            elapsedTime = 0f;
            isInitialized = true;

            LoadSkillData(0, supportId);

            transform.rotation = Quaternion.LookRotation(fixedDirection);

            gameObject.layer = LayerMask.NameToLayer("Projectile");

            // Effect 모드용 Kinematic Rigidbody
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Collider 설정
            EnsureCollider();

            // VFX 활성화
            ActivateVFX(fixedDirection);

            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();
            EffectMovementAsync(lifetimeCts.Token).Forget();
        }
        #endregion

        #region Initialization Helpers
        private void EnsureRigidbody()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        private void EnsureCollider()
        {
            if (col == null) col = GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
                sphereCol.isTrigger = true;
                sphereCol.radius = 0.5f;
                col = sphereCol;
            }
        }

        private void ActivateVFX(Vector3 direction)
        {
            if (vfxMain != null)
            {
                vfxMain.SetActive(true);
                vfxMain.transform.localRotation = Quaternion.LookRotation(direction);
            }
            if (vfxTail != null)
            {
                vfxTail.SetActive(true);
            }
        }

        private void LoadSkillData(int mainSkillId, int supportId)
        {
            skillId = mainSkillId;
            supportSkillId = supportId;
            skillData = null;
            supportSkillData = null;

            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogWarning("[SkillProjectile] CSVLoader not initialized");
                return;
            }

            if (mainSkillId > 0)
            {
                skillData = CSVLoader.Instance.GetData<MainSkillData>(mainSkillId);
            }

            if (supportId > 0)
            {
                supportSkillData = CSVLoader.Instance.GetData<SupportSkillData>(supportId);
            }
        }

        private void InitializeSpecialStates(Vector3 spawnPos, Vector3 targetPos)
        {
            // 체이닝 초기화
            if (currentChainCount == 0 && supportSkillData != null && supportSkillData.GetStatusEffectType() == StatusEffectType.Chain)
            {
                maxChainCount = supportSkillData.chain_count;
                chainHitTargets = new HashSet<ITargetable>();
                currentChainDamage = damage;
            }

            // 관통 초기화
            if (currentPierceCount == 0 && !isBoomerang)
            {
                int basePierce = skillData?.pierce_count ?? 0;
                int supportPierce = supportSkillData?.add_pierce ?? 0;

                if (basePierce > 0 || supportPierce > 0)
                {
                    maxPierceCount = basePierce + supportPierce;
                    baseDamageForPierce = damage;
                }
            }

            // 부메랑 초기화
            if (skillData != null && skillData.IsBoomerangSkill && !isReturning)
            {
                isBoomerang = true;
                ownerPosition = spawnPos;
                boomerangMaxDistance = skillData.range;
                boomerangTraveledDistance = 0f;
                boomerangHitCounts = new Dictionary<int, int>();
            }

            // 다이너마이트 초기화
            if (skillData != null && skillData.IsDynamiteSkill)
            {
                isDynamite = true;
                dynamiteFuseTime = skillData.skill_lifetime;
                dynamiteAoeRadius = skillData.aoe_radius;
                dynamiteExploded = false;
                dynamiteStopped = false;
                dynamiteBounceCount = 0;
                dynamiteTargetPosition = targetPos;
                dynamiteHorizontalSpeed = speed;
                dynamiteVerticalVelocity = 4f;
                dynamiteGravity = 15f;
            }

            // 전설의 지팡이 초기화
            if (skillData != null && skillData.IsLegendaryStaffSkill)
            {
                isLegendaryStaff = true;
                legendaryStaffAoeRadius = skillData.aoe_radius;
                legendaryStaffMaxRange = skillData.range;
                legendaryStaffTraveledDistance = 0f;
                legendaryStaffLastTickTime = 0f;
                legendaryStaffHitTargets = new HashSet<int>();
            }

            // 시한폭탄 초기화
            if (skillData != null && skillData.IsTimeBombSkill)
            {
                isTimeBomb = true;
                timeBombFuseTime = skillData.skill_lifetime;
                timeBombExploded = false;
                timeBombAttached = false;
                timeBombAttachTarget = null;
                timeBombEffectInstance = null;
            }

            // 유도 초기화
            if (skillData != null && skillData.is_homing)
            {
                isHoming = true;
                homingTarget = FindNearestEnemy(targetPos);
            }
        }
        #endregion

        #region Movement Updates
        private void FixedUpdate()
        {
            if (mode != SkillProjectileMode.Physics) return;
            if (!isInitialized) return;
            if (Time.timeScale == 0f)
            {
                if (rb != null) rb.linearVelocity = Vector3.zero;
                return;
            }

            // 에셋 스크립트가 이동을 담당하는 경우 (ScriptBased 이펙트)
            // SkillProjectile은 충돌 감지만 담당하고 이동은 에셋 스크립트에 맡김
            if (useAssetMovement)
            {
                // 범위 체크만 수행
                if (Vector3.Distance(startPosition, transform.position) > OUT_OF_BOUNDS_DISTANCE)
                {
                    ReturnToPool();
                }
                return;
            }

            // 특수 투사체 처리
            if (isBoomerang) { UpdateBoomerangMovement(); return; }
            if (isDynamite) { UpdateDynamiteMovement(); return; }
            if (isLegendaryStaff) { UpdateLegendaryStaffMovement(); return; }
            if (isTimeBomb) { UpdateTimeBombMovement(); return; }
            if (isHoming) { UpdateHomingMovement(); return; }

            // 일반 직선 이동
            if (rb != null)
            {
                rb.linearVelocity = fixedDirection * speed;
            }
            else
            {
                transform.position += fixedDirection * speed * Time.fixedDeltaTime;
            }

            if (fixedDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(fixedDirection);
            }

            if (Vector3.Distance(startPosition, transform.position) > OUT_OF_BOUNDS_DISTANCE)
            {
                ReturnToPool();
            }
        }

        private void UpdateBoomerangMovement()
        {
            float moveDistance = speed * Time.fixedDeltaTime;

            if (!isReturning)
            {
                boomerangTraveledDistance += moveDistance;

                if (rb != null)
                    rb.linearVelocity = fixedDirection * speed;
                else
                    transform.position += fixedDirection * speed * Time.fixedDeltaTime;

                if (boomerangTraveledDistance >= boomerangMaxDistance)
                {
                    isReturning = true;
                    fixedDirection = -fixedDirection;
                }
            }
            else
            {
                Vector3 toOwner = (ownerPosition - transform.position);
                float distanceToOwner = toOwner.magnitude;

                if (distanceToOwner <= moveDistance * 2f)
                {
                    ReturnToPool();
                    return;
                }

                fixedDirection = toOwner.normalized;

                if (rb != null)
                    rb.linearVelocity = fixedDirection * speed;
                else
                    transform.position += fixedDirection * speed * Time.fixedDeltaTime;
            }

            if (fixedDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(fixedDirection);
            }
        }

        private void UpdateDynamiteMovement()
        {
            if (dynamiteExploded) return;

            dynamiteFuseTime -= Time.fixedDeltaTime;

            if (dynamiteFuseTime <= 0f)
            {
                ExplodeDynamite();
                return;
            }

            if (dynamiteStopped) return;

            dynamiteVerticalVelocity -= dynamiteGravity * Time.fixedDeltaTime;

            Vector3 horizontalMove = new Vector3(fixedDirection.x, 0f, fixedDirection.z).normalized * dynamiteHorizontalSpeed * Time.fixedDeltaTime;
            float verticalMove = dynamiteVerticalVelocity * Time.fixedDeltaTime;

            Vector3 newPos = transform.position + horizontalMove;
            newPos.y += verticalMove;

            float groundY = 0.5f;
            if (newPos.y <= groundY && dynamiteVerticalVelocity < 0)
            {
                dynamiteBounceCount++;
                newPos.y = groundY;
                dynamiteVerticalVelocity = Mathf.Abs(dynamiteVerticalVelocity) * 0.6f;
                dynamiteHorizontalSpeed *= 0.7f;

                if (dynamiteBounceCount >= DYNAMITE_MAX_BOUNCES)
                {
                    transform.position = newPos;
                    dynamiteStopped = true;
                    if (rb != null) rb.linearVelocity = Vector3.zero;
                    return;
                }
            }

            transform.position = newPos;

            if (rb != null)
            {
                rb.linearVelocity = horizontalMove / Time.fixedDeltaTime + Vector3.up * dynamiteVerticalVelocity;
            }
        }

        private void ExplodeDynamite()
        {
            if (dynamiteExploded) return;
            dynamiteExploded = true;

            if (rb != null) rb.linearVelocity = Vector3.zero;

            Vector3 explosionPos = transform.position;

            // 히트 이펙트 스폰
            SpawnHitEffect(explosionPos);

            // AOE 데미지
            Collider[] hitColliders = Physics.OverlapSphere(explosionPos, dynamiteAoeRadius);
            foreach (var hitCol in hitColliders)
            {
                ApplyDamageToTarget(hitCol);
            }

            ReturnToPool();
        }

        private void UpdateLegendaryStaffMovement()
        {
            float moveDistance = speed * Time.fixedDeltaTime;
            legendaryStaffTraveledDistance += moveDistance;

            transform.position += fixedDirection * moveDistance;

            if (rb != null)
            {
                rb.linearVelocity = fixedDirection * speed;
            }

            if (fixedDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(fixedDirection);
            }

            legendaryStaffLastTickTime += Time.fixedDeltaTime;
            if (legendaryStaffLastTickTime >= legendaryStaffTickInterval)
            {
                legendaryStaffLastTickTime = 0f;
                ApplyLegendaryStaffAOEDamage();
            }

            if (legendaryStaffTraveledDistance >= legendaryStaffMaxRange)
            {
                ReturnToPool();
            }
        }

        private void ApplyLegendaryStaffAOEDamage()
        {
            Vector3 currentPos = transform.position;
            Collider[] hitColliders = Physics.OverlapSphere(currentPos, legendaryStaffAoeRadius);

            foreach (Collider hitCol in hitColliders)
            {
                if (hitCol.CompareTag(Tag.Monster))
                {
                    Monster monster = hitCol.GetComponent<Monster>();
                    if (monster != null)
                    {
                        int instanceId = monster.GetInstanceID();
                        if (legendaryStaffHitTargets.Contains(instanceId)) continue;

                        legendaryStaffHitTargets.Add(instanceId);
                        var (damageToApply, isCrit) = CalculateDamageToApply();
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, monster.GetGenre());
                        monster.TakeDamage(damageToApply * genreMultiplier, isCrit);
                        SpawnHitEffectAtCollider(hitCol);
                    }
                }
                else if (hitCol.CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = hitCol.GetComponent<BossMonster>();
                    if (boss != null)
                    {
                        int instanceId = boss.GetInstanceID();
                        if (legendaryStaffHitTargets.Contains(instanceId)) continue;

                        legendaryStaffHitTargets.Add(instanceId);
                        var (damageToApply, isCrit) = CalculateDamageToApply();
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, boss.GetGenre());
                        boss.TakeDamage(damageToApply * genreMultiplier, isCrit);
                        SpawnHitEffectAtCollider(hitCol);
                    }
                }
            }
        }

        private void UpdateTimeBombMovement()
        {
            if (timeBombExploded) return;

            if (timeBombAttached)
            {
                timeBombFuseTime -= Time.fixedDeltaTime;

                if (timeBombAttachTarget == null)
                {
                    ExplodeTimeBomb();
                    return;
                }

                transform.position = timeBombAttachTarget.position + Vector3.up * 1.5f;

                if (timeBombEffectInstance != null)
                {
                    timeBombEffectInstance.transform.position = timeBombAttachTarget.position + Vector3.up * 1.5f;
                }

                if (timeBombFuseTime <= 0f)
                {
                    ExplodeTimeBomb();
                }
                return;
            }

            if (rb != null)
                rb.linearVelocity = fixedDirection * speed;
            else
                transform.position += fixedDirection * speed * Time.fixedDeltaTime;

            if (fixedDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(fixedDirection);
            }
        }

        private void AttachTimeBombToMonster(Transform monsterTransform)
        {
            if (timeBombAttached || timeBombExploded) return;

            timeBombAttached = true;
            timeBombAttachTarget = monsterTransform;

            lifetimeCts?.Cancel();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            if (col != null) col.enabled = false;

            // VFX 비활성화
            if (vfxMain != null) vfxMain.SetActive(false);
            if (vfxTail != null) vfxTail.SetActive(false);

            // 부착 이펙트 생성
            Vector3 attachPos = monsterTransform.position + Vector3.up * 1.5f;
            if (vfxMain != null)
            {
                timeBombEffectInstance = Instantiate(vfxMain, attachPos, Quaternion.identity);
                timeBombEffectInstance.SetActive(true);
            }
        }

        private void ExplodeTimeBomb()
        {
            if (timeBombExploded) return;
            timeBombExploded = true;

            Vector3 explosionPos = timeBombAttachTarget != null
                ? timeBombAttachTarget.position
                : transform.position;

            SpawnHitEffect(explosionPos);

            if (timeBombEffectInstance != null)
            {
                Destroy(timeBombEffectInstance);
                timeBombEffectInstance = null;
            }

            if (timeBombAttachTarget != null)
            {
                var (damageToApply, isCrit) = CalculateDamageToApply();

                if (timeBombAttachTarget.CompareTag(Tag.Monster))
                {
                    Monster monster = timeBombAttachTarget.GetComponent<Monster>();
                    if (monster != null)
                    {
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, monster.GetGenre());
                        monster.TakeDamage(damageToApply * genreMultiplier, isCrit);
                    }
                }
                else if (timeBombAttachTarget.CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = timeBombAttachTarget.GetComponent<BossMonster>();
                    if (boss != null)
                    {
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, boss.GetGenre());
                        boss.TakeDamage(damageToApply * genreMultiplier, isCrit);
                    }
                }
            }

            ReturnToPool();
        }

        private void UpdateHomingMovement()
        {
            UpdateHomingTarget();

            Vector3 desiredDirection = fixedDirection;

            if (homingTarget != null && homingTarget.IsAlive())
            {
                Vector3 toTarget = homingTarget.GetPosition() - transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > 0.01f)
                {
                    desiredDirection = toTarget.normalized;
                    fixedDirection = Vector3.Slerp(fixedDirection, desiredDirection, homingTurnSpeed * Time.fixedDeltaTime);
                    fixedDirection.Normalize();
                }
            }

            if (rb != null)
                rb.linearVelocity = fixedDirection * speed;
            else
                transform.position += fixedDirection * speed * Time.fixedDeltaTime;

            if (fixedDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(fixedDirection);
            }
        }

        private void UpdateHomingTarget()
        {
            if (homingTarget == null || !homingTarget.IsAlive())
            {
                homingTarget = FindNearestEnemy(transform.position);
            }
        }

        private ITargetable FindNearestEnemy(Vector3 position)
        {
            float searchRadius = 20f;
            Collider[] hits = Physics.OverlapSphere(position, searchRadius);

            ITargetable closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = hit.GetComponent<ITargetable>();
                if (target == null || !target.IsAlive())
                    continue;

                float distance = Vector3.Distance(transform.position, target.GetPosition());
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }
        #endregion

        #region Effect Mode Movement
        private async UniTaskVoid EffectMovementAsync(CancellationToken ct)
        {
            try
            {
                while (isInitialized && !ct.IsCancellationRequested)
                {
                    if (Time.timeScale == 0f)
                    {
                        await UniTask.Yield(ct);
                        continue;
                    }

                    elapsedTime += Time.deltaTime;

                    float distance = Vector3.Distance(startPosition, targetPosition);
                    float t = Mathf.Clamp01(elapsedTime * speed / distance);
                    transform.position = Vector3.Lerp(startPosition, targetPosition, t);

                    if (t >= 1f || elapsedTime >= lifetime)
                    {
                        OnReachTarget();
                        break;
                    }

                    await UniTask.Yield(ct);
                }
            }
            catch (System.OperationCanceledException) { }
        }

        private void OnReachTarget()
        {
            isInitialized = false;
            onHitCallback?.Invoke(targetPosition);
            Destroy(gameObject);
        }

        private async UniTaskVoid TrackLifetimeAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay((int)(lifetime * 1000), cancellationToken: ct);

                if (!ct.IsCancellationRequested)
                {
                    ReturnToPool();
                }
            }
            catch (System.OperationCanceledException) { }
        }
        #endregion

        #region Collision Handling
        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized) return;

            // 벽 무시
            if (other.CompareTag(Tag.Wall)) return;

            // 장애물 충돌
            if (other.CompareTag(Tag.Obstacle))
            {
                if (mode == SkillProjectileMode.Physics)
                    ReturnToPool();
                else
                {
                    lifetimeCts?.Cancel();
                    Destroy(gameObject);
                }
                return;
            }

            // 바닥 충돌
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                if (isDynamite)
                {
                    if (rb != null) rb.linearVelocity = Vector3.zero;
                    return;
                }
                if (isLegendaryStaff) return;

                if (mode == SkillProjectileMode.Physics)
                    ReturnToPool();
                else
                {
                    lifetimeCts?.Cancel();
                    Destroy(gameObject);
                }
                return;
            }

            // 몬스터 충돌
            if (other.CompareTag(Tag.Monster))
            {
                HandleMonsterHit(other);
            }
            else if (other.CompareTag(Tag.BossMonster))
            {
                HandleBossHit(other);
            }
        }

        private void HandleMonsterHit(Collider other)
        {
            Monster monster = other.GetComponent<Monster>();
            if (monster == null) return;

            // 특수 투사체 예외 처리
            if (isDynamite) return;
            if (isLegendaryStaff) return;

            if (isTimeBomb && !timeBombAttached)
            {
                AttachTimeBombToMonster(other.transform);
                return;
            }

            // 부메랑 히트 제한
            if (isBoomerang)
            {
                int instanceId = monster.GetInstanceID();
                if (boomerangHitCounts == null) boomerangHitCounts = new Dictionary<int, int>();

                if (!boomerangHitCounts.TryGetValue(instanceId, out int hitCount)) hitCount = 0;
                if (hitCount >= 2) return;

                boomerangHitCounts[instanceId] = hitCount + 1;
            }

            // 상태이상 적용
            if (supportSkillData != null && supportSkillData.GetStatusEffectType() != StatusEffectType.Chain)
            {
                ApplyStatusEffect(monster);
            }

            // 데미지 계산 및 적용
            var (damageToApply, isCrit) = CalculateDamageToApply();

            // 저체력 보너스 데미지
            if (supportSkillData != null && supportSkillData.IsLowHpBonusSupport)
            {
                damageToApply = DamageCalculator.CalculateLowHpBonusDamage(
                    damageToApply,
                    monster.GetHealth(),
                    monster.GetMaxHealth(),
                    supportSkillData.low_hp_bonus_damage_mult);
            }

            // 상성 배율 적용
            float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, monster.GetGenre());
            monster.TakeDamage(damageToApply * genreMultiplier, isCrit);

            // 히트 이펙트
            SpawnHitEffectAtCollider(other);

            // 체이닝 추적
            if (maxChainCount > 0)
            {
                chainHitTargets.Add(monster);
            }

            // 체이닝 처리
            if (supportSkillData != null && supportSkillData.GetStatusEffectType() == StatusEffectType.Chain && currentChainCount < maxChainCount)
            {
                ITargetable nextTarget = FindNextChainTarget(other.transform.position, chainHitTargets, monster);

                if (nextTarget != null)
                {
                    currentChainCount++;
                    float reductionRate = supportSkillData.chain_damage_reduction / 100f;
                    currentChainDamage = DamageCalculator.CalculatePierceChainDamage(baseDamageForPierce > 0 ? baseDamageForPierce : damage, reductionRate, currentChainCount);

                    Vector3 directionToNext = (nextTarget.GetPosition() - other.transform.position).normalized;
                    Vector3 spawnPos = other.transform.position + directionToNext * 1.0f;

                    Launch(spawnPos, nextTarget.GetPosition(), speed, lifetime, currentChainDamage, skillId, supportSkillId);
                    return;
                }
            }

            // 파편화 처리
            if (supportSkillId == 40002 && supportSkillData != null && supportSkillData.add_projectiles > 0)
            {
                int totalFragments = 1 + supportSkillData.add_projectiles;
                SpawnFragmentProjectilesFan(other.transform.position, totalFragments, fixedDirection, other);
            }

            // 관통 처리
            if (maxPierceCount > 0 && currentPierceCount < maxPierceCount)
            {
                currentPierceCount++;
                return;
            }

            // 부메랑은 계속 진행
            if (isBoomerang) return;

            // 정리
            if (mode == SkillProjectileMode.Physics)
                ReturnToPool();
            else
            {
                lifetimeCts?.Cancel();
                onHitCallback?.Invoke(other.transform.position);
                Destroy(gameObject);
            }
        }

        private void HandleBossHit(Collider other)
        {
            BossMonster boss = other.GetComponent<BossMonster>();
            if (boss == null) return;

            // 특수 투사체 예외 처리
            if (isDynamite) return;
            if (isLegendaryStaff) return;

            if (isTimeBomb && !timeBombAttached)
            {
                AttachTimeBombToMonster(other.transform);
                return;
            }

            // 부메랑 히트 제한
            if (isBoomerang)
            {
                int instanceId = boss.GetInstanceID();
                if (boomerangHitCounts == null) boomerangHitCounts = new Dictionary<int, int>();

                if (!boomerangHitCounts.TryGetValue(instanceId, out int hitCount)) hitCount = 0;
                if (hitCount >= 2) return;

                boomerangHitCounts[instanceId] = hitCount + 1;
            }

            // 상태이상 적용
            if (supportSkillData != null && supportSkillData.GetStatusEffectType() != StatusEffectType.Chain)
            {
                ApplyStatusEffectToBoss(boss);
            }

            // 데미지 계산 및 적용
            var (damageToApply, isCrit) = CalculateDamageToApply();

            // 저체력 보너스 데미지
            if (supportSkillData != null && supportSkillData.IsLowHpBonusSupport)
            {
                damageToApply = DamageCalculator.CalculateLowHpBonusDamage(
                    damageToApply,
                    boss.GetHealth(),
                    boss.GetMaxHealth(),
                    supportSkillData.low_hp_bonus_damage_mult);
            }

            // 상성 배율 적용
            float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, boss.GetGenre());
            boss.TakeDamage(damageToApply * genreMultiplier, isCrit);

            // 히트 이펙트
            SpawnHitEffectAtCollider(other);

            // 체이닝 추적
            if (maxChainCount > 0)
            {
                chainHitTargets.Add(boss);
            }

            // 체이닝 처리
            if (supportSkillData != null && supportSkillData.GetStatusEffectType() == StatusEffectType.Chain && currentChainCount < maxChainCount)
            {
                ITargetable nextTarget = FindNextChainTarget(other.transform.position, chainHitTargets, boss);

                if (nextTarget != null)
                {
                    currentChainCount++;
                    float reductionRate = supportSkillData.chain_damage_reduction / 100f;
                    currentChainDamage = DamageCalculator.CalculatePierceChainDamage(baseDamageForPierce > 0 ? baseDamageForPierce : damage, reductionRate, currentChainCount);

                    Vector3 directionToNext = (nextTarget.GetPosition() - other.transform.position).normalized;
                    Vector3 spawnPos = other.transform.position + directionToNext * 1.0f;

                    Launch(spawnPos, nextTarget.GetPosition(), speed, lifetime, currentChainDamage, skillId, supportSkillId);
                    return;
                }
            }

            // 파편화 처리
            if (supportSkillId == 40002 && supportSkillData != null && supportSkillData.add_projectiles > 0)
            {
                int totalFragments = 1 + supportSkillData.add_projectiles;
                SpawnFragmentProjectilesFan(other.transform.position, totalFragments, fixedDirection, other);
            }

            // 관통 처리
            if (maxPierceCount > 0 && currentPierceCount < maxPierceCount)
            {
                currentPierceCount++;
                return;
            }

            // 부메랑은 계속 진행
            if (isBoomerang) return;

            // 정리
            if (mode == SkillProjectileMode.Physics)
                ReturnToPool();
            else
            {
                lifetimeCts?.Cancel();
                onHitCallback?.Invoke(other.transform.position);
                Destroy(gameObject);
            }
        }
        #endregion

        #region Damage Calculation
        private (float damage, bool isCritical) CalculateDamageToApply()
        {
            float baseDamage;

            if (maxChainCount > 0)
            {
                baseDamage = currentChainDamage;
            }
            else if (maxPierceCount > 0 && currentPierceCount > 0)
            {
                float reductionRate = supportSkillData?.chain_damage_reduction / 100f ?? 0.3f;
                baseDamage = DamageCalculator.CalculatePierceChainDamage(baseDamageForPierce, reductionRate, currentPierceCount);
            }
            else
            {
                baseDamage = damage;
            }

            return DamageCalculator.CalculateCriticalDamage(baseDamage, critChance, critMultiplier);
        }

        private void ApplyDamageToTarget(Collider hitCol)
        {
            if (hitCol.CompareTag(Tag.Monster))
            {
                Monster monster = hitCol.GetComponent<Monster>();
                if (monster != null)
                {
                    var (damageToApply, isCrit) = CalculateDamageToApply();
                    float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, monster.GetGenre());
                    monster.TakeDamage(damageToApply * genreMultiplier, isCrit);
                }
            }
            else if (hitCol.CompareTag(Tag.BossMonster))
            {
                BossMonster boss = hitCol.GetComponent<BossMonster>();
                if (boss != null)
                {
                    var (damageToApply, isCrit) = CalculateDamageToApply();
                    float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, boss.GetGenre());
                    boss.TakeDamage(damageToApply * genreMultiplier, isCrit);
                }
            }
        }
        #endregion

        #region Status Effects
        private void ApplyStatusEffect(Monster monster)
        {
            if (monster == null) return;

            // MainSkillData 효과
            if (skillData != null)
            {
                if (skillData.HasCCEffect)
                {
                    monster.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, hitEffectPrefab);
                }
                if (skillData.HasDOTEffect)
                {
                    monster.ApplyDOT(DOTType.Burn, skillData.dot_damage_per_tick, skillData.dot_tick_interval, skillData.dot_duration, hitEffectPrefab);
                }
                if (skillData.HasMarkEffect)
                {
                    monster.ApplyMark(skillData.GetElementBasedMarkType(), skillData.mark_duration, skillData.mark_damage_mult / 100f, hitEffectPrefab);
                }
            }

            // SupportSkillData 효과
            if (supportSkillData != null)
            {
                switch (supportSkillData.GetStatusEffectType())
                {
                    case StatusEffectType.CC:
                        monster.ApplyCC(supportSkillData.GetCCType(), supportSkillData.cc_duration, supportSkillData.cc_slow_amount, hitEffectPrefab);
                        break;
                    case StatusEffectType.DOT:
                        monster.ApplyDOT(supportSkillData.GetDOTType(), supportSkillData.dot_damage_per_tick, supportSkillData.dot_tick_interval, supportSkillData.dot_duration, hitEffectPrefab);
                        break;
                    case StatusEffectType.Mark:
                        monster.ApplyMark(supportSkillData.GetMarkType(), supportSkillData.mark_duration, supportSkillData.mark_damage_mult, hitEffectPrefab);
                        break;
                }
            }
        }

        private void ApplyStatusEffectToBoss(BossMonster boss)
        {
            if (boss == null) return;

            // MainSkillData 효과
            if (skillData != null)
            {
                if (skillData.HasCCEffect)
                {
                    boss.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, hitEffectPrefab);
                }
                if (skillData.HasDOTEffect)
                {
                    boss.ApplyDOT(DOTType.Burn, skillData.dot_damage_per_tick, skillData.dot_tick_interval, skillData.dot_duration, hitEffectPrefab);
                }
                if (skillData.HasMarkEffect)
                {
                    boss.ApplyMark(skillData.GetElementBasedMarkType(), skillData.mark_duration, skillData.mark_damage_mult / 100f, hitEffectPrefab);
                }
            }

            // SupportSkillData 효과
            if (supportSkillData != null)
            {
                switch (supportSkillData.GetStatusEffectType())
                {
                    case StatusEffectType.CC:
                        boss.ApplyCC(supportSkillData.GetCCType(), supportSkillData.cc_duration, supportSkillData.cc_slow_amount, hitEffectPrefab);
                        break;
                    case StatusEffectType.DOT:
                        boss.ApplyDOT(supportSkillData.GetDOTType(), supportSkillData.dot_damage_per_tick, supportSkillData.dot_tick_interval, supportSkillData.dot_duration, hitEffectPrefab);
                        break;
                    case StatusEffectType.Mark:
                        boss.ApplyMark(supportSkillData.GetMarkType(), supportSkillData.mark_duration, supportSkillData.mark_damage_mult, hitEffectPrefab);
                        break;
                }
            }
        }
        #endregion

        #region Chain & Fragmentation
        private ITargetable FindNextChainTarget(Vector3 currentPosition, HashSet<ITargetable> hitTargets, ITargetable excludeTarget = null)
        {
            if (supportSkillData == null) return null;

            Collider[] hits = Physics.OverlapSphere(currentPosition, supportSkillData.chain_range);

            ITargetable closestTarget = null;
            float closestDistance = float.MaxValue;
            const float MIN_CHAIN_DISTANCE = 0.5f;

            foreach (var hit in hits)
            {
                if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = hit.GetComponent<ITargetable>();
                if (target == null || !target.IsAlive())
                    continue;

                if (excludeTarget != null && target == excludeTarget)
                    continue;

                if (hitTargets.Contains(target))
                    continue;

                float distance = Vector3.Distance(currentPosition, target.GetPosition());

                if (distance < MIN_CHAIN_DISTANCE)
                    continue;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        private void SpawnFragmentProjectilesFan(Vector3 hitPosition, int totalCount, Vector3 originalDirection, Collider hitCollider)
        {
            if (totalCount <= 0) return;

            var pool = GameManager.Instance?.Pool;
            if (pool == null || !pool.HasPool<SkillProjectile>()) return;

            float spreadAngle = 30f;
            float projectileHeight = startPosition.y;

            float passOffset = 2.5f;
            if (hitCollider != null)
            {
                passOffset = hitCollider.bounds.extents.magnitude + 1.0f;
            }
            float spreadOffset = 0.5f;

            for (int i = 0; i < totalCount; i++)
            {
                float angleOffset = 0f;
                if (totalCount > 1)
                {
                    angleOffset = spreadAngle * (i - (totalCount - 1) / 2f);
                }

                Vector3 fragmentDirection = Quaternion.Euler(0, angleOffset, 0) * originalDirection;
                Vector3 adjustedHitPos = new Vector3(hitPosition.x, projectileHeight, hitPosition.z);
                Vector3 behindMonster = adjustedHitPos + originalDirection * passOffset;
                Vector3 spawnPos = behindMonster + fragmentDirection * spreadOffset;
                Vector3 targetPos = spawnPos + fragmentDirection * 50f;

                SkillProjectile fragment = pool.Spawn<SkillProjectile>(spawnPos);
                if (fragment == null) continue;

                fragment.Launch(spawnPos, targetPos, speed, lifetime, damage, skillId, 0, critChance, critMultiplier);
            }
        }
        #endregion

        #region VFX Helpers
        private void SpawnHitEffect(Vector3 position)
        {
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }

        private void SpawnHitEffectAtCollider(Collider col)
        {
            if (hitEffectPrefab != null && col != null)
            {
                Vector3 hitPos = col.bounds.center;
                GameObject effect = Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
        #endregion

        #region Pool Management
        private void ReturnToPool()
        {
            lifetimeCts?.Cancel();
            GameManager.Instance?.Pool?.Despawn(this);
        }

        public void OnSpawn()
        {
            mode = SkillProjectileMode.Physics;
            isInitialized = false;
            fixedDirection = Vector3.zero;
            elapsedTime = 0f;
            onHitCallback = null;

            if (rb != null) rb.linearVelocity = Vector3.zero;

            // VFX 초기화
            if (vfxMain != null) vfxMain.SetActive(true);
            if (vfxTail != null) vfxTail.SetActive(true);

            // ParticleSystem 리셋
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }

        public void OnDespawn()
        {
            isInitialized = false;
            fixedDirection = Vector3.zero;
            elapsedTime = 0f;
            onHitCallback = null;

            // 스킬 데이터 초기화
            skillId = 0;
            skillData = null;
            supportSkillId = 0;
            supportSkillData = null;

            // 체이닝 초기화
            currentChainCount = 0;
            maxChainCount = 0;
            chainHitTargets = null;
            currentChainDamage = 0f;

            // 관통 초기화
            currentPierceCount = 0;
            maxPierceCount = 0;
            baseDamageForPierce = 0f;

            // 부메랑 초기화
            isBoomerang = false;
            isReturning = false;
            ownerPosition = Vector3.zero;
            boomerangMaxDistance = 0f;
            boomerangTraveledDistance = 0f;
            boomerangHitCounts = null;

            // 다이너마이트 초기화
            isDynamite = false;
            dynamiteFuseTime = 0f;
            dynamiteAoeRadius = 0f;
            dynamiteExploded = false;
            dynamiteStopped = false;
            dynamiteBounceCount = 0;
            dynamiteVerticalVelocity = 0f;
            dynamiteHorizontalSpeed = 0f;
            dynamiteGravity = 0f;
            dynamiteTargetPosition = Vector3.zero;

            // 전설의 지팡이 초기화
            isLegendaryStaff = false;
            legendaryStaffAoeRadius = 0f;
            legendaryStaffMaxRange = 0f;
            legendaryStaffTraveledDistance = 0f;
            legendaryStaffLastTickTime = 0f;
            legendaryStaffHitTargets = null;

            // 시한폭탄 초기화
            isTimeBomb = false;
            timeBombFuseTime = 0f;
            timeBombExploded = false;
            timeBombAttached = false;
            timeBombAttachTarget = null;
            if (timeBombEffectInstance != null)
            {
                Destroy(timeBombEffectInstance);
                timeBombEffectInstance = null;
            }

            // 유도 초기화
            isHoming = false;
            homingTarget = null;

            // 상성 초기화
            attackerGenre = Genre.Horror;

            // Rigidbody 복원
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }

            // Collider 복원
            if (col != null) col.enabled = true;

            // VFX 복원
            if (vfxMain != null) vfxMain.SetActive(true);
            if (vfxTail != null) vfxTail.SetActive(true);

            lifetimeCts?.Cancel();
        }

        private void OnDestroy()
        {
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
        }
        #endregion
    }
}
