using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

public class Character : MonoBehaviour, IPoolable
{
    [Header("Character Animator")]
    [SerializeField] private Animator characterAnimator;
    [Header("Character Obj")]
    [SerializeField] private GameObject characterObj;
    [Header("Prefab References")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Targeting")]
    [SerializeField] private Transform target;

    [Header("Character Attributes")]
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private float attackRange = 1000.0f;  // UI-based character has infinite range

    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Image characterImage;

    private ITargetable currentTarget;
    private float timer = 0.0f;
    private async void Start()
    {
        // LMJ: Skip initialization if Pool manager doesn't exist (e.g., in LobbyScene)
        if (GameManager.Instance == null || GameManager.Instance.Pool == null)
        {
            // Debug.Log("[Character] Pool manager not available, skipping projectile pool initialization");
            return;
        }

        // LMJ: Only create pool if it doesn't exist yet
        if (!GameManager.Instance.Pool.HasPool<Projectile>())
        {
            await GameManager.Instance.Pool.CreatePoolAsync<Projectile>(AddressableKey.Projectile, defaultCapacity: 5, maxSize: 20);
            GameManager.Instance.Pool.WarmUp<Projectile>(20);
        }
    }
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
        if (!characterObj.activeSelf)
            return;

        Vector3 spawnPosition = transform.position;
        GameManager.Instance.Pool.Spawn<Projectile>(spawnPosition).SetTarget(target.GetTransform());
        characterAnimator.SetTrigger("2_Attack");
    }

    public void OnSpawn()
    {
        currentTarget = null;
        timer = 0.0f;
        characterObj.SetActive(true);
    }

    public void OnDespawn()
    {
        currentTarget = null;
        timer = 0.0f;
        characterObj.SetActive(false);
    }
}
