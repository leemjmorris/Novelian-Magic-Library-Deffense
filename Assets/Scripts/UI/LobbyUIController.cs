using UnityEngine;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.Core;
using Cysharp.Threading.Tasks;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Controls UI interactions in the Lobby scene
    /// Handles scene transitions from Lobby to Game
    /// </summary>
    public class LobbyUIController : MonoBehaviour
    {
        [Header("Button References")]
        [SerializeField] private Button startGameButton;

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
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameButtonClicked);
                Debug.Log("[LobbyUIController] StartGame button listener setup complete");
            }
            else
            {
                Debug.LogError("[LobbyUIController] StartGame button reference is null!");
            }
        }

        private void RemoveButtonListeners()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(OnStartGameButtonClicked);
            }
        }

        private void OnStartGameButtonClicked()
        {
            Debug.Log("[LobbyUIController] StartGame button clicked - Loading GameScene");
            LoadGameSceneAsync().Forget();
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("GameScene");
        }
    }
}