using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using System.IO;
#endif


public class CSVLoader : MonoBehaviour
{
    public static CSVLoader Instance { get; private set; }
    public bool IsInit { get; private set; }

    /// <summary>
    /// Generic static class for storing tables by type
    /// </summary>
    private static class TableHolder<T> where T : class
    {
        public static CsvTable<T> Table;
    }

    // CSV 파일 경로 (에디터용)
    private const string CSV_PATH = "Assets/Data/CSV";

    // 리로드 이벤트
    public static event System.Action OnCSVReloaded;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async UniTaskVoid Start()
    {
        await LoadAll();
    }

    /// <summary>
    /// Preload all CSV data into memory in parallel
    /// </summary>
    private async UniTask LoadAll()
    {
        Debug.Log("[CSVLoader] Loading all CSV data in parallel...");

        try
        {
            // Load all tables in parallel
            await UniTask.WhenAll(
                // JML: Register new CSV tables here
                RegisterTableAsync<BookmarkData>(AddressableKey.BookmarkTable, x => x.Bookmark_ID),
                RegisterTableAsync<BookmarkCraftData>(AddressableKey.BookmarkCraftTable, x => x.Recipe_ID),
                RegisterTableAsync<BookmarkOptionData>(AddressableKey.BookmarkOptionTable, x => x.Option_ID),
                RegisterTableAsync<BookmarkListData>(AddressableKey.BookmarkListTable, x => x.List_ID),
                RegisterTableAsync<GradeData>(AddressableKey.GradeTable, x => x.Grade_ID),
                RegisterTableAsync<CurrencyData>(AddressableKey.CurrencyTable, x => x.Currency_ID),
                RegisterTableAsync<IngredientData>(AddressableKey.IngredientTable, x => x.Ingredient_ID),
                RegisterTableAsync<CharacterData>(AddressableKey.CharacterTable, x => x.Character_ID),
                RegisterTableAsync<LevelData>(AddressableKey.LevelTable, x => x.Cha_Level_ID),
                RegisterTableAsync<SkillData>(AddressableKey.SkillTable, x => x.Skill_ID),
                RegisterTableAsync<EnhancementLevelData>(AddressableKey.EnhancementLevelTable, x => x.Pw_Level),
                RegisterTableAsync<CharacterEnhancementData>(AddressableKey.CharacterEnhancementTable, x => x.Character_PwUp_ID),
                RegisterTableAsync<StringTable>(AddressableKey.StringTable, x => x.Text_ID),
                RegisterTableAsync<StageData>(AddressableKey.StageTable, x => x.Stage_ID),
                RegisterTableAsync<WaveData>(AddressableKey.WaveTable, x => x.Wave_ID),
                RegisterTableAsync<MonsterLevelData>(AddressableKey.MonsterLevelTable, x => x.Mon_Level_ID),
                RegisterTableAsync<MonsterData>(AddressableKey.MonsterTable, x => x.Monster_ID),
                RegisterTableAsync<RewardData>(AddressableKey.RewardTable, x => x.Reward_ID),
                RegisterTableAsync<RewardGroupData>(AddressableKey.RewardGroupTable, x => x.Reward_Group_ID),
                RegisterTableAsync<DispatchCategoryData>(AddressableKey.DispatchCategoryTable, x => x.Dispatch_ID),
                RegisterTableAsync<DispatchLocationData>(AddressableKey.DispatchLocationTable, x => x.Dispatch_Location_ID),
                RegisterTableAsync<DispatchTimeTableData>(AddressableKey.DispatchTimeTable, x => x.Dispatch_Time_ID),
                RegisterTableAsync<DispatchRewardTableData>(AddressableKey.DispatchRewardTable, x => x.Dispatch_Reward_ID),


                // 새 스킬 테이블 (3행 헤더 형식)
                RegisterSkillTableAsync<MainSkillData>(AddressableKey.MainSkillTable, "MainSkillTable.csv", x => x.skill_id),
                RegisterSkillTableAsync<SupportSkillData>(AddressableKey.SupportSkillTable, "SupportSkillTable.csv", x => x.support_id),
                RegisterSkillTableAsync<SupportCompatibilityData>(AddressableKey.SupportCompatibilityTable, "SupportCompatibilityTable.csv", x => x.support_id),
                RegisterSkillTableAsync<SkillLevelData>(AddressableKey.SkillLevelTable, "SkillLevelTable.csv", x => x.GetCompositeKey()),

                // 인게임 카드 시스템 테이블 (3행 헤더 형식)
                RegisterSkillTableAsync<CardData>(AddressableKey.CardTable, "CardTable.csv", x => x.Card_ID),
                RegisterSkillTableAsync<CardLevelData>(AddressableKey.CardLevelTable, "CardLevelTable.csv", x => x.Card_Level_ID),
                RegisterSkillTableAsync<CardListData>(AddressableKey.CardListTable, "CardListTable.csv", x => x.Card_List_ID),
                RegisterSkillTableAsync<PlayerLevelData>(AddressableKey.PlayerLevelTable, "PlayerLevelTable.csv", x => x.Level_ID)
            );

            IsInit = true;
            Debug.Log("[CSVLoader] All CSV data loaded successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVLoader] Failed to load CSV data: {e.Message}");
            IsInit = false;
        }
    }

