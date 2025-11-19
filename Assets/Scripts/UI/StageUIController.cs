using UnityEngine;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.Core;
using Cysharp.Threading.Tasks;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Controls UI interactions in the Stage scene
    /// Handles scene transitions from Stage to Game
    /// </summary>
    public class StageUIController : MonoBehaviour
    {
        [Header("Button References")]
        [SerializeField] private Button stageNameButton;

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
            if (stageNameButton != null)
            {
                stageNameButton.onClick.AddListener(OnStageNameButtonClicked);
                Debug.Log("[StageUIController] StageName button listener setup complete");
            }
            else
            {
                Debug.LogError("[StageUIController] StageName button reference is null!");
            }
        }

        private void RemoveButtonListeners()
        {
            if (stageNameButton != null)
            {
                stageNameButton.onClick.RemoveListener(OnStageNameButtonClicked);
            }
        }

        private void OnStageNameButtonClicked()
        {
            Debug.Log("[StageUIController] StageName button clicked - Loading GameScene");
            LoadGameSceneAsync().Forget();
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("GameScene");
        }
    }
}
