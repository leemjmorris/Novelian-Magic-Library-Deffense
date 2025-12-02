using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace Dispatch
{
    /// <summary>
    /// 파견 시스템 테스트 UI 패널
    /// CSV 데이터 기반 보상 시스템
    /// DisPatchSelect(전투형/채집형)별로 버튼 생성하여 장소별 보상 로직 테스트
    /// </summary>
    public class DispatchTestPanel : MonoBehaviour
    {
        [Header("파견 매니저 참조")]
        [SerializeField] private DispatchManager dispatchManager;

        [Header("UI 요소")]
        [SerializeField] private Slider timeSlider;                      // 시간 선택 슬라이더
        [SerializeField] private TextMeshProUGUI selectedTimeText;       // 선택된 시간 표시
        //[SerializeField] private TextMeshProUGUI descriptionText;        // 파견 설명
        [SerializeField] private TextMeshProUGUI rewardInfoText;         // 보상 정보 표시
        [SerializeField] private ScrollRect buttonScrollRect;            // 버튼 스크롤뷰

        [Header("전투형 버튼 (5개)")]
        [SerializeField] private Button combatButton1;  // 악몽의 창고
        [SerializeField] private Button combatButton2;  // 운명의 창고
        [SerializeField] private Button combatButton3;  // 웃음의 창고
        [SerializeField] private Button combatButton4;  // 진실의 창고
        [SerializeField] private Button combatButton5;  // 미지의 창고

        [Header("채집형 버튼 (5개)")]
        [SerializeField] private Button collectionButton1;  // 마도 서고 정돈
        [SerializeField] private Button collectionButton2;  // 마력 장벽 유지 검사
        [SerializeField] private Button collectionButton3;  // 마도서 표지 복원
        [SerializeField] private Button collectionButton4;  // 봉인구 안정성 확인
        [SerializeField] private Button collectionButton5;  // 마력 잔재 정화

        [Header("덱 캐릭터 표시 (4개)")]
        [SerializeField] private Image deckCharacterImage1;
        [SerializeField] private Image deckCharacterImage2;
        [SerializeField] private Image deckCharacterImage3;
        [SerializeField] private Image deckCharacterImage4;

        [Header("파견 실행 버튼")]
        [SerializeField] private Button dispatchStartButton;  // 파견하기 버튼
        [SerializeField] private TextMeshProUGUI dispatchButtonText;  // 버튼 텍스트
        [SerializeField] private TextMeshProUGUI countdownTimerText;  // 카운트다운 타이머 텍스트

        [SerializeField] private GameObject sliderObject;  // 슬라이더 오브젝트 (숨김 처리용)
        [SerializeField] private GameObject TipPanelObject;  // 팁표시 오브젝트 (숨김 처리용)

        [Header("창고별 팁 텍스트 (5개)")]
        [SerializeField] private GameObject tipText1;  // 악몽의 창고 팁
        [SerializeField] private GameObject tipText2;  // 운명의 창고 팁
        [SerializeField] private GameObject tipText3;  // 웃음의 창고 팁
        [SerializeField] private GameObject tipText4;  // 진실의 창고 팁
        [SerializeField] private GameObject tipText5;  // 미지의 창고 팁

        private int currentSelectedHours = 4;
        private int currentSelectedTimeID;
        private List<DispatchTimeTableData> availableTimes;
        private DispatchLocation currentSelectedLocation = DispatchLocation.NightmareWarehouse;

        // 파견 상태 관리
        private bool isDispatching = false;
        private float remainingTime = 0f;

        // 스냅 스크롤 관련
        private int totalCombatButtons = 5;  // 전투형 5개
        private int currentButtonIndex = 0;
        private bool isDragging = false;
        private float targetScrollPosition = 0f;
        private float scrollVelocity = 0f;

        private void Start()
        {
            LoadCSVData();
            InitializeUI();
            SetupEventListeners();
            SetupLocationButtons();

            // 초기 UI 상태 설정
            if (countdownTimerText != null)
                countdownTimerText.gameObject.SetActive(false);

            // 모든 팁 텍스트 초기 비활성화
            HideAllTipTexts();

            // 스크롤뷰를 맨 왼쪽(악몽의 창고)으로 이동
            if (buttonScrollRect != null)
                buttonScrollRect.horizontalNormalizedPosition = 0f;

            // 덱 캐릭터 로드
            LoadDeckCharacters();

            // 첫 번째 창고(악몽의 창고) 팁 표시
            ShowTipText(DispatchLocation.NightmareWarehouse);

            AddLog("파견 테스트 패널 초기화 완료");
        }

        private void Update()
        {
            // 파견 중일 때 카운트다운 업데이트
            if (isDispatching && remainingTime > 0f)
            {
                remainingTime -= Time.deltaTime;

                if (remainingTime <= 0f)
                {
                    remainingTime = 0f;
                    OnDispatchComplete();
                }

                UpdateCountdownDisplay();
            }

            // 스냅 스크롤 처리
            if (buttonScrollRect != null && !isDragging)
            {
                // 부드럽게 타겟 위치로 이동
                buttonScrollRect.horizontalNormalizedPosition = Mathf.SmoothDamp(
                    buttonScrollRect.horizontalNormalizedPosition,
                    targetScrollPosition,
                    ref scrollVelocity,
                    0.1f
                );
            }
        }

        /// <summary>
        /// CSV 데이터 로드
        /// </summary>
        private void LoadCSVData()
        {
            // CSV 로더가 초기화될 때까지 대기
            if (!CSVLoader.Instance.IsInit)
            {
                Debug.LogWarning("[DispatchTestPanel] CSVLoader가 아직 초기화되지 않았습니다. 잠시 후 다시 시도하세요.");
                return;
            }

            // 파견 시간 테이블 로드
            var timeTable = CSVLoader.Instance.GetTable<DispatchTimeTableData>();
            if (timeTable != null)
            {
                availableTimes = timeTable.FindAll(x => true).OrderBy(x => x.Required_Hours).ToList();
                Debug.Log($"[DispatchTestPanel] 파견 시간 데이터 로드 완료: {availableTimes.Count}개");
            }
            else
            {
                Debug.LogError("[DispatchTestPanel] 파견 시간 테이블을 로드할 수 없습니다!");
            }
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            if (availableTimes == null || availableTimes.Count == 0)
            {
                Debug.LogError("[DispatchTestPanel] 파견 시간 데이터가 없습니다!");
                return;
            }

            // 슬라이더 설정 (0 ~ 시간 옵션 개수 - 1)
            timeSlider.minValue = 0;
            timeSlider.maxValue = availableTimes.Count - 1;
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

            // 파견하기 버튼 이벤트 등록
            if (dispatchStartButton != null)
            {
                dispatchStartButton.onClick.AddListener(OnDispatchStartButtonClicked);
            }

            // 스크롤 드래그 이벤트 등록
            if (buttonScrollRect != null)
            {
                var eventTrigger = buttonScrollRect.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = buttonScrollRect.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }

                // BeginDrag 이벤트
                var beginDragEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
                beginDragEntry.eventID = UnityEngine.EventSystems.EventTriggerType.BeginDrag;
                beginDragEntry.callback.AddListener((data) => { OnBeginDrag(); });
                eventTrigger.triggers.Add(beginDragEntry);

                // EndDrag 이벤트
                var endDragEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
                endDragEntry.eventID = UnityEngine.EventSystems.EventTriggerType.EndDrag;
                endDragEntry.callback.AddListener((data) => { OnEndDrag(); });
                eventTrigger.triggers.Add(endDragEntry);
            }
        }

        /// <summary>
        /// 드래그 시작
        /// </summary>
        private void OnBeginDrag()
        {
            isDragging = true;
        }

        /// <summary>
        /// 드래그 종료 - 가장 가까운 버튼으로 스냅
        /// </summary>
        private void OnEndDrag()
        {
            isDragging = false;

            if (buttonScrollRect == null) return;

            // 현재 스크롤 위치에서 가장 가까운 버튼 인덱스 계산
            float currentPos = buttonScrollRect.horizontalNormalizedPosition;
            currentButtonIndex = Mathf.RoundToInt(currentPos * (totalCombatButtons - 1));
            currentButtonIndex = Mathf.Clamp(currentButtonIndex, 0, totalCombatButtons - 1);

            // 타겟 위치 설정
            targetScrollPosition = (float)currentButtonIndex / (totalCombatButtons - 1);
        }

        /// <summary>
        /// 장소별 버튼 이벤트 설정
        /// </summary>
        private void SetupLocationButtons()
        {
            AddLog("=== 버튼 이벤트 설정 ===");

            // 전투형 버튼 설정
            SetupButton(combatButton1, DispatchLocation.NightmareWarehouse);
            SetupButton(combatButton2, DispatchLocation.FateWarehouse);
            SetupButton(combatButton3, DispatchLocation.LaughterWarehouse);
            SetupButton(combatButton4, DispatchLocation.TruthWarehouse);
            SetupButton(combatButton5, DispatchLocation.UnknownWarehouse);

            // 채집형 버튼 설정
            SetupButton(collectionButton1, DispatchLocation.MagicLibraryOrganization);
            SetupButton(collectionButton2, DispatchLocation.MagicBarrierInspection);
            SetupButton(collectionButton3, DispatchLocation.SpellbookCoverRestoration);
            SetupButton(collectionButton4, DispatchLocation.SealStabilityCheck);
            SetupButton(collectionButton5, DispatchLocation.MagicResiduePurification);
        }

        /// <summary>
        /// 개별 버튼 설정
        /// </summary>
        private void SetupButton(Button button, DispatchLocation location)
        {
            if (button == null)
            {
                Debug.LogWarning($"[DispatchTestPanel] {GetLocationName(location)} 버튼이 할당되지 않았습니다!");
                AddLog($"⚠️ {GetLocationName(location)} 버튼 없음");
                return;
            }

            // 버튼 클릭 이벤트 등록
            button.onClick.AddListener(() => OnLocationButtonClicked(location));

            // 버튼 텍스트 설정 (있을 경우)
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = GetLocationName(location);
            }

            AddLog($"✓ {GetLocationName(location)} 버튼 설정 완료");
        }

        /// <summary>
        /// 장소 버튼 클릭 시 (장소 선택만)
        /// </summary>
        private void OnLocationButtonClicked(DispatchLocation location)
        {
            currentSelectedLocation = location;

            AddLog($"📍 선택된 장소: {GetLocationName(location)}");

            // 해당 창고의 팁 텍스트 표시
            ShowTipText(location);

            // UI 업데이트 (보상 정보만 표시)
            UpdateTimeDisplay(Mathf.RoundToInt(timeSlider.value));
        }

        /// <summary>
        /// 파견하기 버튼 클릭 시 (실제 파견 실행)
        /// </summary>
        private void OnDispatchStartButtonClicked()
        {
            if (isDispatching)
            {
                // 파견 완료 - 보상 획득
                OnClaimReward();
            }
            else
            {
                // 파견 시작
                StartDispatch();
            }
        }

        /// <summary>
        /// 파견 시작
        /// </summary>
        private void StartDispatch()
        {
            AddLog("\n==============================================");
            AddLog($"🚀 파견 시작 버튼 클릭!");

            // 파견 실행 및 보상 로직 콘솔 출력
            ExecuteDispatch(currentSelectedLocation);

            // 파견 시작 상태로 전환
            isDispatching = true;
            // 테스트용: 초 단위로 시간 설정 (실제 게임에서는 시간 * 3600)
            remainingTime = currentSelectedHours; // 선택한 숫자를 초로 사용 (4시간 선택 = 4초)

            // UI 업데이트
            UpdateDispatchUI();

            AddLog($"⏰ 테스트 모드: {currentSelectedHours}초 후 완료 예정");
            AddLog("==============================================\n");
        }

        /// <summary>
        /// 파견 UI 업데이트 (파견 시작 시)
        /// </summary>
        private void UpdateDispatchUI()
        {
            // 팁표시 숨김
            if (TipPanelObject != null)
                TipPanelObject.SetActive(false);
            // 슬라이더 숨김
            if (sliderObject != null)
                sliderObject.SetActive(false);
            //시간 선택 텍스트 숨김
            if (selectedTimeText != null)
                selectedTimeText.gameObject.SetActive(false);

            // 보상정보설명 텍스트 숨김
            if (rewardInfoText != null)
                rewardInfoText.gameObject.SetActive(false);

            // 카운트다운 타이머 표시
            if (countdownTimerText != null)
                countdownTimerText.gameObject.SetActive(true);

            // 버튼 텍스트 변경 및 비활성화
            if (dispatchButtonText != null)
                dispatchButtonText.text = "획득하기";

            if (dispatchStartButton != null)
                dispatchStartButton.interactable = false;

            UpdateCountdownDisplay();
        }

        /// <summary>
        /// 카운트다운 표시 업데이트
        /// </summary>
        private void UpdateCountdownDisplay()
        {
            if (countdownTimerText == null) return;

            int hours = Mathf.FloorToInt(remainingTime / 3600f);
            int minutes = Mathf.FloorToInt(remainingTime % 3600f / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);

            countdownTimerText.text = $"남은 시간  {hours:D2} : {minutes:D2} : {seconds:D2}";
        }

        /// <summary>
        /// 파견 완료 시
        /// </summary>
        private void OnDispatchComplete()
        {
            // 획득하기 버튼 활성화 (로그 없이)
            if (dispatchStartButton != null)
                dispatchStartButton.interactable = true;

        }

        /// <summary>
        /// 보상 획득 버튼 클릭 시
        /// </summary>
        private void OnClaimReward()
        {
            AddLog("\n==============================================");
            AddLog("🎁 보상 획득!");

            // 보상 정보 출력
            var locationData = GetLocationData(currentSelectedLocation);
            if (locationData != null)
            {
                var categoryData = GetCategoryData(locationData.Dispatch_ID);
                if (categoryData != null)
                {
                    string dispatchTypeName = ((DispatchType)categoryData.Dispatch_Category) == DispatchType.Combat ? "전투형" : "채집형";

                    AddLog($"📍 장소: {GetLocationName(currentSelectedLocation)}");
                    AddLog($"🎯 타입: {dispatchTypeName}");
                    AddLog($"⏰ 소요 시간: {currentSelectedHours}시간");

                    var rewardData = GetRewardData(locationData.Dispatch_Location_ID, currentSelectedTimeID);
                    if (rewardData != null)
                    {
                        AddLog($"💰 보상 배율: x{rewardData.Reward_Multiplier}");

                        // 보상 상세 정보 출력
                        LogRewardDetails(rewardData);
                    }

                    AddLog($"✅ 완료 시간: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }
            }

            AddLog("✅ 보상이 인벤토리에 추가되었습니다.");

            // 파견 상태 초기화
            ResetDispatchUI();

            AddLog("==============================================\n");
            // 슬라이더 다시 표시
            if (sliderObject != null)
                sliderObject.SetActive(true);

            //시간 선택 텍스트 표시
            if (selectedTimeText != null)
                selectedTimeText.gameObject.SetActive(true);

            // 보상정보설명 텍스트 표시
            if (rewardInfoText != null)
                rewardInfoText.gameObject.SetActive(true);
        }

        /// <summary>
        /// 파견 UI 초기화 (보상 획득 후)
        /// </summary>
        private void ResetDispatchUI()
        {
            isDispatching = false;
            remainingTime = 0f;

            // 슬라이더 다시 표시
            if (sliderObject != null)
                sliderObject.SetActive(true);
            //시간 선택 텍스트 표시
            if (selectedTimeText != null)
                selectedTimeText.gameObject.SetActive(true);
            //팁 표시 다시 표시
            if (TipPanelObject != null)
                TipPanelObject.SetActive(true);

            // 카운트다운 타이머 숨김
            if (countdownTimerText != null)
                countdownTimerText.gameObject.SetActive(false);

            // 버튼 텍스트 복원
            if (dispatchButtonText != null)
                dispatchButtonText.text = "파견하기";

            if (dispatchStartButton != null)
                dispatchStartButton.interactable = true;
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
        /// 시간 표시 업데이트 및 보상 정보 표시
        /// </summary>
        private void UpdateTimeDisplay(int index)
        {
            if (availableTimes == null || index >= availableTimes.Count)
                return;

            var timeData = availableTimes[index];
            currentSelectedHours = (int)timeData.Required_Hours;
            currentSelectedTimeID = timeData.Dispatch_Time_ID;

            // 선택된 시간 텍스트
            selectedTimeText.text = $"{currentSelectedHours}시간";

            // 파견 장소 정보 가져오기
            var locationData = GetLocationData(currentSelectedLocation);
            if (locationData == null)
            {
                //descriptionText.text = "장소 정보를 찾을 수 없습니다.";
                return;
            }

            // 보상 정보 가져오기
            var rewardData = GetRewardData(locationData.Dispatch_Location_ID, currentSelectedTimeID);
            if (rewardData == null)
            {
                //descriptionText.text = $"{currentSelectedHours}시간 파견\n보상 정보를 찾을 수 없습니다.";
                return;
            }

            // 설명 텍스트 (에디터 텍스트 크기 사용)
            //descriptionText.text = $"<b>{GetLocationName(currentSelectedLocation)}</b>\n" +
                                   //$"파견 시간: {currentSelectedHours}시간\n" +
                                   //$"<color=yellow>보상 배율: x{rewardData.Reward_Multiplier}</color>";

            // 보상 상세 정보 표시
            DisplayRewardInfo(rewardData);
        }

        /// <summary>
        /// 보상 정보 표시
        /// </summary>
        private void DisplayRewardInfo(DispatchRewardTableData rewardData)
        {
            if (rewardInfoText == null) return;

            // 보상 그룹 데이터 가져오기
            var rewardGroupData = CSVLoader.Instance.GetData<RewardGroupData>(rewardData.Reward_Group_ID);
            if (rewardGroupData == null)
            {
                rewardInfoText.text = "보상 그룹 정보 없음";
                return;
            }

            // 보상 아이템 목록 가져오기
            List<string> rewardTexts = new List<string>();

            // Reward_1_ID ~ Reward_5_ID 체크
            int[] rewardIDs = new int[]
            {
                rewardGroupData.Reward_1_ID,
                rewardGroupData.Reward_2_ID,
                rewardGroupData.Reward_3_ID,
                rewardGroupData.Reward_4_ID,
                rewardGroupData.Reward_5_ID
            };

            foreach (var rewardID in rewardIDs)
            {
                if (rewardID == 0) continue; // 0이면 보상 없음

                var reward = CSVLoader.Instance.GetData<RewardData>(rewardID);
                if (reward != null)
                {
                    int minCount = Mathf.FloorToInt(reward.Min_Count * rewardData.Reward_Multiplier);
                    int maxCount = Mathf.FloorToInt(reward.Max_Count * rewardData.Reward_Multiplier);

                    string fixedText = reward.Is_Fixed ? "[고정]" : $"[{reward.Probability * 100:F0}%]";
                    rewardTexts.Add($"{fixedText} 아이템 ID {reward.Item_ID}: {minCount}~{maxCount}개");
                }
            }

            if (rewardTexts.Count > 0)
            {
                rewardInfoText.text = "<b>예상 보상:</b>\n" + string.Join("\n", rewardTexts);
            }
            else
            {
                rewardInfoText.text = "보상 정보 없음";
            }
        }

        /// <summary>
        /// 파견 장소 데이터 가져오기
        /// </summary>
        private DispatchLocationData GetLocationData(DispatchLocation location)
        {
            var locationTable = CSVLoader.Instance.GetTable<DispatchLocationData>();
            if (locationTable == null) return null;

            return locationTable.FindAll(x => x.Dispatch_Location == location).FirstOrDefault();
        }

        /// <summary>
        /// 파견 장소 이름 가져오기
        /// </summary>
        private string GetLocationName(DispatchLocation location)
        {
            switch (location)
            {
                case DispatchLocation.NightmareWarehouse: return "악몽의 창고";
                case DispatchLocation.FateWarehouse: return "운명의 창고";
                case DispatchLocation.LaughterWarehouse: return "웃음의 창고";
                case DispatchLocation.TruthWarehouse: return "진실의 창고";
                case DispatchLocation.UnknownWarehouse: return "미지의 창고";
                case DispatchLocation.MagicLibraryOrganization: return "마도 서고 정돈";
                case DispatchLocation.MagicBarrierInspection: return "마력 장벽 유지 검사";
                case DispatchLocation.SpellbookCoverRestoration: return "마도서 표지 복원";
                case DispatchLocation.SealStabilityCheck: return "봉인구 안정성 확인";
                case DispatchLocation.MagicResiduePurification: return "마력 잔재 정화";
                default: return "알 수 없는 장소";
            }
        }

        /// <summary>
        /// 보상 데이터 가져오기
        /// </summary>
        private DispatchRewardTableData GetRewardData(int locationID, int timeID)
        {
            var rewardTable = CSVLoader.Instance.GetTable<DispatchRewardTableData>();
            if (rewardTable == null) return null;

            return rewardTable.FindAll(x =>
                x.Dispatch_Location_ID == locationID &&
                x.Dispatch_Time_ID == timeID
            ).FirstOrDefault();
        }

        /// <summary>
        /// 카테고리 데이터 가져오기 (Dispatch_ID로 조회)
        /// </summary>
        private DispatchCategoryData GetCategoryData(int dispatchID)
        {
            var categoryTable = CSVLoader.Instance.GetTable<DispatchCategoryData>();
            if (categoryTable == null)
            {
                Debug.LogError("[DispatchTestPanel] DispatchCategoryTable을 로드할 수 없습니다!");
                return null;
            }

            Debug.Log($"[DispatchTestPanel] DispatchCategoryTable 행 개수: {categoryTable.Count}");
            var result = categoryTable.FindAll(x => x.Dispatch_ID == dispatchID).FirstOrDefault();

            if (result == null)
            {
                Debug.LogError($"[DispatchTestPanel] Dispatch_ID {dispatchID}에 해당하는 카테고리를 찾을 수 없습니다!");
            }
            else
            {
                Debug.Log($"[DispatchTestPanel] 찾은 카테고리: Dispatch_ID={result.Dispatch_ID}, Category={result.Dispatch_Category}");
            }

            return result;
        }

        /// <summary>
        /// 파견 실행 (보상 로직 테스트)
        /// </summary>
        private void ExecuteDispatch(DispatchLocation location)
        {
            var locationData = GetLocationData(location);
            if (locationData == null)
            {
                AddLog("❌ 장소 데이터를 찾을 수 없습니다!");
                return;
            }

            // 장소 상세 정보 출력
            AddLog($"🏛️ 장소 ID: {locationData.Dispatch_Location_ID}");
            AddLog($"📋 Dispatch ID: {locationData.Dispatch_ID}");

            // Dispatch_ID로 카테고리 조회
            var categoryData = GetCategoryData(locationData.Dispatch_ID);
            if (categoryData == null)
            {
                AddLog($"❌ Dispatch_ID {locationData.Dispatch_ID}에 대한 카테고리를 찾을 수 없습니다!");
                return;
            }

            string dispatchTypeName = ((DispatchType)categoryData.Dispatch_Category) == DispatchType.Combat ? "전투형" : "채집형";
            AddLog($"🎯 파견 타입: {dispatchTypeName}");
            AddLog($"⏰ 파견 시간: {currentSelectedHours}시간 (Time ID: {currentSelectedTimeID})");

            // 보상 데이터 가져오기
            var rewardData = GetRewardData(locationData.Dispatch_Location_ID, currentSelectedTimeID);
            if (rewardData == null)
            {
                AddLog("❌ 보상 데이터를 찾을 수 없습니다!");
                return;
            }

            AddLog($"💰 보상 배율: x{rewardData.Reward_Multiplier}");
            AddLog($"🎁 보상 그룹 ID: {rewardData.Reward_Group_ID}");

            // 보상 로직 실행 및 로그 출력
            LogRewardDetails(rewardData);

            // 파견 시작 (DispatchManager가 있는 경우에만)
            if (dispatchManager != null)
            {
                dispatchManager.StartDispatch
                (
                    locationData.Dispatch_Location_ID,
                    GetLocationName(location),
                    (DispatchType)categoryData.Dispatch_Category,
                    currentSelectedHours
                );
                AddLog("✅ 파견 시작!");
            }
            else
            {
                AddLog("⚠️ DispatchManager가 없어 파견은 시작되지 않았습니다. (보상 로직만 테스트)");
            }
        }

        /// <summary>
        /// 모든 팁 텍스트 비활성화
        /// </summary>
        private void HideAllTipTexts()
        {
            if (tipText1 != null) tipText1.SetActive(false);
            if (tipText2 != null) tipText2.SetActive(false);
            if (tipText3 != null) tipText3.SetActive(false);
            if (tipText4 != null) tipText4.SetActive(false);
            if (tipText5 != null) tipText5.SetActive(false);
        }

        /// <summary>
        /// 해당 창고의 팁 텍스트만 활성화
        /// </summary>
        private void ShowTipText(DispatchLocation location)
        {
            // 모든 팁 비활성화
            HideAllTipTexts();

            // 해당 창고의 팁만 활성화
            GameObject targetTip = location switch
            {
                DispatchLocation.NightmareWarehouse => tipText1,
                DispatchLocation.FateWarehouse => tipText2,
                DispatchLocation.LaughterWarehouse => tipText3,
                DispatchLocation.TruthWarehouse => tipText4,
                DispatchLocation.UnknownWarehouse => tipText5,
                _ => null
            };

            if (targetTip != null)
            {
                targetTip.SetActive(true);
                AddLog($"✓ {GetLocationName(location)} 팁 표시");
            }
        }

        /// <summary>
        /// 덱 캐릭터 로드 및 이미지 표시
        /// </summary>
        private void LoadDeckCharacters()
        {
            if (DeckManager.Instance == null)
            {
                AddLog("⚠️ DeckManager가 없습니다.");
                return;
            }

            AddLog("=== 덱 캐릭터 로드 시작 ===");

            // 덱의 4개 슬롯 순회
            for (int i = 0; i < 4; i++)
            {
                int characterId = DeckManager.Instance.GetCharacterAtIndex(i);
                Image targetImage = GetDeckImageByIndex(i);

                if (targetImage == null)
                {
                    AddLog($"⚠️ 덱 이미지 슬롯 {i + 1}이 할당되지 않았습니다.");
                    continue;
                }

                if (characterId > 0)
                {
                    // 캐릭터가 있으면 이미지 로드
                    LoadCharacterImageForSlot(i, characterId, targetImage);
                    targetImage.gameObject.SetActive(true);
                    AddLog($"✓ 슬롯 {i + 1}: 캐릭터 ID {characterId} 로드");
                }
                else
                {
                    // 빈 슬롯 처리 (비활성화)
                    targetImage.gameObject.SetActive(false);
                    AddLog($"✓ 슬롯 {i + 1}: 빈 슬롯 (비활성화)");
                }
            }

            AddLog("=== 덱 캐릭터 로드 완료 ===");
        }

        /// <summary>
        /// 인덱스로 덱 이미지 가져오기
        /// </summary>
        private Image GetDeckImageByIndex(int index)
        {
            return index switch
            {
                0 => deckCharacterImage1,
                1 => deckCharacterImage2,
                2 => deckCharacterImage3,
                3 => deckCharacterImage4,
                _ => null
            };
        }

        /// <summary>
        /// 캐릭터 이미지 로드 (Addressable)
        /// </summary>
        private void LoadCharacterImageForSlot(int slotIndex, int characterId, Image targetImage)
        {
            if (targetImage == null) return;

            // 현재는 모든 캐릭터가 같은 이미지 사용 ("ChaIcon")
            UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(AddressableKey.Icon_Character).Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    targetImage.sprite = handle.Result;
                    AddLog($"✓ 슬롯 {slotIndex + 1} 이미지 로드 성공");
                }
                else
                {
                    AddLog($"❌ 슬롯 {slotIndex + 1} 이미지 로드 실패");
                }
            };
        }

        /// <summary>
        /// 보상 상세 로그 출력
        /// </summary>
        private void LogRewardDetails(DispatchRewardTableData rewardData)
        {
            // 보상 그룹 데이터 가져오기
            var rewardGroupData = CSVLoader.Instance.GetData<RewardGroupData>(rewardData.Reward_Group_ID);
            if (rewardGroupData == null)
            {
                AddLog("❌ 보상 그룹 정보 없음");
                return;
            }

            AddLog("🎁 예상 보상:");

            // Reward_1_ID ~ Reward_5_ID 체크
            int[] rewardIDs = new int[]
            {
                rewardGroupData.Reward_1_ID,
                rewardGroupData.Reward_2_ID,
                rewardGroupData.Reward_3_ID,
                rewardGroupData.Reward_4_ID,
                rewardGroupData.Reward_5_ID
            };

            foreach (var rewardID in rewardIDs)
            {
                if (rewardID == 0) continue;

                var reward = CSVLoader.Instance.GetData<RewardData>(rewardID);
                if (reward != null)
                {
                    int minCount = Mathf.FloorToInt(reward.Min_Count * rewardData.Reward_Multiplier);
                    int maxCount = Mathf.FloorToInt(reward.Max_Count * rewardData.Reward_Multiplier);

                    string fixedText = reward.Is_Fixed ? "[고정]" : $"[{reward.Probability * 100:F0}%]";
                    AddLog($"  {fixedText} 아이템 ID {reward.Item_ID}: {minCount}~{maxCount}개");
                }
            }
        }

        /// <summary>
        /// 로그 추가 (콘솔 출력)
        /// </summary>
        private void AddLog(string message)
        {
            Debug.Log($"[DispatchTestPanel] {message}");
        }

        private void OnDestroy()
        {
            // 이벤트 리스너 제거
            if (timeSlider != null)
                timeSlider.onValueChanged.RemoveListener(OnTimeSliderChanged);

            if (dispatchStartButton != null)
                dispatchStartButton.onClick.RemoveListener(OnDispatchStartButtonClicked);
        }
    }
}
