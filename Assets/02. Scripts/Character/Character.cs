//LMJ : Character - 새 스킬 시스템 구현 완료 (Issue #448)
//     - SkillExecutor 기반 스킬 실행
//     - CSV 기반 MainSkillData + SupportSkillData 연동
//     - Addressable 프리팹 로딩
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;
    using NovelianMagicLibraryDefense.Managers;

    /// <summary>
    /// 캐릭터 메인 클래스
    ///
    /// TODO: 새 스킬 시스템 구현 예정
    /// - 새 에셋팩 연동
    /// - 단순한 구조로 재설계
    ///
    /// Partial Classes (유지):
    /// - Character.Stats.cs: 스탯/버프/성급/책갈피
    /// - Character.Targeting.cs: 타겟 선정 로직
    /// - Character.StatusEffects.cs: 상태이상 적용
    /// - Character.Visuals.cs: 비주얼 파츠 관리
    /// - Character.Animation.cs: 애니메이션 제어
    /// </summary>
    public partial class Character : MonoBehaviour, IPoolable
    {
        #region Serialized Fields

        [Header("Character Visual")]
        [SerializeField] private GameObject characterObj;

        [Header("Character Animator")]
        [SerializeField] private Animator characterAnimator;

        [Header("스킬 장착 (Skill Equipment) - CSV ID 기반")]
        [SerializeField, Tooltip("기본 공격 스킬 ID (MainSkillTable)")]
        private int basicAttackSkillId = 50000;

        [SerializeField, Tooltip("액티브 스킬 ID (MainSkillTable)")]
        private int activeSkillId = 0;

        [SerializeField, Tooltip("보조 스킬 ID (SupportSkillTable)")]
        private int supportSkillId = 0;

        [Header("캐릭터 스텟 변형 (%) (Character Stat Modifiers)")]
        [SerializeField, Tooltip("데미지 변형 (%)")]
        private float damageModifier = 0f;

        [SerializeField, Tooltip("공격 속도 변형 (%)")]
        private float attackSpeedModifier = 0f;

        [SerializeField, Tooltip("투사체 속도 변형 (%)")]
        private float projectileSpeedModifier = 0f;

        [SerializeField, Tooltip("사거리 변형 (%)")]
        private float rangeModifier = 0f;

        [SerializeField, Tooltip("치명타 확률 변형 (%)")]
        private float critChanceModifier = 0f;

        [SerializeField, Tooltip("치명타 배율 변형 (%)")]
        private float critMultiplierModifier = 0f;

        [SerializeField, Tooltip("추가 데미지 변형 (%)")]
        private float bonusDamageModifier = 0f;

        [SerializeField, Tooltip("체력 회복 변형 (%)")]
        private float healthRegenModifier = 0f;

        [SerializeField, Tooltip("쿨타임 감소 (%)")]
        private float cooldownModifier = 0f;

        [SerializeField, Tooltip("시전 속도 감소 (%)")]
        private float castTimeModifier = 0f;

        [SerializeField, Tooltip("보스 데미지 증가 (%)")]
        private float bossDamageModifier = 0f;

        [Header("Spawn Position")]
        [SerializeField, Tooltip("Projectile spawn offset (Y=1.5 for chest height)")]
        private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Targeting Strategy")]
        [SerializeField, Tooltip("Use weight-based targeting (default: distance-based)")]
        private bool useWeightTargeting = false;

        #endregion

        #region Private Fields

        // 캐싱된 스킬 데이터
        private MainSkillData basicAttackData;
        private MainSkillData activeSkillData;
        private SupportSkillData supportData;

        // Attack state
        private CancellationTokenSource attackCts;
        private CancellationTokenSource activeSkillCts;
        private bool isInitialized = false;

        // Issue #476: 도전던전 스턴 상태
        private bool isStunnedByBossDungeon = false;

        // JML: 책갈피 시스템 (Issue #320)
        private int characterId = -1;
        private bool isManuallyInitialized = false;
        private bool autoAttackEnabled = true;

        // JML: 비주얼 파츠 캐시 (Issue #356)
        private Dictionary<string, Transform> cachedTransforms = new Dictionary<string, Transform>();
        private Transform weaponRightSlot;
        private Transform weaponLeftSlot;

        // 속성(장르) 강화 modifier
        private Dictionary<GenreType, float> genreModifiers = new Dictionary<GenreType, float>();

        // TrainingScene용 투사체 템플릿 (Pool 없는 환경)
        private GameObject projectileTemplate;

        #endregion

        #region Lifecycle

        private void Start()
        {
            if (isManuallyInitialized) return;

            ApplyBookmarksIfAvailable();
            LoadSkillData();

            if (autoAttackEnabled)
            {
                StartAttackLoop();
            }

            isInitialized = true;
        }

        /// <summary>
        /// CSV 데이터 기반 초기화
        /// </summary>
        public void Initialize(int csvCharacterId)
        {
            characterId = csvCharacterId;
            isManuallyInitialized = true;


            // 비주얼 파츠 적용
            ApplyVisualConfig(csvCharacterId);

            // CSV에서 캐릭터 데이터 로드
            var characterData = CSVLoader.Instance?.GetData<CharacterData>(csvCharacterId);
            if (characterData != null)
            {
                basicAttackSkillId = characterData.Base_Skill_ID;
            }

            ApplyBookmarksIfAvailable();
            LoadSkillData();
            StartAttackLoop();

            isInitialized = true;
        }

        private void OnDestroy()
        {
            attackCts?.Cancel();
            attackCts?.Dispose();
            activeSkillCts?.Cancel();
            activeSkillCts?.Dispose();
        }

        #endregion

        #region Skill Data Loading (Stub)

        /// <summary>
        /// 스킬 데이터 로드 (스텁 - 새 시스템 구현 시 교체)
        /// </summary>
        private void LoadSkillData()
        {
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogWarning("[Character] CSVLoader not initialized!");
                return;
            }

            // Basic Attack Skill
            if (basicAttackSkillId > 0)
            {
                basicAttackData = CSVLoader.Instance.GetData<MainSkillData>(basicAttackSkillId);
            }

            // Active Skill
            if (activeSkillId > 0)
            {
                activeSkillData = CSVLoader.Instance.GetData<MainSkillData>(activeSkillId);
            }

            // Support Skill
            if (supportSkillId > 0)
            {
                supportData = CSVLoader.Instance.GetData<SupportSkillData>(supportSkillId);
            }
        }

        #endregion

        #region Attack Loop (Stub)

        private void StartAttackLoop()
        {
            attackCts?.Cancel();
            attackCts?.Dispose();
            attackCts = new CancellationTokenSource();
            AttackLoopAsync(attackCts.Token).Forget();
        }

        private async UniTaskVoid AttackLoopAsync(CancellationToken ct)
        {
            // 첫 공격은 즉시 실행
            if (!ct.IsCancellationRequested && Time.timeScale > 0f)
            {
                TryAttack();
            }

            while (!ct.IsCancellationRequested)
            {
                float interval = CalculateAttackInterval();

                await UniTask.Delay((int)(interval * 1000), cancellationToken: ct);

                if (Time.timeScale == 0f) continue;
                if (isStunnedByBossDungeon) continue;

                TryAttack();
            }
        }

        /// <summary>
        /// 공격 간격 계산 (쿨다운 모디파이어 + 서포트 스킬 효과 적용)
        /// </summary>
        private float CalculateAttackInterval()
        {
            float baseCooldown = basicAttackData?.cooldown ?? 1.5f;

            // 쿨다운 모디파이어 적용
            float cooldownReduction = 1f - (cooldownModifier / 100f);
            float interval = baseCooldown * cooldownReduction;

            // Enhance 서포트 - 쿨다운 감소 (scale_multiplier가 0.8이면 80%로 감소)
            if (supportData != null && supportData.IsEnhanceSupport && supportData.scale_multiplier > 0)
            {
                interval *= supportData.scale_multiplier;
            }

            return Mathf.Max(0.1f, interval);
        }

        private void TryAttack()
        {
            if (!isInitialized || basicAttackData == null) return;

            // 타겟 찾기
            float searchRange = basicAttackData.range > 0 ? basicAttackData.range : 100f;
            ITargetable target = TargetRegistry.Instance?.FindTarget(transform.position, searchRange, useWeightTargeting);

            if (target == null)
            {
                LookAtNearestSpawner();
                return;
            }

            LookAtTarget(target);
            PlayAttackAnimation();

            // 스킬 실행 (SkillExecutor 사용)
            ExecuteBasicAttackAsync(target).Forget();
        }

        /// <summary>
        /// 기본 공격 스킬 실행
        /// </summary>
        private async UniTaskVoid ExecuteBasicAttackAsync(ITargetable target)
        {
            if (basicAttackData == null) return;

            await SkillExecutor.Instance.ExecuteSkillAsync(
                transform,
                target,
                basicAttackData,
                supportData
            );
        }

        #endregion

        #region Targeting

        private void LookAtTarget(ITargetable target)
        {
            if (target == null) return;

            Vector3 targetPos = target.GetPosition();
            Vector3 direction = targetPos - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void LookAtNearestSpawner()
        {
            GameObject spawner1 = GameObject.FindWithTag("SpawnArea1");
            GameObject spawner2 = GameObject.FindWithTag("SpawnArea2");

            Vector3? targetDir = null;

            if (spawner1 != null && spawner2 != null)
            {
                float dist1 = Vector3.Distance(transform.position, spawner1.transform.position);
                float dist2 = Vector3.Distance(transform.position, spawner2.transform.position);
                targetDir = dist1 <= dist2
                    ? spawner1.transform.position - transform.position
                    : spawner2.transform.position - transform.position;
            }
            else if (spawner1 != null)
            {
                targetDir = spawner1.transform.position - transform.position;
            }
            else if (spawner2 != null)
            {
                targetDir = spawner2.transform.position - transform.position;
            }

            if (targetDir.HasValue && targetDir.Value.sqrMagnitude > 0.01f)
            {
                Vector3 dir = targetDir.Value;
                dir.y = 0;
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        #endregion

        #region IPoolable Implementation

        public void OnSpawn()
        {
            characterObj?.SetActive(true);

            if (!isInitialized)
            {
                Start();
            }
            else
            {
                StartAttackLoop();
            }

        }

        public void OnDespawn()
        {
            characterObj?.SetActive(false);
            attackCts?.Cancel();
            attackCts?.Dispose();
            attackCts = null;
            activeSkillCts?.Cancel();
            activeSkillCts?.Dispose();
            activeSkillCts = null;
        }

        #endregion

        #region Final Stats (버프 적용 후 최종 값)

        /// <summary>
        /// 최종 데미지 (기본 데미지 * modifier 적용)
        /// </summary>
        public float FinalDamage
        {
            get
            {
                if (basicAttackData == null) return 0f;
                return basicAttackData.base_damage * (1f + damageModifier / 100f);
            }
        }

        /// <summary>
        /// 최종 공격 속도 (기본 쿨다운 / modifier 적용)
        /// </summary>
        public float FinalAttackSpeed
        {
            get
            {
                if (basicAttackData == null || basicAttackData.cooldown <= 0) return 1f;
                // 공격 속도 = 1 / 쿨다운, modifier 적용
                float baseSpeed = 1f / basicAttackData.cooldown;
                return baseSpeed * (1f + attackSpeedModifier / 100f);
            }
        }

        /// <summary>
        /// 최종 사거리 (기본 사거리 * modifier 적용)
        /// </summary>
        public float FinalRange
        {
            get
            {
                if (basicAttackData == null) return 100f;
                return basicAttackData.range * (1f + rangeModifier / 100f);
            }
        }

        /// <summary>
        /// 최종 투사체 속도 (기본 속도 * modifier 적용)
        /// </summary>
        public float FinalProjectileSpeed
        {
            get
            {
                if (basicAttackData == null) return 10f;
                return basicAttackData.projectile_speed * (1f + projectileSpeedModifier / 100f);
            }
        }

        #endregion

        #region Public API

        public void SetAutoAttackEnabled(bool enabled)
        {
            autoAttackEnabled = enabled;

            if (!enabled)
            {
                attackCts?.Cancel();
                attackCts?.Dispose();
                attackCts = null;
            }
            else if (isInitialized)
            {
                StartAttackLoop();
            }
        }

        public void SetSpawnOffsetFromPreset(Vector3 offset)
        {
            spawnOffset = offset;
        }

        public void SetTargetingStrategy(bool useWeight)
        {
            useWeightTargeting = useWeight;
        }

        public void SetSkillIds(int basicAttackId, int activeId = 0, int supportId = 0)
        {
            basicAttackSkillId = basicAttackId;
            activeSkillId = activeId;
            supportSkillId = supportId;
            LoadSkillData();
        }

        public int GetBasicAttackSkillId() => basicAttackSkillId;
        public int GetSupportSkillId() => supportSkillId;
        public bool HasSupportSkill() => supportSkillId > 0 && supportData != null;

        /// <summary>
        /// 서포트 스킬 장착 (스텁 - 새 시스템 구현 시 교체)
        /// </summary>
        /// <param name="supportSkillId">서포트 스킬 ID</param>
        /// <returns>장착 성공 여부</returns>
        public bool EquipSupportSkill(int skillId)
        {
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogWarning("[Character] CSVLoader not initialized!");
                return false;
            }

            var skill = CSVLoader.Instance.GetData<SupportSkillData>(skillId);
            if (skill == null)
            {
                Debug.LogWarning($"[Character] Support skill not found: {skillId}");
                return false;
            }

            supportSkillId = skillId;
            supportData = skill;
            return true;
        }

        /// <summary>
        /// 투사체 템플릿 설정 (TrainingScene 등 Pool 없는 환경용)
        /// </summary>
        public void SetProjectileTemplate(GameObject template)
        {
            projectileTemplate = template;
            Debug.Log($"[Character] ProjectileTemplate 설정됨: {(template != null ? template.name : "null")}");
        }

        #endregion

        #region Boss Dungeon Stun (Issue #476)

        public void ApplyStunFromBossDungeon(float duration)
        {
            if (isStunnedByBossDungeon) return;
            isStunnedByBossDungeon = true;
        }

        public void ReleaseStunFromBossDungeon()
        {
            if (!isStunnedByBossDungeon) return;
            isStunnedByBossDungeon = false;
        }

        public bool IsAlive()
        {
            return gameObject.activeInHierarchy;
        }

        #endregion

        #region Buff/Debuff Modifier API

        /// <summary>
        /// 버프 모디파이어 적용
        /// </summary>
        public void ApplyBuffModifier(string statType, float value)
        {
            switch (statType)
            {
                case "damage":
                    damageModifier += value;
                    break;
                case "attackSpeed":
                    attackSpeedModifier += value;
                    break;
                case "range":
                    rangeModifier += value;
                    break;
                case "critChance":
                    critChanceModifier += value;
                    break;
                case "critMultiplier":
                    critMultiplierModifier += value;
                    break;
                case "projectileSpeed":
                    projectileSpeedModifier += value;
                    break;
                case "cooldown":
                    cooldownModifier += value;
                    break;
            }
        }

        /// <summary>
        /// 버프 모디파이어 제거
        /// </summary>
        public void RemoveBuffModifier(string statType, float value)
        {
            switch (statType)
            {
                case "damage":
                    damageModifier -= value;
                    break;
                case "attackSpeed":
                    attackSpeedModifier -= value;
                    break;
                case "range":
                    rangeModifier -= value;
                    break;
                case "critChance":
                    critChanceModifier -= value;
                    break;
                case "critMultiplier":
                    critMultiplierModifier -= value;
                    break;
                case "projectileSpeed":
                    projectileSpeedModifier -= value;
                    break;
                case "cooldown":
                    cooldownModifier -= value;
                    break;
            }
        }

        #endregion
    }
}
