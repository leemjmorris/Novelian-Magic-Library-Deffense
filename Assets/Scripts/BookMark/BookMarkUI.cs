using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookMarkUI : MonoBehaviour
{
    public BookmarkType SelectedBookmarkType { get; private set; } = BookmarkType.None;

    private List<BookmarkCraftData> statRecipes = new List<BookmarkCraftData>();
    private List<BookmarkCraftData> skillRecipes = new List<BookmarkCraftData>();
    private BookmarkCraftData selectedRecipe = null;

    [Header("Choice Panel")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button selectionStatButton;
    [SerializeField] private Button selectionSkillButton;
    [SerializeField] private Button closeChoicePanelButton;

    [Header("Recipe Panel")]
    [SerializeField] private GameObject recipePanel;
    [SerializeField] private GameObject statRecipeLayout;
    [SerializeField] private GameObject skillRecipeLayout;
    [SerializeField] private TextMeshProUGUI recipePanelTitleText;
    [SerializeField] private Button closeRecipePanelButton;
    [SerializeField] private Button[] statRecipeButtons;
    [SerializeField] private Button[] skillRecipeButtons;

    [Header("Stat Craft Panel")]
    [SerializeField] private GameObject craftPanel;
    [SerializeField] private GameObject statCraftPanel;
    [SerializeField] private TextMeshProUGUI statMetrial1NameText;
    [SerializeField] private Image statMetrial1IconImage;
    [SerializeField] private TextMeshProUGUI statMetrial1CountText;
    [SerializeField] private TextMeshProUGUI statMetrial2NameText;
    [SerializeField] private Image statMetrial2IconImage;
    [SerializeField] private TextMeshProUGUI statMetrial2CountText;
    [SerializeField] private TextMeshProUGUI statSuccessRateText;
    [SerializeField] private TextMeshProUGUI statGreatSuccessRateText;
    [SerializeField] private TextMeshProUGUI statGoldText;
    [SerializeField] private Button statCraftButton;
    [SerializeField] private Button closeCraftPanelButton;

    [Header("Skill Craft Panel")]
    [SerializeField] private GameObject skillCraftPanel;
    [SerializeField] private TextMeshProUGUI skillMetrial1NameText;
    [SerializeField] private Image skillMetrial1IconImage;
    [SerializeField] private TextMeshProUGUI skillMetrial1CountText;
    [SerializeField] private TextMeshProUGUI skillMetrial2NameText;
    [SerializeField] private Image skillMetrial2IconImage;
    [SerializeField] private TextMeshProUGUI skillMetrial2CountText;
    [SerializeField] private TextMeshProUGUI skillMetrial3NameText;
    [SerializeField] private Image skillMetrial3IconImage;
    [SerializeField] private TextMeshProUGUI skillMetrial3CountText;
    [SerializeField] private TextMeshProUGUI skillSuccessRateText;
    [SerializeField] private TextMeshProUGUI skillGreatSuccessRateText;
    [SerializeField] private TextMeshProUGUI skillGoldText;
    [SerializeField] private Button skillCraftButton;
    


    private async UniTaskVoid Start()
    {
        await LoadRecipesFromCSV(); // TODO JML: 부트씬 로드하면 필요 없어짐

        // JML: Choice Panel Button Listeners
        selectionStatButton.onClick.AddListener(OnSelectionStatButtonClicked);
        selectionSkillButton.onClick.AddListener(OnSelectionSkillButtonClicked);
        closeChoicePanelButton.onClick.AddListener(() => OnCloseChicePanelButtonClicked().Forget());

        // JML: Recipe Panel Button Listeners
        closeRecipePanelButton.onClick.AddListener(OnClickCloseRecipePanelButton);

        // JML: Craft Button
        statCraftButton.onClick.AddListener(OnCraftButtonClicked);
        skillCraftButton.onClick.AddListener(OnCraftButtonClicked);

        // JML: Stat Recipe Selection Buttons
        for (int i = 0; i < statRecipeButtons.Length && i < statRecipes.Count; i++)
        {
            int index = i;
            statRecipeButtons[i].onClick.AddListener(() => OnRecipeSelected(statRecipes[index]));
        }

        // JML: Skill Recipe Selection Buttons
        for (int i = 0; i < skillRecipeButtons.Length && i < skillRecipes.Count; i++)
        {
            int index = i;
            skillRecipeButtons[i].onClick.AddListener(() => OnRecipeSelected(skillRecipes[index]));
        }
        // JML: Close Craft Panel Button
        closeCraftPanelButton.onClick.AddListener(OnClickCloseCraftPanelButton);
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

        // JML: Remove Recipe Selection Buttons
        for (int i = 0; i < statRecipeButtons.Length; i++)
        {
            statRecipeButtons[i].onClick.RemoveAllListeners();
        }

        for (int i = 0; i < skillRecipeButtons.Length; i++)
        {
            skillRecipeButtons[i].onClick.RemoveAllListeners();
        }
    }

    #region Choice Panel
    private async UniTaskVoid OnCloseChicePanelButtonClicked()
    {
        await FadeController.Instance.LoadSceneWithFade(sceneName.LobbyScene);
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
                Debug.LogError("[BookMarkUI] 알 수 없는 책갈피 타입!");
                return;
        }
        // JML: Update UI
        UpdateStatCraftPanelUI(recipe);

        Debug.Log($"[BookMarkUI] 레시피 선택됨: {CSVLoader.Instance.GetData<StringTable>(recipe.Recipe_Name_ID)?.Text ?? "Unknown"}");
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
        // JML: Material 1
        if (recipe.Material_1_ID > 0)
        {
            var material1Data = CSVLoader.Instance.GetData<IngredientData>(recipe.Material_1_ID);
            statMetrial1NameText.text = material1Data != null ? CSVLoader.Instance.GetData<StringTable>(material1Data.Ingredient_Name_ID)?.Text ?? "알 수 없음" : "알 수 없음";

            int inventoryCount = IngredientManager.Instance.GetIngredientCount(recipe.Material_1_ID);
            int requiredCount = recipe.Material_1_Count;

            if (inventoryCount < requiredCount)
            {
                statMetrial1CountText.color = Color.red;
            }
            else
            {
                statMetrial1CountText.color = Color.white;
            }

            var material1Count = $"{inventoryCount}  / {requiredCount}";
            statMetrial1CountText.text = material1Count;
        }

        // JML: Material 2
        if (recipe.Material_2_ID > 0)
        {
            var material2Data = CSVLoader.Instance.GetData<IngredientData>(recipe.Material_2_ID);
            statMetrial2NameText.text = material2Data != null ? CSVLoader.Instance.GetData<StringTable>(material2Data.Ingredient_Name_ID)?.Text ?? "알 수 없음" : "알 수 없음";

            int inventoryCount = IngredientManager.Instance.GetIngredientCount(recipe.Material_2_ID);
            int requiredCount = recipe.Material_2_Count;

            if (inventoryCount < requiredCount)
            {
                statMetrial2CountText.color = Color.red;
            }
            else
            {
                statMetrial2CountText.color = Color.white;
            }

            var material2Count = $"{inventoryCount}  / {requiredCount}";
            statMetrial2CountText.text = material2Count;
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
            Debug.LogWarning("[BookMarkUI] 선택된 레시피가 없습니다!");
            return;
        }

        Debug.Log($"[BookMarkUI] 제작 시도: {CSVLoader.Instance.GetData<StringTable>(selectedRecipe.Recipe_Name_ID)?.Text ?? "Unknown"}");

        // JML: Call BookMarkCraft
        BookMarkCraftResult result = BookMarkCraft.CraftBookmark(selectedRecipe.Recipe_ID);

        if (result.IsSuccess)
        {
            Debug.Log($"[BookMarkUI] 제작 성공! {result.Message}");
            // TODO: Panel for success feedback

            // JML: Hide craft panel after success
            //craftPanel.SetActive(false);
        }
    }
    #endregion
    
    #region Skill Craft Panel
    private void UpdateSkillCraftPanelUI(BookmarkCraftData recipe)
    {
        // JML: Material 1
        if (recipe.Material_1_ID > 0)
        {
            var material1Data = CSVLoader.Instance.GetData<IngredientData>(recipe.Material_1_ID);
            skillMetrial1NameText.text = material1Data != null ? CSVLoader.Instance.GetData<StringTable>(material1Data.Ingredient_Name_ID)?.Text ?? "알 수 없음" : "알 수 없음";

            int inventoryCount = IngredientManager.Instance.GetIngredientCount(recipe.Material_1_ID);
            int requiredCount = recipe.Material_1_Count;

            if (inventoryCount < requiredCount)
            {
                skillMetrial1CountText.color = Color.red;
            }
            else
            {
                skillMetrial1CountText.color = Color.white;
            }

            var material1Count = $"{inventoryCount}  / {requiredCount}";
            skillMetrial1CountText.text = material1Count;
        }

        // JML: Material 2
        if (recipe.Material_2_ID > 0)
        {
            var material2Data = CSVLoader.Instance.GetData<IngredientData>(recipe.Material_2_ID);
            skillMetrial2NameText.text = material2Data != null ? CSVLoader.Instance.GetData<StringTable>(material2Data.Ingredient_Name_ID)?.Text ?? "알 수 없음" : "알 수 없음";

            int inventoryCount = IngredientManager.Instance.GetIngredientCount(recipe.Material_2_ID);
            int requiredCount = recipe.Material_2_Count;

            if (inventoryCount < requiredCount)
            {
                skillMetrial2CountText.color = Color.red;
            }
            else
            {
                skillMetrial2CountText.color = Color.white;
            }

            var material2Count = $"{inventoryCount}  / {requiredCount}";
            skillMetrial2CountText.text = material2Count;
        }

        if (recipe.Material_3_ID > 0)
        {
            var material3Data = CSVLoader.Instance.GetData<IngredientData>(recipe.Material_3_ID);
            skillMetrial3NameText.text = material3Data != null ? CSVLoader.Instance.GetData<StringTable>(material3Data.Ingredient_Name_ID)?.Text ?? "알 수 없음" : "알 수 없음";

            int inventoryCount = IngredientManager.Instance.GetIngredientCount(recipe.Material_3_ID);
            int requiredCount = recipe.Material_3_Count;

            if (inventoryCount < requiredCount)
            {
                skillMetrial3CountText.color = Color.red;
            }
            else
            {
                skillMetrial3CountText.color = Color.white;
            }

            var material3Count = $"{inventoryCount}  / {requiredCount}";
            skillMetrial3CountText.text = material3Count;
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
            Debug.LogError("[BookMarkUI] BookmarkCraftData table not found!");
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

        Debug.Log($"[BookMarkUI] Loaded {statRecipes.Count} stat recipes, {skillRecipes.Count} skill recipes");
    }
    #endregion
}