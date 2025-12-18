using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

/// <summary>
/// 스킬 이펙트 매니저
/// SpecialSkillsEffectsPack 에셋 기반 스킬 이펙트의 스폰 및 관리
/// ObjectPoolManager와 연동하여 이펙트 풀링 처리
/// </summary>
public class SkillEffectManager : BaseManager
{
    [Header("설정")]
    [SerializeField] private SkillEffectDatabase database;

    [Header("풀 설정")]
    [Tooltip("이펙트당 기본 풀 크기")]
    [SerializeField] private int defaultPoolCapacity = 10;

    [Tooltip("이펙트당 최대 풀 크기")]
    [SerializeField] private int maxPoolSize = 50;

    // 전역 스케일 (VariousEffectsScene.m_gaph_scenesizefactor 대체)
    public static float GlobalScaleFactor { get; set; } = 1f;

    // 싱글톤 인스턴스
    private static SkillEffectManager _instance;
    public static SkillEffectManager Instance => _instance;

    // 풀 초기화 상태 추적
    private HashSet<int> _initializedPools = new HashSet<int>();

    // ObjectPoolManager 참조
    private ObjectPoolManager _poolManager;

    // 활성 이펙트 추적
    private Dictionary<int, List<SkillEffectWrapper>> _activeEffects = new Dictionary<int, List<SkillEffectWrapper>>();

    protected override void OnInitialize()
    {
        _instance = this;

        // ObjectPoolManager 찾기
        _poolManager = FindAnyObjectByType<ObjectPoolManager>();
        if (_poolManager == null)
        {
            Debug.LogError("[SkillEffectManager] ObjectPoolManager not found!");
        }

        // Database 로드
        if (database == null)
        {
            database = SkillEffectDatabase.Instance;
        }

        if (database != null)
        {
            // 전역 스케일 동기화
            GlobalScaleFactor = database.globalScaleFactor;
            database.Initialize();
        }

        Debug.Log("[SkillEffectManager] Initialized");
    }

    protected override void OnReset()
    {
        // 모든 활성 이펙트 회수
        DespawnAllEffects();
        _initializedPools.Clear();
    }

    protected override void OnDispose()
    {
        DespawnAllEffects();
        _instance = null;
    }

    #region Pool Management

    /// <summary>
    /// 특정 스킬의 이펙트 풀 초기화
    /// </summary>
    public async UniTask InitializePoolForSkill(int skillId)
    {
        if (_initializedPools.Contains(skillId)) return;

        var entry = database?.GetEntry(skillId);
        if (entry == null || entry.mainEffectPrefab == null)
        {
            Debug.LogWarning($"[SkillEffectManager] No effect entry for skill {skillId}");
            return;
        }

        // 래퍼 프리팹 키 생성
        string poolKey = GetPoolKey(skillId);

        // 래퍼 프리팹 동적 생성 및 풀 등록
        var wrapperPrefab = CreateWrapperPrefab(skillId, entry);
        if (wrapperPrefab != null)
        {
            bool success = _poolManager.CreatePoolByKey<SkillEffectWrapper>(
                poolKey,
                wrapperPrefab,
                defaultPoolCapacity,
                maxPoolSize
            );

            if (success)
            {
                _initializedPools.Add(skillId);
                Debug.Log($"[SkillEffectManager] Pool initialized for skill {skillId}");
            }
        }

        await UniTask.CompletedTask;
    }

    /// <summary>
    /// 래퍼 프리팹 동적 생성
    /// </summary>
    private GameObject CreateWrapperPrefab(int skillId, SkillEffectEntry entry)
    {
        // 템플릿 게임오브젝트 생성
        var wrapperObj = new GameObject($"SkillEffectWrapper_{skillId}");
        wrapperObj.SetActive(false);

        // 래퍼 컴포넌트 추가
        var wrapper = wrapperObj.AddComponent<SkillEffectWrapper>();

        // 에셋 프리팹을 자식으로 추가
        var effectInstance = Instantiate(entry.mainEffectPrefab, wrapperObj.transform);
        effectInstance.name = entry.mainEffectPrefab.name;
        effectInstance.transform.localPosition = Vector3.zero;
        effectInstance.transform.localRotation = Quaternion.identity;

        // DontDestroyOnLoad로 유지 (풀에서 관리)
        DontDestroyOnLoad(wrapperObj);

        return wrapperObj;
    }

