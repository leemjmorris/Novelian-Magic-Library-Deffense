// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Cysharp.Threading.Tasks;
// using TMPro;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.UI;

// public class BookMarkCraftSys : MonoBehaviour
// {
//     [Header("Test Buttons")]
//     [SerializeField] private Button AddIngredientButton;    //TODO: Remove after testing

//     [Header("UI Panels")]
//     [SerializeField] private GameObject confirmPopup;
//     [SerializeField] private GameObject bookMarkCraftUI;
//     [SerializeField] private GameObject bookMarkRecipeUI;
//     [SerializeField] private GameObject bookMarkResultUI;
//     [SerializeField] private GameObject craftFailUI;
    
//     [Header("Close Button")]
//     [SerializeField] private Button recipeCloseButton;
//     [SerializeField] private Button craftCloseButton;
//     [SerializeField] private Button resultCloseButton;
//     [Header("Buttons")]
//     [SerializeField] private Button confirmYesButton;
//     [SerializeField] private Button confirmNoButton;
//     [SerializeField] private Button craftingButton;
   
//     [Header("Craft Text Fields")]
//     [SerializeField] private TextMeshProUGUI meaterial1NameText;
//     [SerializeField] private TextMeshProUGUI material2NameText;
//     [SerializeField] private TextMeshProUGUI material1CountText;
//     [SerializeField] private TextMeshProUGUI material2CountText;
//     [SerializeField] private TextMeshProUGUI successRateText;
//     [SerializeField] private TextMeshProUGUI greatSuccessRateText;
//     [SerializeField] private TextMeshProUGUI goldCostText; 

//     [Header("Result Text Fields")]
//     [SerializeField] private TextMeshProUGUI resultSuccessText;
//     [SerializeField] private TextMeshProUGUI resultOptionText;
//     [SerializeField] private Image resultBookmarkImage;

//     [Header("Recipe Choice Buttons")]
//     [SerializeField] private Button[] recipeButtons;


//     [Header("Invetory Icon")]
//     [SerializeField] private GameObject inventoryIcon;

//     //-----------------------------------
//     //JML: BookMark List
//     private List<BookMark> bookMarks = new List<BookMark>();    //JML: Inventory of bookmarks

//     // JML: Result Ids for bookmarks
//     private List<int> resultIds = new List<int>();

//     // JML: BookmarkItem Data
//     private BookmarkCraftData bookmarkData; //TODO: Update to BookmarkItemData after CSV update

//     private bool isGreatSuccess = false;    //JML: Flag for great success


//     private void Start()
//     {
//         for (int i = 0; i < recipeButtons.Length; i++)
//         {
//             int index = i; // Capture the current index
//             recipeButtons[i].onClick.AddListener(() => OnClickRecipeButton(index));
//         }
//         recipeCloseButton.onClick.AddListener(() => OnclickCloseButton().Forget());
//         craftCloseButton.onClick.AddListener(OnclickCraftCloseButton);

//         confirmYesButton.onClick.AddListener(OnClickConfirmYesButton);
//         confirmNoButton.onClick.AddListener(OnClickConfirmNoButton);
//         craftingButton.onClick.AddListener(() => OnClickCraftingButton().Forget());

//         AddIngredientButton.onClick.AddListener(OnClickAddIngredientButton); // JML: Test function for adding ingredients

//         confirmPopup.SetActive(false);
//     }

//     // JML: Test functions for adding ingredients and crafting bookmarks
//     private void OnClickAddIngredientButton()
//     {
//         IngredientManager.Instance.AddIngredient(102113, 10); 
//         IngredientManager.Instance.AddIngredient(102118, 10); 
//         CurrencyManager.Instance.AddGold(10000); 
//         UpdateCraftingUI();
//         Debug.Log("재료와 골드가 추가되었습니다.");
//     }

//     private async UniTaskVoid OnclickCloseButton()
//     {
//         await SceneManager.LoadSceneAsync("LobbyScene");
//     }

//     private void OnclickCraftCloseButton()
//     {
//         bookMarkCraftUI.SetActive(false);
//         bookMarkRecipeUI.SetActive(true);
//     }

