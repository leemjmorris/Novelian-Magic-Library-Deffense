using NovelianMagicLibraryDefense.Events;
using UnityEngine;

public class Wall : MonoBehaviour, IEntity
{
    [Header("Event Channels")]
    [SerializeField] private WallEvents wallEvents;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 200f;
    private float health;

    [Header("Shield Settings (Issue #424)")]
    [SerializeField] private float maxShield = 100f;
    private float shield = 0f;

    private void Awake()
    {
        health = maxHealth;
        shield = 0f; // 쉴드는 0에서 시작, 카드로 추가
    }

    private void Start()
    {
        // LMJ: Raise initial health after all managers are initialized
        if (wallEvents != null)
        {
            wallEvents.RaiseHealthChanged(health, maxHealth);
            wallEvents.RaiseShieldChanged(shield, maxShield);
        }
    }

    // IEntity Implementation
    // JML: Shield 먼저 소진 후 HP 감소 (Issue #424)
    public void TakeDamage(float damage)
    {
        float remainingDamage = damage;

        // 1. Shield가 있으면 먼저 소진
        if (shield > 0)
        {
            if (shield >= remainingDamage)
            {
                // Shield가 충분히 많으면 전부 흡수
                shield -= remainingDamage;
                remainingDamage = 0;
                Debug.Log($"[Wall] Shield absorbed {damage} damage. Shield: {shield}/{maxShield}");
            }
            else
            {
                // Shield가 부족하면 남은 데미지는 HP로
                remainingDamage -= shield;
                Debug.Log($"[Wall] Shield depleted! Absorbed {shield}, remaining damage: {remainingDamage}");
                shield = 0;
            }

            if (wallEvents != null)
            {
                wallEvents.RaiseShieldChanged(shield, maxShield);
            }
        }

        // 2. 남은 데미지는 HP에 적용
        if (remainingDamage > 0)
        {
            health -= remainingDamage;
            // Debug.Log($"[Wall] HP damaged: -{remainingDamage}. HP: {health}/{maxHealth}");

            if (wallEvents != null)
            {
                wallEvents.RaiseHealthChanged(health, maxHealth);
            }
        }

        if (health <= 0)
        {
            Die();
        }
    }

    public float GetHealth() => health;
    public float GetMaxHealth() => maxHealth;
    public float GetShield() => shield;
    public float GetMaxShield() => maxShield;
    public bool IsAlive() => health > 0;
    public Vector3 GetPosition() => transform.position;
    public Transform GetTransform() => transform;
    public WallEvents GetWallEvents() => wallEvents;

    #region Shield System (Issue #424)

    /// <summary>
    /// JML: 결계 내구도(Shield) 추가 (결계 내구도 증가 카드 선택 시 호출)
    /// </summary>
    public void AddShield(float amount)
    {
        if (amount <= 0 || !IsAlive()) return;

        float previousShield = shield;
        shield = Mathf.Min(shield + amount, maxShield);
        float actualShield = shield - previousShield;

        Debug.Log($"[Wall] Shield 추가: +{actualShield} ({previousShield} → {shield}/{maxShield})");

        if (wallEvents != null)
        {
            wallEvents.RaiseShieldChanged(shield, maxShield);
        }
    }

    /// <summary>
    /// JML: 최대 쉴드량의 비율(%)로 Shield 추가
    /// </summary>
    public void AddShieldByPercent(float percent)
    {
        if (percent <= 0 || !IsAlive()) return;
        AddShield(maxShield * percent);
    }

    /// <summary>
    /// JML: 최대 쉴드량 설정 (CSV 데이터 기반)
    /// </summary>
    public void SetMaxShield(float max)
    {
        maxShield = max;
        Debug.Log($"[Wall] MaxShield set to {maxShield}");
    }

    #endregion

    /// <summary>
    /// Wall 체력 회복 (체력 회복 카드 선택 시 호출)
    /// </summary>
    public void Heal(float amount)
    {
        if (amount <= 0 || !IsAlive()) return;

        float previousHealth = health;
        health = Mathf.Min(health + amount, maxHealth);
        float actualHeal = health - previousHealth;

        Debug.Log($"[Wall] 체력 회복: +{actualHeal} ({previousHealth} → {health}/{maxHealth})");

        if (wallEvents != null)
        {
            wallEvents.RaiseHealthChanged(health, maxHealth);
        }
    }

    /// <summary>
    /// Wall 체력을 비율(%)로 회복
    /// </summary>
    public void HealByPercent(float percent)
    {
        if (percent <= 0 || !IsAlive()) return;
        Heal(maxHealth * percent);
    }

    /// <summary>
    /// CSV 데이터 기반으로 Wall의 최대 체력 설정 (StageData.Barrier_HP)
    /// Start() 전에 호출되어야 함
    /// </summary>
    public void SetMaxHealth(float hp)
    {
        maxHealth = hp;
        health = maxHealth;
        Debug.Log($"[Wall] MaxHealth set to {maxHealth} from CSV");
    }

    public void Die()
    {
        GameOver();
    }

    private void GameOver()
    {
        // Debug.Log("Game Over!");

        // LMJ: Use EventChannel instead of static event
        if (wallEvents != null)
        {
            wallEvents.RaiseWallDestroyed();
        }
    }
}
