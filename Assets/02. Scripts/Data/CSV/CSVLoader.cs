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

#if UNITY_EDITOR
    // Hot Reload 시스템
    private static FileSystemWatcher skillCsvWatcher;
    private static bool pendingReload = false;
    private static float lastReloadTime = 0f;
    private const float RELOAD_DEBOUNCE_TIME = 0.5f; // 중복 이벤트 방지
#endif

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

    // CSV 파일 경로 매핑 (Addressable Key -> 실제 하위 폴더 경로)
    private static readonly Dictionary<string, string> csvPathMap = new Dictionary<string, string>
    {
        // BookMark 폴더
        { AddressableKey.BookmarkTable, "BookMark/BookmarkTable.csv" },
        { AddressableKey.BookmarkCraftTable, "BookMark/BookMarkCraftTable.csv" },
        { AddressableKey.BookmarkOptionTable, "BookMark/BookmarkOptionTable.csv" },
        { AddressableKey.BookmarkStatListTable, "BookMark/BookmarkStatListTable.csv" },
        { AddressableKey.BookmarkSkillListTable, "BookMark/BookmarkSKillListTable.csv" },

        // Character 폴더
        { AddressableKey.CharacterTable, "Character/CharacterTable.csv" },
        { AddressableKey.LevelTable, "Character/LevelTable.csv" },
        { AddressableKey.EnhancementLevelTable, "Character/EnhancementLevelTable.csv" },
        { AddressableKey.CharacterEnhancementTable, "Character/CharacterEnhancementTable.csv" },

        // Monster 폴더
        { AddressableKey.MonsterTable, "Monster/MonsterTable.csv" },
        { AddressableKey.MonsterLevelTable, "Monster/MonsterLevelTable.csv" },

        // Stage 폴더
        { AddressableKey.StageTable, "Stage/StageTable.csv" },
        { AddressableKey.WaveTable, "Stage/WaveTable.csv" },

        // Reward 폴더
        { AddressableKey.RewardTable, "Reward/RewardTable.csv" },
        { AddressableKey.RewardGroupTable, "Reward/RewardGroupTable.csv" },

        // Dispatch 폴더
        { AddressableKey.DispatchCategoryTable, "Dispatch/DispatchCategoryTable.csv" },
        { AddressableKey.DispatchLocationTable, "Dispatch/DispatchLocationTable.csv" },
        { AddressableKey.DispatchTimeTable, "Dispatch/DispatchTimeTable.csv" },
        { AddressableKey.DispatchRewardTable, "Dispatch/DispatchRewardTable.csv" },

        // Party 폴더
        { AddressableKey.PartySynergyTable, "Party/PartySynergyTable.csv" },
        { AddressableKey.PartySynergyEnhancementTable, "Party/PartySynergyEnhancementTable.csv" },
        { AddressableKey.PartySynergyEffectTable, "Party/PartySynergyEffectTable.csv" },

        // BossDungeon 폴더 (Issue #476)
        { AddressableKey.BossDungeonTable, "BossDungeon/BossDungeonTable.csv" },

        // 루트 폴더 (하위 폴더 없음)
        { AddressableKey.GradeTable, "GradeTable.csv" },
        { AddressableKey.CurrencyTable, "CurrencyTable.csv" },
        { AddressableKey.IngredientTable, "IngredientTable.csv" },
        { AddressableKey.StringTable, "StringTable.csv" },
        { AddressableKey.PathTable, "PathTable.csv" },
        { AddressableKey.LayoutPresetTable, "LayoutPresetTable.csv" },
    };

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        InitializeHotReload();
#endif
    }

