using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartySynergyInfoPanel : MonoBehaviour
{
    [Header("Party Info")]
    [SerializeField] private TextMeshProUGUI partyNameText;

    [Header("Character Slots")]
    [SerializeField] private PartySlot slot1;
    [SerializeField] private PartySlot slot2;
    [SerializeField] private PartySlot slot3;
    [SerializeField] private PartySlot slot4;

    [Header("Enhance Button")]
    [SerializeField] private Button enhanceButton;

    private int partyId;
    private PartySynergyData synergyData;

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

        // 캐릭터 슬롯 초기화 (Party_Size에 따라)
        int partySize = data.Party_Size;

        if (slot1 != null && partySize >= 1)
        {
            slot1.Init(data.Req_Char_1_ID);
        }

        if (slot2 != null && partySize >= 2)
        {
            slot2.Init(data.Req_Char_2_ID);
        }

        if (slot3 != null && partySize >= 3)
        {
            slot3.Init(data.Req_Char_3_ID);
        }

        if (slot4 != null && partySize >= 4)
        {
            slot4.Init(data.Req_Char_4_ID);
        }

        // 강화 버튼은 일단 비활성화 (추후 연결)
        if (enhanceButton != null)
        {
            enhanceButton.interactable = false;
        }

        Debug.Log($"[PartySynergyInfoPanel] Initialized - PartyID: {partyId}, Name: {partyNameText?.text}");
    }

    public int GetPartyId()
    {
        return partyId;
    }
}
