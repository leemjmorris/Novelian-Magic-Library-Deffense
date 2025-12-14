using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 스킬 이펙트 래퍼 컴포넌트
/// SpecialSkillsEffectsPack 에셋 프리팹을 감싸서 게임 로직을 연결합니다
/// - 에셋 스크립트의 OnHit 이벤트를 구독하여 데미지 처리
/// - ObjectPool과 연동하여 재사용
/// </summary>
public class SkillEffectWrapper : MonoBehaviour, IPoolable
{
    [Header("스킬 정보 (런타임 설정)")]
    [Tooltip("스킬 ID")]
    public int skillId;

    [Tooltip("스킬 데이터 참조")]
    public MainSkillData skillData;

    [Tooltip("타겟 Transform")]
    public Transform target;

    [Header("설정")]
    [Tooltip("스케일 배율")]
    public float scaleMultiplier = 1f;

    [Tooltip("데미지 배율 (기본 1)")]
    public float damageMultiplier = 1f;

    [Header("이펙트 타임아웃")]
    [Tooltip("이펙트 자동 회수 시간 (초)")]
    public float lifetime = 10f;

    // 에셋 스크립트 참조
    private ObjectMoveDestroy _assetMoveDestroy;
    private ObjectMove _assetMove;

    // 이벤트
    /// <summary>
    /// 타겟에 명중했을 때 발생하는 이벤트
    /// </summary>
    public event Action<ITargetable, Vector3> OnHitTarget;

    /// <summary>
    /// 이펙트가 완료되었을 때 발생하는 이벤트 (풀 반환용)
    /// </summary>
    public event Action<SkillEffectWrapper> OnEffectComplete;

    // 내부 상태
    private float _spawnTime;
    private bool _isActive;
    private int _hitCount;

    private void Awake()
    {
        // 에셋 스크립트 찾기 (자식에서)
        _assetMoveDestroy = GetComponentInChildren<ObjectMoveDestroy>();
        _assetMove = GetComponentInChildren<ObjectMove>();

        // 이벤트 구독
        SubscribeToAssetEvents();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        UnsubscribeFromAssetEvents();
    }

    private void Update()
    {
        if (!_isActive) return;

        // 타임아웃 체크
        if (Time.time - _spawnTime > lifetime)
        {
            CompleteEffect();
        }
    }

    /// <summary>
    /// 이펙트 초기화
    /// </summary>
    /// <param name="skillId">스킬 ID</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="target">타겟 (선택)</param>
    /// <param name="scale">스케일</param>
    /// <param name="damageMultiplier">데미지 배율</param>
    public void Initialize(int skillId, MainSkillData skillData, Transform target = null, float scale = 1f, float damageMultiplier = 1f)
    {
        this.skillId = skillId;
        this.skillData = skillData;
        this.target = target;
        this.scaleMultiplier = scale;
        this.damageMultiplier = damageMultiplier;

        // 스케일 적용
        transform.localScale = Vector3.one * scale;

        // 에셋 스크립트 속도 오버라이드 (필요 시)
        if (skillData != null)
        {
            OverrideAssetSpeed(skillData.projectile_speed);
        }
    }

    /// <summary>
    /// 에셋 스크립트의 속도를 스킬 데이터 값으로 오버라이드
    /// </summary>
    private void OverrideAssetSpeed(float speed)
    {
        if (speed <= 0) return;

        if (_assetMoveDestroy != null)
        {
            _assetMoveDestroy.MoveSpeed = speed;
        }

        if (_assetMove != null)
        {
            _assetMove.MoveSpeed = speed;
        }
    }

    #region Asset Event Handling

    private void SubscribeToAssetEvents()
    {
        if (_assetMoveDestroy != null)
        {
            _assetMoveDestroy.OnHit += HandleAssetHit;
        }

        if (_assetMove != null)
        {
            _assetMove.OnHit += HandleAssetHit;
        }
    }

    private void UnsubscribeFromAssetEvents()
    {
        if (_assetMoveDestroy != null)
        {
            _assetMoveDestroy.OnHit -= HandleAssetHit;
        }

        if (_assetMove != null)
        {
            _assetMove.OnHit -= HandleAssetHit;
        }
    }

    /// <summary>
    /// 에셋 스크립트의 충돌 이벤트 핸들러
    /// </summary>
    private void HandleAssetHit(RaycastHit hit)
    {
        if (hit.collider == null) return;

        _hitCount++;

        // ITargetable 인터페이스로 타겟 찾기
        var targetable = hit.collider.GetComponent<ITargetable>();
        if (targetable != null && targetable.IsAlive())
        {
            ApplyDamage(targetable, hit.point);
            OnHitTarget?.Invoke(targetable, hit.point);
        }
    }

