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

    [Header("Detail Popup")]
    [SerializeField] private ChaCard characterInfoCard; // 씬에 미리 배치된 상세 정보 카드
    [SerializeField] private GameObject dimBackground; // 딤 처리 배경
    [SerializeField] private GameObject characterInfoCardPanel; // 상세 정보 패널

    /// <summary>
    /// 슬롯 인덱스 → 캐릭터 ID 매핑
    /// </summary>
    private Dictionary<int, int> slotToCharacterId = new Dictionary<int, int>();

    private void Awake()
    {
        // Issue #476: Inspector에서 연결 필수 (Find 메서드 사용 금지)
        if (placementManager == null)
        {
            Debug.LogError("[CharacterCardGridManager] PlacementManager가 Inspector에서 연결되지 않았습니다!");
        }
    }

    private void Start()
    {
        // Issue #476: ClearAllSlots() 제거 - ChaCard.Awake()에서 이미 SetEmpty() 호출됨
        // 레이스 컨디션 방지: BossDungeonManager의 async 초기화와 충돌 가능

        // 상세 정보 카드 초기 비활성화
        if (characterInfoCard != null)
        {
            characterInfoCard.gameObject.SetActive(false);
        }
        if (characterInfoCardPanel != null)
        {
            characterInfoCardPanel.SetActive(false);
        }
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

        // Issue #476: 디버그 - 초기화할 카드 객체 정보
        Debug.Log($"[CharacterCardGridManager] OnCharacterSpawned: slotIndex={slotIndex}, card={card.gameObject.name}, instanceId={card.GetInstanceID()}");

        // 슬롯 매핑 저장
        slotToCharacterId[slotIndex] = characterId;

        // ChaCard 초기화 (Character 인스턴스 포함)
        await card.Initialize(characterId, starTier, character);

        Debug.Log($"[CharacterCardGridManager] Slot {slotIndex} updated: Character {characterId}, {starTier}성, card.IsEmpty={card.IsEmpty}");
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

    #region Support Skill Selection (Issue #437)

    // 현재 연결된 CardSelectPanel (스킬 장착용)
    private NovelianMagicLibraryDefense.UI.CardSelectPanel linkedCardSelectPanel;

    /// <summary>
    /// JML: 서포트 스킬과 호환되는 캐릭터만 하이라이트 (Issue #437)
    /// 호환되는 캐릭터: 올라오는 애니메이션
    /// 비호환 캐릭터: 딤 처리
    /// </summary>
    /// <param name="supportId">Support_ID (40001~)</param>
    /// <param name="cardSelectPanel">스킬 장착을 위한 CardSelectPanel 참조</param>
    public void ShowCompatibleCharacters(int supportId, NovelianMagicLibraryDefense.UI.CardSelectPanel cardSelectPanel)
    {
        linkedCardSelectPanel = cardSelectPanel;

        // 서포트 스킬 데이터 조회
        var supportData = CSVLoader.Instance?.GetData<SupportSkillData>(supportId);
        if (supportData == null)
        {
            Debug.LogWarning($"[CharacterCardGridManager] SupportSkillData not found for ID: {supportId}");
            ResetCharacterAnimations();
            return;
        }

        int compatibleCount = 0;

        // Issue #476: 디버그 - 각 슬롯 상태 출력
        Debug.Log($"[CharacterCardGridManager] ShowCompatibleCharacters 시작: cardSlots.Length={cardSlots.Length}");
        for (int i = 0; i < cardSlots.Length; i++)
        {
            var card = cardSlots[i];
            Debug.Log($"[CharacterCardGridManager] Slot[{i}]: card={card != null}, name={card?.gameObject.name}, instanceId={card?.GetInstanceID()}, IsEmpty={card?.IsEmpty}, CharacterId={card?.CharacterId}");
            if (card == null || card.IsEmpty) continue;

            // 해당 슬롯의 캐릭터 가져오기
            int characterId = card.CharacterId;
            var character = placementManager?.GetCharacterById(characterId);

            if (character == null) continue;

            // 동일 스킬이면 Dim (교체 의미 없음)
            if (character.GetSupportSkillId() == supportId)
            {
                card.PlayDimAnimation();
                continue;
            }

            // 메인 스킬 타입 조회
            int mainSkillId = character.GetBasicAttackSkillId();
            var mainSkillData = CSVLoader.Instance?.GetData<MainSkillData>(mainSkillId);

            if (mainSkillData == null)
            {
                card.PlayDimAnimation();
                continue;
            }

            // 호환성 검증 (SkillExecutor 사용)
            if (SkillExecutor.Instance != null && SkillExecutor.Instance.IsValidCombination(mainSkillData, supportData))
            {
                card.PlaySelectableAnimation();
                compatibleCount++;
            }
            else
            {
                card.PlayDimAnimation();
            }
        }

        Debug.Log($"[CharacterCardGridManager] ShowCompatibleCharacters: SupportID={supportId}, 호환={compatibleCount}명");
    }

    /// <summary>
    /// JML: 캐릭터 카드 애니메이션 초기화 (Issue #437)
    /// </summary>
    public void ResetCharacterAnimations()
    {
        linkedCardSelectPanel = null;

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null && !cardSlots[i].IsEmpty)
            {
                cardSlots[i].ResetAnimation();
            }
        }

        Debug.Log("[CharacterCardGridManager] Character animations reset");
    }

    /// <summary>
    /// JML: 캐릭터 카드 클릭 처리 (Issue #437)
    /// ChaCard에서 호출됨 → CardSelectPanel에 스킬 장착 요청
    /// </summary>
    public void OnCharacterCardClicked(int characterId)
    {
        if (linkedCardSelectPanel == null)
        {
            Debug.Log("[CharacterCardGridManager] No linked CardSelectPanel");
            return;
        }

        if (!linkedCardSelectPanel.HasSelectedSupportSkill())
        {
            Debug.Log("[CharacterCardGridManager] No support skill selected");
            return;
        }

        // CardSelectPanel에 장착 요청
        linkedCardSelectPanel.EquipSelectedSkillToCharacter(characterId);
    }

    #endregion

    #region Detail Popup

    /// <summary>
    /// JML: 캐릭터 카드 상세 팝업 표시
    /// 씬에 미리 배치된 characterInfoCard를 활성화하고 데이터 복사
    /// </summary>
    /// <param name="sourceCard">원본 ChaCard</param>
    public void ShowCardDetailPopup(ChaCard sourceCard)
    {
        if (sourceCard == null || sourceCard.IsEmpty)
        {
            Debug.LogWarning("[CharacterCardGridManager] Cannot show popup for empty card");
            return;
        }

        if (characterInfoCard == null)
        {
            Debug.LogError("[CharacterCardGridManager] characterInfoCard is not assigned!");
            return;
        }

        // 딤 배경 활성화
        if (dimBackground != null)
        {
            dimBackground.SetActive(true);
        }

        // 상세 정보 패널 활성화
        if (characterInfoCardPanel != null)
        {
            characterInfoCardPanel.SetActive(true);
        }

        // 상세 정보 카드 활성화
        characterInfoCard.gameObject.SetActive(true);

        // 팝업 모드 설정 및 데이터 복사
        characterInfoCard.SetPopupMode(true, CloseCardDetailPopup);
        characterInfoCard.CopyDataFrom(sourceCard);

        // UI 최상위로 이동 (다른 UI 위에 표시)
        characterInfoCard.transform.SetAsLastSibling();

        Debug.Log($"[CharacterCardGridManager] Popup opened for CharacterId={sourceCard.CharacterId}");
    }

    /// <summary>
    /// JML: 캐릭터 카드 상세 팝업 닫기
    /// </summary>
    public void CloseCardDetailPopup()
    {
        // 상세 정보 카드 비활성화
        if (characterInfoCard != null)
        {
            characterInfoCard.SetPopupMode(false);
            characterInfoCard.gameObject.SetActive(false);
            Debug.Log("[CharacterCardGridManager] Popup closed");
        }

        // 딤 배경 비활성화
        if (dimBackground != null)
        {
            dimBackground.SetActive(false);
        }

        // 상세 정보 패널 비활성화
        if (characterInfoCardPanel != null)
        {
            characterInfoCardPanel.SetActive(false);
        }
    }

    #endregion
}
