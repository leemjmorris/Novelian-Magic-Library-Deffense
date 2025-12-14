//LMJ : Character partial class - Skill Data Loading and Calculations
namespace Novelian.Combat
{
    using UnityEngine;
    using NovelianMagicLibraryDefense.Managers;

    /// <summary>
    /// 캐릭터 스킬 데이터 관리
    /// - CSV에서 스킬 데이터 로딩
    /// - 최종 수치 계산 프로퍼티
    /// - 오브젝트 풀 초기화
    /// </summary>
    public partial class Character
    {
        #region Final Stat Calculations (Basic Attack)

        // 최종 수치 계산 프로퍼티 (새 데미지 공식 적용)
        // 공식: (기본 데미지) × (레벨 배율) × (보조 스킬 배율) × (캐릭터 변형)
        private float FinalDamage
        {
            get
            {
                if (basicAttackData == null) return 0f;
                // 레벨 데이터 조회 (없으면 배율 1)
                float levelMult = 1f;
                // 보조 스킬 배율
                float supportMult = supportData?.damage_mult ?? 1f;
                // DamageCalculator 사용
                float baseDamage = DamageCalculator.CalculateSingleDamage(basicAttackData.base_damage, levelMult, supportMult);
                // 캐릭터 변형 적용
                return baseDamage * (1f + damageModifier / 100f);
            }
        }

        private float FinalAttackSpeed
        {
            get
            {
                if (basicAttackData == null) return 1f;
                // 쿨다운에 서포트 배율 적용
                float cooldown = basicAttackData.cooldown;
                if (supportData != null) cooldown *= supportData.cooldown_mult;
                cooldown = Mathf.Max(cooldown, 0.1f); // 최소 쿨다운 보장
                // 공격 속도 계산
                float attackSpeed = (1f / cooldown) * (1f + attackSpeedModifier / 100f);
                if (supportData != null) attackSpeed *= supportData.attack_speed_mult;
                return Mathf.Max(attackSpeed, 0.1f); // 음수 방지
            }
        }

        private float FinalProjectileSpeed
        {
            get
            {
                if (basicAttackData == null) return 10f;
                // projectile_speed가 0이면 기본값 15 사용 (의문의 예고장 등)
                float baseSpeed = basicAttackData.projectile_speed > 0 ? basicAttackData.projectile_speed : 15f;
                float speed = baseSpeed * (1f + projectileSpeedModifier / 100f);
                if (supportData != null) speed *= supportData.speed_mult;
                return speed;
            }
        }

        private float FinalRange => basicAttackData != null
            ? basicAttackData.range * (1f + rangeModifier / 100f)
            : 1000f;

        private float FinalProjectileLifetime => basicAttackData != null
            ? (basicAttackData.skill_lifetime > 0 ? basicAttackData.skill_lifetime : 5f)
            : 5f;

        #endregion

        #region Final Stat Calculations (Active Skill)

        // Active Skill 최종 수치 계산 프로퍼티 (새 데미지 공식 적용)
        // 공식: (기본 데미지) × (레벨 배율) × (보조 스킬 배율) × (캐릭터 변형)
        private float FinalActiveDamage
        {
            get
            {
                if (activeSkillData == null) return 0f;
                // 레벨 배율 (현재 레벨 1 고정)
                float levelMult = 1f;
                // 보조 스킬 배율
                float supportMult = supportData?.damage_mult ?? 1f;
                // DamageCalculator로 단일 데미지 계산
                float baseDamage = DamageCalculator.CalculateSingleDamage(activeSkillData.base_damage, levelMult, supportMult);
                // 캐릭터 변형 적용
                return baseDamage * (1f + damageModifier / 100f);
            }
        }

        private float FinalActiveAttackSpeed
        {
            get
            {
                if (activeSkillData == null) return 1f;
                // 쿨다운에 서포트 배율 적용
                float cooldown = activeSkillData.cooldown;
                if (supportData != null) cooldown *= supportData.cooldown_mult;
                cooldown = Mathf.Max(cooldown, 0.1f); // 최소 쿨다운 보장
                // 공격 속도 계산
                float attackSpeed = (1f / cooldown) * (1f + attackSpeedModifier / 100f);
                if (supportData != null) attackSpeed *= supportData.attack_speed_mult;
                return Mathf.Max(attackSpeed, 0.1f); // 음수 방지
            }
        }

