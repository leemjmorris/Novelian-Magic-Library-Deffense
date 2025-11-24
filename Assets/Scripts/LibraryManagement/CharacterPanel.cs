using System.Collections.Generic;
using UnityEngine;

public class CharacterPanel : MonoBehaviour
{   
    [SerializeField] private const int MAX_CHARACTERS = 20; 
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject characterSlotPrefab;
    [SerializeField] private GameObject characterInfoPanel;
    [SerializeField] private CharacterInfoPanel infoPanel;
    private List<LibraryCharacterSlot> characterSlots = new List<LibraryCharacterSlot>();

    private void Start()
    {
        for (int i = 0; i < MAX_CHARACTERS; i++)
        {
            var slot = Instantiate(characterSlotPrefab, contentParent);
            slot.name = "CharacterSlot_" + i;

            var characterSlot = slot.GetComponent<LibraryCharacterSlot>();
            characterSlots.Add(characterSlot);
            characterSlots[i].SetInfoPanelObj(characterInfoPanel);
            characterSlots[i].InitSlot(i, "Character " + (i + 1), 1, 0, 1000, null); // Placeholder data
        }
        characterInfoPanel.SetActive(false);
    }
}
