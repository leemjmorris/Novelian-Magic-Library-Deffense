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
        // Start DOT (틱마다 이펙트 재생)
        StartDOT(dotType, damagePerTick, tickInterval, duration, dotEffectPrefab).Forget();
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid StartDOT(DOTType dotType, float damagePerTick, float tickInterval, float duration, GameObject dotEffectPrefab)
    {
        float elapsed = 0f;
        Debug.Log($"[BossMonster] {dotType} DOT started: {damagePerTick} dmg every {tickInterval}s for {duration}s");

        while (elapsed < duration && IsAlive())
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(tickInterval * 1000));
            if (!IsAlive()) break;

            elapsed += tickInterval;
            TakeDamage(damagePerTick);

            // 틱마다 히트 이펙트 재생
            if (dotEffectPrefab != null)
            {
                GameObject tickEffect = Instantiate(dotEffectPrefab, transform.position, Quaternion.identity);
                Destroy(tickEffect, 0.5f);
            }

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
        Debug.Log($"[BossMonster] {debuffType} Debuff applied: {value}% for {duration}s");

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
                // 받는 피해 증가는 markDamageMultiplier 사용
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
                currentDebuffEffect = Instantiate(effectPrefab, transform.position + Vector3.up * 2f, Quaternion.identity, transform);
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
                    currentDebuffEffect = Instantiate(effectPrefab, transform.position + Vector3.up * 2f, Quaternion.identity, transform);
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
        Debug.Log($"[BossMonster] Debuff ended");
    }

    // 기존 StartDebuff는 하위 호환성을 위해 유지
    private async Cysharp.Threading.Tasks.UniTaskVoid StartDebuff(float duration, System.Threading.CancellationToken ct)
    {
        try
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(duration * 1000), cancellationToken: ct);

            if (!ct.IsCancellationRequested)
            {
                EndDebuff();
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
