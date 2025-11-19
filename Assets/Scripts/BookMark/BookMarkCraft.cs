using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class BookMarkCraft : MonoBehaviour
{
    [Header("Craft Button")]
    [SerializeField] private Button CraftButton;
    [SerializeField] private Button AddIngredientButton;

    [Header("Ingredient Text")]
    [SerializeField] private TextMeshProUGUI IngredientText;
    List<int> resultIds = new List<int>();
    private void Start()
    {
        CraftButton.onClick.AddListener(OnClickCraftButton);
        AddIngredientButton.onClick.AddListener(OnClickAddIngredientButton);
    } 

    private void OnClickCraftButton()
    {
        CraftBookMark();
        UpdateUI($"이름: {IngredientManager.Instance.GetIngredientName(102113)} 수량:{IngredientManager.Instance.GetIngredientCount(102113)}\n이름:{IngredientManager.Instance.GetIngredientName(102118)} 수량:{IngredientManager.Instance.GetIngredientCount(102118)}");
    }
    private void OnClickAddIngredientButton()
    {
        IngredientManager.Instance.AddIngredient(102113, 10); //JML: 재료1 추가
        IngredientManager.Instance.AddIngredient(102118, 10); //JML: 재료2 추가
        UpdateUI($"이름: {IngredientManager.Instance.GetIngredientName(102113)} 수량:{IngredientManager.Instance.GetIngredientCount(102113)}\n이름:{IngredientManager.Instance.GetIngredientName(102118)} 수량:{IngredientManager.Instance.GetIngredientCount(102118)}");
    }
    private void CraftBookMark()
    {
        var BookmarkCraftData = CSVLoader.Instance.GetTable<BookmarkCraftData>().GetId(111);
        if (BookmarkCraftData != null)
        {
            var count1 = BookmarkCraftData.Material_1_Count;
            var count2 = BookmarkCraftData.Material_2_Count;

            if (!CheckIngredients(BookmarkCraftData.Material_1_ID, count1) || !CheckIngredients(BookmarkCraftData.Material_2_ID, count2))
            {
                Debug.Log("재료가 부족합니다.");
                return;
            }

            if (Random.Range(0f, 1f) < BookmarkCraftData.Success_Rate) //JML: Success Rate 95%
            {
                var resultData = CSVLoader.Instance.GetTable<BookmarkResultData>().GetId(BookmarkCraftData.Result_Grade);
                var optionData = CSVLoader.Instance.GetTable<BookmarkOptionData>().GetId(resultData.Option_ID);

                resultIds = new List<int> { optionData.Bookmark_1_ID, optionData.Bookmark_2_ID, optionData.Bookmark_3_ID, optionData.Bookmark_4_ID };
                int idx = Random.Range(optionData.Min_Value, optionData.Max_Value + 1); 
                
                Debug.Log("북마크 제작 성공!");

                BookmarkStatUpdate(resultIds[idx]);
            }
            else
            {
                var resultData = CSVLoader.Instance.GetTable<BookmarkResultData>().GetId(BookmarkCraftData.Great_Result_Grade);
                var optionData = CSVLoader.Instance.GetTable<BookmarkOptionData>().GetId(resultData.Option_ID);
                
                resultIds = new List<int> { optionData.Bookmark_1_ID, optionData.Bookmark_2_ID, optionData.Bookmark_3_ID, optionData.Bookmark_4_ID };
                int idx = Random.Range(optionData.Min_Value, optionData.Max_Value + 1);

                Debug.Log("북마크 제작 대성공");

                BookmarkStatUpdate(resultIds[idx]);
            }
            IngredientManager.Instance.RemoveIngredient(BookmarkCraftData.Material_1_ID, count1);
            IngredientManager.Instance.RemoveIngredient(BookmarkCraftData.Material_2_ID, count2);
        }
    }

    private bool CheckIngredients(int materialId, int requiredCount)
    {
        int playerCount = IngredientManager.Instance.GetIngredientCount(materialId);
        return playerCount >= requiredCount;
    }

    private void BookmarkStatUpdate(int id)
    {
        Debug.Log($"획득한 북마크 ID: {id}");
        var bookmarkData = CSVLoader.Instance.GetData<BookmarkItemData>(id);
        Debug.Log($"북마크 등급: {bookmarkData.Grade}, 옵션 타입: {bookmarkData.Option_Type}, 옵션 값: {bookmarkData.Option_Value}");
    }

    private void UpdateUI(string text)
    {
        IngredientText.text = text;
    }
}
