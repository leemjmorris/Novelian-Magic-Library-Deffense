using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 파티 시너지 강화 패널
/// - 시너지 강화 UI 표시
/// - Close 버튼으로 패널 닫기
/// </summary>
public class PartySynergyEnhancementPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Synergy Info")]
    [SerializeField] private TextMeshProUGUI synergyNameText;

    [Header("Character Slots")]
    [SerializeField] private PartySlot[] characterSlots;

    [Header("Required Level")]
    [SerializeField] private TextMeshProUGUI requiredLevelText;

    [Header("Current Level Panel")]
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI currentEffectText;

    [Header("Next Level Panel")]
    [SerializeField] private TextMeshProUGUI nextLevelText;
    [SerializeField] private TextMeshProUGUI nextEffectText;

    private PartySynergyData currentSynergyData;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    /// <summary>
    /// 시너지 데이터로 패널 초기화
    /// </summary>
    public void Initialize(PartySynergyData data)
    {
        if (data == null)
        {
            Debug.LogError("[PartySynergyEnhancementPanel] PartySynergyData is null!");
            return;
        }

        currentSynergyData = data;

        // 시너지 이름 표시
        if (synergyNameText != null)
        {
            var stringData = CSVLoader.Instance?.GetData<StringTable>(data.Party_Name_ID);
            synergyNameText.text = stringData?.Text ?? $"Party_{data.Party_ID}";
        }

        // 캐릭터 슬롯 초기화
        InitializeCharacterSlots(data);

        // 시너지 레벨 정보 업데이트
        int currentSynergyLevel = PartySynergyManager.Instance?.GetSynergyLevel() ?? 1;
        UpdateRequiredLevelText(data, currentSynergyLevel);
        UpdateSynergyLevelInfo(data, currentSynergyLevel);

        Debug.Log($"[PartySynergyEnhancementPanel] Initialized - PartyID: {data.Party_ID}, PartySize: {data.Party_Size}");
    }

    private void InitializeCharacterSlots(PartySynergyData data)
    {
        if (characterSlots == null) return;

        int[] charIds = { data.Req_Char_1_ID, data.Req_Char_2_ID, data.Req_Char_3_ID, data.Req_Char_4_ID };

        for (int i = 0; i < characterSlots.Length && i < data.Party_Size; i++)
        {
            characterSlots[i]?.Init(charIds[i]);
        }
    }

    private void UpdateRequiredLevelText(PartySynergyData data, int currentSynergyLevel)
    {
        if (requiredLevelText == null) return;

        // 다음 강화에 필요한 ID 가져오기
        int nextUpgradeId = GetNextUpgradeId(data, currentSynergyLevel);
        if (nextUpgradeId == 0)
        {
            requiredLevelText.text = "최대 레벨";
            return;
        }

        // 강화 데이터 조회
        var enhancementData = CSVLoader.Instance?.GetData<PartySynergyEnhancementData>(nextUpgradeId);
        if (enhancementData == null)
        {
            requiredLevelText.text = "";
            return;
        }

        // 필요 레벨 추출 (401 → 1, 402 → 2, 404 → 4 등)
        int requiredLevel = enhancementData.Need_Party_Characters_Lv % 100;

        // 캐릭터 레벨 체크
        bool allMeetLevel = CheckAllCharactersMeetLevel(data, requiredLevel);

        // 텍스트 설정 (Rich Text)
        if (allMeetLevel)
        {
            requiredLevelText.text = $"필요 캐릭터 레벨: Lv {requiredLevel}";
        }
        else
        {
            requiredLevelText.text = $"필요 캐릭터 레벨: <color=red>Lv {requiredLevel}</color>";
        }
    }

    private bool CheckAllCharactersMeetLevel(PartySynergyData data, int requiredLevel)
    {
        if (CharacterEnhancementManager.Instance == null) return false;

        int[] charIds = { data.Req_Char_1_ID, data.Req_Char_2_ID, data.Req_Char_3_ID, data.Req_Char_4_ID };

        for (int i = 0; i < data.Party_Size; i++)
        {
            int charLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(charIds[i]);
            if (charLevel < requiredLevel)
            {
                return false;
            }
        }
        return true;
    }

    private void UpdateSynergyLevelInfo(PartySynergyData data, int currentLevel)
    {
        // 시너지 이름 가져오기
        var synergyNameData = CSVLoader.Instance?.GetData<StringTable>(data.Party_Name_ID);
        string synergyName = synergyNameData?.Text ?? $"Party_{data.Party_ID}";

        // 현재 레벨 정보
        var (currentEffectId, currentEffectValue) = GetEffectData(data, currentLevel);
        string currentEffectName = GetEffectName(currentEffectId);

        if (currentLevelText != null)
        {
            currentLevelText.text = $"Lv {currentLevel} {synergyName}";
        }
        if (currentEffectText != null)
        {
            currentEffectText.text = $"{currentEffectName} +{currentEffectValue}%";
        }

        // 다음 레벨 정보
        int nextLevel = currentLevel + 1;
        if (nextLevel > 5)
        {
            // 최대 레벨
            if (nextLevelText != null) nextLevelText.text = "최대 레벨";
            if (nextEffectText != null) nextEffectText.text = "";
            return;
        }

        var (nextEffectId, nextEffectValue) = GetEffectData(data, nextLevel);
        string nextEffectName = GetEffectName(nextEffectId);

        if (nextLevelText != null)
        {
            nextLevelText.text = $"Lv {nextLevel} {synergyName}";
        }
        if (nextEffectText != null)
        {
            nextEffectText.text = $"{nextEffectName} +{nextEffectValue}%";
        }
    }

    private (int effectId, float effectValue) GetEffectData(PartySynergyData data, int level)
    {
        return level switch
        {
            1 => (data.Effect_1_ID, data.Effect_1_Value),
            2 => (data.Effect_2_ID, data.Effect_2_Value),
            3 => (data.Effect_3_ID, data.Effect_3_Value),
            4 => (data.Effect_4_ID, data.Effect_4_Value),
            5 => (data.Effect_5_ID, data.Effect_5_Value),
            _ => (0, 0f)
        };
    }

    private string GetEffectName(int effectId)
    {
        var effectData = CSVLoader.Instance?.GetData<PartySynergyEffectData>(effectId);
        if (effectData == null) return "시너지 효과";

        var stringData = CSVLoader.Instance?.GetData<StringTable>(effectData.Party_Effect_Name_ID);
        return stringData?.Text ?? "시너지 효과";
    }

    private int GetNextUpgradeId(PartySynergyData data, int currentLevel)
    {
        return currentLevel switch
        {
            1 => data.Party_Upgrade_Lv2_ID,
            2 => data.Party_Upgrade_Lv3_ID,
            3 => data.Party_Upgrade_Lv4_ID,
            4 => data.Party_Upgrade_Lv5_ID,
            _ => 0 // 최대 레벨
        };
    }

    /// <summary>
    /// 패널 열기
    /// </summary>
    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 패널 닫기
    /// </summary>
    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Close 버튼 클릭 시 호출
    /// </summary>
    private void OnCloseButtonClicked()
    {
        Debug.Log("[PartySynergyEnhancementPanel] Close button clicked");
        HidePanel();
    }

    /// <summary>
    /// 현재 시너지 데이터 반환
    /// </summary>
    public PartySynergyData GetCurrentSynergyData()
    {
        return currentSynergyData;
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }
    }
}
