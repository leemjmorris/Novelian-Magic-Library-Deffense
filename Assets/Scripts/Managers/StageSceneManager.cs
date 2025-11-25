using UnityEngine;
using NovelianMagicLibraryDefense.Core;
using Cysharp.Threading.Tasks;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// StageScene manager for handling scene transitions
    /// Home button -> LobbyScene
    /// Stage button -> GameScene3D
    /// </summary>
    public class StageSceneManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panel3;

        private void Awake()
        {
            // Panel3 초기 비활성화
            if (panel3 != null)
            {
                panel3.SetActive(false);
            }
        }

        public void ShowPanel3()
        {
            if (panel3 != null)
            {
                panel3.SetActive(true);
            }
        }

        public void HidePanel3()
        {
            if (panel3 != null)
            {
                panel3.SetActive(false);
            }
        }

        private void OnHomeButtonClicked()
        {
            Debug.Log("[StageSceneManager] Home button clicked - Loading LobbyScene");
            LoadLobbySceneAsync().Forget();
        }

        private void OnStageStartButtonClicked()
        {
            Debug.Log("[StageSceneManager] Stage button clicked - Loading GameScene3D");
            LoadGameSceneAsync().Forget();
        }

        public void OnLoadLobbyScene()
        {
            LoadLobbySceneAsync().Forget();
        }
        public void OnLoadGameScene()
        {
            LoadGameSceneAsync().Forget();
        }

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("LobbyScene");
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("GameScene");
        }
    }
}
