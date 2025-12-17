using NovelianMagicLibraryDefense.Managers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    [SerializeField] private Button closeButton;

    [Header("Shop")]
    [SerializeField] private Button shopButton;
    [SerializeField] private GameObject shopPanel;

    [Header("Package")]
    [SerializeField] private Button packageButton;
    [SerializeField] private GameObject packagePanel;

    [Header("Costume")]
    [SerializeField] private Button costumeButton;
    [SerializeField] private GameObject costumePanel;

    [Header("Gacha")]
    [SerializeField] private Button gachaButton;
    [SerializeField] private GameObject gachaPanel;

    private void OnEnable()
    {
        if (shopButton != null) shopButton.onClick.AddListener(OnShopButtonClicked);
        if (packageButton != null) packageButton.onClick.AddListener(OnPackageButtonClicked);
        if (costumeButton != null) costumeButton.onClick.AddListener(OnCostumeButtonClicked);
        if (gachaButton != null) gachaButton.onClick.AddListener(OnGachaButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnDisable()
    {
        if (shopButton != null) shopButton.onClick.RemoveListener(OnShopButtonClicked);
        if (packageButton != null) packageButton.onClick.RemoveListener(OnPackageButtonClicked);
        if (costumeButton != null) costumeButton.onClick.RemoveListener(OnCostumeButtonClicked);
        if (gachaButton != null) gachaButton.onClick.RemoveListener(OnGachaButtonClicked);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseButtonClicked);
    }

    public void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
    }

    public void OnShopButtonClicked()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
        // shopPanel.SetActive(true);
        // packagePanel.SetActive(false);
        // costumePanel.SetActive(false);
        // gachaPanel.SetActive(false);
    }

    public void OnPackageButtonClicked()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
        // shopPanel.SetActive(false);
        // packagePanel.SetActive(true);
        // costumePanel.SetActive(false);
        // gachaPanel.SetActive(false);
    }

    public void OnCostumeButtonClicked()
    {
        WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
        // shopPanel.SetActive(false);
        // packagePanel.SetActive(false);
        // costumePanel.SetActive(true);
        // gachaPanel.SetActive(false);
    }

    public void OnGachaButtonClicked()
    {
        shopPanel.SetActive(false);
        // packagePanel.SetActive(false);
        // costumePanel.SetActive(false);
        gachaPanel.SetActive(true);
    }
}
