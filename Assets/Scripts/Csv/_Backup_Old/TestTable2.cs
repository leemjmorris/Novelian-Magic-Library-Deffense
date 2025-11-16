using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "TestTable2", menuName = "CSV/TestTable2")]
public class TestTable2 : BaseCsvTable<TestData2>
{
    private Dictionary<int, TestData2> dataMap;

    /// <summary>
    /// JML: Load CSV Data from String
    /// </summary>
    public override void LoadFromString(string csvText)
    {
        dataList = CsvUtility.LoadCsvFromText<TestData2>(csvText);
        BuildDictionary();
    }

    /// <summary>
    /// JML: Load CSV Data from String
    /// </summary>
    public override string ToCSV()
    {
        return CsvUtility.SaveCsv(dataList);
    }


    /// <summary>
    /// JML: Load CSV Data from Addressable
    /// </summary>
    /// <param name="key">Addressable Key</param>
    public async UniTask LoadDataAsync(string key)
    {
        TextAsset asset = await Addressables.LoadAssetAsync<TextAsset>(key);
        LoadFromString(asset.text);
    }

    /// <summary>
    /// JML: Convert Data to CSV String
    /// </summary>
    private void BuildDictionary()
    {
        dataMap = dataList.ToDictionary(x => x.ID);
    }

    public TestData2 GetId(int id)
    {
        if (dataMap.TryGetValue(id, out var data))
        {
            return data;
        }
        Debug.LogError($"[TestTable] Data with ID {id} not found.");
        return null;
    }
}