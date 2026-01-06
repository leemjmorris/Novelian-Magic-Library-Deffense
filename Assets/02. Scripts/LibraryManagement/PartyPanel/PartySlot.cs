using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class PartySlot : MonoBehaviour
{
    [SerializeField] private Image characterIcon;
    [SerializeField] private Image genreIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;

    private int characterId;

    public void Init(int charId)
    {
        GameLog.Log($"[PartySlot] Init() called with charId: {charId}");
        characterId = charId;

        if (characterId <= 0)
        {
            GameLog.LogWarning($"[PartySlot] Invalid characterId: {charId}, clearing slot");
            ClearSlot();
            return;
        }

        var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
        if (characterData == null)
        {
            GameLog.LogWarning($"[PartySlot] CharacterData not found for ID: {characterId}");
            ClearSlot();
            return;
        }

        // 캐릭터 이름
        var stringData = CSVLoader.Instance.GetData<StringTable>(characterData.Character_Name_ID);
        if (nameText != null)
        {
            nameText.text = stringData?.Text ?? "???";
        }
        else
        {
            GameLog.LogWarning($"[PartySlot] nameText is null for character ID: {characterId}. Check prefab references!");
        }

        // 캐릭터 레벨
        if (levelText != null)
        {
            int enhanceLevel = 1;
            if (CharacterEnhancementManager.Instance != null)
            {
                enhanceLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(characterId);
            }
            levelText.text = $"Lv.{enhanceLevel}";
            GameLog.Log($"[PartySlot] Set level text to 'Lv.{enhanceLevel}' for character ID: {characterId}");
        }
        else
        {
            GameLog.LogWarning($"[PartySlot] levelText is null for character ID: {characterId}. Check prefab references!");
        }

        // 캐릭터 아이콘
        LoadCharacterIcon(characterData);

        // 장르 아이콘
        LoadGenreIcon((int)characterData.Genre);
    }

    private void LoadCharacterIcon(CharacterData characterData)
    {
        if (characterIcon == null)
        {
            GameLog.LogWarning($"[PartySlot] characterIcon is null. Check prefab references!");
            return;
        }

        string spriteKey = AddressableKey.Icon_Character;

        if (characterData.Path_ID > 0)
        {
            var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
            if (pathData != null && !string.IsNullOrEmpty(pathData.Addressable_Key))
            {
                spriteKey = pathData.Addressable_Key;
            }
        }

        Addressables.LoadAssetAsync<Sprite>(spriteKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && characterIcon != null)
            {
                characterIcon.sprite = handle.Result;
            }
        };
    }

    private void LoadGenreIcon(int genreId)
    {
        if (genreIcon == null)
        {
            GameLog.LogWarning($"[PartySlot] genreIcon is null. Check prefab references!");
            return;
        }

        // Genre enum: 1=Horror, 2=Romance, 3=Adventure, 4=Comedy, 5=Mystery
        string genreKey = genreId switch
        {
            1 => AddressableKey.IconHorror,
            2 => AddressableKey.IconRomance,
            3 => AddressableKey.IconAdventure,
            4 => AddressableKey.IconComedy,
            5 => AddressableKey.Icon_Mystery,
            _ => AddressableKey.Icon_Mystery
        };

        Addressables.LoadAssetAsync<Sprite>(genreKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && genreIcon != null)
            {
                genreIcon.sprite = handle.Result;
            }
        };
    }

    public void ClearSlot()
    {
        characterId = 0;
        if (nameText != null) nameText.text = "";
        if (levelText != null) levelText.text = "";
        if (characterIcon != null) characterIcon.sprite = null;
        if (genreIcon != null) genreIcon.sprite = null;
    }
}
