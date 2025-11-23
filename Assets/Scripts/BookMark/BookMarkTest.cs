using UnityEngine;
using UnityEngine.UI;

public class BookMarkTest : MonoBehaviour
{
    [SerializeField] private Button test;
    [SerializeField] private Button add;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        test.onClick.AddListener(OnClick);
        add.onClick.AddListener(OnAddClick);
    }

    private void OnAddClick()
    {
        IngredientManager.Instance.AddIngredient(1011, 5);
        IngredientManager.Instance.AddIngredient(1016, 3);
        CurrencyManager.Instance.AddGold(5000);
        Debug.Log($"{IngredientManager.Instance.GetIngredientName(1011)} 5개, {IngredientManager.Instance.GetIngredientName(1016)} 3개 추가됨, 보유 골드: {CurrencyManager.Instance.Gold}");
    }
    private void OnClick()
    {
        Debug.Log("BookMarkTest Clicked");

        BookMarkCraftResult result = BookMarkCraft.CraftBookmark(121);
        
        if (result.IsSuccess)
        {
            Debug.Log($"제작 성공! {result.Message}");
            Debug.Log($"북마크: {result.CraftedBookmark.Name}");
        }
        else
        {
            Debug.LogError($"제작 실패! {result.Message}");
        }
    }
}