        private float FinalActiveProjectileSpeed
        {
            get
            {
                if (activeSkillData == null) return 10f;
                float speed = activeSkillData.projectile_speed * (1f + projectileSpeedModifier / 100f);
                if (supportData != null) speed *= supportData.speed_mult;
                return speed;
            }
        }

        private float FinalActiveRange => activeSkillData != null
            ? activeSkillData.range * (1f + rangeModifier / 100f)
            : 1000f;

        private float FinalActiveProjectileLifetime
        {
            get
            {
                if (activeSkillData == null) return 5f;
                float lifetime = activeSkillData.skill_lifetime;
                // Support skill duration multiplier (currently not in CSV, future feature)
                return lifetime;
            }
        }

        // Active Skill 발사체 개수 (기본 개수 + 서포트 추가 개수)
        private int FinalActiveProjectileCount
        {
            get
            {
                if (activeSkillData == null) return 1;

                // 기본 발사체 개수 + 서포트 스킬 추가 개수
                int baseCount = activeSkillData.projectile_count;
                int additionalCount = supportData?.add_projectiles ?? 0;
                return Mathf.Max(1, baseCount + additionalCount);
            }
        }

        #endregion

        #region Skill Data Loading

        //LMJ : Load skill data from CSV and SkillEffectDatabase (fully migrated - no legacy fallback)
        private void LoadSkillData()
        {
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogError("[Character] CSVLoader not initialized!");
                return;
            }

            // Use new SkillEffectDatabase exclusively
            var effectDb = SkillEffectDatabase.Instance;

            // Basic Attack Skill
            if (basicAttackSkillId > 0)
            {
                basicAttackData = CSVLoader.Instance.GetData<MainSkillData>(basicAttackSkillId);
                if (basicAttackData != null)
                {
                    var effectEntry = effectDb?.GetEntry(basicAttackSkillId);
                    if (effectEntry != null && effectEntry.HasMainEffect())
                    {
                        basicAttackPrefabs = ConvertToMainSkillPrefabEntry(effectEntry);
                    }
                    // No fallback - if not in SkillEffectDatabase, prefabs remain null
                    Debug.Log($"[Character] Loaded basic attack: {basicAttackData.skill_name} (ID: {basicAttackSkillId})");
                }
                else
                {
                    Debug.LogError($"[Character] Basic attack skill ID {basicAttackSkillId} not found in CSV!");
                }
            }

            // Active Skill
            if (activeSkillId > 0)
            {
                activeSkillData = CSVLoader.Instance.GetData<MainSkillData>(activeSkillId);
                if (activeSkillData != null)
                {
                    var effectEntry = effectDb?.GetEntry(activeSkillId);
                    if (effectEntry != null && effectEntry.HasMainEffect())
                    {
                        activeSkillPrefabs = ConvertToMainSkillPrefabEntry(effectEntry);
                    }
                    // No fallback - if not in SkillEffectDatabase, prefabs remain null
                    Debug.Log($"[Character] Loaded active skill: {activeSkillData.skill_name} (ID: {activeSkillId})");
                }
                else
                {
                    Debug.LogWarning($"[Character] Active skill ID {activeSkillId} not found in CSV!");
                }
            }

            // Support Skill + Compatibility 검증
            if (supportSkillId > 0)
            {
                supportData = CSVLoader.Instance.GetData<SupportSkillData>(supportSkillId);
                if (supportData != null)
                {
                    // CompatibilityTable 검증
                    bool isCompatible = ValidateSupportCompatibility(basicAttackData, supportData);
                    if (!isCompatible)
                    {
                        Debug.LogWarning($"[Character] 서포트 스킬 '{supportData.support_name}'은(는) 메인 스킬 '{basicAttackData?.skill_name}'과 호환되지 않습니다! 서포트 효과가 제한됩니다.");
                    }

                    // Support prefabs no longer used - effect modifiers come from CSV data
                    Debug.Log($"[Character] Loaded support skill: {supportData.support_name} (ID: {supportSkillId}, speed_mult: {supportData.speed_mult}, damage_mult: {supportData.damage_mult}, compatible: {isCompatible})");
                }
                else
                {
                    Debug.LogWarning($"[Character] Support skill ID {supportSkillId} not found in CSV!");
                }
            }
            else
            {
                // 서포트 스킬이 없을 때 초기화
                supportData = null;
                Debug.Log("[Character] No support skill selected (supportData = null)");
            }
        }

