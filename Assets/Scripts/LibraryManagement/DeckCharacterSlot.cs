using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class DeckCharacterSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image chaImage;
    [SerializeField] private Image genreIcon;

    private int characterId;
    public int CharacterId => characterId;

    // TeamSetupPanel 참조
    private TeamSetupPanel teamSetupPanel;

    /// <summary>
    /// TeamSetupPanel 연결
    /// </summary>
    public void SetPanel(TeamSetupPanel panel)
    {
        teamSetupPanel = panel;
    }

    /// <summary>
    /// 캐릭터 ID로 슬롯 초기화
    /// </summary>
    public void Init(int characterId)
    {
        this.characterId = characterId;

        // 1. CharacterData 가져오기
        var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
        if (characterData == null)
        {
            Debug.LogWarning($"[DeckCharacterSlot] CharacterData not found for ID: {characterId}");
            return;
        }

        // 2. 캐릭터 이름 설정
        var stringData = CSVLoader.Instance.GetData<StringTable>(characterData.Character_Name_ID);
        characterNameText.text = stringData?.Text ?? "Unknown";

        // 3. 캐릭터 레벨 설정
        int level = CharacterEnhancementManager.Instance != null
            ? CharacterEnhancementManager.Instance.GetEnhancementLevel(characterId)
            : 1;
        levelText.text = $"Lv {level}";

        // 4. 캐릭터 이미지 로드
        LoadCharacterImage();

        // 5. 장르 아이콘 로드
        LoadGenreIcon(characterData.Genre);
    }

    /// <summary>
    /// 캐릭터 정보 갱신 (승급 후 레벨 등 업데이트)
    /// </summary>
    public void RefreshCharacterInfo()
    {
        if (characterId <= 0) return;

        // 레벨 갱신
        int level = CharacterEnhancementManager.Instance != null
            ? CharacterEnhancementManager.Instance.GetEnhancementLevel(characterId)
            : 1;
        if (levelText != null)
            levelText.text = $"Lv {level}";
    }

    /// <summary>
    /// 캐릭터 이미지 로드 (Addressables)
    /// </summary>
    private void LoadCharacterImage()
    {
        Addressables.LoadAssetAsync<Sprite>(AddressableKey.Icon_Character).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                chaImage.sprite = handle.Result;
            }
            else
            {
                Debug.LogWarning($"[DeckCharacterSlot] Failed to load character image: {AddressableKey.Icon_Character}");
            }
        };
    }

    /// <summary>
    /// 장르 아이콘 로드 (Addressables)
    /// </summary>
    private void LoadGenreIcon(Genre genre)
    {
        string genreKey = GetGenreIconKey(genre);

        Addressables.LoadAssetAsync<Sprite>(genreKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                genreIcon.sprite = handle.Result;
            }
            else
            {
                Debug.LogWarning($"[DeckCharacterSlot] Failed to load genre icon: {genreKey}");
            }
        };
    }

    /// <summary>
    /// 장르에 따른 아이콘 키 반환
    /// </summary>
    private string GetGenreIconKey(Genre genre)
    {
        return genre switch
        {
            Genre.Horror => AddressableKey.IconHorror,
            Genre.Romance => AddressableKey.IconRomance,
            Genre.Adventure => AddressableKey.IconAdventure,
            Genre.Comedy => AddressableKey.IconComedy,
            Genre.Mystery => AddressableKey.Icon_Mystery,
            _ => AddressableKey.Icon_Mystery
        };
    }

    /// <summary>
    /// 캐릭터 슬롯 클릭 시 호출 (Inspector Button OnClick에서 연결)
    /// </summary>
    public void OnClicked()
    {
        Debug.Log($"[DeckCharacterSlot] Clicked: {characterNameText.text} (ID: {characterId})");

        // TeamSetupPanel에 선택된 캐릭터 전달
        if (teamSetupPanel != null)
        {
            teamSetupPanel.OnCharacterSelected(characterId);
        }
    }
}
