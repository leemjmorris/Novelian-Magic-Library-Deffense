using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Dispatch
{
    /// <summary>
    /// 파견 시스템 UI 컨트롤러 (전투형/채집형 통합)
    /// 느낌표 패널 버튼을 클릭하면 SelectImage-M이 SelectImage-S 크기로 축소 애니메이션되고,
    /// 애니메이션 완료 후 선택하기 버튼이 활성화됩니다.
    /// 선택하기 버튼 클릭 시 Map 오브젝트가 비활성화되고 해당 파견 패널이 활성화됩니다.
    /// </summary>
    public class CombatDispatchController : MonoBehaviour
    {
        [Header("Dispatch Type")]
        [SerializeField] private DispatchType dispatchType = DispatchType.Combat; // 파견 타입 (Inspector에서 선택)
        [SerializeField] private string dispatchSaveKey = "CombatDispatch_SaveData"; // 파견 상태 저장 키 (전투형: CombatDispatch_SaveData, 채집형: GatheringDispatch_SaveData)

        [Header("Dispatch UI")]
        [SerializeField] private Button exclamationPanelButton;    // 느낌표 패널 버튼
        [SerializeField] private RectTransform selectImageM;       // SelectImage-M (축소될 이미지, Scale X: 1.08 -> 1.0)
        [SerializeField] private Button selectButton;              // 선택하기 버튼 (SlelectButton)
        [SerializeField] private GameObject redDotImage;           // Red Dot 이미지 (파견 완료 알림)

        [Header("Scene Objects")]
        [SerializeField] private GameObject mapObject;             // Map 오브젝트
        [SerializeField] private GameObject dispatchPanel;   // 파견 패널 오브젝트 (전투형/채집형)

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 1.0f;   // 애니메이션 지속 시간 (초)

        private Vector2 originalSizeM;  // SelectImage-M의 원본 크기 저장
        private bool isAnimationComplete = false;
        private bool isDispatching = false; // 파견 중 여부
        private CancellationTokenSource cancellationTokenSource;

        // 타입별 로그 태그
        private string LogTag => $"[{dispatchType}DispatchController]";

        // JML: 파견 선택 스위칭용 static 이벤트
        private static event System.Action<CombatDispatchController> OnDispatchSelected;

        private void OnEnable()
        {
            OnDispatchSelected += HandleOtherSelected;
        }

        private void OnDisable()
        {
            OnDispatchSelected -= HandleOtherSelected;
        }

        /// <summary>
        /// 다른 파견이 선택됐을 때 호출 - 자신은 선택 해제
        /// </summary>
        private void HandleOtherSelected(CombatDispatchController selected)
        {
            if (selected != this)
            {
                Deselect();
            }
        }

        /// <summary>
        /// 선택 해제 - SelectImage 비활성화 및 버튼 비활성화
        /// </summary>
        private void Deselect()
        {
            // SelectImage 비활성화
            if (selectImageM != null)
            {
                selectImageM.gameObject.SetActive(false);
            }

            // 선택하기 버튼 비활성화
            if (selectButton != null)
            {
                selectButton.interactable = false;
            }

            // 애니메이션 플래그 리셋
            isAnimationComplete = false;

            // 애니메이션 진행 중이면 취소
            cancellationTokenSource?.Cancel();

            Debug.Log($"{LogTag} 선택 해제됨 (다른 파견 선택)");
        }

        private void Start()
        {
            // UI 초기화
            InitializeUI();

            // 파견 완료 상태 확인 및 Red Dot 표시
            CheckDispatchStateAndShowRedDot();
        }

        /// <summary>
        /// UI 초기 상태 설정
        /// </summary>
        private void InitializeUI()
        {
            // SelectImage-M 비활성화 (초기 상태)
            if (selectImageM != null)
            {
                selectImageM.gameObject.SetActive(false);
                Debug.Log($"{LogTag} SelectImage-M 초기 비활성화");
            }

            // 선택하기 버튼 비활성화
            if (selectButton != null)
            {
                selectButton.interactable = false;
                Debug.Log($"{LogTag} 선택하기 버튼 초기 비활성화");
            }

            // dispatchPanel 비활성화 (초기 상태)
            if (dispatchPanel != null)
            {
                dispatchPanel.SetActive(false);
                Debug.Log($"{LogTag} {dispatchType} 패널 초기 비활성화");
            }

            // Map 활성화 (초기 상태)
            if (mapObject != null)
            {
                mapObject.SetActive(true);
                Debug.Log($"{LogTag} Map 초기 활성화");
            }

            // Red Dot은 초기 비활성화 (파견 상태 확인 후 활성화 여부 결정)
            if (redDotImage != null)
            {
                redDotImage.SetActive(false);
                Debug.Log($"{LogTag} Red Dot 초기 비활성화");
            }
        }

        /// <summary>
        /// 파견 완료 상태 확인 및 Red Dot 표시
        /// </summary>
        private void CheckDispatchStateAndShowRedDot()
        {
            if (!PlayerPrefs.HasKey(dispatchSaveKey))
            {
                Debug.Log($"{LogTag} 저장된 파견 상태 없음");
                return;
            }

            string json = PlayerPrefs.GetString(dispatchSaveKey);
            var saveData = JsonUtility.FromJson<DispatchSaveData>(json);

            if (saveData == null || !saveData.isDispatching)
            {
                Debug.Log($"{LogTag} 파견 중이 아님");
                return;
            }

            // 시작 시간 파싱
            if (!System.DateTime.TryParse(saveData.startTimeString, out System.DateTime startTime))
            {
                Debug.LogError($"{LogTag} 파견 시작 시간 파싱 실패");
                return;
            }

            // 경과 시간 계산
            System.TimeSpan elapsed = System.DateTime.Now - startTime;
            float elapsedSeconds = (float)elapsed.TotalSeconds;
            float remainingTime = saveData.totalDispatchTime - elapsedSeconds;

            // 파견 완료 상태라면 Red Dot 활성화
            if (remainingTime <= 0f)
            {
                if (redDotImage != null)
                {
                    redDotImage.SetActive(true);
                    Debug.Log($"{LogTag} ✅ 파견 완료 - Map에 Red Dot 표시");
                }
            }
            else
            {
                Debug.Log($"{LogTag} 파견 진행 중 - 남은 시간: {remainingTime:F0}초");
            }
        }

        /// <summary>
        /// 파견 상태 저장 데이터 구조 (DispatchPanel과 동일)
        /// </summary>
        [System.Serializable]
        private class DispatchSaveData
        {
            public bool isDispatching;
            public float totalDispatchTime;
            public string startTimeString;
            public int selectedLocation;
            public int selectedHours;
            public int selectedTimeID;
        }

        /// <summary>
        /// 느낌표 패널 버튼 클릭 시 - SelectImage-M 축소 애니메이션 실행 (Inspector OnClick에서 연결)
        /// </summary>
        public void OnExclamationButtonClicked()
        {
            OnExclamationButtonClickedAsync().Forget();
        }

        private async UniTaskVoid OnExclamationButtonClickedAsync()
        {
            Debug.Log($"{LogTag} OnExclamationButtonClickedAsync 호출됨");

            // JML: 다른 파견 선택 해제 (스위칭)
            OnDispatchSelected?.Invoke(this);

            if (isAnimationComplete)
            {
                return;
            }

            if (selectImageM == null)
            {
                Debug.LogError($"{LogTag} SelectImage-M이 할당되지 않았습니다!");
                return;
            }

            // SelectImage-M 활성화 (애니메이션 시작 전)
            selectImageM.gameObject.SetActive(true);
            Debug.Log($"{LogTag} SelectImage-M 활성화됨");

            // 애니메이션 캔슬 토큰 생성
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            // Width 값 설정 (350 -> 200)
            float startWidth = 350f;
            float targetWidth = 200f;
            float currentHeight = selectImageM.sizeDelta.y;

            // 초기 크기를 350으로 설정
            selectImageM.sizeDelta = new Vector2(startWidth, currentHeight);

            // UniTask를 사용한 부드러운 Width 축소 애니메이션
            float elapsedTime = 0f;
            int frameCount = 0;

            while (elapsedTime < animationDuration)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / animationDuration);

                // OutCubic 이징 함수 적용
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                // Width 보간
                float newWidth = Mathf.Lerp(startWidth, targetWidth, easedT);
                selectImageM.sizeDelta = new Vector2(newWidth, currentHeight);

                frameCount++;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationTokenSource.Token);
            }

            // 최종 Width 확정
            selectImageM.sizeDelta = new Vector2(targetWidth, currentHeight);

            // 애니메이션 완료 플래그 설정
            isAnimationComplete = true;

            // 선택하기 버튼 활성화
            EnableSelectButton();
        }

        /// <summary>
        /// 선택하기 버튼 활성화
        /// </summary>
        private void EnableSelectButton()
        {
            if (selectButton != null)
            {
                selectButton.interactable = true;
                Debug.Log($"{LogTag} ✅ 선택하기 버튼 활성화됨");
            }
        }

        /// <summary>
        /// 선택하기 버튼 클릭 시 - Map 비활성화 및 파견 패널 활성화 (Inspector OnClick에서 연결)
        /// </summary>
        public void OnSelectButtonClicked()
        {
            Debug.Log($"{LogTag} 선택하기 버튼 클릭");

            // Map 오브젝트 비활성화
            if (mapObject != null)
            {
                mapObject.SetActive(false);
            }

            // 파견 패널 오브젝트 활성화
            if (dispatchPanel != null)
            {
                dispatchPanel.SetActive(true);
                Debug.Log($"{LogTag} ✅ {dispatchType} 패널 활성화");
            }

            // 파견 중 상태로 설정
            isDispatching = true;
        }

        /// <summary>
        /// 파견/보상받기 버튼 클릭 시 (Inspector OnClick에서 연결)
        /// isDispatching 상태에 따라 동작 분기
        /// </summary>
        public void OnDispatchOrRewardButtonClicked()
        {
            if (isDispatching)
            {
                // 파견 중일 때 = 보상받기
                OnRewardReceived();
            }
            else
            {
                // 파견 중이 아닐 때 = 아무 동작 없음 (DispatchPanel 등에서 처리)
                Debug.Log($"{LogTag} 파견 중이 아닙니다. (다른 컨트롤러에서 처리)");
            }
        }

        /// <summary>
        /// 파견 완료 처리 (DispatchPanel에서 호출)
        /// Red Dot만 활성화, 상태 저장 없음 (씬 나가면 사라짐)
        /// </summary>
        public void OnDispatchCompleted()
        {
            Debug.Log($"{LogTag} 파견 완료 - Map에 Red Dot 활성화");
            // Red Dot 활성화
            if (redDotImage != null)
            {
                redDotImage.SetActive(true);
            }
        }

        /// <summary>
        /// 보상 획득 처리 (DispatchPanel에서 호출)
        /// Red Dot 비활성화
        /// </summary>
        public void OnRewardClaimed()
        {
            Debug.Log($"{LogTag} 보상 획득 - Red Dot 비활성화");
            if (redDotImage != null)
            {
                redDotImage.SetActive(false);
            }
        }

        /// <summary>
        /// 보상 받기 처리
        /// Red Dot만 비활성화 (상태 저장 없음)
        /// </summary>
        private void OnRewardReceived()
        {
            Debug.Log($"{LogTag} 보상 받기 - Red Dot 비활성화");

            // 파견 상태를 false로 변경
            isDispatching = false;

            // 애니메이션 플래그 리셋
            isAnimationComplete = false;

            // Red Dot 비활성화
            if (redDotImage != null)
            {
                redDotImage.SetActive(false);
            }

            Debug.Log($"{LogTag} ✅ 보상 받기 완료");
        }

        /// <summary>
        /// 초기 상태로 리셋 (디버그/테스트용)
        /// </summary>
        public void ResetToInitialState()
        {
            // SelectImage-M 크기 복원
            if (selectImageM != null)
            {
                selectImageM.sizeDelta = originalSizeM;
            }

            // 애니메이션 플래그 리셋
            isAnimationComplete = false;

            // 파견 상태 초기화
            isDispatching = false;

            // UI 초기 상태로 복원
            InitializeUI();

            Debug.Log($"{LogTag} 초기 상태로 리셋됨");
        }

        private void OnDestroy()
        {
            // 애니메이션 캔슬 토큰 정리
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }
    }
}
