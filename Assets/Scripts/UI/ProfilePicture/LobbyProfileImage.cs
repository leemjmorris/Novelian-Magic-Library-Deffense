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

    private bool isSubscribed = false;

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
            // 장착된 사진 없음 - 기본 이미지 유지
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

        // TODO: 프레임 이미지 로드 구현
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
