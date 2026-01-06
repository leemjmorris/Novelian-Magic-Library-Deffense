using System.Collections.Generic;
using UnityEngine;
using Firebase.Data;
using Cysharp.Threading.Tasks;
using System;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    private const int MAX_DECK_SIZE = 4;
    private const int MIN_DECK_SIZE = 3;
    private const int PRESET_COUNT = 4;

    // 현재 선택된 프리셋 인덱스 (0~3)
    private int currentPresetIndex = 0;

    // 4개의 프리셋 덱
    private List<int>[] deckPresets = new List<int>[PRESET_COUNT];

    // 프리셋 변경 이벤트
    public event Action<int> OnPresetChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePresets();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 프리셋 배열 초기화
    /// </summary>
    private void InitializePresets()
    {
        for (int i = 0; i < PRESET_COUNT; i++)
        {
            deckPresets[i] = new List<int> { -1, -1, -1, -1 };
        }
    }

    #region Preset Management

    /// <summary>
    /// 현재 선택된 프리셋 인덱스 반환
    /// </summary>
    public int GetCurrentPresetIndex()
    {
        return currentPresetIndex;
    }

    /// <summary>
    /// 프리셋 전환 (자동 저장 후 전환)
    /// </summary>
    public void SwitchPreset(int newPresetIndex)
    {
        if (newPresetIndex < 0 || newPresetIndex >= PRESET_COUNT)
        {
            GameLog.LogWarning($"[DeckManager] 잘못된 프리셋 인덱스: {newPresetIndex}");
            return;
        }

        if (currentPresetIndex == newPresetIndex)
        {
            GameLog.Log($"[DeckManager] 이미 프리셋 {newPresetIndex + 1}이 선택되어 있습니다.");
            return;
        }

        int oldPreset = currentPresetIndex;
        currentPresetIndex = newPresetIndex;

        GameLog.Log($"[DeckManager] 프리셋 전환: {oldPreset + 1} → {newPresetIndex + 1}");
        GameLog.Log($"[DeckManager] 새 프리셋 덱: [{string.Join(", ", deckPresets[newPresetIndex])}]");

        // Firebase에 현재 프리셋 인덱스 저장
        SaveToFirebase();

        // 이벤트 발생
        OnPresetChanged?.Invoke(newPresetIndex);
    }

    /// <summary>
    /// 특정 프리셋이 비어있는지 확인
    /// </summary>
    public bool IsPresetEmpty(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= PRESET_COUNT)
            return true;

        foreach (int id in deckPresets[presetIndex])
        {
            if (id > 0) return false;
        }
        return true;
    }

    /// <summary>
    /// 특정 프리셋의 덱 가져오기
    /// </summary>
    public List<int> GetPresetDeck(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= PRESET_COUNT)
            return new List<int> { -1, -1, -1, -1 };

        return new List<int>(deckPresets[presetIndex]);
    }

    /// <summary>
    /// 특정 프리셋의 덱 설정
    /// </summary>
    public void SetPresetDeck(int presetIndex, List<int> deck)
    {
        if (presetIndex < 0 || presetIndex >= PRESET_COUNT)
            return;

        deckPresets[presetIndex].Clear();
        for (int i = 0; i < MAX_DECK_SIZE; i++)
        {
            if (i < deck.Count)
                deckPresets[presetIndex].Add(deck[i]);
            else
                deckPresets[presetIndex].Add(-1);
        }

        GameLog.Log($"[DeckManager] 프리셋 {presetIndex + 1} 덱 설정: [{string.Join(", ", deckPresets[presetIndex])}]");
    }

    /// <summary>
    /// 특정 프리셋의 유효한 캐릭터 수 반환
    /// </summary>
    public int GetPresetDeckCount(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= PRESET_COUNT)
            return 0;

        int count = 0;
        foreach (int id in deckPresets[presetIndex])
        {
            if (id > 0) count++;
        }
        return count;
    }

    /// <summary>
    /// 프리셋 개수 반환
    /// </summary>
    public int GetPresetCount()
    {
        return PRESET_COUNT;
    }

    #endregion

    #region Current Deck Operations (기존 API 유지 - 현재 프리셋에 대해 동작)

    /// <summary>
    /// 특정 인덱스에 캐릭터 설정 (현재 프리셋의 덱 슬롯)
    /// </summary>
    public bool SetCharacterAtIndex(int index, int characterID)
    {
        if (index < 0 || index >= MAX_DECK_SIZE)
        {
            GameLog.LogWarning($"[DeckManager] 잘못된 인덱스: {index}");
            return false;
        }

        var currentDeck = deckPresets[currentPresetIndex];

        // 이미 다른 슬롯에 같은 캐릭터가 있으면 제거
        int existingIndex = currentDeck.IndexOf(characterID);
        if (existingIndex >= 0 && existingIndex != index)
        {
            GameLog.LogWarning($"[DeckManager] 캐릭터 ID {characterID}는 이미 슬롯 {existingIndex}에 있습니다. 교체합니다.");
            currentDeck[existingIndex] = -1;
        }

        // 리스트 크기 확장 (필요 시)
        while (currentDeck.Count <= index)
        {
            currentDeck.Add(-1);
        }

        currentDeck[index] = characterID;
        GameLog.Log($"[DeckManager] 프리셋 {currentPresetIndex + 1} 슬롯 {index}에 캐릭터 ID {characterID} 설정. (현재 덱: {GetDeckCount()}/{MAX_DECK_SIZE})");
        SaveToFirebase();
        return true;
    }

    /// <summary>
    /// 특정 인덱스의 캐릭터 가져오기 (현재 프리셋)
    /// </summary>
    public int GetCharacterAtIndex(int index)
    {
        var currentDeck = deckPresets[currentPresetIndex];
        if (index < 0 || index >= currentDeck.Count)
        {
            return -1;
        }
        return currentDeck[index];
    }

    /// <summary>
    /// 특정 인덱스의 캐릭터 제거 (현재 프리셋)
    /// </summary>
    public void RemoveAtIndex(int index)
    {
        var currentDeck = deckPresets[currentPresetIndex];
        if (index >= 0 && index < currentDeck.Count)
        {
            int characterID = currentDeck[index];
            currentDeck[index] = -1;
            GameLog.Log($"[DeckManager] 프리셋 {currentPresetIndex + 1} 슬롯 {index}에서 캐릭터 ID {characterID} 제거.");
            SaveToFirebase();
        }
    }

    /// <summary>
    /// 덱에 캐릭터 추가 (빈 슬롯 자동 찾기, 현재 프리셋)
    /// </summary>
    public bool AddToDeck(int characterID)
    {
        var currentDeck = deckPresets[currentPresetIndex];

        // 이미 덱에 있는지 확인
        if (currentDeck.Contains(characterID))
        {
            GameLog.LogWarning($"캐릭터 ID {characterID}는 이미 덱에 있습니다.");
            return false;
        }

        // 빈 슬롯 찾기
        for (int i = 0; i < MAX_DECK_SIZE; i++)
        {
            if (i >= currentDeck.Count || currentDeck[i] == -1)
            {
                return SetCharacterAtIndex(i, characterID);
            }
        }

        GameLog.LogWarning("덱이 가득 찼습니다. (최대 4명)");
        return false;
    }

    public bool RemoveFromDeck(int characterID)
    {
        var currentDeck = deckPresets[currentPresetIndex];
        int index = currentDeck.IndexOf(characterID);
        if (index >= 0)
        {
            RemoveAtIndex(index);
            return true;
        }

        GameLog.LogWarning($"캐릭터 ID {characterID}는 덱에 없습니다.");
        return false;
    }

    /// <summary>
    /// 현재 프리셋의 덱 초기화
    /// </summary>
    public void ClearDeck()
    {
        deckPresets[currentPresetIndex] = new List<int> { -1, -1, -1, -1 };
        GameLog.Log($"[DeckManager] 프리셋 {currentPresetIndex + 1} 덱을 초기화했습니다.");
        SaveToFirebase();
    }

    /// <summary>
    /// 덱 전체 가져오기 (빈 슬롯 포함, 현재 프리셋)
    /// </summary>
    public List<int> GetDeck()
    {
        return new List<int>(deckPresets[currentPresetIndex]);
    }

    /// <summary>
    /// 유효한 캐릭터만 가져오기 (빈 슬롯 제외, 현재 프리셋)
    /// </summary>
    public List<int> GetValidCharacters()
    {
        List<int> validChars = new List<int>();
        foreach (int id in deckPresets[currentPresetIndex])
        {
            if (id > 0)
            {
                validChars.Add(id);
            }
        }
        return validChars;
    }

    public bool IsInDeck(int characterID)
    {
        return deckPresets[currentPresetIndex].Contains(characterID);
    }

    public bool IsDeckFull()
    {
        int validCount = 0;
        foreach (int id in deckPresets[currentPresetIndex])
        {
            if (id > 0) validCount++;
        }
        return validCount >= MAX_DECK_SIZE;
    }

    public bool IsDeckEmpty()
    {
        foreach (int id in deckPresets[currentPresetIndex])
        {
            if (id > 0) return false;
        }
        return true;
    }

    public bool IsDeckValid()
    {
        int validCount = 0;
        foreach (int id in deckPresets[currentPresetIndex])
        {
            if (id > 0) validCount++;
        }
        return validCount >= MIN_DECK_SIZE;
    }

    /// <summary>
    /// 유효한 캐릭터 수 반환 (빈 슬롯 제외, 현재 프리셋)
    /// </summary>
    public int GetDeckCount()
    {
        int count = 0;
        foreach (int id in deckPresets[currentPresetIndex])
        {
            if (id > 0) count++;
        }
        return count;
    }

    public int GetMaxDeckSize()
    {
        return MAX_DECK_SIZE;
    }

    public int GetMinDeckSize()
    {
        return MIN_DECK_SIZE;
    }

    public int GetRandomCharacterFromDeck()
    {
        List<int> validChars = GetValidCharacters();

        if (validChars.Count == 0)
        {
            GameLog.LogWarning("[DeckManager] 덱이 비어있습니다!");
            return -1;
        }

        int randomIndex = UnityEngine.Random.Range(0, validChars.Count);
        return validChars[randomIndex];
    }

    #endregion

    #region Firebase Integration

    /// <summary>
    /// 현재 덱 상태를 Firebase에 저장
    /// </summary>
    private void SaveToFirebase()
    {
        if (FirebaseSaveManager.Instance == null || !FirebaseSaveManager.Instance.IsInitialized)
            return;

        if (FirebaseManager.Instance == null || string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserId))
            return;

        // 전체 프리셋 데이터 저장
        var deckData = CreateDeckData();
        FirebaseSaveManager.Instance.SaveDeckPresetsAsync(
            FirebaseManager.Instance.CurrentUserId,
            deckData
        ).Forget();
    }

    /// <summary>
    /// 현재 상태로 DeckData 생성
    /// </summary>
    private DeckData CreateDeckData()
    {
        var data = new DeckData();
        data.currentPreset = currentPresetIndex;

        data.preset0.FromList(deckPresets[0]);
        data.preset1.FromList(deckPresets[1]);
        data.preset2.FromList(deckPresets[2]);
        data.preset3.FromList(deckPresets[3]);

        return data;
    }

    /// <summary>
    /// Firebase 데이터로 덱 설정 (BootScene에서 호출)
    /// </summary>
    public void SetDeckFromFirebase(DeckData data)
    {
        if (data == null)
        {
            GameLog.LogWarning("[DeckManager] DeckData가 null입니다. 기본값 사용.");
            return;
        }

        // 마이그레이션 처리 (기존 데이터가 있으면 preset0으로 이동)
        if (data.HasLegacyData())
        {
            GameLog.Log("[DeckManager] 기존 덱 데이터 마이그레이션 수행");
            data.MigrateLegacyData();
        }

        // 프리셋 인덱스 설정
        currentPresetIndex = Mathf.Clamp(data.currentPreset, 0, PRESET_COUNT - 1);

        // 각 프리셋 데이터 로드
        deckPresets[0] = data.preset0?.ToList() ?? new List<int> { -1, -1, -1, -1 };
        deckPresets[1] = data.preset1?.ToList() ?? new List<int> { -1, -1, -1, -1 };
        deckPresets[2] = data.preset2?.ToList() ?? new List<int> { -1, -1, -1, -1 };
        deckPresets[3] = data.preset3?.ToList() ?? new List<int> { -1, -1, -1, -1 };

        // 리스트 크기 보정
        for (int i = 0; i < PRESET_COUNT; i++)
        {
            while (deckPresets[i].Count < MAX_DECK_SIZE)
            {
                deckPresets[i].Add(-1);
            }
        }

        int totalValidCount = 0;
        for (int i = 0; i < PRESET_COUNT; i++)
        {
            totalValidCount += GetPresetDeckCount(i);
        }

        GameLog.Log($"<color=#3EB489>[DeckManager]</color> Firebase에서 덱 로드 완료. 현재 프리셋: {currentPresetIndex + 1}, 총 캐릭터: {totalValidCount}개");
        GameLog.Log($"[DeckManager] 프리셋 1: [{string.Join(", ", deckPresets[0])}]");
        GameLog.Log($"[DeckManager] 프리셋 2: [{string.Join(", ", deckPresets[1])}]");
        GameLog.Log($"[DeckManager] 프리셋 3: [{string.Join(", ", deckPresets[2])}]");
        GameLog.Log($"[DeckManager] 프리셋 4: [{string.Join(", ", deckPresets[3])}]");
    }

    #endregion
}
