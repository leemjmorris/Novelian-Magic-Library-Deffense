using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button characterTabButton;
    [SerializeField] private Button partyTabButton;
    [SerializeField] private Button teamSetupTabButton;

    [Header("Panels")]
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private GameObject teamSetupPanel;

    [Header("Debug Settings")]
    [SerializeField] private bool enableUIDebug = true;

    private void Start()
    {
        characterTabButton.onClick.AddListener(OnCharacterTabClicked);
        partyTabButton.onClick.AddListener(OnPartyTabClicked);
        teamSetupTabButton.onClick.AddListener(OnTeamSetupTabClicked);

        // UI 클릭 문제 디버깅
        if (enableUIDebug)
        {
            CheckUIClickIssuesAsync().Forget();
        }
    }

    /// <summary>
    /// UI 클릭 문제 원인 체크 (비동기로 1초 후 실행)
    /// </summary>
    private async UniTaskVoid CheckUIClickIssuesAsync()
    {
        // 씬 로드 후 안정화 대기
        await UniTask.Delay(1000);

        Debug.Log("========== [UIClickDebug] LibraryManagementScene 상태 체크 ==========");

        // 1. FadeController 체크
        if (FadeController.Instance != null && FadeController.Instance.fadePanel != null)
        {
            bool panelActive = FadeController.Instance.fadePanel.activeSelf;
            float alpha = FadeController.Instance.fadeImage != null ? FadeController.Instance.fadeImage.color.a : -1;
            bool raycastTarget = FadeController.Instance.fadeImage != null && FadeController.Instance.fadeImage.raycastTarget;

            if (panelActive && raycastTarget)
            {
                Debug.LogError($"[UIClickDebug] ❌ FadePanel이 UI 클릭을 차단 중! panelActive={panelActive}, alpha={alpha:F2}, raycastTarget={raycastTarget}");
            }
            else
            {
                Debug.Log($"[UIClickDebug] ✅ FadePanel 정상 (active={panelActive}, alpha={alpha:F2})");
            }
        }

        // 2. EventSystem 체크
        var eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length == 0)
        {
            Debug.LogError("[UIClickDebug] ❌ EventSystem이 없습니다!");
        }
        else if (eventSystems.Length > 1)
        {
            Debug.LogWarning($"[UIClickDebug] ⚠️ EventSystem이 {eventSystems.Length}개 존재! (중복 문제 가능)");
            foreach (var es in eventSystems)
            {
                Debug.LogWarning($"  - {es.gameObject.name} (scene: {es.gameObject.scene.name})");
            }
        }
        else
        {
            Debug.Log($"[UIClickDebug] ✅ EventSystem 정상 ({eventSystems[0].gameObject.name})");
        }

        // 3. RaycastBlocker 체크
        var allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
        foreach (var img in allImages)
        {
            string name = img.gameObject.name.ToLower();
            if ((name.Contains("raycast") || name.Contains("blocker")) &&
                img.gameObject.activeInHierarchy && img.raycastTarget)
            {
                Debug.LogWarning($"[UIClickDebug] ⚠️ RaycastBlocker 활성화: {img.gameObject.name}");
            }
        }

        // 4. CanvasGroup 체크
        var canvasGroups = FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None);
        foreach (var cg in canvasGroups)
        {
            if (cg.gameObject.activeInHierarchy && !cg.interactable)
            {
                Debug.LogWarning($"[UIClickDebug] ⚠️ CanvasGroup interactable=false: {cg.gameObject.name}");
            }
        }

        Debug.Log("========== [UIClickDebug] 체크 완료 ==========");
    }
    
    private void OnDestroy()
    {
        characterTabButton.onClick.RemoveListener(OnCharacterTabClicked);
        partyTabButton.onClick.RemoveListener(OnPartyTabClicked);
        teamSetupTabButton.onClick.RemoveListener(OnTeamSetupTabClicked);
    }

    private void OnCharacterTabClicked()
    {
        Debug.Log("Character Tab Clicked");
        
        characterTabButton.interactable = false;
        partyTabButton.interactable = true;
        teamSetupTabButton.interactable = true;

        characterPanel.SetActive(true);
        partyPanel.SetActive(false);
        teamSetupPanel.SetActive(false);
        
    }
    
    private void OnPartyTabClicked()
    {
        Debug.Log("Party Tab Clicked");

        // characterTabButton.interactable = true;
        // partyTabButton.interactable = false;
        // teamSetupTabButton.interactable = true;

        // characterPanel.SetActive(false);
        // partyPanel.SetActive(true);
        // teamSetupPanel.SetActive(false);
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);

        
    }
    
    private void OnTeamSetupTabClicked()
    {
        Debug.Log("Team Setup Tab Clicked");

        characterTabButton.interactable = true;
        partyTabButton.interactable = true;
        teamSetupTabButton.interactable = false;

        characterPanel.SetActive(false);
        partyPanel.SetActive(false);
        teamSetupPanel.SetActive(true);
        
    }

    public void LobbyButtonClicked()
    {
        LoadLobbyScene().Forget();
    }

    private async UniTaskVoid LoadLobbyScene()
    {
        await FadeController.Instance.LoadSceneWithFade("LobbyScene");
    }
}