    private string GetPoolKey(int skillId)
    {
        return $"SkillEffect_{skillId}";
    }

    #endregion

    #region Spawn Methods

    /// <summary>
    /// 스킬 이펙트 스폰
    /// </summary>
    /// <param name="skillId">스킬 ID</param>
    /// <param name="position">스폰 위치</param>
    /// <param name="rotation">스폰 회전</param>
    /// <param name="target">타겟 (선택)</param>
    /// <param name="damageMultiplier">데미지 배율</param>
    /// <returns>스폰된 SkillEffectWrapper</returns>
    public async UniTask<SkillEffectWrapper> SpawnSkillEffect(
        int skillId,
        Vector3 position,
        Quaternion rotation,
        Transform target = null,
        float damageMultiplier = 1f)
    {
        // 풀 초기화 확인
        if (!_initializedPools.Contains(skillId))
        {
            await InitializePoolForSkill(skillId);
        }

        if (!_initializedPools.Contains(skillId))
        {
            Debug.LogWarning($"[SkillEffectManager] Failed to initialize pool for skill {skillId}");
            return null;
        }

        // 스킬 데이터 로드
        MainSkillData skillData = null;
        if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            skillData = CSVLoader.Instance.GetData<MainSkillData>(skillId);
        }

        // 풀에서 스폰
        string poolKey = GetPoolKey(skillId);
        var wrapper = _poolManager.SpawnByKey<SkillEffectWrapper>(poolKey, position, rotation);

        if (wrapper != null)
        {
            // 초기화
            float scale = database?.GetEffectScale(skillId) ?? GlobalScaleFactor;
            wrapper.Initialize(skillId, skillData, target, scale, damageMultiplier);

            // 완료 이벤트 등록
            wrapper.OnEffectComplete += HandleEffectComplete;

            // 활성 이펙트 추적
            TrackActiveEffect(skillId, wrapper);
        }

