using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using TMPro;

/// <summary>
/// 시작 카드 선택 시스템 - 2개 캐릭터 카드에서 선택
/// - 20초 타이머 (Time.timeScale 무시)
/// - 타임아웃 시 50% 확률 자동 선택
/// </summary>
public class CardSelectionManager : MonoBehaviour
{
    [Header("카드 패널")]
    public GameObject cardPanel;

    [Header("2개 카드")]
    public GameObject card1;
    public GameObject card2;

    [Header("타이머 텍스트 (선택사항)")]
    public TextMeshProUGUI timerText;

    [Header("사용 가능한 캐릭터 ID")]
    //[SerializeField] private List<int> availableCharacterIds = new List<int> { 1, 2, 3, 4, 5 };

    // 카드 선택 타임아웃 (20초)
    private const float SELECTION_TIME = 20f;

    // 선택된 캐릭터 ID
    private int selectedCard1Id;
    private int selectedCard2Id;
    private bool isCardSelected = false;

    // 카드 스프라이트 캐시 (임시: 사용 안 함)
    // private Dictionary<int, Sprite> cardSprites = new Dictionary<int, Sprite>();

    // 타이머 취소 토큰
    private CancellationTokenSource selectionCts;

    // CharacterPlacementManager 캐싱
    [SerializeField] private CharacterPlacementManager placementManager;

    private void Awake()
    {
        // CharacterPlacementManager를 태그로 찾아서 캐싱
        GameObject managerObj = GameObject.FindGameObjectWithTag("CharacterPlacementManager");
        if (managerObj != null)
        {
            placementManager = managerObj.GetComponent<CharacterPlacementManager>();
        }
    }

    // 임시: 카드 스프라이트 로드 비활성화 (CharacterCard 프리팹만 사용)
    // private async void Start()
    // {
    //     await PreloadCardSprites();
    // }

    /// <summary>
    /// 게임 시작 시 카드 선택 (StageManager에서 호출)
    /// </summary>
    public async UniTask ShowStartCards()
    {
        isCardSelected = false;

        // 1. 카드 패널 활성화
        if (cardPanel != null)
        {
            cardPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[CardSelectionManager] cardPanel이 null입니다! Inspector에서 할당해주세요.");
        }

        // 2. 2개 랜덤 캐릭터 로드
        LoadTwoRandomCharacters();

        // 3. 20초 타이머 시작 & 선택 대기
        await WaitForSelection();

        // 4. 카드 패널 비활성화
        if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 카드 선택 대기 (20초 타이머)
    /// </summary>
    async UniTask WaitForSelection()
    {
        selectionCts?.Dispose();
        selectionCts = new CancellationTokenSource();
        float remainingTime = SELECTION_TIME;

        try
        {
            while (remainingTime > 0 && !isCardSelected)
            {
                // 타이머 텍스트 업데이트
                if (timerText != null)
                {
                    timerText.text = $"{(int)remainingTime}s";
                }

                // 1초 대기 (ignoreTimeScale=true로 Time.timeScale 무시)
                await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: selectionCts.Token);
                remainingTime -= 1f;
            }

            // 타임아웃 시 50% 확률 자동 선택
            if (!isCardSelected)
            {
                AutoSelectCard();
            }
        }
        catch (OperationCanceledException)
        {
            // Timer cancelled (normal when card selected)
        }
        finally
        {
            selectionCts?.Dispose();
            selectionCts = null;
        }
    }

    /// <summary>
    /// 2개의 랜덤 캐릭터 로드 (중복 방지)
    /// </summary>
    void LoadTwoRandomCharacters()
    {
        // DeckManager에서 덱 가져오기
        List<int> availableCharacterIds = DeckManager.Instance != null
            ? DeckManager.Instance.GetDeck()
            : new List<int> { 1, 2, 3, 4, 5 }; // Fallback (DeckManager 없을 때만)

        if (availableCharacterIds == null || availableCharacterIds.Count == 0)
        {
            Debug.LogError("[CardSelectionManager] 덱이 비어있습니다!");
            return;
        }

        // 랜덤 선택 (중복 허용)
        int idx1 = UnityEngine.Random.Range(0, availableCharacterIds.Count);
        int idx2 = UnityEngine.Random.Range(0, availableCharacterIds.Count);

        selectedCard1Id = availableCharacterIds[idx1];
        selectedCard2Id = availableCharacterIds[idx2];

        UpdateCardUI(card1, selectedCard1Id);
        UpdateCardUI(card2, selectedCard2Id);

        Debug.Log($"[CardSelectionManager] 덱에서 랜덤 캐릭터 선택: ID {selectedCard1Id}, ID {selectedCard2Id}");
    }

