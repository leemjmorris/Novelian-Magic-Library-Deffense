using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Managers;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.UI;

namespace Dispatch
{
    /// <summary>
    /// 전투형 파견 시스템 UI 패널
    /// CSV 데이터 기반 보상 시스템
    /// 전투형 장소별 보상 로직 테스트
    /// </summary>
    public class CombatDispatchPanel : MonoBehaviour
    {
        /// <summary>
        /// 파견 상태 저장 데이터
        /// </summary>
        [System.Serializable]
        private class DispatchSaveData
        {
            public bool isDispatching;
            public float totalDispatchTime; // 전체 파견 시간 (초)
            public string startTimeString; // 파견 시작 시간 (DateTime 직렬화)
            public int selectedLocation; // DispatchLocation enum 값 (int로 저장)
            public int selectedHours;
            public int selectedTimeID;
            public int dispatchType; // 파견 타입 (Combat=1, Gathering=2)
        }

        // 파견 타입별 저장 키 (전투형/채집형 분리)
        // 패널의 고정된 타입을 사용
        private string GetSaveKey()
        {
            return panelDispatchType == DispatchType.Combat
                ? "CombatDispatch_SaveData"
                : "GatheringDispatch_SaveData";
        }
        [Header("패널 타입 설정")]
        [SerializeField] private DispatchType panelDispatchType = DispatchType.Combat; // 이 패널의 파견 타입 (Inspector에서 고정)

        [Header("테스트 모드")]
        [SerializeField] private bool useTestMode = true; // true: 시간=초로 변환 (4시간→4초), false: 실제 시간 사용

        [Header("프리셋 선택")]
        [SerializeField] private DispatchPresetSelector presetSelector; // 프리셋 선택 컴포넌트

        [Header("파견 매니저 참조")]
        [SerializeField] private DispatchManager dispatchManager;
        [SerializeField] private CombatDispatchController combatDispatchController;
        [SerializeField] private CombatDispatchController gatheringDispatchController; // 채집형 디스패치 컨트롤러

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

        [Header("덱 슬롯 버튼 (빈 슬롯 클릭 시 덱 설정으로 이동)")]
        [SerializeField] private Button deckSlotButton1;
        [SerializeField] private Button deckSlotButton2;
        [SerializeField] private Button deckSlotButton3;
        [SerializeField] private Button deckSlotButton4;

        [Header("파견 실행 버튼")]
        [SerializeField] private Button dispatchStartButton;  // 파견하기 버튼
        [SerializeField] private TextMeshProUGUI dispatchButtonText;  // 버튼 텍스트
        [SerializeField] private TextMeshProUGUI countdownTimerText;  // 카운트다운 타이머 텍스트

        [SerializeField] private GameObject sliderObject;  // 슬라이더 오브젝트 (숨김 처리용)
        [SerializeField] private GameObject TipPanelObject;  // 팁표시 오브젝트 (숨김 처리용)

        [Header("보상 정보 패널")]
        [SerializeField] private GameObject infoPanel;  // 보상 정보 패널 (RewardInfoText 포함)
        [SerializeField] private Button[] infoImageButton;  // 보상 정보 버튼 (InfoImage)

        [Header("파견 횟수 표시")]
        [SerializeField] private TextMeshProUGUI dispatchCountText;  // 파견 횟수 텍스트 (예: "0/1", "1/1")

        [Header("파견별 아이템 프리뷰 (각 파견의 대표 아이템 슬롯)")]
        [SerializeField] private Image itemSlot1;  // 1번 파견 아이템 슬롯
        [SerializeField] private Image itemSlot2;  // 2번 파견 아이템 슬롯
        [SerializeField] private Image itemSlot3;  // 3번 파견 아이템 슬롯
        [SerializeField] private Image itemSlot4;  // 4번 파견 아이템 슬롯
        [SerializeField] private Image itemSlot5;  // 5번 파견 아이템 슬롯

        [Header("창고별 팁 텍스트 (5개)")]
        [SerializeField] private GameObject tipText1;  // 악몽의 창고 팁
        [SerializeField] private GameObject tipText2;  // 운명의 창고 팁
        [SerializeField] private GameObject tipText3;  // 웃음의 창고 팁
        [SerializeField] private GameObject tipText4;  // 진실의 창고 팁
        [SerializeField] private GameObject tipText5;  // 미지의 창고 팁
        [SerializeField] private GameObject tipText6;  // 마도 서고 정돈 팁
        [SerializeField] private GameObject tipText7;  // 마력 장벽 유지 검사 팁
        [SerializeField] private GameObject tipText8;  // 마도서 표지 복원 팁
        [SerializeField] private GameObject tipText9;  // 봉인구 안정성 확인 팁
        [SerializeField] private GameObject tipText10; // 마력 잔재 정화 팁

        [Header("스크롤 방향 화살표")]
        [SerializeField] private Image leftArrowImage;   // 왼쪽 화살표 (L-Image)
        [SerializeField] private Image rightArrowImage;  // 오른쪽 화살표 (R-Image)
        [SerializeField] private float arrowBlinkSpeed = 2f;  // 깜박임 속도
        [SerializeField] private float arrowMoveDistance = 10f;  // 화살표 이동 거리
        [SerializeField] private float arrowMoveSpeed = 3f;  // 화살표 이동 속도

        [Header("맵 선택 확대 애니메이션")]
        [SerializeField] private ScaleAnimator combatScaleAnimator1;
        [SerializeField] private ScaleAnimator combatScaleAnimator2;
        [SerializeField] private ScaleAnimator combatScaleAnimator3;
        [SerializeField] private ScaleAnimator combatScaleAnimator4;
        [SerializeField] private ScaleAnimator combatScaleAnimator5;
        [SerializeField] private ScaleAnimator collectionScaleAnimator1;
        [SerializeField] private ScaleAnimator collectionScaleAnimator2;
        [SerializeField] private ScaleAnimator collectionScaleAnimator3;
        [SerializeField] private ScaleAnimator collectionScaleAnimator4;
        [SerializeField] private ScaleAnimator collectionScaleAnimator5;

        private int currentSelectedHours = 4;
        private int currentSelectedTimeID;
        private List<DispatchTimeTableData> availableTimes;
        private DispatchLocation currentSelectedLocation = DispatchLocation.NightmareWarehouse;

        // 파견 상태 관리
        private bool isDispatching = false;
        private float remainingTime = 0f;
        private DispatchType currentDispatchType = DispatchType.Combat; // 현재 파견 타입

        // 스냅 스크롤 관련
        private int totalCombatButtons = 5;  // 전투형 5개
        private int totalGatheringButtons = 5; // 채집형 5개
        private int currentButtonIndex = 0;
        private bool isDragging = false;
        private float targetScrollPosition = 0f;
        private float scrollVelocity = 0f;

        // 아이템 아이콘 캐시 (Item_ID → Sprite)
        private Dictionary<int, Sprite> cachedItemIcons = new Dictionary<int, Sprite>();

        // 화살표 애니메이션용 변수
        private Vector3 leftArrowOriginalPos;
        private Vector3 rightArrowOriginalPos;
        private float arrowAnimTime = 0f;

        private void OnEnable()
        {
            // 저장된 파견 상태 복원
            LoadDispatchState();

            // 프리셋 변경 이벤트 구독
            if (presetSelector != null)
            {
                presetSelector.OnPresetSelected += OnPresetChanged;
            }

            // 현재 로컬 선택 프리셋에 맞게 덱 캐릭터 이미지 갱신
            // (씬 재진입 시 1번 프리셋으로 초기화되므로 해당 프리셋의 캐릭터로 표시)
            LoadDeckCharacters();
        }

        /// <summary>
        /// 프리셋 변경 시 호출 - 덱 캐릭터 이미지 갱신
        /// </summary>
        private void OnPresetChanged(int newPresetIndex)
        {
            AddLog($"프리셋 변경됨: {newPresetIndex + 1}");
            LoadDeckCharacters();
        }

        private void Start()
        {
            LoadCSVData();
            InitializeUI();
            SetupEventListeners();
            SetupLocationButtons();

            // 초기 UI 상태 설정
            if (!isDispatching && countdownTimerText != null)
                countdownTimerText.gameObject.SetActive(false);

            // 모든 팁 텍스트 초기 비활성화
            HideAllTipTexts();

            // 스크롤뷰를 맨 왼쪽으로 이동 (파견 중이 아닐 때만)
            if (!isDispatching && buttonScrollRect != null)
                buttonScrollRect.horizontalNormalizedPosition = 0f;

            // 덱 캐릭터 로드
            LoadDeckCharacters();

            // 모든 파견의 아이템 프리뷰 초기화 (아이콘 로드)
            InitializeAllItemPreviews();

            // 파견 횟수 텍스트 업데이트
            UpdateDispatchCountText();

            // 패널 타입에 따라 첫 번째 장소 설정 (파견 중이 아닐 때만)
            if (!isDispatching)
            {
                DispatchLocation initialLocation = panelDispatchType == DispatchType.Combat
                    ? DispatchLocation.NightmareWarehouse
                    : DispatchLocation.MagicLibraryOrganization;

                currentSelectedLocation = initialLocation;
                ShowTipText(initialLocation);
                UpdateTimeDisplay(0);

                // 초기 선택 애니메이션 적용 (즉시 적용)
                ApplySelectionAnimationImmediate(initialLocation);
            }

            // 화살표 초기 위치 저장
            InitializeArrows();

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

                // 스크롤 이동 중일 때 창고 변경 감지 (화살표 클릭으로 이동 시)
                if (!isDispatching)
                {
                    CheckAndUpdateWarehouse();
                }
            }

