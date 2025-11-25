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
    [SerializeField] private float fallOffThreshold = -10f;

    public int Exp { get; private set; } = 11; // JML: Example exp amount

    private float attackTimer = 0f;
    private bool isWallHit = false;
    public bool IsWallHit => isWallHit;
    private bool isDead = false;
    private bool isDizzy = false;
    private float dizzyTimer = 0f;
    public float Weight { get; private set; } = 1f;

    [Header("Weight System")]
    [SerializeField, Tooltip("Distance thresholds for weight calculation (closer to wall = higher weight)")]
    private float[] weightThresholds = { 10f, 5f, 3f, 1f };

    private const float WEIGHT_UPDATE_INTERVAL = 0.5f;
    private const string WALL_TAG = "Wall";

    private Transform wallTransform;
    private System.Threading.CancellationTokenSource weightUpdateCts;

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
        if (isDead || isDizzy) return;
        monsterMove.Move(this, moveSpeed);
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

        // 벽 공격 처리 (Dizzy 상태가 아닐 때만)
        if (isWallHit && wall != null)
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
            // 벽에서 떨어지면 다시 이동 상태로 (Dizzy 상태가 아닐 때만)
            monsterAnimator.SetBool(ANIM_IS_MOVING, true);
        }
    }

    public override void TakeDamage(float damage)
    {
        if (isDead) return;

        base.TakeDamage(damage);

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

        // Legacy Rigidbody support (if exists)
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// CC 효과 적용 (Support 스킬용)
    /// </summary>
    public void ApplyCC(CCType ccType, float duration, float slowAmount, GameObject ccEffectPrefab = null)
    {
        if (isDead)
        {
            Debug.Log("[Monster] ApplyCC called but monster is dead");
            return;
        }

        Debug.Log($"[Monster] ApplyCC called: Type={ccType}, Duration={duration}s");

        // CC 이펙트 생성 (몬스터를 따라다니면서 재생)
        if (ccEffectPrefab != null)
        {
            GameObject ccEffect = Instantiate(ccEffectPrefab, transform.position, Quaternion.identity, transform);
            Destroy(ccEffect, duration);
            Debug.Log($"[Monster] CC effect spawned: {ccEffectPrefab.name}");
        }

        switch (ccType)
        {
            case CCType.Stun:
            case CCType.Freeze:
                Debug.Log($"[Monster] Applying {ccType} → ApplyDizzy({duration})");
                ApplyDizzy(duration);
                break;

            case CCType.Slow:
                // TODO: Slow 효과 구현 (moveSpeed 감소)
                Debug.Log($"[Monster] Slow applied: {slowAmount}% for {duration}s");
                break;

            case CCType.Root:
                // TODO: Root 효과 구현 (이동 불가, 공격 가능)
                Debug.Log($"[Monster] Root applied for {duration}s");
                break;

            case CCType.Knockback:
                // TODO: Knockback 효과 구현
                Debug.Log($"[Monster] Knockback applied");
                break;

            case CCType.Silence:
                // TODO: Silence 효과 구현 (스킬 사용 불가)
                Debug.Log($"[Monster] Silence applied for {duration}s");
                break;

            case CCType.None:
                Debug.Log("[Monster] CCType is None");
                break;
        }
    }

    /// <summary>
    /// DOT 효과 적용 (Support 스킬용)
    /// </summary>
    public void ApplyDOT(DOTType dotType, float damagePerTick, float tickInterval, float duration, GameObject dotEffectPrefab = null)
    {
        if (isDead) return;

        // DOT 이펙트 생성 (몬스터를 따라다니면서 재생)
        if (dotEffectPrefab != null)
        {
            GameObject dotEffect = Instantiate(dotEffectPrefab, transform.position, Quaternion.identity, transform);
            Destroy(dotEffect, duration);
            Debug.Log($"[Monster] DOT effect spawned: {dotEffectPrefab.name}");
        }

        // Start DOT coroutine
        StartDOT(dotType, damagePerTick, tickInterval, duration).Forget();
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid StartDOT(DOTType dotType, float damagePerTick, float tickInterval, float duration)
    {
        float elapsed = 0f;
        Debug.Log($"[Monster] {dotType} DOT started: {damagePerTick} dmg every {tickInterval}s for {duration}s");

        while (elapsed < duration && !isDead)
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(tickInterval * 1000));
            if (isDead) break;

            elapsed += tickInterval;
            TakeDamage(damagePerTick);
            Debug.Log($"[Monster] {dotType} tick: {damagePerTick} damage");
        }

        Debug.Log($"[Monster] {dotType} DOT ended");
    }

    /// <summary>
    /// Mark 효과 적용 (Support 스킬용)
    /// </summary>
    public void ApplyMark(MarkType markType, float duration, float damageMultiplier, GameObject markEffectPrefab)
    {
        if (isDead) return;

        Debug.Log($"[Monster] {markType} Mark applied: +{damageMultiplier}% damage for {duration}s");

        // TODO: Mark 효과 구현
        // 1. Mark 비주얼 이펙트 생성 (markEffectPrefab)
        // 2. Mark가 있는 동안 받는 데미지 증가
        // 3. duration 후 Mark 제거

        if (markEffectPrefab != null)
        {
            GameObject markEffect = Instantiate(markEffectPrefab, transform.position, Quaternion.identity, transform);
            Destroy(markEffect, duration);
        }
    }

    /// <summary>
    /// 승리 애니메이션 재생 (게임 승리 시 호출)
    /// </summary>
    public void PlayVictory()
    {
        monsterAnimator.SetTrigger(ANIM_VICTORY);
    }

    /// <summary>
    /// 외부에서 목적지 설정
    /// </summary>
    public void SetDestination(Vector3 destination)
    {
        if (monsterMove != null)
        {
            monsterMove.SetDestination(destination);
        }
    }

    //LMJ : Override to return collider center instead of transform position
    public new Vector3 GetPosition()
    {
        return collider3D != null ? collider3D.bounds.center : transform.position;
    }

    //LMJ : Start weight update loop
    private void StartWeightUpdate()
    {
        // Find wall if not assigned
        if (wallTransform == null)
        {
            GameObject wallObj = GameObject.FindWithTag(WALL_TAG);
            if (wallObj != null)
            {
                wallTransform = wallObj.transform;
            }
        }

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
    private void DespawnMonster()
    {
        // LMJ: Disable collider before despawning
        if (collider3D != null)
        {
            collider3D.enabled = false;
        }
        NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool.Despawn(this);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(Tag.Wall))
        {
            wall = collision.gameObject.GetComponent<Wall>();
            isWallHit = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(Tag.Wall))
        {
            isWallHit = false;
            wall = null;
        }
    }

    public override void OnSpawn()
    {
        base.OnSpawn(); // Initialize health
        isDead = false;
        isWallHit = false;
        isDizzy = false;
        dizzyTimer = 0f;
        wall = null;
        attackTimer = 0f;
        Weight = 1f;

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

        //LMJ : Start weight update system
        StartWeightUpdate();

        TargetRegistry.Instance.RegisterTarget(this);
    }

    public override void OnDespawn()
    {
        isWallHit = false;
        wall = null;
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
    }
}
