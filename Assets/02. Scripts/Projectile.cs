#if false
// LMJ: Old Projectile disabled for combat system refactor (Issue #265)
// Will be replaced with straight-line projectile
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

//JML: Projectile with Rigidbody-based movement and target tracking
public class Projectile_OLD : MonoBehaviour, IPoolable
{
    // Old code preserved for reference
}
#endif

//LMJ : Unified projectile system - supports both physics and effect modes
//      Migrated to new CSV-based skill system
namespace Novelian.Combat
{
    using NovelianMagicLibraryDefense.Managers;
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;

    public enum ProjectileMode
    {
        Physics,    // Rigidbody-based with collision detection
        Effect      // Visual-only with lerp movement
    }

    public class Projectile : MonoBehaviour, IPoolable
    {
        private const float OUT_OF_BOUNDS_DISTANCE = 100f;

        [Header("Components")]
        [SerializeField, Tooltip("Rigidbody for physics movement (Physics mode only)")]
        private Rigidbody rb;

        [Header("Damage")]
        [SerializeField, Tooltip("Projectile damage")]
        private float damage = 10f;

        // Movement mode
        private ProjectileMode mode = ProjectileMode.Physics;
        private System.Action<Vector3> onHitCallback;

        // Skill data (CSV-based)
        private int skillId;
        private MainSkillData skillData;
        private MainSkillPrefabEntry skillPrefabs;

        // Support skill data for status effects (CSV-based)
        private int supportSkillId;
        private SupportSkillData supportSkillData;

        // Chain state tracking
        private int currentChainCount = 0;
        private int maxChainCount = 0;
        private System.Collections.Generic.HashSet<ITargetable> chainHitTargets;
        private float currentChainDamage = 0f;

        // Pierce state tracking (관통 시스템)
        private int currentPierceCount = 0;
        private int maxPierceCount = 0;
        private float baseDamageForPierce = 0f; // 관통 데미지 감소 계산을 위한 기본 데미지

        // Boomerang state tracking (부메랑 시스템)
        private bool isBoomerang = false;
        private bool isReturning = false;
        private Vector3 ownerPosition; // 발사자 위치 (돌아올 목표)
        private float boomerangMaxDistance = 0f; // 최대 이동 거리
        private float boomerangTraveledDistance = 0f; // 현재 이동한 거리
        private System.Collections.Generic.Dictionary<int, int> boomerangHitCounts; // 적별 히트 횟수 (instanceID -> hitCount)

        // Dynamite state tracking (다이너마이트 시스템 - 던진 후 N초 뒤 폭발)
        private bool isDynamite = false;
        private float dynamiteFuseTime = 0f; // 폭발까지 남은 시간
        private float dynamiteAoeRadius = 0f; // 폭발 범위
        private bool dynamiteExploded = false; // 이미 폭발했는지 체크
        private bool dynamiteStopped = false; // 3번 튕긴 후 멈춤 상태
        private int dynamiteBounceCount = 0; // 현재 튕긴 횟수
        private const int DYNAMITE_MAX_BOUNCES = 3; // 최대 튕김 횟수
        private float dynamiteVerticalVelocity = 0f; // Y축 속도 (튕김용)
        private float dynamiteHorizontalSpeed = 0f; // 수평 속도 (동적 계산)
        private float dynamiteGravity = 0f; // 중력 (동적 계산)
        private Vector3 dynamiteTargetPosition; // 목표 위치

        // Legendary Staff state tracking (전설의 지팡이 - 일직선 이동하며 경로상 AOE 데미지)
        private bool isLegendaryStaff = false;
        private float legendaryStaffAoeRadius = 0f; // 경로상 AOE 범위
        private float legendaryStaffMaxRange = 0f; // 최대 사거리
        private float legendaryStaffTraveledDistance = 0f; // 현재 이동 거리
        private float legendaryStaffTickInterval = 0.1f; // AOE 틱 간격 (초)
        private float legendaryStaffLastTickTime = 0f; // 마지막 틱 시간
        private System.Collections.Generic.HashSet<int> legendaryStaffHitTargets; // 이미 맞은 적 목록

        // Time Bomb state tracking (의문의 예고장 - 몬스터에 부착 후 시간 뒤 폭발)
        private bool isTimeBomb = false;
        private float timeBombFuseTime = 0f; // 폭발까지 남은 시간
        private bool timeBombExploded = false; // 이미 폭발했는지 체크
        private bool timeBombAttached = false; // 몬스터에 부착되었는지 체크
        private Transform timeBombAttachTarget = null; // 부착된 몬스터 Transform
        private GameObject timeBombEffectInstance = null; // 몬스터에 부착된 이펙트

        // Movement state
        private Vector3 fixedDirection;
        private float speed;
        private float lifetime;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float elapsedTime;
        private bool isInitialized = false;

        // Critical hit state (치명타 시스템)
        private float critChance = 5f;      // 기본 5%
        private float critMultiplier = 150f; // 기본 150%

        // Genre system (상성 시스템)
        private Genre attackerGenre = Genre.Horror; // 발사자 장르 (기본값: Horror)

        // Lifetime tracking
        private CancellationTokenSource lifetimeCts;