    /// <summary>
    /// Register table (internal helper method)
    /// </summary>
    private async UniTask RegisterTableAsync<T>(string addressableKey, Func<T, int> idSelector) where T : class
    {
        var table = await LoadTableAsync<T>(addressableKey, idSelector);
        if (table != null)
        {
            TableHolder<T>.Table = table; // Store directly in type-specific static field
        }
    }

    /// <summary>
    /// Register skill table with 3-row header format (한글헤더/영문헤더/타입)
    /// 에디터: 파일 직접 읽기 (즉시 반영)
    /// 빌드: Addressables 사용
    /// </summary>
    private async UniTask RegisterSkillTableAsync<T>(string addressableKey, string fileName, Func<T, int> idSelector) where T : class
    {
        var table = await LoadSkillTableAsync<T>(addressableKey, fileName, idSelector);
        if (table != null)
        {
            TableHolder<T>.Table = table;
        }
    }

    /// <summary>
    /// Load skill CSV table with 3-row header format
    /// </summary>
    private async UniTask<CsvTable<T>> LoadSkillTableAsync<T>(string addressableKey, string fileName, Func<T, int> idSelector) where T : class
    {
        Debug.Log($"[CSVLoader] Loading skill table: {addressableKey}");

        try
        {
            string csvText = null;

#if UNITY_EDITOR
            // 에디터: 파일 직접 읽기 (CSV 수정 시 즉시 반영)
            string filePath = $"{CSV_PATH}/{fileName}";
            if (File.Exists(filePath))
            {
                csvText = File.ReadAllText(filePath);
                Debug.Log($"[CSVLoader] Editor mode: Direct file read from {filePath}");
            }
            else
            {
                Debug.LogWarning($"[CSVLoader] File not found: {filePath}, falling back to Addressables");
            }
#endif

            // 빌드 또는 에디터에서 파일을 못 찾은 경우: Addressables 사용
            if (string.IsNullOrEmpty(csvText))
            {
                TextAsset asset = await Addressables.LoadAssetAsync<TextAsset>(addressableKey);
                if (asset == null)
                {
                    Debug.LogError($"[CSVLoader] Failed to load TextAsset: {addressableKey}");
                    return null;
                }
                csvText = asset.text;
            }

            // 3행 헤더 형식 처리: 첫번째(한글헤더)와 세번째(타입) 행 제거
            csvText = ProcessSkillCsvFormat(csvText);

            // N/A 처리
            csvText = System.Text.RegularExpressions.Regex.Replace(csvText, @",N/A(?=[,\r\n]|$)", ",0");

            // Create and load CsvTable
            var table = new CsvTable<T>(idSelector);
            table.LoadFromText(csvText);

            Debug.Log($"[CSVLoader] Skill table loaded: {addressableKey} ({table.Count} rows)");
            return table;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVLoader] Error loading skill table {addressableKey}: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// 3행 헤더 CSV 형식 처리
    /// 1행: 한글 헤더 (제거)
    /// 2행: 영문 헤더 (유지 - CsvHelper가 사용)
    /// 3행: 타입 정의 (제거)
    /// 4행~: 데이터 (유지)
    /// </summary>
    private string ProcessSkillCsvFormat(string csvText)
    {
        var lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();

        if (lines.Count < 4)
        {
            Debug.LogWarning("[CSVLoader] CSV has less than 4 lines, returning as-is");
            return csvText;
        }

        // 1행(한글헤더)와 3행(타입) 제거, 2행(영문헤더)와 4행~(데이터) 유지
        var processedLines = new List<string>();
        processedLines.Add(lines[1]); // 영문 헤더 (index 1)

        for (int i = 3; i < lines.Count; i++) // 데이터 (index 3부터)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                processedLines.Add(lines[i]);
            }
        }

