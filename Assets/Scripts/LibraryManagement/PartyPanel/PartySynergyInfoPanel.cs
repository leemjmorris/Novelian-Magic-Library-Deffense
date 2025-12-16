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

    private int partyId;
    private PartySynergyData synergyData;
    private PartySynergyEnhancementPanel enhancementPanel;

    public void Init(PartySynergyData data)
    {
        if (data == null)
        {
            Debug.LogError("[PartySynergyInfoPanel] PartySynergyData is null!");
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

        // 강화 버튼 활성화 조건: 파티 캐릭터 4명 모두 보유
        if (enhanceButton != null)
        {
            bool hasAllCharacters = CheckHasAllPartyCharacters(data);
            enhanceButton.interactable = hasAllCharacters;
            enhanceButton.onClick.RemoveAllListeners();
            enhanceButton.onClick.AddListener(OnEnhanceButtonClicked);
        }

        Debug.Log($"[PartySynergyInfoPanel] Initialized - PartyID: {partyId}, Name: {partyNameText?.text}");
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
            Debug.LogWarning("[PartySynergyInfoPanel] EnhancementPanel is not set!");
            return;
        }

        Debug.Log($"[PartySynergyInfoPanel] Enhance button clicked - PartyID: {partyId}");
        enhancementPanel.Initialize(synergyData);
        enhancementPanel.ShowPanel();
    }

    public int GetPartyId()
    {
        return partyId;
    }
}
