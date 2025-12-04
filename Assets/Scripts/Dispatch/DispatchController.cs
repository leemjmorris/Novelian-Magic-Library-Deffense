using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Dispatch
{
    /// <summary>
    /// 전투형 파견 시스템 UI 컨트롤러
    /// Ground dispatch의 느낌표 패널 버튼을 클릭하면 SelectImage-M이 SelectImage-S 크기로 축소 애니메이션되고,
    /// 애니메이션 완료 후 선택하기 버튼이 활성화됩니다.
    /// 선택하기 버튼 클릭 시 Map 오브젝트가 비활성화되고 combatDispatch가 활성화됩니다.
    /// </summary>
    public class CombatDispatchController : MonoBehaviour
    {
        [Header("Ground Dispatch UI")]
        [SerializeField] private Button exclamationPanelButton;    // 느낌표 패널 버튼
        [SerializeField] private RectTransform selectImageM;       // SelectImage-M (축소될 이미지, Scale X: 1.08 -> 1.0)
        [SerializeField] private Button selectButton;              // 선택하기 버튼 (SlelectButton)
        [SerializeField] private GameObject redDotImage;           // Red Dot 이미지 (파견 완료 알림)

        [Header("Scene Objects")]
        [SerializeField] private GameObject mapObject;             // Map 오브젝트
        [SerializeField] private GameObject combatDispatch;   // combat dispatch 오브젝트

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 1.0f;   // 애니메이션 지속 시간 (초)

        private const string DISPATCH_STATE_KEY = "DispatchController_State";

        private Vector2 originalSizeM;  // SelectImage-M의 원본 크기 저장
        private bool isAnimationComplete = false;
        private bool isDispatching = false; // 파견 중 여부
        private CancellationTokenSource cancellationTokenSource;

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
                Debug.Log("[DispatchController] SelectImage-M 초기 비활성화");
            }

            // 선택하기 버튼 비활성화
            if (selectButton != null)
            {
                selectButton.interactable = false;
                Debug.Log("[DispatchController] 선택하기 버튼 초기 비활성화");
            }

            // combatDispatch 비활성화 (초기 상태)
            if (combatDispatch != null)
            {
                combatDispatch.SetActive(false);
                Debug.Log("[DispatchController] combatDispatch 초기 비활성화");
            }

            // Map 활성화 (초기 상태)
            if (mapObject != null)
            {
                mapObject.SetActive(true);
                Debug.Log("[DispatchController] Map 초기 활성화");
            }

            // Red Dot은 초기 비활성화 (파견 상태 확인 후 활성화 여부 결정)
            if (redDotImage != null)
            {
                redDotImage.SetActive(false);
                Debug.Log("[DispatchController] Red Dot 초기 비활성화");
            }
        }

        /// <summary>
        /// 파견 완료 상태 확인 및 Red Dot 표시
        /// </summary>
        private void CheckDispatchStateAndShowRedDot()
        {
            const string DISPATCH_SAVE_KEY = "DispatchTestPanel_SaveData";

            if (!PlayerPrefs.HasKey(DISPATCH_SAVE_KEY))
            {
                Debug.Log("[DispatchController] 저장된 파견 상태 없음");
                return;
            }

            string json = PlayerPrefs.GetString(DISPATCH_SAVE_KEY);
            var saveData = JsonUtility.FromJson<DispatchSaveData>(json);

            if (saveData == null || !saveData.isDispatching)
            {
                Debug.Log("[DispatchController] 파견 중이 아님");
                return;
            }

            // 시작 시간 파싱
            if (!System.DateTime.TryParse(saveData.startTimeString, out System.DateTime startTime))
            {
                Debug.LogError("[DispatchController] 파견 시작 시간 파싱 실패");
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
                    Debug.Log("[DispatchController] ✅ 파견 완료 - Map에 Red Dot 표시");
                }
            }
            else
            {
                Debug.Log($"[DispatchController] 파견 진행 중 - 남은 시간: {remainingTime:F0}초");
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
            Debug.Log($"[DispatchController] OnExclamationButtonClickedAsync 호출됨");

            if (isAnimationComplete)
            {
                Debug.Log("[DispatchController] 애니메이션이 이미 완료되었습니다.");
                return;
            }

            if (selectImageM == null)
            {
                Debug.LogError("[DispatchController] SelectImage-M이 할당되지 않았습니다!");
                return;
            }

            // SelectImage-M 활성화 (애니메이션 시작 전)
            selectImageM.gameObject.SetActive(true);
            Debug.Log("[DispatchController] SelectImage-M 활성화됨");

            // 애니메이션 캔슬 토큰 생성
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            // Width 값 설정 (350 -> 200)
            float startWidth = 350f;
            float targetWidth = 200f;
            float currentHeight = selectImageM.sizeDelta.y;

            // 초기 크기를 350으로 설정
            selectImageM.sizeDelta = new Vector2(startWidth, currentHeight);

            Debug.Log($"[DispatchController] 축소 애니메이션 시작 - 시작 Width: {startWidth}, 목표 Width: {targetWidth}, 지속시간: {animationDuration}초");

            // UniTask를 사용한 부드러운 Width 축소 애니메이션
            float elapsedTime = 0f;
            int frameCount = 0;

            while (elapsedTime < animationDuration)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Debug.LogWarning("[DispatchController] 애니메이션이 취소되었습니다.");
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
                if (frameCount % 10 == 0) // 10프레임마다 로그 출력
                {
                    Debug.Log($"[DispatchController] 애니메이션 진행중 - t: {t:F2}, easedT: {easedT:F2}, 현재 Width: {newWidth:F1}");
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationTokenSource.Token);
            }

            // 최종 Width 확정
            selectImageM.sizeDelta = new Vector2(targetWidth, currentHeight);

            Debug.Log($"[DispatchController] ✅ 축소 애니메이션 완료 - 최종 Width: {targetWidth}, 총 프레임: {frameCount}");

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
                Debug.Log("[UndergroundDispatchController] ✅ 선택하기 버튼 활성화됨");
            }
        }

        /// <summary>
        /// 선택하기 버튼 클릭 시 - Map 비활성화 및 Underground dispatch 활성화 (Inspector OnClick에서 연결)
        /// </summary>
        public void OnSelectButtonClicked()
        {
            Debug.Log("[DispatchController] 선택하기 버튼 클릭");

            // Map 오브젝트 비활성화
            if (mapObject != null)
            {
                mapObject.SetActive(false);
            }

            // combatDispatch 오브젝트 활성화
            if (combatDispatch != null)
            {
                combatDispatch.SetActive(true);
                Debug.Log("[DispatchController] ✅ combatDispatch 활성화");
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
                // 파견 중이 아닐 때 = 아무 동작 없음 (DispatchTestPanel 등에서 처리)
                Debug.Log("[DispatchController] 파견 중이 아닙니다. (다른 컨트롤러에서 처리)");
            }
        }

        /// <summary>
        /// 파견 완료 처리 (CombatDispatchPanel에서 호출)
        /// Red Dot만 활성화, 상태 저장 없음 (씬 나가면 사라짐)
        /// </summary>
        public void OnDispatchCompleted()
        {
          
                Debug.Log("[CombatDispatchController] 파견 완료 - Map에 Red Dot 활성화");
            // Red Dot 활성화
            if (redDotImage != null)
            {
                redDotImage.SetActive(true);
            }
        }

        /// <summary>
        /// 보상 받기 처리
        /// Red Dot만 비활성화 (상태 저장 없음)
        /// </summary>
        private void OnRewardReceived()
        {
            Debug.Log("[CombatDispatchController] 보상 받기 - Red Dot 비활성화");

            // 파견 상태를 false로 변경
            isDispatching = false;

            // 애니메이션 플래그 리셋
            isAnimationComplete = false;

            // Red Dot 비활성화
            if (redDotImage != null)
            {
                redDotImage.SetActive(false);
            }

            Debug.Log("[CombatDispatchController] ✅ 보상 받기 완료");
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

            Debug.Log("[CombatDispatchController] 초기 상태로 리셋됨");
        }

        private void OnDestroy()
        {
            // 애니메이션 캔슬 토큰 정리
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }
    }
}
