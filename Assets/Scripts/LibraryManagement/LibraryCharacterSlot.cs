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
    [SerializeField] private TextMeshProUGUI characterExp;
    [SerializeField] private TextMeshProUGUI characterLevel;
    [SerializeField] private Slider characterExpBar;
    [SerializeField] private Image characterSprite;
    [SerializeField] private Button characterInfoButton;
    private int currentLevel;
    private CharacterInfoPanel infoPanel;

    private void Start()
    {
        characterInfoButton.onClick.AddListener(OnClickCharacterInfo);


    }

    private void OnDestroy()
    {
        characterInfoButton.onClick.RemoveListener(OnClickCharacterInfo);
    }

    private void OnClickCharacterInfo()
    {
        Debug.Log($"Character Info Clicked for ID: {characterID}");
        infoPanel.InitInfo(characterID, currentLevel);
        infoPanel.ShowPanel();
    }

    public void SetInfoPanelObj(GameObject panel)
    {
        infoPanel = panel.GetComponent<CharacterInfoPanel>();
    }

    public void InitSlot(CharacterData data)
    {
        // 1. 캐릭터 ID 저장
        characterID = data.Character_ID;

        // 2. 캐릭터 이름 표시
        characterName.text = data.Character_Name;

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
            characterLevel.text = $"Lv.{currentLevel}";

            // 7. 경험치 바 초기화 (현재는 0/100으로 임시 설정)
            // 나중에 저장 데이터에서 현재 경험치와 최대 경험치 가져올 것
            UpdateUI(currentLevel, 10);
        }

        // 8. 캐릭터 스프라이트 로드 (Addressables 사용)
        LoadCharacterSprite(data.Character_ID);
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
    // 캐릭터 스프라이트 로드
    private void LoadCharacterSprite(int characterId)
    {
        string spriteKey = "ChaIcon";
        //= AddressableKey.GetCardSpriteKey(characterId);

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
    public void UpdateUI(int exp, int maxExp)
    {
        characterExp.text = $"{exp} / {maxExp}";
        characterExpBar.maxValue = maxExp;
        characterExpBar.value = exp;
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
            characterLevel.text = $"Lv.{currentLevel}";

            // InfoPanel이 열려있다면 갱신
            if (infoPanel != null)
            {
                infoPanel.InitInfo(characterID, currentLevel);
            }
        }
    }
}
