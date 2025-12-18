using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 스킬 이펙트 데이터베이스
/// SpecialSkillsEffectsPack 에셋 프리팹과 게임 스킬을 매핑하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "SkillEffectDatabase", menuName = "Skills/Skill Effect Database", order = 1)]
public class SkillEffectDatabase : ScriptableObject
{
    private const string ASSET_PATH = "Assets/ScriptableObjects/Skills/SkillEffectDatabase.asset";
    private const string ADDRESSABLE_KEY = "SkillEffectDatabase";

    private static SkillEffectDatabase _instance;
    private static bool _isLoading = false;

    public static SkillEffectDatabase Instance
    {
        get
        {
            if (_instance == null && !_isLoading)
            {
                LoadInstance();
            }
            return _instance;
        }
    }

    /// <summary>
    /// 인스턴스 로드 (에디터: AssetDatabase, 빌드: Addressables)
    /// </summary>
    private static void LoadInstance()
    {
        _isLoading = true;

#if UNITY_EDITOR
        // 에디터: AssetDatabase로 직접 로드
        _instance = AssetDatabase.LoadAssetAtPath<SkillEffectDatabase>(ASSET_PATH);
        if (_instance == null)
        {
            // 프로젝트에서 검색
            string[] guids = AssetDatabase.FindAssets("t:SkillEffectDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _instance = AssetDatabase.LoadAssetAtPath<SkillEffectDatabase>(path);
            }
        }

        if (_instance == null)
        {
            Debug.LogWarning($"[SkillEffectDatabase] Not found at {ASSET_PATH}. Create via Tools > Skills > Skill Effect Mapper");
        }
#else
        // 빌드: Addressables 동기 로드 시도
        try
        {
            var handle = Addressables.LoadAssetAsync<SkillEffectDatabase>(ADDRESSABLE_KEY);
            _instance = handle.WaitForCompletion();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SkillEffectDatabase] Failed to load from Addressables: {e.Message}");
        }
#endif

        _isLoading = false;
    }

    /// <summary>
    /// 인스턴스 수동 설정 (GameManager 등에서 Addressables 로드 후 호출)
    /// </summary>
    public static void SetInstance(SkillEffectDatabase database)
    {
        _instance = database;
        _instance?.Initialize();
    }

    /// <summary>
    /// Addressables 비동기 로드 (권장)
    /// 에디터: AssetDatabase 사용, 빌드: Addressables 사용
    /// </summary>
    public static async UniTask LoadInstanceAsync()
    {
        if (_instance != null) return;

        _isLoading = true;

#if UNITY_EDITOR
        // 에디터: AssetDatabase로 직접 로드 (Addressables 설정 없이도 동작)
        _instance = AssetDatabase.LoadAssetAtPath<SkillEffectDatabase>(ASSET_PATH);
        if (_instance == null)
        {
            // 프로젝트에서 검색
            string[] guids = AssetDatabase.FindAssets("t:SkillEffectDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _instance = AssetDatabase.LoadAssetAtPath<SkillEffectDatabase>(path);
            }
        }

        if (_instance != null)
        {
            _instance.Initialize();
            Debug.Log("[SkillEffectDatabase] Loaded from AssetDatabase (Editor)");
        }
        else
        {
            Debug.LogWarning($"[SkillEffectDatabase] Not found. Create via Assets > Create > Skills > Skill Effect Database");
        }

        await UniTask.Yield(); // 에디터에서도 async 유지
#else
        // 빌드: Addressables 비동기 로드
        try
        {
            var handle = Addressables.LoadAssetAsync<SkillEffectDatabase>(ADDRESSABLE_KEY);
            await handle.Task;
            _instance = handle.Result;
            _instance?.Initialize();
            Debug.Log("[SkillEffectDatabase] Loaded from Addressables");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SkillEffectDatabase] Async load failed: {e.Message}");
        }
