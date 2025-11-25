//LMJ : Character with simple projectile-based combat (Issue #265)
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;

    public class Character : MonoBehaviour, IPoolable
    {
        [Header("Character Visual")]
        [SerializeField] private GameObject characterObj;

        [Header("스킬 장착 (Skill Equipment)")]
        [SerializeField, Tooltip("기본 공격 스킬 (Basic Attack Skill)")]
        private SkillAssetData basicAttackSkill;

        [SerializeField, Tooltip("액티브 스킬 (Active Skill - 자동 실행)")]
        private SkillAssetData activeSkill;

        [SerializeField, Tooltip("보조 스킬 (Support Skill - Active Skill에 영향)")]
        private SkillAssetData supportSkill;

        [Header("캐릭터 스텟 변형 (%) (Character Stat Modifiers)")]
        [SerializeField, Tooltip("데미지 변형 (%)")]
        private float damageModifier = 0f;

        [SerializeField, Tooltip("공격 속도 변형 (%)")]
        private float attackSpeedModifier = 0f;

        [SerializeField, Tooltip("투사체 속도 변형 (%)")]
        private float projectileSpeedModifier = 0f;

        [SerializeField, Tooltip("사거리 변형 (%)")]
        private float rangeModifier = 0f;

        [Header("Spawn Position")]
        [SerializeField, Tooltip("Projectile spawn offset")]
        private Vector3 spawnOffset = Vector3.zero;

        [Header("Projectile Template")]
        [SerializeField, Tooltip("Generic projectile template (used when skill has no projectile prefab)")]
        private GameObject projectileTemplate;

        [Header("Targeting Strategy")]
        [SerializeField, Tooltip("Use weight-based targeting (default: distance-based)")]
        private bool useWeightTargeting = false;

        // 최종 수치 계산 프로퍼티 (스킬 기본값 × 캐릭터 변형)
        private float FinalDamage => basicAttackSkill != null
            ? basicAttackSkill.baseDamage * (1f + damageModifier / 100f)
            : 0f;

        private float FinalAttackSpeed => basicAttackSkill != null
            ? (1f / basicAttackSkill.cooldown) * (1f + attackSpeedModifier / 100f)
            : 1f;

        private float FinalProjectileSpeed => basicAttackSkill != null
            ? basicAttackSkill.projectileSpeed * (1f + projectileSpeedModifier / 100f)
            : 10f;

        private float FinalRange => basicAttackSkill != null
            ? basicAttackSkill.range * (1f + rangeModifier / 100f)
            : 1000f;

        private float FinalProjectileLifetime => basicAttackSkill != null
            ? basicAttackSkill.projectileLifetime
            : 5f;

        // Active Skill 최종 수치 계산 프로퍼티 (캐릭터 변형 + Support 스킬 변형)
        private float FinalActiveDamage
        {
            get
            {
                if (activeSkill == null) return 0f;
                float damage = activeSkill.baseDamage * (1f + damageModifier / 100f);
                if (supportSkill != null) damage *= (1f + supportSkill.damageModifier / 100f);
                return damage;
            }
        }

        private float FinalActiveAttackSpeed
        {
            get
            {
                if (activeSkill == null) return 1f;
                float attackSpeed = (1f / activeSkill.cooldown) * (1f + attackSpeedModifier / 100f);
                if (supportSkill != null) attackSpeed *= (1f + supportSkill.attackSpeedModifier / 100f);
                return attackSpeed;
            }
        }

        private float FinalActiveProjectileSpeed
        {
            get
            {
                if (activeSkill == null) return 10f;
                float speed = activeSkill.projectileSpeed * (1f + projectileSpeedModifier / 100f);
                if (supportSkill != null) speed *= (1f + supportSkill.projectileSpeedMultiplier / 100f);
                return speed;
            }
        }

        private float FinalActiveRange => activeSkill != null
            ? activeSkill.range * (1f + rangeModifier / 100f)
            : 1000f;

        private float FinalActiveProjectileLifetime
        {
            get
            {
                if (activeSkill == null) return 5f;
                float lifetime = activeSkill.projectileLifetime;
                if (supportSkill != null) lifetime *= (1f + supportSkill.durationMultiplier / 100f);
                return lifetime;
            }
        }

        // Active Skill 발사체 개수 (Support 스킬이 있으면 총 개수로 대체)
        private int FinalActiveProjectileCount
        {
            get
            {
                if (activeSkill == null) return 1;

                // Support 스킬이 있고 additionalProjectiles > 0이면 총 개수로 사용
                if (supportSkill != null && supportSkill.additionalProjectiles > 0)
                {
                    return supportSkill.additionalProjectiles;
                }

                // Support 스킬이 없으면 Active Skill의 기본 개수 사용
                return activeSkill.projectileCount;
            }
        }

        // Attack state
        private CancellationTokenSource attackCts;
        private CancellationTokenSource activeSkillCts;
        private bool isInitialized = false;

        private void Start()
        {
            InitializeProjectilePool();
            InitializeActiveSkillPool();
            StartAttackLoop();
            StartActiveSkillLoop();
            isInitialized = true;
        }

        //LMJ : Initialize projectile pool (from basic attack skill)
        private void InitializeProjectilePool()
        {
            if (basicAttackSkill == null)
            {
                Debug.LogError("[Character] basicAttackSkill is null!");
                return;
            }

            // Effect만 있는 경우 ProjectileTemplate 사용
            if (basicAttackSkill.projectileEffectPrefab == null)
            {
                Debug.LogWarning($"[Character] {basicAttackSkill.skillName} has no effect prefab. Cannot fire skill.");
                return;
            }

            if (projectileTemplate == null)
            {
                Debug.LogError("[Character] ProjectileTemplate is not assigned! Please assign it in the Inspector.");
                return;
            }

            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;

            if (!pool.HasPool<Projectile>())
            {
                pool.CreatePool<Projectile>(projectileTemplate, defaultCapacity: 20, maxSize: 100);
                pool.WarmUp<Projectile>(20);
                Debug.Log($"[Character] Projectile pool initialized with ProjectileTemplate for skill: {basicAttackSkill.skillName}");
            }
        }

        //LMJ : Initialize active skill projectile pool
        private void InitializeActiveSkillPool()
        {
            if (activeSkill == null)
            {
                Debug.LogWarning("[Character] activeSkill is null. Skipping active skill initialization.");
                return;
            }

            // Effect만 있는 경우 ProjectileTemplate 사용
            if (activeSkill.projectileEffectPrefab == null)
            {
                Debug.LogWarning($"[Character] {activeSkill.skillName} has no effect prefab. Cannot fire skill.");
                return;
            }

            if (projectileTemplate == null)
            {
                Debug.LogError("[Character] ProjectileTemplate is not assigned! Please assign it in the Inspector.");
                return;
            }

            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;

            // ProjectileTemplate은 모든 스킬이 공유하므로 이미 생성되어 있을 수 있음
            if (!pool.HasPool<Projectile>())
            {
                pool.CreatePool<Projectile>(projectileTemplate, defaultCapacity: 10, maxSize: 50);
                pool.WarmUp<Projectile>(10);
                Debug.Log($"[Character] Active skill pool initialized with ProjectileTemplate for skill: {activeSkill.skillName}");
            }
        }

        //LMJ : Start attack loop
        private void StartAttackLoop()
        {
            attackCts?.Cancel();
            attackCts = new CancellationTokenSource();
            AttackLoopAsync(attackCts.Token).Forget();
        }

        //LMJ : Main attack loop with UniTask (using skill-based attack speed)
        private async UniTaskVoid AttackLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // Wait for attack interval (using final attack speed from skill + character modifier)
                float interval = 1f / FinalAttackSpeed;
                await UniTask.Delay((int)(interval * 1000), cancellationToken: ct);

                // Pause support (skip attack when Time.timeScale = 0)
                if (Time.timeScale == 0f) continue;

                TryAttack();
            }
        }

        //LMJ : Start active skill loop
        private void StartActiveSkillLoop()
        {
            if (activeSkill == null)
            {
                Debug.LogWarning("[Character] activeSkill is null. Skipping active skill loop.");
                return;
            }

            activeSkillCts?.Cancel();
            activeSkillCts = new CancellationTokenSource();
            ActiveSkillLoopAsync(activeSkillCts.Token).Forget();
        }

        //LMJ : Active skill loop with UniTask (independent cooldown)
        private async UniTaskVoid ActiveSkillLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // Wait for active skill interval (using final attack speed from active skill + character modifier)
                float interval = 1f / FinalActiveAttackSpeed;
                await UniTask.Delay((int)(interval * 1000), cancellationToken: ct);

                // Pause support (skip attack when Time.timeScale = 0)
                if (Time.timeScale == 0f) continue;

                TryUseActiveSkill();
            }
        }

        //LMJ : Attempt to attack nearest or highest weight target (skill-based)
        private void TryAttack()
        {
            if (!isInitialized || basicAttackSkill == null) return;

            // Find target based on strategy (using final range from skill + character modifier)
            ITargetable target = useWeightTargeting
                ? TargetRegistry.Instance.FindSkillTarget(transform.position, FinalRange)
                : TargetRegistry.Instance.FindTarget(transform.position, FinalRange);

            if (target == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Effect가 있으면 ProjectileTemplate 발사
            if (basicAttackSkill.projectileEffectPrefab != null)
            {
                var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;
                Projectile projectile = pool.Spawn<Projectile>(spawnPos);
                projectile.Launch(spawnPos, targetPos, FinalProjectileSpeed, FinalProjectileLifetime, FinalDamage, basicAttackSkill, null);

                Debug.Log($"[Character] Fired projectile {basicAttackSkill.skillName} at {target.GetTransform().name} (Damage: {FinalDamage:F1})");
            }
            // Effect도 없으면 즉발 공격
            else
            {
                // 피격 이펙트만 표시
                if (basicAttackSkill.hitEffectPrefab != null)
                {
                    GameObject hitEffect = Object.Instantiate(basicAttackSkill.hitEffectPrefab, targetPos, Quaternion.identity);
                    Object.Destroy(hitEffect, 2f);
                }

                // 즉시 데미지 적용
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    Monster monster = target.GetTransform().GetComponent<Monster>();
                    if (monster != null) monster.TakeDamage(FinalDamage);
                }
                else if (target.GetTransform().CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                    if (boss != null) boss.TakeDamage(FinalDamage);
                }

                Debug.Log($"[Character] Instant attack {basicAttackSkill.skillName} at {target.GetTransform().name} (Instant Damage: {FinalDamage:F1})");
            }
        }

        //LMJ : Attempt to use active skill on target
        private void TryUseActiveSkill()
        {
            if (!isInitialized || activeSkill == null) return;

            // Find target based on strategy (using final range from active skill + character modifier)
            ITargetable target = useWeightTargeting
                ? TargetRegistry.Instance.FindSkillTarget(transform.position, FinalActiveRange)
                : TargetRegistry.Instance.FindTarget(transform.position, FinalActiveRange);

            if (target == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Effect가 있으면 ProjectileTemplate으로 다중 발사체 부채꼴 발사
            if (activeSkill.projectileEffectPrefab != null)
            {
                var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;
                int projectileCount = FinalActiveProjectileCount;
                float spreadAngle = 15f; // 각 발사체 간 각도 (도 단위)

                for (int i = 0; i < projectileCount; i++)
                {
                    // 부채꼴 각도 계산: 중앙을 0도로 하여 좌우로 퍼짐
                    float angleOffset = (i - (projectileCount - 1) / 2f) * spreadAngle;

                    // 타겟 방향 벡터 계산
                    Vector3 direction = (targetPos - spawnPos).normalized;

                    // Y축 회전을 적용하여 부채꼴 방향 계산
                    Quaternion rotation = Quaternion.Euler(0, angleOffset, 0);
                    Vector3 spreadDirection = rotation * direction;

                    // 새로운 타겟 위치 계산 (멀리 떨어진 지점)
                    Vector3 spreadTargetPos = spawnPos + spreadDirection * 1000f;

                    // 발사체 생성 및 발사 (Support 스킬 데이터 전달)
                    Projectile projectile = pool.Spawn<Projectile>(spawnPos);
                    projectile.Launch(spawnPos, spreadTargetPos, FinalActiveProjectileSpeed, FinalActiveProjectileLifetime, FinalActiveDamage, activeSkill, supportSkill);

                    // Debug: Support 스킬 전달 확인
                    if (supportSkill != null)
                    {
                        Debug.Log($"[Character] Support Skill passed to projectile: {supportSkill.skillName}, StatusEffect: {supportSkill.statusEffectType}");
                    }
                }

                Debug.Log($"[Character] Used Active Skill (projectile x{projectileCount}): {activeSkill.skillName} at {target.GetTransform().name} (Damage: {FinalActiveDamage:F1})");
            }
            // Effect도 없으면 즉발 공격
            else
            {
                // 피격 이펙트만 표시
                if (activeSkill.hitEffectPrefab != null)
                {
                    GameObject hitEffect = Object.Instantiate(activeSkill.hitEffectPrefab, targetPos, Quaternion.identity);
                    Object.Destroy(hitEffect, 2f);
                }

                // 즉시 데미지 적용
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    Monster monster = target.GetTransform().GetComponent<Monster>();
                    if (monster != null) monster.TakeDamage(FinalActiveDamage);
                }
                else if (target.GetTransform().CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                    if (boss != null) boss.TakeDamage(FinalActiveDamage);
                }

                Debug.Log($"[Character] Active instant attack {activeSkill.skillName} at {target.GetTransform().name} (Instant Damage: {FinalActiveDamage:F1})");
            }
        }

        // IPoolable implementation
        public void OnSpawn()
        {
            characterObj.SetActive(true);

            if (!isInitialized)
            {
                Start(); // Initialize if not started yet
            }
            else
            {
                StartAttackLoop(); // Restart attack loop
                StartActiveSkillLoop(); // Restart active skill loop
            }

            Debug.Log("[Character] Character spawned and ready");
        }

        public void OnDespawn()
        {
            characterObj.SetActive(false);
            attackCts?.Cancel();
            activeSkillCts?.Cancel();
            Debug.Log("[Character] Character despawned");
        }

        private void OnDestroy()
        {
            attackCts?.Cancel();
            attackCts?.Dispose();
            activeSkillCts?.Cancel();
            activeSkillCts?.Dispose();
        }

        //LMJ : Set spawn offset from CharacterPreset (future feature)
        public void SetSpawnOffsetFromPreset(Vector3 offset)
        {
            spawnOffset = offset;
        }

        //LMJ : Set targeting strategy at runtime
        public void SetTargetingStrategy(bool useWeight)
        {
            useWeightTargeting = useWeight;
        }
    }
}