#if UNITY_EDITOR
    private void InitializeHotReload()
    {
        // 기존 Watcher 정리
        if (skillCsvWatcher != null)
        {
            skillCsvWatcher.EnableRaisingEvents = false;
            skillCsvWatcher.Changed -= OnSkillCsvChanged;
            skillCsvWatcher.Dispose();
            skillCsvWatcher = null;
        }

        string skillCsvPath = Path.Combine(Application.dataPath, "Data/CSV/Skill");

        if (!Directory.Exists(skillCsvPath))
        {
            Debug.LogWarning($"[CSVLoader] Skill CSV 폴더를 찾을 수 없습니다: {skillCsvPath}");
            return;
        }

        try
        {
            skillCsvWatcher = new FileSystemWatcher(skillCsvPath)
            {
                Filter = "*.csv",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                IncludeSubdirectories = false
            };

            skillCsvWatcher.Changed += OnSkillCsvChanged;
            skillCsvWatcher.EnableRaisingEvents = true;

            Debug.Log($"[CSVLoader] Hot Reload 활성화됨 - 감시 경로: {skillCsvPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CSVLoader] FileSystemWatcher 초기화 실패: {ex.Message}");
        }
    }

    private void OnSkillCsvChanged(object sender, FileSystemEventArgs e)
    {
        // 백그라운드 스레드에서 호출됨 - 플래그만 설정
        pendingReload = true;
    }

    private void Update()
    {
        // 메인 스레드에서 리로드 처리
        if (pendingReload && IsInit)
        {
            // 중복 이벤트 방지 (파일 저장 시 여러 번 호출될 수 있음)
            if (Time.realtimeSinceStartup - lastReloadTime < RELOAD_DEBOUNCE_TIME)
            {
                pendingReload = false;
                return;
            }

            lastReloadTime = Time.realtimeSinceStartup;
            pendingReload = false;

            Debug.Log($"[CSVLoader] CSV 변경 감지됨 - Hot Reload 실행");
            ExecuteHotReload().Forget();
        }
    }

    private async UniTaskVoid ExecuteHotReload()
    {
        Debug.Log("[CSVLoader] Hot Reload 실행 중...");
        await ReloadSkillTablesAsync();
        Debug.Log("[CSVLoader] Hot Reload 완료!");
    }

    private void OnDestroy()
    {
        if (skillCsvWatcher != null)
        {
            skillCsvWatcher.EnableRaisingEvents = false;
            skillCsvWatcher.Changed -= OnSkillCsvChanged;
            skillCsvWatcher.Dispose();
            skillCsvWatcher = null;
        }
    }

    private void OnApplicationQuit()
    {
        if (skillCsvWatcher != null)
        {
            skillCsvWatcher.EnableRaisingEvents = false;
            skillCsvWatcher.Changed -= OnSkillCsvChanged;
            skillCsvWatcher.Dispose();
            skillCsvWatcher = null;
        }
    }