//     private void OnClickRecipeButton(int index)
//     {
//         if (index != 0) return; //JML: Test only first recipe
        
//         confirmPopup.SetActive(true);
//         Debug.Log("레시피 버튼 " + index + "이 클릭되었습니다.");
//     }

//     private void OnClickConfirmYesButton()
//     {
//         UpdateCraftingUI();
//         confirmPopup.SetActive(false);
//         bookMarkRecipeUI.SetActive(false);
//         bookMarkCraftUI.SetActive(true);
//         Debug.Log("제작 버튼이 클릭되었습니다.");
//     }

//     private async UniTaskVoid OnClickCraftingButton()
//     {
//         if(CraftBookMark())
//         {
//             UpdateResultUI().Forget();
//             bookMarkResultUI.SetActive(true);
//             bookMarkCraftUI.SetActive(false);
//         }
//         else
//         {
//             craftFailUI.SetActive(true);
//             await Task.Delay(2000); //JML: Wait for 2 seconds
//             craftFailUI.SetActive(false);
//         }
//     }

//     private void OnClickConfirmNoButton()
//     {
//         confirmPopup.SetActive(false);
//         Debug.Log("취소 버튼이 클릭되었습니다.");
//     }

//     private bool CraftBookMark()
//     {
//         var BookmarkCraftData = CSVLoader.Instance.GetTable<BookmarkCraftData>().GetId(111);
//         if (BookmarkCraftData != null)
//         {
//             var count1 = BookmarkCraftData.Material_1_Count;
//             var count2 = BookmarkCraftData.Material_2_Count;

//             if (!CheckIngredients(BookmarkCraftData.Material_1_ID, count1) || !CheckIngredients(BookmarkCraftData.Material_2_ID, count2))
//             {
//                 Debug.Log("재료가 부족합니다.");
//                 return false;
//             }

//             if (CurrencyManager.Instance.Gold < BookmarkCraftData.Gold)
//             {
//                 Debug.Log("골드가 부족합니다.");
//                 return false;
//             }

//             if (Random.Range(0f, 1f) < BookmarkCraftData.Success_Rate) //JML: Success Rate 95%
//             {
//                 var resultData = CSVLoader.Instance.GetTable<BookmarkResultData>().GetId(BookmarkCraftData.Result_Grade);
//                 var optionData = CSVLoader.Instance.GetTable<BookmarkOptionData>().GetId(resultData.Option_ID);

//                 resultIds = new List<int> { optionData.Bookmark_1_ID, optionData.Bookmark_2_ID, optionData.Bookmark_3_ID, optionData.Bookmark_4_ID };
//                 int idx = Random.Range(optionData.Min_Value, optionData.Max_Value + 1); 
                
//                 Debug.Log("북마크 제작 성공!");

//                 BookmarkStatUpdate(resultIds[idx]);
//             }
//             else
//             {
//                 isGreatSuccess = true;
//                 var resultData = CSVLoader.Instance.GetTable<BookmarkResultData>().GetId(BookmarkCraftData.Great_Result_Grade);
//                 var optionData = CSVLoader.Instance.GetTable<BookmarkOptionData>().GetId(resultData.Option_ID);
                
//                 resultIds = new List<int> { optionData.Bookmark_1_ID, optionData.Bookmark_2_ID, optionData.Bookmark_3_ID, optionData.Bookmark_4_ID };
//                 int idx = Random.Range(optionData.Min_Value, optionData.Max_Value + 1);

//                 Debug.Log("북마크 제작 대성공");

//                 BookmarkStatUpdate(resultIds[idx]);
//             }
//             IngredientManager.Instance.RemoveIngredient(BookmarkCraftData.Material_1_ID, count1);
//             IngredientManager.Instance.RemoveIngredient(BookmarkCraftData.Material_2_ID, count2);
//             CurrencyManager.Instance.SpendGold(BookmarkCraftData.Gold);
//             AddListBookMark(); //JML: Add a new bookmark to inventory
//             return true;
//         }
//         return false;
//     }

