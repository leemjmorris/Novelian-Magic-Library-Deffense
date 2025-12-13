using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 프로필 사진/프레임 개별 슬롯
/// CharacterSlot 프리팹 구조에 맞춤:
/// - Select: 선택됨 표시
/// - BackGround (SlotBackGround): 캐릭터별 이미지
/// - ImagePanel > SkinImage: 캐릭터 스킨 이미지
/// - RockPanel (LockOverlay): 비활성화(잠금) 패널
/// - Lock: 잠금 아이콘
/// - Check: 장착 중인 캐릭터 표시
/// </summary>
public class ProfilePictureSlot : MonoBehaviour
{
    [Header("UI References - CharacterSlot 프리팹 구조")]
    [SerializeField] private GameObject selectIndicator;      // Select - 선택됨 표시
    [SerializeField] private Image slotBackgroundImage;       // BackGround/BackGround2 - 캐릭터별 이미지
    [SerializeField] private Image skinImage;                 // ImagePanel > SkinImage - 캐릭터 스킨
    [SerializeField] private GameObject rockPanel;            // RockPanel - 비활성화 패널 (어두운 오버레이)
    [SerializeField] private GameObject lockIcon;             // Lock - 잠금 아이콘
    [SerializeField] private GameObject checkIcon;            // Check - 장착 중 표시
    [SerializeField] private Button slotButton;               // Select에 있는 Button

    [Header("Lock Settings")]
    [SerializeField] private float lockedAlpha = 0.5f;        // 잠금 시 SkinImage 투명도

    private int itemId;
    private bool isLocked = true;
    private bool isEquipped = false;
    private bool isSelected = false;
    private ProfilePicturePanel parentPanel;

    public int ItemId => itemId;
    public bool IsLocked => isLocked;
    public bool IsEquipped => isEquipped;
    public bool IsSelected => isSelected;

    private void Awake()
    {
        if (slotButton == null)
            slotButton = GetComponentInChildren<Button>();
    }

    private bool isButtonSetup = false;

    private void Start()
    {
        SetupButtonListener();

        // 초기 상태: 선택 해제
        SetSelected(false);
    }

    /// <summary>
    /// 버튼 리스너 설정 (중복 방지)
    /// </summary>
    private void SetupButtonListener()
    {
        if (isButtonSetup) return;

        if (slotButton == null)
            slotButton = GetComponentInChildren<Button>();

        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
            isButtonSetup = true;
        }
    }

    private void OnDestroy()
    {
        if (slotButton != null && isButtonSetup)
        {
            slotButton.onClick.RemoveListener(OnSlotClicked);
        }
    }

    /// <summary>
    /// 슬롯 초기화 (사진용)
    /// </summary>
    public void InitPictureSlot(int characterId, ProfilePicturePanel panel)
    {
        itemId = characterId;
        parentPanel = panel;

        // 버튼 리스너 등록 (Start보다 먼저 호출될 수 있으므로)
        SetupButtonListener();

        // 해금 상태 확인
        bool isUnlocked = ProfilePictureManager.Instance != null &&
                          ProfilePictureManager.Instance.IsPictureUnlocked(characterId);
        SetLocked(!isUnlocked);

        // 장착 상태 확인
        bool equipped = ProfilePictureManager.Instance != null &&
                        ProfilePictureManager.Instance.GetEquippedPictureId() == characterId;
        SetEquipped(equipped);

        // 선택 초기화
        SetSelected(false);

        // 캐릭터 아이콘 로드
        LoadCharacterIcon(characterId);

        Debug.Log($"[ProfilePictureSlot] InitPictureSlot: ID={characterId}, Locked={isLocked}, Button={slotButton != null}");
    }

    /// <summary>
    /// 슬롯 초기화 (프레임용)
    /// </summary>
    public void InitFrameSlot(int frameId, ProfilePicturePanel panel)
    {
        itemId = frameId;
        parentPanel = panel;

        // 해금 상태 확인
        bool isUnlocked = ProfilePictureManager.Instance != null &&
                          ProfilePictureManager.Instance.IsFrameUnlocked(frameId);
        SetLocked(!isUnlocked);

        // 장착 상태 확인
        bool equipped = ProfilePictureManager.Instance != null &&
                        ProfilePictureManager.Instance.GetEquippedFrameId() == frameId;
        SetEquipped(equipped);

        // 선택 초기화
        SetSelected(false);

        // 프레임 이미지 로드
        LoadFrameImage(frameId);
    }

    /// <summary>
    /// 잠금 상태 설정
    /// - RockPanel 활성화 (어두운 오버레이)
    /// - Lock 아이콘 표시
    /// - SkinImage 투명도 조정
    /// - 버튼 비활성화
    /// </summary>
    public void SetLocked(bool locked)
    {
        isLocked = locked;

        // RockPanel (잠금 오버레이) 표시
        if (rockPanel != null)
            rockPanel.SetActive(locked);

        // Lock 아이콘 표시
        if (lockIcon != null)
            lockIcon.SetActive(locked);

        // SkinImage 투명도 조정
        if (skinImage != null)
        {
            Color color = skinImage.color;
            color.a = locked ? lockedAlpha : 1f;
            skinImage.color = color;
        }

        // 버튼 상호작용 비활성화
        if (slotButton != null)
            slotButton.interactable = !locked;
    }

    /// <summary>
    /// 선택 상태 설정
    /// - Select 오브젝트 활성화/비활성화
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectIndicator != null)
            selectIndicator.SetActive(selected);
    }

    /// <summary>
    /// 장착 상태 설정
    /// - Check 아이콘 표시
    /// </summary>
    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;

        if (checkIcon != null)
            checkIcon.SetActive(equipped);
    }

    /// <summary>
    /// 슬롯 클릭 시
    /// </summary>
    private void OnSlotClicked()
    {
        if (isLocked) return;

        parentPanel?.OnSlotSelected(this);
    }

    /// <summary>
    /// 캐릭터 아이콘 로드 (Addressables)
    /// SkinImage에 캐릭터 스프라이트 적용
    /// </summary>
    private void LoadCharacterIcon(int characterId)
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
            if (handle.Status == AsyncOperationStatus.Succeeded && skinImage != null)
            {
                skinImage.sprite = handle.Result;
            }
            else
            {
                Debug.LogWarning($"[ProfilePictureSlot] 캐릭터 아이콘 로드 실패: {spriteKey}");
            }
        };
    }

    /// <summary>
    /// 프레임 이미지 로드
    /// </summary>
    private void LoadFrameImage(int frameId)
    {
        // TODO: 프레임 에셋 로드 구현
        // 현재는 기본 이미지 사용
    }

    /// <summary>
    /// 슬롯 UI 갱신
    /// </summary>
    public void RefreshUI()
    {
        // 해금 상태 갱신
        bool isUnlocked = ProfilePictureManager.Instance != null &&
                          ProfilePictureManager.Instance.IsPictureUnlocked(itemId);
        SetLocked(!isUnlocked);

        // 장착 상태 갱신
        bool equipped = ProfilePictureManager.Instance != null &&
                        ProfilePictureManager.Instance.GetEquippedPictureId() == itemId;
        SetEquipped(equipped);
    }

    /// <summary>
    /// SkinImage 반환 (프리뷰용)
    /// </summary>
    public Image GetSkinImage()
    {
        return skinImage;
    }
}