        return string.Join("\n", processedLines);
    }

    /// <summary>
    /// Load CSV table (internal helper method)
    /// 에디터: 파일 직접 읽기 (즉시 반영)
    /// 빌드: Addressables 사용
    /// </summary>
    private async UniTask<CsvTable<T>> LoadTableAsync<T>(string addressableKey, Func<T, int> idSelector) where T : class
    {
        Debug.Log($"[CSVLoader] Loading table: {addressableKey}");

        try
        {
            string csvText = null;

#if UNITY_EDITOR
            // 에디터: CSV 파일 직접 읽기 (CSV 수정 시 즉시 반영)
            string fileName = addressableKey + ".csv";
            string filePath = $"{CSV_PATH}/{fileName}";
            if (File.Exists(filePath))
            {
                csvText = File.ReadAllText(filePath);
                Debug.Log($"[CSVLoader] Editor mode: Direct file read from {filePath}");
            }
            else
            {
                Debug.LogWarning($"[CSVLoader] File not found: {filePath}, falling back to Addressables");
            }
#endif

            // 빌드 또는 에디터에서 파일을 못 찾은 경우: Addressables 사용
            if (string.IsNullOrEmpty(csvText))
            {
                TextAsset asset = await Addressables.LoadAssetAsync<TextAsset>(addressableKey);
                if (asset == null)
                {
                    Debug.LogError($"[CSVLoader] Failed to load TextAsset: {addressableKey}");
                    return null;
                }
                csvText = asset.text;
            }

            // Create and load CsvTable
            var table = new CsvTable<T>(idSelector);
            csvText = System.Text.RegularExpressions.Regex.Replace(csvText, @",N/A(?=[,\r\n]|$)", ",0");
            table.LoadFromText(csvText);

            Debug.Log($"[CSVLoader] Table loaded: {addressableKey} ({table.Count} rows)");
            return table;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVLoader] Error loading table {addressableKey}: {e.Message}");
            return null;
        }
    }


    public CsvTable<T> GetTable<T>() where T : class
    {
        return TableHolder<T>.Table; // Direct access, no casting!
    }


    public T GetData<T>(int id) where T : class
    {
        return TableHolder<T>.Table?.GetId(id);
    }

    /// <summary>
    /// 스킬 레벨 데이터 조회 (skill_id + level 조합)
    /// </summary>
    public SkillLevelData GetSkillLevelData(int skillId, int level)
    {
        int compositeKey = skillId * 100 + level;
        return GetData<SkillLevelData>(compositeKey);
    }

    /// <summary>
    /// 특정 스킬의 모든 레벨 데이터 조회
    /// </summary>
    public List<SkillLevelData> GetAllSkillLevels(int skillId)
    {
        var table = GetTable<SkillLevelData>();
        if (table == null) return new List<SkillLevelData>();

        return table.FindAll(x => x.skill_id == skillId);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 스킬 테이블만 다시 로드 (CSV 수정 후 즉시 반영용)
    /// </summary>
    public async UniTask ReloadSkillTablesAsync()
    {
        Debug.Log("[CSVLoader] Reloading skill tables...");

        await UniTask.WhenAll(
            RegisterSkillTableAsync<MainSkillData>(AddressableKey.MainSkillTable, "MainSkillTable.csv", x => x.skill_id),
            RegisterSkillTableAsync<SupportSkillData>(AddressableKey.SupportSkillTable, "SupportSkillTable.csv", x => x.support_id),
            RegisterSkillTableAsync<SkillLevelData>(AddressableKey.SkillLevelTable, "SkillLevelTable.csv", x => x.GetCompositeKey())
        );

        Debug.Log("[CSVLoader] Skill tables reloaded!");
        OnCSVReloaded?.Invoke();
    }

    /// <summary>
    /// 에디터에서 모든 CSV 테이블 다시 로드 (밸런싱 도구용)
    /// </summary>
    public async UniTask ReloadAllTablesAsync()
    {
        Debug.Log("[CSVLoader] Reloading ALL CSV tables...");

        try
        {
            await UniTask.WhenAll(
                // 표준 테이블
                RegisterTableAsync<BookmarkData>(AddressableKey.BookmarkTable, x => x.Bookmark_ID),
                RegisterTableAsync<BookmarkCraftData>(AddressableKey.BookmarkCraftTable, x => x.Recipe_ID),
                RegisterTableAsync<BookmarkOptionData>(AddressableKey.BookmarkOptionTable, x => x.Option_ID),
                RegisterTableAsync<BookmarkListData>(AddressableKey.BookmarkListTable, x => x.List_ID),
                RegisterTableAsync<GradeData>(AddressableKey.GradeTable, x => x.Grade_ID),
                RegisterTableAsync<CurrencyData>(AddressableKey.CurrencyTable, x => x.Currency_ID),
                RegisterTableAsync<IngredientData>(AddressableKey.IngredientTable, x => x.Ingredient_ID),
                RegisterTableAsync<CharacterData>(AddressableKey.CharacterTable, x => x.Character_ID),
                RegisterTableAsync<LevelData>(AddressableKey.LevelTable, x => x.Cha_Level_ID),
                RegisterTableAsync<SkillData>(AddressableKey.SkillTable, x => x.Skill_ID),
                RegisterTableAsync<EnhancementLevelData>(AddressableKey.EnhancementLevelTable, x => x.Pw_Level),
                RegisterTableAsync<CharacterEnhancementData>(AddressableKey.CharacterEnhancementTable, x => x.Character_PwUp_ID),
                RegisterTableAsync<StringTable>(AddressableKey.StringTable, x => x.Text_ID),
                RegisterTableAsync<StageData>(AddressableKey.StageTable, x => x.Stage_ID),
                RegisterTableAsync<WaveData>(AddressableKey.WaveTable, x => x.Wave_ID),
                RegisterTableAsync<MonsterLevelData>(AddressableKey.MonsterLevelTable, x => x.Mon_Level_ID),
                RegisterTableAsync<MonsterData>(AddressableKey.MonsterTable, x => x.Monster_ID),
                RegisterTableAsync<RewardData>(AddressableKey.RewardTable, x => x.Reward_ID),
                RegisterTableAsync<RewardGroupData>(AddressableKey.RewardGroupTable, x => x.Reward_Group_ID),
                RegisterTableAsync<DispatchCategoryData>(AddressableKey.DispatchCategoryTable, x => x.Dispatch_ID),
                RegisterTableAsync<DispatchLocationData>(AddressableKey.DispatchLocationTable, x => x.Dispatch_Location_ID),
                RegisterTableAsync<DispatchTimeTableData>(AddressableKey.DispatchTimeTable, x => x.Dispatch_Time_ID),
                RegisterTableAsync<DispatchRewardTableData>(AddressableKey.DispatchRewardTable, x => x.Dispatch_Reward_ID),

                // 스킬 테이블 (3행 헤더)
                RegisterSkillTableAsync<MainSkillData>(AddressableKey.MainSkillTable, "MainSkillTable.csv", x => x.skill_id),
                RegisterSkillTableAsync<SupportSkillData>(AddressableKey.SupportSkillTable, "SupportSkillTable.csv", x => x.support_id),
                RegisterSkillTableAsync<SupportCompatibilityData>(AddressableKey.SupportCompatibilityTable, "SupportCompatibilityTable.csv", x => x.support_id),
                RegisterSkillTableAsync<SkillLevelData>(AddressableKey.SkillLevelTable, "SkillLevelTable.csv", x => x.GetCompositeKey()),

                // 인게임 카드 시스템 테이블 (3행 헤더)
                RegisterSkillTableAsync<CardData>(AddressableKey.CardTable, "CardTable.csv", x => x.Card_ID),
                RegisterSkillTableAsync<CardLevelData>(AddressableKey.CardLevelTable, "CardLevelTable.csv", x => x.Card_Level_ID),
                RegisterSkillTableAsync<CardListData>(AddressableKey.CardListTable, "CardListTable.csv", x => x.Card_List_ID),
                RegisterSkillTableAsync<PlayerLevelData>(AddressableKey.PlayerLevelTable, "PlayerLevelTable.csv", x => x.Level_ID)
            );

            Debug.Log("[CSVLoader] All CSV tables reloaded successfully!");
            OnCSVReloaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVLoader] Failed to reload tables: {e.Message}");
        }
    }
#endif
}
