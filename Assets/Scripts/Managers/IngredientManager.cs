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
    //JML: IngredientDataTable
    public IngredientData GetIngredientInfo(int id)
    {
        return CSVLoader.Instance.GetData<IngredientData>(id);
    }

    public string GetIngredientName(int id)
    {
        var data = GetIngredientInfo(id);
        return data?.Ingredient_Name ?? "Unknown";
    }

    public int GetIngredientGradeID(int id)
    {
        var data = GetIngredientInfo(id);
        return data?.Grade_ID ?? 151;
    }

    public UseType GetIngredientUseType(int id)
    {
        var data = GetIngredientInfo(id);
        return data?.Use_Type ?? UseType.BookmarkCraft;
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

        var ingredientData = CSVLoader.Instance.GetData<IngredientData>(id);
        if (ingredientData == null)
        {
            Debug.LogError($"존재하지 않는 재료 ID: {id}");
            return;
        }

        int currentCount = GetIngredientCount(id);
        int maxCount = ingredientData.Max_Count;

        if (currentCount + count > maxCount)
        {
            Debug.LogWarning($"{ingredientData.Ingredient_Name} 최대 수량 초과! (최대: {maxCount})");
            count = maxCount - currentCount;
        }

        if (Ingredients.ContainsKey(id))
            Ingredients[id] += count;
        else
            Ingredients[id] = count;

        Debug.Log($"{ingredientData.Ingredient_Name} {count}개 획득! (보유: {Ingredients[id]}/{maxCount})");
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