#endif

    private async UniTaskVoid Start()
    {
        Debug.Log("[CSVLoader] Start() called - Beginning CSV load...");
        await LoadAll();
        Debug.Log($"[CSVLoader] Start() completed - IsInit: {IsInit}");
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
                RegisterTableAsync<BookmarkStatListData>(AddressableKey.BookmarkStatListTable, x => x.List_ID),
                RegisterTableAsync<BookmarkSkillListData>(AddressableKey.BookmarkSkillListTable, x => x.List_2_ID),
                RegisterTableAsync<GradeData>(AddressableKey.GradeTable, x => x.Grade_ID),
                RegisterTableAsync<CurrencyData>(AddressableKey.CurrencyTable, x => x.Currency_ID),
                RegisterTableAsync<IngredientData>(AddressableKey.IngredientTable, x => x.Ingredient_ID),
                RegisterTableAsync<CharacterData>(AddressableKey.CharacterTable, x => x.Character_ID),
                RegisterTableAsync<LevelData>(AddressableKey.LevelTable, x => x.Cha_Level_ID),
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
                RegisterTableAsync<PathData>(AddressableKey.PathTable, x => x.Addressable_ID),
                RegisterTableAsync<LayoutPresetData>(AddressableKey.LayoutPresetTable, x => x.Layout_ID),  // Issue #420

                // 파티 시너지 테이블
                RegisterTableAsync<PartySynergyData>(AddressableKey.PartySynergyTable, x => x.Party_ID),
                RegisterTableAsync<PartySynergyEnhancementData>(AddressableKey.PartySynergyEnhancementTable, x => x.Party_Lv_ID),
                RegisterTableAsync<PartySynergyEffectData>(AddressableKey.PartySynergyEffectTable, x => x.Party_Effect_ID),

                // 도전던전 테이블 (Issue #476)
                RegisterTableAsync<BossDungeonData>(AddressableKey.BossDungeonTable, x => x.Dungeon_ID),

                // 스킬 테이블 (3행 헤더 형식) - Skill 폴더
                RegisterSkillTableAsync<MainSkillData>(AddressableKey.MainSkillTable, "Skill/MainSkillTable.csv", x => x.skill_id),
                RegisterSkillTableAsync<SupportSkillData>(AddressableKey.SupportSkillTable, "Skill/SupportSkillTable.csv", x => x.support_id),

                // 인게임 카드 시스템 테이블 (3행 헤더 형식) - Card/Stage 폴더
                RegisterSkillTableAsync<CardData>(AddressableKey.CardTable, "Card/CardTable.csv", x => x.Card_ID),
                RegisterSkillTableAsync<CardLevelData>(AddressableKey.CardLevelTable, "Card/CardLevelTable.csv", x => x.Card_Level_ID),
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
                // 경고 없이 Addressables로 fallback
                Debug.Log($"[CSVLoader] File not found at {filePath}, using Addressables");
            }
#endif

            // 빌드 또는 에디터에서 파일을 못 찾은 경우: Addressables 사용
            if (string.IsNullOrEmpty(csvText))
            {
                Debug.Log($"[CSVLoader] Loading skill table via Addressables: {addressableKey}");
                try
                {
                    var handle = Addressables.LoadAssetAsync<TextAsset>(addressableKey);
                    TextAsset asset = await handle;
                    if (asset == null)
                    {
                        Debug.LogError($"[CSVLoader] Failed to load skill TextAsset (null): {addressableKey}");
                        return null;
                    }
                    csvText = asset.text;
                    Debug.Log($"[CSVLoader] Skill Addressables load SUCCESS: {addressableKey} (length: {csvText.Length})");
                }
                catch (System.Exception loadEx)
                {
                    Debug.LogError($"[CSVLoader] Skill Addressables load FAILED: {addressableKey} - {loadEx.Message}");
                    return null;
                }
            }

            // 3행 헤더 형식 처리: 첫번째(한글헤더)와 세번째(타입) 행 제거
            csvText = ProcessSkillCsvFormat(csvText);

            // N/A 및 빈 값 처리
            csvText = System.Text.RegularExpressions.Regex.Replace(csvText, @",N/A(?=[,\r\n]|$)", ",0");
            // 연속된 쉼표 사이의 빈 값을 0으로 변환 (int 필드용)
            csvText = System.Text.RegularExpressions.Regex.Replace(csvText, @",,", ",0,");
            // 한번 더 실행 (연속된 빈 값 처리)
            csvText = System.Text.RegularExpressions.Regex.Replace(csvText, @",,", ",0,");

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
            // 경로 매핑 사용 (하위 폴더 지원)
            string relativePath;
            if (csvPathMap.TryGetValue(addressableKey, out relativePath))
            {
                // 매핑된 경로 사용
            }
            else
            {
                // 매핑이 없으면 기본 경로 (루트 폴더)
                relativePath = addressableKey + ".csv";
            }

            string filePath = $"{CSV_PATH}/{relativePath}";
            if (File.Exists(filePath))
            {
                csvText = File.ReadAllText(filePath);
                Debug.Log($"[CSVLoader] Editor mode: Direct file read from {filePath}");
            }
            else
            {
                // 경고 없이 Addressables로 fallback (빌드 환경과 동일하게 동작)
                Debug.Log($"[CSVLoader] File not found at {filePath}, using Addressables");
            }
