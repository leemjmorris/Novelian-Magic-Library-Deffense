using System.Collections.Generic;
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

    #region Stage Instant Clear (Issue #435)

    [Header("Stage Clear Cheat")]
    [SerializeField] private NovelianMagicLibraryDefense.Managers.WaveManager waveManager;

    /// <summary>
    /// 스테이지 즉시 클리어 (치트)
    /// - 현재 필드의 모든 몬스터를 전멸 처리
    /// - 아직 스폰되지 않은 예정된 몬스터도 처리
    /// - 정상적인 승리 흐름과 동일하게 동작 (Wave 완료 → 승리 이벤트 발생)
    /// StageClearBtn의 OnClick에서 호출
    /// </summary>
    public void InstantStageClear()
    {
        Debug.Log("[CheatManager] === 스테이지 즉시 클리어 시작 ===");

        // 1. 현재 필드의 몬스터 처치
        List<ITargetable> allTargets = TargetRegistry.Instance.GetAllTargets();
        int killCount = 0;

        if (allTargets != null && allTargets.Count > 0)
        {
            // 리스트 복사 후 순회 (Die() 호출 시 원본 리스트가 변경될 수 있음)
            List<ITargetable> targetsCopy = new List<ITargetable>(allTargets);

            int count = targetsCopy.Count;
            for (int i = 0; i < count; i++)
            {
                ITargetable target = targetsCopy[i];
                if (target == null || !target.IsAlive()) continue;

                // Monster로 캐스팅하여 Die() 호출
                if (target is Monster monster)
                {
                    monster.Die();
                    killCount++;
                }
                else if (target is BossMonster boss)
                {
                    boss.Die();
                    killCount++;
                }
            }
        }

        Debug.Log($"[CheatManager] 필드 몬스터 {killCount}마리 처치");

        // 2. WaveManager에서 스폰 루프 중단 + 남은 웨이브 강제 완료
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<NovelianMagicLibraryDefense.Managers.WaveManager>();
        }

        if (waveManager != null)
        {
            waveManager.ForceCompleteAllWaves();
        }
        else
        {
            Debug.LogError("[CheatManager] WaveManager를 찾을 수 없습니다!");
        }

        Debug.Log("[CheatManager] === 스테이지 즉시 클리어 완료 ===");
    }

    #endregion
}
