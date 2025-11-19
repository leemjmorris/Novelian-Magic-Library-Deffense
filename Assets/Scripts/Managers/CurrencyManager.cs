using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    private static CurrencyManager instance;
    public static CurrencyManager Instance => instance;

    // currency field
    private int gold;
    public int Gold => gold;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        gold = 1000; //JML: Initialize gold to zero at start
        Debug.Log("CurrencyManager initialized with " + gold + " gold.");
    }
    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError("Cannot add negative gold.");
            return;
        }
        gold += amount;
        Debug.Log($"Added {amount} gold. Total gold: {gold}");
    }

    public bool SpendGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError("Cannot spend negative gold.");
            return false;
        }
        if (gold >= amount)
        {
            gold -= amount;
            Debug.Log($"Spent {amount} gold. Remaining gold: {gold}");
            return true;
        }
        else
        {
            Debug.LogWarning("Not enough gold to spend.");
            return false;
        }
    }
}