        /// <summary>
        /// SkillEffectEntry를 기존 MainSkillPrefabEntry로 변환 (호환성 유지)
        /// </summary>
        private MainSkillPrefabEntry ConvertToMainSkillPrefabEntry(SkillEffectEntry entry)
        {
            if (entry == null) return null;
            return new MainSkillPrefabEntry
            {
                skillId = entry.skillId,
                skillName = entry.skillName,
                projectilePrefab = entry.mainEffectPrefab,
                hitEffectPrefab = entry.hitEffectPrefab,
                castEffectPrefab = entry.castEffectPrefab,
                trailEffectPrefab = entry.trailEffectPrefab,
                areaEffectPrefab = entry.areaEffectPrefab
            };
        }

        /// <summary>
        /// 서포트 스킬과 메인 스킬의 호환성 검증
        /// SupportCompatibilityTable 기반
        /// </summary>
        private bool ValidateSupportCompatibility(MainSkillData mainSkill, SupportSkillData support)
        {
            if (mainSkill == null || support == null) return false;

            var compatibilityTable = CSVLoader.Instance?.GetTable<SupportCompatibilityData>();
            if (compatibilityTable == null)
            {
                Debug.LogWarning("[Character] SupportCompatibilityTable not loaded. Skipping compatibility check.");
                return true; // 테이블 없으면 통과
            }

            var compatibility = compatibilityTable.GetId(support.support_id);
            if (compatibility == null)
            {
                Debug.LogWarning($"[Character] Compatibility data not found for support {support.support_id}");
                return true; // 데이터 없으면 통과
            }

            return compatibility.IsCompatibleWith(mainSkill.GetSkillType());
        }

        /// <summary>
        /// 현재 서포트 스킬이 메인 스킬과 호환되는지 확인
        /// </summary>
        private bool IsSupportCompatible(MainSkillData mainSkill)
        {
            if (supportData == null) return true; // 서포트 없으면 항상 true
            return ValidateSupportCompatibility(mainSkill, supportData);
        }

        #endregion

        #region Pool Initialization

        //LMJ : Initialize projectile pool (from basic attack skill)
        private void InitializeProjectilePool()
        {
            if (basicAttackData == null)
            {
                Debug.LogError("[Character] basicAttackData is null!");
                return;
            }

            // Check for projectile prefab
            GameObject projectilePrefab = basicAttackPrefabs?.projectilePrefab;
            if (projectilePrefab == null && projectileTemplate == null)
            {
                Debug.LogWarning($"[Character] No projectile prefab for skill {basicAttackData.skill_name}. Using template.");
            }

            if (projectileTemplate == null)
            {
                Debug.LogError("[Character] ProjectileTemplate is not assigned! Please assign it in the Inspector.");
                return;
            }

            var pool = GameManager.Instance?.Pool;
            if (pool == null)
            {
                Debug.LogError("[Character] ObjectPoolManager is null!");
                return;
            }

            if (!pool.HasPool<Projectile>())
            {
                bool success = pool.CreatePool<Projectile>(projectileTemplate, defaultCapacity: 20, maxSize: 100);
                if (success)
                {
                    pool.WarmUp<Projectile>(20);
                    Debug.Log($"[Character] Projectile pool initialized for skill: {basicAttackData.skill_name}");
                }
                else
                {
                    Debug.LogError($"[Character] Failed to create Projectile pool!");
                }
            }
        }

        //LMJ : Initialize active skill projectile pool
        private void InitializeActiveSkillPool()
        {
            // activeSkillId가 0이면 액티브 스킬이 없는 캐릭터 (정상 케이스)
            if (activeSkillData == null)
            {
                // Debug 레벨을 Log로 낮춤 (모든 캐릭터가 액티브 스킬을 가지는 것은 아님)
                // Debug.Log("[Character] No active skill assigned. Skipping active skill initialization.");
                return;
            }

            if (projectileTemplate == null)
            {
                Debug.LogError("[Character] ProjectileTemplate is not assigned! Please assign it in the Inspector.");
                return;
            }

            var pool = GameManager.Instance?.Pool;
            if (pool == null)
            {
                Debug.LogError("[Character] ObjectPoolManager is null!");
                return;
            }

            // ProjectileTemplate은 모든 스킬이 공유하므로 이미 생성되어 있을 수 있음
            if (!pool.HasPool<Projectile>())
            {
                bool success = pool.CreatePool<Projectile>(projectileTemplate, defaultCapacity: 10, maxSize: 50);
                if (success)
                {
                    pool.WarmUp<Projectile>(10);
                    Debug.Log($"[Character] Active skill pool initialized for skill: {activeSkillData.skill_name}");
                }
            }
        }

        #endregion
    }
}
