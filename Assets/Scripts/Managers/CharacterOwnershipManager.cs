using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Data;
using Cysharp.Threading.Tasks;

/// <summary>
/// 캐릭터 보유 시스템 관리
/// - 메모리(HashSet)에 보유 캐릭터 ID 저장 (재시작 시 초기화)
/// - 초기 보유 캐릭터: 022007, 024013, 025017
/// - 가챠 시스템에서 UnlockCharacter() 호출하여 해금
/// </summary>
public class CharacterOwnershipManager : MonoBehaviour
{
    private static CharacterOwnershipManager instance;
    public static CharacterOwnershipManager Instance => instance;

    [Header("테스트 설정")]
    [SerializeField] private bool grantGenreTestCharacters = false;

    // 보유 캐릭터 ID 목록
    private HashSet<int> ownedCharacters = new HashSet<int>();

    // 초기 보유 캐릭터 ID
    private static readonly int[] INITIAL_CHARACTERS = { 22007, 24013, 25017 };

    // 테스트용 공포(Genre 1) 캐릭터 ID (파티 시너지 테스트용)
    private static readonly int[] HORROR_TEST_CHARACTERS = { 21001, 21002, 21003, 21004 };

    // 캐릭터 해금 이벤트 (UI 갱신용)
    public event Action<int> OnCharacterUnlocked;

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

    private void Init()
    {
        // Awake에서는 빈 HashSet 상태 유지
        // 실제 캐릭터 추가는 Firebase 로드 후 EnsureInitialCharacters()에서 처리
        Debug.Log("[CharacterOwnershipManager] 초기화 완료 (Firebase 로드 대기 중)");
    }

    /// <summary>
    /// 초기 캐릭터 및 테스트 캐릭터 보장
    /// Firebase 로드 후 항상 호출되어 필수 캐릭터 존재 보장
    /// </summary>
    private void EnsureInitialCharacters()
    {
        // 1. 초기 캐릭터 보장 (22007, 24013, 25017)
        foreach (int characterId in INITIAL_CHARACTERS)
        {
            ownedCharacters.Add(characterId); // HashSet이므로 중복 자동 무시
        }

        // 2. 테스트 캐릭터 추가 (Inspector 설정에 따라)
        if (grantGenreTestCharacters)
        {
            foreach (int characterId in HORROR_TEST_CHARACTERS)
            {
                ownedCharacters.Add(characterId);
            }
            Debug.Log("[CharacterOwnershipManager] 테스트용 공포 캐릭터 4명 지급 (파티 시너지 테스트용)");
        }
    }

    /// <summary>
    /// Firebase 로그인 실패/오프라인 시 초기화
    /// BootScene에서 Firebase 사용 불가 시 호출
    /// </summary>
    public void InitializeOffline()
    {
        ownedCharacters.Clear();
        EnsureInitialCharacters();
        Debug.Log($"[CharacterOwnershipManager] 오프라인 초기화 완료: {ownedCharacters.Count}개");
    }

    /// <summary>
    /// 캐릭터 보유 여부 확인
    /// </summary>
    public bool IsOwned(int characterId)
    {
        return ownedCharacters.Contains(characterId);
    }

    /// <summary>
    /// 캐릭터 해금 (가챠 시스템에서 호출)
    /// </summary>
    public void UnlockCharacter(int characterId)
    {
        if (ownedCharacters.Contains(characterId))
        {
            Debug.LogWarning($"[CharacterOwnershipManager] 이미 보유한 캐릭터입니다: {characterId}");
            return;
        }

        ownedCharacters.Add(characterId);

        // 캐릭터 이름 가져오기
        string characterName = GetCharacterName(characterId);
        Debug.Log($"[CharacterOwnershipManager] 캐릭터 해금! {characterName} (ID: {characterId})");

        // 이벤트 발생 (UI 갱신용)
        OnCharacterUnlocked?.Invoke(characterId);

        // Firebase에 저장
        SaveToFirebase();
    }

    /// <summary>
    /// 현재 보유 캐릭터 상태를 Firebase에 저장
    /// </summary>
    private void SaveToFirebase()
    {
        if (FirebaseSaveManager.Instance == null || !FirebaseSaveManager.Instance.IsInitialized)
            return;

        if (FirebaseManager.Instance == null || string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserId))
            return;

        FirebaseSaveManager.Instance.SaveOwnedCharactersAsync(
            FirebaseManager.Instance.CurrentUserId,
            ownedCharacters
        ).Forget();
    }

    /// <summary>
    /// 보유 캐릭터 목록 반환
    /// </summary>
    public List<int> GetOwnedCharacters()
    {
        return new List<int>(ownedCharacters);
    }

    /// <summary>
    /// 보유 캐릭터 수 반환
    /// </summary>
    public int GetOwnedCount()
    {
        return ownedCharacters.Count;
    }

    /// <summary>
    /// Firebase 데이터로 보유 캐릭터 설정 (BootScene에서 호출)
    /// Firebase 데이터 로드 후 초기 캐릭터 및 테스트 캐릭터 보장
    /// </summary>
    public void SetOwnedCharactersFromFirebase(Dictionary<string, bool> owned)
    {
        ownedCharacters.Clear();

        // 1. Firebase 데이터 적용 (가챠에서 얻은 캐릭터 포함)
        if (owned != null)
        {
            foreach (var kvp in owned)
            {
                if (kvp.Value && int.TryParse(kvp.Key, out int characterId))
                {
                    ownedCharacters.Add(characterId);
                }
            }
        }

        // 2. 초기 캐릭터 및 테스트 캐릭터 보장
        EnsureInitialCharacters();

        Debug.Log($"<color=#3EB489>[CharacterOwnership]</color> 보유 캐릭터 로드 완료: {ownedCharacters.Count}개");
    }

    /// <summary>
    /// 캐릭터 이름 조회 (StringTable 연동)
    /// </summary>
    private string GetCharacterName(int characterId)
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            return $"Character_{characterId}";
        }

        var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
        if (characterData == null)
        {
            return $"Character_{characterId}";
        }

        var stringData = CSVLoader.Instance.GetData<StringTable>(characterData.Character_Name_ID);
        return stringData?.Text ?? $"Character_{characterId}";
    }

    #region Debug

    [ContextMenu("보유 캐릭터 출력")]
    private void PrintOwnedCharacters()
    {
        Debug.Log("=== 보유 캐릭터 목록 ===");
        foreach (int characterId in ownedCharacters)
        {
            string name = GetCharacterName(characterId);
            Debug.Log($"{name} (ID: {characterId})");
        }
    }

    [ContextMenu("테스트: 모든 캐릭터 해금")]
    private void UnlockAllCharacters()
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            Debug.LogError("[CharacterOwnershipManager] CSVLoader가 초기화되지 않았습니다.");
            return;
        }

        var characterTable = CSVLoader.Instance.GetTable<CharacterData>();
        if (characterTable == null) return;

        foreach (var characterData in characterTable.GetAll())
        {
            if (!ownedCharacters.Contains(characterData.Character_ID))
            {
                UnlockCharacter(characterData.Character_ID);
            }
        }
    }

    #endregion
}
