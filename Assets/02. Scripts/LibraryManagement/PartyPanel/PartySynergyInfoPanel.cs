using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartySynergyInfoPanel : MonoBehaviour
{
    [Header("Party Info")]
    [SerializeField] private TextMeshProUGUI partyNameText;

    [Header("Character Slots")]
    [SerializeField] private PartySlot[] characterSlots;

    [Header("Enhance Button")]
    [SerializeField] private Button enhanceButton;
    [SerializeField] private TextMeshProUGUI enhanceButtonText;

    // 강화 가능 시 버튼 색상
    private static readonly Color EnhanceAvailableColor = new Color(1f, 0.569f, 0.329f, 1f); // #FF9154

    private int partyId;
    private PartySynergyData synergyData;
    private PartySynergyEnhancementPanel enhancementPanel;

    public void Init(PartySynergyData data)
    {
        if (data == null)
        {
            GameLog.LogError("[PartySynergyInfoPanel] PartySynergyData is null!");
            return;
        }

        partyId = data.Party_ID;
        synergyData = data;

        // 파티 이름 표시
        var stringData = CSVLoader.Instance.GetData<StringTable>(data.Party_Name_ID);
        if (partyNameText != null)
        {
            partyNameText.text = stringData?.Text ?? $"Party_{partyId}";
        }

        // 캐릭터 슬롯 초기화
        InitializeCharacterSlots(data);

        // 강화 버튼 설정
        if (enhanceButton != null)
        {
            bool hasAllCharacters = CheckHasAllPartyCharacters(data);
            bool isMaxLevel = CheckIsMaxLevel(data);

            // 최대 레벨이면 버튼 비활성화 및 텍스트 변경
            if (isMaxLevel)
            {
                enhanceButton.interactable = false;
                if (enhanceButtonText != null)
                {
                    enhanceButtonText.text = "최대 강화 상태입니다";
                }
            }
            else
            {
                enhanceButton.interactable = hasAllCharacters;
                enhanceButton.onClick.RemoveAllListeners();
                enhanceButton.onClick.AddListener(OnEnhanceButtonClicked);

                // 강화 가능 시 버튼 색상 변경 (#FF9154)
                if (hasAllCharacters)
                {
                    var colors = enhanceButton.colors;
                    colors.normalColor = EnhanceAvailableColor;
                    enhanceButton.colors = colors;
                }
            }
        }

        GameLog.Log($"[PartySynergyInfoPanel] Initialized - PartyID: {partyId}, Name: {partyNameText?.text}");
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

    private bool CheckHasAllPartyCharacters(PartySynergyData data)
    {
        if (CharacterOwnershipManager.Instance == null) return false;

        int[] charIds = { data.Req_Char_1_ID, data.Req_Char_2_ID, data.Req_Char_3_ID, data.Req_Char_4_ID };

        for (int i = 0; i < data.Party_Size; i++)
        {
            if (!CharacterOwnershipManager.Instance.IsOwned(charIds[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 시너지가 최대 레벨인지 확인 (레벨 5가 최대)
    /// </summary>
    private bool CheckIsMaxLevel(PartySynergyData data)
    {
        if (PartySynergyManager.Instance == null) return false;

        int currentLevel = PartySynergyManager.Instance.GetSynergyLevel(data.Party_ID);
        return currentLevel >= 5;
    }

    /// <summary>
    /// Enhancement Panel 참조 설정 (PartyPanel에서 호출)
    /// </summary>
    public void SetEnhancementPanel(PartySynergyEnhancementPanel panel)
    {
        enhancementPanel = panel;
    }

    /// <summary>
    /// 강화 버튼 클릭 시 호출
    /// </summary>
    private void OnEnhanceButtonClicked()
    {
        if (enhancementPanel == null)
        {
            GameLog.LogWarning("[PartySynergyInfoPanel] EnhancementPanel is not set!");
            return;
        }

        GameLog.Log($"[PartySynergyInfoPanel] Enhance button clicked - PartyID: {partyId}");
        enhancementPanel.Initialize(synergyData);
        enhancementPanel.ShowPanel();
    }

    public int GetPartyId()
    {
        return partyId;
    }
}
