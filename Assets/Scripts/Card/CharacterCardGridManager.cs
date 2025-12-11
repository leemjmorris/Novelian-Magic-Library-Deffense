using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Novelian.Combat;
using UnityEngine;

/// <summary>
/// JML: CharacterCardGrid 관리자 (Issue #424)
/// 소환된 캐릭터들의 UI 표시 관리 (4슬롯)
/// CharacterPlacementManager와 연동하여 캐릭터 소환/업그레이드 시 UI 업데이트
/// </summary>
public class CharacterCardGridManager : MonoBehaviour
{
    [Header("Card Slots")]
    [SerializeField] private ChaCard[] cardSlots = new ChaCard[4];

    [Header("Dependencies")]
    [SerializeField] private CharacterPlacementManager placementManager;

    /// <summary>
    /// 슬롯 인덱스 → 캐릭터 ID 매핑
    /// </summary>
    private Dictionary<int, int> slotToCharacterId = new Dictionary<int, int>();

    private void Awake()
    {
        // CharacterPlacementManager 자동 찾기 (인스펙터에 설정 안 된 경우)
        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<CharacterPlacementManager>();
        }
    }

    private void Start()
    {
        // 모든 슬롯 빈 상태로 초기화
        ClearAllSlots();
    }

    /// <summary>
    /// JML: 캐릭터 소환 시 UI 업데이트
    /// CharacterPlacementManager.SpawnCharacterById() 후 호출
    /// </summary>
    /// <param name="slotIndex">GridSlot 인덱스 (0~3)</param>
    /// <param name="characterId">캐릭터 ID</param>
    /// <param name="starTier">성급 (기본 1)</param>
    /// <param name="character">소환된 Character 인스턴스 (스탯 표시용)</param>
    public async UniTask OnCharacterSpawned(int slotIndex, int characterId, int starTier = 1, Character character = null)
    {
        if (slotIndex < 0 || slotIndex >= cardSlots.Length)
        {
            Debug.LogWarning($"[CharacterCardGridManager] Invalid slot index: {slotIndex}");
            return;
        }

        var card = cardSlots[slotIndex];
        if (card == null)
        {
            Debug.LogWarning($"[CharacterCardGridManager] Card slot {slotIndex} is null");
            return;
        }

        // 슬롯 매핑 저장
        slotToCharacterId[slotIndex] = characterId;

        // ChaCard 초기화 (Character 인스턴스 포함)
        await card.Initialize(characterId, starTier, character);

        Debug.Log($"[CharacterCardGridManager] Slot {slotIndex} updated: Character {characterId}, {starTier}성");
    }

    /// <summary>
    /// JML: 캐릭터 성급 업그레이드 시 UI 업데이트
    /// </summary>
    /// <param name="characterId">캐릭터 ID</param>
    /// <param name="newStarTier">새 성급</param>
    public void OnCharacterUpgraded(int characterId, int newStarTier)
    {
        // 해당 캐릭터가 있는 슬롯 찾기
        foreach (var kvp in slotToCharacterId)
        {
            if (kvp.Value == characterId)
            {
                int slotIndex = kvp.Key;
                if (slotIndex >= 0 && slotIndex < cardSlots.Length && cardSlots[slotIndex] != null)
                {
                    cardSlots[slotIndex].UpdateStarTier(newStarTier);
                    Debug.Log($"[CharacterCardGridManager] Character {characterId} upgraded to {newStarTier}성 at slot {slotIndex}");
                }
                return;
            }
        }

        Debug.LogWarning($"[CharacterCardGridManager] Character {characterId} not found in any slot");
    }

    /// <summary>
    /// JML: 특정 슬롯 비우기
    /// </summary>
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= cardSlots.Length) return;

        var card = cardSlots[slotIndex];
        if (card != null)
        {
            card.SetEmpty();
        }

        slotToCharacterId.Remove(slotIndex);
        Debug.Log($"[CharacterCardGridManager] Slot {slotIndex} cleared");
    }

    /// <summary>
    /// JML: 모든 슬롯 비우기
    /// </summary>
    public void ClearAllSlots()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null)
            {
                cardSlots[i].SetEmpty();
            }
        }

        slotToCharacterId.Clear();
        Debug.Log("[CharacterCardGridManager] All slots cleared");
    }

    /// <summary>
    /// JML: CharacterPlacementManager의 현재 상태로 UI 동기화
    /// 게임 시작 시 또는 상태 복원 시 사용
    /// </summary>
    public async UniTask SyncWithPlacementManager()
    {
        if (placementManager == null)
        {
            Debug.LogWarning("[CharacterCardGridManager] PlacementManager is null, cannot sync");
            return;
        }

        // 모든 슬롯 초기화
        ClearAllSlots();

        // 필드의 모든 캐릭터 조회
        var characters = placementManager.GetAllCharacters();
        if (characters == null || characters.Count == 0)
        {
            Debug.Log("[CharacterCardGridManager] No characters to sync");
            return;
        }

        // 각 캐릭터의 슬롯 인덱스를 찾아서 UI 업데이트
        for (int slotIndex = 0; slotIndex < 4; slotIndex++)
        {
            var character = GetCharacterAtSlot(slotIndex);
            if (character != null)
            {
                int charId = character.GetCharacterId();
                int starTier = character.GetStarTier();
                await OnCharacterSpawned(slotIndex, charId, starTier);
            }
        }

        Debug.Log($"[CharacterCardGridManager] Synced {characters.Count} characters");
    }

    /// <summary>
    /// JML: 특정 슬롯의 캐릭터 조회 (CharacterPlacementManager 연동)
    /// </summary>
    private Character GetCharacterAtSlot(int slotIndex)
    {
        if (placementManager == null) return null;

        // CharacterPlacementManager의 GridSlot에서 캐릭터 가져오기
        // 이 부분은 CharacterPlacementManager의 구현에 따라 조정 필요
        var allCharacters = placementManager.GetAllCharacters();

        // 슬롯 인덱스에 해당하는 캐릭터 찾기 (이름 기반)
        foreach (var character in allCharacters)
        {
            if (character.gameObject.name.Contains($"_Slot{slotIndex}"))
            {
                return character;
            }
        }

        return null;
    }

    /// <summary>
    /// JML: 캐릭터 ID로 슬롯 인덱스 조회
    /// </summary>
    public int GetSlotIndexByCharacterId(int characterId)
    {
        foreach (var kvp in slotToCharacterId)
        {
            if (kvp.Value == characterId)
            {
                return kvp.Key;
            }
        }
        return -1;
    }

    /// <summary>
    /// JML: 첫 번째 빈 슬롯 인덱스 반환
    /// </summary>
    public int GetFirstEmptySlotIndex()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null && cardSlots[i].IsEmpty)
            {
                return i;
            }
        }
        return -1; // 빈 슬롯 없음
    }

    /// <summary>
    /// JML: 모든 슬롯이 채워져 있는지 확인
    /// </summary>
    public bool IsAllSlotsFilled()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] == null || cardSlots[i].IsEmpty)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// JML: 모든 슬롯의 스탯 정보 갱신 (Issue #424)
    /// 스탯 카드 적용 후 호출하여 UI 실시간 업데이트
    /// </summary>
    public void RefreshAllStats()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null && !cardSlots[i].IsEmpty)
            {
                cardSlots[i].RefreshStats();
            }
        }
        Debug.Log("[CharacterCardGridManager] All card stats refreshed");
    }
}
