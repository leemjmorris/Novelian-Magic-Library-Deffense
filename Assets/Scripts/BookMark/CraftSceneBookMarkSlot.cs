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
                bookMarkData.Name,
                description,
                bookMarkData,
                this  // 슬롯 참조 전달
            );
        }
        // BookMarkInfo 사용 (BookMarkCraftScene)
        else if (bookMarkInfo != null)
        {
            bookMarkInfo.OpenInfoPanel(loadedBookmarkSprite, bookMarkData.Name, description);
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
            string optionName = GetOptionTypeName(bookMark.OptionType);
            return $"등급: {gradeName}\n{optionName} +{bookMark.OptionValue}";
        }
        else // Skill
        {
            // JML: CSV에서 스킬 이름 가져오기
            string skillName = GetSkillName(bookMark.SkillID);
            return $"등급: {gradeName}\n{skillName}";
        }
    }

    /// <summary>
    /// JML: 스킬 ID로 스킬 이름 가져오기
    /// </summary>
    private string GetSkillName(int skillID)
    {
        var skillData = CSVLoader.Instance.GetData<SkillData>(skillID);
        if (skillData != null && !string.IsNullOrEmpty(skillData.Skill_Name))
        {
            return skillData.Skill_Name;
        }
        return $"알 수 없는 스킬 ({skillID})";
    }

    /// <summary>
    /// JML: 옵션 타입 이름 반환
    /// TODO: 나중에 CSV 테이블에서 가져오도록 변경
    /// </summary>
    private string GetOptionTypeName(int optionType)
    {
        switch (optionType)
        {
            case 1: return "공격력";
            case 2: return "방어력";
            case 3: return "체력";
            default: return "알 수 없음";
        }
    }
}
