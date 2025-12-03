using System;
using System.Collections.Generic;
using UnityEngine;

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

    // 보유 캐릭터 ID 목록
    private HashSet<int> ownedCharacters = new HashSet<int>();

    // 초기 보유 캐릭터 ID
    private static readonly int[] INITIAL_CHARACTERS = { 22007, 24013, 25017 };

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
        // 초기 보유 캐릭터 등록
        foreach (int characterId in INITIAL_CHARACTERS)
        {
            ownedCharacters.Add(characterId);
        }

        Debug.Log($"[CharacterOwnershipManager] 초기화 완료. 보유 캐릭터: {ownedCharacters.Count}개");
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
