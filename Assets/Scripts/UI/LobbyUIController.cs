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
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button bookMarkCraftButton;

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

            if (inventoryButton != null)
            {
                inventoryButton.onClick.AddListener(OnInventoryButtonClicked);
                Debug.Log("[LobbyUIController] Inventory button listener setup complete");
            }
            else
            {
                Debug.LogError("[LobbyUIController] Inventory button reference is null!");
            }

            if (bookMarkCraftButton != null)
            {
                bookMarkCraftButton.onClick.AddListener(OnBookMarkCraftButtonClicked);
                Debug.Log("[LobbyUIController] BookMarkCraft button listener setup complete");
            }
            else
            {
                Debug.LogError("[LobbyUIController] BookMarkCraft button reference is null!");
            }
        }

        private void RemoveButtonListeners()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(OnStartGameButtonClicked);
            }

            if (inventoryButton != null)
            {
                inventoryButton.onClick.RemoveListener(OnInventoryButtonClicked);
            }

            if (bookMarkCraftButton != null)
            {
                bookMarkCraftButton.onClick.RemoveListener(OnBookMarkCraftButtonClicked);
            }
        }

        private void OnStartGameButtonClicked()
        {
            Debug.Log("[LobbyUIController] StartGame button clicked - Loading GameScene");
            LoadGameSceneAsync().Forget();
        }

        private void OnInventoryButtonClicked()
        {
            Debug.Log("[LobbyUIController] Inventory button clicked - Loading Inventory Scene");
            LoadInventorySceneAsync().Forget();
        }

        private void OnBookMarkCraftButtonClicked()
        {
            Debug.Log("[LobbyUIController] BookMarkCraft button clicked - Loading BookMarkCraftScene");
            LoadBookMarkCraftSceneAsync().Forget();
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("GameScene3D");
        }

        private async UniTaskVoid LoadInventorySceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("Inventory");
        }

        private async UniTaskVoid LoadBookMarkCraftSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("BookMarkCraftScene");
        }
    }
}