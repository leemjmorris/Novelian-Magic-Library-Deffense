using NovelianMagicLibraryDefense.Events;
using UnityEngine;

//JML: Monster entity with movement and wall attack behavior
public class Monster : BaseEntity, ITargetable, IMovable
{
    [Header("Event Channels")]
    [SerializeField] private MonsterEvents monsterEvents;

    [Header("Monster Animator")]
    [SerializeField] private Animator monsterAnimator;
    [Header("References")]
    [SerializeField] private MonsterMove monsterMove;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider collider3D;
    private Wall wall;

    [Header("Stats")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackInterval = 0.7f;
    [SerializeField] private float attackRange = 2f; // 공격 범위 (Wall과의 거리)
    [SerializeField] private float fallOffThreshold = -10f;

    public int Exp { get; private set; } = 11; // JML: Example exp amount

    // 테스트용 무적 모드
    private bool isInvincible = false;

    /// <summary>
    /// 테스트용 무적 모드 설정
    /// </summary>
    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        Debug.Log($"[Monster] 무적 모드: {isInvincible}");
    }

    /// <summary>
    /// CSV 데이터 기반으로 몬스터 스탯 초기화 (MonsterLevelData)
    /// OnSpawn() 후 WaveManager에서 호출
    /// </summary>
    public void Initialize(MonsterLevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogWarning("[Monster] MonsterLevelData is null, using default stats");
            return;
        }

        // BaseEntity의 maxHealth 설정
        SetMaxHealth(levelData.HP);

