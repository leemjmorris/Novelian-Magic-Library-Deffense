using System.Collections.Generic;
using NovelianMagicLibraryDefense.Core;
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
        [SerializeField] private CardSelectionManager cardSelectionManager;

        // LMJ: Explicit manager references for type-safe access
        private InputManager inputManager;
        private UIManager uiManager;
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
        /// Some managers are optional and only created when required references exist (for LobbyScene support)
        /// </summary>
        private void InitializeManagers()
        {
            // LMJ: Create InputManager first (no dependencies)
            RegisterManager(inputManager = new InputManager());

            // LMJ: Create UIManager only if UI references exist (GameScene)
            if (monsterCountText != null || waveTimerText != null || barrierHPSlider != null)
            {
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
                Debug.Log("[GameManager] UIManager created (GameScene mode)");
            }
            else
            {
                Debug.Log("[GameManager] UIManager skipped (LobbyScene mode)");
            }

            // LMJ: Create game managers only if UIManager exists (GameScene)
            if (uiManager != null)
            {
                RegisterManager(poolManager = new ObjectPoolManager());
                RegisterManager(waveManager = new WaveManager(poolManager, uiManager));
                RegisterManager(stageManager = new StageManager(waveManager, uiManager, cardSelectionManager));

                // LMJ: Create StageStateManager only if wall reference exists
                if (wallReference != null && winLosePanel != null)
                {
                    RegisterManager(stageStateManager = new StageStateManager(waveManager, stageManager, wallReference, winLosePanel));
                }
            }

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
