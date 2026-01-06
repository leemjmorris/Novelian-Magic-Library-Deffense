using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 테스트용 AP(행동력) 추가 버튼
/// </summary>
public class ApButton : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int apAmount = 1;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(AddAP);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(AddAP);
        }
    }

    /// <summary>
    /// AP(행동력) 추가
    /// </summary>
    public void AddAP()
    {
        if (CurrencyManager.Instance == null)
        {
            GameLog.LogWarning("[ApButton] CurrencyManager가 없습니다.");
            return;
        }

        CurrencyManager.Instance.AddCurrency(CurrencyManager.AP_ID, apAmount);
        GameLog.Log($"[ApButton] AP +{apAmount} 추가됨. 현재 AP: {CurrencyManager.Instance.GetCurrency(CurrencyManager.AP_ID)}");
    }

    /// <summary>
    /// 지정한 양만큼 AP 추가
    /// </summary>
    public void AddAP(int amount)
    {
        if (CurrencyManager.Instance == null)
        {
            GameLog.LogWarning("[ApButton] CurrencyManager가 없습니다.");
            return;
        }

        CurrencyManager.Instance.AddCurrency(CurrencyManager.AP_ID, amount);
        GameLog.Log($"[ApButton] AP +{amount} 추가됨. 현재 AP: {CurrencyManager.Instance.GetCurrency(CurrencyManager.AP_ID)}");
    }

    /// <summary>
    /// AP를 최대치로 채우기
    /// </summary>
    public void FillAPToMax()
    {
        if (CurrencyManager.Instance == null)
        {
            GameLog.LogWarning("[ApButton] CurrencyManager가 없습니다.");
            return;
        }

        int maxAP = CurrencyManager.Instance.GetMaxAP();
        int currentAP = CurrencyManager.Instance.GetCurrency(CurrencyManager.AP_ID);
        int needed = maxAP - currentAP;

        if (needed > 0)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyManager.AP_ID, needed);
            GameLog.Log($"[ApButton] AP 최대치로 채움. 현재 AP: {maxAP}/{maxAP}");
        }
        else
        {
            GameLog.Log($"[ApButton] AP가 이미 최대치입니다. 현재 AP: {currentAP}/{maxAP}");
        }
    }
}
