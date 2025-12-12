using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

/// <summary>
/// 프로필 사진/프레임 선택 패널
/// 탭 전환(사진/프레임), 슬롯 생성 및 선택 관리
/// </summary>
public class ProfilePicturePanel : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Tab Buttons")]
    [SerializeField] private Button pictureTabButton;
    [SerializeField] private Button frameTabButton;

    [Header("Content")]
    [SerializeField] private Transform pictureContent;
    [SerializeField] private Transform frameContent;
    [SerializeField] private GameObject pictureContentPanel;
    [SerializeField] private GameObject frameContentPanel;

    [Header("Prefabs")]
    [SerializeField] private GameObject pictureSlotPrefab;
    [SerializeField] private GameObject frameSlotPrefab;

    [Header("Action Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;

    [Header("Selected Display")]
    [SerializeField] private Image selectedPreviewImage;

    private List<ProfilePictureSlot> pictureSlots = new List<ProfilePictureSlot>();
    private List<ProfilePictureSlot> frameSlots = new List<ProfilePictureSlot>();
    private ProfilePictureSlot selectedSlot;
    private bool isPictureTab = true;

    private void Awake()
    {
        SetupButtons();
    }

    private async void OnEnable()
    {
        // CSV 로딩 대기
        await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);

        // 슬롯 생성
        GeneratePictureSlots();
        GenerateFrameSlots();

        // 기본 탭 선택
        OnPictureTabClicked();

        // 선택 초기화
        selectedSlot = null;
        UpdateActionButtons();
    }

    private void OnDisable()
    {
        // 슬롯 정리
        ClearSlots();
    }

    private void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);

        if (pictureTabButton != null)
            pictureTabButton.onClick.AddListener(OnPictureTabClicked);

        if (frameTabButton != null)
            frameTabButton.onClick.AddListener(OnFrameTabClicked);

        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipButtonClicked);

        if (unequipButton != null)
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);

        if (pictureTabButton != null)
            pictureTabButton.onClick.RemoveListener(OnPictureTabClicked);

        if (frameTabButton != null)
            frameTabButton.onClick.RemoveListener(OnFrameTabClicked);

        if (equipButton != null)
            equipButton.onClick.RemoveListener(OnEquipButtonClicked);

        if (unequipButton != null)
            unequipButton.onClick.RemoveListener(OnUnequipButtonClicked);
    }

    /// <summary>
    /// 사진 탭 클릭
    /// </summary>
    private void OnPictureTabClicked()
    {
        isPictureTab = true;

        // 탭 버튼 상태
        if (pictureTabButton != null)
            pictureTabButton.interactable = false;
        if (frameTabButton != null)
            frameTabButton.interactable = true;

        // 콘텐츠 패널 전환
        if (pictureContentPanel != null)
            pictureContentPanel.SetActive(true);
        if (frameContentPanel != null)
            frameContentPanel.SetActive(false);

        // 선택 초기화
        selectedSlot = null;
        UpdateActionButtons();
    }

    /// <summary>
    /// 프레임 탭 클릭
    /// </summary>
    private void OnFrameTabClicked()
    {
        isPictureTab = false;

        // 탭 버튼 상태
        if (pictureTabButton != null)
            pictureTabButton.interactable = true;
        if (frameTabButton != null)
            frameTabButton.interactable = false;

        // 콘텐츠 패널 전환
        if (pictureContentPanel != null)
            pictureContentPanel.SetActive(false);
        if (frameContentPanel != null)
            frameContentPanel.SetActive(true);

        // 선택 초기화
        selectedSlot = null;
        UpdateActionButtons();
    }

    /// <summary>
    /// 사진 슬롯 생성
    /// </summary>
    private void GeneratePictureSlots()
    {
        if (pictureSlotPrefab == null || pictureContent == null) return;

        // 기존 슬롯 제거
        foreach (var slot in pictureSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        pictureSlots.Clear();

        // 모든 캐릭터 데이터 가져오기
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit) return;

        var characterTable = CSVLoader.Instance.GetTable<CharacterData>();
        if (characterTable == null) return;

        // 해금된 캐릭터가 먼저 오도록 정렬
        var sortedCharacters = new List<CharacterData>(characterTable.GetAll());
        sortedCharacters.Sort((a, b) =>
        {
            bool aUnlocked = ProfilePictureManager.Instance?.IsPictureUnlocked(a.Character_ID) ?? false;
            bool bUnlocked = ProfilePictureManager.Instance?.IsPictureUnlocked(b.Character_ID) ?? false;

            if (aUnlocked != bUnlocked)
                return bUnlocked.CompareTo(aUnlocked); // 해금된 것이 먼저

            return a.Character_ID.CompareTo(b.Character_ID);
        });

        // 슬롯 생성
        foreach (var characterData in sortedCharacters)
        {
            var slotObj = Instantiate(pictureSlotPrefab, pictureContent);
            var slot = slotObj.GetComponent<ProfilePictureSlot>();
            if (slot != null)
            {
                slot.InitPictureSlot(characterData.Character_ID, this);
                pictureSlots.Add(slot);
            }
        }

        Debug.Log($"[ProfilePicturePanel] 사진 슬롯 {pictureSlots.Count}개 생성");
    }

    /// <summary>
    /// 프레임 슬롯 생성
    /// </summary>
    private void GenerateFrameSlots()
    {
        if (frameSlotPrefab == null || frameContent == null) return;

        // 기존 슬롯 제거
        foreach (var slot in frameSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        frameSlots.Clear();

        // TODO: 프레임 데이터 테이블에서 가져오기
        // 현재는 기본 프레임만 생성
        var slotObj = Instantiate(frameSlotPrefab, frameContent);
        var frameSlot = slotObj.GetComponent<ProfilePictureSlot>();
        if (frameSlot != null)
        {
            frameSlot.InitFrameSlot(0, this); // 기본 프레임 ID = 0
            frameSlots.Add(frameSlot);
        }

        Debug.Log($"[ProfilePicturePanel] 프레임 슬롯 {frameSlots.Count}개 생성");
    }

    /// <summary>
    /// 슬롯 선택 시 호출
    /// </summary>
    public void OnSlotSelected(ProfilePictureSlot slot)
    {
        if (slot == null || slot.IsLocked) return;

        // 이전 선택 해제
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        // 새 슬롯 선택
        selectedSlot = slot;
        selectedSlot.SetSelected(true);

        // 프리뷰 이미지 업데이트
        if (selectedPreviewImage != null)
        {
            var skinImage = slot.GetSkinImage();
            if (skinImage != null)
            {
                selectedPreviewImage.sprite = skinImage.sprite;
            }
        }

        UpdateActionButtons();
        Debug.Log($"[ProfilePicturePanel] 슬롯 선택: ID={slot.ItemId}");
    }

    /// <summary>
    /// 장착 버튼 클릭
    /// - 선택된 슬롯에 체크 아이콘 표시
    /// - 선택 오버레이 해제
    /// </summary>
    private void OnEquipButtonClicked()
    {
        if (selectedSlot == null || selectedSlot.IsLocked) return;

        if (ProfilePictureManager.Instance == null) return;

        if (isPictureTab)
        {
            ProfilePictureManager.Instance.EquipPicture(selectedSlot.ItemId);
        }
        else
        {
            ProfilePictureManager.Instance.EquipFrame(selectedSlot.ItemId);
        }

        // 선택 상태 해제 (체크로 전환됨)
        selectedSlot.SetSelected(false);

        // 모든 슬롯의 장착 상태 갱신
        RefreshAllSlots();

        // 선택 초기화
        selectedSlot = null;
        UpdateActionButtons();
    }

    /// <summary>
    /// 장착 해제 버튼 클릭
    /// </summary>
    private void OnUnequipButtonClicked()
    {
        if (ProfilePictureManager.Instance == null) return;

        if (isPictureTab)
        {
            ProfilePictureManager.Instance.UnequipPicture();
        }
        // 프레임은 기본 프레임으로 변경 (완전 해제 없음)

        RefreshAllSlots();
        UpdateActionButtons();
    }

    /// <summary>
    /// 닫기 버튼 클릭
    /// </summary>
    private void OnCloseButtonClicked()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    /// <summary>
    /// 패널 표시
    /// </summary>
    public void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    /// <summary>
    /// 패널 숨기기
    /// </summary>
    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    /// <summary>
    /// 액션 버튼 상태 업데이트
    /// </summary>
    private void UpdateActionButtons()
    {
        bool hasSelection = selectedSlot != null && !selectedSlot.IsLocked;
        bool isCurrentlyEquipped = selectedSlot != null && selectedSlot.IsEquipped;

        if (equipButton != null)
        {
            equipButton.interactable = hasSelection && !isCurrentlyEquipped;
        }

        if (unequipButton != null)
        {
            // 현재 장착된 것이 있는지 확인
            bool hasEquipped = ProfilePictureManager.Instance != null &&
                               ProfilePictureManager.Instance.GetEquippedPictureId() != -1;
            unequipButton.interactable = hasEquipped && isPictureTab;
        }
    }

    /// <summary>
    /// 모든 슬롯 갱신
    /// </summary>
    private void RefreshAllSlots()
    {
        int equippedPictureId = ProfilePictureManager.Instance?.GetEquippedPictureId() ?? -1;
        int equippedFrameId = ProfilePictureManager.Instance?.GetEquippedFrameId() ?? -1;

        foreach (var slot in pictureSlots)
        {
            if (slot != null)
            {
                slot.SetEquipped(slot.ItemId == equippedPictureId);
            }
        }

        foreach (var slot in frameSlots)
        {
            if (slot != null)
            {
                slot.SetEquipped(slot.ItemId == equippedFrameId);
            }
        }
    }

    /// <summary>
    /// 슬롯 정리
    /// </summary>
    private void ClearSlots()
    {
        foreach (var slot in pictureSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        pictureSlots.Clear();

        foreach (var slot in frameSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        frameSlots.Clear();
    }
}
