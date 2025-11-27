using NovelianMagicLibraryDefense.Events;
using UnityEngine;
//JML: Boss monster entity with enhanced stats and wall attack behavior
public class BossMonster : BaseEntity, ITargetable, IMovable
{
    [Header("Event Channels")]
    [SerializeField] private MonsterEvents monsterEvents;

    [Header("References")]
    [SerializeField] private Collider collider3D;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private MonsterMove monsterMove;

    [Header("Stats")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackInterval = 0.7f;
    [SerializeField] private float fallOffThreshold = -10f;

    private float attackTimer = 0f;
    private Wall wall;
    private bool isWallHit = false;
    private bool isDead = false;
    public bool IsWallHit => isWallHit;

    // Mark state tracking
    private MarkType currentMarkType = MarkType.None;
    private float markDamageMultiplier = 0f;
    private float markEndTime = 0f; // Time.time when mark expires
    private System.Threading.CancellationTokenSource markCts;

    // JML: ITargetable implementation
    public float Weight { get; private set; } = 5f; // Example weight value
    //--------------------------------
    private void OnEnable()
    {
        collider3D.enabled = true;
    }
    private void OnDisable()
    {
        collider3D.enabled = false;
    }

    // JML: Removed hardcoded spawn position - now handled by WaveManager via SpawnArea

    //JML: Physics-based movement in FixedUpdate
    private void FixedUpdate()
    {
        monsterMove.Move(this, moveSpeed);
    }

    //JML: Game logic in Update
    private void Update()
    {
        // 맵 밖으로 떨어진 경우 despawn
        if (transform.position.y < fallOffThreshold)
        {
            Debug.LogWarning($"[BossMonster] Fell off map at {transform.position}, despawning");
            Die();
            return;
        }

        if (isWallHit && wall != null)
        {
            attackTimer += Time.deltaTime;
            if (attackInterval <= attackTimer)
            {
                wall.TakeDamage(damage);
                attackTimer = 0f;
            }
        }
        // Weight는 고정값 사용 (매 프레임 증가 제거)
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
            Debug.Log($"[BossMonster] Mark amplified damage: {damage:F1} -> {finalDamage:F1} (+{markDamageMultiplier}%)");
        }

        // LMJ: Show floating damage text
        if (NovelianMagicLibraryDefense.Managers.DamageTextManager.Instance != null)
        {
            Vector3 textPosition = collider3D != null ? collider3D.bounds.center : transform.position;
            NovelianMagicLibraryDefense.Managers.DamageTextManager.Instance.ShowDamage(textPosition, finalDamage, isCritical);
        }

        Debug.Log($"BossMonster took {finalDamage} damage. current Health: {currentHealth - finalDamage}");
        base.TakeDamage(finalDamage);
    }

    /// <summary>
    /// CC 효과 적용 (Support 스킬용)
    /// Boss는 CC에 면역 - 이펙트만 표시하고 실제 효과는 적용하지 않음
    /// </summary>
    public void ApplyCC(CCType ccType, float duration, float slowAmount, GameObject ccEffectPrefab = null)
    {
        // Boss is immune to CC - show effect but don't apply actual CC
        Debug.Log($"[BossMonster] CC IMMUNE: {ccType} blocked (Boss cannot be crowd controlled)");

        // LMJ: Show "IMMUNE" floating text
        if (NovelianMagicLibraryDefense.Managers.DamageTextManager.Instance != null)
        {
            Vector3 textPosition = collider3D != null ? collider3D.bounds.center : transform.position;
            NovelianMagicLibraryDefense.Managers.DamageTextManager.Instance.ShowStatus(textPosition, "IMMUNE", Color.gray);
        }

        // Show CC effect briefly to indicate hit (optional visual feedback)
        if (ccEffectPrefab != null)
        {
            GameObject ccEffect = Instantiate(ccEffectPrefab, transform.position, Quaternion.identity, transform);
            // Destroy effect quickly since CC doesn't apply
            Destroy(ccEffect, 0.5f);
            Debug.Log($"[BossMonster] CC effect shown briefly (immune): {ccEffectPrefab.name}");
        }

        // No actual CC effect is applied to Boss
    }