#endif

            // 빌드 또는 에디터에서 파일을 못 찾은 경우: Addressables 사용
            if (string.IsNullOrEmpty(csvText))
            {
                Debug.Log($"[CSVLoader] Loading table via Addressables: {addressableKey}");
                try
                {
                    var handle = Addressables.LoadAssetAsync<TextAsset>(addressableKey);
                    TextAsset asset = await handle;
                    if (asset == null)
                    {
                        Debug.LogError($"[CSVLoader] Failed to load TextAsset (null): {addressableKey}");
                        return null;
                    }
                    csvText = asset.text;
                    Debug.Log($"[CSVLoader] Addressables load SUCCESS: {addressableKey} (length: {csvText.Length})");
                }
                catch (System.Exception loadEx)
                {
                    Debug.LogError($"[CSVLoader] Addressables load FAILED: {addressableKey} - {loadEx.Message}");
                    return null;
                }
            }

            // Create and load CsvTable
            var table = new CsvTable<T>(idSelector);
            csvText = System.Text.RegularExpressions.Regex.Replace(csvText, @",N/A(?=[,\r\n]|$)", ",0");
            // 연속된 쉼표 사이의 빈 값을 0으로 변환 (int 필드용)
            csvText = System.Text.RegularExpressions.Regex.Replace(csvText, @",,", ",0,");
            // 한번 더 실행 (연속된 빈 값 처리)
            csvText = System.Text.RegularExpressions.Regex.Replace(csvText, @",,", ",0,");
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

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 스킬 테이블만 다시 로드 (CSV 수정 후 즉시 반영용)
    /// </summary>
    public async UniTask ReloadSkillTablesAsync()
    {
        Debug.Log("[CSVLoader] Reloading skill tables...");

        await UniTask.WhenAll(
            RegisterSkillTableAsync<MainSkillData>(AddressableKey.MainSkillTable, "Skill/MainSkillTable.csv", x => x.skill_id),
            RegisterSkillTableAsync<SupportSkillData>(AddressableKey.SupportSkillTable, "Skill/SupportSkillTable.csv", x => x.support_id)
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
                RegisterTableAsync<BookmarkStatListData>(AddressableKey.BookmarkStatListTable, x => x.List_ID),
                RegisterTableAsync<GradeData>(AddressableKey.GradeTable, x => x.Grade_ID),
                RegisterTableAsync<CurrencyData>(AddressableKey.CurrencyTable, x => x.Currency_ID),
                RegisterTableAsync<IngredientData>(AddressableKey.IngredientTable, x => x.Ingredient_ID),
                RegisterTableAsync<CharacterData>(AddressableKey.CharacterTable, x => x.Character_ID),
                RegisterTableAsync<LevelData>(AddressableKey.LevelTable, x => x.Cha_Level_ID),
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

                // 도전던전 테이블 (Issue #476)
                RegisterTableAsync<BossDungeonData>(AddressableKey.BossDungeonTable, x => x.Dungeon_ID),

                // 스킬 테이블 (3행 헤더) - Skill 폴더
                RegisterSkillTableAsync<MainSkillData>(AddressableKey.MainSkillTable, "Skill/MainSkillTable.csv", x => x.skill_id),
                RegisterSkillTableAsync<SupportSkillData>(AddressableKey.SupportSkillTable, "Skill/SupportSkillTable.csv", x => x.support_id),

                // 인게임 카드 시스템 테이블 (3행 헤더) - Card/Stage 폴더
                RegisterSkillTableAsync<CardData>(AddressableKey.CardTable, "Card/CardTable.csv", x => x.Card_ID),
                RegisterSkillTableAsync<CardLevelData>(AddressableKey.CardLevelTable, "Card/CardLevelTable.csv", x => x.Card_Level_ID),
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
