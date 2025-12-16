using System.Collections.Generic;
using System.Linq;
using Novelian.Combat;
using UnityEngine;

/// <summary>
/// 파티 시너지 효과 관리
/// - 덱의 활성 시너지 확인
/// - 장르 강화 버프 적용
/// </summary>
public class PartySynergyManager : MonoBehaviour
{
    public static PartySynergyManager Instance { get; private set; }

    // Effect_ID → Genre 매핑 (PartySynergyEffectTable 기준)
    // 05001=로맨스, 05002=공포, 05003=코믹, 05004=모험, 05005=추리
    private static readonly Dictionary<int, Genre> EffectToGenreMapping = new Dictionary<int, Genre>
    {
        { 5001, Genre.Romance },
        { 5002, Genre.Horror },
        { 5003, Genre.Comedy },
        { 5004, Genre.Adventure },
        { 5005, Genre.Mystery }
    };

    // 현재 활성 시너지
    private PartySynergyData activeSynergy;
    private int synergyLevel = 1; // 1~5 (업그레이드 레벨, 추후 구현)

    // 시너지 적용 여부 추적 (중복 방지)
    private bool isSynergyApplied = false;

    private void Awake()
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
    /// 현재 덱 기준 활성 시너지 확인
    /// </summary>
    public PartySynergyData GetActiveSynergy()
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogWarning("[PartySynergyManager] DeckManager.Instance is null");
            return null;
        }

        var deckCharacterIds = DeckManager.Instance.GetValidCharacters();
        if (deckCharacterIds.Count < 4)
        {
            Debug.Log($"[PartySynergyManager] 덱에 캐릭터가 {deckCharacterIds.Count}명뿐입니다. 시너지 활성화에는 4명 필요.");
            return null;
        }

        // 시너지 테이블 로드
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            Debug.LogWarning("[PartySynergyManager] CSVLoader가 초기화되지 않았습니다.");
            return null;
        }

        var synergyTable = CSVLoader.Instance.GetTable<PartySynergyData>();
        if (synergyTable == null)
        {
            Debug.LogWarning("[PartySynergyManager] PartySynergyTable이 null입니다.");
            return null;
        }

        var allSynergies = synergyTable.GetAll()
            .Where(s => s.Party_Name_ID != 0)
            .ToList();

        var deckSet = new HashSet<int>(deckCharacterIds);

        foreach (var synergy in allSynergies)
        {
            bool isActive = deckSet.Contains(synergy.Req_Char_1_ID)
                         && deckSet.Contains(synergy.Req_Char_2_ID)
                         && deckSet.Contains(synergy.Req_Char_3_ID)
                         && deckSet.Contains(synergy.Req_Char_4_ID);

            if (isActive)
            {
                Debug.Log($"[PartySynergyManager] 시너지 활성화됨! Party_ID: {synergy.Party_ID}");
                activeSynergy = synergy;
                return synergy;
            }
        }

        Debug.Log("[PartySynergyManager] 활성화된 시너지 없음");
        activeSynergy = null;
        return null;
    }

    /// <summary>
    /// 현재 시너지 레벨의 효과 데이터 반환
    /// </summary>
    public (int effectId, float effectValue) GetEffectData()
    {
        if (activeSynergy == null)
        {
            return (0, 0f);
        }

        return synergyLevel switch
        {
            1 => (activeSynergy.Effect_1_ID, activeSynergy.Effect_1_Value),
            2 => (activeSynergy.Effect_2_ID, activeSynergy.Effect_2_Value),
            3 => (activeSynergy.Effect_3_ID, activeSynergy.Effect_3_Value),
            4 => (activeSynergy.Effect_4_ID, activeSynergy.Effect_4_Value),
            5 => (activeSynergy.Effect_5_ID, activeSynergy.Effect_5_Value),
            _ => (activeSynergy.Effect_1_ID, activeSynergy.Effect_1_Value)
        };
    }

    /// <summary>
    /// Effect_ID → Genre 변환
    /// </summary>
    public Genre? GetGenreFromEffectId(int effectId)
    {
        if (EffectToGenreMapping.TryGetValue(effectId, out Genre genre))
        {
            return genre;
        }

        Debug.LogWarning($"[PartySynergyManager] 알 수 없는 Effect_ID: {effectId}");
        return null;
    }

    /// <summary>
    /// 캐릭터들에게 시너지 버프 적용
    /// </summary>
    public void ApplySynergyToCharacters(List<Character> characters)
    {
        if (isSynergyApplied)
        {
            Debug.Log("[PartySynergyManager] 이미 시너지가 적용되어 있습니다.");
            return;
        }

        // 활성 시너지 확인
        var synergy = GetActiveSynergy();
        if (synergy == null)
        {
            Debug.Log("[PartySynergyManager] 적용할 시너지가 없습니다.");
            return;
        }

        var (effectId, effectValue) = GetEffectData();
        var genre = GetGenreFromEffectId(effectId);

        if (!genre.HasValue)
        {
            Debug.LogWarning($"[PartySynergyManager] Effect_ID {effectId}에 대한 장르를 찾을 수 없습니다.");
            return;
        }

        // 값 변환: CSV의 5 → 0.05 (5%)
        float buffValue = effectValue / 100f;

        Debug.Log($"[PartySynergyManager] 시너지 적용 시작: {genre.Value} 장르 +{effectValue}% ({characters.Count}명)");

        foreach (var character in characters)
        {
            if (character != null)
            {
                character.ApplyGenreSynergyBuff(genre.Value, buffValue);
            }
        }

        isSynergyApplied = true;
        Debug.Log($"[PartySynergyManager] 시너지 적용 완료! {genre.Value} 장르 데미지 +{effectValue}%");
    }

    /// <summary>
    /// 단일 캐릭터에 시너지 버프 적용 (새로 스폰된 캐릭터용)
    /// </summary>
    public void ApplySynergyToCharacter(Character character)
    {
        if (activeSynergy == null) return;
        if (character == null) return;

        var (effectId, effectValue) = GetEffectData();
        var genre = GetGenreFromEffectId(effectId);

        if (!genre.HasValue) return;

        float buffValue = effectValue / 100f;
        character.ApplyGenreSynergyBuff(genre.Value, buffValue);

        Debug.Log($"[PartySynergyManager] 새 캐릭터에 시너지 적용: {genre.Value} +{effectValue}%");
    }

    /// <summary>
    /// 시너지 상태 초기화 (스테이지 종료 시)
    /// </summary>
    public void ResetSynergy()
    {
        activeSynergy = null;
        isSynergyApplied = false;
        Debug.Log("[PartySynergyManager] 시너지 상태 초기화");
    }

    /// <summary>
    /// 현재 활성 시너지 반환 (캐시된 값)
    /// </summary>
    public PartySynergyData GetCurrentActiveSynergy()
    {
        return activeSynergy;
    }

    /// <summary>
    /// 시너지 레벨 설정 (추후 업그레이드 시스템용)
    /// </summary>
    public void SetSynergyLevel(int level)
    {
        synergyLevel = Mathf.Clamp(level, 1, 5);
        Debug.Log($"[PartySynergyManager] 시너지 레벨 설정: {synergyLevel}");
    }

    /// <summary>
    /// 현재 시너지 레벨 반환
    /// </summary>
    public int GetSynergyLevel()
    {
        return synergyLevel;
    }
}
