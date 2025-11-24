using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Individual stage button controller
    /// Handles locked/unlocked state with overlay effect
    /// </summary>
    public class StageButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image stageImage;
        [SerializeField] private TextMeshProUGUI stageNumberText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private Button button;

        [Header("Settings")]
        [SerializeField] private int stageNumber;
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private bool isLocked = true;
        [SerializeField] private bool startLocked = true; // Inspector에서 초기 잠금 상태 설정

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void Start()
        {
            // 시작할 때 잠금 상태 적용
            SetStageNumber(stageNumber);
            SetLocked(startLocked);
        }

        /// <summary>
        /// Set the locked state of this stage button
        /// </summary>
        public void SetLocked(bool locked)
        {
            isLocked = locked;

            // Show/hide lock overlay and icon
            if (lockOverlay != null)
                lockOverlay.SetActive(locked);

            if (lockIcon != null)
                lockIcon.SetActive(locked);

            // Change image color (darker when locked)
            if (stageImage != null)
                stageImage.color = locked ? lockedColor : unlockedColor;

            // Disable button interaction when locked
            if (button != null)
                button.interactable = !locked;
        }

        /// <summary>
        /// Get whether this stage is locked
        /// </summary>
        public bool IsLocked()
        {
            return isLocked;
        }

        /// <summary>
        /// Get the stage number
        /// </summary>
        public int GetStageNumber()
        {
            return stageNumber;
        }

        /// <summary>
        /// Set the stage number and update display
        /// </summary>
        public void SetStageNumber(int number)
        {
            stageNumber = number;
            if (stageNumberText != null)
                stageNumberText.text = number.ToString();
        }
    }
}
