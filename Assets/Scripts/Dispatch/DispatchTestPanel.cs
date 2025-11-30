using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Dispatch
{
    /// <summary>
    /// 파견 시스템 테스트 UI 패널
    /// 기획서 10페이지 구조 참조 (간소화 버전)
    /// </summary>
    public class DispatchTestPanel : MonoBehaviour
    {
        [Header("파견 매니저 참조")]
        [SerializeField] private DispatchManager dispatchManager;

        [Header("UI 요소")]
        [SerializeField] private Slider timeSlider;                      // 시간 선택 슬라이더
        [SerializeField] private TextMeshProUGUI selectedTimeText;       // 선택된 시간 표시
        [SerializeField] private TextMeshProUGUI descriptionText;        // 파견 설명
        [SerializeField] private Button dispatchButton;                  // 파견하기 버튼

        [Header("파견 시간 설정")]
        [SerializeField] private DispatchTimeSettings timeSettings;

        [Header("테스트 설정")]
        [SerializeField] private string testLocationName = "마력 잔재 정화";  // 테스트용 파견 장소
        [SerializeField] private DispatchType testDispatchType = DispatchType.Collection;

        private int currentSelectedHours = 4;
        private int[] availableHours = { 4, 8, 12, 23 };

        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // 슬라이더 설정 (0 ~ 3 인덱스)
            timeSlider.minValue = 0;
            timeSlider.maxValue = availableHours.Length - 1;
            timeSlider.wholeNumbers = true;
            timeSlider.value = 0;

            UpdateTimeDisplay(0);
        }

        /// <summary>
        /// 이벤트 리스너 설정
        /// </summary>
        private void SetupEventListeners()
        {
            timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            dispatchButton.onClick.AddListener(OnDispatchButtonClicked);
        }

        /// <summary>
        /// 슬라이더 값 변경 시
        /// </summary>
        private void OnTimeSliderChanged(float value)
        {
            int index = Mathf.RoundToInt(value);
            UpdateTimeDisplay(index);
        }

        /// <summary>
        /// 시간 표시 업데이트
        /// </summary>
        private void UpdateTimeDisplay(int index)
        {
            currentSelectedHours = availableHours[index];
            var timeData = timeSettings.GetTimeData(currentSelectedHours);

            // 선택된 시간 텍스트
            selectedTimeText.text = $"{currentSelectedHours}시간";

            // 설명 텍스트
            descriptionText.text = $"{timeData.description}\n\n" +
                                   $"<color=yellow>보상 배율: x{timeData.rewardMultiplier}</color>\n" +
                                   $"하루 횟수: {timeData.dailyLimit}회";
        }

        /// <summary>
        /// 파견하기 버튼 클릭
        /// </summary>
        private void OnDispatchButtonClicked()
        {
            if (dispatchManager == null)
            {
                Debug.LogError("[DispatchTestPanel] DispatchManager가 할당되지 않았습니다!");
                return;
            }

            // 파견 시작
            int locationId = 1; // 테스트용 ID
            dispatchManager.StartDispatch(
                locationId,
                testLocationName,
                testDispatchType,
                currentSelectedHours
            );

            // 버튼 비활성화 (진행 중에는 중복 파견 불가)
            dispatchButton.interactable = false;

            // 일정 시간 후 버튼 다시 활성화 (테스트용)
            Invoke(nameof(EnableDispatchButton), 2f);
        }

        /// <summary>
        /// 파견하기 버튼 다시 활성화
        /// </summary>
        private void EnableDispatchButton()
        {
            dispatchButton.interactable = true;
        }

        private void OnDestroy()
        {
            // 이벤트 리스너 제거
            if (timeSlider != null)
                timeSlider.onValueChanged.RemoveListener(OnTimeSliderChanged);
            if (dispatchButton != null)
                dispatchButton.onClick.RemoveListener(OnDispatchButtonClicked);
        }
    }
}
