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

        // 캐릭터 슬롯 초기화
        if (slot1 != null)
        {
            slot1.Init(data.Req_Char_1_ID);
        }

        if (slot2 != null)
        {
            slot2.Init(data.Req_Char_2_ID);
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
