using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.Managers;
using Novelian.Combat;
using UnityEngine;
//JML: Boss monster entity with enhanced stats and wall attack behavior
public class BossMonster : BaseEntity, ITargetable, IMovable
{
    [Header("Event Channels")]
    [SerializeField] private MonsterEvents monsterEvents;

    [Header("Boss Dungeon (Issue #476)")]
    [SerializeField] private BossDungeonManager bossDungeonManager;

    [Header("References")]
    [SerializeField] private Collider collider3D;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private MonsterMove monsterMove;

    [Header("Stats")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackInterval = 0.7f;
    [SerializeField] private float fallOffThreshold = -10f;
    [SerializeField] private float attackRange = 2f; // 공격 범위 (Wall과의 거리)

    private float attackTimer = 0f;
    private Wall wall;
    private bool isWallHit = false; // 물리 충돌 백업용
    private bool isInAttackRange = false; // 공격 범위 내 진입 여부 (Monster.cs와 동일)
    private bool isDead = false;

    // Wall 참조 (거리 기반 공격 범위 체크용)
    private Collider targetWallCollider;

    public bool IsWallHit => isInAttackRange || isWallHit; // 둘 중 하나라도 true면 정지

    /// <summary>
    /// JML: Override IsAlive to include isDead flag check.
    /// This ensures dead bosses are not targeted during death animation.
    /// </summary>
    public override bool IsAlive()
    {
        if (isDead) return false;
        return base.IsAlive();
    }

    // Mark state tracking
    private MarkType currentMarkType = MarkType.None;
    private float markDamageMultiplier = 0f;
    private float markEndTime = 0f; // Time.time when mark expires
    private System.Threading.CancellationTokenSource markCts;

    // Issue #476: 스턴 상태 (도전던전용)
    private bool isStunned = false;
    private float originalMoveSpeedForStun;
    private System.Threading.CancellationTokenSource stunCts;

    // JML: ITargetable implementation
    public float Weight { get; private set; } = 5f; // Example weight value

    // 상성 시스템용 Genre
    private Genre bossGenre = Genre.Horror; // 기본값: Horror

    /// <summary>
    /// 보스 장르 반환 (상성 계산용)
    /// </summary>
    public Genre GetGenre() => bossGenre;

    /// <summary>
    /// Issue #476: BossDungeonManager 참조 설정 (스폰 시 주입)
    /// </summary>
    public void SetBossDungeonManager(BossDungeonManager manager)
    {
        bossDungeonManager = manager;
    }

    /// <summary>
    /// Issue #476: Wall 참조 설정 (거리 기반 공격 범위 체크용)
    /// BossDungeonManager에서 호출
    /// </summary>
    public void SetWallTarget(Wall wallTarget, Collider wallCollider)
    {
        wall = wallTarget;
        targetWallCollider = wallCollider;
    }

    /// <summary>
    /// Issue #476: 런타임에 AddComponent로 추가될 때 참조 자동 설정
    /// 기존 프리팹의 컴포넌트들을 자동으로 찾아서 연결
    /// </summary>
    public void InitializeReferences()
    {
        if (collider3D == null) collider3D = GetComponent<Collider>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (monsterMove == null) monsterMove = GetComponent<MonsterMove>();

    }

    /// <summary>
    /// CSV 데이터 기반으로 보스 스탯 초기화
    /// </summary>
    /// <param name="levelData">몬스터 레벨 데이터</param>
    /// <param name="monsterId">몬스터 ID (Genre 조회용)</param>
    /// <param name="events">MonsterEvents (옵션)</param>
    public void Initialize(MonsterLevelData levelData, int monsterId = 0, MonsterEvents events = null)
    {
        // MonsterEvents 주입
        if (events != null)
        {
            monsterEvents = events;
        }

        // Monster ID로 Genre 조회 (상성 시스템)
        if (monsterId > 0 && CSVLoader.Instance != null)
        {
            var monsterData = CSVLoader.Instance.GetData<MonsterData>(monsterId);
            if (monsterData != null)
            {
                bossGenre = monsterData.Genre;
            }
        }

        if (levelData == null)
        {
            Debug.LogWarning("[BossMonster] MonsterLevelData is null, using default stats");
            return;
        }

        // BaseEntity의 maxHealth 설정
        SetMaxHealth(levelData.HP);

        // Boss 스탯 설정
        damage = levelData.ATK;
        moveSpeed = levelData.Move_Speed;
        attackInterval = 1f / levelData.Attack_Speed;
    }
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
        // Wall에 닿으면 이동 중지 (IsWallHit = isInAttackRange || isWallHit)
        if (IsWallHit)
        {
            return;
        }

        // Wall의 ClosestPoint 방향으로 계속 조준 (원형 Wall 대응)
        if (targetWallCollider != null && monsterMove != null)
        {
            Vector3 closestPoint = targetWallCollider.ClosestPoint(transform.position);
            monsterMove.UpdateDirection(closestPoint);
        }

        // 디버그: 이동 상태 확인
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"[BossMonster] 이동 중: moveSpeed={moveSpeed}, monsterMove={monsterMove != null}, targetWallCollider={targetWallCollider != null}, rb={rb != null}");
        }

        if (monsterMove != null)
        {
            monsterMove.Move(this, moveSpeed);
        }
        else
        {
            Debug.LogError("[BossMonster] monsterMove가 null입니다!");
        }
    }

    //JML: Game logic in Update
    private void Update()
    {
        if (isDead) return;

        // 맵 밖으로 떨어진 경우 despawn
        if (transform.position.y < fallOffThreshold)
        {
            Debug.LogWarning($"[BossMonster] Fell off map at {transform.position}, despawning");
            Die();
            return;
        }

        // Issue #476: 스턴 중이면 공격하지 않음
        if (isStunned) return;

        // 공격 범위 체크 (거리 기반) - Monster.cs와 동일한 방식
        CheckAttackRange();

        // 벽 공격 처리 (공격 범위 내이거나 물리 충돌 시)
        // targetWallCollider 사용 (wall은 OnCollisionExit에서 null 될 수 있음)
        bool canAttack = (isInAttackRange || isWallHit) && targetWallCollider != null;
        if (canAttack)
        {
            attackTimer += Time.deltaTime;

            // Issue #476: 공격 카운트다운 UI 업데이트
            float remainingSeconds = attackInterval - attackTimer;
            if (bossDungeonManager != null)
            {
                bossDungeonManager.UpdateAttackCountdown(remainingSeconds);
            }

            if (attackInterval <= attackTimer)
            {
                // Issue #476: 도전던전에서는 스턴 게이지 증가 (결계 공격)
                if (bossDungeonManager != null)
                {
                    bossDungeonManager.OnBossAttackWall();
                }

                attackTimer = 0f;
            }
        }
        else
        {
            // 공격 범위 밖이면 카운트다운 숨김
            if (bossDungeonManager != null)
            {
                bossDungeonManager.UpdateAttackCountdown(0f);
            }
        }
        // Weight는 고정값 사용 (매 프레임 증가 제거)
    }

    /// <summary>
    /// 공격 범위 체크 (Wall Collider 기반 - Monster.cs와 동일한 방식)
    /// </summary>
    private void CheckAttackRange()
    {
        if (targetWallCollider == null) return;

        // Wall Collider의 가장 가까운 지점까지의 거리 계산
        Vector3 closestPoint = targetWallCollider.ClosestPoint(transform.position);
        float distanceToWall = Vector3.Distance(transform.position, closestPoint);

        // 공격 범위 내 진입 체크
        bool wasInRange = isInAttackRange;
        isInAttackRange = distanceToWall <= attackRange;

        // 공격 범위 진입 시 이동 정지
        if (isInAttackRange && !wasInRange)
        {
            if (monsterMove != null)
            {
                monsterMove.SetEnabled(false);
            }
        }
        // 공격 범위 이탈 시 이동 재활성화 (Dead나 Stunned가 아닐 때)
        else if (!isInAttackRange && wasInRange && !isDead && !isStunned)
        {
            // Issue #476: 거리 기반 체크가 물리 충돌보다 우선
            // 넉백으로 밀려났을 때 물리 충돌이 아직 남아있어도 이동 재개
            isWallHit = false;

            if (monsterMove != null)
            {
                monsterMove.SetEnabled(true);
                // 목적지 재설정 (넉백 후 다시 Wall로 이동)
                if (targetWallCollider != null)
                {
                    monsterMove.SetDestination(targetWallCollider.transform.position);
                }
            }
        }
    }

    public override void TakeDamage(float damage)
    {
        TakeDamage(damage, false);
    }

    /// <summary>
    /// 데미지 처리 (치명타 여부 포함)
    /// </summary>
    /// <param name="damage">데미지량</param>
    /// <param name="isCriticalHit">치명타 여부 (Projectile에서 전달)</param>
    public void TakeDamage(float damage, bool isCriticalHit)
    {
        if (isDead) return;

        // Apply Mark damage multiplier if active
        float finalDamage = damage;
        bool isCritical = isCriticalHit; // 외부에서 전달받은 크리티컬 여부
        if (currentMarkType != MarkType.None && Time.time < markEndTime)
        {
            finalDamage = damage * (1f + markDamageMultiplier / 100f);
            isCritical = true; // Mark amplified damage도 크리티컬로 표시
        }

        // LMJ: Show floating damage text
        if (NovelianMagicLibraryDefense.Managers.DamageTextManager.Instance != null)
        {
            Vector3 textPosition = collider3D != null ? collider3D.bounds.center : transform.position;
            NovelianMagicLibraryDefense.Managers.DamageTextManager.Instance.ShowDamage(textPosition, finalDamage, isCritical);
        }

        // Issue #476: 스턴 게이지는 보스가 결계를 공격할 때만 증가 (캐릭터 공격 시 아님)

        base.TakeDamage(finalDamage);
    }

    /// <summary>
    /// 상성 시스템 적용 데미지 처리 (Issue #531)
    /// 공격자 장르에 따라 상성 배율 적용 후 데미지 처리
    /// </summary>
    /// <param name="damage">기본 데미지</param>
    /// <param name="isCriticalHit">치명타 여부</param>
    /// <param name="attackerGenre">공격자 장르</param>
    public void TakeDamage(float damage, bool isCriticalHit, Genre attackerGenre)
    {
        float multiplier = AffinityCalculator.GetMultiplier(attackerGenre, bossGenre);
        float affinityDamage = damage * multiplier;
        TakeDamage(affinityDamage, isCriticalHit);
    }

    /// <summary>
    /// CC 효과 적용 (Support 스킬용)
    /// Boss는 CC에 면역 - 이펙트만 표시하고 실제 효과는 적용하지 않음
    /// </summary>
    public void ApplyCC(CCType ccType, float duration, float slowAmount, GameObject ccEffectPrefab = null)
    {
        // Boss is immune to CC - show effect but don't apply actual CC

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

        }
    }

    /// <summary>
    /// Mark 효과 적용 (Support 스킬용)
    /// </summary>
    public void ApplyMark(MarkType markType, float duration, float damageMultiplier, GameObject markEffectPrefab)
    {

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

    #region Issue #476: 스턴 시스템 (도전던전용)

    /// <summary>
    /// 스턴 적용 (이동/공격 중지)
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (isStunned || !IsAlive()) return;

        isStunned = true;
        originalMoveSpeedForStun = moveSpeed;
        moveSpeed = 0f;

        // 스턴 해제 예약
        stunCts?.Cancel();
        stunCts?.Dispose();
        stunCts = new System.Threading.CancellationTokenSource();
        ReleaseStunAfterDelayAsync(duration, stunCts.Token).Forget();
    }

    /// <summary>
    /// 스턴 해제
    /// </summary>
    public void ReleaseStun()
    {
        if (!isStunned) return;

        isStunned = false;
        moveSpeed = originalMoveSpeedForStun;
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid ReleaseStunAfterDelayAsync(float duration, System.Threading.CancellationToken ct)
    {
        try
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(duration * 1000), cancellationToken: ct);
            if (!ct.IsCancellationRequested)
            {
                ReleaseStun();
            }
        }
        catch (System.OperationCanceledException)
        {
            // Expected when cancelled
        }
    }

    /// <summary>
    /// 스턴 상태 확인
    /// </summary>
    public bool IsStunned => isStunned;

    /// <summary>
    /// 공격 주기 설정 (도전던전 전용)
    /// </summary>
    public void SetAttackInterval(float interval)
    {
        attackInterval = interval;
    }

    #endregion

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
        else
        {
            Debug.LogError("[BossMonster] monsterEvents가 null이라 이벤트 발생 불가!");
        }

        // Issue #476: BossDungeon에서 스폰된 경우 풀이 아닌 Destroy 사용
        // (Monster 타입 풀로 스폰 후 BossMonster 컴포넌트 추가했으므로 Despawn<BossMonster> 불가)
        if (bossDungeonManager != null)
        {
            Destroy(gameObject);
        }
        else
        {
            // 기존 스테이지 시스템: 풀로 반환
            NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool.Despawn(this);
        }
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
            wall = collision.gameObject.GetComponent<Wall>();
            isWallHit = true;

            // Wall에 닿으면 이동 비활성화
            if (monsterMove != null)
            {
                monsterMove.SetEnabled(false);
            }

            // Rigidbody velocity 초기화
            if (rb != null)
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
            if (rb != null)
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

            // Wall에서 떨어지면 이동 다시 활성화 (Dead나 Stunned가 아닐 때)
            if (monsterMove != null && !isDead && !isStunned)
            {
                monsterMove.SetEnabled(true);
            }
        }
    }

    public override void OnSpawn()
    {
        base.OnSpawn(); // Initialize health
        isDead = false;

        isWallHit = false;
        isInAttackRange = false; // 거리 기반 체크 초기화
        wall = null;
        targetWallCollider = null;
        attackTimer = 0f;
        Weight = 5f;

        // Reset Mark state
        currentMarkType = MarkType.None;
        markDamageMultiplier = 0f;

        // Issue #476: Reset Stun state
        isStunned = false;
        stunCts?.Cancel();
        stunCts?.Dispose();
        stunCts = null;

        // Rigidbody 초기 상태 설정 (물리 기반 이동)
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.mass = 20f; // 보스는 더 무거움 - 밀림 감소
            rb.linearDamping = 5f; // 저항 추가 - 밀림 후 빠른 정지
            rb.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 이동
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // MonsterMove 초기화 (Rigidbody + Wall 방향)
        // targetWallCollider가 설정되어 있으면 해당 위치로, 없으면 전방으로
        if (monsterMove != null && rb != null)
        {
            Vector3 targetPos = targetWallCollider != null ? targetWallCollider.transform.position : transform.position + transform.forward * 10f;
            monsterMove.Initialize(rb, targetPos);
        }

        TargetRegistry.Instance.RegisterTarget(this);
    }

    public override void OnDespawn()
    {
        isWallHit = false;
        isInAttackRange = false;
        wall = null;
        targetWallCollider = null;
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

        // Issue #476: 스턴 CTS 정리
        stunCts?.Cancel();
        stunCts?.Dispose();
    }
}
