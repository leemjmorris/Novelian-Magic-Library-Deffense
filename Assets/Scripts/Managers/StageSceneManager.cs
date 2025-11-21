using UnityEngine;
using UnityEngine.UI;
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
        [Header("Button References")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button stageStartButton;

        private void Awake()
        {
            SetupButtonListeners();
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
        }

        private void SetupButtonListeners()
        {
            if (homeButton != null)
            {
                homeButton.onClick.AddListener(OnHomeButtonClicked);
                Debug.Log("[StageSceneManager] Home button listener setup complete");
            }
            else
            {
                Debug.LogWarning("[StageSceneManager] Home button reference is null!");
            }

            if (stageStartButton != null)
            {
                stageStartButton.onClick.AddListener(OnStageStartButtonClicked);
                Debug.Log("[StageSceneManager] StageStart button listener setup complete");
            }
            else
            {
                Debug.LogWarning("[StageSceneManager] StageStart button reference is null!");
            }
        }

        private void RemoveButtonListeners()
        {
            if (homeButton != null)
            {
                homeButton.onClick.RemoveListener(OnHomeButtonClicked);
            }

            if (stageStartButton != null)
            {
                stageStartButton.onClick.RemoveListener(OnStageStartButtonClicked);
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

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("LobbyScene");
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("GameScene3D");
        }
    }
}
