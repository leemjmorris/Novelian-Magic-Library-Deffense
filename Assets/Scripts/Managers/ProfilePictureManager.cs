using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 프로필 사진/프레임 해금 관리
/// - CharacterOwnershipManager와 유사한 구조
/// - 캐릭터 보유 시 해당 캐릭터 아이콘이 프로필 사진으로 해금됨
/// </summary>
public class ProfilePictureManager : MonoBehaviour
{
    private static ProfilePictureManager instance;
    public static ProfilePictureManager Instance => instance;

    // 해금된 프로필 사진 ID (캐릭터 ID 기반)
    private HashSet<int> unlockedPictures = new HashSet<int>();

    // 해금된 프레임 ID
    private HashSet<int> unlockedFrames = new HashSet<int>();

    // 현재 장착된 사진/프레임 ID
    private int equippedPictureId = -1;
    private int equippedFrameId = -1;

    // 기본 프레임 ID (항상 해금)
    private const int DEFAULT_FRAME_ID = 0;

    // 이벤트
    public event Action<int> OnPictureUnlocked;
    public event Action<int> OnFrameUnlocked;
    public event Action<int> OnPictureEquipped;
    public event Action<int> OnFrameEquipped;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        Init();
    }

    private void Start()
    {
        // CharacterOwnershipManager 이벤트 구독
        if (CharacterOwnershipManager.Instance != null)
        {
            CharacterOwnershipManager.Instance.OnCharacterUnlocked += OnCharacterUnlockedHandler;
        }
    }

    private void OnDestroy()
    {
        if (CharacterOwnershipManager.Instance != null)
        {
            CharacterOwnershipManager.Instance.OnCharacterUnlocked -= OnCharacterUnlockedHandler;
        }
    }

    private void Init()
    {
        // 기본 프레임 해금
        unlockedFrames.Add(DEFAULT_FRAME_ID);

        // CharacterOwnershipManager에서 보유 캐릭터 동기화
        SyncWithCharacterOwnership();

        Debug.Log($"[ProfilePictureManager] 초기화 완료. 해금된 사진: {unlockedPictures.Count}개, 프레임: {unlockedFrames.Count}개");
    }

    /// <summary>
    /// CharacterOwnershipManager와 동기화
    /// 보유 캐릭터 = 해금된 프로필 사진
    /// </summary>
    private void SyncWithCharacterOwnership()
    {
        if (CharacterOwnershipManager.Instance == null) return;

        var ownedCharacters = CharacterOwnershipManager.Instance.GetOwnedCharacters();
        foreach (int characterId in ownedCharacters)
        {
            unlockedPictures.Add(characterId);
        }
    }

    /// <summary>
    /// 캐릭터 해금 시 프로필 사진도 해금
    /// </summary>
    private void OnCharacterUnlockedHandler(int characterId)
    {
        UnlockPicture(characterId);
    }

    /// <summary>
    /// 프로필 사진 해금 여부 확인
    /// </summary>
    public bool IsPictureUnlocked(int pictureId)
    {
        // CharacterOwnershipManager가 있으면 캐릭터 보유 여부로 판단
        if (CharacterOwnershipManager.Instance != null)
        {
            return CharacterOwnershipManager.Instance.IsOwned(pictureId);
        }
        return unlockedPictures.Contains(pictureId);
    }

    /// <summary>
    /// 프레임 해금 여부 확인
    /// </summary>
    public bool IsFrameUnlocked(int frameId)
    {
        return unlockedFrames.Contains(frameId);
    }

    /// <summary>
    /// 프로필 사진 해금
    /// </summary>
    public void UnlockPicture(int pictureId)
    {
        if (unlockedPictures.Contains(pictureId)) return;

        unlockedPictures.Add(pictureId);
        Debug.Log($"[ProfilePictureManager] 프로필 사진 해금: {pictureId}");
        OnPictureUnlocked?.Invoke(pictureId);
    }

    /// <summary>
    /// 프레임 해금
    /// </summary>
    public void UnlockFrame(int frameId)
    {
        if (unlockedFrames.Contains(frameId)) return;

        unlockedFrames.Add(frameId);
        Debug.Log($"[ProfilePictureManager] 프레임 해금: {frameId}");
        OnFrameUnlocked?.Invoke(frameId);
    }

    /// <summary>
    /// 프로필 사진 장착
    /// </summary>
    public void EquipPicture(int pictureId)
    {
        if (!IsPictureUnlocked(pictureId))
        {
            Debug.LogWarning($"[ProfilePictureManager] 해금되지 않은 사진: {pictureId}");
            return;
        }

        equippedPictureId = pictureId;
        Debug.Log($"[ProfilePictureManager] 프로필 사진 장착: {pictureId}");
        OnPictureEquipped?.Invoke(pictureId);

        // Firebase 저장
        SaveToFirebase();
    }

    /// <summary>
    /// 프레임 장착
    /// </summary>
    public void EquipFrame(int frameId)
    {
        if (!IsFrameUnlocked(frameId))
        {
            Debug.LogWarning($"[ProfilePictureManager] 해금되지 않은 프레임: {frameId}");
            return;
        }

        equippedFrameId = frameId;
        Debug.Log($"[ProfilePictureManager] 프레임 장착: {frameId}");
        OnFrameEquipped?.Invoke(frameId);

        // Firebase 저장
        SaveToFirebase();
    }

    /// <summary>
    /// 현재 장착된 사진 해제
    /// </summary>
    public void UnequipPicture()
    {
        equippedPictureId = -1;
        Debug.Log("[ProfilePictureManager] 프로필 사진 해제");
        OnPictureEquipped?.Invoke(-1);
        SaveToFirebase();
    }

    /// <summary>
    /// 현재 장착된 사진 ID 반환
    /// </summary>
    public int GetEquippedPictureId()
    {
        return equippedPictureId;
    }

    /// <summary>
    /// 현재 장착된 프레임 ID 반환
    /// </summary>
    public int GetEquippedFrameId()
    {
        return equippedFrameId;
    }

    /// <summary>
    /// 해금된 모든 사진 ID 반환
    /// </summary>
    public List<int> GetUnlockedPictures()
    {
        // CharacterOwnershipManager 기반으로 반환
        if (CharacterOwnershipManager.Instance != null)
        {
            return CharacterOwnershipManager.Instance.GetOwnedCharacters();
        }
        return new List<int>(unlockedPictures);
    }

    /// <summary>
    /// 해금된 모든 프레임 ID 반환
    /// </summary>
    public List<int> GetUnlockedFrames()
    {
        return new List<int>(unlockedFrames);
    }

    /// <summary>
    /// 모든 캐릭터 ID 반환 (CSV 기반)
    /// </summary>
    public List<int> GetAllCharacterIds()
    {
        var characterIds = new List<int>();

        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            Debug.LogWarning("[ProfilePictureManager] CSVLoader가 초기화되지 않음");
            return characterIds;
        }

        var characterTable = CSVLoader.Instance.GetTable<CharacterData>();
        if (characterTable == null) return characterIds;

        foreach (var characterData in characterTable.GetAll())
        {
            characterIds.Add(characterData.Character_ID);
        }

        return characterIds;
    }

    /// <summary>
    /// Firebase에 저장
    /// </summary>
    private void SaveToFirebase()
    {
        // TODO: Firebase 연동 시 구현
        // FirebaseSaveManager.Instance.SaveProfilePictureAsync(...)
    }

    /// <summary>
    /// Firebase에서 불러오기
    /// </summary>
    public void LoadFromFirebase(int pictureId, int frameId)
    {
        equippedPictureId = pictureId;
        equippedFrameId = frameId;
        Debug.Log($"[ProfilePictureManager] Firebase에서 로드: 사진={pictureId}, 프레임={frameId}");
    }

    #region Debug

    [ContextMenu("해금된 사진 출력")]
    private void PrintUnlockedPictures()
    {
        Debug.Log("=== 해금된 프로필 사진 ===");
        foreach (int pictureId in GetUnlockedPictures())
        {
            Debug.Log($"Picture ID: {pictureId}");
        }
    }

    [ContextMenu("테스트: 모든 사진 해금")]
    private void UnlockAllPictures()
    {
        var allIds = GetAllCharacterIds();
        foreach (int id in allIds)
        {
            UnlockPicture(id);
        }
    }

    #endregion
}
