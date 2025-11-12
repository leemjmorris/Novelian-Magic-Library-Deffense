using System.Collections.Generic;
using NovelianMagicLibraryDefense.Core;
using TMPro;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Central manager that controls all other managers lifecycle
    /// Implements hybrid pattern: explicit references + list management
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance => instance;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI monsterCountText;

        [Header("Stage References")]
        [SerializeField] private Wall wallReference;

        // LMJ: Explicit manager references for type-safe access
        private ObjectPoolManager poolManager;
        private WaveManager waveManager;
        private StageManager stageManager;
        private StageStateManager stageStateManager;

        // LMJ: List management for batch operations
        private List<IManager> managers = new List<IManager>();

        // LMJ: Public accessors for type-safe manager access
        public ObjectPoolManager Pool => poolManager;
        public WaveManager Wave => waveManager;
        public StageManager Stage => stageManager;
        public StageStateManager StageState => stageStateManager;
        public TextMeshProUGUI MonsterCountText => monsterCountText;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeManagers();
        }

        /// <summary>
        /// LMJ: Initialize all managers in dependency order
        /// Order matters: Pool -> Wave -> Stage
        /// </summary>
        private void InitializeManagers()
        {
            // LMJ: Create manager instances with dependencies
            RegisterManager(poolManager = new ObjectPoolManager());
            RegisterManager(waveManager = new WaveManager(poolManager, monsterCountText));
            RegisterManager(stageManager = new StageManager(waveManager));
            RegisterManager(stageStateManager = new StageStateManager(waveManager, stageManager, wallReference));
            
            // LMJ: Initialize all managers in registration order
            InitializeAll();
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
