using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
public class CharacterPanel : MonoBehaviour
{
    [SerializeField] private const int MAX_CHARACTERS = 20;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject characterSlotPrefab;
    [SerializeField] private GameObject characterInfoPanel;
    [SerializeField] private CharacterInfoPanel infoPanel;

    
    private List<LibraryCharacterSlot> characterSlots = new List<LibraryCharacterSlot>();

    private async UniTaskVoid Start()
    {
        // CSV 로딩 완료될 때까지 대기
        await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);
        
        CsvTable<CharacterData> characterTable = CSVLoader.Instance.GetTable<CharacterData>();
        
        // null 체크도 추가
        if (characterTable == null)
        {
            Debug.LogError("CharacterTable is null after loading!");
            return;
        }

        int slotIndex = 0;
        foreach (var characterData in characterTable.GetAll())
        {
            if (slotIndex >= MAX_CHARACTERS) break;

            var slot = Instantiate(characterSlotPrefab, contentParent);
            var characterSlot = slot.GetComponent<LibraryCharacterSlot>();

            // InfoPanel 연결
            characterSlot.SetInfoPanelObj(characterInfoPanel);

            // 테이블 데이터로 초기화
            characterSlot.InitSlot(characterData);

            characterSlots.Add(characterSlot);
            slotIndex++;
        }
        characterInfoPanel.SetActive(false);
    }
}

// 장르 이름 강화 정렬