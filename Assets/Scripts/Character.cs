using Unity.VisualScripting;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Targeting")]
    [SerializeField] private Transform target;

    [Header("Character Attributes")]
    [SerializeField] private float timer = 0.0f;
    [SerializeField] private float attackInterval = 1.0f;

    private GameObject projectileObject;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= attackInterval)
        {
            if (target == null)
            {
                if (projectileObject != null)
                {
                    Destroy(projectileObject);
                }
                return;
            }
            projectileObject = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var projectile = projectileObject.GetComponent<Projectile>();
            projectile.SetTarget(target);

            timer = 0.0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Monster"))
        {
            Debug.Log("몬스터 감지");
            target = collision.transform;
            
        }
    }
}