        return wrapper;
    }

    /// <summary>
    /// 스킬 이펙트 스폰 (간편 버전)
    /// </summary>
    public async UniTask<SkillEffectWrapper> SpawnSkillEffect(int skillId, Vector3 position, Transform target = null)
    {
        return await SpawnSkillEffect(skillId, position, Quaternion.identity, target);
    }

    /// <summary>
    /// 타겟 방향으로 스킬 이펙트 스폰
    /// </summary>
    public async UniTask<SkillEffectWrapper> SpawnSkillEffectToward(
        int skillId,
        Vector3 position,
        Transform target,
        float damageMultiplier = 1f)
    {
        Quaternion rotation = Quaternion.identity;
        if (target != null)
        {
            Vector3 direction = (target.position - position).normalized;
            if (direction != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(direction);
            }
        }

        return await SpawnSkillEffect(skillId, position, rotation, target, damageMultiplier);
    }

    /// <summary>
    /// 피격 이펙트 스폰 (모든 히트 이펙트는 이 메서드를 통해 스폰)
    /// 에셋 원본 그대로 사용 - 스케일/속도 override 없음
    /// </summary>
    public GameObject SpawnHitEffect(int skillId, Vector3 position, Quaternion rotation = default)
    {
        var entry = database?.GetEntry(skillId);
        if (entry?.hitEffectPrefab == null) return null;

        if (rotation == default) rotation = Quaternion.identity;
        var effect = Instantiate(entry.hitEffectPrefab, position, rotation);
        effect.name = $"HitEffect_{skillId}_{Time.frameCount}";

        // 에셋 원본 스케일 유지 (override 제거)

        // 에셋 자체 PlayOnAwake 또는 스크립트에 의존 (강제 재생 제거)

        // 에셋 자체 DestroyObject 스크립트에 의존 (자동 삭제)
        // DestroyObject가 없는 에셋은 fallback으로 3초 후 삭제
        var destroyer = effect.GetComponent<DestroyObject>();
        if (destroyer == null)
        {
            Destroy(effect, 3f);
        }

        Debug.Log($"[SkillEffectManager] SpawnHitEffect: skill={skillId}, pos={position} (asset native)");
        return effect;
    }

    /// <summary>
    /// 피격 이펙트 스폰 (Collider 중심점에 스폰)
    /// </summary>
    public GameObject SpawnHitEffectAtCollider(int skillId, Collider collider)
    {
        if (collider == null) return null;
        Vector3 hitPos = collider.bounds.center;
        return SpawnHitEffect(skillId, hitPos);
    }

    /// <summary>
    /// 시전 이펙트 스폰 (모든 시전 이펙트는 이 메서드를 통해 스폰)
    /// 에셋 원본 그대로 사용 - 스케일/속도 override 없음
    /// </summary>
    public GameObject SpawnCastEffect(int skillId, Vector3 position, Transform parent = null)
    {
        var entry = database?.GetEntry(skillId);
        if (entry?.castEffectPrefab == null) return null;

        var effect = Instantiate(entry.castEffectPrefab, position, Quaternion.identity);
        effect.name = $"CastEffect_{skillId}_{Time.frameCount}";

        // 에셋 원본 스케일 유지 (override 제거)

        // 에셋 자체 PlayOnAwake 또는 스크립트에 의존 (강제 재생 제거)

        if (parent != null)
        {
            effect.transform.SetParent(parent);
            effect.transform.localPosition = Vector3.zero;
        }

        // 에셋 자체 DestroyObject 스크립트에 의존 (자동 삭제)
        var destroyer = effect.GetComponent<DestroyObject>();
        if (destroyer == null)
        {
            Destroy(effect, 3f);
        }

        Debug.Log($"[SkillEffectManager] SpawnCastEffect: skill={skillId}, pos={position} (asset native)");
        return effect;
    }

    /// <summary>
    /// 메인 이펙트 직접 스폰 (프리팹 직접 Instantiate가 필요한 경우)
    /// 에셋 원본 그대로 사용 - 스케일/속도/hitObjectScale override 없음
    /// </summary>
    public GameObject SpawnMainEffect(int skillId, Vector3 position, Quaternion rotation = default)
    {
        var entry = database?.GetEntry(skillId);
        if (entry?.mainEffectPrefab == null) return null;

        if (rotation == default) rotation = Quaternion.identity;
        var effect = Instantiate(entry.mainEffectPrefab, position, rotation);
        effect.name = $"MainEffect_{skillId}_{Time.frameCount}";

        // 에셋 원본 스케일 유지 (override 제거)

        // 에셋 자체 PlayOnAwake 또는 스크립트에 의존 (강제 재생 제거)

        // 에셋 자체 ObjectMoveDestroy의 hitObjectScale 사용 (override 제거)

        Debug.Log($"[SkillEffectManager] SpawnMainEffect: skill={skillId}, pos={position} (asset native)");
        return effect;
    }

    #endregion

    #region Despawn Methods

    /// <summary>
    /// 이펙트 완료 핸들러
    /// </summary>
    private void HandleEffectComplete(SkillEffectWrapper wrapper)
    {
        if (wrapper == null) return;

        wrapper.OnEffectComplete -= HandleEffectComplete;

        // 활성 이펙트에서 제거
        UntrackActiveEffect(wrapper.skillId, wrapper);

        // 풀에 반환
        string poolKey = GetPoolKey(wrapper.skillId);
        _poolManager.DespawnByKey(poolKey, wrapper);
    }

    /// <summary>
    /// 특정 이펙트 수동 회수
    /// </summary>
    public void DespawnEffect(SkillEffectWrapper wrapper)
    {
        if (wrapper == null) return;

        wrapper.OnEffectComplete -= HandleEffectComplete;
        UntrackActiveEffect(wrapper.skillId, wrapper);

        string poolKey = GetPoolKey(wrapper.skillId);
        _poolManager.DespawnByKey(poolKey, wrapper);
    }

    /// <summary>
    /// 특정 스킬의 모든 이펙트 회수
    /// </summary>
    public void DespawnAllEffectsForSkill(int skillId)
    {
        if (!_activeEffects.TryGetValue(skillId, out var effects)) return;

        var effectsCopy = new List<SkillEffectWrapper>(effects);
        foreach (var wrapper in effectsCopy)
        {
            DespawnEffect(wrapper);
        }
    }

    /// <summary>
    /// 모든 이펙트 회수
    /// </summary>
    public void DespawnAllEffects()
    {
        var skillIds = new List<int>(_activeEffects.Keys);
        foreach (var skillId in skillIds)
        {
            DespawnAllEffectsForSkill(skillId);
        }
    }

    #endregion

    #region Active Effect Tracking

    private void TrackActiveEffect(int skillId, SkillEffectWrapper wrapper)
    {
        if (!_activeEffects.ContainsKey(skillId))
        {
            _activeEffects[skillId] = new List<SkillEffectWrapper>();
        }
        _activeEffects[skillId].Add(wrapper);
    }

    private void UntrackActiveEffect(int skillId, SkillEffectWrapper wrapper)
    {
        if (_activeEffects.TryGetValue(skillId, out var effects))
        {
            effects.Remove(wrapper);
        }
    }

    /// <summary>
    /// 특정 스킬의 활성 이펙트 수 반환
    /// </summary>
    public int GetActiveEffectCount(int skillId)
    {
        if (_activeEffects.TryGetValue(skillId, out var effects))
        {
            return effects.Count;
        }
        return 0;
    }

    /// <summary>
    /// 전체 활성 이펙트 수 반환
    /// </summary>
    public int GetTotalActiveEffectCount()
    {
        int count = 0;
        foreach (var pair in _activeEffects)
        {
            count += pair.Value.Count;
        }
        return count;
    }

    #endregion

    #region Independent Effect Spawn (독립 이펙트 스폰)

    /// <summary>
    /// 독립 이펙트 스폰 (Projectile/스킬 로직과 분리)
    /// 에셋 원본 그대로 사용 - 스케일/속도/hitObjectScale override 없음
    /// 이펙트는 에셋 자체 스크립트(DestroyObject/ObjectMoveDestroy)에 의해 관리됨
    /// </summary>
    /// <param name="skillId">스킬 ID</param>
    /// <param name="position">스폰 위치</param>
    /// <param name="rotation">스폰 회전</param>
    /// <param name="followTarget">따라갈 Transform (선택)</param>
    /// <param name="useAssetMovement">에셋 자체 이동 로직 사용 여부 (기본 true)</param>
    /// <returns>스폰된 이펙트 GameObject</returns>
    public async UniTask<GameObject> SpawnIndependentEffect(
        int skillId,
        Vector3 position,
        Quaternion rotation,
        Transform followTarget = null,
        bool useAssetMovement = true)
    {
        var entry = database?.GetEntry(skillId);
        if (entry == null || entry.mainEffectPrefab == null)
        {
            Debug.LogWarning($"[SkillEffectManager] No effect entry for skill {skillId}");
            return null;
        }

        // 이펙트 인스턴스 생성 (독립 오브젝트)
        var effectInstance = Instantiate(entry.mainEffectPrefab, position, rotation);
        effectInstance.name = $"SkillEffect_{skillId}_{Time.frameCount}";

        // 에셋 원본 스케일 유지 (override 제거)

        Debug.Log($"[SkillEffectManager] SpawnIndependentEffect: skill={skillId}, position={position}, prefab={entry.mainEffectPrefab.name} (asset native)");

        // 에셋 자체 PlayOnAwake 또는 스크립트에 의존 (강제 재생 제거)

        // 에셋 자체 ObjectMoveDestroy의 hitObjectScale 사용 (override 제거)

        // FollowTarget 컴포넌트 추가 (따라가기 필요시)
        if (followTarget != null)
        {
            var follower = effectInstance.AddComponent<EffectFollower>();
            follower.Initialize(followTarget);
        }

        // 에셋 자체 DestroyObject/ObjectMoveDestroy 스크립트에 의존
        // Fallback: 에셋에 자동 삭제 스크립트가 없으면 ParticleSystem duration 기반으로 삭제
        var destroyer = effectInstance.GetComponent<DestroyObject>();
        var moveDestroy = effectInstance.GetComponentInChildren<ObjectMoveDestroy>();
        if (destroyer == null && moveDestroy == null)
        {
            float effectDuration = CalculateEffectDuration(effectInstance);
            AutoDespawnEffectAsync(effectInstance, effectDuration).Forget();
        }

        await UniTask.CompletedTask;
        return effectInstance;
    }

    /// <summary>
    /// 순수 시각 이펙트 재생 (데미지 없음, 독립 실행)
    /// 에셋 원본 그대로 사용
    /// </summary>
    public async UniTask<GameObject> PlayVisualEffect(
        int skillId,
        Vector3 position,
        Quaternion rotation)
    {
        return await SpawnIndependentEffect(skillId, position, rotation, null, true);
    }

    /// <summary>
    /// 위치 기반 이펙트 재생 (AOE, 즉발 스킬용)
    /// 에셋 원본 그대로 사용
    /// </summary>
    public async UniTask<GameObject> PlayEffectAtPosition(
        int skillId,
        Vector3 position)
    {
        return await SpawnIndependentEffect(skillId, position, Quaternion.identity, null, true);
    }

    /// <summary>
    /// 타겟 따라가는 이펙트 재생 (버프, 상태이상 아이콘용)
    /// 에셋 원본 그대로 사용
    /// </summary>
    public async UniTask<GameObject> PlayEffectOnTarget(
        int skillId,
        Transform target,
        Vector3 offset = default)
    {
        if (target == null) return null;

        Vector3 spawnPos = target.position + offset;
        var effect = await SpawnIndependentEffect(skillId, spawnPos, Quaternion.identity, target, true);

        // 오프셋이 있으면 EffectFollower에 설정
        if (effect != null && offset != default)
        {
            var follower = effect.GetComponent<EffectFollower>();
            if (follower != null)
            {
                follower.SetOffset(offset);
            }
        }

        return effect;
    }

    /// <summary>
    /// Projectile과 함께 이동하는 이펙트 스폰 (투사체용)
    /// Projectile이 despawn되어도 이펙트는 재생 완료까지 유지
    /// </summary>
    public async UniTask<GameObject> SpawnProjectileEffect(
        int skillId,
        Transform projectileTransform,
        Vector3 direction)
    {
        if (projectileTransform == null) return null;

        Quaternion rotation = direction != Vector3.zero
            ? Quaternion.LookRotation(direction)
            : projectileTransform.rotation;

        // 이펙트 스폰 (에셋 자체 이동 사용)
        var effect = await SpawnIndependentEffect(
            skillId,
            projectileTransform.position,
            rotation,
            projectileTransform, // Projectile을 따라감
            true // 에셋 이동 로직 활성화
        );

        return effect;
    }

    // DisableAssetMovement 제거 - 에셋 원본 그대로 사용

    /// <summary>
    /// 이펙트 duration 계산 (ParticleSystem 기반)
    /// </summary>
    private float CalculateEffectDuration(GameObject effectInstance)
    {
        float maxDuration = 2f; // 기본값

        var particles = effectInstance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            var main = ps.main;
            float psDuration = main.duration + main.startLifetime.constantMax;

            if (main.loop)
            {
                // 루프 파티클은 기본 duration 사용
                psDuration = main.duration;
            }

            if (psDuration > maxDuration)
            {
                maxDuration = psDuration;
            }
        }

        // 최소 2초, 최대 10초
        return Mathf.Clamp(maxDuration, 2f, 10f);
    }

    /// <summary>
    /// 이펙트 자동 회수 (duration 후)
    /// </summary>
    private async UniTaskVoid AutoDespawnEffectAsync(GameObject effect, float duration)
    {
        try
        {
            await UniTask.Delay((int)(duration * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());

            if (effect != null)
            {
                Destroy(effect);
            }
        }
        catch (System.OperationCanceledException)
        {
            // 매니저가 파괴되면 무시
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// 데이터베이스 설정 (외부에서 로드한 경우)
    /// </summary>
    public void SetDatabase(SkillEffectDatabase db)
    {
        database = db;
        if (database != null)
        {
            GlobalScaleFactor = database.globalScaleFactor;
            database.Initialize();
        }
    }

    /// <summary>
    /// 전역 스케일 설정
    /// </summary>
    public void SetGlobalScale(float scale)
    {
        GlobalScaleFactor = scale;
        if (database != null)
        {
            database.globalScaleFactor = scale;
        }

        // VariousEffectsScene도 동기화
        VariousEffectsScene.m_gaph_scenesizefactor = scale;
    }

    #endregion
}
