using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookMarkUI : MonoBehaviour
{
    public BookmarkType SelectedBookmarkType { get; private set; } = BookmarkType.None;

    private List<BookmarkCraftData> statRecipes = new List<BookmarkCraftData>();
    private List<BookmarkCraftData> skillRecipes = new List<BookmarkCraftData>();
    private List<CraftSceneBookMarkSlot> bookSlotList = new List<CraftSceneBookMarkSlot>();
    private BookmarkCraftData selectedRecipe = null;

    // JML: 책갈피 등급 아이콘 캐시 (즉시 로딩용)
    private Dictionary<Grade, Sprite> cachedBookmarkIcons = new Dictionary<Grade, Sprite>();

    // CBL: 재료 아이콘 캐시 (Ingredient_ID → Sprite)
    private Dictionary<int, Sprite> cachedIngredientIcons = new Dictionary<int, Sprite>();

    [Header("Choice Panel")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button selectionStatButton;
    [SerializeField] private Button selectionSkillButton;
    [SerializeField] private Button closeChoicePanelButton;
    [SerializeField] private GameObject BookmarkUISlotPrefab;
    [SerializeField] private Transform statBookmarkSlotParent;
    [SerializeField] private GameObject bookMarkInfoPanel;
    [SerializeField] private TMP_Dropdown filterDropdown;

    [Header("Recipe Panel")]
    [SerializeField] private GameObject recipePanel;
    [SerializeField] private GameObject statRecipeLayout;
    [SerializeField] private GameObject skillRecipeLayout;
    [SerializeField] private TextMeshProUGUI recipePanelTitleText;
    [SerializeField] private Button closeRecipePanelButton;
    [SerializeField] private Button[] statRecipeButtons;
    [SerializeField] private Button[] statRecipeTextButtons;
    [SerializeField] private Button[] skillRecipeButtons;
    [SerializeField] private Button[] skillRecipeTextButtons;

    [Header("Stat Craft Panel")]
    [SerializeField] private GameObject craftPanel;
    [SerializeField] private GameObject statCraftPanel;
    [SerializeField] private Image statCraftBookmarkIcon;  // JML: 제작할 책갈피 등급 아이콘
    [SerializeField] private Image statMetrial1IconImage;
    [SerializeField] private TextMeshProUGUI statMetrial1CountText;
    [SerializeField] private Image statMetrial2IconImage;
    [SerializeField] private TextMeshProUGUI statMetrial2CountText;
    [SerializeField] private TextMeshProUGUI statSuccessRateText;
    [SerializeField] private TextMeshProUGUI statGreatSuccessRateText;
    [SerializeField] private TextMeshProUGUI statGoldText;
    [SerializeField] private Button statCraftButton;
    [SerializeField] private Button closeCraftPanelButton;

    [Header("Skill Craft Panel")]
    [SerializeField] private GameObject skillCraftPanel;
    [SerializeField] private Image skillCraftBookmarkIcon;  // JML: 제작할 책갈피 등급 아이콘
    [SerializeField] private Image skillMetrial1IconImage;
    [SerializeField] private TextMeshProUGUI skillMetrial1CountText;
    [SerializeField] private Image skillMetrial2IconImage;
    [SerializeField] private TextMeshProUGUI skillMetrial2CountText;
    [SerializeField] private Image skillMetrial3IconImage;
    [SerializeField] private TextMeshProUGUI skillMetrial3CountText;
    [SerializeField] private TextMeshProUGUI skillSuccessRateText;
    [SerializeField] private TextMeshProUGUI skillGreatSuccessRateText;
    [SerializeField] private TextMeshProUGUI skillGoldText;
    [SerializeField] private Button skillCraftButton;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Image ResultBookMarkImage;
    [SerializeField] private TextMeshProUGUI resultOptionText;


    private async UniTaskVoid Start()
    {
        GameLog.Log("[BookMarkUI] Start() called - Loading recipes...");

        // BookMark BGM 재생 (크로스페이드)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.CrossfadeBGM("BGM_BookMark", 1f);
        }

        await LoadRecipesFromCSV(); // TODO JML: 부트씬 로드하면 필요 없어짐

        GameLog.Log($"[BookMarkUI] Recipes loaded - stat: {statRecipes.Count}, skill: {skillRecipes.Count}");
        GameLog.Log("[BookMarkUI] Preloading bookmark icons...");

        await PreloadBookmarkIcons(); // JML: 책갈피 아이콘 미리 캐싱

        GameLog.Log($"[BookMarkUI] Icons cached: {cachedBookmarkIcons.Count}");
        GameLog.Log("[BookMarkUI] Setting up button listeners...");

        // JML: Choice Panel Button Listeners
        selectionStatButton.onClick.AddListener(OnSelectionStatButtonClicked);
        selectionSkillButton.onClick.AddListener(OnSelectionSkillButtonClicked);
        closeChoicePanelButton.onClick.AddListener(() => OnCloseChicePanelButtonClicked().Forget());

        // JML: Recipe Panel Button Listeners
        closeRecipePanelButton.onClick.AddListener(OnClickCloseRecipePanelButton);

        // JML: Craft Button
        statCraftButton.onClick.AddListener(OnCraftButtonClicked);
        skillCraftButton.onClick.AddListener(OnCraftButtonClicked);

        // JML: Stat Recipe Selection Buttons (아이콘 버튼)
        for (int i = 0; i < statRecipeButtons.Length && i < statRecipes.Count; i++)
        {
            if (statRecipeButtons[i] == null) continue;
            int index = i;
            statRecipeButtons[i].onClick.AddListener(() => OnRecipeSelected(statRecipes[index]));
        }

        // CBL: Stat Recipe Selection Buttons (텍스트 버튼)
        for (int i = 0; i < statRecipeTextButtons.Length && i < statRecipes.Count; i++)
        {
            if (statRecipeTextButtons[i] == null) continue;
            int index = i;
            statRecipeTextButtons[i].onClick.AddListener(() => OnRecipeSelected(statRecipes[index]));
        }

        // JML: Skill Recipe Selection Buttons (아이콘 버튼)
        for (int i = 0; i < skillRecipeButtons.Length && i < skillRecipes.Count; i++)
        {
            if (skillRecipeButtons[i] == null) continue;
            int index = i;
            skillRecipeButtons[i].onClick.AddListener(() => OnRecipeSelected(skillRecipes[index]));
        }

        // CBL: Skill Recipe Selection Buttons (텍스트 버튼)
        for (int i = 0; i < skillRecipeTextButtons.Length && i < skillRecipes.Count; i++)
        {
            if (skillRecipeTextButtons[i] == null) continue;
            int index = i;
            skillRecipeTextButtons[i].onClick.AddListener(() => OnRecipeSelected(skillRecipes[index]));
        }
        // JML: Close Craft Panel Button
        closeCraftPanelButton.onClick.AddListener(OnClickCloseCraftPanelButton);

        // JML: 보유한 책갈피 수량만큼 슬롯 생성
        CreateBookmarkSlots();

        // JML: 책갈피 추가 이벤트 구독 (제작 완료 시 슬롯 동적 추가)
        if (BookMarkManager.Instance != null)
        {
            BookMarkManager.Instance.OnBookmarkAdded += OnBookmarkAdded;
        }

        // JML: 필터 드롭다운 이벤트 연결
        if (filterDropdown != null)
        {
            filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        }

        GameLog.Log("[BookMarkUI] Start() completed - All listeners setup done");
    }

    private void OnDestroy()
    {
        // JML: Remove Choice Panel Button Listeners
        selectionStatButton.onClick.RemoveListener(OnSelectionStatButtonClicked);
        selectionSkillButton.onClick.RemoveListener(OnSelectionSkillButtonClicked);
        closeChoicePanelButton.onClick.RemoveListener(() => OnCloseChicePanelButtonClicked().Forget());

        // JML: Remove Recipe Panel Button Listeners
        closeRecipePanelButton.onClick.RemoveListener(OnClickCloseRecipePanelButton);

        // JML: Remove Craft Button
        statCraftButton.onClick.RemoveListener(OnCraftButtonClicked);
        skillCraftButton.onClick.RemoveListener(OnCraftButtonClicked);

        // JML: Remove Recipe Selection Buttons (아이콘 버튼)
        for (int i = 0; i < statRecipeButtons.Length; i++)
        {
            if (statRecipeButtons[i] == null) continue;
            statRecipeButtons[i].onClick.RemoveAllListeners();
        }

        // CBL: Remove Recipe Selection Buttons (텍스트 버튼)
        for (int i = 0; i < statRecipeTextButtons.Length; i++)
        {
            if (statRecipeTextButtons[i] == null) continue;
            statRecipeTextButtons[i].onClick.RemoveAllListeners();
        }

        for (int i = 0; i < skillRecipeButtons.Length; i++)
        {
            if (skillRecipeButtons[i] == null) continue;
            skillRecipeButtons[i].onClick.RemoveAllListeners();
        }

        // CBL: Remove Skill Recipe Text Buttons
        for (int i = 0; i < skillRecipeTextButtons.Length; i++)
        {
            if (skillRecipeTextButtons[i] == null) continue;
            skillRecipeTextButtons[i].onClick.RemoveAllListeners();
        }

        // JML: 책갈피 추가 이벤트 구독 해제
        if (BookMarkManager.Instance != null)
        {
            BookMarkManager.Instance.OnBookmarkAdded -= OnBookmarkAdded;
        }

        // JML: 필터 드롭다운 이벤트 해제
        if (filterDropdown != null)
        {
            filterDropdown.onValueChanged.RemoveListener(OnFilterChanged);
        }
    }

    #region Choice Panel
    private async UniTaskVoid OnCloseChicePanelButtonClicked()
    {
        await FadeController.Instance.LoadSceneWithFade(SceneName.LobbyScene);
    }

    private void OnSelectionStatButtonClicked()
    {
        choicePanel.SetActive(false);

        recipePanel.SetActive(true);
        statRecipeLayout.SetActive(true);
        skillRecipeLayout.SetActive(false);
        recipePanelTitleText.text = "스탯 책갈피 제작";
        SelectedBookmarkType = BookmarkType.Stat;
    }

    private void OnSelectionSkillButtonClicked()
    {
        choicePanel.SetActive(false);

        recipePanel.SetActive(true);
        statRecipeLayout.SetActive(false);
        skillRecipeLayout.SetActive(true);
        recipePanelTitleText.text = "스킬 책갈피 제작";
        SelectedBookmarkType = BookmarkType.Skill;
    }
    #endregion

    #region Recipe Panel
    private void OnClickCloseRecipePanelButton()
    {
        recipePanel.SetActive(false);
        choicePanel.SetActive(true);
        SelectedBookmarkType = BookmarkType.None;
        selectedRecipe = null;
    }
    private void OnRecipeSelected(BookmarkCraftData recipe)
    {
        selectedRecipe = recipe;

        // JML: 제작할 책갈피 등급 아이콘 (캐시에서 즉시 적용)
        SetCraftBookmarkIcon(recipe);

        switch (SelectedBookmarkType)
        {
            case BookmarkType.Stat:
                // JML: Show craft panel
                recipePanel.SetActive(false);
                craftPanel.SetActive(true);
                statCraftPanel.SetActive(true);
                skillCraftPanel.SetActive(false);
                UpdateStatCraftPanelUI(recipe);
                break;
            case BookmarkType.Skill:
                recipePanel.SetActive(false);
                craftPanel.SetActive(true);
                statCraftPanel.SetActive(false);
                skillCraftPanel.SetActive(true);
                UpdateSkillCraftPanelUI(recipe);
                break;
            default:
                GameLog.LogError("[BookMarkUI] 알 수 없는 책갈피 타입!");
                return;
        }
        // JML: Update UI
        UpdateStatCraftPanelUI(recipe);

        GameLog.Log($"[BookMarkUI] 레시피 선택됨: {CSVLoader.Instance.GetData<StringTable>(recipe.Recipe_Name_ID)?.Text ?? "Unknown"}");
    }


    #endregion

    #region Stat Craft Panel
    private void OnClickCloseCraftPanelButton()
    {
        craftPanel.SetActive(false);
        recipePanel.SetActive(true);
        selectedRecipe = null;
    }

    private void UpdateStatCraftPanelUI(BookmarkCraftData recipe)
    {
        // CBL: Material 1
        if (recipe.Material_1_ID > 0)
        {
            int inventoryCount = IngredientManager.Instance.GetIngredientCount(recipe.Material_1_ID);
            int requiredCount = recipe.Material_1_Count;

            statMetrial1CountText.color = inventoryCount < requiredCount ? Color.red : Color.white;
            statMetrial1CountText.text = $"{inventoryCount}  / {requiredCount}";

            // CBL: 재료 1 아이콘 로드
            SetIngredientIcon(statMetrial1IconImage, recipe.Material_1_ID).Forget();
        }

        // CBL: Material 2
        if (recipe.Material_2_ID > 0)
        {
            int inventoryCount = IngredientManager.Instance.GetIngredientCount(recipe.Material_2_ID);
            int requiredCount = recipe.Material_2_Count;

            statMetrial2CountText.color = inventoryCount < requiredCount ? Color.red : Color.white;
            statMetrial2CountText.text = $"{inventoryCount}  / {requiredCount}";

            // CBL: 재료 2 아이콘 로드
            SetIngredientIcon(statMetrial2IconImage, recipe.Material_2_ID).Forget();
        }

        // JML: Success rates
        statSuccessRateText.text = $"제작 성공 확률: {recipe.Success_Rate * 100}%";
        statGreatSuccessRateText.text = $"제작 대성공 확률: {recipe.Great_Success_Rate * 100}%";

        // JML: Gold cost
        statGoldText.text = $"소모 골드: {recipe.Currency_Count} G";
    }

    private void OnCraftButtonClicked()
    {
        if (selectedRecipe == null)
        {
            GameLog.LogWarning("[BookMarkUI] 선택된 레시피가 없습니다!");
            return;
        }

        GameLog.Log($"[BookMarkUI] 제작 시도: {CSVLoader.Instance.GetData<StringTable>(selectedRecipe.Recipe_Name_ID)?.Text ?? "Unknown"}");

        // JML: Call BookMarkCraft
        BookMarkCraftResult result = BookMarkCraft.CraftBookmark(selectedRecipe.Recipe_ID);

        if (result.IsSuccess)
        {
            GameLog.Log($"[BookMarkUI] 제작 성공! {result.Message}");
            // JML: ResultPanel 표시
            ShowCraftResult(result);
        }
        else
        {
            GameLog.LogWarning($"[BookMarkUI] 제작 실패: {result.Message}");
            // TODO: 실패 메시지 UI 표시
        }
    }
    #endregion
    
    #region Skill Craft Panel
    private void UpdateSkillCraftPanelUI(BookmarkCraftData recipe)
    {
        // CBL: Material 1
        if (recipe.Material_1_ID > 0)
        {
            int inventoryCount = IngredientManager.Instance.GetIngredientCount(recipe.Material_1_ID);
            int requiredCount = recipe.Material_1_Count;

            skillMetrial1CountText.color = inventoryCount < requiredCount ? Color.red : Color.white;
            skillMetrial1CountText.text = $"{inventoryCount}  / {requiredCount}";

            // CBL: 재료 1 아이콘 로드
            SetIngredientIcon(skillMetrial1IconImage, recipe.Material_1_ID).Forget();
        }

        // CBL: Material 2
        if (recipe.Material_2_ID > 0)
        {
            int inventoryCount = IngredientManager.Instance.GetIngredientCount(recipe.Material_2_ID);
            int requiredCount = recipe.Material_2_Count;

            skillMetrial2CountText.color = inventoryCount < requiredCount ? Color.red : Color.white;
            skillMetrial2CountText.text = $"{inventoryCount}  / {requiredCount}";

            // CBL: 재료 2 아이콘 로드
            SetIngredientIcon(skillMetrial2IconImage, recipe.Material_2_ID).Forget();
        }

        // CBL: Material 3
        if (recipe.Material_3_ID > 0)
        {
            int inventoryCount = IngredientManager.Instance.GetIngredientCount(recipe.Material_3_ID);
            int requiredCount = recipe.Material_3_Count;

            skillMetrial3CountText.color = inventoryCount < requiredCount ? Color.red : Color.white;
            skillMetrial3CountText.text = $"{inventoryCount}  / {requiredCount}";

            // CBL: 재료 3 아이콘 로드
            SetIngredientIcon(skillMetrial3IconImage, recipe.Material_3_ID).Forget();
        }

        // JML: Success rates
        skillSuccessRateText.text = $"제작 성공 확률: {recipe.Success_Rate * 100}%";
        skillGreatSuccessRateText.text = $"제작 대성공 확률: {recipe.Great_Success_Rate * 100}%";

        // JML: Gold cost
        skillGoldText.text = $"소모 골드: {recipe.Currency_Count} G";
    }
    #endregion


    #region Utility Methods
    private async UniTask LoadRecipesFromCSV()
    {
        await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);    // TODO: 부트씬에서 로드 하면 필요 없어짐
        // JML: Get table from CSVLoader
        var recipeTable = CSVLoader.Instance.GetTable<BookmarkCraftData>();

        if (recipeTable == null)
        {
            GameLog.LogError("[BookMarkUI] BookmarkCraftData table not found!");
            return;
        }

        // JML: Get all recipes
        List<BookmarkCraftData> allRecipes = recipeTable.GetAll();

        // JML: Filter by Recipe_Type
        for (int i = 0; i < allRecipes.Count; i++)
        {
            if (allRecipes[i].Recipe_Type == BookmarkType.Stat)
            {
                statRecipes.Add(allRecipes[i]);
            }
            else if (allRecipes[i].Recipe_Type == BookmarkType.Skill)
            {
                skillRecipes.Add(allRecipes[i]);
            }
        }

        // JML: Sort by Recipe_ID
        statRecipes.Sort((a, b) => a.Recipe_ID.CompareTo(b.Recipe_ID));
        skillRecipes.Sort((a, b) => a.Recipe_ID.CompareTo(b.Recipe_ID));

        GameLog.Log($"[BookMarkUI] Loaded {statRecipes.Count} stat recipes, {skillRecipes.Count} skill recipes");
    }

    /// <summary>
    /// JML: 보유한 책갈피로 슬롯 생성
    /// </summary>
    private void CreateBookmarkSlots()
    {
        // JML: BookMarkManager에서 실제 보유 책갈피 가져오기
        List<BookMark> ownedBookmarks = BookMarkManager.Instance.GetAllBookmarks();

        for (int i = 0; i < ownedBookmarks.Count; i++)
        {
            BookMark bookMark = ownedBookmarks[i];

            var slot = Instantiate(BookmarkUISlotPrefab, statBookmarkSlotParent);
            var slotComponent = slot.GetComponent<CraftSceneBookMarkSlot>();
            bookSlotList.Add(slotComponent);

            // JML: 아이콘 키 결정 (현재: 하드코딩 / 나중: 테이블에서 조회)
            string categoryKey = GetCategoryIconKey(bookMark.Type);
            string bookmarkKey = GetBookmarkIconKey(bookMark);

            slotComponent.Init(bookMark, categoryKey, bookmarkKey, choicePanel, bookMarkInfoPanel).Forget();
        }

        GameLog.Log($"[BookMarkUI] 보유 책갈피 슬롯 {ownedBookmarks.Count}개 생성 완료");
    }

    /// <summary>
    /// JML: 책갈피 추가 시 슬롯 동적 생성 (이벤트 핸들러)
    /// </summary>
    private void OnBookmarkAdded(BookMark bookMark)
    {
        var slot = Instantiate(BookmarkUISlotPrefab, statBookmarkSlotParent);
        var slotComponent = slot.GetComponent<CraftSceneBookMarkSlot>();
        bookSlotList.Add(slotComponent);

        string categoryKey = GetCategoryIconKey(bookMark.Type);
        string bookmarkKey = GetBookmarkIconKey(bookMark);

        slotComponent.Init(bookMark, categoryKey, bookmarkKey, choicePanel, bookMarkInfoPanel).Forget();

        GameLog.Log($"[BookMarkUI] 새 책갈피 슬롯 추가: {bookMark.Name}");
    }

    /// <summary>
    /// JML: 필터 드롭다운 변경 시 슬롯 필터링
    /// 드롭다운 인덱스: 0=전체, 1=스텟, 2=스킬
    /// </summary>
    private void OnFilterChanged(int index)
    {
        // JML: 드롭다운 인덱스를 BookmarkType으로 변환
        BookmarkType filterType = index switch
        {
            0 => BookmarkType.All,
            1 => BookmarkType.Stat,
            2 => BookmarkType.Skill,
            _ => BookmarkType.All
        };

        // JML: 슬롯들 필터링
        foreach (var slot in bookSlotList)
        {
            if (slot == null) continue;

            if (filterType == BookmarkType.All)
            {
                // 전체 보기: 모든 슬롯 활성화
                slot.gameObject.SetActive(true);
            }
            else
            {
                // 특정 타입만 표시
                bool shouldShow = (slot.BookmarkType == filterType);
                slot.gameObject.SetActive(shouldShow);
            }
        }

        GameLog.Log($"[BookMarkUI] 필터 변경: {filterType}");
    }

    /// <summary>
    /// JML: 카테고리 아이콘 키 반환
    /// TODO: 나중에 IconPathTable에서 조회하도록 변경
    /// </summary>
    private string GetCategoryIconKey(BookmarkType type)
    {
        return type switch
        {
            BookmarkType.Stat => "Stat_Bookmark",
            BookmarkType.Skill => "Skill_Bookmark",
            _ => "Skill_Bookmark"
        };
    }

    /// <summary>
    /// JML: 책갈피 아이콘 키 반환 (등급별 아이콘)
    /// </summary>
    private string GetBookmarkIconKey(BookMark bookMark)
    {
        return AddressableKey.GetBookmarkIconKey(bookMark.Grade);
    }

    /// <summary>
    /// JML: Recipe_ID에서 등급 추출
    /// Stat: 1201-1205 → Common-Mythic
    /// Skill: 1206-1210 → Common-Mythic
    /// </summary>
    private Grade GetGradeFromRecipe(BookmarkCraftData recipe)
    {
        if (recipe.Recipe_Type == BookmarkType.Stat)
        {
            return (Grade)(recipe.Recipe_ID - 1200);
        }
        else // Skill
        {
            return (Grade)(recipe.Recipe_ID - 1205);
        }
    }

    /// <summary>
    /// JML: 모든 등급 책갈피 아이콘 미리 캐싱 (시작 시 로드)
    /// </summary>
    private async UniTask PreloadBookmarkIcons()
    {
        Grade[] allGrades = { Grade.Common, Grade.Rare, Grade.Unique, Grade.Legendary, Grade.Mythic };

        foreach (Grade grade in allGrades)
        {
            string iconKey = AddressableKey.GetBookmarkIconKey(grade);
            var icon = await UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(iconKey).ToUniTask();

            if (icon != null)
            {
                cachedBookmarkIcons[grade] = icon;
            }
        }
    }

    /// <summary>
    /// CBL: 재료 아이콘 로드 (캐시 우선, 없으면 PathTable → Addressable 로드)
    /// Ingredient_ID → IngredientData.Path_ID → PathData.Addressable_Key
    /// </summary>
    private async UniTask<Sprite> LoadIngredientIcon(int ingredientId)
    {
        // 캐시에 있으면 즉시 반환
        if (cachedIngredientIcons.TryGetValue(ingredientId, out Sprite cachedIcon))
        {
            return cachedIcon;
        }

        // CBL: IngredientData에서 Path_ID 조회
        var ingredientData = CSVLoader.Instance.GetData<IngredientData>(ingredientId);
        if (ingredientData == null || ingredientData.Path_ID <= 0)
        {
            GameLog.LogWarning($"[BookMarkUI] IngredientData 없음 또는 Path_ID 없음: {ingredientId}");
            return null;
        }

        // CBL: PathTable에서 Addressable_Key 조회
        var pathData = CSVLoader.Instance.GetData<PathData>(ingredientData.Path_ID);
        if (pathData == null || string.IsNullOrEmpty(pathData.Addressable_Key) || pathData.Addressable_Key == "0")
        {
            GameLog.LogWarning($"[BookMarkUI] PathData 없음 또는 키 없음: Path_ID={ingredientData.Path_ID}");
            return null;
        }

        // CBL: Addressable에서 로드
        string iconKey = pathData.Addressable_Key;
        try
        {
            var icon = await UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(iconKey).ToUniTask();
            if (icon != null)
            {
                cachedIngredientIcons[ingredientId] = icon;
                return icon;
            }
        }
        catch (System.Exception e)
        {
            GameLog.LogWarning($"[BookMarkUI] 재료 아이콘 로드 실패: {iconKey} - {e.Message}");
        }

        return null;
    }

    /// <summary>
    /// CBL: 재료 아이콘 이미지에 적용 (비동기)
    /// </summary>
    private async UniTaskVoid SetIngredientIcon(Image targetImage, int ingredientId)
    {
        if (targetImage == null || ingredientId <= 0) return;

        var icon = await LoadIngredientIcon(ingredientId);
        if (icon != null)
        {
            targetImage.sprite = icon;
        }
    }

    /// <summary>
    /// JML: 제작 패널 책갈피 등급 아이콘 로드 (캐시 사용으로 즉시 적용)
    /// </summary>
    private void SetCraftBookmarkIcon(BookmarkCraftData recipe)
    {
        Grade grade = GetGradeFromRecipe(recipe);

        Image targetImage = (SelectedBookmarkType == BookmarkType.Stat)
            ? statCraftBookmarkIcon
            : skillCraftBookmarkIcon;

        if (targetImage != null && cachedBookmarkIcons.TryGetValue(grade, out Sprite icon))
        {
            targetImage.sprite = icon;
        }
    }
    #endregion

    #region Result Panel
    /// <summary>
    /// JML: 제작 결과 패널 표시
    /// </summary>
    private void ShowCraftResult(BookMarkCraftResult result)
    {
        if (result.CraftedBookmark == null)
        {
            GameLog.LogError("[BookMarkUI] CraftedBookmark가 null입니다!");
            return;
        }

        // BGM 일시적으로 낮추고 제작 결과 효과음 재생
        string sfxName = (result.SuccessType == CraftSuccessType.GreatSuccess)
            ? "CraftingGreatSuccessSFX"
            : "CraftingSuccessSFX";
        DuckBGMForResultSFX(sfxName).Forget();

        // JML: 1. 성공 메시지 설정
        if (result.SuccessType == CraftSuccessType.GreatSuccess)
        {
            resultText.text = "제작 대성공!";
        }
        else
        {
            resultText.text = "제작 성공!";
        }

        // JML: 2. 책갈피 아이콘 (캐시에서 즉시 적용)
        SetResultBookmarkIcon(result.CraftedBookmark);

        // JML: 3. 옵션 정보 텍스트 생성
        resultOptionText.text = GenerateResultOptionText(result.CraftedBookmark);

        // JML: 4. 패널 활성화
        resultPanel.SetActive(true);
    }

    /// <summary>
    /// JML: 결과 패널 책갈피 아이콘 (캐시에서 즉시 적용)
    /// </summary>
    private void SetResultBookmarkIcon(BookMark bookMark)
    {
        if (ResultBookMarkImage != null && cachedBookmarkIcons.TryGetValue(bookMark.Grade, out Sprite icon))
        {
            ResultBookMarkImage.sprite = icon;
        }
    }

    /// <summary>
    /// JML: 결과 옵션 텍스트 생성
    /// </summary>
    private string GenerateResultOptionText(BookMark bookMark)
    {
        string gradeName = bookMark.GetGradeName(bookMark.Grade);

        if (bookMark.Type == BookmarkType.Stat)
        {
            // JML: CSV에서 옵션 이름 가져오기
            string optionName = GetOptionNameFromCSV(bookMark.BookmarkDataID);
            // JML: 소수점 값을 %로 변환 (0.02 → 2%)
            int percentValue = Mathf.RoundToInt(bookMark.OptionValue * 100);
            return $"{gradeName} 등급\n{optionName} +{percentValue}%\n책갈피 제작 성공!";
        }
        else // Skill
        {
            // JML: BookMark.DisplayName으로 스킬 이름 가져오기
            return $"{gradeName} 등급\n{bookMark.DisplayName}\n책갈피 제작 성공!";
        }
    }

    /// <summary>
    /// JML: BookmarkDataID로 CSV에서 옵션 이름 가져오기
    /// BookmarkData → BookmarkOptionData → StringTable 체인 조회
    /// </summary>
    private string GetOptionNameFromCSV(int bookmarkDataID)
    {
        // 1. BookmarkData에서 Option_ID 가져오기
        var bookmarkData = CSVLoader.Instance.GetData<BookmarkData>(bookmarkDataID);
        if (bookmarkData == null || bookmarkData.Option_ID <= 0)
        {
            return "알 수 없음";
        }

        // 2. BookmarkOptionData에서 Option_Name_ID 가져오기
        var optionData = CSVLoader.Instance.GetData<BookmarkOptionData>(bookmarkData.Option_ID);
        if (optionData == null)
        {
            return "알 수 없음";
        }

        // 3. StringTable에서 이름 가져오기
        var stringData = CSVLoader.Instance.GetData<StringTable>(optionData.Option_Name_ID);
        if (stringData == null || string.IsNullOrEmpty(stringData.Text))
        {
            return "알 수 없음";
        }

        return stringData.Text;
    }

    /// <summary>
    /// JML: 결과 패널 닫기
    /// </summary>
    public void OnCloseResultPanel()
    {
        resultPanel.SetActive(false);
    }

    /// <summary>
    /// 결과 효과음 재생 시 BGM 볼륨 일시 감소
    /// </summary>
    private async UniTaskVoid DuckBGMForResultSFX(string sfxName)
    {
        var audioManager = AudioManager.Instance;
        if (audioManager == null) return;

        // 원래 볼륨 저장
        float originalVolume = audioManager.GetBGMVolume();

        // 볼륨 낮추기 (0.2 = 20%)
        audioManager.SetBGMVolume(0.2f);

        // 효과음 재생
        audioManager.PlaySFX(sfxName);

        // 3초 대기 (효과음 재생 시간)
        await UniTask.Delay(3000, ignoreTimeScale: true);

        // 효과음 정지
        audioManager.StopAllSFX();

        // 원래 볼륨으로 복구
        audioManager.SetBGMVolume(originalVolume);
    }
    #endregion
}