//     private bool CheckIngredients(int materialId, int requiredCount)
//     {
//         int playerCount = IngredientManager.Instance.GetIngredientCount(materialId);
//         return playerCount >= requiredCount;
//     }

//     private void BookmarkStatUpdate(int id)
//     {
//         Debug.Log($"획득한 북마크 ID: {id}");
//         bookmarkData = CSVLoader.Instance.GetData<BookmarkItemData>(id);
//         Debug.Log($"북마크 등급: {bookmarkData.Grade}, 옵션 타입: {bookmarkData.Option_Type}, 옵션 값: {bookmarkData.Option_Value}");
//     }

//     private void AddListBookMark()
//     {
//         var newBookmark = new BookMark("책갈피", (Grade)bookmarkData.Grade, bookmarkData.Option_Type, bookmarkData.Option_Value);
//         bookMarks.Add(newBookmark); //JML: Add a new bookmark to inventory

//         //TODO JML: 2차 빌드 후 삭제 예정 - TempBookMarkManager에 책갈피 저장
//         if (TempBookMarkManager.Instance != null)
//         {
//             TempBookMarkManager.Instance.AddBookmark(newBookmark);
//             Debug.Log("[BookMarkCraftSys] 책갈피가 TempBookMarkManager에 추가되었습니다.");
//         }
//         else
//         {
//             Debug.LogWarning("[BookMarkCraftSys] TempBookMarkManager 인스턴스가 없습니다. Hierarchy에 생성해주세요.");
//         }
//     }

//     public void GetListBookMark()
//     {
//         foreach (var bookmark in bookMarks)
//         {
//             Debug.Log(bookmark.ToString());
//         }
//     }

//     private void UpdateCraftingUI()
//     {
//         //TODO JML: Update the crafting UI with current material counts and gold cost
//         var BookmarkCraftData = CSVLoader.Instance.GetTable<BookmarkCraftData>().GetId(111);
//         meaterial1NameText.text = IngredientManager.Instance.GetIngredientName(BookmarkCraftData.Material_1_ID);
//         material2NameText.text = IngredientManager.Instance.GetIngredientName(BookmarkCraftData.Material_2_ID);

//         successRateText.text = $"{BookmarkCraftData.Success_Rate * 100}%";
//         greatSuccessRateText.text = $"{BookmarkCraftData.Great_Success_Rate * 100}%";

//         material1CountText.text = $"{IngredientManager.Instance.GetIngredientCount(BookmarkCraftData.Material_1_ID)} / {BookmarkCraftData.Material_1_Count}";
//         material2CountText.text = $"{IngredientManager.Instance.GetIngredientCount(BookmarkCraftData.Material_2_ID)} / {BookmarkCraftData.Material_2_Count}";

//         if (IngredientManager.Instance.GetIngredientCount(BookmarkCraftData.Material_1_ID) < BookmarkCraftData.Material_1_Count)
//         {
//             material1CountText.color = Color.red;
//         }
//         else
//         {
//             material1CountText.color = Color.white;
//         }

//         if (IngredientManager.Instance.GetIngredientCount(BookmarkCraftData.Material_2_ID) < BookmarkCraftData.Material_2_Count)
//         {
//             material2CountText.color = Color.red;
//         }
//         else
//         {
//             material2CountText.color = Color.white;
//         }

//         goldCostText.text = $"소모골드: {BookmarkCraftData.Gold} G";
//     }

//     private async UniTaskVoid UpdateResultUI()
//     {
//         //TODO JML: Update the result UI with the crafted bookmark details
//         if (isGreatSuccess)
//         {
//             resultSuccessText.text = "제작 대성공!";
//         }
//         else
//         {
//             resultSuccessText.text = "제작 성공!";
//         }
//         resultOptionText.text = $"노말 책갈피\n옵션 등급: {bookmarkData.Grade}\n공격력: {bookmarkData.Option_Value}";
        
//         await Task.Delay(3000); //JML: Wait a frame for UI update
//         bookMarkResultUI.SetActive(false);
//         bookMarkCraftUI.SetActive(true);
//         isGreatSuccess = false; //JML: Reset great success flag
//     }
// }