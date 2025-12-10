using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

/// <summary>
/// JML: CharacterCardGrid의 각 슬롯에 표시되는 캐릭터 카드 (Issue #424)
/// 소환된 캐릭터 정보 표시 (아이콘, 이름, 성급)
/// </summary>
public class ChaCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterInfoText;
    [SerializeField] private Image backgroundImage;

    [Header("Star Icons (성급 표시)")]
    [SerializeField] private GameObject star1;
    [SerializeField] private GameObject star2;
    [SerializeField] private GameObject star3;

    [Header("Empty State")]
    [SerializeField] private Color emptyBackgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color activeBackgroundColor = Color.white;

    private int characterId = -1;
    private int starTier = 1;
    private bool isEmpty = true;

    public int CharacterId => characterId;
    public int StarTier => starTier;
    public bool IsEmpty => isEmpty;

    private void Awake()
    {
        // 시작 시 빈 상태로 초기화
        SetEmpty();
    }

    /// <summary>
    /// JML: 캐릭터 정보로 카드 초기화
    /// CharacterCardGridManager에서 캐릭터 소환 시 호출
    /// </summary>
    /// <param name="charId">캐릭터 ID (CharacterTable)</param>
    /// <param name="tier">성급 (1~3)</param>
    public async UniTask Initialize(int charId, int tier = 1)
    {
        characterId = charId;
        starTier = Mathf.Clamp(tier, 1, 3);
        isEmpty = false;

        // 1. CSV에서 캐릭터 데이터 로드
        if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            var characterData = CSVLoader.Instance.GetData<CharacterData>(charId);
            if (characterData != null)
            {
                // 캐릭터 이름 설정
                var stringData = CSVLoader.Instance.GetData<StringTable>(characterData.Character_Name_ID);
                if (characterNameText != null)
                {
                    characterNameText.text = stringData?.Text ?? $"Character_{charId}";
                }

                // 캐릭터 정보 (성급 표시)
                if (characterInfoText != null)
                {
                    characterInfoText.text = $"{starTier}성";
                }

                // 아이콘 로드
                if (iconImage != null)
                {
                    string addressableKey = null;

                    // Path_ID가 있으면 PathTable에서 조회
                    if (characterData.Path_ID > 0)
                    {
                        var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
                        addressableKey = pathData?.Addressable_Key;
                    }

                    // Path_ID가 0이거나 PathData가 없으면 기본 아이콘 사용
                    if (string.IsNullOrEmpty(addressableKey))
                    {
                        addressableKey = AddressableKey.Icon_Character; // "ChaIcon"
                    }

                    try
                    {
                        var sprite = await Addressables.LoadAssetAsync<Sprite>(addressableKey).ToUniTask();
                        iconImage.sprite = sprite;
                        iconImage.color = Color.white;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[ChaCard] Failed to load icon for character {charId}: {e.Message}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[ChaCard] CharacterData not found for ID: {charId}");
                SetFallbackData(charId);
            }
        }
        else
        {
            SetFallbackData(charId);
        }

        // 2. 성급 표시 업데이트
        UpdateStarDisplay();

        // 3. 배경 색상 활성화
        if (backgroundImage != null)
        {
            backgroundImage.color = activeBackgroundColor;
        }

        Debug.Log($"[ChaCard] Initialized: Character {charId}, {starTier}성");
    }

    /// <summary>
    /// JML: 성급 업데이트 (중복 카드 선택 시)
    /// </summary>
    public void UpdateStarTier(int newTier)
    {
        starTier = Mathf.Clamp(newTier, 1, 3);

        if (characterInfoText != null)
        {
            characterInfoText.text = $"{starTier}성";
        }

        UpdateStarDisplay();
        Debug.Log($"[ChaCard] Star tier updated: Character {characterId} → {starTier}성");
    }

    /// <summary>
    /// JML: 빈 슬롯 상태로 설정
    /// </summary>
    public void SetEmpty()
    {
        characterId = -1;
        starTier = 1;
        isEmpty = true;

        // 텍스트 초기화
        if (characterNameText != null)
        {
            characterNameText.text = "";
        }

        if (characterInfoText != null)
        {
            characterInfoText.text = "";
        }

        // 아이콘 숨기기/투명하게
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = new Color(1f, 1f, 1f, 0.2f);
        }

        // 성급 아이콘 숨기기
        if (star1 != null) star1.SetActive(false);
        if (star2 != null) star2.SetActive(false);
        if (star3 != null) star3.SetActive(false);

        // 배경 딤 처리
        if (backgroundImage != null)
        {
            backgroundImage.color = emptyBackgroundColor;
        }
    }

    /// <summary>
    /// JML: CSV 로드 실패 시 기본 데이터 설정
    /// </summary>
    private void SetFallbackData(int charId)
    {
        if (characterNameText != null)
        {
            characterNameText.text = $"Character_{charId}";
        }

        if (characterInfoText != null)
        {
            characterInfoText.text = $"{starTier}성";
        }
    }

    /// <summary>
    /// JML: 성급 아이콘 표시 업데이트
    /// </summary>
    private void UpdateStarDisplay()
    {
        if (star1 != null) star1.SetActive(starTier >= 1);
        if (star2 != null) star2.SetActive(starTier >= 2);
        if (star3 != null) star3.SetActive(starTier >= 3);
    }
}
