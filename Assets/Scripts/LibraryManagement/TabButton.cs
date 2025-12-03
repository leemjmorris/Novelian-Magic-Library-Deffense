using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;
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
    
    private void Start()
    {
        characterTabButton.onClick.AddListener(OnCharacterTabClicked);
        partyTabButton.onClick.AddListener(OnPartyTabClicked);
        teamSetupTabButton.onClick.AddListener(OnTeamSetupTabClicked);
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