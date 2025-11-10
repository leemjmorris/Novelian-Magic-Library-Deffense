using Unity.VisualScripting;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Targeting")]
    [SerializeField] private Transform target;

    [Header("Character Attributes")]
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private float attackRange = 5.0f;

    private ITargetable currentTarget;
    private float timer = 0.0f;

    private void Update()
    {
        if (currentTarget == null || !currentTarget.IsAlive())
        {
            currentTarget = TargetRegistry.Instance.FindTarget(transform.position, attackRange);
        }

        if (currentTarget != null)
        {
            timer += Time.deltaTime;
            if (timer >= attackInterval)
            {
                Attack(currentTarget);
                timer = 0.0f;
            }
        }
    }

    private void Attack(ITargetable target)
    {
        GameObject projectileObject = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var projectile = projectileObject.GetComponent<Projectile>();
        projectile.SetTarget(target.GetTransform());
    }
    // private void OnTriggerEnter2D(Collider2D collision)
    // {
    //     if (collision.CompareTag("Monster"))
    //     {
    //         Debug.Log("몬스터 감지");
    //         target = collision.transform;
            
    //     }
    // }
}
