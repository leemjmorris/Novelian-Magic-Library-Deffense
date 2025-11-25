using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class DeckCharacterSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Image image;

    public void SetCharacter(CharacterData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning("[DeckCharacterSlot] characterData is null!");
            return;
        }

        // 캐릭터 이름 설정
        if (characterNameText != null)
        {
            characterNameText.text = characterData.Character_Name;
            characterNameText.enabled = true;
        }

        // 캐릭터 스프라이트 로드 (비동기)
        string spriteKey = "ChaIcon";
        Addressables.LoadAssetAsync<Sprite>(spriteKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (image != null) // Null 체크!
                {
                    image.sprite = handle.Result;
                }
            }
            else
            {
                Debug.LogWarning($"[DeckCharacterSlot] Failed to load sprite: {spriteKey}");
            }
        };
    }
}
