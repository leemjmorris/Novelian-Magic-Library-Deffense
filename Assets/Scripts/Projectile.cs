using System;
using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable
{
    [Header("Projectile Attributes")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f; // JML: Lifetime before auto-despawn
    private float spawnTime;

    private Vector3 direction;
    private Transform target;
    private bool hasLostTarget = false; // JML: Track if the target has been lost
    private void Update()
    {
        if (target != null && !hasLostTarget)
        {
            if (!target.gameObject.activeInHierarchy)
            {
                hasLostTarget = true; // JML: Mark that the target has been lost
            }
            else
            {
                direction = (target.position - transform.position).normalized;

            }
        }
        
        if (direction != Vector3.zero)
        {
            transform.Translate(direction * speed * Time.deltaTime);

            // JML: Auto-despawn after lifetime
            spawnTime += Time.deltaTime;
            if (spawnTime >= lifetime)
            {
                ObjectPoolManager.Instance.Despawn(this);
                spawnTime = 0f;
                return;
            }
        }
    }

    public void SetTarget(Transform target)
    {
        if (!hasLostTarget)
        {
            this.target = target;
        }
        
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(Tag.Monster))
        {
            var monster = collision.GetComponent<Monster>();
            if (monster != null)
            {
                monster.TakeDamage(damage);
            }
            ObjectPoolManager.Instance.Despawn(this);
        }
        if (collision.CompareTag(Tag.BossMonster))
        {
            var bossMonster = collision.GetComponent<BossMonster>();
            if (bossMonster != null)
            {
                bossMonster.TakeDamage(damage);
            }
            ObjectPoolManager.Instance.Despawn(this);
        }
    }

    public void OnSpawn()
    {
        hasLostTarget = false;
        target = null;
        direction = Vector3.zero;
    }

    public void OnDespawn()
    {
        hasLostTarget = false;
        target = null;
        direction = Vector3.zero;
    }
}
