using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NovelianMagicLibraryDefense.Core;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LCB: Central UI manager that handles all UI elements and button interactions
    /// Manages monster count, wave timer, barrier HP, and skill list displays
    /// </summary>
    public class UIManager : MonoBehaviour, IManager
    {
        private static UIManager instance;
        public static UIManager Instance => instance;

        [Header("Monster Display")]
        [SerializeField] private TextMeshProUGUI monsterCountText;

        [Header("Wave Timer Display")]
        [SerializeField] private TextMeshProUGUI waveTimerText;

        [Header("Barrier HP Display")]
        [SerializeField] private Slider barrierHPSlider;
        [SerializeField] private TextMeshProUGUI barrierHPText;
        [SerializeField] private Wall wallReference;

        [Header("Skill List Display")]
        [SerializeField] private Transform skillListContainer;
        [SerializeField] private GameObject skillItemPrefab;

        [Header("Button References")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button skillButton1;
        [SerializeField] private Button skillButton2;
        [SerializeField] private Button skillButton3;
        [SerializeField] private Button skillButton4;

        private void Awake()
        {
            //LCB: Singleton pattern for global UI access
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        public void Initialize()
        {
            //LCB: Setup button listeners on initialization
            SetupButtonListeners();
            Debug.Log("[UIManager] Initialized");
        }

        public void Reset()
        {
            //LCB: Reset all UI elements to initial state
            UpdateMonsterCount(0);
            UpdateWaveTimer(0f);
            Debug.Log("[UIManager] Reset");
        }

        public void Dispose()
        {
            //LCB: Cleanup button listeners on disposal
            RemoveButtonListeners();
            Debug.Log("[UIManager] Disposed");
        }

        #region Button Setup

        //LCB: Setup all button click listeners
        private void SetupButtonListeners()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseButtonClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);

            if (skillButton1 != null)
                skillButton1.onClick.AddListener(() => OnSkillButtonClicked(1));

            if (skillButton2 != null)
                skillButton2.onClick.AddListener(() => OnSkillButtonClicked(2));

            if (skillButton3 != null)
                skillButton3.onClick.AddListener(() => OnSkillButtonClicked(3));

            if (skillButton4 != null)
                skillButton4.onClick.AddListener(() => OnSkillButtonClicked(4));
        }

        //LCB: Remove all button click listeners
        private void RemoveButtonListeners()
        {
            if (pauseButton != null)
                pauseButton.onClick.RemoveListener(OnPauseButtonClicked);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);

            if (skillButton1 != null)
                skillButton1.onClick.RemoveAllListeners();

            if (skillButton2 != null)
                skillButton2.onClick.RemoveAllListeners();

            if (skillButton3 != null)
                skillButton3.onClick.RemoveAllListeners();

            if (skillButton4 != null)
                skillButton4.onClick.RemoveAllListeners();
        }

        #endregion

        #region Button Callbacks

        //LCB: Handle pause button click - to be implemented with pause logic
        private void OnPauseButtonClicked()
        {
            Debug.Log("[UIManager] Pause button clicked - Logic to be implemented");
            //TODO: Implement pause/resume game logic
        }

        //LCB: Handle settings button click - to be implemented with settings menu
        private void OnSettingsButtonClicked()
        {
            Debug.Log("[UIManager] Settings button clicked - Logic to be implemented");
            //TODO: Implement settings menu logic
        }

        //LCB: Handle skill button clicks with skill index parameter
        private void OnSkillButtonClicked(int skillIndex)
        {
            Debug.Log($"[UIManager] Skill button {skillIndex} clicked - Logic to be implemented");
            //TODO: Implement skill activation logic
        }

        #endregion

        #region Monster Count Display

        //LCB: Update monster count display with current remaining monsters
        public void UpdateMonsterCount(int count)
        {
            if (monsterCountText != null)
            {
                monsterCountText.text = $"남은 몬스터: {count}";
            }
        }

        //LCB: Get current monster count from text (if needed for calculations)
        public int GetCurrentMonsterCount()
        {
            //TODO: Implement logic to get actual monster count from game state
            return 0;
        }

        #endregion

        #region Wave Timer Display

        //LCB: Update wave timer display with remaining time in seconds
        public void UpdateWaveTimer(float timeInSeconds)
        {
            if (waveTimerText != null)
            {
                int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
                int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
                waveTimerText.text = $"웨이브 시간: {minutes:00}:{seconds:00}";
            }
        }

        //LCB: Start wave timer countdown
        public void StartWaveTimer(float duration)
        {
            //TODO: Implement timer countdown logic using coroutine or update
            Debug.Log($"[UIManager] Wave timer started for {duration} seconds");
        }

        //LCB: Stop wave timer
        public void StopWaveTimer()
        {
            //TODO: Implement timer stop logic
            Debug.Log("[UIManager] Wave timer stopped");
        }

        #endregion

        #region Barrier HP Display

        //LCB: Update barrier HP slider and text with current and max values
        public void UpdateBarrierHP(float currentHP, float maxHP)
        {
            if (barrierHPSlider != null)
            {
                barrierHPSlider.value = currentHP / maxHP;
            }

            if (barrierHPText != null)
            {
                barrierHPText.text = $"결계 HP: {currentHP:F0}/{maxHP:F0}";
            }
        }

        //LCB: Get wall reference for HP monitoring
        public void SetWallReference(Wall wall)
        {
            wallReference = wall;
            Debug.Log("[UIManager] Wall reference set");
        }

        //LCB: Manually refresh barrier HP from wall reference
        public void RefreshBarrierHP()
        {
            if (wallReference != null)
            {
                //TODO: Access wall's current HP and update display
                Debug.Log("[UIManager] Barrier HP refreshed");
            }
        }

        #endregion

        #region Skill List Management

        // //LCB: Add skill item to skill list container
        // public void AddSkillToList(string skillName, Sprite skillIcon)
        // {
        //     if (skillListContainer == null || skillItemPrefab == null)
        //     {
        //         Debug.LogWarning("[UIManager] Skill list container or prefab is null");
        //         return;
        //     }

        //     //TODO: Instantiate skill item prefab and populate with data
        //     Debug.Log($"[UIManager] Added skill to list: {skillName}");
        // }

        // //LCB: Remove skill item from skill list
        // public void RemoveSkillFromList(int index)
        // {
        //     if (skillListContainer == null || index < 0 || index >= skillListContainer.childCount)
        //     {
        //         Debug.LogWarning("[UIManager] Invalid skill list index");
        //         return;
        //     }

        //     //TODO: Remove skill item at index
        //     Debug.Log($"[UIManager] Removed skill from list at index: {index}");
        // }

        // //LCB: Clear all skills from skill list
        // public void ClearSkillList()
        // {
        //     if (skillListContainer == null) return;

        //     foreach (Transform child in skillListContainer)
        //     {
        //         Destroy(child.gameObject);
        //     }

        //     Debug.Log("[UIManager] Skill list cleared");
        // }

        // //LCB: Update skill button state (enabled/disabled)
        // public void SetSkillButtonState(int skillIndex, bool isEnabled)
        // {
        //     Button targetButton = skillIndex switch
        //     {
        //         1 => skillButton1,
        //         2 => skillButton2,
        //         3 => skillButton3,
        //         4 => skillButton4,
        //         _ => null
        //     };

        //     if (targetButton != null)
        //     {
        //         targetButton.interactable = isEnabled;
        //     }
        // }

        #endregion
    }
}
