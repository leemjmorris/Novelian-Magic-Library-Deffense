using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 로비 화면의 프로필 이미지 표시
/// ProfilePictureManager의 장착 이벤트를 구독하여 실시간 갱신
/// </summary>
public class LobbyProfileImage : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image profileImage;
    [SerializeField] private Image frameImage;

    [Header("RectTransform References")]
    [SerializeField] private RectTransform profilePanelRect;
    [SerializeField] private RectTransform profileMaskRect;
    [SerializeField] private RectTransform profileMaskImageRect; // ProfileMask 하위의 ProfileImage
    [SerializeField] private RectTransform profileImageRect; // ProfileImage (1)

    [Header("Frame Equipped Size Settings - ProfilePanel")]
    [SerializeField] private Vector2 frameEquippedPanelSize = new Vector2(240f, 230f);

    [Header("Frame Equipped Size Settings - Mask")]
    [SerializeField] private Vector2 frameEquippedMaskSize = new Vector2(130f, 130f);
    [SerializeField] private float frameEquippedMaskPosY = 0f;

    [Header("Frame Equipped Size Settings - MaskImage (Mask 하위 ProfileImage)")]
    [SerializeField] private Vector2 frameEquippedMaskImageSize = new Vector2(130f, 130f);
    [SerializeField] private float frameEquippedMaskImagePosY = 0f;

    [Header("Frame Equipped Size Settings - ProfileImage (1)")]
    [SerializeField] private Vector2 frameEquippedImageSize = new Vector2(228.8388f, 228.8388f);
    [SerializeField] private float frameEquippedImagePosY = -44f;

    private bool isSubscribed = false;
    private Sprite defaultProfileSprite; // 프리팹의 기본 프로필 이미지 저장
    private Sprite defaultFrameSprite; // 프리팹의 기본 프레임 이미지 저장
    private Vector2 defaultPanelSize; // 기본 패널 크기 저장
    private Vector2 defaultMaskSize; // 기본 마스크 크기 저장
    private float defaultMaskPosY; // 기본 마스크 Y 위치 저장
    private Vector2 defaultMaskImageSize; // 기본 마스크 이미지 크기 저장
    private float defaultMaskImagePosY; // 기본 마스크 이미지 Y 위치 저장
    private Vector2 defaultImageSize; // 기본 이미지 크기 저장
    private float defaultImagePosY; // 기본 이미지 Y 위치 저장

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

        // 기본 RectTransform 크기 저장
        if (profilePanelRect != null)
        {
            defaultPanelSize = profilePanelRect.sizeDelta;
        }
        if (profileMaskRect != null)
        {
            defaultMaskSize = profileMaskRect.sizeDelta;
            defaultMaskPosY = profileMaskRect.anchoredPosition.y;
        }
        if (profileMaskImageRect != null)
        {
            defaultMaskImageSize = profileMaskImageRect.sizeDelta;
            defaultMaskImagePosY = profileMaskImageRect.anchoredPosition.y;
        }
        if (profileImageRect != null)
        {
            defaultImageSize = profileImageRect.sizeDelta;
            defaultImagePosY = profileImageRect.anchoredPosition.y;
        }
    }

    private void Start()
    {
        TrySubscribeToManager();
    }

    private void OnEnable()
    {
        TrySubscribeToManager();
        RefreshProfileImage();
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
        }
    }

    private void OnDestroy()
    {
        if (ProfilePictureManager.Instance != null && isSubscribed)
        {
            ProfilePictureManager.Instance.OnPictureEquipped -= OnPictureEquipped;
            ProfilePictureManager.Instance.OnFrameEquipped -= OnFrameEquipped;
            isSubscribed = false;
        }
    }

    private void OnPictureEquipped(int pictureId)
    {
        RefreshProfileImage();
    }

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
                Debug.Log("[LobbyProfileImage] 프로필 이미지를 기본 이미지로 복원");
            }
            return;
        }

        LoadProfileImage(equippedPictureId);
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
                Debug.Log("[LobbyProfileImage] 프레임 이미지를 기본 이미지로 복원");
            }

            // RectTransform 기본 크기로 복원
            RestoreDefaultRectSize();
            return;
        }

        LoadFrameImage(equippedFrameId);

        // RectTransform 프레임 장착 크기로 변경
        ApplyFrameEquippedRectSize();
    }

    /// <summary>
    /// 프레임 장착 시 RectTransform 크기 적용
    /// </summary>
    private void ApplyFrameEquippedRectSize()
    {
        if (profilePanelRect != null)
        {
            profilePanelRect.sizeDelta = frameEquippedPanelSize;
            Debug.Log($"[LobbyProfileImage] ProfilePanel 크기 변경: {frameEquippedPanelSize}");
        }

        if (profileMaskRect != null)
        {
            profileMaskRect.sizeDelta = frameEquippedMaskSize;
            var maskPos = profileMaskRect.anchoredPosition;
            maskPos.y = frameEquippedMaskPosY;
            profileMaskRect.anchoredPosition = maskPos;
            Debug.Log($"[LobbyProfileImage] ProfileMask 크기 변경: {frameEquippedMaskSize}, PosY: {frameEquippedMaskPosY}");
        }

        if (profileMaskImageRect != null)
        {
            profileMaskImageRect.sizeDelta = frameEquippedMaskImageSize;
            var maskImgPos = profileMaskImageRect.anchoredPosition;
            maskImgPos.y = frameEquippedMaskImagePosY;
            profileMaskImageRect.anchoredPosition = maskImgPos;
            Debug.Log($"[LobbyProfileImage] ProfileMaskImage 크기 변경: {frameEquippedMaskImageSize}, PosY: {frameEquippedMaskImagePosY}");
        }

        if (profileImageRect != null)
        {
            profileImageRect.sizeDelta = frameEquippedImageSize;
            var imgPos = profileImageRect.anchoredPosition;
            imgPos.y = frameEquippedImagePosY;
            profileImageRect.anchoredPosition = imgPos;
            Debug.Log($"[LobbyProfileImage] ProfileImage 크기 변경: {frameEquippedImageSize}, PosY: {frameEquippedImagePosY}");
        }
    }

    /// <summary>
    /// RectTransform 기본 크기로 복원
    /// </summary>
    private void RestoreDefaultRectSize()
    {
        if (profilePanelRect != null)
        {
            profilePanelRect.sizeDelta = defaultPanelSize;
            Debug.Log($"[LobbyProfileImage] ProfilePanel 크기 복원: {defaultPanelSize}");
        }

        if (profileMaskRect != null)
        {
            profileMaskRect.sizeDelta = defaultMaskSize;
            var maskPos = profileMaskRect.anchoredPosition;
            maskPos.y = defaultMaskPosY;
            profileMaskRect.anchoredPosition = maskPos;
            Debug.Log($"[LobbyProfileImage] ProfileMask 크기 복원: {defaultMaskSize}, PosY: {defaultMaskPosY}");
        }

        if (profileMaskImageRect != null)
        {
            profileMaskImageRect.sizeDelta = defaultMaskImageSize;
            var maskImgPos = profileMaskImageRect.anchoredPosition;
            maskImgPos.y = defaultMaskImagePosY;
            profileMaskImageRect.anchoredPosition = maskImgPos;
            Debug.Log($"[LobbyProfileImage] ProfileMaskImage 크기 복원: {defaultMaskImageSize}, PosY: {defaultMaskImagePosY}");
        }

        if (profileImageRect != null)
        {
            profileImageRect.sizeDelta = defaultImageSize;
            var imgPos = profileImageRect.anchoredPosition;
            imgPos.y = defaultImagePosY;
            profileImageRect.anchoredPosition = imgPos;
            Debug.Log($"[LobbyProfileImage] ProfileImage 크기 복원: {defaultImageSize}, PosY: {defaultImagePosY}");
        }
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
                Debug.Log($"[LobbyProfileImage] 프레임 이미지 변경: {spriteKey}");
            }
            else
            {
                Debug.LogWarning($"[LobbyProfileImage] 프레임 이미지 로드 실패: {spriteKey}");
            }
        };
    }

    /// <summary>
    /// 프로필 이미지 로드 (Addressables)
    /// </summary>
    private void LoadProfileImage(int characterId)
    {
        string spriteKey = AddressableKey.Icon_Character;

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
                Debug.Log($"[LobbyProfileImage] 프로필 이미지 변경: {spriteKey}");
            }
            else
            {
                Debug.LogWarning($"[LobbyProfileImage] 프로필 이미지 로드 실패: {spriteKey}");
            }
        };
    }
}
