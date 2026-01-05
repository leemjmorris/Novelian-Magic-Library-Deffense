using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class CraftSceneBookMarkSlot : MonoBehaviour
{
    [SerializeField] private Image categoryIcon;
    [SerializeField] private Image bookMarkIcon;
    [SerializeField] private GameObject equipIcon;
    [SerializeField] private GameObject bookMarkInfoPanel;
    [SerializeField] private GameObject choicePanel;

    // JML: 실제 책갈피 데이터
    private BookMark bookMarkData;
    public BookMark BookMarkData => bookMarkData;  // 외부에서 접근용

    private BookMarkInfo bookMarkInfo;
    private LibraryBookMarkInfoPanel libraryBookMarkInfoPanel;

    // JML: 로드된 스프라이트 캐싱 (Info 패널에 전달용)
    private Sprite loadedBookmarkSprite;

    // JML: 필터링용 타입 프로퍼티
    public BookmarkType BookmarkType => bookMarkData?.Type ?? BookmarkType.None;

    /// <summary>
    /// 슬롯 초기화 - BookMark 데이터와 아이콘 키를 받음
    /// </summary>
    /// <param name="bookMark">실제 책갈피 데이터</param>
    /// <param name="categorySpriteKey">카테고리 아이콘 어드레서블 키</param>
    /// <param name="bookmarkSpriteKey">책갈피 아이콘 어드레서블 키</param>
    /// <param name="choicePanel">선택 패널</param>
    /// <param name="bookMarkInfoPanel">정보 패널</param>
    public async UniTaskVoid Init(
        BookMark bookMark,
        string categorySpriteKey,
        string bookmarkSpriteKey,
        GameObject choicePanel,
        GameObject bookMarkInfoPanel)
    {
        // JML: 장착 아이콘 즉시 설정 (await 전에 먼저 설정하여 깜빡임 방지)
        if (equipIcon != null)
        {
            bool isEquipped = bookMark != null && bookMark.IsEquipped;
            equipIcon.SetActive(isEquipped);
            Debug.Log($"[CraftSceneBookMarkSlot] Init - 책갈피: {bookMark?.Name}, IsEquipped: {bookMark?.IsEquipped}, equipIcon 활성화: {isEquipped}");
        }

        // JML: 데이터 저장
        this.bookMarkData = bookMark;
        this.choicePanel = choicePanel;
        this.bookMarkInfoPanel = bookMarkInfoPanel;

        // JML: 패널 타입에 따라 컴포넌트 가져오기
        if (bookMarkInfoPanel != null)
        {
            this.bookMarkInfo = bookMarkInfoPanel.GetComponent<BookMarkInfo>();
            this.libraryBookMarkInfoPanel = bookMarkInfoPanel.GetComponent<LibraryBookMarkInfoPanel>();
        }

        // JML: 어드레서블로 스프라이트 로드
        Sprite categorySprite = await Addressables.LoadAssetAsync<Sprite>(categorySpriteKey).ToUniTask();
        Sprite bookmarkSprite = await Addressables.LoadAssetAsync<Sprite>(bookmarkSpriteKey).ToUniTask();

        // JML: Info 패널에 전달할 스프라이트 캐싱
        loadedBookmarkSprite = bookmarkSprite;

        SetIcons(categorySprite, bookmarkSprite);
    }

    private void SetIcons(Sprite categorySprite, Sprite bookmarkSprite)
    {
        if (categoryIcon != null)
            categoryIcon.sprite = categorySprite;
        if (bookMarkIcon != null)
            bookMarkIcon.sprite = bookmarkSprite;
    }

    /// <summary>
    /// 장착 아이콘 활성/비활성화
    /// </summary>
    public void SetEquipIconActive(bool active)
    {
        if (equipIcon != null)
        {
            equipIcon.SetActive(active);
        }
    }

    public void OnClickSlot()
    {
        Debug.Log("[CraftSceneBookMarkSlot] Slot clicked!");

        if (bookMarkData == null)
        {
            // 데이터 없으면 패널만 활성화 (기존 동작)
            if (bookMarkInfoPanel != null)
                bookMarkInfoPanel.SetActive(true);
            return;
        }

        string description = GenerateDescription(bookMarkData);

        // LibraryBookMarkInfoPanel 사용 (LibraryManagementScene)
        if (libraryBookMarkInfoPanel != null)
        {
            libraryBookMarkInfoPanel.OpenInfoPanel(
                loadedBookmarkSprite,
                bookMarkData.DisplayName,
                description,
                bookMarkData,
                this  // 슬롯 참조 전달
            );
        }
        // BookMarkInfo 사용 (BookMarkCraftScene)
        else if (bookMarkInfo != null)
        {
            bookMarkInfo.OpenInfoPanel(loadedBookmarkSprite, bookMarkData.DisplayName, description);
        }
        else
        {
            // 둘 다 없으면 패널만 활성화
            if (bookMarkInfoPanel != null)
                bookMarkInfoPanel.SetActive(true);
        }
    }

    /// <summary>
    /// JML: 책갈피 타입에 따라 설명 텍스트 생성
    /// </summary>
    private string GenerateDescription(BookMark bookMark)
    {
        string gradeName = bookMark.GetGradeName(bookMark.Grade);

        if (bookMark.Type == BookmarkType.Stat)
        {
            string optionName = GetOptionNameFromCSV(bookMark);
            float displayPercent = bookMark.OptionValue * 100f;
            return $"등급: {gradeName}\n{optionName} +{displayPercent}%";
        }
        else // Skill
        {
            // JML: BookMark.DisplayName으로 스킬 이름 가져오기
            return $"등급: {gradeName}\n{bookMark.DisplayName}";
        }
    }

    /// <summary>
    /// JML: CSV에서 옵션 이름 가져오기 (BookmarkTable → BookmarkOptionTable → StringTable 연동)
    /// </summary>
    private string GetOptionNameFromCSV(BookMark bookMark)
    {
        // 1. BookmarkTable에서 책갈피 데이터 조회 (BookmarkDataID = Bookmark_ID)
        var bookmarkData = CSVLoader.Instance?.GetData<BookmarkData>(bookMark.BookmarkDataID);
        if (bookmarkData == null) return "알 수 없음";

        // 2. BookmarkOptionTable에서 옵션 데이터 조회 (Option_ID)
        var optionData = CSVLoader.Instance?.GetData<BookmarkOptionData>(bookmarkData.Option_ID);
        if (optionData == null) return "알 수 없음";

        // 3. StringTable에서 옵션 이름 조회 (Option_Name_ID)
        var stringData = CSVLoader.Instance?.GetData<StringTable>(optionData.Option_Name_ID);
        return stringData?.Text ?? "알 수 없음";
    }
}
