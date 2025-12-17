using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NovelianMagicLibraryDefense.UI
{
    public enum RewardRank
    {
        F = 0,
        B = 1,
        A = 2,
        S = 3
    }

    /// <summary>
    /// Reward rank system UI controller
    /// Pressing reward buttons 1, 2, 3 in order fills gauge by 1/3 each
    /// Rank progression: F -> B (reward1) -> A (reward2) -> S (reward3)
    /// </summary>
    public class RewardRankUI : MonoBehaviour
    {
        [Header("Reward Buttons")]
        [SerializeField] private Button reward1Button;
        [SerializeField] private Button reward2Button;
        [SerializeField] private Button reward3Button;

        [Header("UI Display")]
        [SerializeField] private Slider rewardGaugeSlider;

        private int rewardCount = 0;
        private const int MAX_REWARD_COUNT = 3;
        private RewardRank currentRank = RewardRank.F;

        private void Awake()
        {
            SetupButtonListeners();
        }

        private void Start()
        {
            ResetReward();
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
        }

        private void SetupButtonListeners()
        {
            if (reward1Button != null)
                reward1Button.onClick.AddListener(OnReward1Clicked);

            if (reward2Button != null)
                reward2Button.onClick.AddListener(OnReward2Clicked);

            if (reward3Button != null)
                reward3Button.onClick.AddListener(OnReward3Clicked);
        }

        private void RemoveButtonListeners()
        {
            if (reward1Button != null)
                reward1Button.onClick.RemoveListener(OnReward1Clicked);

            if (reward2Button != null)
                reward2Button.onClick.RemoveListener(OnReward2Clicked);

            if (reward3Button != null)
                reward3Button.onClick.RemoveListener(OnReward3Clicked);
        }

        private void OnReward1Clicked()
        {
            if (rewardCount == 0)
            {
                rewardCount = 1;
                currentRank = RewardRank.B;
                UpdateUI();
                Debug.Log($"[RewardRankUI] Reward 1 claimed! Rank: {currentRank}");
            }
        }

        private void OnReward2Clicked()
        {
            if (rewardCount == 1)
            {
                rewardCount = 2;
                currentRank = RewardRank.A;
                UpdateUI();
                Debug.Log($"[RewardRankUI] Reward 2 claimed! Rank: {currentRank}");
            }
        }

        private void OnReward3Clicked()
        {
            if (rewardCount == 2)
            {
                rewardCount = 3;
                currentRank = RewardRank.S;
                UpdateUI();
                Debug.Log($"[RewardRankUI] Reward 3 claimed! Rank: {currentRank}");
            }
        }

        private void UpdateUI()
        {
            // Update gauge (0, 1/3, 2/3, 3/3)
            if (rewardGaugeSlider != null)
            {
                rewardGaugeSlider.value = (float)rewardCount / MAX_REWARD_COUNT;
            }

            // Update button interactability
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (reward1Button != null)
                reward1Button.interactable = (rewardCount == 0);

            if (reward2Button != null)
                reward2Button.interactable = (rewardCount == 1);

            if (reward3Button != null)
                reward3Button.interactable = (rewardCount == 2);
        }

        /// <summary>
        /// Reset reward system to initial state (Rank F)
        /// </summary>
        public void ResetReward()
        {
            rewardCount = 0;
            currentRank = RewardRank.F;
            UpdateUI();
            Debug.Log("[RewardRankUI] Reward reset to F rank");
        }

        /// <summary>
        /// Get current reward rank
        /// </summary>
        public RewardRank GetCurrentRank()
        {
            return currentRank;
        }

        /// <summary>
        /// Get current reward progress (0-3)
        /// </summary>
        public int GetRewardCount()
        {
            return rewardCount;
        }

        /// <summary>
        /// Get gauge fill amount (0.0 ~ 1.0)
        /// </summary>
        public float GetGaugeFillAmount()
        {
            return (float)rewardCount / MAX_REWARD_COUNT;
        }
    }
}
