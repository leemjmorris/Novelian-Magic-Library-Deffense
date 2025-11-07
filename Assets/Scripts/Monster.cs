using UnityEngine;
using UnityEngine.Pool;
public class Monster : MonoBehaviour
{
    public static event System.Action<Monster> OnMonsterDied;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackInterval = 0.7f;
    private float attackTimer = 0f;
    private Wall wall;
    private bool isWallHit = false;

    private float Health { get; set; } = 100f;
    void Start()
    {
        float randomX = Random.Range(-0.4f, 0.4f);
        transform.position = new Vector3(randomX, 2, -7.5f);
    }

    private void Update()
    {
        Move();

        if (isWallHit)
        {
            attackTimer += Time.deltaTime;
            if (attackInterval <= attackTimer)
            {
                wall.TakeDamage(damage);
                attackTimer = 0f;
            }
        }
    }

    private void Move()
    {
        if (!isWallHit)
        {
            transform.Translate(Vector2.down * moveSpeed * Time.deltaTime);
        }
    }

    public void TakeDamage(float damage)
    {
        Health -= damage;
        Debug.Log($"Monster took {damage} damage. current Health: {Health}");
        if (Health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Monster died.");
        OnMonsterDied?.Invoke(this);
        Destroy(gameObject);
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Wall"))
        {
            Debug.Log("벽에 충돌");
            wall = collision.GetComponent<Wall>();
            
            isWallHit = true;
            
        }
    }
}
