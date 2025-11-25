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
    public bool IsWallHit => isWallHit;

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
        Debug.Log($"BossMonster took {damage} damage. current Health: {currentHealth - damage}");
        base.TakeDamage(damage);
    }

    /// <summary>
    /// CC 효과 적용 (Support 스킬용)
    /// </summary>
    public void ApplyCC(CCType ccType, float duration, float slowAmount, GameObject ccEffectPrefab = null)
    {
        // CC 이펙트 생성 (몬스터를 따라다니면서 재생)
        if (ccEffectPrefab != null)
        {
            GameObject ccEffect = Instantiate(ccEffectPrefab, transform.position, Quaternion.identity, transform);
            Destroy(ccEffect, duration);
            Debug.Log($"[BossMonster] CC effect spawned: {ccEffectPrefab.name}");
        }

        switch (ccType)
        {
            case CCType.Stun:
            case CCType.Freeze:
                Debug.Log($"[BossMonster] {ccType} applied for {duration}s");
                // TODO: Boss CC 효과 구현
                break;

            case CCType.Slow:
                Debug.Log($"[BossMonster] Slow applied: {slowAmount}% for {duration}s");
                break;

            case CCType.Root:
                Debug.Log($"[BossMonster] Root applied for {duration}s");
                break;

            case CCType.Knockback:
                Debug.Log($"[BossMonster] Knockback applied");
                break;

            case CCType.Silence:
                Debug.Log($"[BossMonster] Silence applied for {duration}s");
                break;
        }
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

        // TODO: Mark 효과 구현
        if (markEffectPrefab != null)
        {
            GameObject markEffect = Instantiate(markEffectPrefab, transform.position, Quaternion.identity, transform);
            Destroy(markEffect, duration);
        }
    }

    public override void Die()
    {
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

        isWallHit = false;
        wall = null;
        attackTimer = 0f;
        Weight = 5f;

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
}