    /// <summary>
    /// DOT 효과 적용 (Support 스킬용)
    /// </summary>
    public void ApplyDOT(DOTType dotType, float damagePerTick, float tickInterval, float duration, GameObject dotEffectPrefab = null)
    {
        // DOT 이펙트 생성 (몬스터를 따라다니면서 재생)
        if (dotEffectPrefab != null)
        {
            GameObject dotEffect = Instantiate(dotEffectPrefab, transform.position, Quaternion.identity, transform);
            Destroy(dotEffect, duration);
            Debug.Log($"[BossMonster] DOT effect spawned: {dotEffectPrefab.name}");
        }

        // Start DOT coroutine
        StartDOT(dotType, damagePerTick, tickInterval, duration).Forget();
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid StartDOT(DOTType dotType, float damagePerTick, float tickInterval, float duration)
    {
        float elapsed = 0f;
        Debug.Log($"[BossMonster] {dotType} DOT started: {damagePerTick} dmg every {tickInterval}s for {duration}s");

        while (elapsed < duration && IsAlive())
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(tickInterval * 1000));
            if (!IsAlive()) break;

            elapsed += tickInterval;
            TakeDamage(damagePerTick);
            Debug.Log($"[BossMonster] {dotType} tick: {damagePerTick} damage");
        }

        Debug.Log($"[BossMonster] {dotType} DOT ended");
    }

    /// <summary>
    /// Mark 효과 적용 (Support 스킬용)
    /// </summary>
    public void ApplyMark(MarkType markType, float duration, float damageMultiplier, GameObject markEffectPrefab)
    {
        Debug.Log($"[BossMonster] {markType} Mark applied: +{damageMultiplier}% damage for {duration}s");

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
            // Calculate position above boss's head
            float bossHeight = collider3D != null ? collider3D.bounds.extents.y * 2f : 3f; // Use collider height or default 3m (boss is bigger)
            Vector3 markOffset = Vector3.up * (bossHeight + 0.5f); // 0.5m above head

            GameObject markEffect = Instantiate(markEffectPrefab, transform.position + markOffset, Quaternion.identity, transform);

            // Set local position to ensure it follows boss correctly
            markEffect.transform.localPosition = Vector3.up * (bossHeight + 0.5f);

            Destroy(markEffect, duration);
            Debug.Log($"[BossMonster] Mark effect spawned above head: {markEffectPrefab.name}, height offset: {bossHeight + 0.5f}m");
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
                Debug.Log($"[BossMonster] Mark ended");
            }
        }
        catch (System.OperationCanceledException)
        {
            // Expected when cancelled
        }
    }

    /// <summary>
    /// Check if this boss has a Focus Mark (for focus targeting)
    /// </summary>
    public bool HasFocusMark()
    {
        return currentMarkType == MarkType.Focus && IsAlive();
    }

    /// <summary>
    /// Get remaining mark duration in seconds (for priority targeting)
    /// </summary>
    public float GetMarkRemainingTime()
    {
        if (currentMarkType == MarkType.None || !IsAlive())
        {
            return float.MaxValue; // No mark or dead = lowest priority
        }

        float remaining = markEndTime - Time.time;
        return Mathf.Max(0f, remaining); // Never return negative
    }

    public override void Die()
    {
        // Prevent double Die() calls
        if (isDead) return;
        isDead = true;

        // LMJ: Unregister BEFORE despawning to prevent accessing destroyed object
        TargetRegistry.Instance.UnregisterTarget(this);

        // LMJ: Use EventChannel instead of static event
        if (monsterEvents != null)
        {
            monsterEvents.RaiseBossDied(this);
        }

        // LMJ: Changed from ObjectPoolManager.Instance to GameManager.Instance.Pool
        NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool.Despawn(this);
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
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(Tag.Wall))
        {
            Debug.Log("BossMonster hit the wall.");
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
        wall = null;
        attackTimer = 0f;
        Weight = 5f;

        // Reset Mark state
        currentMarkType = MarkType.None;
        markDamageMultiplier = 0f;

        // 목적지는 WaveManager에서 SetDestination()으로 설정됨

        TargetRegistry.Instance.RegisterTarget(this);
    }

    public override void OnDespawn()
    {
        isWallHit = false;
        wall = null;
        attackTimer = 0f;
        Weight = 5f;

        // MonsterMove 상태 초기화
        if (monsterMove != null)
        {
            monsterMove.ResetState();
        }

        // JML: Redundant safety check - should already be unregistered in Die()
        // But kept as failsafe for edge cases
        TargetRegistry.Instance.UnregisterTarget(this);
    }

    private void OnDestroy()
    {
        markCts?.Cancel();
        markCts?.Dispose();
    }
}
