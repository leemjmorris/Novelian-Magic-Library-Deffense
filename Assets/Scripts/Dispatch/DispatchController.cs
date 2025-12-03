using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Dispatch
{
    /// <summary>
    /// 지하 파견 시스템 UI 컨트롤러
    /// Ground dispatch의 느낌표 패널 버튼을 클릭하면 SelectImage-M이 SelectImage-S 크기로 축소 애니메이션되고,
    /// 애니메이션 완료 후 선택하기 버튼이 활성화됩니다.
    /// 선택하기 버튼 클릭 시 Map 오브젝트가 비활성화되고 combatDispatch가 활성화됩니다.
    /// </summary>
    public class DispatchController : MonoBehaviour
    {
        [Header("Ground Dispatch UI")]
        [SerializeField] private Button exclamationPanelButton;    // 느낌표 패널 버튼
        [SerializeField] private RectTransform selectImageM;       // SelectImage-M (축소될 이미지, Scale X: 1.08 -> 1.0)
        [SerializeField] private Button selectButton;              // 선택하기 버튼 (SlelectButton)

        [Header("Scene Objects")]
        [SerializeField] private GameObject mapObject;             // Map 오브젝트
        [SerializeField] private GameObject combatDispatch;   // combat dispatch 오브젝트

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 1.0f;   // 애니메이션 지속 시간 (초)

        private const string DISPATCH_STATE_KEY = "DispatchController_State";

        private Vector2 originalSizeM;  // SelectImage-M의 원본 크기 저장
        private bool isAnimationComplete = false;
        private bool isDispatching = false; // 파견 중 여부
        private bool hasReceivedReward = false; // 보상 받았는지 여부 (OnDisable에서 참조)
        private CancellationTokenSource cancellationTokenSource;

        [System.Serializable]
        private class DispatchState
        {
            public bool isDispatching;
            public bool mapActive;
            public bool combatDispatchActive;
            public bool rewardReceived; // 보상 받았는지 여부
        }

        private void Start()
        {
            // 저장된 상태 복원
            bool stateRestored = LoadDispatchState();

            // 초기 상태 설정 (저장된 상태가 없을 때만)
            if (!stateRestored)
            {
                InitializeUI();
            }
        }

        private void OnDisable()
        {
            // 파견 중이거나 보상 받은 상태일 때 저장
            SaveDispatchState(rewardReceived: hasReceivedReward);
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

            // 상태 저장
            SaveDispatchState();
        }

        /// <summary>
        /// 파견 상태 저장
        /// </summary>
        private void SaveDispatchState(bool rewardReceived = false)
        {
            // 파견 중이거나 보상을 받은 상태일 때만 저장
            if (!isDispatching && !rewardReceived) return;

            DispatchState state = new DispatchState
            {
                isDispatching = isDispatching,
                mapActive = mapObject != null && mapObject.activeSelf,
                combatDispatchActive = combatDispatch != null && combatDispatch.activeSelf,
                rewardReceived = rewardReceived
            };

            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(DISPATCH_STATE_KEY, json);
            PlayerPrefs.Save();

            Debug.Log($"[DispatchController] 파견 상태 저장됨 - Map: {state.mapActive}, CombatDispatch: {state.combatDispatchActive}, RewardReceived: {rewardReceived}");
        }

        /// <summary>
        /// 파견 상태 복원
        /// </summary>
        /// <returns>상태가 복원되었으면 true, 아니면 false</returns>
        private bool LoadDispatchState()
        {
            if (!PlayerPrefs.HasKey(DISPATCH_STATE_KEY))
            {
                Debug.Log("[DispatchController] 저장된 파견 상태 없음");
                return false;
            }

            string json = PlayerPrefs.GetString(DISPATCH_STATE_KEY);
            DispatchState state = JsonUtility.FromJson<DispatchState>(json);

            if (state == null)
            {
                Debug.Log("[DispatchController] 파견 상태 파싱 실패");
                return false;
            }

            // 보상을 받은 경우 -> Map 패널로 초기화
            if (state.rewardReceived)
            {
                Debug.Log("[DispatchController] 보상 받은 상태 -> Map 패널로 복원");
                InitializeUI();
                ClearDispatchState();
                hasReceivedReward = false; // 플래그 리셋
                return true;
            }

            // 파견 중이 아니면 복원 안 함
            if (!state.isDispatching)
            {
                Debug.Log("[DispatchController] 파견 중이 아님");
                return false;
            }

            // 파견 중 상태 복원
            isDispatching = state.isDispatching;
            isAnimationComplete = true; // 애니메이션은 이미 완료된 상태

            // 잘못된 상태 감지: Map과 CombatDispatch가 둘 다 꺼진 경우 -> 초기화
            if (!state.mapActive && !state.combatDispatchActive)
            {
                Debug.LogWarning("[DispatchController] 잘못된 상태 감지 (둘 다 비활성) - 초기 상태로 복원");
                InitializeUI();
                ClearDispatchState();
                return true;
            }

            // Map 상태 복원
            if (mapObject != null)
            {
                mapObject.SetActive(state.mapActive);
            }

            // combatDispatch 상태 복원
            if (combatDispatch != null)
            {
                combatDispatch.SetActive(state.combatDispatchActive);
            }

            // SelectImage-M 비활성화 (애니메이션 완료 후 상태)
            if (selectImageM != null)
            {
                selectImageM.gameObject.SetActive(false);
            }

            // 선택하기 버튼 비활성화
            if (selectButton != null)
            {
                selectButton.interactable = false;
            }

            Debug.Log($"[DispatchController] ✅ 파견 상태 복원됨 - Map: {state.mapActive}, CombatDispatch: {state.combatDispatchActive}");
            return true;
        }

        /// <summary>
        /// 파견 상태 초기화
        /// </summary>
        private void ClearDispatchState()
        {
            if (PlayerPrefs.HasKey(DISPATCH_STATE_KEY))
            {
                PlayerPrefs.DeleteKey(DISPATCH_STATE_KEY);
                PlayerPrefs.Save();
                Debug.Log("[DispatchController] 파견 상태 삭제됨");
            }

            isDispatching = false;
            hasReceivedReward = false;
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
        /// 보상 받기 처리
        /// </summary>
        private void OnRewardReceived()
        {
            Debug.Log("[DispatchController] 보상 받기 - rewardReceived 플래그 저장");

            // 파견 상태를 false로 변경
            isDispatching = false;

            // 애니메이션 플래그 리셋
            isAnimationComplete = false;

            // 보상 받은 상태 플래그 설정
            hasReceivedReward = true;

            // rewardReceived 플래그를 true로 저장
            // 다음에 씬 들어올 때 Map 패널로 전환됨
            SaveDispatchState(rewardReceived: true);

            Debug.Log("[DispatchController] ✅ 보상 받기 완료 - 다음 씬 진입 시 Map 패널로 전환됩니다");
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
            ClearDispatchState();

            // UI 초기 상태로 복원
            InitializeUI();

            Debug.Log("[DispatchController] 초기 상태로 리셋됨");
        }

        private void OnDestroy()
        {
            // 애니메이션 캔슬 토큰 정리
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }
    }
}
