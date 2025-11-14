using UnityEngine;
//JML: Boss monster entity with enhanced stats and wall attack behavior
public class BossMonster : BaseEntity, ITargetable, IMovable
{
    public static event System.Action<BossMonster> OnBossDied;
    [SerializeField] new Collider2D collider2D;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackInterval = 0.7f;
    [SerializeField] private MonsterMove monsterMove;

    private float attackTimer = 0f;
    private Wall wall;
    private bool isWallHit = false;
    public bool IsWallHit => isWallHit;

    // JML: ITargetable implementation
    public float Weight { get; private set; } = 5f; // Example weight value
    //--------------------------------
    private void OnEnable()
    {
        collider2D.enabled = true;
    }
    private void OnDisable()
    {
        collider2D.enabled = false;
    }

    void Start()
    {
        float randomX = Random.Range(-0.4f, 0.4f);
        transform.position = new Vector3(randomX, 2, -7.5f);
    }

    //JML: Physics-based movement in FixedUpdate
    private void FixedUpdate()
    {
        monsterMove.Move(this, moveSpeed);
    }

    //JML: Game logic in Update
    private void Update()
    {
        if (isWallHit)
        {
            attackTimer += Time.deltaTime;
            if (attackInterval <= attackTimer)
            {
                wall.TakeDamage(damage);
                attackTimer = 0f;
            }
        }
        Weight += 1f;
    }

    public override void TakeDamage(float damage)
    {
        Debug.Log($"BossMonster took {damage} damage. current Health: {currentHealth - damage}");
        base.TakeDamage(damage);
    }

    public override void Die()
    {
        OnBossDied?.Invoke(this);
        // LMJ: Changed from ObjectPoolManager.Instance to GameManager.Instance.Pool
        NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool.Despawn(this);
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(Tag.Wall))
        {
            Debug.Log("Monster hit the wall.");
            wall = collision.GetComponent<Wall>();
            
            isWallHit = true;
        }
    }

    public override void OnSpawn()
    {
        base.OnSpawn(); // Initialize health

        isWallHit = false;
        wall = null;
        attackTimer = 0f;
        Weight = 5f;

        TargetRegistry.Instance.RegisterTarget(this);

    }

    public override void OnDespawn()
    {
        isWallHit = false;
        wall = null;
        attackTimer = 0f;
        Weight = 5f;
        TargetRegistry.Instance.UnregisterTarget(this);
    }
}
