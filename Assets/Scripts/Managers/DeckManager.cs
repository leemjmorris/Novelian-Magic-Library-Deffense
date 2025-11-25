using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    private const int MAX_DECK_SIZE = 4;
    private const int MIN_DECK_SIZE = 3;
    private List<int> currentDeck = new List<int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 특정 인덱스에 캐릭터 설정 (덱 슬롯 순서 유지)
    /// </summary>
    public bool SetCharacterAtIndex(int index, int characterID)
    {
        if (index < 0 || index >= MAX_DECK_SIZE)
        {
            Debug.LogWarning($"[DeckManager] 잘못된 인덱스: {index}");
            return false;
        }

        // 이미 다른 슬롯에 같은 캐릭터가 있으면 제거
        int existingIndex = currentDeck.IndexOf(characterID);
        if (existingIndex >= 0 && existingIndex != index)
        {
            Debug.LogWarning($"[DeckManager] 캐릭터 ID {characterID}는 이미 슬롯 {existingIndex}에 있습니다. 교체합니다.");
            currentDeck[existingIndex] = -1; // 빈 슬롯으로 표시
        }

        // 리스트 크기 확장 (필요 시)
        while (currentDeck.Count <= index)
        {
            currentDeck.Add(-1); // -1은 빈 슬롯
        }

        currentDeck[index] = characterID;
        Debug.Log($"[DeckManager] 슬롯 {index}에 캐릭터 ID {characterID} 설정. (현재 덱: {GetDeckCount()}/{MAX_DECK_SIZE})");
        return true;
    }

    /// <summary>
    /// 특정 인덱스의 캐릭터 가져오기
    /// </summary>
    public int GetCharacterAtIndex(int index)
    {
        if (index < 0 || index >= currentDeck.Count)
        {
            return -1; // 빈 슬롯
        }
        return currentDeck[index];
    }

    /// <summary>
    /// 특정 인덱스의 캐릭터 제거
    /// </summary>
    public void RemoveAtIndex(int index)
    {
        if (index >= 0 && index < currentDeck.Count)
        {
            int characterID = currentDeck[index];
            currentDeck[index] = -1;
            Debug.Log($"[DeckManager] 슬롯 {index}에서 캐릭터 ID {characterID} 제거.");
        }
    }

    /// <summary>
    /// 덱에 캐릭터 추가 (빈 슬롯 자동 찾기)
    /// </summary>
    public bool AddToDeck(int characterID)
    {
        // 이미 덱에 있는지 확인
        if (currentDeck.Contains(characterID))
        {
            Debug.LogWarning($"캐릭터 ID {characterID}는 이미 덱에 있습니다.");
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

        Debug.LogWarning("덱이 가득 찼습니다. (최대 4명)");
        return false;
    }

    public bool RemoveFromDeck(int characterID)
    {
        int index = currentDeck.IndexOf(characterID);
        if (index >= 0)
        {
            RemoveAtIndex(index);
            return true;
        }

        Debug.LogWarning($"캐릭터 ID {characterID}는 덱에 없습니다.");
        return false;
    }

    public void ClearDeck()
    {
        currentDeck.Clear();
        Debug.Log("덱을 초기화했습니다.");
    }

    /// <summary>
    /// 덱 전체 가져오기 (빈 슬롯 포함)
    /// </summary>
    public List<int> GetDeck()
    {
        return new List<int>(currentDeck);
    }

    /// <summary>
    /// 유효한 캐릭터만 가져오기 (빈 슬롯 제외)
    /// </summary>
    public List<int> GetValidCharacters()
    {
        List<int> validChars = new List<int>();
        foreach (int id in currentDeck)
        {
            if (id > 0) // -1이 아닌 유효한 ID만
            {
                validChars.Add(id);
            }
        }
        return validChars;
    }

    public bool IsInDeck(int characterID)
    {
        return currentDeck.Contains(characterID);
    }

    public bool IsDeckFull()
    {
        int validCount = 0;
        foreach (int id in currentDeck)
        {
            if (id > 0) validCount++;
        }
        return validCount >= MAX_DECK_SIZE;
    }

    public bool IsDeckEmpty()
    {
        foreach (int id in currentDeck)
        {
            if (id > 0) return false;
        }
        return true;
    }

    public bool IsDeckValid()
    {
        int validCount = 0;
        foreach (int id in currentDeck)
        {
            if (id > 0) validCount++;
        }
        return validCount >= MIN_DECK_SIZE;
    }

    /// <summary>
    /// 유효한 캐릭터 수 반환 (빈 슬롯 제외)
    /// </summary>
    public int GetDeckCount()
    {
        int count = 0;
        foreach (int id in currentDeck)
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
            Debug.LogWarning("[DeckManager] 덱이 비어있습니다!");
            return -1;
        }

        int randomIndex = Random.Range(0, validChars.Count);
        return validChars[randomIndex];
    }
}
