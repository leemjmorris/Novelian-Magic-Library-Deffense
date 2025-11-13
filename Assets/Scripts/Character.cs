using NovelianMagicLibraryDefense.Core;
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
        // LMJ: Use ServiceLocator for decoupled manager access
        await ServiceLocator.Get<ObjectPoolManager>().CreatePoolAsync<Projectile>(AddressableKey.Projectile, defaultCapacity: 5, maxSize: 20);
        ServiceLocator.Get<ObjectPoolManager>().WarmUp<Projectile>(20);
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
        // LMJ: Use ServiceLocator for decoupled manager access
        ServiceLocator.Get<ObjectPoolManager>().Spawn<Projectile>(transform.position).SetTarget(target.GetTransform());
    }
}
