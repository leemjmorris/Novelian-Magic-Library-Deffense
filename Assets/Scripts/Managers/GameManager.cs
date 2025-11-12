using System.Collections.Generic;
using NovelianMagicLibraryDefense.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Central manager that controls all other managers lifecycle
    /// Implements hybrid pattern: explicit references + list management
    /// Refactored to properly manage UIManager and remove UI responsibilities from GameManager
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance => instance;

        #region UI References (To be injected into UIManager)
        [Header("UI References - Monster Display")]
        [SerializeField] private TextMeshProUGUI monsterCountText;

        [Header("UI References - Timer Display")]
        [SerializeField] private TextMeshProUGUI waveTimerText;

        [Header("UI References - Barrier HP")]
        [SerializeField] private Slider barrierHPSlider;
        [SerializeField] private TextMeshProUGUI barrierHPText;

        [Header("UI References - Panels")]
        [SerializeField] private GameObject cardPanel;

        [Header("UI References - Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button skillButton1;
        [SerializeField] private Button skillButton2;
        [SerializeField] private Button skillButton3;
        [SerializeField] private Button skillButton4;
        #endregion

        [Header("Stage References")]
        [SerializeField] private Wall wallReference;

        // LMJ: Explicit manager references for type-safe access
        private UIManager uiManager;
        private ObjectPoolManager poolManager;
        private WaveManager waveManager;
        private StageManager stageManager;
        private StageStateManager stageStateManager;

        // LMJ: List management for batch operations
        private List<IManager> managers = new List<IManager>();

        // LMJ: Public accessors for type-safe manager access
        public UIManager UI => uiManager;
        public ObjectPoolManager Pool => poolManager;
        public WaveManager Wave => waveManager;
        public StageManager Stage => stageManager;
        public StageStateManager StageState => stageStateManager;

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
        /// Order matters: UI -> Pool -> Wave -> Stage -> StageState
        /// </summary>
        private void InitializeManagers()
        {
            // LMJ: Create UIManager first with all UI references
            uiManager = new UIManager(
                monsterCountText,
                waveTimerText,
                barrierHPSlider,
                barrierHPText,
                cardPanel,
                pauseButton,
                settingsButton,
                skillButton1,
                skillButton2,
                skillButton3,
                skillButton4
            );
            RegisterManager(uiManager);

            // LMJ: Create other managers with proper dependencies
            RegisterManager(poolManager = new ObjectPoolManager());
            RegisterManager(waveManager = new WaveManager(poolManager, uiManager));
            RegisterManager(stageManager = new StageManager(waveManager, uiManager));
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
