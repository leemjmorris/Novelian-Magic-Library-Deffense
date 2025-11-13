using System.Collections.Generic;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [Header("UI References - Exp Slider")]
        [SerializeField] private Slider expSlider;

        [Header("UI References - Panels")]
        [SerializeField] private GameObject cardPanel;
        [SerializeField] private WinLosePanel winLosePanel;

        [Header("UI References - SpeedButtonText")]
        [SerializeField] private TextMeshProUGUI speedButtonText;

        [Header("UI References - Buttons")]
        [SerializeField] private Button speedButton;
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

            // LMJ: Subscribe to scene unload event to clean up pools on scene transition
            SceneManager.sceneUnloaded += OnSceneUnloaded;

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
                expSlider,
                cardPanel,
                speedButton,
                speedButtonText,
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
            RegisterManager(stageStateManager = new StageStateManager(waveManager, stageManager, wallReference, winLosePanel));

            // LMJ: Initialize all managers in registration order
            InitializeAll();
        }

        /// <summary>
        /// LMJ: Register manager to the list and ServiceLocator
        /// </summary>
        private void RegisterManager(IManager manager)
        {
            managers.Add(manager);

            // LMJ: Register to ServiceLocator for decoupled access
            if (manager is UIManager ui)
                ServiceLocator.Register(ui);
            else if (manager is ObjectPoolManager pool)
                ServiceLocator.Register(pool);
            else if (manager is WaveManager wave)
                ServiceLocator.Register(wave);
            else if (manager is StageManager stage)
                ServiceLocator.Register(stage);
            else if (manager is StageStateManager stageState)
                ServiceLocator.Register(stageState);
            else if (manager is DataManager data)
                ServiceLocator.Register(data);
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
        /// LMJ: Handle scene unload event - clean up object pools to prevent memory leaks
        /// Called automatically when any scene is unloaded
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"[GameManager] Scene unloaded: {scene.name}, cleaning up object pools");

            // LMJ: Clear all pooled objects to prevent zombie objects in DontDestroyOnLoad
            if (poolManager != null)
            {
                poolManager.ClearAll();
            }
        }

        /// <summary>
        /// LMJ: Clean up all managers in reverse order (safe dependency cleanup)
        /// </summary>
        private void OnDestroy()
        {
            if (instance != this) return;

            // LMJ: Unsubscribe from scene events to prevent memory leaks
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            Debug.Log("[GameManager] Disposing all managers");
            for (int i = managers.Count - 1; i >= 0; i--)
            {
                managers[i].Dispose();
            }

            managers.Clear();

            // LMJ: Clear ServiceLocator to prevent dangling references
            ServiceLocator.Clear();
        }
    }
}
