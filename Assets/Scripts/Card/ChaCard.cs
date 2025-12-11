using Cysharp.Threading.Tasks;
using Novelian.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

/// <summary>
/// JML: CharacterCardGrid의 각 슬롯에 표시되는 캐릭터 카드 (Issue #424)
/// 소환된 캐릭터 정보 표시 (아이콘, 이름, 스탯)
/// 스탯: 장착 스킬, 공격력, 공격속도, 치명타 확률, 치명타 배율
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
    private Character linkedCharacter; // 연결된 캐릭터 인스턴스

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
    /// <param name="character">연결할 Character 인스턴스 (스탯 표시용)</param>
    public async UniTask Initialize(int charId, int tier = 1, Character character = null)
    {
        characterId = charId;
        starTier = Mathf.Clamp(tier, 1, 3);
        isEmpty = false;
        linkedCharacter = character;

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

        // 3. 스탯 정보 업데이트
        UpdateStatDisplay();

        // 4. 배경 색상 활성화
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

    /// <summary>
    /// JML: 스탯 정보 표시 업데이트 (Issue #424)
    /// characterInfoText에 스탯 정보 표시
    /// 형식: 장착 스킬: {스킬명}\n공격력: {값}\n공격속도: {값}\n치명타 확률: {값}%\n치명타 배율: {값}%
    /// </summary>
    private void UpdateStatDisplay()
    {
        if (characterInfoText == null) return;

        if (linkedCharacter != null)
        {
            // 캐릭터 인스턴스에서 실제 스탯 가져오기
            string skillName = linkedCharacter.GetDisplaySkillName();
            float damage = linkedCharacter.GetDisplayDamage();
            float attackSpeed = linkedCharacter.GetDisplayAttackSpeed();
            float critChance = linkedCharacter.GetDisplayCritChance();
            float critMultiplier = linkedCharacter.GetDisplayCritMultiplier();

            characterInfoText.text = $"장착 스킬: {skillName}\n" +
                                     $"공격력: {damage:F1}\n" +
                                     $"공격속도: {attackSpeed:F2}\n" +
                                     $"치명타 확률: {critChance:F1}%\n" +
                                     $"치명타 배율: {critMultiplier:F1}%";
        }
        else
        {
            // 캐릭터 인스턴스가 없으면 기본값 표시
            characterInfoText.text = "장착 스킬: 없음\n" +
                                     "공격력: -\n" +
                                     "공격속도: -\n" +
                                     "치명타 확률: -\n" +
                                     "치명타 배율: -";
        }
    }

    /// <summary>
    /// JML: 캐릭터 인스턴스 연결 및 스탯 갱신 (Issue #424)
    /// 캐릭터 소환 완료 후 호출
    /// </summary>
    public void LinkCharacter(Character character)
    {
        linkedCharacter = character;
        UpdateStatDisplay();
    }

    /// <summary>
    /// JML: 스탯 갱신 (외부에서 호출 가능)
    /// 캐릭터 스탯이 변경될 때 호출
    /// </summary>
    public void RefreshStats()
    {
        UpdateStatDisplay();
    }
}
