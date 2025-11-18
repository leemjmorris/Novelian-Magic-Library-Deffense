using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

public class Character : MonoBehaviour, IPoolable
{
    [Header("Character Animator")]
    [SerializeField] private Animator characterAnimator;
    [Header("Character Obj")]
    [SerializeField] private GameObject characterObj;
    [Header("Prefab References")]
    [SerializeField] private GameObject projectilePrefab;  // JML: Legacy - will be replaced by SkillConfig

    [Header("Skill Configuration")]
    [SerializeField] private SkillConfig skillConfig;  // JML: CSV-based skill configuration

    [Header("Targeting")]
    [SerializeField] private Transform target;

    [Header("Character Attributes")]
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private float attackRange = 1000.0f;  // UI-based character has infinite range

    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Image characterImage;

    private ITargetable currentTarget;
    private float timer = 0.0f;

    private void Start()
    {
        // LMJ: Skip initialization if Pool manager doesn't exist (e.g., in LobbyScene)
        if (GameManager.Instance == null || GameManager.Instance.Pool == null)
        {
            // Debug.Log("[Character] Pool manager not available, skipping projectile pool initialization");
            return;
        }

        // JML: Create pool using direct prefab reference from SkillConfig
        if (skillConfig != null && skillConfig.hasProjectile && skillConfig.projectilePrefab != null)
        {
            if (!GameManager.Instance.Pool.HasPool<Projectile>())
            {
                GameManager.Instance.Pool.CreatePool<Projectile>(skillConfig.projectilePrefab, defaultCapacity: 5, maxSize: 20);
                GameManager.Instance.Pool.WarmUp<Projectile>(20);
            }
        }
    }
    private void Update()
    {
        if (currentTarget == null || !currentTarget.IsAlive())
        {
            currentTarget = TargetRegistry.Instance.FindTarget(transform.position, attackRange);
            if (currentTarget != null)
            {
                Debug.Log($"[Character] Found target at position: {currentTarget.GetPosition()}");
            }
            else
            {
                Debug.Log($"[Character] No target found. Character position: {transform.position}, AttackRange: {attackRange}");
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
        if (!characterObj.activeSelf)
            return;

        // JML: Ensure Z position is 0 for 2D
        Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y, 0f);
        var projectile = GameManager.Instance.Pool.Spawn<Projectile>(spawnPosition);

        // JML: Initialize projectile with SkillConfig values if available
        if (skillConfig != null && skillConfig.hasProjectile)
        {
            projectile.Initialize(skillConfig.projectileSpeed, skillConfig.projectileDuration);
        }

        projectile.SetTarget(target.GetTransform());
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