#endif

        _isLoading = false;
    }

    [Header("전역 설정")]
    [Tooltip("모든 이펙트에 적용되는 기본 스케일")]
    [Min(0.1f)]
    public float globalScaleFactor = 1f;

    [Header("메인 스킬 이펙트 목록")]
    [Tooltip("메인 스킬 ID와 이펙트 프리팹 매핑")]
    public List<SkillEffectEntry> entries = new List<SkillEffectEntry>();

    [Header("서포트 스킬 이펙트 목록")]
    [Tooltip("서포트 스킬 ID와 상태이상 아이콘 매핑")]
    public List<SupportSkillEffectEntry> supportEntries = new List<SupportSkillEffectEntry>();

    // 런타임 캐시
    private Dictionary<int, SkillEffectEntry> _cache;
    private Dictionary<int, SupportSkillEffectEntry> _supportCache;

    /// <summary>
    /// 캐시 초기화
    /// </summary>
    public void Initialize()
    {
        BuildCache();
    }

    private void OnEnable()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        // 메인 스킬 캐시
        _cache = new Dictionary<int, SkillEffectEntry>();
        int mainDuplicateCount = 0;
        foreach (var entry in entries)
        {
            if (!_cache.ContainsKey(entry.skillId))
            {
                _cache[entry.skillId] = entry;
            }
            else
            {
                mainDuplicateCount++;
            }
        }

        if (mainDuplicateCount > 0)
        {
            Debug.LogWarning($"[SkillEffectDatabase] Found {mainDuplicateCount} duplicate skill entries. Use Tools > Skills > Skill Effect Mapper > Settings > 'Remove Duplicate Entries' to fix.");
        }

        // 서포트 스킬 캐시
        _supportCache = new Dictionary<int, SupportSkillEffectEntry>();
        int supportDuplicateCount = 0;
        foreach (var entry in supportEntries)
        {
            if (!_supportCache.ContainsKey(entry.supportId))
            {
                _supportCache[entry.supportId] = entry;
            }
            else
            {
                supportDuplicateCount++;
            }
        }

        if (supportDuplicateCount > 0)
        {
            Debug.LogWarning($"[SkillEffectDatabase] Found {supportDuplicateCount} duplicate support skill entries.");
        }
    }

    #region Query Methods

    /// <summary>
    /// 스킬 이펙트 엔트리 조회
    /// </summary>
    public SkillEffectEntry GetEntry(int skillId)
    {
        if (_cache == null) BuildCache();

        if (_cache.TryGetValue(skillId, out var entry))
        {
            return entry;
        }
        return null;
    }

    /// <summary>
    /// 메인 이펙트 프리팹 조회
    /// </summary>
    public GameObject GetMainEffect(int skillId)
    {
        return GetEntry(skillId)?.mainEffectPrefab;
    }

    /// <summary>
    /// 피격 이펙트 프리팹 조회
    /// </summary>
    public GameObject GetHitEffect(int skillId)
    {
        return GetEntry(skillId)?.hitEffectPrefab;
    }

    /// <summary>
    /// 시전 이펙트 프리팹 조회
    /// </summary>
    public GameObject GetCastEffect(int skillId)
    {
        return GetEntry(skillId)?.castEffectPrefab;
    }

    /// <summary>
    /// 트레일 이펙트 프리팹 조회
    /// </summary>
    public GameObject GetTrailEffect(int skillId)
    {
        return GetEntry(skillId)?.trailEffectPrefab;
    }

    /// <summary>
    /// 스킬의 실제 적용 스케일 조회
    /// </summary>
    public float GetEffectScale(int skillId)
    {
        var entry = GetEntry(skillId);
        if (entry == null) return globalScaleFactor;
        return entry.GetEffectiveScale(globalScaleFactor);
    }

    /// <summary>
    /// 이펙트가 에셋의 이동 로직을 사용하는지 확인
    /// </summary>
    public bool UsesAssetMovement(int skillId)
    {
        var entry = GetEntry(skillId);
        return entry?.useAssetMovement ?? false;
    }

    /// <summary>
    /// 이펙트 지속시간 조회 (AOE DOT에 사용)
    /// </summary>
    public float GetEffectDuration(int skillId)
    {
        var entry = GetEntry(skillId);
        return entry?.effectDuration ?? 0f;
    }

    #endregion

    #region Support Skill Query Methods

    /// <summary>
    /// 서포트 스킬 이펙트 엔트리 조회
    /// </summary>
    public SupportSkillEffectEntry GetSupportEntry(int supportId)
    {
        if (_supportCache == null) BuildCache();

        if (_supportCache.TryGetValue(supportId, out var entry))
        {
            return entry;
        }
        return null;
    }

    /// <summary>
    /// 상태이상 아이콘 프리팹 조회
    /// </summary>
    public GameObject GetStatusIcon(int supportId, MarkType markType = MarkType.None, CCType ccType = CCType.None)
    {
        var entry = GetSupportEntry(supportId);
        if (entry == null) return null;

        return entry.GetIconPrefab(entry.effectType, markType, ccType);
    }

    /// <summary>
    /// 상태이상 적용 이펙트 프리팹 조회
    /// </summary>
    public GameObject GetApplyEffect(int supportId)
    {
        return GetSupportEntry(supportId)?.applyEffectPrefab;
    }

    /// <summary>
    /// DOT 틱 이펙트 프리팹 조회
    /// </summary>
    public GameObject GetDOTTickEffect(int supportId)
    {
        return GetSupportEntry(supportId)?.dotTickEffectPrefab;
    }

    /// <summary>
    /// 아이콘 Y 오프셋 조회
    /// </summary>
    public float GetIconYOffset(int supportId)
    {
        var entry = GetSupportEntry(supportId);
        return entry?.iconYOffset ?? 2f;
    }

    /// <summary>
    /// 아이콘 스케일 조회
    /// </summary>
    public float GetIconScale(int supportId)
    {
        var entry = GetSupportEntry(supportId);
        return entry?.iconScale ?? 1f;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 이펙트가 연결되지 않은 스킬 ID 목록 반환
    /// </summary>
    public List<int> GetUnmappedSkillIds()
    {
        var unmapped = new List<int>();

        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            return unmapped;

        var table = CSVLoader.Instance.GetTable<MainSkillData>();
        if (table == null) return unmapped;

        foreach (var skill in table.GetAll())
        {
            var entry = GetEntry(skill.skill_id);
            if (entry == null || !entry.HasMainEffect())
            {
                unmapped.Add(skill.skill_id);
            }
        }

        return unmapped;
    }

    /// <summary>
    /// 특정 타입의 스킬 이펙트 목록 반환
    /// </summary>
    public List<SkillEffectEntry> GetEntriesByType(SkillAssetType type)
    {
        var result = new List<SkillEffectEntry>();
        foreach (var entry in entries)
        {
            if (entry.skillType == type)
            {
                result.Add(entry);
            }
        }
        return result;
    }

    /// <summary>
    /// 엔트리 추가 또는 업데이트
    /// </summary>
    public void AddOrUpdateEntry(SkillEffectEntry newEntry)
    {
        if (_cache == null) BuildCache();

        // 기존 엔트리 찾기
        var existingIndex = entries.FindIndex(e => e.skillId == newEntry.skillId);
        if (existingIndex >= 0)
        {
            entries[existingIndex] = newEntry;
        }
        else
        {
            entries.Add(newEntry);
        }

        _cache[newEntry.skillId] = newEntry;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// 엔트리 제거
    /// </summary>
    public void RemoveEntry(int skillId)
    {
        if (_cache == null) BuildCache();

        entries.RemoveAll(e => e.skillId == skillId);
        _cache.Remove(skillId);

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// CSV 데이터로 엔트리 목록 동기화 (이름, 타입 등)
    /// </summary>
    public void SyncWithCSV()
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            Debug.LogWarning("[SkillEffectDatabase] CSVLoader not initialized");
            return;
        }

        var table = CSVLoader.Instance.GetTable<MainSkillData>();
        if (table == null) return;

        foreach (var entry in entries)
        {
            var skillData = table.GetId(entry.skillId);
            if (skillData != null)
            {
                entry.skillName = skillData.skill_name;
                entry.skillType = skillData.GetSkillType();
            }
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    #endregion
}
