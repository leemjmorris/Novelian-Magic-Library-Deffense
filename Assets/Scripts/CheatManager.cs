using UnityEngine;

/// <summary>
/// Cheat manager for debugging and testing
/// OnClick은 Inspector에서 직접 할당
/// </summary>
public class CheatManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterPlacementManager placementManager;

    [Header("Sequential Spawn Settings")]
    private int currentCharacterId = 1; // Current character to spawn (1~5)
    private const int MIN_CHARACTER_ID = 1;
    private const int MAX_CHARACTER_ID = 5;

    /// <summary>
    /// Spawn next character in sequence (01 → 02 → 03 → 04 → 05 → 01...)
    /// This method is called by CheatBtn's OnClick event in Inspector
    /// </summary>
    public void SpawnNextCharacter()
    {
        if (placementManager == null)
        {
            // Try to find CharacterPlacementManager in scene
            placementManager = FindFirstObjectByType<CharacterPlacementManager>();

            if (placementManager == null)
            {
                Debug.LogError("[CheatManager] CharacterPlacementManager not found!");
                return;
            }
        }

        // Check if preload is complete
        if (!placementManager.IsPreloadComplete())
        {
            Debug.LogWarning("[CheatManager] Character prefabs are not preloaded yet! Please wait.");
            return;
        }

        // Spawn current character
        Debug.Log($"[CheatManager] Spawning Character {currentCharacterId:D2}...");
        bool success = placementManager.SpawnCharacterById(currentCharacterId);

        if (success)
        {
            Debug.Log($"[CheatManager] Character {currentCharacterId:D2} spawned successfully");

            // Move to next character (cycle: 1 → 2 → 3 → 4 → 5 → 1...)
            currentCharacterId++;
            if (currentCharacterId > MAX_CHARACTER_ID)
            {
                currentCharacterId = MIN_CHARACTER_ID;
                Debug.Log($"[CheatManager] Reached Character 05, cycling back to Character 01");
            }
        }
        else
        {
            Debug.LogWarning($"[CheatManager] Failed to spawn Character {currentCharacterId:D2} (no empty slots or prefab not loaded)");
            // Don't increment if spawn failed, retry same character next time
        }
    }

    /// <summary>
    /// Reset spawn sequence back to Character 01
    /// </summary>
    public void ResetSpawnSequence()
    {
        currentCharacterId = MIN_CHARACTER_ID;
        Debug.Log("[CheatManager] Spawn sequence reset to Character 01");
    }
}
