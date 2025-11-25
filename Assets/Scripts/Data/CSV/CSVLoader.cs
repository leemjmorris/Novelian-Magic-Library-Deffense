using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;


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
                RegisterTableAsync<CharacterEnhancementData>(AddressableKey.CharacterEnhancementTable, x => x.Character_PwUp_ID)
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
    /// Load CSV table (internal helper method)
    /// </summary>
    private async UniTask<CsvTable<T>> LoadTableAsync<T>(string addressableKey, Func<T, int> idSelector) where T : class
    {
        Debug.Log($"[CSVLoader] Loading table: {addressableKey}");

        try
        {
            // Load TextAsset from Addressables
            TextAsset asset = await Addressables.LoadAssetAsync<TextAsset>(addressableKey);

            if (asset == null)
            {
                Debug.LogError($"[CSVLoader] Failed to load TextAsset: {addressableKey}");
                return null;
            }

            // Create and load CsvTable
            var table = new CsvTable<T>(idSelector);
            string csvText = System.Text.RegularExpressions.Regex.Replace(asset.text, @",N/A(?=[,\r\n]|$)", ",0");
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
}
