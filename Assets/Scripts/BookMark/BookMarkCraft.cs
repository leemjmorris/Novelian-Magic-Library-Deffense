using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;

public class BookMarkCraft : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button CraftButton;
    [SerializeField] private Button AddIngredientButton;
    [SerializeField] private Button GetListButton;  //TODO JML: 테스트용 북마크 리스트 확인 버튼 제가 안지워도 본 사람이 있으면 지워주세요
    [SerializeField] private Button BackToLobbyButton;  // 로비로 돌아가기 버튼

    [Header("Ingredient Text")]
    [SerializeField] private TextMeshProUGUI IngredientText;

    //-----------------------------------
    //JML: BookMark List
    private List<BookMark> bookMarks = new List<BookMark>();    //JML: Inventory of bookmarks

    // JML: Result Ids for bookmarks
    private List<int> resultIds = new List<int>();

    // JML: BookmarkItem Data
    private BookmarkItemData bookmarkData;

    private void Start()
    {
        CraftButton.onClick.AddListener(OnClickCraftButton);
        AddIngredientButton.onClick.AddListener(OnClickAddIngredientButton);

        //TODO JML: 테스트용 북마크 리스트 확인 버튼 제가 안지워도 본 사람이 있다면 지워주세요
        GetListButton.onClick.AddListener(OnClickGetListButton);

        // 로비로 돌아가기 버튼 리스너 설정
        if (BackToLobbyButton != null)
        {
            BackToLobbyButton.onClick.AddListener(OnClickBackToLobbyButton);
        }
    } 

    private void OnClickCraftButton()
    {
        CraftBookMark();
        UpdateUI($"이름: {IngredientManager.Instance.GetIngredientName(102113)} 수량:{IngredientManager.Instance.GetIngredientCount(102113)}\n이름:{IngredientManager.Instance.GetIngredientName(102118)} 수량:{IngredientManager.Instance.GetIngredientCount(102118)}");
    }

    private void OnClickAddIngredientButton()
    {
        IngredientManager.Instance.AddIngredient(102113, 1000); //JML: 재료1 추가
        IngredientManager.Instance.AddIngredient(102118, 1000); //JML: 재료2 추가
        UpdateUI($"이름: {IngredientManager.Instance.GetIngredientName(102113)} 수량:{IngredientManager.Instance.GetIngredientCount(102113)}\n이름:{IngredientManager.Instance.GetIngredientName(102118)} 수량:{IngredientManager.Instance.GetIngredientCount(102118)}");
    }

    //TODO JML: 테스트용 북마크 리스트 확인 버튼 제가 안지워도 본 사람이 있다면 지워주세요
    private void OnClickGetListButton()
    {
        GetListBookMark();
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
            AddListBookMark(); //JML: Add a new bookmark to inventory
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
        bookmarkData = CSVLoader.Instance.GetData<BookmarkItemData>(id);
        Debug.Log($"북마크 등급: {bookmarkData.Grade}, 옵션 타입: {bookmarkData.Option_Type}, 옵션 값: {bookmarkData.Option_Value}");
    }

    private void AddListBookMark()
    {
        bookMarks.Add(new BookMark("책갈피", (Grade)bookmarkData.Grade, bookmarkData.Option_Type, bookmarkData.Option_Value)); //JML: Add a new bookmark to inventory
    }

    public void GetListBookMark()
    {
        foreach (var bookmark in bookMarks)
        {
            Debug.Log(bookmark.ToString());
        }
    }

    private void UpdateUI(string text)
    {
        IngredientText.text = text;
    }

    /// <summary>
    /// 로비로 돌아가기 버튼 클릭 이벤트
    /// FadeController를 사용하여 페이드 효과와 함께 LobbyScene으로 이동
    /// </summary>
    private void OnClickBackToLobbyButton()
    {
        Debug.Log("[BookMarkCraft] 로비로 돌아가기 - LobbyScene으로 이동");
        LoadLobbySceneAsync().Forget();
    }

    /// <summary>
    /// LobbyScene 비동기 로드 (페이드 효과 포함)
    /// </summary>
    private async UniTaskVoid LoadLobbySceneAsync()
    {
        await FadeController.Instance.LoadSceneWithFade("LobbyScene");
    }
}
