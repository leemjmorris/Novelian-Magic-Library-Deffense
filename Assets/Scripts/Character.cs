using NovelianMagicLibraryDefense.Managers;
using NovelianMagicLibraryDefense.Skills;
using UnityEngine;

/// <summary>
/// LMJ: Character component - manages character state and lifecycle
/// Skill execution is now delegated to SkillExecutor component
/// </summary>
public class Character : MonoBehaviour, IPoolable
{
    [Header("Character Visual")]
    [SerializeField] private GameObject characterObj;

    [Header("Skill System")]
    [Tooltip("SkillExecutor handles all skill logic - should be configured in prefab")]
    private SkillExecutor skillExecutor;

    private void Awake()
    {
        // Get existing SkillExecutor (should already be on prefab)
        skillExecutor = GetComponent<SkillExecutor>();
        if (skillExecutor == null)
        {
            Debug.LogWarning("[Character] SkillExecutor not found! Please add it manually in the prefab.");
        }
        else
        {
            Debug.Log("[Character] SkillExecutor found and ready");
        }
    }

    /// <summary>
    /// Called when character is spawned from pool
    /// </summary>
    public void OnSpawn()
    {
        characterObj.SetActive(true);

        // Reset skill executor cooldown
        if (skillExecutor != null)
        {
            skillExecutor.ResetCooldown();
        }

        Debug.Log("[Character] Character spawned and ready");
    }

    /// <summary>
    /// Called when character is returned to pool
    /// </summary>
    public void OnDespawn()
    {
        characterObj.SetActive(false);
        Debug.Log("[Character] Character despawned");
    }

    /// <summary>
    /// Get skill executor (for external access if needed)
    /// </summary>
    public SkillExecutor GetSkillExecutor()
    {
        return skillExecutor;
    }
}
