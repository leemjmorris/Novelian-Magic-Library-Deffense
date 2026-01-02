using NovelianMagicLibraryDefense.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    [SerializeField] private Button closeButton;

    [Header("Currency Display")]
    [SerializeField] private TextMeshProUGUI apText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI premiumText;
    private int maxAP = 30;

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

    [Header("Gacha Sub Buttons (준비중 경고용)")]
    [SerializeField] private Button bookmarkGachaButton;
    [SerializeField] private Button costumeGachaButton;

    private void OnEnable()
    {
        if (shopButton != null) shopButton.onClick.AddListener(OnShopButtonClicked);
        if (packageButton != null) packageButton.onClick.AddListener(OnPackageButtonClicked);
        if (costumeButton != null) costumeButton.onClick.AddListener(OnCostumeButtonClicked);
        if (gachaButton != null) gachaButton.onClick.AddListener(OnGachaButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseButtonClicked);
        if (bookmarkGachaButton != null) bookmarkGachaButton.onClick.AddListener(OnFeatureNotReadyClicked);
        if (costumeGachaButton != null) costumeGachaButton.onClick.AddListener(OnFeatureNotReadyClicked);

        // 재화 UI 초기화 및 이벤트 구독
        InitializeCurrency();
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }

        // Shop BGM 재생 (Lobby BGM 일시정지 후 크로스페이드)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseBGMAndPlay("BGM_Shop", 1f);
        }
    }

    private void OnDisable()
    {
        if (shopButton != null) shopButton.onClick.RemoveListener(OnShopButtonClicked);
        if (packageButton != null) packageButton.onClick.RemoveListener(OnPackageButtonClicked);
        if (costumeButton != null) costumeButton.onClick.RemoveListener(OnCostumeButtonClicked);
        if (gachaButton != null) gachaButton.onClick.RemoveListener(OnGachaButtonClicked);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        if (bookmarkGachaButton != null) bookmarkGachaButton.onClick.RemoveListener(OnFeatureNotReadyClicked);
        if (costumeGachaButton != null) costumeGachaButton.onClick.RemoveListener(OnFeatureNotReadyClicked);

        // 재화 이벤트 구독 해제
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }

        // Shop 닫힐 때 Lobby BGM 재개 (크로스페이드)
        if (AudioManager.Instance != null && AudioManager.Instance.HasPausedBGM)
        {
            AudioManager.Instance.StopAndResumePausedBGM(1f);
        }
    }

    #region Currency Display

    private void InitializeCurrency()
    {
        // CurrencyTable에서 최대 AP 조회
        if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            var currencyData = CSVLoader.Instance.GetData<CurrencyData>(CurrencyManager.AP_ID);
            if (currencyData != null && currencyData.Currency_Max_Count > 0)
            {
                maxAP = currencyData.Currency_Max_Count;
            }
        }

        UpdateAPText();
        UpdateGoldText();
        UpdatePremiumText();
    }

    private void UpdateAPText()
    {
        if (apText == null) return;

        int currentAP = 0;
        if (CurrencyManager.Instance != null)
        {
            currentAP = CurrencyManager.Instance.GetCurrency(CurrencyManager.AP_ID);
        }

        apText.text = $"{currentAP}/{maxAP}";
    }

    private void UpdateGoldText()
    {
        if (goldText == null) return;

        int gold = 0;
        if (CurrencyManager.Instance != null)
        {
            gold = CurrencyManager.Instance.GetCurrency(CurrencyManager.GOLD_ID);
        }

        goldText.text = CurrencyManager.FormatCurrency(gold);
    }

    private void UpdatePremiumText()
    {
        if (premiumText == null) return;

        int magicStone = 0;
        if (CurrencyManager.Instance != null)
        {
            magicStone = CurrencyManager.Instance.GetCurrency(CurrencyManager.MAGIC_STONE_ID);
        }

        premiumText.text = $"{magicStone}";
    }

    private void OnCurrencyChanged(int currencyId, int newAmount)
    {
        if (currencyId == CurrencyManager.AP_ID && apText != null)
        {
            apText.text = $"{newAmount}/{maxAP}";
        }
        else if (currencyId == CurrencyManager.GOLD_ID && goldText != null)
        {
            goldText.text = CurrencyManager.FormatCurrency(newAmount);
        }
        else if (currencyId == CurrencyManager.MAGIC_STONE_ID && premiumText != null)
        {
            premiumText.text = $"{newAmount}";
        }
    }

    #endregion

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

    /// <summary>
    /// 준비중인 기능 클릭 시 경고 표시
    /// </summary>
    private void OnFeatureNotReadyClicked()
    {
        if (WarningUIManager.Instance != null)
        {
            WarningUIManager.Instance.ShowWarning(WarningText.FeatureNotReady);
        }
    }
}
