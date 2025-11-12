using NovelianMagicLibraryDefense.Managers;
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
    private async void Start()
    {
        // LMJ: Changed from ObjectPoolManager.Instance to GameManager.Instance.Pool
        await GameManager.Instance.Pool.CreatePoolAsync<Projectile>(AddressableKey.Projectile, defaultCapacity: 5, maxSize: 20);
        GameManager.Instance.Pool.WarmUp<Projectile>(20);
    }
    private void Update()
    {
        if (currentTarget == null || !currentTarget.IsAlive())
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                currentTarget = TargetRegistry.Instance.FindSkillTarget(transform.position, attackRange);
            }
            else
            {
                currentTarget = TargetRegistry.Instance.FindTarget(transform.position, attackRange);
            }
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
        // LMJ: Changed from ObjectPoolManager.Instance to GameManager.Instance.Pool
        GameManager.Instance.Pool.Spawn<Projectile>(transform.position).SetTarget(target.GetTransform());
    }
}
