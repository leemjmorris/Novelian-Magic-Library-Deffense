using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DeckSlot : MonoBehaviour
{
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI levelText;

    private int slotIndex;
    private System.Action<int> onSlotClicked;

    public void Initialize(int index, System.Action<int> callback)
    {
        slotIndex = index;
        onSlotClicked = callback;
        //ClearSlot(); // 초기화 시 빈 슬롯으로 시작
    }

    /// <summary>
    /// Inspector Button OnClick에 연결
    /// </summary>
    public void OnSlotClicked()
    {
        onSlotClicked?.Invoke(slotIndex);
    }

    /// <summary>
    /// 캐릭터 설정 (비동기 Addressables)
    /// </summary>
    public void SetCharacter(CharacterData characterData)
    {
        if (characterData == null)
        {
            ClearSlot();
            return;
        }

        // 이미지 로드 (비동기)
        string spriteKey = "ChaIcon";
        Addressables.LoadAssetAsync<Sprite>(spriteKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (characterImage != null)
                {
                    characterImage.sprite = handle.Result;
                    characterImage.enabled = true;
                }
            }
            else
            {
                Debug.LogWarning($"[DeckSlot] Failed to load sprite: {spriteKey}");
            }
        };

        // 캐릭터 이름 표시
        if (characterNameText != null)
        {
            characterNameText.text = characterData.Character_Name;
            characterNameText.enabled = true;
        }

        // 레벨 표시
        if (levelText != null)
        {
            levelText.text = "Lv.1";
            levelText.enabled = true;
        }
    }

    /// <summary>
    /// 슬롯 비우기
    /// </summary>
    public void ClearSlot()
    {
        if (characterImage != null)
        {
            characterImage.sprite = null;
        }

        if (characterNameText != null)
        {
            characterNameText.text = "이름";
        }

        if (levelText != null)
        {
            levelText.text = "Lv 0";
        }
    }
}