        //LMJ : Launch projectile in Physics mode - basic version (for backward compatibility)
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime)
        {
            Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime, this.damage, 0, 0, 5f, 150f);
        }

        //LMJ : Launch projectile in Physics mode - with skill IDs (backward compatibility without crit)
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, int mainSkillId, int supportId)
        {
            Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime, damageAmount, mainSkillId, supportId, 5f, 150f);
        }

        //LMJ : Launch projectile in Physics mode - full version with crit and genre support (치명타 + 상성 시스템)
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, int mainSkillId, int supportId, float criticalChance, float criticalMultiplier, Genre genre)
        {
            attackerGenre = genre;
            Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime, damageAmount, mainSkillId, supportId, criticalChance, criticalMultiplier);
        }

        //LMJ : Launch projectile in Physics mode - full version with crit support (치명타 시스템)
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, int mainSkillId, int supportId, float criticalChance, float criticalMultiplier)
        {
            mode = ProjectileMode.Physics;
            transform.position = spawnPos;
            startPosition = spawnPos;
            targetPosition = targetPos;

            // Calculate fixed direction (NO HOMING)
            fixedDirection = (targetPos - spawnPos).normalized;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            damage = damageAmount;
            elapsedTime = 0f;
            isInitialized = true;

            // 치명타 정보 저장
            critChance = criticalChance;
            critMultiplier = criticalMultiplier;

            // Rigidbody 자동 추가 (없으면 Physics 충돌이 작동하지 않음)
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.isKinematic = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    Debug.Log("[Projectile] Rigidbody auto-added for physics movement");
                }
            }

            // Collider 자동 추가 (없으면 충돌 감지가 작동하지 않음)
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
                sphereCol.isTrigger = true;
                sphereCol.radius = 0.5f;
                Debug.Log("[Projectile] SphereCollider auto-added for collision detection");
            }

            // 레이어 설정 (Projectile 레이어)
            int projectileLayer = LayerMask.NameToLayer("Projectile");
            if (projectileLayer >= 0)
            {
                gameObject.layer = projectileLayer;
            }

            // Load skill data from CSV and PrefabDatabase
            LoadSkillData(mainSkillId, supportId);

            // JML: Launch 로그 제거

            // Spawn effect as INDEPENDENT object (not child) - 이펙트는 Projectile despawn과 무관하게 재생 완료
            if (mainSkillId > 0)
            {
                // Clear any existing child effects (legacy cleanup)
                foreach (Transform child in transform)
                {
                    if (child.gameObject != null)
                    {
                        Object.Destroy(child.gameObject);
                    }
                }

                // 독립 이펙트 스폰 (SkillEffectManager 사용)
                SpawnIndependentEffectAsync(mainSkillId, fixedDirection).Forget();
            }

            // Initialize Chain state (only on first launch, not re-launch)
            if (currentChainCount == 0 && supportSkillData != null && supportSkillData.GetStatusEffectType() == StatusEffectType.Chain)
            {
                maxChainCount = supportSkillData.chain_count;
                chainHitTargets = new System.Collections.Generic.HashSet<ITargetable>();
                currentChainDamage = damageAmount;
                // JML: Chain init 로그 제거
            }

            // Initialize Pierce state (관통 시스템 - 메인 스킬 또는 서포트 스킬에서 관통 가능)
            // 부메랑 스킬은 pierce 시스템 대신 boomerang 시스템 사용
            if (currentPierceCount == 0 && !isBoomerang)
            {
                int basePierce = skillData?.pierce_count ?? 0;
                int supportPierce = supportSkillData?.add_pierce ?? 0;

                // 디버그: Pierce 초기화 상태 확인
                Debug.Log($"[Projectile] Pierce Init - supportSkillId={supportSkillId}, supportSkillData={(supportSkillData != null ? "OK" : "NULL")}, basePierce={basePierce}, supportPierce={supportPierce}");

                if (basePierce > 0 || supportPierce > 0)
                {
                    maxPierceCount = basePierce + supportPierce;
                    baseDamageForPierce = damageAmount;
                    Debug.Log($"[Projectile] Pierce ENABLED! maxPierceCount={maxPierceCount}");
                }
            }

            // Initialize Boomerang state (부메랑 시스템)
            if (skillData != null && skillData.IsBoomerangSkill && !isReturning)
            {
                isBoomerang = true;
                ownerPosition = spawnPos;
                boomerangMaxDistance = skillData.range; // CSV의 range 값을 최대 거리로 사용
                boomerangTraveledDistance = 0f;
                boomerangHitCounts = new System.Collections.Generic.Dictionary<int, int>();
                Debug.Log($"[Projectile] Boomerang initialized: maxDistance={boomerangMaxDistance}, ownerPos={ownerPosition}");
            }

            // Initialize Dynamite state (다이너마이트 시스템 - 단순화된 3회 튕김 + 폭발)
            if (skillData != null && skillData.IsDynamiteSkill)
            {
                isDynamite = true;
                dynamiteFuseTime = skillData.skill_lifetime; // skill_lifetime을 폭발 딜레이로 사용 (CSV: 2초)
                dynamiteAoeRadius = skillData.aoe_radius; // 폭발 범위 (CSV: 5)
                dynamiteExploded = false;
                dynamiteStopped = false;
                dynamiteBounceCount = 0;
                dynamiteTargetPosition = targetPos;

                // 단순화: 고정된 물리값 사용 (가까운 적에게 던지는 폭탄)
                dynamiteHorizontalSpeed = speed; // CSV의 projectile_speed 사용 (8)
                dynamiteVerticalVelocity = 4f; // 낮은 포물선
                dynamiteGravity = 15f; // 빠르게 떨어지는 중력

                Debug.Log($"[Projectile] Dynamite initialized: fuseTime={dynamiteFuseTime}s, aoeRadius={dynamiteAoeRadius}, speed={dynamiteHorizontalSpeed}");
            }

            // Initialize Legendary Staff state (전설의 지팡이 - 일직선 이동하며 경로상 AOE 데미지)
            if (skillData != null && skillData.IsLegendaryStaffSkill)
            {
                isLegendaryStaff = true;
                legendaryStaffAoeRadius = skillData.aoe_radius; // 경로상 AOE 범위
                legendaryStaffMaxRange = skillData.range; // 최대 사거리
                legendaryStaffTraveledDistance = 0f;
                legendaryStaffLastTickTime = 0f;
                legendaryStaffHitTargets = new System.Collections.Generic.HashSet<int>();
                Debug.Log($"[Projectile] LegendaryStaff initialized: aoeRadius={legendaryStaffAoeRadius}, maxRange={legendaryStaffMaxRange}");
            }

            // Initialize Time Bomb state (의문의 예고장 - 몬스터에 부착 후 시간 뒤 폭발)
            if (skillData != null && skillData.IsTimeBombSkill)
            {
                isTimeBomb = true;
                timeBombFuseTime = skillData.skill_lifetime; // 부착 후 폭발까지 시간
                timeBombExploded = false;
                timeBombAttached = false;
                timeBombAttachTarget = null;
                timeBombEffectInstance = null;
                Debug.Log($"[Projectile] TimeBomb initialized: fuseTime={timeBombFuseTime}s");
            }

            // Cancel previous lifetime token
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();

            // Start lifetime countdown
            TrackLifetimeAsync(lifetimeCts.Token).Forget();
        }

        //LMJ : Load skill data from CSV and SkillEffectDatabase (fully migrated - no legacy fallback)
        private void LoadSkillData(int mainSkillId, int supportId)
        {
            skillId = mainSkillId;
            supportSkillId = supportId;
            skillData = null;
            skillPrefabs = null;
            supportSkillData = null;

            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogWarning("[Projectile] CSVLoader not initialized");
                return;
            }

            // Load main skill data from CSV
            if (mainSkillId > 0)
            {
                skillData = CSVLoader.Instance.GetData<MainSkillData>(mainSkillId);
                if (skillData != null)
                {
                    // Use new SkillEffectDatabase exclusively
                    var effectDb = SkillEffectDatabase.Instance;
                    var effectEntry = effectDb?.GetEntry(mainSkillId);
                    if (effectEntry != null && effectEntry.HasMainEffect())
                    {
                        // Convert SkillEffectEntry to MainSkillPrefabEntry for compatibility
                        skillPrefabs = new MainSkillPrefabEntry
                        {
                            skillId = mainSkillId,
                            skillName = effectEntry.skillName,
                            projectilePrefab = effectEntry.mainEffectPrefab,
                            hitEffectPrefab = effectEntry.hitEffectPrefab,
                            castEffectPrefab = effectEntry.castEffectPrefab,
                            trailEffectPrefab = effectEntry.trailEffectPrefab,
                            areaEffectPrefab = effectEntry.areaEffectPrefab
                        };
                    }
                    // No fallback - if not in SkillEffectDatabase, skillPrefabs remains null
                }
            }

            // Load support skill data from CSV (support prefabs handled separately)
            if (supportId > 0)
            {
                supportSkillData = CSVLoader.Instance.GetData<SupportSkillData>(supportId);
                // Support prefabs are optional - effect modifiers come from CSV data
            }
        }

        //LMJ : Launch projectile in Effect mode (for visual-only projectiles without physics)
        public void LaunchEffect(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, System.Action<Vector3> onHit = null, int supportId = 0)
        {
            mode = ProjectileMode.Effect;
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

            // Load support skill data
            LoadSkillData(0, supportId);

            transform.rotation = Quaternion.LookRotation(fixedDirection);

            // Set layer to Projectile for proper collision detection
            gameObject.layer = LayerMask.NameToLayer("Projectile");

            // Add Kinematic Rigidbody for collision detection (required for Trigger detection)
            Rigidbody effectRb = gameObject.GetComponent<Rigidbody>();
            if (effectRb == null)
            {
                effectRb = gameObject.AddComponent<Rigidbody>();
            }
            effectRb.isKinematic = true;
            effectRb.useGravity = false;
            effectRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Add SphereCollider for collision detection
            SphereCollider collider = gameObject.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }
            collider.isTrigger = true;
            collider.radius = 1.0f;

            // Cancel previous lifetime token
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();

            // Start effect movement
            EffectMovementAsync(lifetimeCts.Token).Forget();
        }

        //LMJ : Physics-based movement in fixed direction (Physics mode only)
        private void FixedUpdate()
        {
            if (mode != ProjectileMode.Physics) return;
            if (!isInitialized)
            {
                // Debug.Log($"[Projectile] FixedUpdate skipped: isInitialized={isInitialized}");
                return;
            }

            if (Time.timeScale == 0f)
            {
                if (rb != null) rb.linearVelocity = Vector3.zero;
                return;
            }

            // 부메랑 이동 처리
            if (isBoomerang)
            {
                UpdateBoomerangMovement();
                return;
            }

            // 다이너마이트 이동 및 폭발 처리
            if (isDynamite)
            {
                UpdateDynamiteMovement();
                return;
            }

            // 전설의 지팡이 이동 및 경로상 AOE 데미지 처리
            if (isLegendaryStaff)
            {
                UpdateLegendaryStaffMovement();
                return;
            }

            // 시한폭탄(의문의 예고장) 처리 - 부착 후 타이머
            if (isTimeBomb)
            {
                UpdateTimeBombMovement();
                return;
            }

            // Rigidbody가 있으면 velocity로 이동, 없으면 transform 직접 이동
            if (rb != null)
            {
                rb.linearVelocity = fixedDirection * speed;
            }
            else
            {
                // Fallback: Rigidbody 없이 직접 이동
                transform.position += fixedDirection * speed * Time.fixedDeltaTime;
            }

            if (fixedDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(fixedDirection);
            }

            // 이동 거리 체크용 디버그 (매 프레임마다 출력하면 로그가 많아지므로 주석 처리)
            // float distance = Vector3.Distance(startPosition, transform.position);
            // Debug.Log($"[Projectile] Moving: pos={transform.position}, dir={fixedDirection}, speed={speed}, distance={distance:F1}");

            if (Vector3.Distance(startPosition, transform.position) > OUT_OF_BOUNDS_DISTANCE)
            {
                ReturnToPool();
            }
        }

        //LMJ : Boomerang movement - go forward then return to owner
        private void UpdateBoomerangMovement()
        {
            float moveDistance = speed * Time.fixedDeltaTime;

            if (!isReturning)
            {
                // 전진 중
                boomerangTraveledDistance += moveDistance;

                if (rb != null)
                {
                    rb.linearVelocity = fixedDirection * speed;
                }
                else
                {
                    transform.position += fixedDirection * speed * Time.fixedDeltaTime;
                }

                // 최대 거리 도달 시 되돌아오기 시작
                if (boomerangTraveledDistance >= boomerangMaxDistance)
                {
                    isReturning = true;
                    fixedDirection = -fixedDirection; // 방향 반전
                    Debug.Log($"[Projectile] Boomerang returning: traveled={boomerangTraveledDistance:F1}, maxDist={boomerangMaxDistance}");
                }
            }
            else
            {
                // 되돌아오는 중
                Vector3 toOwner = (ownerPosition - transform.position);
                float distanceToOwner = toOwner.magnitude;

                if (distanceToOwner <= moveDistance * 2f)
                {
                    // 발사자에게 도착 - 풀로 반환
                    Debug.Log($"[Projectile] Boomerang returned to owner");
                    ReturnToPool();
                    return;
                }

                // 발사자 방향으로 이동
                fixedDirection = toOwner.normalized;

                if (rb != null)
                {
                    rb.linearVelocity = fixedDirection * speed;
                }
                else
                {
                    transform.position += fixedDirection * speed * Time.fixedDeltaTime;
                }
            }

            // 회전 업데이트
            if (fixedDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(fixedDirection);
            }
        }

        //LMJ : Dynamite movement - bouncing to target position (타겟 거리 기반 동적 계산)
        private void UpdateDynamiteMovement()
        {
            if (dynamiteExploded) return;

            // 퓨즈 타이머 감소 (멈춰있든 이동중이든 항상 감소)
            dynamiteFuseTime -= Time.fixedDeltaTime;

            // 퓨즈 타이머 완료 시 폭발 (유일한 폭발 조건)
            if (dynamiteFuseTime <= 0f)
            {
                ExplodeDynamite();
                return;
            }

            // 이미 멈춘 상태면 타이머만 감소하고 대기
            if (dynamiteStopped)
            {
                return;
            }

            // 동적으로 계산된 중력 적용
            dynamiteVerticalVelocity -= dynamiteGravity * Time.fixedDeltaTime;

            // 수평 이동 (동적으로 계산된 수평 속도 사용)
            Vector3 horizontalMove = new Vector3(fixedDirection.x, 0f, fixedDirection.z).normalized * dynamiteHorizontalSpeed * Time.fixedDeltaTime;

            // 수직 이동
            float verticalMove = dynamiteVerticalVelocity * Time.fixedDeltaTime;

            // 위치 업데이트
            Vector3 newPos = transform.position + horizontalMove;
            newPos.y += verticalMove;

            // 바닥 체크 (Y가 일정 이하면 튕김)
            float groundY = 0.5f;
            if (newPos.y <= groundY && dynamiteVerticalVelocity < 0)
            {
                // 튕김!
                dynamiteBounceCount++;
                newPos.y = groundY;

                // 튕길 때마다 속도 감소 (수직 60%, 수평 70%)
                dynamiteVerticalVelocity = Mathf.Abs(dynamiteVerticalVelocity) * 0.6f;
                dynamiteHorizontalSpeed *= 0.7f;

                Debug.Log($"[Projectile] Dynamite bounce {dynamiteBounceCount}/{DYNAMITE_MAX_BOUNCES} at {newPos}, hSpeed={dynamiteHorizontalSpeed:F1}, fuseRemaining={dynamiteFuseTime:F1}s");

                // 3번째 튕김이면 멈추고 퓨즈 타이머 대기
                if (dynamiteBounceCount >= DYNAMITE_MAX_BOUNCES)
                {
                    transform.position = newPos;
                    dynamiteStopped = true;
                    if (rb != null) rb.linearVelocity = Vector3.zero;
                    Debug.Log($"[Projectile] Dynamite stopped, waiting for fuse: {dynamiteFuseTime:F1}s remaining");
                    return;
                }
            }

            transform.position = newPos;

            // Rigidbody 속도도 업데이트 (물리 충돌용)
            if (rb != null)
            {
                rb.linearVelocity = horizontalMove / Time.fixedDeltaTime + Vector3.up * dynamiteVerticalVelocity;
            }

            // 회전 업데이트 (이동 방향으로)
            Vector3 moveDir = horizontalMove + Vector3.up * verticalMove;
            if (moveDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(moveDir.normalized);
            }
        }

        //LMJ : Dynamite explosion - AOE damage at current position
        private void ExplodeDynamite()
        {
            if (dynamiteExploded) return;
            dynamiteExploded = true;

            // 속도 멈춤
            if (rb != null) rb.linearVelocity = Vector3.zero;

            Vector3 explosionPos = transform.position;
            Debug.Log($"[Projectile] Dynamite exploding at {explosionPos}, radius={dynamiteAoeRadius}");

            // 폭발 이펙트 재생 (적절한 크기로)
            GameObject hitEffectPrefab = skillPrefabs?.hitEffectPrefab;
            if (hitEffectPrefab != null)
            {
                GameObject explosionEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, explosionPos, Quaternion.identity);
                // 폭발 이펙트는 기본 크기 사용 (너무 크거나 작지 않게)
                explosionEffect.transform.localScale = Vector3.one * 1.5f;
                UnityEngine.Object.Destroy(explosionEffect, 2f);
            }

            // AOE 범위 내 모든 적에게 데미지
            Collider[] hitColliders = Physics.OverlapSphere(explosionPos, dynamiteAoeRadius);
            int hitCount = 0;

            for (int i = 0; i < hitColliders.Length; i++)
            {
                Collider col = hitColliders[i];

                if (col.CompareTag(Tag.Monster))
                {
                    Monster monster = col.GetComponent<Monster>();
                    if (monster != null)
                    {
                        var (damageToApply, isCrit) = CalculateDamageToApply();
                        // 상성 배율 적용
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, monster.GetGenre());
                        float finalDamage = damageToApply * genreMultiplier;
                        monster.TakeDamage(finalDamage, isCrit);
                        hitCount++;
                        Debug.Log($"[Projectile] Dynamite hit {monster.name}: damage={finalDamage:F1} (x{genreMultiplier}){(isCrit ? " CRIT!" : "")}");
                    }
                }
                else if (col.CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = col.GetComponent<BossMonster>();
                    if (boss != null)
                    {
                        var (damageToApply, isCrit) = CalculateDamageToApply();
                        // 상성 배율 적용
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, boss.GetGenre());
                        float finalDamage = damageToApply * genreMultiplier;
                        boss.TakeDamage(finalDamage, isCrit);
                        hitCount++;
                        Debug.Log($"[Projectile] Dynamite hit {boss.name}: damage={finalDamage:F1} (x{genreMultiplier}){(isCrit ? " CRIT!" : "")}");
                    }
                }
            }

            Debug.Log($"[Projectile] Dynamite explosion complete: hitCount={hitCount}");

            // 폭발 후 풀로 반환
            ReturnToPool();
        }

        //LMJ : Legendary Staff movement - straight line with AOE damage along path
        private void UpdateLegendaryStaffMovement()
        {
            // 이동
            float moveDistance = speed * Time.fixedDeltaTime;
            legendaryStaffTraveledDistance += moveDistance;

            Vector3 movement = fixedDirection * moveDistance;
            transform.position += movement;

            // Rigidbody 동기화
            if (rb != null)
            {
                rb.linearVelocity = fixedDirection * speed;
            }

            // 회전 업데이트
            if (fixedDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(fixedDirection);
            }

            // 틱 간격마다 AOE 데미지 체크
            legendaryStaffLastTickTime += Time.fixedDeltaTime;
            if (legendaryStaffLastTickTime >= legendaryStaffTickInterval)
            {
                legendaryStaffLastTickTime = 0f;
                ApplyLegendaryStaffAOEDamage();
            }

            // 최대 사거리 도달 시 소멸
            if (legendaryStaffTraveledDistance >= legendaryStaffMaxRange)
            {
                Debug.Log($"[Projectile] LegendaryStaff reached max range: {legendaryStaffMaxRange}, totalHits={legendaryStaffHitTargets.Count}");
                ReturnToPool();
            }
        }

        //LMJ : Apply AOE damage at current position for Legendary Staff
        private void ApplyLegendaryStaffAOEDamage()
        {
            Vector3 currentPos = transform.position;

            // 현재 위치 주변의 적 탐지
            Collider[] hitColliders = Physics.OverlapSphere(currentPos, legendaryStaffAoeRadius);

            foreach (Collider col in hitColliders)
            {
                if (col.CompareTag(Tag.Monster))
                {
                    Monster monster = col.GetComponent<Monster>();
                    if (monster != null)
                    {
                        int instanceId = monster.GetInstanceID();
                        // 이미 맞은 적은 스킵 (한 번만 데미지)
                        if (legendaryStaffHitTargets.Contains(instanceId)) continue;

                        legendaryStaffHitTargets.Add(instanceId);
                        var (damageToApply, isCrit) = CalculateDamageToApply();
                        // 상성 배율 적용
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, monster.GetGenre());
                        float finalDamage = damageToApply * genreMultiplier;
                        monster.TakeDamage(finalDamage, isCrit);

                        // 히트 이펙트 재생 (콜라이더 중심점)
                        SpawnHitEffectAtCollider(col);

                        Debug.Log($"[Projectile] LegendaryStaff hit {monster.name}: damage={finalDamage:F1} (x{genreMultiplier}){(isCrit ? " CRIT!" : "")}");
                    }
                }
                else if (col.CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = col.GetComponent<BossMonster>();
                    if (boss != null)
                    {
                        int instanceId = boss.GetInstanceID();
                        // 이미 맞은 적은 스킵 (한 번만 데미지)
                        if (legendaryStaffHitTargets.Contains(instanceId)) continue;

                        legendaryStaffHitTargets.Add(instanceId);
                        var (damageToApply, isCrit) = CalculateDamageToApply();
                        // 상성 배율 적용
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, boss.GetGenre());
                        float finalDamage = damageToApply * genreMultiplier;
                        boss.TakeDamage(finalDamage, isCrit);

                        // 히트 이펙트 재생 (콜라이더 중심점)
                        SpawnHitEffectAtCollider(col);

                        Debug.Log($"[Projectile] LegendaryStaff hit {boss.name}: damage={finalDamage:F1} (x{genreMultiplier}){(isCrit ? " CRIT!" : "")}");
                    }
                }
            }
        }

        //LMJ : Time Bomb movement - fly to target, attach to monster, then explode after fuse time
        private void UpdateTimeBombMovement()
        {
            if (timeBombExploded) return;

            // 이미 몬스터에 부착된 상태
            if (timeBombAttached)
            {
                // 퓨즈 타이머 감소
                timeBombFuseTime -= Time.fixedDeltaTime;

                // 부착된 타겟이 죽었거나 사라진 경우 - 현재 위치에서 폭발
                if (timeBombAttachTarget == null)
                {
                    Debug.Log($"[Projectile] TimeBomb target destroyed, exploding at current position");
                    ExplodeTimeBomb();
                    return;
                }

                // 몬스터를 따라다님 (부착 상태 유지)
                transform.position = timeBombAttachTarget.position + Vector3.up * 1.5f;

                // 이펙트도 몬스터를 따라다님
                if (timeBombEffectInstance != null)
                {
                    timeBombEffectInstance.transform.position = timeBombAttachTarget.position + Vector3.up * 1.5f;
                }

                // 퓨즈 타이머 완료 시 폭발
                if (timeBombFuseTime <= 0f)
                {
                    ExplodeTimeBomb();
                }
                return;
            }

            // 아직 부착되지 않은 상태 - 타겟을 향해 이동
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
        }

        //LMJ : Time Bomb attach to monster
        private void AttachTimeBombToMonster(Transform monsterTransform)
        {
            if (timeBombAttached || timeBombExploded) return;

            timeBombAttached = true;
            timeBombAttachTarget = monsterTransform;

            // 부착 후에는 자체 타이머로 관리하므로 lifetime 추적 취소
            lifetimeCts?.Cancel();

            // 투사체 물리 멈춤
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // 충돌 비활성화
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // 투사체 본체는 숨기고 이펙트만 몬스터에 부착
            // 기존 자식 이펙트들 비활성화
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }

            // 몬스터에 부착할 이펙트 생성
            GameObject projectileEffectPrefab = skillPrefabs?.projectilePrefab;
            if (projectileEffectPrefab != null)
            {
                Vector3 attachPos = monsterTransform.position + Vector3.up * 1.5f;
                timeBombEffectInstance = Object.Instantiate(projectileEffectPrefab, attachPos, Quaternion.identity);
                // 이펙트는 폭발 시 제거됨
            }

            Debug.Log($"[Projectile] TimeBomb attached to {monsterTransform.name}, fuseTime={timeBombFuseTime:F1}s");
        }

        //LMJ : Time Bomb explosion - damage to attached monster
        private void ExplodeTimeBomb()
        {
            if (timeBombExploded) return;
            timeBombExploded = true;

            // 폭발 위치는 부착된 타겟 위치 사용 (타겟이 없으면 현재 위치)
            Vector3 explosionPos = timeBombAttachTarget != null
                ? timeBombAttachTarget.position
                : transform.position;

            Debug.Log($"[Projectile] TimeBomb exploding at {explosionPos}, skillPrefabs={(skillPrefabs != null ? "OK" : "NULL")}");

            // 폭발 이펙트 재생 (hitEffect)
            GameObject hitEffectPrefab = skillPrefabs?.hitEffectPrefab;
            Debug.Log($"[Projectile] TimeBomb hitEffectPrefab={(hitEffectPrefab != null ? hitEffectPrefab.name : "NULL")}");

            if (hitEffectPrefab != null)
            {
                GameObject explosionEffect = Object.Instantiate(hitEffectPrefab, explosionPos, Quaternion.identity);
                Object.Destroy(explosionEffect, 2f);
                Debug.Log($"[Projectile] TimeBomb explosion effect spawned at {explosionPos}");
            }
            else
            {
                Debug.LogWarning($"[Projectile] TimeBomb hitEffectPrefab is null! skillId={skillId}");
            }

            // 부착된 이펙트 제거
            if (timeBombEffectInstance != null)
            {
                Object.Destroy(timeBombEffectInstance);
                timeBombEffectInstance = null;
            }

            // 부착된 타겟에게 데미지
            if (timeBombAttachTarget != null)
            {
                var (damageToApply, isCrit) = CalculateDamageToApply();

                if (timeBombAttachTarget.CompareTag(Tag.Monster))
                {
                    Monster monster = timeBombAttachTarget.GetComponent<Monster>();
                    if (monster != null)
                    {
                        // 상성 배율 적용
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, monster.GetGenre());
                        float finalDamage = damageToApply * genreMultiplier;
                        monster.TakeDamage(finalDamage, isCrit);
                        Debug.Log($"[Projectile] TimeBomb hit {monster.name}: damage={finalDamage:F1} (x{genreMultiplier}){(isCrit ? " CRIT!" : "")}");
                    }
                }
                else if (timeBombAttachTarget.CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = timeBombAttachTarget.GetComponent<BossMonster>();
                    if (boss != null)
                    {
                        // 상성 배율 적용
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, boss.GetGenre());
                        float finalDamage = damageToApply * genreMultiplier;
                        boss.TakeDamage(finalDamage, isCrit);
                        Debug.Log($"[Projectile] TimeBomb hit {boss.name}: damage={finalDamage:F1} (x{genreMultiplier}){(isCrit ? " CRIT!" : "")}");
                    }
                }
            }

            // 폭발 후 풀로 반환
            ReturnToPool();
        }

        //LMJ : Spawn hit effect at collider center (몬스터 몸통 중심에 이펙트 생성)
        private void SpawnHitEffectAtCollider(Collider col)
        {
            if (col == null) return;
            GameObject hitEffectPrefab = skillPrefabs?.hitEffectPrefab;
            if (hitEffectPrefab != null)
            {
                Vector3 hitPos = col.bounds.center;
                GameObject hitEffect = Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                Object.Destroy(hitEffect, 2f);
            }
        }

        //LMJ : Effect-based movement with lerp (Effect mode only)
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
            catch (System.OperationCanceledException)
            {
                // Expected
            }
        }

        //LMJ : Handle reaching target in Effect mode
        private void OnReachTarget()
        {
            isInitialized = false;
            onHitCallback?.Invoke(targetPosition);
            Destroy(gameObject);
        }

        //LMJ : Track lifetime and auto-despawn
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
            catch (System.OperationCanceledException)
            {
                // Expected when projectile hits target before lifetime ends
            }
        }

        //LMJ : Handle collision with monsters and obstacles (both Physics and Effect modes)
        private void OnTriggerEnter(Collider other)
        {
            // JML: OnTriggerEnter 로그 제거

            if (!isInitialized) return;

            // Wall collision - ignore (projectiles should pass through walls)
            if (other.CompareTag(Tag.Wall))
            {
                return; // 벽 무시 - 투사체는 벽을 통과
            }

            // Obstacle collision
            if (other.CompareTag(Tag.Obstacle))
            {
                if (mode == ProjectileMode.Physics)
                {
                    ReturnToPool();
                }
                else if (mode == ProjectileMode.Effect)
                {
                    lifetimeCts?.Cancel();
                    Destroy(gameObject);
                }
                return;
            }

            // Ground collision
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                // 다이너마이트: 바닥에 닿으면 멈추고 퓨즈 타이머 대기
                if (isDynamite)
                {
                    if (rb != null) rb.linearVelocity = Vector3.zero;
                    return;
                }

                // 전설의 지팡이: Ground 무시 (일직선 이동)
                if (isLegendaryStaff)
                {
                    return;
                }

                if (mode == ProjectileMode.Physics)
                {
                    ReturnToPool();
                }
                else if (mode == ProjectileMode.Effect)
                {
                    lifetimeCts?.Cancel();
                    Destroy(gameObject);
                }
                return;
            }

            if (other.CompareTag(Tag.Monster))
            {
                Monster monster = other.GetComponent<Monster>();

                // Monster 컴포넌트가 없는 경우 (DummyTarget 등) ITargetable로 처리
                if (monster == null)
                {
                    ITargetable targetable = other.GetComponent<ITargetable>();
                    if (targetable != null)
                    {
                        // 다이너마이트/전설의 지팡이/시한폭탄은 별도 처리
                        if (isDynamite || isLegendaryStaff || (isTimeBomb && !timeBombAttached))
                        {
                            return;
                        }

                        // Apply damage
                        var (damageToApply, isCrit) = CalculateDamageToApply();
                        targetable.TakeDamage(damageToApply);

                        // Spawn hit effect
                        SpawnHitEffectAtCollider(other);

                        // 부메랑이 아니면 풀로 반환
                        if (!isBoomerang)
                        {
                            if (mode == ProjectileMode.Physics)
                            {
                                ReturnToPool();
                            }
                            else if (mode == ProjectileMode.Effect)
                            {
                                lifetimeCts?.Cancel();
                                Destroy(gameObject);
                            }
                        }
                    }
                    return;
                }

                // 이하 기존 Monster 처리 코드
                {
                    // 다이너마이트: 적과 충돌해도 무시 (퓨즈 타이머가 끝나야 폭발)
                    if (isDynamite)
                    {
                        return;
                    }

                    // 전설의 지팡이: 충돌로 데미지 주지 않음 (AOE 틱으로 처리)
                    if (isLegendaryStaff)
                    {
                        return;
                    }

                    // 시한폭탄(의문의 예고장): 몬스터에 부착 후 퓨즈 타이머 대기
                    if (isTimeBomb && !timeBombAttached)
                    {
                        AttachTimeBombToMonster(other.transform);
                        return;
                    }

                    // 부메랑: 같은 적을 최대 2번만 타격 가능 (가는 길 1번, 오는 길 1번)
                    if (isBoomerang)
                    {
                        int instanceId = monster.GetInstanceID();
                        if (boomerangHitCounts == null)
                            boomerangHitCounts = new System.Collections.Generic.Dictionary<int, int>();

                        if (!boomerangHitCounts.TryGetValue(instanceId, out int hitCount))
                            hitCount = 0;

                        if (hitCount >= 2)
                        {
                            // 이미 2번 맞은 적은 무시하고 통과
                            return;
                        }

                        boomerangHitCounts[instanceId] = hitCount + 1;
                        Debug.Log($"[Projectile] Boomerang hit {monster.name}: hitCount={hitCount + 1}/2, returning={isReturning}");
                    }

                    // Apply status effects BEFORE damage
                    if (supportSkillData != null && supportSkillData.GetStatusEffectType() != StatusEffectType.Chain)
                    {
                        ApplyStatusEffect(monster);
                    }

                    // Apply damage (새 데미지 공식 + 치명타 적용)
                    var (damageToApply, isCrit) = CalculateDamageToApply();

                    // Issue #362 - 저체력 보너스 데미지 적용 (처형 서포트 등)
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
                    float finalDamage = damageToApply * genreMultiplier;
                    monster.TakeDamage(finalDamage, isCrit);

                    // 상성 로그 (1.0이 아닐 때만)
                    if (genreMultiplier != 1.0f)
                    {
                        Debug.Log($"[Projectile] 상성! {attackerGenre} vs {monster.GetGenre()} = x{genreMultiplier} (데미지: {damageToApply:F0} → {finalDamage:F0})");
                    }

                    // Spawn hit effect at monster collider center (몸통 중심)
                    SpawnHitEffectAtCollider(other);

                    // Add to hit targets for chain tracking
                    if (maxChainCount > 0)
                    {
                        chainHitTargets.Add(monster);
                    }

                    // Process Chain (체이닝: DamageCalculator 사용)
                    if (supportSkillData != null && supportSkillData.GetStatusEffectType() == StatusEffectType.Chain && currentChainCount < maxChainCount)
                    {
                        // Chain effect spawning removed - no longer using supportPrefabs

                        // Find next target
                        ITargetable nextTarget = FindNextChainTarget(other.transform.position, chainHitTargets, monster);

                        if (nextTarget != null)
                        {
                            // DamageCalculator로 체이닝 데미지 계산 (n번째 타격)
                            currentChainCount++;
                            float reductionRate = supportSkillData.chain_damage_reduction / 100f;
                            currentChainDamage = DamageCalculator.CalculatePierceChainDamage(baseDamageForPierce > 0 ? baseDamageForPierce : damage, reductionRate, currentChainCount);

                            // JML: Chain 로그 압축
                            Debug.Log($"[Proj] Chain {currentChainCount}/{maxChainCount}");

                            Vector3 directionToNext = (nextTarget.GetPosition() - other.transform.position).normalized;
                            float spawnOffset = 1.0f;
                            Vector3 spawnPos = other.transform.position + directionToNext * spawnOffset;

                            Launch(spawnPos, nextTarget.GetPosition(), speed, lifetime, currentChainDamage, skillId, supportSkillId);
                            return;
                        }
                    }

                    // Process Fragmentation (파편화 40002: 명중 시 분열)
                    // 관통과 동시에 작동 - 관통 체크 전에 파편화 처리
                    Debug.Log($"[Projectile] Hit - Fragmentation check: supportSkillId={supportSkillId}, supportSkillData={(supportSkillData != null ? "OK" : "NULL")}, add_projectiles={(supportSkillData?.add_projectiles ?? -1)}");
                    if (supportSkillId == 40002 && supportSkillData != null && supportSkillData.add_projectiles > 0)
                    {
                        int totalFragments = 1 + supportSkillData.add_projectiles; // 원본 포함 총 5발
                        Debug.Log($"[Projectile] Fragmentation triggered! Spawning {totalFragments} fragments at {other.transform.position}");
                        SpawnFragmentProjectilesFan(other.transform.position, totalFragments, fixedDirection, other);
                    }

                    // Process Pierce (관통: DamageCalculator 사용)
                    // 파편화 후에도 관통 가능
                    if (maxPierceCount > 0 && currentPierceCount < maxPierceCount)
                    {
                        currentPierceCount++;
                        // JML: Pierce 로그 제거
                        return; // 관통하여 계속 진행 (풀로 돌아가지 않음)
                    }

                    // 부메랑은 맞아도 계속 진행 (풀로 돌아가지 않음)
                    if (isBoomerang)
                    {
                        return;
                    }

                    // Process Fragmentation (파편화 40002: 명중 시 분열)
                    if (supportSkillId == 40002 && supportSkillData != null && supportSkillData.add_projectiles > 0)
                    {
                        int totalFragments = 1 + supportSkillData.add_projectiles;
                        SpawnFragmentProjectilesFan(other.transform.position, totalFragments, fixedDirection, other);
                    }

                    // Cleanup
                    if (mode == ProjectileMode.Physics)
                    {
                        ReturnToPool();
                    }
                    else if (mode == ProjectileMode.Effect)
                    {
                        lifetimeCts?.Cancel();
                        onHitCallback?.Invoke(other.transform.position);
                        Destroy(gameObject);
                    }
                }
            }
            else if (other.CompareTag(Tag.BossMonster))
            {
                BossMonster boss = other.GetComponent<BossMonster>();
                if (boss != null)
                {
                    // 다이너마이트: 적과 충돌해도 무시 (퓨즈 타이머가 끝나야 폭발)
                    if (isDynamite)
                    {
                        return;
                    }

                    // 전설의 지팡이: 충돌로 데미지 주지 않음 (AOE 틱으로 처리)
                    if (isLegendaryStaff)
                    {
                        return;
                    }

                    // 시한폭탄(의문의 예고장): 보스 몬스터에 부착 후 퓨즈 타이머 대기
                    if (isTimeBomb && !timeBombAttached)
                    {
                        AttachTimeBombToMonster(other.transform);
                        return;
                    }

                    // 부메랑: 같은 적을 최대 2번만 타격 가능 (가는 길 1번, 오는 길 1번)
                    if (isBoomerang)
                    {
                        int instanceId = boss.GetInstanceID();
                        if (boomerangHitCounts == null)
                            boomerangHitCounts = new System.Collections.Generic.Dictionary<int, int>();

                        if (!boomerangHitCounts.TryGetValue(instanceId, out int hitCount))
                            hitCount = 0;

                        if (hitCount >= 2)
                        {
                            // 이미 2번 맞은 적은 무시하고 통과
                            return;
                        }

                        boomerangHitCounts[instanceId] = hitCount + 1;
                        Debug.Log($"[Projectile] Boomerang hit {boss.name}: hitCount={hitCount + 1}/2, returning={isReturning}");
                    }

                    // Apply status effects BEFORE damage
                    if (supportSkillData != null && supportSkillData.GetStatusEffectType() != StatusEffectType.Chain)
                    {
                        ApplyStatusEffectToBoss(boss);
                    }

                    // Apply damage (새 데미지 공식 + 치명타 적용)
                    var (damageToApply, isCrit) = CalculateDamageToApply();

                    // Issue #362 - 저체력 보너스 데미지 적용 (처형 서포트 등)
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
                    float finalDamage = damageToApply * genreMultiplier;
                    boss.TakeDamage(finalDamage, isCrit);

                    // 상성 로그 (1.0이 아닐 때만)
                    if (genreMultiplier != 1.0f)
                    {
                        Debug.Log($"[Projectile] 상성! {attackerGenre} vs {boss.GetGenre()} = x{genreMultiplier} (데미지: {damageToApply:F0} → {finalDamage:F0})");
                    }

                    // Spawn hit effect at boss collider center (몸통 중심)
                    SpawnHitEffectAtCollider(other);

                    // Add to hit targets for chain tracking
                    if (maxChainCount > 0)
                    {
                        chainHitTargets.Add(boss);
                    }

                    // Process Chain (체이닝: DamageCalculator 사용)
                    if (supportSkillData != null && supportSkillData.GetStatusEffectType() == StatusEffectType.Chain && currentChainCount < maxChainCount)
                    {
                        // Chain effect spawning removed - no longer using supportPrefabs

                        ITargetable nextTarget = FindNextChainTarget(other.transform.position, chainHitTargets, boss);

                        if (nextTarget != null)
                        {
                            // DamageCalculator로 체이닝 데미지 계산 (n번째 타격)
                            currentChainCount++;
                            float reductionRate = supportSkillData.chain_damage_reduction / 100f;
                            currentChainDamage = DamageCalculator.CalculatePierceChainDamage(baseDamageForPierce > 0 ? baseDamageForPierce : damage, reductionRate, currentChainCount);

                            // JML: Chain 로그 압축
                            Debug.Log($"[Proj] Chain {currentChainCount}/{maxChainCount}");

                            Vector3 directionToNext = (nextTarget.GetPosition() - other.transform.position).normalized;
                            float spawnOffset = 1.0f;
                            Vector3 spawnPos = other.transform.position + directionToNext * spawnOffset;

                            Launch(spawnPos, nextTarget.GetPosition(), speed, lifetime, currentChainDamage, skillId, supportSkillId);
                            return;
                        }
                    }

                    // Process Fragmentation (파편화 40002: 명중 시 분열)
                    // 관통과 동시에 작동 - 관통 체크 전에 파편화 처리
                    Debug.Log($"[Projectile] Hit - Fragmentation check: supportSkillId={supportSkillId}, supportSkillData={(supportSkillData != null ? "OK" : "NULL")}, add_projectiles={(supportSkillData?.add_projectiles ?? -1)}");
                    if (supportSkillId == 40002 && supportSkillData != null && supportSkillData.add_projectiles > 0)
                    {
                        int totalFragments = 1 + supportSkillData.add_projectiles; // 원본 포함 총 5발
                        Debug.Log($"[Projectile] Fragmentation triggered! Spawning {totalFragments} fragments at {other.transform.position}");
                        SpawnFragmentProjectilesFan(other.transform.position, totalFragments, fixedDirection, other);
                    }

                    // Process Pierce (관통: DamageCalculator 사용)
                    // 파편화 후에도 관통 가능
                    if (maxPierceCount > 0 && currentPierceCount < maxPierceCount)
                    {
                        currentPierceCount++;
                        // JML: Pierce 로그 제거
                        return; // 관통하여 계속 진행 (풀로 돌아가지 않음)
                    }

                    // 부메랑은 맞아도 계속 진행 (풀로 돌아가지 않음)
                    if (isBoomerang)
                    {
                        return;
                    }

                    // Process Fragmentation (파편화 40002: 명중 시 분열)
                    if (supportSkillId == 40002 && supportSkillData != null && supportSkillData.add_projectiles > 0)
                    {
                        int totalFragments = 1 + supportSkillData.add_projectiles;
                        SpawnFragmentProjectilesFan(other.transform.position, totalFragments, fixedDirection, other);
                    }

                    // Cleanup
                    if (mode == ProjectileMode.Physics)
                    {
                        ReturnToPool();
                    }
                    else if (mode == ProjectileMode.Effect)
                    {
                        lifetimeCts?.Cancel();
                        onHitCallback?.Invoke(other.transform.position);
                        Destroy(gameObject);
                    }
                }
            }
        }

        //LMJ : Calculate damage to apply (새 데미지 공식 + 치명타 시스템)
        // 관통/체이닝 감소 공식: n번째 타격 데미지 = (단일 타격 데미지) × (1 - 감소율)^n
        // 치명타 공식: 데미지 × (치명타 배율 / 100)
        private (float damage, bool isCritical) CalculateDamageToApply()
        {
            float baseDamage;

            // 1. 체이닝 활성화된 경우
            if (maxChainCount > 0)
            {
                baseDamage = currentChainDamage;
            }
            // 2. 관통 활성화된 경우 - DamageCalculator 사용
            else if (maxPierceCount > 0 && currentPierceCount > 0)
            {
                // 관통 감소율: 서포트 스킬의 chain_damage_reduction 또는 기본값 30%
                float reductionRate = supportSkillData?.chain_damage_reduction / 100f ?? 0.3f;
                baseDamage = DamageCalculator.CalculatePierceChainDamage(baseDamageForPierce, reductionRate, currentPierceCount);
            }
            // 3. 기본 데미지
            else
            {
                baseDamage = damage;
            }

            // 4. 치명타 판정 및 적용
            return DamageCalculator.CalculateCriticalDamage(baseDamage, critChance, critMultiplier);
        }

        //LMJ : Apply status effect to monster (MainSkillData + SupportSkillData)
        private void ApplyStatusEffect(Monster monster)
        {
            if (monster == null) return;

            // Get effect prefabs from skill data (supportPrefabs removed - using skillPrefabs only)
            GameObject ccEffectPrefab = skillPrefabs?.hitEffectPrefab;
            GameObject dotEffectPrefab = skillPrefabs?.hitEffectPrefab;
            GameObject markEffectPrefab = skillPrefabs?.hitEffectPrefab;

            // 1. MainSkillData 자체 효과 적용 (스킬 테이블에 정의된 CC/DOT/표식)
            if (skillData != null)
            {
                // CC 효과 (stun_use=true 또는 cc_duration > 0)
                if (skillData.HasCCEffect)
                {
                    monster.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, ccEffectPrefab);
                }

                // DOT 효과 (dot_duration > 0)
                if (skillData.HasDOTEffect)
                {
                    monster.ApplyDOT(DOTType.Burn, skillData.dot_damage_per_tick, skillData.dot_tick_interval, skillData.dot_duration, dotEffectPrefab);
                }

                // 표식 효과 (mark_duration > 0)
                if (skillData.HasMarkEffect)
                {
                    monster.ApplyMark(skillData.GetElementBasedMarkType(), skillData.mark_duration, skillData.mark_damage_mult / 100f, markEffectPrefab);
                }
            }

            // 2. SupportSkillData 추가 효과 적용
            if (supportSkillData != null)
            {
                switch (supportSkillData.GetStatusEffectType())
                {
                    case StatusEffectType.CC:
                        monster.ApplyCC(supportSkillData.GetCCType(), supportSkillData.cc_duration, supportSkillData.cc_slow_amount, ccEffectPrefab);
                        break;

                    case StatusEffectType.DOT:
                        monster.ApplyDOT(supportSkillData.GetDOTType(), supportSkillData.dot_damage_per_tick, supportSkillData.dot_tick_interval, supportSkillData.dot_duration, dotEffectPrefab);
                        break;

                    case StatusEffectType.Mark:
                        monster.ApplyMark(supportSkillData.GetMarkType(), supportSkillData.mark_duration, supportSkillData.mark_damage_mult, markEffectPrefab);
                        break;

                    case StatusEffectType.Chain:
                        // Chain is handled separately
                        break;
                }
            }
        }

        //LMJ : Apply status effect to boss monster (MainSkillData + SupportSkillData)
        private void ApplyStatusEffectToBoss(BossMonster boss)
        {
            if (boss == null) return;

            // Get effect prefabs from skill data (supportPrefabs removed - using skillPrefabs only)
            GameObject ccEffectPrefab = skillPrefabs?.hitEffectPrefab;
            GameObject dotEffectPrefab = skillPrefabs?.hitEffectPrefab;
            GameObject markEffectPrefab = skillPrefabs?.hitEffectPrefab;

            // 1. MainSkillData 자체 효과 적용 (스킬 테이블에 정의된 CC/DOT/표식)
            if (skillData != null)
            {
                // CC 효과 (stun_use=true 또는 cc_duration > 0)
                if (skillData.HasCCEffect)
                {
                    boss.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, ccEffectPrefab);
                }

                // DOT 효과 (dot_duration > 0)
                if (skillData.HasDOTEffect)
                {
                    boss.ApplyDOT(DOTType.Burn, skillData.dot_damage_per_tick, skillData.dot_tick_interval, skillData.dot_duration, dotEffectPrefab);
                }

                // 표식 효과 (mark_duration > 0)
                if (skillData.HasMarkEffect)
                {
                    boss.ApplyMark(skillData.GetElementBasedMarkType(), skillData.mark_duration, skillData.mark_damage_mult / 100f, markEffectPrefab);
                }
            }

            // 2. SupportSkillData 추가 효과 적용
            if (supportSkillData != null)
            {
                switch (supportSkillData.GetStatusEffectType())
                {
                    case StatusEffectType.CC:
                        boss.ApplyCC(supportSkillData.GetCCType(), supportSkillData.cc_duration, supportSkillData.cc_slow_amount, ccEffectPrefab);
                        break;

                    case StatusEffectType.DOT:
                        boss.ApplyDOT(supportSkillData.GetDOTType(), supportSkillData.dot_damage_per_tick, supportSkillData.dot_tick_interval, supportSkillData.dot_duration, dotEffectPrefab);
                        break;

                    case StatusEffectType.Mark:
                        boss.ApplyMark(supportSkillData.GetMarkType(), supportSkillData.mark_duration, supportSkillData.mark_damage_mult, markEffectPrefab);
                        break;

                    case StatusEffectType.Chain:
                        break;
                }
            }
        }

        // SpawnChainEffect removed - supportPrefabs no longer used

        //LMJ : Find next target for chain effect
        private ITargetable FindNextChainTarget(Vector3 currentPosition, System.Collections.Generic.HashSet<ITargetable> hitTargets, ITargetable excludeTarget = null)
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

        //LMJ : Spawn fragment projectiles in fan pattern on hit (파편화 40002)
        // 원본 진행 방향을 기준으로 부채꼴 형태로 발사
        // 몬스터 콜라이더 크기에 따라 동적으로 스폰 위치 계산
        private void SpawnFragmentProjectilesFan(Vector3 hitPosition, int totalCount, Vector3 originalDirection, Collider hitCollider)
        {
            if (totalCount <= 0) return;

            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance?.Pool;

            // 스테이지 클리어 등으로 풀이 정리된 경우 분열 스킵
            if (pool == null || !pool.HasPool<Projectile>())
            {
                return;
            }

            float spreadAngle = 30f; // 부채꼴 총 각도 (좌우 각각 15도씩)

            // 원본 발사체의 이펙트 프리팹 참조 저장
            GameObject effectPrefab = skillPrefabs?.projectilePrefab;

            // 원본 발사체의 시작 높이 사용 (바닥에서 생성되지 않도록)
            float projectileHeight = startPosition.y;

            // 몬스터 콜라이더 크기에 따라 동적으로 통과 오프셋 계산
            // 콜라이더 바운드의 magnitude + 여유 공간으로 몬스터 뒤쪽에서 생성
            float passOffset = 2.5f; // 기본값 (fallback)
            if (hitCollider != null)
            {
                passOffset = hitCollider.bounds.extents.magnitude + 1.0f;
            }
            float spreadOffset = 0.5f; // 부채꼴 방향으로의 추가 오프셋

            // JML: Fragment spawn 로그 제거

            for (int i = 0; i < totalCount; i++)
            {
                // 부채꼴 각도 계산: 중앙을 0도로 하여 좌우로 퍼짐
                // 예: 5발이면 -60도, -30도, 0도, +30도, +60도
                float angleOffset = 0f;
                if (totalCount > 1)
                {
                    angleOffset = spreadAngle * (i - (totalCount - 1) / 2f);
                }

                // Y축 회전을 적용하여 부채꼴 방향 계산
                Vector3 fragmentDirection = Quaternion.Euler(0, angleOffset, 0) * originalDirection;

                // 히트 위치의 XZ + 원본 발사체의 Y 높이 사용
                Vector3 adjustedHitPos = new Vector3(hitPosition.x, projectileHeight, hitPosition.z);

                // 몬스터 뒤쪽(원본 방향으로 통과한 지점)에서 생성
                Vector3 behindMonster = adjustedHitPos + originalDirection * passOffset;
                Vector3 spawnPos = behindMonster + fragmentDirection * spreadOffset;
                Vector3 targetPos = spawnPos + fragmentDirection * 50f;

                Projectile fragment = pool.Spawn<Projectile>(spawnPos);
                if (fragment == null) continue; // 풀이 비었거나 스폰 실패 시 스킵

                // 분열 발사체는 supportSkillId = 0으로 설정하여 재분열 방지, 원본의 crit 정보 유지
                fragment.Launch(spawnPos, targetPos, speed, lifetime, damage, skillId, 0, critChance, critMultiplier);

                // 파편에 원본 이펙트 복사
                if (effectPrefab != null)
                {
                    // 기존 자식 이펙트 제거
                    foreach (Transform child in fragment.transform)
                    {
                        if (child.gameObject != null)
                        {
                            Object.Destroy(child.gameObject);
                        }
                    }
                    // 새 이펙트 생성
                    GameObject fragmentEffect = Object.Instantiate(effectPrefab, fragment.transform);
                    fragmentEffect.transform.localPosition = Vector3.zero;
                    fragmentEffect.transform.localRotation = Quaternion.LookRotation(fragmentDirection);
                }

                // JML: Fragment 개별 로그 제거
            }

            Debug.Log($"[Proj] Frag {totalCount}발");
        }

        //LMJ : Return projectile to pool
        private void ReturnToPool()
        {
            lifetimeCts?.Cancel();
            var pool = GameManager.Instance?.Pool;
            if (pool != null)
            {
                pool.Despawn(this);
            }
            else
            {
                // Pool 없으면 Destroy (TrainingScene 등)
                Destroy(gameObject);
            }
        }

        // IPoolable implementation
        public void OnSpawn()
        {
            mode = ProjectileMode.Physics;
            isInitialized = false;
            fixedDirection = Vector3.zero;
            elapsedTime = 0f;
            onHitCallback = null;

            if (rb != null) rb.linearVelocity = Vector3.zero;

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

            // Clear skill references
            skillId = 0;
            skillData = null;
            skillPrefabs = null;
            supportSkillId = 0;
            supportSkillData = null;

            // Reset chain state
            currentChainCount = 0;
            maxChainCount = 0;
            chainHitTargets = null;
            currentChainDamage = 0f;

            // Reset pierce state
            currentPierceCount = 0;
            maxPierceCount = 0;
            baseDamageForPierce = 0f;

            // Reset boomerang state
            isBoomerang = false;
            isReturning = false;
            ownerPosition = Vector3.zero;
            boomerangMaxDistance = 0f;
            boomerangTraveledDistance = 0f;
            boomerangHitCounts = null;

            // Reset dynamite state
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

            // Reset legendary staff state
            isLegendaryStaff = false;
            legendaryStaffAoeRadius = 0f;
            legendaryStaffMaxRange = 0f;
            legendaryStaffTraveledDistance = 0f;
            legendaryStaffLastTickTime = 0f;
            legendaryStaffHitTargets = null;

            // Reset time bomb state
            isTimeBomb = false;
            timeBombFuseTime = 0f;
            timeBombExploded = false;
            timeBombAttached = false;
            timeBombAttachTarget = null;
            if (timeBombEffectInstance != null)
            {
                Object.Destroy(timeBombEffectInstance);
                timeBombEffectInstance = null;
            }

            // Reset genre state (상성 시스템)
            attackerGenre = Genre.Horror;

            // Rigidbody 상태 복원 (시한폭탄에서 isKinematic을 true로 변경했을 수 있음)
            // 주의: isKinematic을 먼저 false로 설정해야 linearVelocity 설정 가능
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }

            // Collider 활성화 복원 (시한폭탄에서 비활성화했을 수 있음)
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // 자식 오브젝트 활성화 복원 (시한폭탄에서 비활성화했을 수 있음)
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
            }
            lifetimeCts?.Cancel();
        }

        private void OnDestroy()
        {
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
        }

        #region Independent Effect Spawn

        // 현재 이펙트 인스턴스 추적 (선택적)
        private GameObject _currentEffectInstance;

        /// <summary>
        /// 독립 이펙트 스폰 (Projectile과 분리된 라이프사이클)
        /// 이펙트는 Projectile이 despawn되어도 재생 완료까지 유지됨
        /// </summary>
        private async UniTaskVoid SpawnIndependentEffectAsync(int skillId, Vector3 direction)
        {
            // SkillEffectManager 확인
            var effectManager = SkillEffectManager.Instance;
            if (effectManager == null)
            {
                // Fallback: 기존 방식으로 자식 이펙트 생성
                SpawnChildEffectFallback(direction);
                return;
            }

            try
            {
                // 독립 이펙트 스폰 (Projectile을 따라감)
                _currentEffectInstance = await effectManager.SpawnProjectileEffect(
                    skillId,
                    transform,
                    direction
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Projectile] Failed to spawn independent effect: {ex.Message}");
                // Fallback: 기존 방식으로 자식 이펙트 생성
                SpawnChildEffectFallback(direction);
            }
        }

        /// <summary>
        /// Fallback: 기존 방식으로 자식 이펙트 생성
        /// SkillEffectManager가 없거나 실패한 경우 사용
        /// </summary>
        private void SpawnChildEffectFallback(Vector3 direction)
        {
            if (skillPrefabs != null && skillPrefabs.projectilePrefab != null)
            {
                GameObject effectInstance = Object.Instantiate(skillPrefabs.projectilePrefab, transform);
                effectInstance.transform.localPosition = Vector3.zero;
                effectInstance.transform.localRotation = Quaternion.LookRotation(direction);
                Debug.Log($"[Projectile] Fallback: effect spawned as child");
            }
        }

        #endregion
    }
}
