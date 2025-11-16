using Cysharp.Threading.Tasks;
using UnityEngine;

public class CSVManager : MonoBehaviour
{
    public static CSVManager Instance { get; private set; }
    public bool IsInit { get; private set; }
    public TestTable testTable;
    public TestTable2 testTable2;

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

    private async UniTask LoadAll()
    {
        await testTable.LoadDataAsync("Test");
        await testTable2.LoadDataAsync("Test2");
        Debug.Log("[CSVManager] All CSV Data Loaded.");
        IsInit = true;
    }
}
