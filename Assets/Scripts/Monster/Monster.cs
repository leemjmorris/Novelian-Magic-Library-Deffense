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
    public float Weight { get; private set; } = 1f; // Example weight value

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

        // 벽 공격 처리
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
        else
        {
            // 벽에서 떨어지면 다시 이동 상태로
            monsterAnimator.SetBool(ANIM_IS_MOVING, true);
        }
    }

    public override void TakeDamage(float damage)
    {
        if (isDead) return;

        base.TakeDamage(damage);
        monsterAnimator.SetTrigger(ANIM_GET_HIT);
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

    public override void Die()
    {
        isDead = true;
        monsterAnimator.SetTrigger(ANIM_DIE);
        collider3D.enabled = false;

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

        TargetRegistry.Instance.RegisterTarget(this);
    }

    public override void OnDespawn()
    {
        isWallHit = false;
        wall = null;
        attackTimer = 0f;
        Weight = 1f;
        CancelInvoke(nameof(DespawnMonster));

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
