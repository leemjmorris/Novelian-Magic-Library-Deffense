using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Threading;

public class DeckSlot : MonoBehaviour
{
    [SerializeField] private CanvasGroup plusCanvasGroup;
    [SerializeField] private GameObject selectFrame; // 선택 프레임 (SeletFrame)
    [SerializeField] private Image characterImage;
    [SerializeField] private Image genreIcon;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private CancellationTokenSource blinkCts;
    private int characterId = -1;
    private int slotIndex = -1;

    public int CharacterId => characterId;
    public int SlotIndex => slotIndex;
    public bool IsSet => characterId > 0;

    private void OnEnable()
    {
        // 초기화
        if (selectFrame != null)
            selectFrame.SetActive(false);
    }
    private void OnDisable()
    {
        StopBlinking();
        if (selectFrame != null)
            selectFrame.SetActive(false);
    }

    private void Start()
    {
        // 시작하면 깜빡이기 시작
        ClearSlot();
    }

    /// <summary>
    /// 슬롯 선택 상태 설정 (SeletFrame 활성화/비활성화)
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectFrame != null)
            selectFrame.SetActive(isSelected);
    }

    /// <summary>
    /// 슬롯 인덱스 설정 (TeamSetupPanel에서 호출)
    /// </summary>
    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    /// <summary>
    /// 캐릭터 설정
    /// </summary>
    public void SetCharacter(int characterId)
    {
        this.characterId = characterId;

        // CharacterData 가져오기
        var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
        if (characterData == null)
        {
            Debug.LogWarning($"[DeckSlot] CharacterData not found for ID: {characterId}");
            return;
        }

        // 이름 설정
        var stringData = CSVLoader.Instance.GetData<StringTable>(characterData.Character_Name_ID);
        if (characterNameText != null)
            characterNameText.text = stringData?.Text ?? "Unknown";

        // 레벨 설정
        int level = CharacterEnhancementManager.Instance != null
            ? CharacterEnhancementManager.Instance.GetEnhancementLevel(characterId)
            : 1;
        if (levelText != null)
            levelText.text = $"Lv {level}";

        // 캐릭터 이미지 로드
        LoadCharacterImage();

        // 장르 아이콘 로드
        LoadGenreIcon(characterData.Genre);

        // 페이드 효과 멈추고 알파값 1로 고정
        StopBlinking();
        if (plusCanvasGroup != null)
            plusCanvasGroup.alpha = 1f;

        // 캐릭터 정보 UI 활성화
        if (genreIcon != null)
            genreIcon.gameObject.SetActive(true);
        if (characterNameText != null)
            characterNameText.gameObject.SetActive(true);
        if (levelText != null)
            levelText.gameObject.SetActive(true);

        Debug.Log($"[DeckSlot] 슬롯 {slotIndex}에 캐릭터 '{stringData?.Text}' (ID: {characterId}) 설정");
    }

    /// <summary>
    /// 캐릭터 정보 갱신 (승급 후 레벨 등 업데이트)
    /// </summary>
    public void RefreshCharacterInfo()
    {
        if (!IsSet) return;

        // 레벨 갱신
        int level = CharacterEnhancementManager.Instance != null
            ? CharacterEnhancementManager.Instance.GetEnhancementLevel(characterId)
            : 1;
        if (levelText != null)
            levelText.text = $"Lv {level}";

        Debug.Log($"[DeckSlot] 슬롯 {slotIndex} 캐릭터 정보 갱신 완료 (Lv {level})");
    }

    /// <summary>
    /// 슬롯 초기화 (캐릭터 제거)
    /// </summary>
    public void ClearSlot()
    {
        characterId = -1;

        // 캐릭터 정보 숨기기
        if (genreIcon != null)
            genreIcon.gameObject.SetActive(false);
        if (characterNameText != null)
            characterNameText.gameObject.SetActive(false);
        if (levelText != null)
            levelText.gameObject.SetActive(false);

        // 플러스 이미지 로드 후 페이드 효과 시작
        LoadPlusImage();
    }

    /// <summary>
    /// 캐릭터 이미지 로드
    /// </summary>
    private void LoadCharacterImage()
    {
        Addressables.LoadAssetAsync<Sprite>(AddressableKey.Icon_Character).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && characterImage != null)
            {
                characterImage.sprite = handle.Result;
            }
        };
    }

    /// <summary>
    /// 플러스 이미지 로드 후 페이드 효과 시작
    /// </summary>
    private void LoadPlusImage()
    {
        Addressables.LoadAssetAsync<Sprite>(AddressableKey.Icon_Plus).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && characterImage != null)
            {
                characterImage.sprite = handle.Result;
                StartBlinking();
            }
        };
    }

    /// <summary>
    /// 장르 아이콘 로드
    /// </summary>
    private void LoadGenreIcon(Genre genre)
    {
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

    /// <summary>
    /// 깜빡이기 시작
    /// </summary>
    public void StartBlinking()
    {
        // 캐릭터가 설정되어 있으면 깜빡이지 않음
        if (IsSet) return;

        StopBlinking();
        blinkCts = new CancellationTokenSource();
        BlinkLoopAsync(blinkCts.Token).Forget();
    }

    /// <summary>
    /// 깜빡이기 중지
    /// </summary>
    public void StopBlinking()
    {
        blinkCts?.Cancel();
        blinkCts?.Dispose();
        blinkCts = null;
    }

    /// <summary>
    /// 깜빡이기 루프 (Time.time 기반 동기화)
    /// </summary>
    private async UniTaskVoid BlinkLoopAsync(CancellationToken ct)
    {
        if (plusCanvasGroup == null) return;

        float totalDuration = fadeInDuration + fadeOutDuration;

        while (!ct.IsCancellationRequested)
        {
            // Time.time 기반으로 알파값 계산 (모든 슬롯이 같은 시간 기준)
            float t = Mathf.PingPong(Time.time / totalDuration * 2f, 1f);
            plusCanvasGroup.alpha = t;

            await UniTask.Yield(ct);
        }
    }

    private void OnDestroy()
    {
        StopBlinking();
    }
}