    /// <summary>
    /// 타겟에 데미지 적용
    /// </summary>
    private void ApplyDamage(ITargetable target, Vector3 hitPoint)
    {
        if (skillData == null)
        {
            Debug.LogWarning($"[SkillEffectWrapper] skillData is null for skill {skillId}");
            return;
        }

        // 데미지 계산
        float finalDamage = DamageCalculator.CalculateFinalDamage(
            skillData,
            levelData: null, // TODO: 레벨 데이터 연동
            supportData: null, // TODO: 서포트 데이터 연동
            hasMarkEffect: target.HasFocusMark(),
            pierceOrChainCount: _hitCount - 1
        );

        // 데미지 배율 적용
        finalDamage *= damageMultiplier;

        // 데미지 적용
        target.TakeDamage(finalDamage);

        // 디버그 로그
#if UNITY_EDITOR
        Debug.Log($"[SkillEffectWrapper] Skill {skillId} hit {target.GetTransform().name} for {finalDamage:F1} damage (hit #{_hitCount})");
#endif
    }

    #endregion

    #region Effect Lifecycle

    /// <summary>
    /// 이펙트 완료 처리 (풀 반환)
    /// </summary>
    public void CompleteEffect()
    {
        if (!_isActive) return;

        _isActive = false;
        OnEffectComplete?.Invoke(this);
    }

    /// <summary>
    /// 수동으로 이펙트 중지
    /// </summary>
    public void StopEffect()
    {
        CompleteEffect();
    }

    #endregion

    #region IPoolable Implementation

    public void OnSpawn()
    {
        _isActive = true;
        _spawnTime = Time.time;
        _hitCount = 0;

        // 자식 오브젝트들 활성화
        gameObject.SetActive(true);

        // 에셋 스크립트 다시 찾기 (프리팹 구조에 따라)
        if (_assetMoveDestroy == null)
            _assetMoveDestroy = GetComponentInChildren<ObjectMoveDestroy>();
        if (_assetMove == null)
            _assetMove = GetComponentInChildren<ObjectMove>();

        SubscribeToAssetEvents();
    }

    public void OnDespawn()
    {
        _isActive = false;

        // 이벤트 클리어
        OnHitTarget = null;
        OnEffectComplete = null;

        // 상태 초기화
        skillId = 0;
        skillData = null;
        target = null;
        damageMultiplier = 1f;
        _hitCount = 0;

        UnsubscribeFromAssetEvents();

        gameObject.SetActive(false);
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// SkillEffectDatabase에서 프리팹을 가져와 래퍼 생성
    /// </summary>
    public static SkillEffectWrapper CreateFromDatabase(int skillId, Vector3 position, Quaternion rotation)
    {
        var database = SkillEffectDatabase.Instance;
        if (database == null)
        {
            Debug.LogError("[SkillEffectWrapper] SkillEffectDatabase not found");
            return null;
        }

        var entry = database.GetEntry(skillId);
        if (entry == null || entry.mainEffectPrefab == null)
        {
            Debug.LogWarning($"[SkillEffectWrapper] No effect found for skill {skillId}");
            return null;
        }

        // 래퍼 프리팹이 있으면 사용, 없으면 동적 생성
        GameObject instance;
        if (!string.IsNullOrEmpty(entry.wrapperPrefabPath))
        {
            // TODO: Addressables 로드
            instance = Instantiate(entry.mainEffectPrefab, position, rotation);
        }
        else
        {
            // 동적으로 래퍼 생성
            instance = new GameObject($"SkillEffect_{skillId}");
            instance.transform.position = position;
            instance.transform.rotation = rotation;

            // 에셋 프리팹을 자식으로 추가
            var effectInstance = Instantiate(entry.mainEffectPrefab, instance.transform);
            effectInstance.transform.localPosition = Vector3.zero;
            effectInstance.transform.localRotation = Quaternion.identity;
        }

        // 래퍼 컴포넌트 추가/가져오기
        var wrapper = instance.GetComponent<SkillEffectWrapper>();
        if (wrapper == null)
        {
            wrapper = instance.AddComponent<SkillEffectWrapper>();
        }

        // CSV에서 스킬 데이터 로드
        MainSkillData skillData = null;
        if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            skillData = CSVLoader.Instance.GetData<MainSkillData>(skillId);
        }

        // 초기화
        wrapper.Initialize(
            skillId,
            skillData,
            target: null,
            scale: database.GetEffectScale(skillId),
            damageMultiplier: 1f
        );

        return wrapper;
    }

    #endregion
}
