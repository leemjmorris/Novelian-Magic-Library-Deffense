using System.Collections.Generic;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Central manager that controls all other managers lifecycle
    /// Implements hybrid pattern: explicit references + list management
    /// Refactored to properly manage UIManager and remove UI responsibilities from GameManager
    /// Each scene has its own independent GameManager (no DontDestroyOnLoad)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance => instance;

        [Header("Manager References")]
        [SerializeField] private UIManager uiManager;

        [Header("Event Channels")]
        [SerializeField] private MonsterEvents monsterEvents;
        [SerializeField] private WallEvents wallEvents;
        [SerializeField] private StageEvents stageEvents;

        [Header("Stage References")]
        [SerializeField] private Wall wallReference;
        [SerializeField] private CardSelectionManager cardSelectionManager;
        [SerializeField] private WinLosePanel winLosePanel;

        // LMJ: Explicit manager references for type-safe access
        private InputManager inputManager;
        private ObjectPoolManager poolManager;
        private WaveManager waveManager;
        private StageManager stageManager;
        private StageStateManager stageStateManager;

        // LMJ: List management for batch operations
        private List<IManager> managers = new List<IManager>();

        // LMJ: Public accessors for type-safe manager access
        public InputManager Input => inputManager;
        public UIManager UI => uiManager;
        public ObjectPoolManager Pool => poolManager;
        public WaveManager Wave => waveManager;
        public StageManager Stage => stageManager;
        public StageStateManager StageState => stageStateManager;

        private void Awake()
        {
            // LMJ: Simple singleton without DontDestroyOnLoad
            // Each scene has its own independent GameManager
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[GameManager] Multiple GameManagers detected in scene! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            instance = this;
            InitializeManagers();
        }

        /// <summary>
        /// LMJ: Initialize all managers in dependency order
        /// Order matters: Input -> UI -> Pool -> Wave -> Stage -> StageState
        /// Note: SceneManager is removed (scene transitions now handled by FadeController)
        /// UIManager is now a MonoBehaviour component
        /// </summary>
        private void InitializeManagers()
        {
            // LMJ: Create InputManager first (no dependencies)
            RegisterManager(inputManager = new InputManager());

            // LMJ: UIManager is now a MonoBehaviour - check if it exists in scene
            if (uiManager != null)
            {
                Debug.Log("[GameManager] UIManager found (GameScene mode)");

                // LMJ: Create game managers only if UIManager exists (GameScene)
                RegisterManager(poolManager = new ObjectPoolManager());
                RegisterManager(waveManager = new WaveManager(poolManager, uiManager, monsterEvents, stageEvents));
                RegisterManager(stageManager = new StageManager(waveManager, uiManager, monsterEvents, stageEvents, cardSelectionManager));

                // LMJ: Create StageStateManager only if wall reference exists
                if (wallReference != null && winLosePanel != null)
                {
                    RegisterManager(stageStateManager = new StageStateManager(waveManager, stageManager, wallReference, winLosePanel, stageEvents, wallEvents));
                }
            }
            else
            {
                Debug.Log("[GameManager] UIManager not found (LobbyScene mode)");
            }

            // LMJ: Initialize all managers in registration order
            InitializeAll();

            // LMJ: Initialize UIManager separately (it's MonoBehaviour now)
            if (uiManager != null)
            {
                uiManager.Initialize();
            }
        }

        /// <summary>
        /// LMJ: Register manager to the list while maintaining explicit reference
        /// </summary>
        private void RegisterManager(IManager manager)
        {
            managers.Add(manager);
        }

        /// <summary>
        /// LMJ: Initialize all managers in order
        /// </summary>
        private void InitializeAll()
        {
            foreach (var manager in managers)
            {
                manager.Initialize();
                Debug.Log($"[GameManager] {manager.GetType().Name} initialized");
            }
        }

        /// <summary>
        /// LMJ: Reset all managers to initial state
        /// Useful for game restart without scene reload
        /// </summary>
        public void ResetAll()
        {
            Debug.Log("[GameManager] Resetting all managers");
            foreach (var manager in managers)
            {
                manager.Reset();
            }

            // LMJ: Reset UIManager separately (it's MonoBehaviour now)
            if (uiManager != null)
            {
                uiManager.Reset();
            }
        }

        /// <summary>
        /// LMJ: Clean up all managers in reverse order (safe dependency cleanup)
        /// </summary>
        private void OnDestroy()
        {
            if (instance != this) return;

            Debug.Log("[GameManager] Disposing all managers");
            for (int i = managers.Count - 1; i >= 0; i--)
            {
                managers[i].Dispose();
            }

            managers.Clear();
        }
    }
}
