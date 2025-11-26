using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class DeckCharacterSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI sliderLevelText; 
    [SerializeField] private Slider sliderLevel;
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

        // 강화 레벨 가져오기
        int enhancementLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(characterData.Character_ID);

        // 레벨 텍스트 표시 (Lv 1 형식)
        if (levelText != null)
        {
            levelText.text = $"Lv {enhancementLevel}";
            levelText.enabled = true;
        }

        // 슬라이더 레벨 텍스트 표시 (1/10 형식)
        if (sliderLevelText != null)
        {
            sliderLevelText.text = $"{enhancementLevel}/10";
            sliderLevelText.enabled = true;
        }

        // 슬라이더 값 설정
        if (sliderLevel != null)
        {
            sliderLevel.value = enhancementLevel / 10f;
        }

        // 캐릭터 스프라이트 로드 (비동기)
        string spriteKey = "ChaIcon";
        Addressables.LoadAssetAsync<Sprite>(spriteKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (image != null)
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
