using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using Cysharp.Threading.Tasks;

public class UserInfoPanel : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panel1;
    [SerializeField] private GameObject panel2;

    [Header("Profile Picture")]
    [SerializeField] private Button profileImageButton;
    [SerializeField] private Image profileImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private ProfilePicturePanel profilePicturePanel;

    [Header("Nickname")]
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private LobbyUI lobbyUI;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI stageClearText;
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private TextMeshProUGUI rankText;

    [Header("Title")]
    [SerializeField] private TMP_Dropdown titleDropdown;

    private bool isSubscribed = false;
    private Sprite defaultProfileSprite; // 프리팹의 기본 이미지 저장
    private Sprite defaultFrameSprite; // 프리팹의 기본 프레임 이미지 저장

    private void Awake()
    {
        // 프리팹의 기본 이미지 저장
        if (profileImage != null)
        {
            defaultProfileSprite = profileImage.sprite;
        }
        if (frameImage != null)
        {
            defaultFrameSprite = frameImage.sprite;
        }
    }

    private void Start()
    {
        if (profileImageButton != null)
        {
            profileImageButton.onClick.AddListener(OnProfileImageClicked);
        }

        // 닉네임 입력 완료 이벤트 등록
        if (nicknameInputField != null)
        {
            nicknameInputField.onEndEdit.AddListener(OnNicknameEndEdit);
        }

        // 칭호 드롭다운 이벤트 등록
        if (titleDropdown != null)
        {
            titleDropdown.onValueChanged.AddListener(OnTitleChanged);
        }

        // 프로필 매니저 이벤트 구독 시도
        TrySubscribeToManager();
    }

    private void OnEnable()
    {
        // 활성화될 때마다 구독 시도 및 이미지 갱신
        TrySubscribeToManager();
        RefreshProfileImage();
        RefreshFrameImage();
        RefreshNickname();
        RefreshStatsDisplay();
        RefreshTitleDropdown();
    }

    private void TrySubscribeToManager()
    {
        if (isSubscribed) return;

        if (ProfilePictureManager.Instance != null)
        {
            ProfilePictureManager.Instance.OnPictureEquipped += OnPictureEquipped;
            ProfilePictureManager.Instance.OnFrameEquipped += OnFrameEquipped;
            isSubscribed = true;
            RefreshProfileImage();
            RefreshFrameImage();
        }
    }

    private void OnDestroy()
    {
        if (profileImageButton != null)
        {
            profileImageButton.onClick.RemoveListener(OnProfileImageClicked);
        }

        if (nicknameInputField != null)
        {
            nicknameInputField.onEndEdit.RemoveListener(OnNicknameEndEdit);
        }

        if (titleDropdown != null)
        {
            titleDropdown.onValueChanged.RemoveListener(OnTitleChanged);
        }

        if (ProfilePictureManager.Instance != null && isSubscribed)
        {
            ProfilePictureManager.Instance.OnPictureEquipped -= OnPictureEquipped;
            ProfilePictureManager.Instance.OnFrameEquipped -= OnFrameEquipped;
            isSubscribed = false;
        }
    }

    public void OnCloseButton()
    {
        if (panel2 && panel2.activeSelf)
        {
            panel2.SetActive(false);
        }
        else if (panel1 && panel1.activeSelf)
        {
            panel1.SetActive(false);

            // 닫을 때 로비 화면 닉네임 갱신
            if (lobbyUI != null)
            {
                lobbyUI.RefreshNickname();
            }
        }
    }

    public void ShowPanel2()
    {
        if (panel2)
        {
            panel2.SetActive(true);
        }
    }

    /// <summary>
    /// 프로필 이미지 클릭 시 사진/프레임 선택 패널 표시
    /// </summary>
    private void OnProfileImageClicked()
    {
        if (profilePicturePanel != null)
        {
            profilePicturePanel.ShowPanel();
        }
        else
        {
            GameLog.LogWarning("[UserInfoPanel] ProfilePicturePanel이 연결되지 않았습니다.");
        }
    }

    /// <summary>
    /// 프로필 사진 장착 시 호출
    /// </summary>
    private void OnPictureEquipped(int pictureId)
    {
        RefreshProfileImage();
    }

    /// <summary>
    /// 프레임 장착 시 호출
    /// </summary>
    private void OnFrameEquipped(int frameId)
    {
        RefreshFrameImage();
    }

    /// <summary>
    /// 프로필 이미지 갱신
    /// </summary>
    public void RefreshProfileImage()
    {
        if (profileImage == null) return;

        if (ProfilePictureManager.Instance == null) return;

        int equippedPictureId = ProfilePictureManager.Instance.GetEquippedPictureId();

        if (equippedPictureId == -1)
        {
            // 장착된 사진 없음 - 프리팹의 기본 이미지로 복원
            if (profileImage != null && defaultProfileSprite != null)
            {
                profileImage.sprite = defaultProfileSprite;
            }
            return;
        }

        // 캐릭터 아이콘 로드
        LoadProfileImage(equippedPictureId);
    }

    /// <summary>
    /// 프로필 이미지 로드 (Addressables)
    /// </summary>
    private void LoadProfileImage(int characterId)
    {
        string spriteKey = AddressableKey.Icon_Character;

        // CharacterData에서 Path_ID로 개별 아이콘 키 조회
        if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
            if (characterData != null && characterData.Path_ID > 0)
            {
                var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
                if (pathData != null && !string.IsNullOrEmpty(pathData.Addressable_Key))
                {
                    spriteKey = pathData.Addressable_Key;
                }
            }
        }

        Addressables.LoadAssetAsync<Sprite>(spriteKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && profileImage != null)
            {
                profileImage.sprite = handle.Result;
            }
            else
            {
                GameLog.LogWarning($"[UserInfoPanel] 프로필 이미지 로드 실패: {spriteKey}");
            }
        };
    }

    /// <summary>
    /// 프레임 이미지 갱신
    /// </summary>
    public void RefreshFrameImage()
    {
        if (frameImage == null) return;
        if (ProfilePictureManager.Instance == null) return;

        int equippedFrameId = ProfilePictureManager.Instance.GetEquippedFrameId();

        if (equippedFrameId == -1)
        {
            // 장착된 프레임 없음 - 기본 프레임으로 복원
            if (frameImage != null && defaultFrameSprite != null)
            {
                frameImage.sprite = defaultFrameSprite;
            }
            return;
        }

        LoadFrameImage(equippedFrameId);
    }

    /// <summary>
    /// 프레임 이미지 로드 (Addressables)
    /// </summary>
    private void LoadFrameImage(int frameId)
    {
        string spriteKey = AddressableKey.GetAvatarFrameKey(frameId);

        Addressables.LoadAssetAsync<Sprite>(spriteKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && frameImage != null)
            {
                frameImage.sprite = handle.Result;
            }
            else
            {
                GameLog.LogWarning($"[UserInfoPanel] 프레임 이미지 로드 실패: {spriteKey}");
            }
        };
    }

    #region Nickname

    /// <summary>
    /// 저장된 닉네임을 InputField에 표시
    /// </summary>
    private void RefreshNickname()
    {
        if (nicknameInputField == null) return;

        if (FirebaseSaveManager.Instance != null && FirebaseSaveManager.Instance.CachedData != null)
        {
            string nickname = FirebaseSaveManager.Instance.CachedData.nickname;
            nicknameInputField.text = string.IsNullOrEmpty(nickname) ? "" : nickname;
        }
    }

    /// <summary>
    /// 닉네임 입력 완료 시 Firebase에 저장
    /// </summary>
    private void OnNicknameEndEdit(string newNickname)
    {
        if (string.IsNullOrEmpty(newNickname)) return;

        SaveNicknameAsync(newNickname).Forget();
    }

    /// <summary>
    /// 닉네임을 Firebase에 저장
    /// </summary>
    private async UniTaskVoid SaveNicknameAsync(string nickname)
    {
        if (FirebaseSaveManager.Instance == null || FirebaseManager.Instance == null)
        {
            GameLog.LogWarning("[UserInfoPanel] Firebase 매니저가 초기화되지 않았습니다.");
            return;
        }

        string userId = FirebaseManager.Instance.CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            GameLog.LogWarning("[UserInfoPanel] 로그인된 유저가 없습니다.");
            return;
        }

        await FirebaseSaveManager.Instance.SaveNicknameAsync(userId, nickname);
        GameLog.Log($"[UserInfoPanel] 닉네임 저장 완료: {nickname}");
    }

    #endregion

    #region Stats Display

    /// <summary>
    /// 유저 통계 정보 표시 (레벨, 스테이지, 몬스터 수, 랭킹)
    /// </summary>
    private void RefreshStatsDisplay()
    {
        var progression = FirebaseSaveManager.Instance?.CachedData?.progression;
        if (progression == null)
        {
            GameLog.LogWarning("[UserInfoPanel] Progression 데이터 없음");
            return;
        }

        // 레벨 표시
        if (levelText != null)
        {
            levelText.text = $"Lv{progression.playerLevel}";
        }

        // 스테이지 클리어 표시
        if (stageClearText != null)
        {
            stageClearText.text = $"스테이지 {progression.highestClearedStage}";
        }

        // 처치 몬스터 수 표시
        if (killCountText != null)
        {
            killCountText.text = $"{progression.totalKilledMonsters}마리";
        }

        // 랭킹 비동기 조회
        RefreshRankAsync().Forget();
    }

    /// <summary>
    /// 랭킹 비동기 조회 및 표시
    /// </summary>
    private async UniTaskVoid RefreshRankAsync()
    {
        if (rankText == null) return;

        // 로딩 중 표시
        rankText.text = "#...";

        var progression = FirebaseSaveManager.Instance?.CachedData?.progression;
        if (progression == null)
        {
            rankText.text = "#-";
            return;
        }

        if (FirebaseSaveManager.Instance == null)
        {
            rankText.text = "#-";
            return;
        }

        int rank = await FirebaseSaveManager.Instance.GetMyRankAsync(progression.totalKilledMonsters);

        // 패널이 비활성화되었을 수 있으므로 체크
        if (rankText != null && gameObject.activeInHierarchy)
        {
            rankText.text = $"#{rank}";
        }
    }

    #endregion

    #region Title

    /// <summary>
    /// 저장된 칭호 인덱스로 Dropdown 선택 갱신
    /// </summary>
    private void RefreshTitleDropdown()
    {
        if (titleDropdown == null) return;

        int savedTitleIndex = 0;
        if (FirebaseSaveManager.Instance != null && FirebaseSaveManager.Instance.CachedData?.profile != null)
        {
            savedTitleIndex = FirebaseSaveManager.Instance.CachedData.profile.equippedTitleIndex;
        }

        // 유효한 인덱스인지 확인
        if (savedTitleIndex >= 0 && savedTitleIndex < titleDropdown.options.Count)
        {
            titleDropdown.SetValueWithoutNotify(savedTitleIndex);
        }
    }

    /// <summary>
    /// 칭호 선택 변경 시 Firebase에 저장
    /// </summary>
    private void OnTitleChanged(int index)
    {
        SaveTitleAsync(index).Forget();
    }

    /// <summary>
    /// 칭호를 Firebase에 저장
    /// </summary>
    private async UniTaskVoid SaveTitleAsync(int titleIndex)
    {
        if (FirebaseSaveManager.Instance == null || FirebaseManager.Instance == null)
        {
            GameLog.LogWarning("[UserInfoPanel] Firebase 매니저가 초기화되지 않았습니다.");
            return;
        }

        string userId = FirebaseManager.Instance.CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            GameLog.LogWarning("[UserInfoPanel] 로그인된 유저가 없습니다.");
            return;
        }

        await FirebaseSaveManager.Instance.SaveTitleAsync(userId, titleIndex);
        GameLog.Log($"[UserInfoPanel] 칭호 저장 완료: 인덱스 {titleIndex}");
    }

    #endregion
}