            // 스와이프 중일 때 실시간으로 창고 변경 감지 (파견 중이 아닐 때만)
            if (buttonScrollRect != null && isDragging && !isDispatching)
            {
                CheckAndUpdateWarehouse();
            }

            // 화살표 애니메이션 업데이트
            UpdateArrowAnimation();
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

            // 파견 중이 아닐 때만 슬라이더 초기화 (파견 중이면 저장된 시간 유지)
            if (!isDispatching)
            {
                timeSlider.value = 0;
                UpdateTimeDisplay(0);
            }
            else
            {
                // 파견 중일 때는 저장된 시간으로 슬라이더 설정
                int sliderIndex = GetSliderIndexFromHours(currentSelectedHours);
                timeSlider.value = sliderIndex;
                UpdateTimeDisplay(sliderIndex);
            }
        }

        /// <summary>
        /// 시간(hours)으로부터 슬라이더 인덱스 조회
        /// </summary>
        private int GetSliderIndexFromHours(int hours)
        {
            if (availableTimes == null) return 0;

            for (int i = 0; i < availableTimes.Count; i++)
            {
                if ((int)availableTimes[i].Required_Hours == hours)
                {
                    return i;
                }
            }
            return 0; // 찾지 못하면 기본값 0
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

            // 보상 정보 버튼 이벤트 등록 (각 버튼별로 해당 파견 정보 표시)
            if (infoImageButton != null && infoImageButton.Length > 0)
            {
                for (int i = 0; i < infoImageButton.Length; i++)
                {
                    if (infoImageButton[i] != null)
                    {
                        int index = i; // 클로저 캡처용
                        infoImageButton[i].onClick.AddListener(() => OnInfoImageButtonClicked(index));
                    }
                }
            }

            // InfoPanel 클릭 시 닫기 이벤트 등록
            if (infoPanel != null)
            {
                var infoPanelButton = infoPanel.GetComponent<Button>();
                if (infoPanelButton == null)
                {
                    infoPanelButton = infoPanel.AddComponent<Button>();
                    // 버튼 시각적 효과 제거 (투명하게)
                    infoPanelButton.transition = Selectable.Transition.None;
                }
                infoPanelButton.onClick.AddListener(OnInfoPanelClicked);
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

            // 화살표 버튼 클릭 이벤트 등록
            SetupArrowButtonEvents();

            // 아이템 슬롯 버튼 이벤트 등록 (각 슬롯 클릭 시 해당 파견의 보상 정보 패널 표시)
            SetupItemSlotButtons();

            // CBL: 덱 슬롯 버튼 이벤트 등록 (빈 슬롯 클릭 시 덱 설정으로 이동)
            SetupDeckSlotButtons();
        }

        /// <summary>
        /// 화살표 버튼 클릭 이벤트 설정
        /// </summary>
        private void SetupArrowButtonEvents()
        {
            // 왼쪽 화살표 버튼 설정
            if (leftArrowImage != null)
            {
                var leftButton = leftArrowImage.GetComponent<Button>();
                if (leftButton == null)
                {
                    leftButton = leftArrowImage.gameObject.AddComponent<Button>();
                    leftButton.transition = Selectable.Transition.None;
                }
                leftButton.onClick.AddListener(OnLeftArrowClicked);
            }

            // 오른쪽 화살표 버튼 설정
            if (rightArrowImage != null)
            {
                var rightButton = rightArrowImage.GetComponent<Button>();
                if (rightButton == null)
                {
                    rightButton = rightArrowImage.gameObject.AddComponent<Button>();
                    rightButton.transition = Selectable.Transition.None;
                }
                rightButton.onClick.AddListener(OnRightArrowClicked);
            }
        }

        /// <summary>
        /// 왼쪽 화살표 클릭 - 이전 파견 위치로 이동
        /// </summary>
        private void OnLeftArrowClicked()
        {
            // 파견 중에는 이동 불가
            if (isDispatching) return;

            int totalButtons = panelDispatchType == DispatchType.Combat ? totalCombatButtons : totalGatheringButtons;

            if (currentButtonIndex > 0)
            {
                currentButtonIndex--;
                // 스와이프와 동일하게 targetScrollPosition 설정
                targetScrollPosition = (float)currentButtonIndex / (totalButtons - 1);
                AddLog($"⬅️ 왼쪽 화살표 클릭: 인덱스 {currentButtonIndex}");
            }
        }

        /// <summary>
        /// 오른쪽 화살표 클릭 - 다음 파견 위치로 이동
        /// </summary>
        private void OnRightArrowClicked()
        {
            // 파견 중에는 이동 불가
            if (isDispatching) return;

            int totalButtons = panelDispatchType == DispatchType.Combat ? totalCombatButtons : totalGatheringButtons;

            if (currentButtonIndex < totalButtons - 1)
            {
                currentButtonIndex++;
                // 스와이프와 동일하게 targetScrollPosition 설정
                targetScrollPosition = (float)currentButtonIndex / (totalButtons - 1);
                AddLog($"➡️ 오른쪽 화살표 클릭: 인덱스 {currentButtonIndex}");
            }
        }

        /// <summary>
        /// 아이템 슬롯에 버튼 컴포넌트 추가 및 이벤트 등록
        /// </summary>
        private void SetupItemSlotButtons()
        {
            Debug.Log($"[DispatchPanel] SetupItemSlotButtons 호출됨!");

            Image[] itemSlots = { itemSlot1, itemSlot2, itemSlot3, itemSlot4, itemSlot5 };

            Debug.Log($"[DispatchPanel] itemSlot1={itemSlot1}, itemSlot2={itemSlot2}, itemSlot3={itemSlot3}, itemSlot4={itemSlot4}, itemSlot5={itemSlot5}");

            for (int i = 0; i < itemSlots.Length; i++)
            {
                if (itemSlots[i] == null)
                {
                    Debug.LogWarning($"[DispatchPanel] ItemSlot{i + 1}이 null입니다! Inspector에서 연결해주세요.");
                    continue;
                }

                // ItemSlotClickHandler를 사용하여 클릭 이벤트 등록 (ScrollRect 내부에서도 안정적으로 작동)
                var clickHandler = itemSlots[i].GetComponent<ItemSlotClickHandler>();
                if (clickHandler == null)
                {
                    clickHandler = itemSlots[i].gameObject.AddComponent<ItemSlotClickHandler>();
                    Debug.Log($"[DispatchPanel] ItemSlot{i + 1}에 ItemSlotClickHandler 컴포넌트 추가됨");
                }

                clickHandler.SlotIndex = i;

                // 기존 이벤트 제거 후 새로 등록
                clickHandler.OnSlotClicked -= OnItemSlotClicked;
                clickHandler.OnSlotClicked += OnItemSlotClicked;

                Debug.Log($"[DispatchPanel] ItemSlot{i + 1} ItemSlotClickHandler 등록 완료");
            }
        }

        /// <summary>
        /// 덱 슬롯 버튼 이벤트 설정 (빈 슬롯 클릭 시 덱 설정으로 이동)
        /// </summary>
        private void SetupDeckSlotButtons()
        {
            Button[] deckSlotButtons = { deckSlotButton1, deckSlotButton2, deckSlotButton3, deckSlotButton4 };

            for (int i = 0; i < deckSlotButtons.Length; i++)
            {
                if (deckSlotButtons[i] == null) continue;

                int slotIndex = i; // 클로저 캡처용
                deckSlotButtons[i].onClick.AddListener(() => OnDeckSlotButtonClicked(slotIndex));
            }
        }

        /// <summary>
        /// 덱 슬롯 버튼 클릭 시 덱 설정 씬으로 이동
        /// </summary>
        private void OnDeckSlotButtonClicked(int slotIndex)
        {
            Debug.Log($"[DispatchPanel] OnDeckSlotButtonClicked 호출됨! slotIndex={slotIndex}");
            AddLog($"덱 슬롯 {slotIndex + 1} 클릭 - 덱 설정으로 이동");
            NavigateToLibraryManagementScene().Forget();
        }

        /// <summary>
        /// LibraryManagementScene으로 씬 전환 (페이드 효과)
        /// 덱 설정 탭으로 바로 이동
        /// </summary>
        private async UniTaskVoid NavigateToLibraryManagementScene()
        {
            // CBL: 씬 전환 전 덱 설정 탭 바로 열기 플래그 설정
            NovelianMagicLibraryDefense.UI.TabButton.ShouldOpenTeamSetup = true;
            Debug.Log($"[DispatchPanel] ShouldOpenTeamSetup 플래그 설정됨: {NovelianMagicLibraryDefense.UI.TabButton.ShouldOpenTeamSetup}");

            // FadeController의 LoadSceneWithFade 사용 (깜빡임 방지)
            if (FadeController.Instance != null)
            {
                await FadeController.Instance.LoadSceneWithFade(SceneName.LibraryManagementScene);
            }
            else
            {
                Debug.LogWarning("[DispatchPanel] FadeController not available, loading scene directly");
                await SceneManager.LoadSceneAsync(SceneName.LibraryManagementScene);
            }
        }

        /// <summary>
        /// 덱 슬롯 버튼 활성화/비활성화 업데이트
        /// 빈 슬롯일 때만 버튼 활성화
        /// </summary>
        private void UpdateDeckSlotButtons()
        {
            if (DeckManager.Instance == null) return;

            var localDeck = presetSelector != null ? presetSelector.GetLocalSelectedDeck() : DeckManager.Instance.GetDeck();
            Button[] deckSlotButtons = { deckSlotButton1, deckSlotButton2, deckSlotButton3, deckSlotButton4 };

            // 프리셋에 캐릭터가 하나라도 있는지 확인
            bool hasAnyCharacter = false;
            foreach (int id in localDeck)
            {
                if (id > 0)
                {
                    hasAnyCharacter = true;
                    break;
                }
            }

            // 프리셋에 캐릭터가 하나라도 있으면 모든 버튼 비활성화
            // 프리셋이 완전히 비어있을 때만 버튼 표시
            for (int i = 0; i < deckSlotButtons.Length; i++)
            {
                if (deckSlotButtons[i] == null) continue;

                if (hasAnyCharacter)
                {
                    // 프리셋에 캐릭터가 있으면 모든 버튼 비활성화
                    deckSlotButtons[i].interactable = false;
                    deckSlotButtons[i].gameObject.SetActive(false);
                }
                else
                {
                    // 프리셋이 완전히 비어있으면 버튼 활성화
                    deckSlotButtons[i].interactable = true;
                    deckSlotButtons[i].gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 아이템 슬롯 클릭 시 해당 파견의 보상 정보 패널 표시
        /// </summary>
        private void OnItemSlotClicked(int slotIndex)
        {
            Debug.Log($"[DispatchPanel] OnItemSlotClicked 호출됨! slotIndex={slotIndex}");

            // 슬롯 인덱스에 해당하는 파견 장소 결정
            DispatchLocation location = GetLocationBySlotIndex(slotIndex);
            Debug.Log($"[DispatchPanel] 파견 장소: {location}");

            // 파견 중일 때는 현재 파견 중인 장소의 보상 정보만 표시
            if (isDispatching)
            {
                location = currentSelectedLocation;
            }

            // 해당 장소의 보상 정보로 패널 업데이트
            UpdateRewardInfoForLocation(location);

            // 파견 중이면 rewardInfoText 활성화 (UpdateDispatchUI에서 숨겨졌으므로)
            if (isDispatching && rewardInfoText != null)
            {
                rewardInfoText.gameObject.SetActive(true);
            }

            // 보상 정보 패널 표시
            if (infoPanel != null)
            {
                infoPanel.SetActive(true);
                AddLog($" 아이템 슬롯 {slotIndex + 1} 클릭 - {GetLocationName(location)} 보상 정보 표시");
            }
            else
            {
                Debug.LogWarning("[DispatchPanel] infoPanel이 null입니다!");
            }
        }

        /// <summary>
        /// 슬롯 인덱스로 파견 장소 반환
        /// </summary>
        private DispatchLocation GetLocationBySlotIndex(int slotIndex)
        {
            if (panelDispatchType == DispatchType.Combat)
            {
                return slotIndex switch
                {
                    0 => DispatchLocation.NightmareWarehouse,
                    1 => DispatchLocation.FateWarehouse,
                    2 => DispatchLocation.LaughterWarehouse,
                    3 => DispatchLocation.TruthWarehouse,
                    4 => DispatchLocation.UnknownWarehouse,
                    _ => DispatchLocation.NightmareWarehouse
                };
            }
            else
            {
                return slotIndex switch
                {
                    0 => DispatchLocation.MagicLibraryOrganization,
                    1 => DispatchLocation.MagicBarrierInspection,
                    2 => DispatchLocation.SpellbookCoverRestoration,
                    3 => DispatchLocation.SealStabilityCheck,
                    4 => DispatchLocation.MagicResiduePurification,
                    _ => DispatchLocation.MagicLibraryOrganization
                };
            }
        }

        /// <summary>
        /// 특정 파견 장소의 보상 정보로 패널 업데이트
        /// </summary>
        private void UpdateRewardInfoForLocation(DispatchLocation location)
        {
            if (rewardInfoText == null) return;

            var locationData = GetLocationData(location);
            if (locationData == null)
            {
                Debug.LogError($"[DispatchPanel] locationData null - location: {location}");
                rewardInfoText.text = "보상 정보를 불러올 수 없습니다.";
                return;
            }

            // 현재 선택된 시간의 보상 데이터 사용 (파견 중이면 파견 시작 시의 시간)
            int timeID = currentSelectedTimeID > 0 ? currentSelectedTimeID : 5201;
            Debug.Log($"[DispatchPanel] UpdateRewardInfoForLocation - location: {location}, locationID: {locationData.Dispatch_Location_ID}, timeID: {timeID}, hours: {currentSelectedHours}");

            var rewardTableData = GetRewardData(locationData.Dispatch_Location_ID, timeID);
            if (rewardTableData == null)
            {
                Debug.LogError($"[DispatchPanel] rewardTableData null - locationID: {locationData.Dispatch_Location_ID}, timeID: {timeID}");
                rewardInfoText.text = $"보상 정보를 불러올 수 없습니다.\n(Location: {locationData.Dispatch_Location_ID}, TimeID: {timeID})";
                return;
            }

            // 보상 정보 표시 (Reward_Multiplier 적용: 4시간=1배, 8시간=1.8배, 12시간=2.6배, 23시간=5배)
            DisplayRewardInfoForLocation(location, rewardTableData, rewardTableData.Reward_Multiplier);
        }

        /// <summary>
        /// 특정 장소의 보상 정보 텍스트 생성 및 표시
        /// </summary>
        private void DisplayRewardInfoForLocation(DispatchLocation location, DispatchRewardTableData rewardTableData, float rewardMultiplier)
        {
            if (rewardInfoText == null || rewardTableData == null) return;

            var rewardGroupData = CSVLoader.Instance.GetData<RewardGroupData>(rewardTableData.Reward_Group_ID);
            if (rewardGroupData == null)
            {
                rewardInfoText.text = "보상 그룹 정보를 찾을 수 없습니다.";
                return;
            }

            string locationName = GetLocationName(location);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{locationName}</b>");
            sb.AppendLine($"<color=#88CCFF> {currentSelectedHours}시간</color>");
            sb.AppendLine("<color=#AAAAAA>───────────────</color>");
            sb.AppendLine("<b>예상 보상:</b>");

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

                var rewardData = CSVLoader.Instance.GetData<RewardData>(rewardID);
                if (rewardData == null) continue;

                string itemName = GetItemName(rewardData.Item_ID);
                int minCount = Mathf.RoundToInt(rewardData.Min_Count * rewardMultiplier);
                int maxCount = Mathf.RoundToInt(rewardData.Max_Count * rewardMultiplier);

                if (rewardData.Is_Fixed)
                {
                    sb.AppendLine($"• {itemName} {minCount}~{maxCount}개");
                }
                else
                {
                    int probability = Mathf.RoundToInt(rewardData.Probability * 100f);
                    sb.AppendLine($"• <color=#FFD700>[{probability}%]</color> {itemName} {minCount}~{maxCount}개");
                }
            }

            rewardInfoText.text = sb.ToString();
        }

        /// <summary>
        /// 보상 정보 버튼 클릭 시 (해당 파견의 보상 정보 표시)
        /// </summary>
        private void OnInfoImageButtonClicked(int buttonIndex)
        {
            if (infoPanel == null) return;

            // 버튼 인덱스에 해당하는 파견 장소 결정
            DispatchLocation location = GetLocationBySlotIndex(buttonIndex);

            // 파견 중일 때는 현재 파견 중인 장소의 보상 정보만 표시
            if (isDispatching)
            {
                location = currentSelectedLocation;
            }

            // 해당 장소의 보상 정보로 패널 업데이트
            UpdateRewardInfoForLocation(location);

            // 파견 중이면 rewardInfoText 활성화 (UpdateDispatchUI에서 숨겨졌으므로)
            if (isDispatching && rewardInfoText != null)
            {
                rewardInfoText.gameObject.SetActive(true);
            }

            // 보상 정보 패널 표시
            infoPanel.SetActive(true);
            AddLog($"ℹ️ 버튼 {buttonIndex + 1} 클릭 - {GetLocationName(location)} 보상 정보 표시");
        }

        /// <summary>
        /// InfoPanel 클릭 시 닫기
        /// </summary>
        private void OnInfoPanelClicked()
        {
            if (infoPanel != null && infoPanel.activeSelf)
            {
                infoPanel.SetActive(false);

                // 파견 중이면 rewardInfoText 다시 숨기기
                if (isDispatching && rewardInfoText != null)
                {
                    rewardInfoText.gameObject.SetActive(false);
                }

                AddLog("ℹ️ 보상 정보 패널 닫힘 (패널 클릭)");
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

            // 패널 타입으로 전투형/채집형 판단
            bool isCombatPanel = panelDispatchType == DispatchType.Combat;

            int totalButtons = isCombatPanel ? totalCombatButtons : totalGatheringButtons;
            currentButtonIndex = Mathf.RoundToInt(currentPos * (totalButtons - 1));
            currentButtonIndex = Mathf.Clamp(currentButtonIndex, 0, totalButtons - 1);

            // 타겟 위치 설정
            targetScrollPosition = (float)currentButtonIndex / (totalButtons - 1);
        }

        /// <summary>
        /// 스크롤 위치에 따라 창고를 확인하고 UI 업데이트
        /// </summary>
        private void CheckAndUpdateWarehouse()
        {
            if (buttonScrollRect == null) return;

            // 파견 중에는 장소 변경 불가
            if (isDispatching) return;

            // 현재 스크롤 위치에서 가장 가까운 버튼 인덱스 계산
            float currentPos = buttonScrollRect.horizontalNormalizedPosition;

            // 패널 타입으로 전투형/채집형 판단
            bool isCombatPanel = panelDispatchType == DispatchType.Combat;

            int totalButtons = isCombatPanel ? totalCombatButtons : totalGatheringButtons;
            int newButtonIndex = Mathf.RoundToInt(currentPos * (totalButtons - 1));
            newButtonIndex = Mathf.Clamp(newButtonIndex, 0, totalButtons - 1);

            // 인덱스에 따라 창고 위치 결정 (전투형/채집형 구분)
            DispatchLocation newLocation;

            if (isCombatPanel)
            {
                // 전투형 장소 (1-5)
                newLocation = newButtonIndex switch
                {
                    0 => DispatchLocation.NightmareWarehouse,
                    1 => DispatchLocation.FateWarehouse,
                    2 => DispatchLocation.LaughterWarehouse,
                    3 => DispatchLocation.TruthWarehouse,
                    4 => DispatchLocation.UnknownWarehouse,
                    _ => DispatchLocation.NightmareWarehouse
                };
            }
            else
            {
                // 채집형 장소 (6-10)
                newLocation = newButtonIndex switch
                {
                    0 => DispatchLocation.MagicLibraryOrganization,
                    1 => DispatchLocation.MagicBarrierInspection,
                    2 => DispatchLocation.SpellbookCoverRestoration,
                    3 => DispatchLocation.SealStabilityCheck,
                    4 => DispatchLocation.MagicResiduePurification,
                    _ => DispatchLocation.MagicLibraryOrganization
                };
            }

            // 창고가 변경되었을 때만 업데이트
            if (newButtonIndex != currentButtonIndex || currentSelectedLocation != newLocation)
            {
                currentButtonIndex = newButtonIndex;

                // OnLocationButtonClicked()를 호출하여 버튼 클릭과 동일한 로직 사용
                OnLocationButtonClicked(newLocation);

                AddLog($"📍 스와이프로 장소 변경: {GetLocationName(newLocation)}");
            }
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
            // 파견 중에는 장소 변경 불가
            if (isDispatching) return;

            currentSelectedLocation = location;

            AddLog($"📍 선택된 장소: {GetLocationName(location)}");

            // 선택 확대 애니메이션 적용
            ApplySelectionAnimation(location);

            // 해당 창고의 팁 텍스트 표시
            ShowTipText(location);

            // UI 업데이트 (보상 정보만 표시)
            UpdateTimeDisplay(Mathf.RoundToInt(timeSlider.value));
        }

        /// <summary>
        /// 선택된 맵에 확대 애니메이션 적용
        /// </summary>
        private void ApplySelectionAnimation(DispatchLocation selectedLocation)
        {
            if (panelDispatchType == DispatchType.Combat)
            {
                // 전투형 애니메이터 배열
                ScaleAnimator[] animators = { combatScaleAnimator1, combatScaleAnimator2, combatScaleAnimator3, combatScaleAnimator4, combatScaleAnimator5 };
                DispatchLocation[] locations = { DispatchLocation.NightmareWarehouse, DispatchLocation.FateWarehouse, DispatchLocation.LaughterWarehouse, DispatchLocation.TruthWarehouse, DispatchLocation.UnknownWarehouse };

                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] == null) continue;

                    if (locations[i] == selectedLocation)
                    {
                        animators[i].PlaySelect();
                    }
                    else
                    {
                        animators[i].PlayDeselect();
                    }
                }
            }
            else
            {
                // 채집형 애니메이터 배열
                ScaleAnimator[] animators = { collectionScaleAnimator1, collectionScaleAnimator2, collectionScaleAnimator3, collectionScaleAnimator4, collectionScaleAnimator5 };
                DispatchLocation[] locations = { DispatchLocation.MagicLibraryOrganization, DispatchLocation.MagicBarrierInspection, DispatchLocation.SpellbookCoverRestoration, DispatchLocation.SealStabilityCheck, DispatchLocation.MagicResiduePurification };

                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] == null) continue;

                    if (locations[i] == selectedLocation)
                    {
                        animators[i].PlaySelect();
                    }
                    else
                    {
                        animators[i].PlayDeselect();
                    }
                }
            }
        }

        /// <summary>
        /// 선택된 맵에 확대 애니메이션 즉시 적용 (초기화용)
        /// </summary>
        private void ApplySelectionAnimationImmediate(DispatchLocation selectedLocation)
        {
            if (panelDispatchType == DispatchType.Combat)
            {
                ScaleAnimator[] animators = { combatScaleAnimator1, combatScaleAnimator2, combatScaleAnimator3, combatScaleAnimator4, combatScaleAnimator5 };
                DispatchLocation[] locations = { DispatchLocation.NightmareWarehouse, DispatchLocation.FateWarehouse, DispatchLocation.LaughterWarehouse, DispatchLocation.TruthWarehouse, DispatchLocation.UnknownWarehouse };

                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] == null) continue;

                    if (locations[i] == selectedLocation)
                    {
                        animators[i].SetSelectedImmediate();
                    }
                    else
                    {
                        animators[i].SetDeselectedImmediate();
                    }
                }
            }
            else
            {
                ScaleAnimator[] animators = { collectionScaleAnimator1, collectionScaleAnimator2, collectionScaleAnimator3, collectionScaleAnimator4, collectionScaleAnimator5 };
                DispatchLocation[] locations = { DispatchLocation.MagicLibraryOrganization, DispatchLocation.MagicBarrierInspection, DispatchLocation.SpellbookCoverRestoration, DispatchLocation.SealStabilityCheck, DispatchLocation.MagicResiduePurification };

                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] == null) continue;

                    if (locations[i] == selectedLocation)
                    {
                        animators[i].SetSelectedImmediate();
                    }
                    else
                    {
                        animators[i].SetDeselectedImmediate();
                    }
                }
            }
        }

        /// <summary>
        /// 파견하기 버튼 클릭 시 (실제 파견 실행)
        /// </summary>
        private void OnDispatchStartButtonClicked()
        {
            // 보상 정보 패널이 열려있으면 닫기
            CloseInfoPanelIfOpen();

            if (isDispatching)
            {
                // 파견 완료 - 보상 획득
                OnClaimReward();
            }
            else
            {
                // 파견 시작 전 유효성 검사
                if (!ValidateDispatchConditions())
                {
                    return;
                }

                // 파견 시작
                StartDispatch();
            }
        }

        /// <summary>
        /// 보상 정보 패널이 열려있으면 닫기
        /// </summary>
        private void CloseInfoPanelIfOpen()
        {
            if (infoPanel != null && infoPanel.activeSelf)
            {
                infoPanel.SetActive(false);

                // 파견 중이면 rewardInfoText도 숨기기
                if (isDispatching && rewardInfoText != null)
                {
                    rewardInfoText.gameObject.SetActive(false);
                }

                AddLog("ℹ️ 보상 정보 패널 닫힘 (버튼 클릭)");
            }
        }

        /// <summary>
        /// 파견 시작 전 유효성 검사
        /// </summary>
        /// <returns>유효하면 true, 아니면 false</returns>
        private bool ValidateDispatchConditions()
        {
            if (presetSelector == null)
            {
                AddLog("⚠️ 프리셋 선택기가 없습니다.");
                return true; // 프리셋 선택기가 없으면 검사 스킵
            }

            // 1. 프리셋이 비어있는지 확인
            var deck = presetSelector.GetLocalSelectedDeck();
            int validCharacterCount = 0;
            foreach (int charId in deck)
            {
                if (charId > 0) validCharacterCount++;
            }

            if (validCharacterCount == 0)
            {
                // 프리셋이 완전히 비어있음
                if (WarningUIManager.Instance != null)
                {
                    WarningUIManager.Instance.ShowWarning("프리셋이 비어있습니다.");
                }
                AddLog("❌ 파견 불가: 프리셋이 비어있습니다.");
                return false;
            }

            // 2. 3명 이상인지 확인
            if (validCharacterCount < 3)
            {
                if (WarningUIManager.Instance != null)
                {
                    WarningUIManager.Instance.ShowWarning("3명부터 파견이 가능합니다.");
                }
                AddLog($"❌ 파견 불가: 캐릭터 {validCharacterCount}명 (최소 3명 필요)");
                return false;
            }

            // 3. 다른 파견에서 사용 중인 프리셋인지 확인
            if (presetSelector.IsLocalSelectedPresetUsedByOtherDispatch())
            {
                if (WarningUIManager.Instance != null)
                {
                    WarningUIManager.Instance.ShowWarning("이미 파견중인 캐릭터가 있습니다!");
                }
                AddLog("❌ 파견 불가: 다른 파견에서 사용 중인 프리셋입니다.");
                return false;
            }

            AddLog("✅ 파견 조건 검사 통과");
            return true;
        }

        /// <summary>
        /// 파견 시작
        /// </summary>
        private void StartDispatch()
        {
            AddLog("\n==============================================");
            AddLog($"🚀 파견 시작 버튼 클릭!");

            // 서버 시간 체크
            if (ServerTimeManager.Instance == null || !ServerTimeManager.Instance.IsSynced)
            {
                AddLog("❌ 서버 시간이 동기화되지 않아 파견을 시작할 수 없습니다.");
                return;
            }

            // 파견 실행 및 보상 로직 콘솔 출력
            ExecuteDispatch(currentSelectedLocation);

            // 파견 시작 시간 기록 (서버 시간 기준)
            dispatchStartTimeMs = ServerTimeManager.Instance.GetServerTimeMs();
            dispatchStartTime = ServerTimeManager.Instance.GetServerDateTime();

            // 파견 시작 상태로 전환
            isDispatching = true;

            // 프리셋 선택 적용 (로컬 선택을 DeckManager에 저장), 변경 잠금
            if (presetSelector != null)
            {
                presetSelector.ApplyPresetSelection();
                presetSelector.Lock();
            }

            // 파견 상태 Firebase에 저장 (프리셋 인덱스 포함)
            SaveDispatchState();

            // Firebase 저장 후 UI 갱신 (프리셋 정보 텍스트 표시)
            if (presetSelector != null)
            {
                presetSelector.RefreshPresetMarks();
            }

            // 북마크 파견 시간 감소 modifier 적용
            float reducedTimeHours = RewardHelper.CalculateDispatchTime(currentSelectedHours);

            // 테스트 모드: 시간=초로 변환 (4시간→4초), 실제 모드: 시간→초 변환
            if (useTestMode)
            {
                remainingTime = reducedTimeHours; // 테스트: 시간 값을 그대로 초로 사용
                AddLog($"⏰ [테스트 모드] 파견 시작: {remainingTime:F1}초 후 완료 예정 (원본: {currentSelectedHours}시간)");
            }
            else
            {
                remainingTime = reducedTimeHours * 3600f; // 실제: 시간 → 초 변환
                AddLog($"⏰ 파견 시작: {reducedTimeHours:F1}시간 ({remainingTime:F0}초) 후 완료 예정 (원본: {currentSelectedHours}시간)");
            }

            // UI 업데이트
            UpdateDispatchUI();
            UpdateDispatchCountText();  // 파견 횟수 텍스트 업데이트 (0/1 → 1/1)
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

            // 스크롤 비활성화 (파견 중에는 스와이프 불가)
            if (buttonScrollRect != null)
                buttonScrollRect.enabled = false;

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

            // 패널 타입에 맞는 DispatchController에게 파견 완료 알림 (Red Dot 활성화)
            if (panelDispatchType == DispatchType.Combat)
            {
                if (combatDispatchController != null)
                {
                    combatDispatchController.OnDispatchCompleted();
                    AddLog("✅ 전투형 파견 완료 - Red Dot 활성화");
                }
            }
            else if (panelDispatchType == DispatchType.Gathering)
            {
                if (gatheringDispatchController != null)
                {
                    gatheringDispatchController.OnDispatchCompleted();
                    AddLog("✅ 채집형 파견 완료 - Red Dot 활성화");
                }
            }
        }

        /// <summary>
        /// 보상 획득 버튼 클릭 시
        /// </summary>
        private void OnClaimReward()
        {
            // 파견 보상 획득 사운드 재생
            AudioManager.Instance?.PlaySFX("Dispatch_result");

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

                        // 실제 보상 드랍 계산 및 출력
                        CalculateAndDropRewards(rewardData);
                    }

                    AddLog($"✅ 완료 시간: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }
            }

            AddLog("✅ 보상이 인벤토리에 추가되었습니다.");

            // 패널 타입에 맞는 DispatchController에게 보상 획득 알림 (Red Dot 비활성화)
            if (panelDispatchType == DispatchType.Combat)
            {
                if (combatDispatchController != null)
                {
                    combatDispatchController.OnRewardClaimed();
                    AddLog("✅ 전투형 보상 획득 - Red Dot 비활성화");
                }
            }
            else if (panelDispatchType == DispatchType.Gathering)
            {
                if (gatheringDispatchController != null)
                {
                    gatheringDispatchController.OnRewardClaimed();
                    AddLog("✅ 채집형 보상 획득 - Red Dot 비활성화");
                }
            }

            // 저장된 파견 상태 삭제 (먼저 삭제해야 RefreshPresetMarks에서 파견 중이 아님을 인식)
            ClearDispatchState();

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

            // 프리셋 변경 잠금 해제 및 UI 갱신
            if (presetSelector != null)
            {
                presetSelector.Unlock();
                presetSelector.RefreshPresetMarks();
            }

            // 파견 횟수 텍스트 업데이트 (1/1 → 0/1)
            UpdateDispatchCountText();

            // 슬라이더 다시 표시
            if (sliderObject != null)
                sliderObject.SetActive(true);
            //시간 선택 텍스트 표시
            if (selectedTimeText != null)
                selectedTimeText.gameObject.SetActive(true);
            //팁 표시 다시 표시
            if (TipPanelObject != null)
                TipPanelObject.SetActive(true);

            // 현재 선택된 창고의 팁 텍스트 표시
            ShowTipText(currentSelectedLocation);

            // 카운트다운 타이머 숨김
            if (countdownTimerText != null)
                countdownTimerText.gameObject.SetActive(false);

            // 버튼 텍스트 복원
            if (dispatchButtonText != null)
                dispatchButtonText.text = "파견하기";

            if (dispatchStartButton != null)
                dispatchStartButton.interactable = true;

            // 스크롤 다시 활성화
            if (buttonScrollRect != null)
                buttonScrollRect.enabled = true;
        }

        /// <summary>
        /// 슬라이더 값 변경 시
        /// </summary>
        private void OnTimeSliderChanged(float value)
        {
            int index = Mathf.RoundToInt(value);
            UpdateTimeDisplay(index);

            // 정보 패널이 열려있으면 자동으로 업데이트
            if (infoPanel != null && infoPanel.activeSelf)
            {
                UpdateRewardInfoForLocation(currentSelectedLocation);
            }
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
        /// 보상 정보 표시 (텍스트만 - 아이콘은 InitializeAllItemPreviews에서 이미 로드됨)
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

                    // 아이템 이름 가져오기
                    string itemName = GetItemName(reward.Item_ID);

                    string fixedText = reward.Is_Fixed ? "" : $"[{reward.Probability * 100:F0}%]";
                    rewardTexts.Add($"{fixedText} {itemName} {minCount}~{maxCount}개");
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
        /// 인덱스로 아이템 슬롯 가져오기 (단일 Image)
        /// </summary>
        private Image GetItemSlotByIndex(int index)
        {
            return index switch
            {
                0 => itemSlot1,
                1 => itemSlot2,
                2 => itemSlot3,
                3 => itemSlot4,
                4 => itemSlot5,
                _ => null
            };
        }

        /// <summary>
        /// 모든 파견의 아이템 프리뷰 초기화 (시작 시 모든 장소의 대표 보상 아이콘 로드)
        /// </summary>
        private void InitializeAllItemPreviews()
        {
            // 패널 타입에 따라 5개 장소의 보상 정보를 로드
            DispatchLocation[] locations;
            if (panelDispatchType == DispatchType.Combat)
            {
                locations = new DispatchLocation[]
                {
                    DispatchLocation.NightmareWarehouse,
                    DispatchLocation.FateWarehouse,
                    DispatchLocation.LaughterWarehouse,
                    DispatchLocation.TruthWarehouse,
                    DispatchLocation.UnknownWarehouse
                };
            }
            else
            {
                locations = new DispatchLocation[]
                {
                    DispatchLocation.MagicLibraryOrganization,
                    DispatchLocation.MagicBarrierInspection,
                    DispatchLocation.SpellbookCoverRestoration,
                    DispatchLocation.SealStabilityCheck,
                    DispatchLocation.MagicResiduePurification
                };
            }

            // 각 파견별로 대표 아이템 아이콘 로드 (첫 번째 보상만)
            for (int i = 0; i < locations.Length; i++)
            {
                Image slot = GetItemSlotByIndex(i);
                if (slot == null) continue;

                // 해당 장소의 보상 데이터 가져오기
                var locationData = GetLocationData(locations[i]);
                if (locationData == null)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                // 기본 시간(4시간, Time_ID=5201)의 보상 데이터 사용
                var rewardData = GetRewardData(locationData.Dispatch_Location_ID, 5201);
                if (rewardData == null)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                var rewardGroupData = CSVLoader.Instance.GetData<RewardGroupData>(rewardData.Reward_Group_ID);
                if (rewardGroupData == null)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                // 등급이 가장 높은 보상 찾기 (높은 등급 = 희귀 아이템을 프리뷰로 표시)
                int[] rewardIDs = new int[]
                {
                    rewardGroupData.Reward_1_ID,
                    rewardGroupData.Reward_2_ID,
                    rewardGroupData.Reward_3_ID,
                    rewardGroupData.Reward_4_ID,
                    rewardGroupData.Reward_5_ID
                };

                RewardData highestGradeReward = null;
                int highestGrade = -1;

                foreach (var rewardID in rewardIDs)
                {
                    if (rewardID == 0) continue;
                    var rewardCandidate = CSVLoader.Instance.GetData<RewardData>(rewardID);
                    if (rewardCandidate == null) continue;

                    // 아이템의 등급 조회 (IngredientData에서 찾고, 없으면 CurrencyData로 간주)
                    int gradeId = 0;
                    var ingredientTable = CSVLoader.Instance.GetTable<IngredientData>();
                    var ingredientData = ingredientTable?.DataList?.Find(x => x.Ingredient_ID == rewardCandidate.Item_ID);
                    if (ingredientData != null)
                    {
                        gradeId = ingredientData.Grade_ID;
                    }
                    // CurrencyData는 등급이 없으므로 0으로 유지

                    if (gradeId > highestGrade)
                    {
                        highestGrade = gradeId;
                        highestGradeReward = rewardCandidate;
                    }
                }

                if (highestGradeReward == null)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                var reward = highestGradeReward;

                // 슬롯 활성화 및 아이콘 로드
                slot.gameObject.SetActive(true);
                LoadItemIcon(slot, reward.Item_ID).Forget();

                AddLog($"✓ 파견 {i + 1} ({GetLocationName(locations[i])}) 아이템 프리뷰 초기화 완료");
            }
        }

        /// <summary>
        /// 파견 횟수 텍스트 업데이트 (파견 중: "1/1", 아닐 때: "0/1")
        /// </summary>
        private void UpdateDispatchCountText()
        {
            if (dispatchCountText == null) return;

            dispatchCountText.text = isDispatching ? "1/1" : "0/1";
        }

        /// <summary>
        /// 아이템 아이콘 로드 (PathTable 경유)
        /// Item_ID → CurrencyData/IngredientData.Path_ID → PathData.Addressable_Key
        /// </summary>
        private async UniTaskVoid LoadItemIcon(Image targetImage, int itemId)
        {
            if (targetImage == null) return;

            // 캐시에 있으면 즉시 적용
            if (cachedItemIcons.TryGetValue(itemId, out Sprite cachedIcon))
            {
                targetImage.sprite = cachedIcon;
                return;
            }

            string iconKey = null;
            int pathId = 0;

            // 화폐 아이템 처리 (1600번대: CurrencyData)
            if (itemId >= 1600 && itemId < 1700)
            {
                var currencyData = CSVLoader.Instance.GetData<CurrencyData>(itemId);
                if (currencyData != null && currencyData.Path_ID > 0)
                {
                    pathId = currencyData.Path_ID;
                }
            }
            else
            {
                // 일반 재료 아이템: IngredientData에서 Path_ID 조회
                var ingredientData = CSVLoader.Instance.GetData<IngredientData>(itemId);
                if (ingredientData != null && ingredientData.Path_ID > 0)
                {
                    pathId = ingredientData.Path_ID;
                }
            }

            if (pathId <= 0)
            {
                AddLog($"⚠️ Path_ID 없음: itemId={itemId}");
                return;
            }

            // PathTable에서 Addressable_Key 조회
            var pathData = CSVLoader.Instance.GetData<PathData>(pathId);
            if (pathData == null || string.IsNullOrEmpty(pathData.Addressable_Key) || pathData.Addressable_Key == "0")
            {
                AddLog($"⚠️ PathData 없음 또는 키 없음: Path_ID={pathId}");
                return;
            }

            iconKey = pathData.Addressable_Key;

            // Addressable에서 로드
            try
            {
                var icon = await UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(iconKey).ToUniTask();
                if (icon != null)
                {
                    cachedItemIcons[itemId] = icon;
                    targetImage.sprite = icon;
                    AddLog($"✓ 아이템 아이콘 로드 완료: {itemId} → {iconKey}");
                }
            }
            catch (System.Exception e)
            {
                AddLog($"❌ 아이템 아이콘 로드 실패: {iconKey} - {e.Message}");
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
        /// 시간(hours)으로부터 Time_ID 조회
        /// </summary>
        private int GetTimeIDFromHours(int hours)
        {
            // availableTimes에서 먼저 찾기
            if (availableTimes != null && availableTimes.Count > 0)
            {
                var timeData = availableTimes.FirstOrDefault(x => (int)x.Required_Hours == hours);
                if (timeData != null)
                {
                    return timeData.Dispatch_Time_ID;
                }
            }

            // availableTimes가 없으면 직접 CSV에서 조회
            if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
            {
                var timeTable = CSVLoader.Instance.GetTable<DispatchTimeTableData>();
                if (timeTable != null)
                {
                    var timeData = timeTable.FindAll(x => (int)x.Required_Hours == hours).FirstOrDefault();
                    if (timeData != null)
                    {
                        return timeData.Dispatch_Time_ID;
                    }
                }
            }

            // 하드코딩 fallback (CSV 로드 전에도 동작하도록)
            return hours switch
            {
                4 => 5201,
                8 => 5202,
                12 => 5203,
                23 => 5204,
                _ => 5201
            };
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

            // 현재 파견 타입 저장
            currentDispatchType = (DispatchType)categoryData.Dispatch_Category;
            string dispatchTypeName = currentDispatchType == DispatchType.Combat ? "전투형" : "채집형";
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
        /// 화살표 초기화 (원래 위치 저장 및 초기 표시 설정)
        /// </summary>
        private void InitializeArrows()
        {
            if (leftArrowImage != null)
            {
                leftArrowOriginalPos = leftArrowImage.rectTransform.anchoredPosition;
            }

            if (rightArrowImage != null)
            {
                rightArrowOriginalPos = rightArrowImage.rectTransform.anchoredPosition;
            }

            // 초기 화살표 표시 업데이트
            UpdateArrowVisibility();
        }

        /// <summary>
        /// 화살표 표시/숨김 업데이트 (스크롤 위치에 따라)
        /// </summary>
        private void UpdateArrowVisibility()
        {
            if (buttonScrollRect == null) return;

            // 파견 중에는 화살표 숨김
            if (isDispatching)
            {
                if (leftArrowImage != null) leftArrowImage.gameObject.SetActive(false);
                if (rightArrowImage != null) rightArrowImage.gameObject.SetActive(false);
                return;
            }

            float scrollPos = buttonScrollRect.horizontalNormalizedPosition;

            // 맨 왼쪽(0)이면 왼쪽 화살표 숨김, 오른쪽 화살표만 표시
            // 맨 오른쪽(1)이면 오른쪽 화살표 숨김, 왼쪽 화살표만 표시
            // 중간이면 양쪽 다 표시

            bool isAtLeftEnd = scrollPos <= 0.01f;
            bool isAtRightEnd = scrollPos >= 0.99f;

            if (leftArrowImage != null)
            {
                leftArrowImage.gameObject.SetActive(!isAtLeftEnd);
            }

            if (rightArrowImage != null)
            {
                rightArrowImage.gameObject.SetActive(!isAtRightEnd);
            }
        }

        /// <summary>
        /// 화살표 애니메이션 업데이트 (깜박임 + 좌우 이동)
        /// </summary>
        private void UpdateArrowAnimation()
        {
            // 화살표 표시 상태 업데이트
            UpdateArrowVisibility();

            // 파견 중에는 애니메이션 중지
            if (isDispatching) return;

            arrowAnimTime += Time.deltaTime;

            // 깜박임 효과 (알파값 변화: 0.3 ~ 1.0)
            float alpha = Mathf.Lerp(0.3f, 1f, (Mathf.Sin(arrowAnimTime * arrowBlinkSpeed * Mathf.PI) + 1f) / 2f);

            // 이동 효과 (좌우로 움직임)
            float moveOffset = Mathf.Sin(arrowAnimTime * arrowMoveSpeed) * arrowMoveDistance;

            // 왼쪽 화살표 애니메이션 (왼쪽으로 이동)
            if (leftArrowImage != null && leftArrowImage.gameObject.activeSelf)
            {
                Color leftColor = leftArrowImage.color;
                leftColor.a = alpha;
                leftArrowImage.color = leftColor;

                Vector2 leftPos = leftArrowOriginalPos;
                leftPos.x -= moveOffset; // 왼쪽으로 이동
                leftArrowImage.rectTransform.anchoredPosition = leftPos;
            }

            // 오른쪽 화살표 애니메이션 (오른쪽으로 이동)
            if (rightArrowImage != null && rightArrowImage.gameObject.activeSelf)
            {
                Color rightColor = rightArrowImage.color;
                rightColor.a = alpha;
                rightArrowImage.color = rightColor;

                Vector2 rightPos = rightArrowOriginalPos;
                rightPos.x += moveOffset; // 오른쪽으로 이동
                rightArrowImage.rectTransform.anchoredPosition = rightPos;
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
            if (tipText6 != null) tipText6.SetActive(false);
            if (tipText7 != null) tipText7.SetActive(false);
            if (tipText8 != null) tipText8.SetActive(false);
            if (tipText9 != null) tipText9.SetActive(false);
            if (tipText10 != null) tipText10.SetActive(false);
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
                // 전투형
                DispatchLocation.NightmareWarehouse => tipText1,
                DispatchLocation.FateWarehouse => tipText2,
                DispatchLocation.LaughterWarehouse => tipText3,
                DispatchLocation.TruthWarehouse => tipText4,
                DispatchLocation.UnknownWarehouse => tipText5,
                // 채집형
                DispatchLocation.MagicLibraryOrganization => tipText6,
                DispatchLocation.MagicBarrierInspection => tipText7,
                DispatchLocation.SpellbookCoverRestoration => tipText8,
                DispatchLocation.SealStabilityCheck => tipText9,
                DispatchLocation.MagicResiduePurification => tipText10,
                _ => null
            };

            if (targetTip != null)
            {
                targetTip.SetActive(true);
                AddLog($"✓ {GetLocationName(location)} 팁 표시");
            }
        }

        /// <summary>
        /// 실제 보상 계산 및 드랍
        /// </summary>
        private void CalculateAndDropRewards(DispatchRewardTableData rewardData)
        {
            // 보상 그룹 데이터 가져오기
            var rewardGroupData = CSVLoader.Instance.GetData<RewardGroupData>(rewardData.Reward_Group_ID);
            if (rewardGroupData == null)
            {
                AddLog("❌ 보상 그룹 정보 없음");
                return;
            }

            AddLog("🎲 보상 드랍 결과:");

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
                if (rewardID == 0) continue; // 보상 없음

                var reward = CSVLoader.Instance.GetData<RewardData>(rewardID);
                if (reward == null) continue;

                // Is_Fixed = 1이면 무조건 드랍, 0이면 확률에 따라 드랍 (북마크 아이템 드랍율 보너스 적용)
                float modifiedProbability = RewardHelper.CalculateItemDropRate(reward.Probability);
                bool shouldDrop = reward.Is_Fixed || Random.value <= modifiedProbability;

                if (shouldDrop)
                {
                    // 디버그: 원본 데이터 확인
                    //AddLog($"  [DEBUG] 원본 Min: {reward.Min_Count}, Max: {reward.Max_Count}, 배율: {rewardData.Reward_Multiplier}");

                    // 배율 적용한 드랍 수량 계산
                    int minCount = Mathf.FloorToInt(reward.Min_Count * rewardData.Reward_Multiplier);
                    int maxCount = Mathf.FloorToInt(reward.Max_Count * rewardData.Reward_Multiplier);

                    //AddLog($"  [DEBUG] 계산된 Min: {minCount}, Max: {maxCount}");

                    int dropCount = Random.Range(minCount, maxCount + 1);

                    // 북마크 보너스 적용 (골드인 경우 골드 보너스 적용)
                    int finalAmount = RewardHelper.CalculateRewardAmount(dropCount, reward.Item_ID);

                    // 아이템 이름 가져오기
                    string itemName = GetItemName(reward.Item_ID);

                    // 드랍 로그 (확률 보너스 표시)
                    string fixedText = reward.Is_Fixed ? "[고정]" : $"[{modifiedProbability * 100:F1}% 성공]";
                    AddLog($"  ✅ {fixedText} {itemName} x{finalAmount}");

                    // 골드(1601)는 CurrencyManager로, 나머지는 IngredientManager로 추가
                    if (reward.Item_ID == 1601)
                    {
                        if (CurrencyManager.Instance != null)
                        {
                            CurrencyManager.Instance.AddGold(finalAmount);
                            AddLog($"  💰 골드 추가됨 (북마크 보너스 적용)");
                        }
                    }
                    else
                    {
                        if (IngredientManager.Instance != null)
                        {
                            IngredientManager.Instance.AddIngredient(reward.Item_ID, finalAmount);
                            AddLog($"  💼 인벤토리에 추가됨");
                        }
                    }

                    // 토스트로 획득 아이템 표시
                    if (RewardToastManager.Instance != null)
                    {
                        Debug.Log($"[DispatchPanel] RewardToastManager.ShowReward 호출: itemId={reward.Item_ID}, amount={finalAmount}");
                        RewardToastManager.Instance.ShowReward(reward.Item_ID, finalAmount);
                    }
                    else
                    {
                        Debug.LogWarning("[DispatchPanel] RewardToastManager.Instance is NULL!");
                    }
                }
                else
                {
                    // 확률 실패
                    string itemName = GetItemName(reward.Item_ID);
                    AddLog($"  ❌ [{reward.Probability * 100:F1}% 실패] {itemName}");
                }
            }
        }

        /// <summary>
        /// 아이템 ID로 아이템 이름 가져오기
        /// </summary>
        private string GetItemName(int itemID)
        {
            return itemID switch
            {
                10101 => "희미 종이",
                10102 => "응축 종이",
                10103 => "비범 종이",
                10104 => "신성 종이",
                10105 => "고대 종이",
                10106 => "잉크",
                10207 => "로맨스페이지",
                10208 => "코미디페이지",
                10209 => "모험페이지",
                10210 => "공포페이지",
                10211 => "추리페이지",
                10313 => "클립",
                10114 => "룬석",
                1601 => "골드",
                _ => $"알 수 없는 아이템 (ID: {itemID})"
            };
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

            // 현재 프리셋이 다른 파견에서 사용 중인지 확인 (로컬 선택 기준)
            bool isUsedByOtherDispatch = presetSelector != null && presetSelector.IsLocalSelectedPresetUsedByOtherDispatch();
            if (isUsedByOtherDispatch)
            {
                AddLog("⚠️ 현재 프리셋이 다른 파견에서 사용 중 - 캐릭터 아이콘 비활성화");
            }

            // 로컬 선택 프리셋의 덱 가져오기
            var localDeck = presetSelector != null ? presetSelector.GetLocalSelectedDeck() : DeckManager.Instance.GetDeck();

            // 덱의 4개 슬롯 순회
            for (int i = 0; i < 4; i++)
            {
                int characterId = (i < localDeck.Count) ? localDeck[i] : -1;
                Image targetImage = GetDeckImageByIndex(i);

                if (targetImage == null)
                {
                    AddLog($"⚠️ 덱 이미지 슬롯 {i + 1}이 할당되지 않았습니다.");
                    continue;
                }

                // 항상 활성화 (레이아웃 유지)
                targetImage.gameObject.SetActive(true);

                if (characterId > 0)
                {
                    // 캐릭터가 있으면 이미지 로드
                    LoadCharacterImageForSlot(i, characterId, targetImage);

                    // 다른 파견에서 사용 중이면 약간 어둡게 표시
                    if (isUsedByOtherDispatch)
                    {
                        targetImage.color = new Color(0.65f, 0.65f, 0.65f, 1f); // 약간 어두운 색
                    }
                    else
                    {
                        targetImage.color = Color.white;
                    }
                    AddLog($"✓ 슬롯 {i + 1}: 캐릭터 ID {characterId} 로드{(isUsedByOtherDispatch ? " (비활성화)" : "")}");
                }
                else
                {
                    // 빈 슬롯 처리 (투명하게 - 레이아웃 유지)
                    targetImage.sprite = null;
                    targetImage.color = new Color(1f, 1f, 1f, 0f);
                    AddLog($"✓ 슬롯 {i + 1}: 빈 슬롯 (투명)");
                }
            }

            // CBL: 덱 슬롯 버튼 활성화/비활성화 업데이트
            UpdateDeckSlotButtons();

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

            string spriteKey = AddressableKey.Icon_Character;

            // CharacterData에서 Path_ID로 개별 아이콘 키 조회
            var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
            if (characterData != null && characterData.Path_ID > 0)
            {
                var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
                if (pathData != null && !string.IsNullOrEmpty(pathData.Addressable_Key))
                {
                    spriteKey = pathData.Addressable_Key;
                }
            }

            UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(spriteKey).Completed += handle =>
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

        // 파견 시작 시간 저장용
        private System.DateTime dispatchStartTime;
        private long dispatchStartTimeMs; // 서버 시간 (밀리초)

        /// <summary>
        /// 파견 상태 저장 (Firebase)
        /// </summary>
        private void SaveDispatchState()
        {
            if (FirebaseSaveManager.Instance == null || FirebaseManager.Instance?.CurrentUserId == null)
            {
                AddLog($"⚠️ Firebase 연결 안됨 - 파견 상태 저장 실패");
                return;
            }

            // 종료 시간 계산 (북마크 파견 시간 감소 modifier 적용)
            float reducedDispatchTimeHours = RewardHelper.CalculateDispatchTime(currentSelectedHours);
            long endTimeMs;
            if (useTestMode)
            {
                endTimeMs = dispatchStartTimeMs + (long)(reducedDispatchTimeHours * 1000f); // 테스트: 시간=초로 사용 → 밀리초 변환
            }
            else
            {
                endTimeMs = dispatchStartTimeMs + (long)(reducedDispatchTimeHours * 3600f * 1000f); // 실제: 시간 → 밀리초 변환
            }
            System.DateTime endTime = System.DateTimeOffset.FromUnixTimeMilliseconds(endTimeMs).LocalDateTime;

            var state = new Firebase.Data.DispatchStateData
            {
                isActive = isDispatching,
                locationId = (int)currentSelectedLocation,
                hours = currentSelectedHours,
                startTimeMs = dispatchStartTimeMs,
                endTimeMs = endTimeMs,
                startTime = dispatchStartTime.ToString("o"),
                endTime = endTime.ToString("o"),
                presetIndex = presetSelector != null ? presetSelector.LockedPresetIndex : (DeckManager.Instance != null ? DeckManager.Instance.GetCurrentPresetIndex() : -1)
            };

            string dispatchType = panelDispatchType == DispatchType.Combat ? "combat" : "gathering";
            FirebaseSaveManager.Instance.SaveDispatchAsync(
                FirebaseManager.Instance.CurrentUserId,
                dispatchType,
                state
            ).Forget();

            AddLog($"💾 파견 상태 저장됨 ({currentDispatchType}) - 남은 시간: {remainingTime}초");
        }

        /// <summary>
        /// 파견 상태 복원 (Firebase 데이터에서 로드)
        /// </summary>
        private void LoadDispatchState()
        {
            // CSV 데이터가 로드되지 않았으면 먼저 로드
            if (availableTimes == null || availableTimes.Count == 0)
            {
                LoadCSVData();
            }

            // Firebase 캐시 데이터에서 로드
            var dispatchData = FirebaseSaveManager.Instance?.CachedData?.dispatch;
            if (dispatchData == null)
            {
                AddLog($"📂 Firebase 데이터 없음 ({panelDispatchType})");
                return;
            }

            var state = panelDispatchType == DispatchType.Combat ? dispatchData.combat : dispatchData.gathering;
            if (state == null || !state.isActive)
            {
                AddLog($"📂 저장된 파견 상태 없음 ({panelDispatchType})");
                return;
            }

            // 현재 파견 타입을 패널 타입으로 설정
            currentDispatchType = panelDispatchType;

            float elapsedSeconds = 0f;

            // 새 형식 (밀리초) 우선 확인
            if (state.startTimeMs > 0 && state.endTimeMs > 0)
            {
                // ServerTimeManager가 초기화되지 않은 경우
                if (ServerTimeManager.Instance == null || !ServerTimeManager.Instance.IsSynced)
                {
                    AddLog("❌ 서버 시간이 동기화되지 않아 파견 상태 복원 불가");
                    return;
                }

                dispatchStartTimeMs = state.startTimeMs;
                dispatchStartTime = System.DateTimeOffset.FromUnixTimeMilliseconds(state.startTimeMs).UtcDateTime;

                // 서버 시간 기준 경과 시간 계산
                long elapsedMs = ServerTimeManager.Instance.GetElapsedMs(state.startTimeMs);
                elapsedSeconds = elapsedMs / 1000f;

                AddLog($"📂 새 형식으로 파견 복원 (서버 시간 기준)");
            }
            else
            {
                // 레거시 형식 (string) - 하위 호환
                if (string.IsNullOrEmpty(state.startTime))
                {
                    AddLog("❌ 파견 시작 시간 없음");
                    ClearDispatchState();
                    return;
                }

                if (!System.DateTime.TryParse(state.startTime, out dispatchStartTime))
                {
                    AddLog("❌ 파견 시작 시간 파싱 실패");
                    ClearDispatchState();
                    return;
                }

                // 로컬 시간 기준 경과 시간 계산 (레거시)
                System.TimeSpan elapsed = System.DateTime.Now - dispatchStartTime;
                elapsedSeconds = (float)elapsed.TotalSeconds;

                AddLog("⚠️ 레거시 형식으로 파견 복원 (로컬 시간 기준 - 마이그레이션 필요)");
            }

            // 남은 시간 계산 (총 파견 시간 - 경과 시간)
            float reducedTimeHours = RewardHelper.CalculateDispatchTime(state.hours);
            float totalDispatchTimeSeconds;
            if (useTestMode)
            {
                totalDispatchTimeSeconds = reducedTimeHours; // 테스트: 시간=초로 사용
            }
            else
            {
                totalDispatchTimeSeconds = reducedTimeHours * 3600f; // 실제: 시간 → 초 변환
            }
            remainingTime = totalDispatchTimeSeconds - elapsedSeconds;

            AddLog($"📂 남은 시간 계산: 총 {totalDispatchTimeSeconds:F0}초 - 경과 {elapsedSeconds:F0}초 = {remainingTime:F0}초 (테스트모드: {useTestMode})");

            // 이미 파견 완료된 경우
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                isDispatching = true; // 완료 상태로 설정
                currentSelectedLocation = (DispatchLocation)state.locationId;
                currentSelectedHours = state.hours;
                currentSelectedTimeID = GetTimeIDFromHours(state.hours);

                AddLog($"📂 파견 완료! 보상을 획득하세요.");

                // UI 업데이트
                RestoreDispatchUI().Forget();
            }
            else
            {
                // 저장된 상태 복원
                isDispatching = state.isActive;
                currentSelectedLocation = (DispatchLocation)state.locationId;
                currentSelectedHours = state.hours;
                currentSelectedTimeID = GetTimeIDFromHours(state.hours);

                AddLog($"📂 파견 상태 복원됨 - 장소: {GetLocationName(currentSelectedLocation)}, 남은 시간: {remainingTime:F0}초");

                // UI 업데이트 (Start 이후에 호출되므로 다음 프레임에서 실행)
                RestoreDispatchUI().Forget();
            }
        }

        /// <summary>
        /// 파견 UI 복원 (UniTask)
        /// </summary>
        private async UniTaskVoid RestoreDispatchUI()
        {
            // 다음 프레임까지 대기 (UI 요소들이 초기화될 때까지)
            await UniTask.Yield();

            // 파견 중인 창고 위치로 스크롤 이동
            MoveScrollToCurrentWarehouse();

            // 파견 UI 상태 복원
            UpdateDispatchUI();

            // 파견 중이면 프리셋 잠금 및 UI 갱신
            if (isDispatching && presetSelector != null)
            {
                presetSelector.Lock();
                presetSelector.RefreshPresetMarks();
            }

            // 파견 완료 상태라면 획득하기 버튼 활성화 + Red Dot 활성화
            if (isDispatching && remainingTime <= 0f)
            {
                if (dispatchStartButton != null)
                    dispatchStartButton.interactable = true;

                // 패널 타입에 맞는 DispatchController에게 파견 완료 알림 (Red Dot 활성화)
                if (panelDispatchType == DispatchType.Combat)
                {
                    if (combatDispatchController != null)
                    {
                        combatDispatchController.OnDispatchCompleted();
                        AddLog("✅ 파견 UI 복원 완료 - 획득하기 버튼 활성화 + 전투형 Red Dot 활성화");
                    }
                    else
                    {
                        AddLog("✅ 파견 UI 복원 완료 - 획득하기 버튼 활성화 (전투형 컨트롤러 없음)");
                    }
                }
                else if (panelDispatchType == DispatchType.Gathering)
                {
                    if (gatheringDispatchController != null)
                    {
                        gatheringDispatchController.OnDispatchCompleted();
                        AddLog("✅ 파견 UI 복원 완료 - 획득하기 버튼 활성화 + 채집형 Red Dot 활성화");
                    }
                    else
                    {
                        AddLog("✅ 파견 UI 복원 완료 - 획득하기 버튼 활성화 (채집형 컨트롤러 없음)");
                    }
                }
            }
            else
            {
                AddLog("✅ 파견 UI 복원 완료");
            }
        }

        /// <summary>
        /// 현재 선택된 창고 위치로 스크롤뷰 이동
        /// </summary>
        private void MoveScrollToCurrentWarehouse()
        {
            if (buttonScrollRect == null) return;

            // 패널 타입에 따라 인덱스 및 총 버튼 수 결정
            int warehouseIndex;
            int totalButtons;

            if (panelDispatchType == DispatchType.Combat)
            {
                // 전투형 장소 (0-4)
                warehouseIndex = currentSelectedLocation switch
                {
                    DispatchLocation.NightmareWarehouse => 0,
                    DispatchLocation.FateWarehouse => 1,
                    DispatchLocation.LaughterWarehouse => 2,
                    DispatchLocation.TruthWarehouse => 3,
                    DispatchLocation.UnknownWarehouse => 4,
                    _ => 0
                };
                totalButtons = totalCombatButtons;
            }
            else
            {
                // 채집형 장소 (0-4)
                warehouseIndex = currentSelectedLocation switch
                {
                    DispatchLocation.MagicLibraryOrganization => 0,
                    DispatchLocation.MagicBarrierInspection => 1,
                    DispatchLocation.SpellbookCoverRestoration => 2,
                    DispatchLocation.SealStabilityCheck => 3,
                    DispatchLocation.MagicResiduePurification => 4,
                    _ => 0
                };
                totalButtons = totalGatheringButtons;
            }

            // 버튼 인덱스 업데이트
            currentButtonIndex = warehouseIndex;

            // 스크롤 위치 계산 및 이동
            float scrollPosition = (float)warehouseIndex / (totalButtons - 1);
            buttonScrollRect.horizontalNormalizedPosition = scrollPosition;
            targetScrollPosition = scrollPosition;

            AddLog($"📍 스크롤 이동: {GetLocationName(currentSelectedLocation)} (인덱스: {warehouseIndex})");
        }

        /// <summary>
        /// 저장된 파견 상태 삭제 (Firebase)
        /// </summary>
        private void ClearDispatchState()
        {
            if (FirebaseSaveManager.Instance == null || FirebaseManager.Instance?.CurrentUserId == null)
            {
                AddLog($"⚠️ Firebase 연결 안됨 - 파견 상태 삭제 실패");
                return;
            }

            var state = new Firebase.Data.DispatchStateData
            {
                isActive = false,
                locationId = 0,
                hours = 0,
                startTime = "",
                endTime = ""
            };

            string dispatchType = panelDispatchType == DispatchType.Combat ? "combat" : "gathering";
            FirebaseSaveManager.Instance.SaveDispatchAsync(
                FirebaseManager.Instance.CurrentUserId,
                dispatchType,
                state
            ).Forget();

            AddLog($"🗑️ 파견 상태 삭제됨 ({currentDispatchType})");
        }

        private void OnDestroy()
        {
            // 이벤트 리스너 제거
            if (timeSlider != null)
                timeSlider.onValueChanged.RemoveListener(OnTimeSliderChanged);

            if (dispatchStartButton != null)
                dispatchStartButton.onClick.RemoveListener(OnDispatchStartButtonClicked);

            if (infoImageButton != null && infoImageButton.Length > 0)
            {
                for (int i = 0; i < infoImageButton.Length; i++)
                {
                    if (infoImageButton[i] != null)
                    {
                        infoImageButton[i].onClick.RemoveAllListeners();
                    }
                }
            }

            if (infoPanel != null)
            {
                var infoPanelButton = infoPanel.GetComponent<Button>();
                if (infoPanelButton != null)
                    infoPanelButton.onClick.RemoveListener(OnInfoPanelClicked);
            }

            // 화살표 버튼 이벤트 리스너 제거
            if (leftArrowImage != null)
            {
                var leftButton = leftArrowImage.GetComponent<Button>();
                if (leftButton != null)
                    leftButton.onClick.RemoveListener(OnLeftArrowClicked);
            }

            if (rightArrowImage != null)
            {
                var rightButton = rightArrowImage.GetComponent<Button>();
                if (rightButton != null)
                    rightButton.onClick.RemoveListener(OnRightArrowClicked);
            }
        }

        private void OnDisable()
        {
            // 프리셋 변경 이벤트 구독 해제
            if (presetSelector != null)
            {
                presetSelector.OnPresetSelected -= OnPresetChanged;
            }

            // 보상 정보 패널이 열려있으면 닫기
            CloseInfoPanelIfOpen();

            // 파견 중일 때만 상태 저장
            if (isDispatching)
            {
                SaveDispatchState();
            }
        }
    }
}
