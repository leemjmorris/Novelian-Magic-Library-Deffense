using System.Collections.Generic;
using UnityEngine;

public class IngredientManager : MonoBehaviour
{
    private static IngredientManager instance;
    public static IngredientManager Instance => instance;
    
    //JML: Key: Item_ID, Value: Count
    private Dictionary<int, int> Ingredients;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        Ingredients = new Dictionary<int, int>();
    }
    //JML: ItemTable
    public ItemData GetIngredientInfo(int id)
    {
        return CSVLoader.Instance.GetData<ItemData>(id);
    }

    public string GetIngredientName(int id)
    {
        var data = GetIngredientInfo(id);
        return data?.Item_Name ?? "Unknown";
    }

    public Grade GetIngredientGrade(int id)
    {
        var data = GetIngredientInfo(id);
        return data?.Item_Grade ?? Grade.Common;
    }

    public ItemType GetIngredientType(int id)
    {
        var data = GetIngredientInfo(id);
        return data?.Item_Type ?? ItemType.Material;
    }

    public int GetMaxCount(int id)
    {
        var data = GetIngredientInfo(id);
        return data?.Max_Count ?? 9999;
    }

    public int GetIngredientCount(int id)
    {
        return Ingredients.ContainsKey(id) ? Ingredients[id] : 0;
    }

    public void AddIngredient(int id, int count)
    {

        var itemData = CSVLoader.Instance.GetData<ItemData>(id);
        if (itemData == null)
        {
            Debug.LogError($"존재하지 않는 아이템 ID: {id}");
            return;
        }

        if (itemData.Item_Type != ItemType.Material)
        {
            Debug.LogError($"재료 아이템이 아닙니다: {id} (Type: {itemData.Item_Type})");
            return;
        }

  
        int currentCount = GetIngredientCount(id);
        int maxCount = itemData.Max_Count;
        
        if (currentCount + count > maxCount)
        {
            Debug.LogWarning($"{itemData.Item_Name} 최대 수량 초과! (최대: {maxCount})");
            count = maxCount - currentCount;
        }

        if (Ingredients.ContainsKey(id))
            Ingredients[id] += count;
        else
            Ingredients[id] = count;

        Debug.Log($"{itemData.Item_Name} {count}개 획득! (보유: {Ingredients[id]}/{maxCount})");
    }

    public bool RemoveIngredient(int id, int count)
    {
        if (!Ingredients.ContainsKey(id) || Ingredients[id] < count)
        {
            Debug.LogWarning("재료가 부족합니다!");
            return false;
        }

        Ingredients[id] -= count;
        if (Ingredients[id] == 0)
            Ingredients.Remove(id);

        return true;
    }

    public bool HasIngredient(int id, int count)
    {
        return Ingredients.ContainsKey(id) && Ingredients[id] >= count;
    }
}
