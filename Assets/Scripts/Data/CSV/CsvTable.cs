using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generic CSV Table (ScriptableObject removed)
/// Supports fast ID lookup using Dictionary
/// </summary>
public class CsvTable<T> where T : class
{
    private List<T> dataList;
    private Dictionary<int, T> dataMap;
    private Func<T, int> idSelector;

    public List<T> DataList => dataList;
    public int Count => dataList?.Count ?? 0;

    public CsvTable(Func<T, int> idSelector = null)
    {
        this.idSelector = idSelector;
    }

    public void LoadFromText(string csvText)
    {
        dataList = CsvUtility.LoadCsvFromText<T>(csvText);
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        if (idSelector != null && dataList != null)
        {
            dataMap = dataList.ToDictionary(idSelector);
        }
    }

    /// <summary>
    /// Get data by ID (synchronous - immediate return)
    /// </summary>
    public T GetId(int id)
    {
        if (dataMap == null)
        {
            UnityEngine.Debug.LogError($"[CsvTable] Dictionary not initialized.");
            return null;
        }

        if (dataMap.TryGetValue(id, out var data))
        {
            return data;
        }

        UnityEngine.Debug.LogError($"[CsvTable<{typeof(T).Name}>] Data with ID {id} not found.");
        return null;
    }

    /// <summary>
    /// Find first data matching condition
    /// </summary>
    public T Find(Func<T, bool> predicate)
    {
        return dataList?.FirstOrDefault(predicate);
    }

    /// <summary>
    /// Find all data matching condition
    /// </summary>
    public List<T> FindAll(Func<T, bool> predicate)
    {
        return dataList?.Where(predicate).ToList();
    }

    /// <summary>
    /// Get all data
    /// </summary>
    public List<T> GetAll()
    {
        return dataList;
    }
}