    /// <summary>
    /// 카드 UI 업데이트 (CharacterID 기반)
    /// </summary>
    void UpdateCardUI(GameObject cardObj, int characterId)
    {
        if (cardObj == null) return;

        CharacterCard charCard = cardObj.GetComponent<CharacterCard>();
        if (charCard == null) return;

        // 캐릭터 데이터 가져오기 (임시: 하드코딩, 나중에 CSV로 대체)
        string characterName = GetCharacterName(characterId);
        GenreType genreType = GetCharacterGenre(characterId);

        // 임시: 스프라이트 없이 업데이트 (CharacterCard 프리팹의 기본 스프라이트 사용)
        charCard.UpdateCharacter(null, characterName, genreType);
    }

    /// <summary>
    /// 캐릭터 이름 가져오기 (임시: 하드코딩, 나중에 CSV로 대체)
    /// </summary>
    string GetCharacterName(int characterId)
    {
        // 나중에 CSV 연동: return CSVLoader.Get<CharacterTableData>(characterId).CharacterName;
        switch (characterId)
        {
            case 1: return "Horror Warrior";
            case 2: return "Romance Mage";
            case 3: return "Adventure Ranger";
            case 4: return "Comedy Jester";
            case 5: return "Mystery Detective";
            default: return "Unknown Character";
        }
    }

    /// <summary>
    /// 캐릭터 장르 가져오기 (임시: 하드코딩, 나중에 CSV로 대체)
    /// </summary>
    GenreType GetCharacterGenre(int characterId)
    {
        // 나중에 CSV 연동: return (GenreType)CSVLoader.Get<CharacterTableData>(characterId).GenreType;
        return (GenreType)characterId; // 1=Horror, 2=Romance, 3=Adventure, 4=Comedy, 5=Mystery
    }


    /// <summary>
    /// 50% 확률로 자동 선택
    /// </summary>
    void AutoSelectCard()
    {
        if (UnityEngine.Random.value < 0.5f)
        {
            OnCard1Selected();
        }
        else
        {
            OnCard2Selected();
        }
    }

    /// <summary>
    /// Card 1 클릭 시 호출 (Button.onClick에 연결)
    /// </summary>
    public void OnCard1Selected()
    {
        if (isCardSelected) return;

        isCardSelected = true;

        // 타이머 즉시 취소 및 0초 표시
        selectionCts?.Cancel();
        if (timerText != null)
        {
            timerText.text = "0s";
        }

        SelectCard(selectedCard1Id);
    }

    /// <summary>
    /// Card 2 클릭 시 호출 (Button.onClick에 연결)
    /// </summary>
    public void OnCard2Selected()
    {
        if (isCardSelected) return;

        isCardSelected = true;

        // 타이머 즉시 취소 및 0초 표시
        selectionCts?.Cancel();
        if (timerText != null)
        {
            timerText.text = "0s";
        }

        SelectCard(selectedCard2Id);
    }

    /// <summary>
    /// 카드 선택 처리
    /// </summary>
    void SelectCard(int characterId)
    {
        if (characterId <= 0)
        {
            Debug.LogError("[CardSelectionManager] 유효하지 않은 캐릭터 ID입니다!");
            return;
        }

        if (placementManager != null)
        {
            placementManager.SpawnCharacterById(characterId);
            Debug.Log($"[CardSelectionManager] 월드 좌표에 캐릭터 배치 완료: ID {characterId}");
        }
        else
        {
            Debug.LogError("[CardSelectionManager] CharacterPlacementManager를 찾을 수 없습니다!");
        }
    }

    public void ShowCardPanel()
    {
        if (cardPanel != null)
        {
            cardPanel.SetActive(true);
            LoadTwoRandomCharacters();
        }
    }
}
