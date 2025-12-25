using System.Collections.Generic;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.UI;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// Central manager that controls all other managers lifecycle
    /// All managers are now MonoBehaviour components
    /// Each scene has its own independent GameManager (no DontDestroyOnLoad)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance => instance;

        [Header("Manager References")]
        [SerializeField] private InputManager inputManager;
        [SerializeField] private ObjectPoolManager poolManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private StageManager stageManager;
        [SerializeField] private StageStateManager stageStateManager;

        // Public accessors for type-safe manager access
        public InputManager Input => inputManager;
        public ObjectPoolManager Pool => poolManager;
        public UIManager UI => uiManager;
        public WaveManager Wave => waveManager;
        public StageManager Stage => stageManager;
        public StageStateManager StageState => stageStateManager;

        private void Awake()
        {
            // Simple singleton without DontDestroyOnLoad
            // Each scene has its own independent GameManager
            // 씬 재로드 시 이전 instance가 파괴되었을 수 있으므로 null 체크 추가
            if (instance != null && instance != this && instance.gameObject != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // Frame rate limit (120fps for most mobile devices)
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;

            InitializeManagers();
        }

        /// <summary>
        /// Initialize all managers in dependency order
        /// Order matters: Input -> Pool -> UI -> Wave -> Stage -> StageState
        /// </summary>
        private void InitializeManagers()
        {
            Debug.Log("[GameManager] InitializeManagers called");

            // 몬스터끼리 물리 충돌 비활성화 (Layer 7 = Monster)
            int monsterLayer = LayerMask.NameToLayer("Monster");
            if (monsterLayer >= 0)
            {
                Physics.IgnoreLayerCollision(monsterLayer, monsterLayer, true);
            }

            // Initialize in dependency order
            if (inputManager != null)
            {
                inputManager.Initialize();
            }

            if (poolManager != null)
            {
                poolManager.Initialize();
            }

            if (uiManager != null)
            {
                uiManager.Initialize();
            }

            if (waveManager != null)
            {
                waveManager.Initialize();
            }

            if (stageManager != null)
            {
                Debug.Log("[GameManager] Initializing StageManager...");
                stageManager.Initialize();
            }
            else
            {
                Debug.LogError("[GameManager] stageManager is NULL! Inspector에서 StageManager 할당 필요!");
            }

            if (stageStateManager != null)
            {
                stageStateManager.Initialize();
            }
        }

        /// <summary>
        /// Reset all managers to initial state
        /// Useful for game restart without scene reload
        /// </summary>
        public void ResetAll()
        {
            inputManager?.Reset();
            poolManager?.Reset();
            uiManager?.Reset();
            waveManager?.Reset();
            stageManager?.Reset();
            stageStateManager?.Reset();
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
        }
    }
}