        // Monster 스탯 설정
        damage = levelData.ATK;
        moveSpeed = levelData.Move_Speed;
        attackInterval = 1f / levelData.Attack_Speed; // Attack_Speed는 초당 공격 횟수
        Exp = levelData.Exp_Value;
    }

    private float attackTimer = 0f;
    private bool isWallHit = false; // 물리 충돌 백업용
    private bool isInAttackRange = false; // 공격 범위 내 진입 여부
    public bool IsWallHit => isInAttackRange || isWallHit; // 둘 중 하나라도 true면 정지
    private bool isDead = false;
    private bool isDizzy = false;
    private float dizzyTimer = 0f;
    public float Weight { get; private set; } = 1f;

    [Header("Weight System")]
    [SerializeField, Tooltip("Distance thresholds for weight calculation (closer to wall = higher weight)")]
    private float[] weightThresholds = { 10f, 5f, 3f, 1f };

    private const float WEIGHT_UPDATE_INTERVAL = 0.5f;
    private const string WALL_TAG = "Wall";

    // Static cache for Wall reference (shared across all Monster instances)
    private static Transform cachedWallTransform;
    private static Collider cachedWallCollider;
    private static Wall cachedWall;

    private Transform wallTransform;
    private Collider wallCollider; // Wall Collider 참조 (ClosestPoint 계산용)
    private System.Threading.CancellationTokenSource weightUpdateCts;

    // JML: 키 기반 풀링용 - 이 몬스터가 스폰된 Addressable 키
    private string spawnedAddressableKey;

    /// <summary>
    /// JML: 스폰 시 WaveManager에서 호출하여 Addressable 키 설정
    /// DespawnByKey에서 사용됨
    /// </summary>
    public void SetAddressableKey(string key)
    {
        spawnedAddressableKey = key;
    }

    // Mark state tracking
    private MarkType currentMarkType = MarkType.None;
    private float markDamageMultiplier = 0f;
    private float markEndTime = 0f; // Time.time when mark expires
    private System.Threading.CancellationTokenSource markCts;

    // CC state tracking
    private bool isSlowed = false;
    private float slowMultiplier = 1f; // 1.0 = normal speed, 0.5 = 50% speed
    private System.Threading.CancellationTokenSource slowCts;

    private bool isRooted = false;
    private System.Threading.CancellationTokenSource rootCts;

    // 애니메이터 파라미터 해시 (성능 최적화)
    private static readonly int ANIM_IS_MOVING = Animator.StringToHash("IsMoving");
    private static readonly int ANIM_ATTACK = Animator.StringToHash("Attack");
    private static readonly int ANIM_GET_HIT = Animator.StringToHash("GetHit");
    private static readonly int ANIM_DIE = Animator.StringToHash("Die");
    private static readonly int ANIM_DIZZY = Animator.StringToHash("Dizzy");
    private static readonly int ANIM_VICTORY = Animator.StringToHash("Victory");


    private void OnEnable()
    {
        collider3D.enabled = true;
    }
    private void OnDisable()
    {
        collider3D.enabled = false;
    }

    //JML: Physics-based movement in FixedUpdate
    private void FixedUpdate()
    {
        // Don't move if dead, dizzy, or rooted
        if (isDead || isDizzy || isRooted) return;

        // Apply slow multiplier to movement speed
        float effectiveSpeed = isSlowed ? moveSpeed * slowMultiplier : moveSpeed;
        monsterMove.Move(this, effectiveSpeed);
    }

    //JML: Game logic in Update
    private void Update()
    {
        if (isDead) return;

        // 맵 밖으로 떨어진 경우 despawn
        if (transform.position.y < fallOffThreshold)
        {
            Debug.LogWarning($"[Monster] Fell off map at {transform.position}, despawning");
            Die();
            return;
        }

        // Dizzy 상태 처리
        if (isDizzy)
        {
            dizzyTimer -= Time.deltaTime;
            if (dizzyTimer <= 0f)
            {
                isDizzy = false;
                monsterAnimator.SetBool(ANIM_DIZZY, false);

                // Re-enable NavMeshAgent after dizzy ends
                if (monsterMove != null)
                {
                    monsterMove.SetEnabled(true);
                }
            }
            return;
        }

        // 공격 범위 체크 (거리 기반)
        CheckAttackRange();

        // 벽 공격 처리 (공격 범위 내이거나 물리 충돌 시)
        if ((isInAttackRange || isWallHit) && wall != null)
        {
            monsterAnimator.SetBool(ANIM_IS_MOVING, false);

            attackTimer += Time.deltaTime;
            if (attackInterval <= attackTimer)
            {
                wall.TakeDamage(damage);
                monsterAnimator.SetTrigger(ANIM_ATTACK);
                attackTimer = 0f;
                
            }
        }
        else if (!isDizzy)
        {
            // 공격 범위 밖이면 이동 상태
            monsterAnimator.SetBool(ANIM_IS_MOVING, true);
        }
    }

    public override void TakeDamage(float damage)
    {
        if (isDead) return;

        // Apply Mark damage multiplier if active
        float finalDamage = damage;
        bool isCritical = false;
        if (currentMarkType != MarkType.None && Time.time < markEndTime)
        {
            finalDamage = damage * (1f + markDamageMultiplier / 100f);
            isCritical = true; // Mark amplified damage shows as critical
            // JML: Mark damage 로그 제거
        }

        // LMJ: Show floating damage text (무적 상태에서도 표시)
        if (NovelianMagicLibraryDefense.Managers.DamageTextManager.Instance != null)
        {
            Vector3 textPosition = collider3D != null ? collider3D.bounds.center : transform.position;
            NovelianMagicLibraryDefense.Managers.DamageTextManager.Instance.ShowDamage(textPosition, finalDamage, isCritical);
        }

        // 무적 모드일 때는 데미지 텍스트만 표시하고 실제 체력 감소는 스킵 (테스트용)
        if (isInvincible)
        {
            Debug.Log($"[Monster] 무적 모드 - 데미지 표시만: {finalDamage:F1}");
            return;
        }

        base.TakeDamage(finalDamage);

        // Dizzy 상태에서는 피격 애니메이션 재생하지 않음
        if (!isDizzy)
        {
            monsterAnimator.SetTrigger(ANIM_GET_HIT);
        }
    }

    /// <summary>
    /// CC(Crowd Control) 스킬에 맞았을 때 호출
    /// </summary>
    public void ApplyDizzy(float duration)
    {
        if (isDead) return;

        isDizzy = true;
        dizzyTimer = duration;
        monsterAnimator.SetBool(ANIM_DIZZY, true);

        // Dizzy 상태에서는 NavMeshAgent 비활성화
        if (monsterMove != null)
        {
            monsterMove.SetEnabled(false);
        }

        // Legacy Rigidbody support (if exists and not kinematic)
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Slow 효과 적용 - 이동 속도 감소
    /// </summary>
    public void ApplySlow(float slowPercent, float duration)
    {
        if (isDead) return;

        // Cancel previous slow if exists
        slowCts?.Cancel();
        slowCts?.Dispose();
        slowCts = new System.Threading.CancellationTokenSource();

        isSlowed = true;
        slowMultiplier = 1f - (slowPercent / 100f); // 50% slow = 0.5 multiplier

        // Start slow duration
        SlowDurationAsync(duration, slowCts.Token).Forget();
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid SlowDurationAsync(float duration, System.Threading.CancellationToken ct)
    {
        try
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(duration * 1000), cancellationToken: ct);

            if (!ct.IsCancellationRequested)
            {
                isSlowed = false;
                slowMultiplier = 1f;
            }
        }
        catch (System.OperationCanceledException)
        {
            // Expected when cancelled
        }
    }

    /// <summary>
    /// Root 효과 적용 - 이동 불가 (공격은 가능)
    /// </summary>
    public void ApplyRoot(float duration)
    {
        if (isDead) return;

        // Cancel previous root if exists
        rootCts?.Cancel();
        rootCts?.Dispose();
        rootCts = new System.Threading.CancellationTokenSource();

        isRooted = true;

        // Stop NavMeshAgent but don't disable it completely
        if (monsterMove != null)
        {
            monsterMove.SetEnabled(false);
        }

        // Start root duration
        RootDurationAsync(duration, rootCts.Token).Forget();
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid RootDurationAsync(float duration, System.Threading.CancellationToken ct)
    {
        try
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(duration * 1000), cancellationToken: ct);

            if (!ct.IsCancellationRequested)
            {
                isRooted = false;

                // Re-enable NavMeshAgent if not dead or dizzy
                if (!isDead && !isDizzy && monsterMove != null)
                {
                    monsterMove.SetEnabled(true);
                }
            }
        }
        catch (System.OperationCanceledException)
        {
            // Expected when cancelled
        }
    }

    /// <summary>
    /// Knockback 효과 적용 - 공격자 반대 방향으로 밀려남
    /// </summary>
    public void ApplyKnockback(Vector3 sourcePosition, float force)
    {
        if (isDead) return;

        // Calculate knockback direction (away from source)
        Vector3 knockbackDirection = (transform.position - sourcePosition).normalized;
        knockbackDirection.y = 0f; // Keep on same Y level

        // Temporarily disable NavMeshAgent for knockback
        if (monsterMove != null)
        {
            monsterMove.SetEnabled(false);
        }

        // Apply knockback force using Rigidbody
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(knockbackDirection * force, ForceMode.Impulse);
        }

        // Re-enable NavMeshAgent after short delay
        KnockbackRecoveryAsync().Forget();
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid KnockbackRecoveryAsync()
    {
        // Wait for knockback physics to settle
        await Cysharp.Threading.Tasks.UniTask.Delay(500); // 0.5 second recovery

        if (!isDead && !isDizzy && !isRooted)
        {
            // Reset Rigidbody
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Re-enable NavMeshAgent
            if (monsterMove != null)
            {
                monsterMove.SetEnabled(true);
            }
        }
    }

    /// <summary>
    /// CC 효과 적용 (Support 스킬용)
    /// </summary>
    public void ApplyCC(CCType ccType, float duration, float slowAmount, GameObject ccEffectPrefab = null)
    {
        if (isDead) return;

        // CC 이펙트 생성 (몬스터를 따라다니면서 재생)
        if (ccEffectPrefab != null)
        {
            GameObject ccEffect = Instantiate(ccEffectPrefab, transform.position, Quaternion.identity, transform);
            Destroy(ccEffect, duration);
        }

        switch (ccType)
        {
            case CCType.Stun:
            case CCType.Freeze:
                ApplyDizzy(duration);
                break;

            case CCType.Slow:
                ApplySlow(slowAmount, duration);
                break;

            case CCType.Root:
                ApplyRoot(duration);
                break;

            case CCType.Knockback:
                ApplyKnockback(transform.position + transform.forward, 5f);
                break;

            case CCType.Silence:
                // TODO: Silence 효과 구현 (스킬 사용 불가)
                break;

            case CCType.None:
                break;
        }
    }

    /// <summary>
    /// DOT 효과 적용 (Support 스킬용)
    /// </summary>
    public void ApplyDOT(DOTType dotType, float damagePerTick, float tickInterval, float duration, GameObject dotEffectPrefab = null)
    {
        if (isDead) return;

        // Start DOT (틱마다 이펙트 재생)
        StartDOT(dotType, damagePerTick, tickInterval, duration, dotEffectPrefab).Forget();
        // DOT 이펙트 생성 (몬스터를 따라다니면서 재생)
        if (dotEffectPrefab != null)
        {
            GameObject dotEffect = Instantiate(dotEffectPrefab, transform.position, Quaternion.identity, transform);
            Destroy(dotEffect, duration);
        }

        // Start DOT coroutine
        StartDOT(dotType, damagePerTick, tickInterval, duration, dotEffectPrefab).Forget();
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid StartDOT(DOTType dotType, float damagePerTick, float tickInterval, float duration, GameObject dotEffectPrefab)
    {
        float elapsed = 0f;

        while (elapsed < duration && !isDead)
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(tickInterval * 1000));
            if (isDead) break;

            elapsed += tickInterval;
            TakeDamage(damagePerTick);

            // 틱마다 히트 이펙트 재생
            if (dotEffectPrefab != null)
            {
                GameObject tickEffect = Instantiate(dotEffectPrefab, transform.position, Quaternion.identity);
                Destroy(tickEffect, 0.5f);
            }

            Debug.Log($"[Monster] {dotType} tick: {damagePerTick} damage");
        }
    }

    /// <summary>
    /// Mark 효과 적용 (Support 스킬용)
    /// </summary>
    public void ApplyMark(MarkType markType, float duration, float damageMultiplier, GameObject markEffectPrefab)
    {
        if (isDead) return;

        // Cancel previous mark if exists
        markCts?.Cancel();
        markCts?.Dispose();
        markCts = new System.Threading.CancellationTokenSource();

        // Set mark state
        currentMarkType = markType;
        markDamageMultiplier = damageMultiplier;
        markEndTime = Time.time + duration; // Track when mark expires

        // Spawn mark effect above monster's head (follows monster)
        if (markEffectPrefab != null)
        {
            // Calculate position above monster's head
            float monsterHeight = collider3D != null ? collider3D.bounds.extents.y * 2f : 2f;
            Vector3 markOffset = Vector3.up * (monsterHeight + 0.5f);

            GameObject markEffect = Instantiate(markEffectPrefab, transform.position + markOffset, Quaternion.identity, transform);
            markEffect.transform.localPosition = Vector3.up * (monsterHeight + 0.5f);

            Destroy(markEffect, duration);
        }

        // Start mark duration
        StartMark(duration, markCts.Token).Forget();
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid StartMark(float duration, System.Threading.CancellationToken ct)
    {
        try
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(duration * 1000), cancellationToken: ct);

            if (!ct.IsCancellationRequested)
            {
                // Clear mark state
                currentMarkType = MarkType.None;
                markDamageMultiplier = 0f;
            }
        }
        catch (System.OperationCanceledException)
        {
            // Expected when cancelled
        }
    }

    // Debuff state tracking
    private DeBuffType currentDebuffType = DeBuffType.None;
    private float debuffValue = 0f;
    private float originalDamage;
    private float originalMoveSpeed;
    private System.Threading.CancellationTokenSource debuffCts;
    private GameObject currentDebuffEffect; // 현재 재생 중인 디버프 이펙트

    /// <summary>
    /// 디버프 효과 적용
    /// ATK_Damage_Down: 공격력 감소
    /// ATK_Speed_Down: 이동속도/공격속도 감소
    /// Take_Damage_UP: 받는 피해 증가 (mark처럼 작동)
    /// </summary>
    public void ApplyDebuff(DeBuffType debuffType, float value, float duration, GameObject debuffEffectPrefab = null)
    {
        if (isDead) return;

        // Cancel previous debuff if exists
        debuffCts?.Cancel();
        debuffCts?.Dispose();
        debuffCts = new System.Threading.CancellationTokenSource();

        // Store original values on first debuff
        if (currentDebuffType == DeBuffType.None)
        {
            originalDamage = damage;
            originalMoveSpeed = moveSpeed;
        }

        currentDebuffType = debuffType;
        debuffValue = value;

        // Apply debuff effect based on type
        switch (debuffType)
        {
            case DeBuffType.ATK_Damage_Down:
                damage = originalDamage * (1f - value / 100f);
                break;

            case DeBuffType.ATK_Speed_Down:
                moveSpeed = originalMoveSpeed * (1f - value / 100f);
                break;

            case DeBuffType.Take_Damage_UP:
                // 받는 피해 증가는 별도로 TakeDamage에서 처리 (markDamageMultiplier와 유사하게)
                markDamageMultiplier += value / 100f;
                break;
        }

        // 기존 디버프 이펙트 정리
        if (currentDebuffEffect != null)
        {
            Destroy(currentDebuffEffect);
            currentDebuffEffect = null;
        }

        // Start debuff duration with looping effect
        StartDebuffWithEffect(duration, debuffEffectPrefab, debuffCts.Token).Forget();
    }

    /// <summary>
    /// 디버프 지속시간 동안 이펙트를 반복 재생하며 디버프 상태 유지
    /// </summary>
    private async Cysharp.Threading.Tasks.UniTaskVoid StartDebuffWithEffect(float duration, GameObject effectPrefab, System.Threading.CancellationToken ct)
    {
        const float EFFECT_INTERVAL = 1.5f; // 이펙트 반복 주기 (초)
        float elapsed = 0f;

        try
        {
            // 이펙트가 있으면 첫 번째 이펙트 생성
            if (effectPrefab != null)
            {
                currentDebuffEffect = Instantiate(effectPrefab, transform.position + Vector3.up, Quaternion.identity, transform);
            }

            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                await Cysharp.Threading.Tasks.UniTask.Delay((int)(EFFECT_INTERVAL * 1000), cancellationToken: ct);
                elapsed += EFFECT_INTERVAL;

                // 지속시간 내에 있고 이펙트 프리팹이 있으면 이펙트 갱신
                if (elapsed < duration && effectPrefab != null && !ct.IsCancellationRequested)
                {
                    // 기존 이펙트 제거 후 새로 생성 (루프 이펙트 효과)
                    if (currentDebuffEffect != null)
                    {
                        Destroy(currentDebuffEffect);
                    }
                    currentDebuffEffect = Instantiate(effectPrefab, transform.position + Vector3.up, Quaternion.identity, transform);
                }
            }

            if (!ct.IsCancellationRequested)
            {
                // 디버프 종료 - 원래 값 복원
                EndDebuff();
            }
        }
        catch (System.OperationCanceledException)
        {
            // Expected when cancelled
        }
        finally
        {
            // 이펙트 정리
            if (currentDebuffEffect != null)
            {
                Destroy(currentDebuffEffect);
                currentDebuffEffect = null;
            }
        }
    }

    /// <summary>
    /// 디버프 종료 처리 - 원래 값 복원
    /// </summary>
    private void EndDebuff()
    {
        switch (currentDebuffType)
        {
            case DeBuffType.ATK_Damage_Down:
                damage = originalDamage;
                break;

            case DeBuffType.ATK_Speed_Down:
                moveSpeed = originalMoveSpeed;
                break;

            case DeBuffType.Take_Damage_UP:
                markDamageMultiplier -= debuffValue / 100f;
                break;
        }

        currentDebuffType = DeBuffType.None;
        debuffValue = 0f;
        Debug.Log($"[Monster] Debuff ended");
    }

    // 기존 StartDebuff는 하위 호환성을 위해 유지 (다른 곳에서 사용할 수 있음)
    private async Cysharp.Threading.Tasks.UniTaskVoid StartDebuff(float duration, System.Threading.CancellationToken ct)
    {
        try
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(duration * 1000), cancellationToken: ct);

            if (!ct.IsCancellationRequested)
            {
                EndDebuff();
                currentDebuffType = DeBuffType.None;
                debuffValue = 0f;
            }
        }
        catch (System.OperationCanceledException)
        {
            // Expected when cancelled
        }
    }

    /// <summary>
    /// Check if this monster has a Focus Mark (for focus targeting)
    /// </summary>
    public bool HasFocusMark()
    {
        return currentMarkType == MarkType.Focus && !isDead;
    }

    /// <summary>
    /// Get remaining mark duration in seconds (for priority targeting)
    /// </summary>
    public float GetMarkRemainingTime()
    {
        if (currentMarkType == MarkType.None || isDead)
        {
            return float.MaxValue; // No mark or dead = lowest priority
        }

        float remaining = markEndTime - Time.time;
        return Mathf.Max(0f, remaining); // Never return negative
    }

    /// <summary>
    /// 승리 애니메이션 재생 (게임 승리 시 호출)
    /// </summary>
    public void PlayVictory()
    {
        monsterAnimator.SetTrigger(ANIM_VICTORY);
    }

    /// <summary>
    /// Face toward Wall on spawn (instant rotation)
    /// </summary>
    private void FaceTowardWall()
    {
        // Use cached Wall reference
        Transform targetWall = cachedWallTransform;

        if (targetWall == null)
        {
            // Fallback: try to find Wall
            GameObject wallObj = GameObject.FindWithTag(WALL_TAG);
            if (wallObj != null)
            {
                targetWall = wallObj.transform;
            }
        }

        if (targetWall != null)
        {
            // Calculate direction to Wall (horizontal only)
            Vector3 directionToWall = (targetWall.position - transform.position);
            directionToWall.y = 0f; // Keep rotation horizontal

            if (directionToWall.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(directionToWall.normalized);
            }
        }
    }

    /// <summary>
    /// Initialize static Wall cache (called once by WaveManager at game start)
    /// </summary>
    public static void InitializeWallCache(Transform wallTransform, Collider wallCollider, Wall wall)
    {
        cachedWallTransform = wallTransform;
        cachedWallCollider = wallCollider;
        cachedWall = wall;
        Debug.Log("[Monster] Wall cache initialized via WaveManager");
    }

    /// <summary>
    /// Clear static Wall cache (called when scene changes)
    /// </summary>
    public static void ClearWallCache()
    {
        cachedWallTransform = null;
        cachedWallCollider = null;
        cachedWall = null;
    }

    /// <summary>
    /// 외부에서 목적지 설정
    /// Wall Collider의 가장 가까운 지점을 자동 계산하여 자연스럽게 분산
    /// </summary>
    public void SetDestination(Vector3 destination)
    {
        if (monsterMove == null) return;

        // Use cached Wall reference (fallback to FindWithTag only if cache is empty)
        if (wallCollider == null)
        {
            if (cachedWallCollider != null)
            {
                wallCollider = cachedWallCollider;
                wallTransform = cachedWallTransform;
                wall = cachedWall;
            }
            else
            {
                // Fallback: FindWithTag only once per scene if cache not initialized
                GameObject wallObj = GameObject.FindWithTag(WALL_TAG);
                if (wallObj != null)
                {
                    wallCollider = wallObj.GetComponent<Collider>();
                    wallTransform = wallObj.transform;
                    wall = wallObj.GetComponent<Wall>();

                    // Update static cache for other monsters
                    cachedWallCollider = wallCollider;
                    cachedWallTransform = wallTransform;
                    cachedWall = wall;
                    Debug.Log("[Monster] Wall cache initialized via FindWithTag fallback");
                }
            }
        }

        // Wall Collider가 있으면 가장 가까운 지점 계산
        if (wallCollider != null)
        {
            // 몬스터의 현재 위치에서 Wall Collider의 가장 가까운 지점 계산
            Vector3 closestPoint = wallCollider.ClosestPoint(transform.position);
            monsterMove.SetDestination(closestPoint);
        }
        else
        {
            // Fallback: Wall Collider가 없으면 전달받은 위치 사용
            monsterMove.SetDestination(destination);
        }
    }

    //LMJ : Override to return collider center instead of transform position
    public new Vector3 GetPosition()
    {
        return collider3D != null ? collider3D.bounds.center : transform.position;
    }

    /// <summary>
    /// 공격 범위 체크 (Wall Collider의 가장 가까운 지점까지의 거리 기반)
    /// </summary>
    private void CheckAttackRange()
    {
        // Use cached Wall reference
        if (wallCollider == null)
        {
            if (cachedWallCollider != null)
            {
                wallCollider = cachedWallCollider;
                wallTransform = cachedWallTransform;
                wall = cachedWall;
            }
            else
            {
                // Fallback: FindWithTag only if cache not initialized
                GameObject wallObj = GameObject.FindWithTag(WALL_TAG);
                if (wallObj != null)
                {
                    wallCollider = wallObj.GetComponent<Collider>();
                    wallTransform = wallObj.transform;
                    wall = wallObj.GetComponent<Wall>();

                    // Update static cache
                    cachedWallCollider = wallCollider;
                    cachedWallTransform = wallTransform;
                    cachedWall = wall;
                }
            }
        }

        if (wallCollider == null) return;

        // Wall Collider의 가장 가까운 지점까지의 거리 계산
        Vector3 closestPoint = wallCollider.ClosestPoint(transform.position);
        float distanceToWall = Vector3.Distance(transform.position, closestPoint);

        // 공격 범위 내 진입 체크
        bool wasInRange = isInAttackRange;
        isInAttackRange = distanceToWall <= attackRange;

        // 공격 범위 진입 시 NavMeshAgent 정지
        if (isInAttackRange && !wasInRange)
        {
            if (monsterMove != null)
            {
                monsterMove.SetEnabled(false);
            }
        }
        // 공격 범위 이탈 시 NavMeshAgent 재활성화 (Dizzy나 Dead가 아닐 때)
        else if (!isInAttackRange && wasInRange && !isDizzy && !isDead)
        {
            if (monsterMove != null)
            {
                monsterMove.SetEnabled(true);
            }
        }
    }

    //LMJ : Start weight update loop
    private void StartWeightUpdate()
    {
        // Use cached Wall reference
        if (wallTransform == null)
        {
            if (cachedWallTransform != null)
            {
                wallTransform = cachedWallTransform;
            }
            else
            {
                // Fallback: FindWithTag only if cache not initialized
                GameObject wallObj = GameObject.FindWithTag(WALL_TAG);
                if (wallObj != null)
                {
                    wallTransform = wallObj.transform;
                    cachedWallTransform = wallTransform;
                }
            }
        }

        // Cancel previous CTS before creating new one
        weightUpdateCts?.Cancel();
        weightUpdateCts?.Dispose();
        weightUpdateCts = new System.Threading.CancellationTokenSource();
        UpdateWeightLoopAsync(weightUpdateCts.Token).Forget();
    }

    //LMJ : Update weight based on distance to wall
    private async Cysharp.Threading.Tasks.UniTaskVoid UpdateWeightLoopAsync(System.Threading.CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            UpdateWeight();
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(WEIGHT_UPDATE_INTERVAL * 1000), cancellationToken: ct);
        }
    }

    //LMJ : Calculate weight based on wall distance
    private void UpdateWeight()
    {
        if (wallTransform == null || isDead) return;

        float distanceToWall = Vector3.Distance(transform.position, wallTransform.position);

        // Calculate weight: 벽에 가까울수록 가중치 증가
        float newWeight = 1f;
        foreach (float threshold in weightThresholds)
        {
            if (distanceToWall < threshold)
            {
                newWeight += 1f;
            }
        }

        Weight = newWeight;
    }

    public override void Die()
    {
        // Prevent double Die() calls
        if (isDead) return;
        isDead = true;

        monsterAnimator.SetTrigger(ANIM_DIE);
        // LMJ: Don't disable collider immediately - let projectiles still hit during death animation
        // collider3D.enabled = false; // Moved to DespawnMonster()

        // JML: Unregister BEFORE despawning to prevent accessing destroyed object
        TargetRegistry.Instance.UnregisterTarget(this);

        // LMJ: Use EventChannel instead of static event
        if (monsterEvents != null)
        {
            monsterEvents.RaiseMonsterDied(this);
        }
        Invoke(nameof(DespawnMonster), 1.5f); // Die 애니메이션이 끝날 때까지 대기
    }

    private bool isDespawning = false;

    private void DespawnMonster()
    {
        // Prevent double Despawn calls
        if (isDespawning) return;
        isDespawning = true;

        // LMJ: Disable collider before despawning
        if (collider3D != null)
        {
            collider3D.enabled = false;
        }

        // JML: 키 기반 풀링 사용 시 DespawnByKey 호출
        if (!string.IsNullOrEmpty(spawnedAddressableKey))
        {
            NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool.DespawnByKey(spawnedAddressableKey, this);
        }
        else
        {
            // 기존 타입 기반 풀링 (하위 호환)
            NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool.Despawn(this);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(Tag.Wall))
        {
            wall = collision.gameObject.GetComponent<Wall>();
            isWallHit = true;

            // Wall에 닿으면 NavMeshAgent 비활성화하여 밀림 방지
            if (monsterMove != null)
            {
                monsterMove.SetEnabled(false);
            }

            // Rigidbody velocity 초기화
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Wall과 충돌 중일 때 뒤로 밀리지 않도록 처리
        if (collision.gameObject.CompareTag(Tag.Wall) && isWallHit)
        {
            // Rigidbody velocity를 0으로 설정하여 밀림 방지
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(Tag.Wall))
        {
            isWallHit = false;
            wall = null;

            // Wall에서 떨어지면 NavMeshAgent 다시 활성화 (Dizzy 상태가 아닐 때만)
            if (monsterMove != null && !isDizzy && !isDead)
            {
                monsterMove.SetEnabled(true);
            }
        }
    }

    public override void OnSpawn()
    {
        base.OnSpawn(); // Initialize health
        isDead = false;
        isDespawning = false;
        isWallHit = false;
        isInAttackRange = false;
        isDizzy = false;
        dizzyTimer = 0f;
        wall = null;
        wallTransform = null;
        wallCollider = null; // Wall 참조 초기화
        attackTimer = 0f;
        Weight = 1f;

        // Reset CC states
        isSlowed = false;
        slowMultiplier = 1f;
        isRooted = false;

        // Reset Mark state
        currentMarkType = MarkType.None;
        markDamageMultiplier = 0f;

        // 애니메이션 상태 초기화
        if (monsterAnimator != null)
        {
            monsterAnimator.SetBool(ANIM_IS_MOVING, true);
            monsterAnimator.SetBool(ANIM_DIZZY, false);
            monsterAnimator.ResetTrigger(ANIM_ATTACK);
            monsterAnimator.ResetTrigger(ANIM_GET_HIT);
            monsterAnimator.ResetTrigger(ANIM_DIE);
            monsterAnimator.ResetTrigger(ANIM_VICTORY);
        }

        // 목적지는 WaveManager에서 SetDestination()으로 설정됨

        // NavMesh에 Warp (위치 동기화)
        if (monsterMove != null)
        {
            monsterMove.WarpToPosition(transform.position);
        }

        // Face toward Wall on spawn
        FaceTowardWall();

        //LMJ : Start weight update system
        StartWeightUpdate();

        TargetRegistry.Instance.RegisterTarget(this);
    }

    public override void OnDespawn()
    {
        isWallHit = false;
        isInAttackRange = false;
        wall = null;
        wallTransform = null;
        wallCollider = null;
        attackTimer = 0f;
        Weight = 1f;
        CancelInvoke(nameof(DespawnMonster));

        //LMJ : Stop weight update system
        weightUpdateCts?.Cancel();

        // MonsterMove 상태 초기화
        if (monsterMove != null)
        {
            monsterMove.ResetState();
        }

        // JML: Redundant safety check - should already be unregistered in Die()
        // But kept as failsafe for edge cases
        TargetRegistry.Instance.UnregisterTarget(this);
    }

    //LMJ : Cleanup cancellation token on destroy
    private void OnDestroy()
    {
        weightUpdateCts?.Cancel();
        weightUpdateCts?.Dispose();

        markCts?.Cancel();
        markCts?.Dispose();

        slowCts?.Cancel();
        slowCts?.Dispose();

        rootCts?.Cancel();
        rootCts?.Dispose();
    }
}
