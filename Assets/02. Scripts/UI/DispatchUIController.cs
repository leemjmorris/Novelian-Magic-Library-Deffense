using UnityEngine;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using Cysharp.Threading.Tasks;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Controls UI interactions in the Lobby scene
    /// Handles scene transitions from Lobby to Game
    /// </summary> 
    public class DispatchUIController : MonoBehaviour
    {

        [Header("Dispatch Panels")]
        [SerializeField] private Transform dispatchPanels; // DispatchPanels 참조
        [SerializeField] private GameObject mapObject; // Map 오브젝트 참조


        public void OnLobbyButton()
        {
            // DispatchPanels 자식 중 열린 패널이 있으면 닫기만 하고 리턴
            if (CloseDispatchPanels())
            {
                return;
            }

            // 열린 패널이 없으면 로비로 이동
            LoadSceneWithFadeOnly(SceneName.LobbyScene).Forget();
        }

        /// <summary>
        /// DispatchPanels의 자식 중 활성화된 패널을 모두 닫습니다.
        /// </summary>
        /// <returns>닫은 패널이 있으면 true, 없으면 false</returns>
        private bool CloseDispatchPanels()
        {
            if (dispatchPanels == null) return false;

            bool closedAny = false;
            foreach (Transform child in dispatchPanels)
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    closedAny = true;
                    Debug.Log($"[LobbyUIController] {child.name} 패널 닫힘");
                }
            }

            // 패널을 닫았으면 Map 다시 활성화
            if (closedAny && mapObject != null)
            {
                mapObject.SetActive(true);
                Debug.Log("[LobbyUIController] Map 활성화");
            }

            return closedAny;
        }

        /// <summary>
        /// LCB: 로딩 UI 없이 페이드 효과만으로 씬을 전환하는 메서드
        /// LCB: 페이드 아웃 → 씬 로드 → 페이드 인
        /// </summary>
        private async UniTaskVoid LoadSceneWithFadeOnly(string sceneName)
        {
            // 매니저가 없으면 직접 씬 로드 (fallback)
            if (FadeController.Instance == null)
            {
                Debug.LogWarning("FadeController not available, loading scene directly");
                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
                return;
            }

            // Step 1: 페이드 아웃 (화면 어두워짐)
            FadeController.Instance.fadePanel.SetActive(true);
            await FadeController.Instance.FadeOut(0.5f);

            // Step 2: 씬 로드
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            // Step 3: 페이드 인 (새 씬 밝아짐)
            await FadeController.Instance.FadeIn(0.5f);

            // Step 4: 페이드 패널 비활성화
            FadeController.Instance.fadePanel.SetActive(false);
        }

        /// <summary>
        /// LCB: 로딩 UI를 표시하면서 씬을 전환하는 공통 메서드
        /// LCB: 로딩 UI → 페이드 아웃 → 씬 로드 → 페이드 인
        /// </summary>
        private async UniTaskVoid LoadSceneWithLoadingUI(string sceneName)
        {
            // 매니저가 없으면 직접 씬 로드 (fallback)
            if (LoadingUIManager.Instance == null || FadeController.Instance == null)
            {
                Debug.LogWarning("LoadingUIManager or FadeController not available, loading scene directly");
                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
                return;
            }

            // Step 1: 로딩 UI 표시 및 진행률 애니메이션 (Inspector의 LOADING_DURATION_MS 사용)
            LoadingUIManager.Instance.Show();
            await LoadingUIManager.Instance.FakeLoadAsync();

            // Step 2: 100% 상태 잠깐 보여주기
            await UniTask.Delay(200);

            // Step 3: 페이드 아웃 (화면 어두워짐)
            FadeController.Instance.fadePanel.SetActive(true);
            await FadeController.Instance.FadeOut(0.5f);

            // Step 4: 로딩 UI 숨기기
            await LoadingUIManager.Instance.Hide();

            // Step 5: 씬 로드
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            // Step 6: 페이드 인 (새 씬 밝아짐)
            await FadeController.Instance.FadeIn(0.5f);

            // Step 7: 페이드 패널 비활성화
            FadeController.Instance.fadePanel.SetActive(false);
        }
    }
}