using System;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class LibraryCharacterSlot : MonoBehaviour
{
    private int characterID;
    public int CharacterID => characterID;
    [SerializeField] private TextMeshProUGUI characterName;
    [SerializeField] private TextMeshProUGUI characterSliderLevelText;
    [SerializeField] private TextMeshProUGUI characterLevel;
    [SerializeField] private Slider characterLevelBar;
    [SerializeField] private Image characterSprite;
    [SerializeField] private Image genreIcon;
    [SerializeField] private Button characterInfoButton;
    [SerializeField] private CanvasGroup canvasGroup;

    private int currentLevel;
    private CharacterInfoPanel infoPanel;
    private bool isOwned = true;

    private void Start()
    {
        if (characterInfoButton != null)
            characterInfoButton.onClick.AddListener(OnClickCharacterInfo);
    }

    private void OnDestroy()
    {
        if (characterInfoButton != null)
            characterInfoButton.onClick.RemoveListener(OnClickCharacterInfo);
    }

    private void OnClickCharacterInfo()
    {
        if (infoPanel == null)
        {
            Debug.LogError($"[LibraryCharacterSlot] infoPanel is null! Cannot open character info for ID: {characterID}");
            return;
        }

        Debug.Log($"Character Info Clicked for ID: {characterID}");
        infoPanel.InitInfo(characterID, currentLevel, this);
        infoPanel.ShowPanel();
    }

    public void SetInfoPanelObj(GameObject panel)
    {
        infoPanel = panel.GetComponent<CharacterInfoPanel>();
    }

    public void SetInfoPanel(CharacterInfoPanel panel)
    {
        infoPanel = panel;
    }

    public void InitSlot(CharacterData data)
    {
        // 1. 캐릭터 ID 저장
        characterID = data.Character_ID;

        // 2. 캐릭터 이름 표시
        if (characterName != null)
            characterName.text = CSVLoader.Instance.GetData<StringTable>(data.Character_Name_ID)?.Text ?? "Unknown";

        // 3. 현재 강화 레벨 (CharacterEnhancementManager에서 가져오기)
        int currentEnhanceLevel = 1;
        if (CharacterEnhancementManager.Instance != null)
        {
            currentEnhanceLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(characterID);
        }

        // 4. 해당 강화 레벨의 LevelData ID 가져오기
        int levelDataID = GetLevelDataID(data, currentEnhanceLevel);

        // 5. LevelData 테이블에서 실제 스탯 정보 가져오기
        LevelData levelData = CSVLoader.Instance.GetData<LevelData>(levelDataID);

        if (levelData != null)
        {
            currentLevel = levelData.Level;
            // 6. 레벨 표시
            if (characterLevel != null)
                characterLevel.text = $"Lv {currentEnhanceLevel}";
            if (characterSliderLevelText != null)
                characterSliderLevelText.text = $"{currentEnhanceLevel}/10";

            // 7. 레벨 슬라이더 설정 (강화 레벨 기준)
            if (characterLevelBar != null)
                characterLevelBar.value = currentEnhanceLevel / 10f;
        }

        // 8. 캐릭터 스프라이트 로드 (Addressables 사용)
        LoadCharacterSprite(data.Character_ID);

        // 9. 장르 아이콘 로드
        LoadGenreIcon(data.Genre);

        // 10. 보유 여부 확인 및 UI 적용
        ApplyOwnershipState();
    }

    /// <summary>
    /// 보유 여부에 따른 UI 상태 적용
    /// </summary>
    private void ApplyOwnershipState()
    {
        // 보유 여부 확인
        isOwned = CharacterOwnershipManager.Instance != null
            ? CharacterOwnershipManager.Instance.IsOwned(characterID)
            : true; // Manager 없으면 기본 보유로 처리

        if (isOwned)
        {
            // 보유 캐릭터: 밝게 + 클릭 가능
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            if (characterSprite != null)
            {
                characterSprite.color = Color.white;
            }
            if (characterInfoButton != null)
                characterInfoButton.interactable = true;
        }
        else
        {
            // 미보유 캐릭터: 검정색 + 클릭 불가 + 슬라이더 0
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f; // 불투명
            }
            if (characterSprite != null)
            {
                characterSprite.color = Color.black; // 검정색
            }
            if (characterInfoButton != null)
                characterInfoButton.interactable = false;

            // 미보유 캐릭터는 레벨 0으로 표시
            if (characterLevelBar != null)
                characterLevelBar.value = 0f;
            if (characterSliderLevelText != null)
                characterSliderLevelText.text = "0/10";
            if (characterLevel != null)
                characterLevel.text = "Lv 0";
        }
    }
    // 강화 레벨에 따라 올바른 LevelData ID 반환
    private int GetLevelDataID(CharacterData data, int enhanceLevel)
    {
        return enhanceLevel switch
        {
            1 => data.Cha_Level_1_ID,
            2 => data.Cha_Level_2_ID,
            3 => data.Cha_Level_3_ID,
            4 => data.Cha_Level_4_ID,
            5 => data.Cha_Level_5_ID,
            6 => data.Cha_Level_6_ID,
            7 => data.Cha_Level_7_ID,
            8 => data.Cha_Level_8_ID,
            9 => data.Cha_Level_9_ID,
            10 => data.Cha_Level_10_ID,
            _ => data.Cha_Level_1_ID
        };
    }
    /// <summary>
    /// 장르 아이콘 로드
    /// </summary>
    private void LoadGenreIcon(Genre genre)
    {
        if (genreIcon == null) return;

        string genreKey = genre switch
        {
            Genre.Horror => AddressableKey.IconHorror,
            Genre.Romance => AddressableKey.IconRomance,
            Genre.Adventure => AddressableKey.IconAdventure,
            Genre.Comedy => AddressableKey.IconComedy,
            Genre.Mystery => AddressableKey.Icon_Mystery,
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

    // 캐릭터 스프라이트 로드
    private void LoadCharacterSprite(int characterId)
    {
        string spriteKey = AddressableKey.Icon_Character;

        // CharacterData에서 Path_ID로 개별 아이콘 키 조회
        var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
        if (characterData != null && characterData.Path_ID > 0)
        {
            var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
            if (pathData != null && !string.IsNullOrEmpty(pathData.Addressable_Key))
            {
                spriteKey = pathData.Addressable_Key;
            }
        }

        Addressables.LoadAssetAsync<Sprite>(spriteKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                characterSprite.sprite = handle.Result;
            }
            else
            {
                Debug.LogWarning($"Failed to load character sprite: {spriteKey}");
            }
        };
    }
    /// <summary>
    /// 강화 후 캐릭터 레벨 갱신
    /// </summary>
    public void RefreshCharacterLevel()
    {
        // Manager에서 최신 강화 레벨 가져오기
        int currentEnhanceLevel = 1;
        if (CharacterEnhancementManager.Instance != null)
        {
            currentEnhanceLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(characterID);
        }

        // LevelData 갱신
        CharacterData data = CSVLoader.Instance.GetData<CharacterData>(characterID);
        if (data == null)
        {
            Debug.LogError($"CharacterData not found for ID: {characterID}");
            return;
        }

        int levelDataID = GetLevelDataID(data, currentEnhanceLevel);
        LevelData levelData = CSVLoader.Instance.GetData<LevelData>(levelDataID);

        if (levelData != null)
        {
            currentLevel = levelData.Level;
            if (characterSliderLevelText != null)
                characterSliderLevelText.text = $"{currentEnhanceLevel}/10";
            if (characterLevel != null)
                characterLevel.text = $"Lv {currentEnhanceLevel}";
            if (characterLevelBar != null)
                characterLevelBar.value = currentEnhanceLevel / 10f;

            // InfoPanel이 열려있다면 갱신
            if (infoPanel != null)
            {
                infoPanel.InitInfo(characterID, currentLevel);
            }
        }
    }
}
