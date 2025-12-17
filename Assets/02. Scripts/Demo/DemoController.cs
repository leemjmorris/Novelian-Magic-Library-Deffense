using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.Demo
{
    /// <summary>
    /// Demo scene UI controller
    /// Connects UI buttons to demo functionality
    /// No external dependencies (CSVLoader, GameManager, etc.)
    /// </summary>
    public class DemoController : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button spawnCharacterButton;
        [SerializeField] private Button spawnMonsterButton;
        [SerializeField] private Button toggleModeButton;
        [SerializeField] private Button toggleCameraButton;
        [SerializeField] private Button resetButton;

        [Header("UI Labels")]
        [SerializeField] private TextMeshProUGUI modeLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private TextMeshProUGUI characterCountLabel;
        [SerializeField] private TextMeshProUGUI monsterCountLabel;

        [Header("Spawn Settings")]
        [SerializeField] private int monstersPerSpawn = 3;

        [Header("References (Auto-find if not set)")]
        [SerializeField] private DemoPlacementManager placementManager;
        [SerializeField] private DemoMonsterSpawner monsterSpawner;

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            // Auto-find references if not set
            if (placementManager == null)
            {
                placementManager = FindFirstObjectByType<DemoPlacementManager>();
            }

            if (monsterSpawner == null)
            {
                monsterSpawner = FindFirstObjectByType<DemoMonsterSpawner>();
            }

            // Wait for managers to be ready
            if (placementManager != null)
            {
                await UniTask.WaitUntil(() => placementManager.IsReady);
            }

            if (monsterSpawner != null)
            {
                await UniTask.WaitUntil(() => monsterSpawner.IsReady);
            }

            SetupButtons();
            UpdateAllUI();

            Debug.Log("[DemoController] Initialized");
        }

        private void SetupButtons()
        {
            if (spawnCharacterButton != null)
            {
                spawnCharacterButton.onClick.AddListener(OnSpawnCharacterClicked);
            }

            if (spawnMonsterButton != null)
            {
                spawnMonsterButton.onClick.AddListener(OnSpawnMonsterClicked);
            }

            if (toggleModeButton != null)
            {
                toggleModeButton.onClick.AddListener(OnToggleModeClicked);
            }

            if (toggleCameraButton != null)
            {
                toggleCameraButton.onClick.AddListener(OnToggleCameraClicked);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetClicked);
            }
        }

        #region Button Handlers

        private void OnSpawnCharacterClicked()
        {
            if (placementManager == null || !placementManager.IsReady)
            {
                UpdateStatusLabel("Not ready yet!");
                return;
            }

            bool success = placementManager.SpawnCharacter();

            if (success)
            {
                UpdateStatusLabel("Character spawned");
            }
            else
            {
                UpdateStatusLabel("No empty slots!");
            }

            UpdateCountLabels();
        }

        private void OnSpawnMonsterClicked()
        {
            if (monsterSpawner == null || !monsterSpawner.IsReady)
            {
                UpdateStatusLabel("Not ready yet!");
                return;
            }

            monsterSpawner.SpawnMonstersForCurrentMode(monstersPerSpawn);
            UpdateStatusLabel($"Spawned {monstersPerSpawn} monsters");
            UpdateCountLabels();
        }

        private void OnToggleModeClicked()
        {
            if (placementManager == null) return;

            placementManager.TogglePlacementMode();

            // Update monster spawner for new mode
            if (monsterSpawner != null)
            {
                monsterSpawner.UpdateSpawnAreasForMode(placementManager.CurrentMode);
            }

            UpdateModeLabel();
            UpdateStatusLabel($"Mode: {placementManager.CurrentMode}");
            UpdateCountLabels();
        }

        private void OnToggleCameraClicked()
        {
            if (placementManager == null) return;

            placementManager.ToggleCameraView();

            string viewName = placementManager.IsTopDownView ? "TopDown" : "Normal";
            UpdateStatusLabel($"Camera: {viewName}");
        }

        private void OnResetClicked()
        {
            // Clear characters
            if (placementManager != null)
            {
                placementManager.ClearAllCharacters();
            }

            // Clear monsters
            if (monsterSpawner != null)
            {
                monsterSpawner.ClearAllMonsters();
            }

            UpdateStatusLabel("All cleared");
            UpdateCountLabels();
        }

        #endregion

        #region UI Updates

        private void UpdateAllUI()
        {
            UpdateModeLabel();
            UpdateStatusLabel("Ready");
            UpdateCountLabels();
        }

        private void UpdateModeLabel()
        {
            if (modeLabel == null || placementManager == null) return;

            string modeName = placementManager.CurrentMode == DemoPlacementMode.Top ? "Top" : "Center";
            modeLabel.text = $"Mode: {modeName}";
        }

        private void UpdateStatusLabel(string status)
        {
            if (statusLabel == null) return;
            statusLabel.text = status;
        }

        private void UpdateCountLabels()
        {
            if (characterCountLabel != null && placementManager != null)
            {
                int spawned = placementManager.GetSpawnedCharacterCount();
                int total = placementManager.GetTotalSlotCount();
                int empty = placementManager.GetEmptySlotCount();
                characterCountLabel.text = $"Characters: {spawned}/{total} (Empty: {empty})";
            }

            if (monsterCountLabel != null && monsterSpawner != null)
            {
                int count = monsterSpawner.GetSpawnedMonsterCount();
                monsterCountLabel.text = $"Monsters: {count}";
            }
        }

        #endregion

        #region Keyboard Shortcuts

        private void Update()
        {
            // Keyboard shortcuts for quick testing (using new Input System)
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.cKey.wasPressedThisFrame)
            {
                OnSpawnCharacterClicked();
            }

            if (keyboard.mKey.wasPressedThisFrame)
            {
                OnSpawnMonsterClicked();
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                OnToggleModeClicked();
            }

            if (keyboard.vKey.wasPressedThisFrame)
            {
                OnToggleCameraClicked();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                OnResetClicked();
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Cleanup button listeners
            if (spawnCharacterButton != null) spawnCharacterButton.onClick.RemoveListener(OnSpawnCharacterClicked);
            if (spawnMonsterButton != null) spawnMonsterButton.onClick.RemoveListener(OnSpawnMonsterClicked);
            if (toggleModeButton != null) toggleModeButton.onClick.RemoveListener(OnToggleModeClicked);
            if (toggleCameraButton != null) toggleCameraButton.onClick.RemoveListener(OnToggleCameraClicked);
            if (resetButton != null) resetButton.onClick.RemoveListener(OnResetClicked);
        }
    }
}
