using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class IngredientManager : MonoBehaviour
{
    private static IngredientManager instance;
    public static IngredientManager Instance => instance;

    //JML: Key: Item_ID, Value: Count
    private Dictionary<int, int> Ingredients;

    // 재료 변경 이벤트 (ingredientId, newAmount)
    public event Action<int, int> OnIngredientChanged;

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
        if (data == null) return "Unknown";
        return CSVLoader.Instance.GetData<StringTable>(data.Ingredient_Name_ID)?.Text ?? "Unknown";
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
            string ingredientName = CSVLoader.Instance.GetData<StringTable>(ingredientData.Ingredient_Name_ID)?.Text ?? "Unknown";
            Debug.LogWarning($"{ingredientName} 최대 수량 초과! (최대: {maxCount})");
            count = maxCount - currentCount;
        }

        if (Ingredients.ContainsKey(id))
            Ingredients[id] += count;
        else
            Ingredients[id] = count;

        string addedIngredientName = CSVLoader.Instance.GetData<StringTable>(ingredientData.Ingredient_Name_ID)?.Text ?? "Unknown";
        Debug.Log($"{addedIngredientName} {count}개 획득! (보유: {Ingredients[id]}/{maxCount})");

        SaveSingleIngredientToFirebase(id);
        OnIngredientChanged?.Invoke(id, Ingredients[id]);
    }

    public bool RemoveIngredient(int id, int count)
    {
        if (!Ingredients.ContainsKey(id) || Ingredients[id] < count)
        {
            Debug.LogWarning("재료가 부족합니다!");
            return false;
        }

        Ingredients[id] -= count;
        int newAmount = Ingredients[id];
        if (Ingredients[id] == 0)
            Ingredients.Remove(id);

        SaveSingleIngredientToFirebase(id);
        OnIngredientChanged?.Invoke(id, newAmount);
        return true;
    }

    /// <summary>
    /// 단일 재료를 Firebase에 저장
    /// </summary>
    private void SaveSingleIngredientToFirebase(int ingredientId)
    {
        if (FirebaseSaveManager.Instance == null || !FirebaseSaveManager.Instance.IsInitialized)
            return;

        if (FirebaseManager.Instance == null || string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserId))
            return;

        int currentCount = Ingredients.ContainsKey(ingredientId) ? Ingredients[ingredientId] : 0;
        FirebaseSaveManager.Instance.SaveSingleIngredientAsync(
            FirebaseManager.Instance.CurrentUserId,
            ingredientId,
            currentCount
        ).Forget();
    }

    public bool HasIngredient(int id, int count)
    {
        return Ingredients.ContainsKey(id) && Ingredients[id] >= count;
    }

    /// <summary>
    /// Firebase 데이터로 재료 설정 (BootScene에서 호출)
    /// </summary>
    public void SetIngredientsFromFirebase(System.Collections.Generic.Dictionary<string, int> ingredients)
    {
        if (ingredients == null) return;

        Ingredients.Clear();
        foreach (var kvp in ingredients)
        {
            if (int.TryParse(kvp.Key, out int ingredientId))
            {
                Ingredients[ingredientId] = kvp.Value;
            }
        }

        Debug.Log($"<color=#3EB489>[IngredientManager]</color> Firebase에서 재료 로드: {Ingredients.Count}종");
    }

    /// <summary>
    /// 모든 재료 반환 (저장용)
    /// </summary>
    public Dictionary<int, int> GetAllIngredients()
    {
        return new Dictionary<int, int>(Ingredients);
    }
}
