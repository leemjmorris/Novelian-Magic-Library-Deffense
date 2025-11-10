using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
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
        if (collision.CompareTag("Monster"))
        {
            var monster = collision.GetComponent<Monster>();
            if (monster != null)
            {
                monster.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
