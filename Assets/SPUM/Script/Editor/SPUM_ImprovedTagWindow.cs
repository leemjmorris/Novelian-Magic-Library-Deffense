using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

public class SPUM_ImprovedTagWindow : EditorWindow
{
    [MenuItem("SPUM/Improved Tag Manager")]
    public static void ShowWindow()
    {
        SPUM_ImprovedTagWindow window = GetWindow<SPUM_ImprovedTagWindow>("SPUM Tag Manager");
        window.minSize = new Vector2(1200, 600);
        window.Show();
    }
    // Manager reference
    private SPUM_ImprovedTagManager tagManager;
    
    // 탭 상태
    private enum TabType { FilterView, Classes, Styles, GoogleSheet }
    private TabType currentTab = TabType.FilterView;
    
    // UI 상태
    private Vector2 mainScrollPosition;
    
    // Filter View 상태
    private Dictionary<string, bool> partFoldouts = new Dictionary<string, bool>();
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
    private List<string> filterSelectedClasses = new List<string>();
    private List<string> filterSelectedStyles = new List<string>();
    private List<string> filterSelectedAddons = new List<string>();
    private List<string> filterSelectedThemes = new List<string>();
    private List<string> filterSelectedRaces = new List<string>();
    private List<string> filterSelectedGenders = new List<string>();
    private List<string> filterSelectedExpressions = new List<string>();
    private List<string> filterSelectedFacialHair = new List<string>();
    private List<string> filterSelectedParts = new List<string>();
    private List<string> filterSelectedStyleElements = new List<string>();
    private string filterSearchTerm = "";
    private bool styleFilterUseAND = false; // false = OR mode, true = AND mode
    private bool customStyleFilterUseAND = false; // false = OR mode, true = AND mode for custom styles
    private bool addonFilterUseAND = false; // false = OR mode, true = AND mode for addons
    private bool themeFilterUseAND = false; // false = OR mode, true = AND mode for themes
    private bool raceFilterUseAND = false; // false = OR mode, true = AND mode for races
    private bool genderFilterUseAND = false; // false = OR mode, true = AND mode for genders
    private bool classFilterUseAND = false; // false = OR mode, true = AND mode for classes
    private bool partFilterUseAND = false; // false = OR mode, true = AND mode for parts
    private bool combatVisualStyleCombineAND = true; // true = AND mode between combat/visual style filters, false = OR mode
    private bool characterAttributesCombineAND = false; // true = AND mode between character attribute filters, false = OR mode
    
    // Individual default mode toggles for Theme, Race, Gender
    private bool themeDefaultSelectAll = true; // true = select all when empty, false = pass all when empty
    private bool raceDefaultSelectAll = true; // true = select all when empty, false = pass all when empty
    private bool genderDefaultSelectAll = true; // true = select all when empty, false = pass all when empty
    
    // Edit Mode 상태
    private SPUM_ImprovedTagManager.ImprovedCharacterDataItem editingItem = null;
    private Vector2 editScrollPosition;
    private Vector2 filterViewScrollPosition;
    private Vector2 filterPanelScrollPosition;
    private Vector2 editPanelScrollPosition;
    
    // Store displayed items in their exact UI order
    private List<SPUM_ImprovedTagManager.ImprovedCharacterDataItem> displayedItems = new List<SPUM_ImprovedTagManager.ImprovedCharacterDataItem>();
    
    // Edit Panel - Selected toggles
    private HashSet<string> editSelectedRoles = new HashSet<string>();
    private HashSet<string> editSelectedQualities = new HashSet<string>();
    private HashSet<string> editSelectedElements = new HashSet<string>();
    private HashSet<string> editSelectedAttackTypes = new HashSet<string>();
    private HashSet<string> editSelectedDistanceTypes = new HashSet<string>();
    private HashSet<string> editSelectedExpressions = new HashSet<string>();
    private HashSet<string> editSelectedFacialHair = new HashSet<string>();
    private HashSet<string> editSelectedHairstyles = new HashSet<string>();
    
    // Edit Panel - Type selection
    private string editSelectedTypeCategory = "";
    private string editSelectedType = "";
    private string editThemeString = "";
    
    // Edit Panel - Foldout states  
    private bool editRoleFoldout = true;
    private bool editQualityFoldout = true;
    private bool editElementsFoldout = true;
    private bool editAttackTypeFoldout = true;
    private bool editDistanceTypeFoldout = true;
    private bool editTypeFoldout = true;
    
    // Dropdown states
    private bool addonDropdownExpanded = false;
    private bool themeDropdownExpanded = false;
    private bool raceDropdownExpanded = false;
    private bool genderDropdownExpanded = false;
    private bool classDropdownExpanded = false;
    private bool styleDropdownExpanded = false;
    private bool styleElementsDropdownExpanded = false;
    private bool partDropdownExpanded = false;
    
    
    // Character Generator 상태
    private Dictionary<string, SPUM_ImprovedTagManager.ImprovedCharacterDataItem> generatedCharacter;
    private List<string> selectedClasses = new List<string>();
    private List<string> selectedStyles = new List<string>();
    
    // Class/Style 편집 상태
    private string editingClassName = "";
    private string newClassName = "";
    private string newClassDescription = "";
    private string editingStyleName = "";
    private string newStyleName = "";
    private string newStyleDescription = "";
    private Dictionary<string, bool> classFoldouts = new Dictionary<string, bool>();
    private Dictionary<string, bool> styleFoldouts = new Dictionary<string, bool>();
    
    // Semantic search states
    private Dictionary<string, string> searchTerms = new Dictionary<string, string>();
    
    // Style Elements tab fields
    private Vector2 styleElementsCategoryScrollPosition;
    private string editingStyleElementKey = "";
    private Dictionary<string, string> newTagInputs = new Dictionary<string, string>();
    
    // Local style elements data (not using tagManager)
    private SPUM_ImprovedStyleData localStyleElementsData;
    private Dictionary<string, string> cachedStyleElementsWithCategories;
    private bool styleElementsCacheValid = false;
    
    // Filter options cache
    private List<string> cachedAvailableAddons;
    private List<string> cachedAvailableThemes;
    private List<string> cachedAvailableRaces;
    private List<string> cachedAvailableGenders;
    private List<string> cachedAvailableClasses;
    private List<string> cachedAvailableStyles;
    private List<string> cachedAvailableParts;
    private bool filterOptionsCacheValid = false;
    
    // Filtered items cache
    private List<SPUM_ImprovedTagManager.ImprovedCharacterDataItem> cachedFilteredItems;
    private string lastFilterHash = "";
    private bool filteredItemsCacheValid = false;
    
    // 새 커스텀 스타일 추가를 위한 임시 필드
    private SPUM_CustomStyleData.CustomStyle tempNewCustomStyle = new SPUM_CustomStyleData.CustomStyle();
    private Dictionary<string, List<string>> searchResults = new Dictionary<string, List<string>>();
    private Dictionary<string, Vector2> searchScrollPositions = new Dictionary<string, Vector2>();
    
    // Image cache
    private static Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();
    private static DateTime lastCacheClear = DateTime.Now;
    private static readonly TimeSpan cacheLifetime = TimeSpan.FromMinutes(10);
    
    
    // Window size tracking
    private float lastWindowWidth = 0f;
    private float lastWindowHeight = 0f;
    
    // Fixed panel width settings
    private float leftPanelWidth = 300f; // Fixed width for left panel
    private float rightPanelWidth = 300f; // Fixed width for right panel
    
    // Panel width offset for manual adjustment
    private const float PANEL_WIDTH_OFFSET = 700f; // Reduced to show one more column
    
    // Google Sheet 설정
    [Serializable]
    private class GoogleSheetConfig
    {
        public string sheetID = "";
        public List<GoogleSheetTab> tabs = new List<GoogleSheetTab>();
    }
    
    [Serializable]
    private class GoogleSheetTab
    {
        public string name = "";
        public string gid = "";
    }
    
    // 기본값 설정
    private const string DEFAULT_SHEET_ID = "2PACX-1vQeQGfFnAuKyh8d4u3rghnUksnHRr2sfEtnNB-pK7dRVFrsx6yO9pl8Fg0RKkZvxk7SwR5BPvVVezu0";
    private static readonly (string name, string gid)[] DEFAULT_TABS = new[]
    {
        ("New", "1619475490")
    };
    
    private GoogleSheetConfig googleSheetConfig = new GoogleSheetConfig();
    private bool isLoadingFromGoogleSheet = false;
    private Vector2 googleSheetScrollPosition;
    private bool isGoogleSheetConfigInitialized = false;
    
    // 스타일
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle tagStyle;
    private GUIStyle removeButtonStyle;
    
    void OnEnable()
    {
        // Find or create tag manager
        tagManager = SPUM_ImprovedTagManager.Instance;
        
        // Subscribe to events
        if (tagManager != null)
        {
            tagManager.OnDataLoaded += OnDataLoaded;
            
            // Load class and style data
            tagManager.LoadImprovedClassData();
            tagManager.LoadImprovedStyleData();
            tagManager.LoadStyleElementsData();
            tagManager.LoadCustomStyleData();
            
            // Style elements data will be loaded dynamically when accessed
            // through GetStyleElementsData() method
        }
        
        // Load style elements data directly (not using tagManager)
        LoadLocalStyleElementsData();
        
        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    void OnDisable()
    {
        // Unsubscribe from events
        if (tagManager != null)
        {
            tagManager.OnDataLoaded -= OnDataLoaded;
        }
        
        // Unsubscribe from play mode state changes
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    
    /// <summary>
    /// Load style elements data directly from Resources without using tagManager
    /// </summary>
    private void LoadLocalStyleElementsData()
    {
        Debug.Log("[SPUM Window] Loading style elements data directly from Resources");
        
        // Invalidate cache when reloading data
        styleElementsCacheValid = false;
        cachedStyleElementsWithCategories = null;
        
        TextAsset jsonAsset = Resources.Load<TextAsset>("SPUM_Style_Elements");
        if (jsonAsset != null)
        {
            try
            {
                localStyleElementsData = SPUM_ImprovedStyleData.FromJson(jsonAsset.text);
                
                if (localStyleElementsData != null && localStyleElementsData.categories != null)
                {
                    Debug.Log($"[SPUM Window] Successfully loaded {localStyleElementsData.categories.Count} categories");
                    foreach (var cat in localStyleElementsData.categories.Keys)
                    {
                        // Category loaded
                    }
                }
                else
                {
                    // Loaded data but no categories found
                    localStyleElementsData = new SPUM_ImprovedStyleData();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SPUM Window] Error parsing style elements JSON: {e.Message}");
                localStyleElementsData = new SPUM_ImprovedStyleData();
            }
        }
        else
        {
            Debug.LogError("[SPUM Window] Failed to load SPUM_Style_Elements.json from Resources");
            localStyleElementsData = new SPUM_ImprovedStyleData();
        }
    }
    
    void OnDataLoaded(List<SPUM_ImprovedTagManager.ImprovedCharacterDataItem> data)
    {
        Debug.Log($"[SPUM] Data loaded in window: {data.Count} items");
        // Invalidate all caches when data is reloaded
        filterOptionsCacheValid = false;
        styleElementsCacheValid = false;
        filteredItemsCacheValid = false;
        lastFilterHash = "";
        Repaint();
    }
    
    void InvalidateFilterCacheIfNeeded()
    {
        // This could be enhanced to check if tagManager data has changed
        // For now, we'll validate the cache once per session
        if (!filterOptionsCacheValid)
        {
            filterOptionsCacheValid = true;
        }
    }
    
    void InvalidateAllCaches()
    {
        filterOptionsCacheValid = false;
        styleElementsCacheValid = false;
        filteredItemsCacheValid = false;
        cachedAvailableAddons = null;
        cachedAvailableThemes = null;
        cachedAvailableRaces = null;
        cachedAvailableGenders = null;
        cachedAvailableClasses = null;
        cachedAvailableStyles = null;
        cachedAvailableParts = null;
        cachedStyleElementsWithCategories = null;
        cachedFilteredItems = null;
        lastFilterHash = "";
    }
    
    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Re-initialize tag manager when exiting play mode
        if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.ExitingPlayMode)
        {
            // Re-initialize tag manager
            tagManager = SPUM_ImprovedTagManager.Instance;
            
            if (tagManager != null)
            {
                // Re-subscribe to events
                tagManager.OnDataLoaded -= OnDataLoaded; // Remove first to avoid duplicates
                tagManager.OnDataLoaded += OnDataLoaded;
                
                // Reload data
                tagManager.LoadImprovedClassData();
                tagManager.LoadImprovedStyleData();
                tagManager.LoadStyleElementsData();
                tagManager.LoadCustomStyleData();
            }
            
            // Force repaint
            Repaint();
        }
    }
    
    void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
        }
        
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };
        }
        
        if (selectedButtonStyle == null)
        {
            selectedButtonStyle = new GUIStyle(GUI.skin.button);
            selectedButtonStyle.normal.textColor = Color.white;
            selectedButtonStyle.normal.background = MakeColorTexture(new Color(0.2f, 0.5f, 0.8f));
            selectedButtonStyle.hover.background = MakeColorTexture(new Color(0.3f, 0.6f, 0.9f));
            selectedButtonStyle.active.background = MakeColorTexture(new Color(0.1f, 0.4f, 0.7f));
        }
        
        if (tagStyle == null)
        {
            tagStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 20,
                margin = new RectOffset(2, 2, 2, 2)
            };
        }
        
        if (removeButtonStyle == null)
        {
            removeButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fixedWidth = 20,
                fixedHeight = 20,
                margin = new RectOffset(2, 2, 2, 2)
            };
        }
    }
    
    void OnGUI()
    {
        InitializeStyles();
        
        // Detect window resize and repaint if needed
        if (Event.current.type == EventType.Repaint)
        {
            float currentWidth = position.width;
            float currentHeight = position.height;
            
            if (Mathf.Abs(currentWidth - lastWindowWidth) > 1f || 
                Mathf.Abs(currentHeight - lastWindowHeight) > 1f)
            {
                lastWindowWidth = currentWidth;
                lastWindowHeight = currentHeight;
                Repaint();
            }
        }
        
        if (tagManager == null)
        {
            // Try to re-initialize tagManager
            tagManager = SPUM_ImprovedTagManager.Instance;
            
            // If still null, show error
            if (tagManager == null)
            {
                EditorGUILayout.HelpBox("Tag Manager not found!", MessageType.Error);
                if (GUILayout.Button("Create Tag Manager"))
                {
                    tagManager = SPUM_ImprovedTagManager.Instance;
                    if (tagManager != null)
                    {
                        tagManager.OnDataLoaded += OnDataLoaded;
                        tagManager.LoadImprovedClassData();
                        tagManager.LoadImprovedStyleData();
                        tagManager.LoadStyleElementsData();
                        tagManager.LoadCustomStyleData();
                    }
                }
                return;
            }
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("SPUM Improved Tag Manager", headerStyle);
        EditorGUILayout.Space(10);
                
        // 탭 선택
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == TabType.FilterView, "Filter View", "Button", GUILayout.Height(30)))
            currentTab = TabType.FilterView;
        if (GUILayout.Toggle(currentTab == TabType.Classes, "Classes", "Button", GUILayout.Height(30)))
            currentTab = TabType.Classes;
        if (GUILayout.Toggle(currentTab == TabType.Styles, "Styles", "Button", GUILayout.Height(30)))
            currentTab = TabType.Styles;
        if (GUILayout.Toggle(currentTab == TabType.GoogleSheet, "Google Sheet", "Button", GUILayout.Height(30)))
            currentTab = TabType.GoogleSheet;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // 탭 내용 그리기
        switch (currentTab)
        {
            case TabType.FilterView:
                DrawFilterViewTab();
                break;
            case TabType.Classes:
                DrawClassesTab();
                break;
            case TabType.Styles:
                DrawStylesTab();
                break;
            case TabType.GoogleSheet:
                DrawGoogleSheetTab();
                break;
        }
    }
    
    void DrawFilterViewTab()
    {
        // Always get fresh data from tagManager
        var allData = tagManager.GetAllCharacterData();
        
        if (!tagManager.IsDataLoaded() || allData == null || allData.Count == 0)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.HelpBox("No character data available. Please ensure data is loaded in the Tag Manager.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }
        
        EditorGUILayout.Space(10);
        
        // Left side - Filter controls
        EditorGUILayout.BeginHorizontal();
        
        // Left panel with fixed width
        EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(leftPanelWidth), GUILayout.MaxWidth(leftPanelWidth));
        
        // Filters header
        EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // Calculate height for left panel scroll
        float leftPanelHeight = position.height - 200f; // Reserve space for header and tabs
        leftPanelHeight = Mathf.Max(leftPanelHeight, 300f);
        
        // Begin scroll view for filters
        filterPanelScrollPosition = EditorGUILayout.BeginScrollView(
            filterPanelScrollPosition,
            false, // alwaysShowHorizontal
            true,  // alwaysShowVertical
            GUILayout.Height(leftPanelHeight)
        );
        
        // Filter Controls Header
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Filter Controls", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // Get available options from data (cached)
        InvalidateFilterCacheIfNeeded();
        var availableAddons = GetAvailableAddons();
        var availableThemes = GetAvailableThemes();
        var availableRaces = GetAvailableRaces();
        var availableGenders = GetAvailableGenders();
        var availableClasses = GetAvailableClasses();
        var availableStyles = GetAvailableStyles();
        var availableParts = GetAvailableParts();
        
        // Vertical layout for all dropdowns to prevent horizontal scrolling
        // Addon Filter (최상단)
        EditorGUILayout.LabelField("Addon Package", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        
        DrawMultiSelectDropdown("Addon", availableAddons, filterSelectedAddons, ref addonDropdownExpanded);
        EditorGUILayout.Space(10);
        
        // Character Attributes
        EditorGUILayout.LabelField("Character Attributes", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        
        DrawMultiSelectDropdown("Theme", availableThemes, filterSelectedThemes, ref themeDropdownExpanded);
        EditorGUILayout.Space(3);
        
        DrawMultiSelectDropdown("Race (Body)", availableRaces, filterSelectedRaces, ref raceDropdownExpanded);
        EditorGUILayout.Space(3);
        
        DrawMultiSelectDropdown("Gender (Hair/Face/Armor)", availableGenders, filterSelectedGenders, ref genderDropdownExpanded);
        EditorGUILayout.Space(5);
        
        // Combat & Visual Style
        EditorGUILayout.LabelField("Combat & Visual Style", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        
        DrawMultiSelectDropdownWithToggle("Class", availableClasses, filterSelectedClasses, ref classDropdownExpanded, ref classFilterUseAND);
        EditorGUILayout.Space(3);
        
        // Use custom styles dropdown
        var customStyleData = tagManager.GetCustomStyleData();
        // Custom style data loaded
        if (customStyleData != null && customStyleData.custom_styles != null && customStyleData.custom_styles.Count > 0)
        {
            // Create a simple dictionary for custom styles display
            var customStylesDict = new Dictionary<string, string>();
            foreach (var kvp in customStyleData.custom_styles)
            {
                customStylesDict[kvp.Key] = kvp.Value.name; // Use the Korean name for display
            }
            // Using custom styles dropdown
            
            DrawCustomStylesDropdownWithToggle("Style", customStylesDict, filterSelectedStyles, ref styleDropdownExpanded, ref customStyleFilterUseAND);
        }
        else
        {
            // Using regular styles dropdown
            DrawMultiSelectDropdown("Style", availableStyles, filterSelectedStyles, ref styleDropdownExpanded);
        }
        EditorGUILayout.Space(5);
        
        // Style Elements dropdown with categories
        var styleElementsWithCategories = GetStyleElementsWithCategories();
        DrawStyleElementsDropdownWithToggle("Style Elements", styleElementsWithCategories, filterSelectedStyleElements, ref styleElementsDropdownExpanded, ref styleFilterUseAND);
        EditorGUILayout.Space(5);
        
        // Secondary filters (Part at the bottom)
        EditorGUILayout.LabelField("Additional Filters", EditorStyles.miniLabel);
        
        DrawMultiSelectDropdown("Part", availableParts, filterSelectedParts, ref partDropdownExpanded);
        EditorGUILayout.Space(5);
        
        // Search field and Reset button
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        filterSearchTerm = EditorGUILayout.TextField(filterSearchTerm);
        
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            ResetAllFilters();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
        
        // Display selected tags and negative tags
        if (filterSelectedClasses.Count > 0 || filterSelectedStyles.Count > 0)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Selected Tags", EditorStyles.boldLabel);
            
            // Display selected class tags and their negatives
            if (filterSelectedClasses.Count > 0)
            {
                var classData = tagManager.GetClassData();
                foreach (var selectedClass in filterSelectedClasses)
                {
                    if (classData != null && classData.combat_classes.ContainsKey(selectedClass))
                    {
                        var classInfo = classData.combat_classes[selectedClass];
                        
                        // Class name
                        EditorGUILayout.LabelField($"Class: {selectedClass}", EditorStyles.miniBoldLabel);
                        
                        // Type tags in green
                        if (classInfo.type != null && classInfo.type.Count > 0)
                        {
                            GUI.color = new Color(0.3f, 0.8f, 0.3f); // Green
                            EditorGUILayout.LabelField("  + Type: " + string.Join(", ", classInfo.type.Take(5).ToList()) + (classInfo.type.Count > 5 ? "..." : ""), EditorStyles.miniLabel);
                            GUI.color = Color.white;
                        }
                        
                        // Class tags in green
                        if (classInfo.class_tags != null && classInfo.class_tags.Count > 0)
                        {
                            GUI.color = new Color(0.3f, 0.8f, 0.3f); // Green
                            EditorGUILayout.LabelField("  + Class: " + string.Join(", ", classInfo.class_tags), EditorStyles.miniLabel);
                            GUI.color = Color.white;
                        }
                        
                        // Type negative tags in red
                        if (classInfo.type_negative != null && classInfo.type_negative.Count > 0)
                        {
                            GUI.color = new Color(1f, 0.3f, 0.3f); // Red
                            EditorGUILayout.LabelField("  - Type: " + string.Join(", ", classInfo.type_negative), EditorStyles.miniLabel);
                            GUI.color = Color.white;
                        }
                        
                        // Class negative tags in red
                        if (classInfo.class_negative != null && classInfo.class_negative.Count > 0)
                        {
                            GUI.color = new Color(1f, 0.3f, 0.3f); // Red
                            EditorGUILayout.LabelField("  - Class: " + string.Join(", ", classInfo.class_negative), EditorStyles.miniLabel);
                            GUI.color = Color.white;
                        }
                    }
                }
            }
            
            // Display selected style tags and their negatives
            if (filterSelectedStyles.Count > 0)
            {
                var selectedCustomStyleData = tagManager.GetCustomStyleData();
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Styles:", EditorStyles.miniBoldLabel);
                
                foreach (var selectedStyle in filterSelectedStyles)
                {
                    // Find the custom style
                    if (selectedCustomStyleData != null && selectedCustomStyleData.custom_styles.ContainsKey(selectedStyle))
                    {
                        var customStyle = selectedCustomStyleData.custom_styles[selectedStyle];
                        
                        // Style name with Korean display name
                        EditorGUILayout.LabelField($"  {customStyle.name} ({selectedStyle}):", EditorStyles.miniLabel);
                        
                        // Elements tags in green
                        if (customStyle.elements != null && customStyle.elements.Count > 0)
                        {
                            GUI.color = new Color(0.3f, 0.8f, 0.3f); // Green
                            EditorGUILayout.LabelField("    + " + string.Join(", ", customStyle.elements), EditorStyles.miniLabel);
                            GUI.color = Color.white;
                        }
                        
                        // Required parts in blue
                        if (customStyle.required_parts != null && customStyle.required_parts.Count > 0)
                        {
                            GUI.color = new Color(0.3f, 0.5f, 1f); // Blue
                            EditorGUILayout.LabelField("    ⚙ " + string.Join(", ", customStyle.required_parts), EditorStyles.miniLabel);
                            GUI.color = Color.white;
                        }
                        
                        // Negative tags removed - no longer used
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        // Generate button
        EditorGUILayout.Space(10);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Generate Random Character", GUILayout.Height(30)))
        {
            GenerateCharacter();
        }
        GUI.backgroundColor = Color.white;
        
        // Show generated character if exists
        if (generatedCharacter != null && generatedCharacter.Count > 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Generated Character:", EditorStyles.boldLabel);
            
            foreach (var kvp in generatedCharacter.OrderBy(x => GetPartOrder(x.Key)))
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                // Part and filename
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{kvp.Key}:", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField(kvp.Value.FileName, EditorStyles.label, GUILayout.Width(150));
                EditorGUILayout.EndHorizontal();
                
                // Display all attributes horizontally
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(80)); // Indent
                
                // Build info string
                List<string> infoItems = new List<string>();
                
                // Theme
                if (kvp.Value.Theme != null && kvp.Value.Theme.Length > 0)
                {
                    infoItems.Add($"Theme: {string.Join(", ", kvp.Value.Theme)}");
                }
                
                // Race
                if (!string.IsNullOrEmpty(kvp.Value.Race))
                {
                    infoItems.Add($"Race: {kvp.Value.Race}");
                }
                
                // Gender
                if (!string.IsNullOrEmpty(kvp.Value.Gender))
                {
                    infoItems.Add($"Gender: {kvp.Value.Gender}");
                }
                
                // Type
                if (!string.IsNullOrEmpty(kvp.Value.Type))
                {
                    infoItems.Add($"Type: {kvp.Value.Type}");
                }
                
                // Class
                if (kvp.Value.Class != null && kvp.Value.Class.Length > 0)
                {
                    infoItems.Add($"Class: {string.Join(", ", kvp.Value.Class)}");
                }
                
                // Style
                if (kvp.Value.Style != null && kvp.Value.Style.Length > 0)
                {
                    infoItems.Add($"Style: {string.Join(", ", kvp.Value.Style)}");
                }
                
                // Display as single line with separators
                if (infoItems.Count > 0)
                {
                    GUI.color = new Color(0.7f, 0.7f, 0.7f); // Gray
                    EditorGUILayout.LabelField(string.Join(" | ", infoItems), EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndVertical();
        }
        
        // End scroll view for left panel
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
        
        // Middle panel - Filtered items (expandable)
        EditorGUILayout.BeginVertical(boxStyle, GUILayout.ExpandWidth(true));
        
        // Header with refresh button and data status
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Filtered Items by Part", EditorStyles.boldLabel);
        
        // Data status indicator
        GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel);
        if (tagManager.IsDataLoaded() && tagManager.allCharacterData != null)
        {
            statusStyle.normal.textColor = Color.green;
            EditorGUILayout.LabelField($"✓ {tagManager.allCharacterData.Count} items loaded", statusStyle, GUILayout.Width(120));
        }
        else
        {
            statusStyle.normal.textColor = Color.red;
            EditorGUILayout.LabelField("✗ No data", statusStyle, GUILayout.Width(80));
        }
        
        
        // Export TSV button
        if (GUILayout.Button(new GUIContent("Export TSV", "Export data to TSV file"), GUILayout.Width(80)))
        {
            ExportToTSV();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Get filtered items - this dynamically fetches from allCharacterData
        var filteredItems = GetFilteredItemsForGenerator();
        // Items filtered
        
        // Additional search filtering
        if (!string.IsNullOrEmpty(filterSearchTerm))
        {
            string searchLower = filterSearchTerm.ToLower();
            filteredItems = filteredItems.Where(item => 
                item.FileName.ToLower().Contains(searchLower) ||
                item.Type?.ToLower().Contains(searchLower) == true ||
                item.Properties.Any(p => p.ToLower().Contains(searchLower)) ||
                item.Class.Any(c => c.ToLower().Contains(searchLower)) ||
                item.Style.Any(s => s.ToLower().Contains(searchLower))
            ).ToList();
        }
        
        EditorGUILayout.LabelField($"Found {filteredItems.Count} items", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        
        // Group by part and display - dynamically based on what parts exist in the data
        var groupedByPart = filteredItems
            .Where(item => !string.IsNullOrEmpty(item.Part)) // Filter out items without part
            .GroupBy(item => item.Part)
            .OrderBy(g => GetPartOrder(g.Key)); // Custom ordering for parts
        
        // Show available parts count
        var uniqueParts = groupedByPart.Select(g => g.Key).ToList();
        EditorGUILayout.LabelField($"Available Parts: {string.Join(", ", uniqueParts)}", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        
        // Calculate available height for scroll view
        float usedHeight = 280f; // Approximate height used by headers, tabs, filters, etc.
        float availableHeight = position.height - usedHeight;
        availableHeight = Mathf.Max(availableHeight, 100f); // Minimum height
        
        // Main scroll view for all items with constrained height
        filterViewScrollPosition = EditorGUILayout.BeginScrollView(
            filterViewScrollPosition, 
            false, // alwaysShowHorizontal
            true,  // alwaysShowVertical
            GUILayout.Height(availableHeight),
            GUILayout.ExpandWidth(true)
        );
        
        // Clear and rebuild displayed items list in exact UI order
        displayedItems.Clear();
        
        // Display all parts and items
        foreach (var partGroup in groupedByPart)
        {
            // Add items in the order they will be displayed
            displayedItems.AddRange(partGroup.ToList());
            DrawPartSection(partGroup.Key, partGroup.ToList());
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        
        // Right panel - Edit panel (always visible)
        EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(rightPanelWidth), GUILayout.MaxWidth(rightPanelWidth));
        DrawEditPanel();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
    }
    
    void SaveClassData(SPUM_ImprovedClassData classData)
    {
        if (classData != null)
        {
            string json = classData.ToJson();
            string path = "Assets/SPUM/Core/Resources/SPUM_Class_Tags_Improved.json";
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            tagManager.LoadImprovedClassData();
            EditorUtility.DisplayDialog("Success", "Class data saved successfully!", "OK");
        }
    }
    
    void SaveStyleData(SPUM_ImprovedStyleData styleData)
    {
        if (styleData != null)
        {
            string json = styleData.ToJson();
            string path = "Assets/SPUM/Core/Resources/SPUM_Style_Tags_Improved.json";
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            tagManager.LoadImprovedStyleData();
            EditorUtility.DisplayDialog("Success", "Style data saved successfully!", "OK");
        }
    }
    
    void SaveCustomStyleData(SPUM_CustomStyleData customStyleData)
    {
        if (customStyleData != null)
        {
            string json = customStyleData.ToJson();
            string path = "Assets/SPUM/Core/Resources/SPUM_Custom_Styles.json";
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            tagManager.LoadCustomStyleData();
            EditorUtility.DisplayDialog("Success", "Custom style data saved successfully!", "OK");
        }
    }
    
    void SaveStyleElementsData(SPUM_ImprovedStyleData styleElementsData)
    {
        if (styleElementsData != null)
        {
            string json = styleElementsData.ToJson();
            string path = "Assets/SPUM/Core/Resources/SPUM_Style_Elements.json";
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            tagManager.LoadStyleElementsData();
            EditorUtility.DisplayDialog("Success", "Style elements data saved successfully!", "OK");
        }
    }
    
    bool GetFoldoutState(string key)
    {
        if (!foldoutStates.ContainsKey(key))
            foldoutStates[key] = false;
        return foldoutStates[key];
    }
    
    void SetFoldoutState(string key, bool state)
    {
        foldoutStates[key] = state;
    }
    
    void DrawClassesTab()
    {
        // Description text at the top
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Combat Classes Configuration", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Define combat classes with their associated tags and semantic meanings for character generation.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(10);
        
        // Tag type reference
        EditorGUILayout.LabelField("Available Tag Types:", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // Distance Type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Distance Type (거리타입):", GUILayout.Width(150));
        GUI.color = new Color(0.7f, 0.7f, 0.7f);
        EditorGUILayout.LabelField("melee (근접), ranged (원거리)", EditorStyles.miniLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Attack Type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Attack Type (공격타입):", GUILayout.Width(150));
        GUI.color = new Color(0.7f, 0.7f, 0.7f);
        EditorGUILayout.LabelField("physical (물리), magical (마법)", EditorStyles.miniLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Role Type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Role Type (역할타입):", GUILayout.Width(150));
        GUI.color = new Color(0.7f, 0.7f, 0.7f);
        EditorGUILayout.LabelField("damage (딜링), tank (탱킹), healing (힐링), support (지원)", EditorStyles.miniLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Clothing Type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Clothing Type (의류 타입):", GUILayout.Width(150));
        GUI.color = new Color(0.7f, 0.7f, 0.7f);
        EditorGUILayout.LabelField("tunic, robe, leather, plate, chain_mail", EditorStyles.miniLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Equipment Type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Equipment Type (장비 타입):", GUILayout.Width(150));
        GUI.color = new Color(0.7f, 0.7f, 0.7f);
        EditorGUILayout.LabelField("sword, axe, hammer, mace, spear, shield, bow, staff, wand, dagger", EditorStyles.miniLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Additional equipment
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Armor Parts:", GUILayout.Width(150));
        GUI.color = new Color(0.7f, 0.7f, 0.7f);
        EditorGUILayout.LabelField("helmet, shoulder, bracer, pants, shoes, cape, belt", EditorStyles.miniLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
        
        var classData = tagManager.GetClassData();
        if (classData == null)
        {
            classData = new SPUM_ImprovedClassData();
        }
        
        // Calculate available height for scroll view
        float tabUsedHeight = 350f; // Height used by tab header, description, tag reference, and buttons
        float tabAvailableHeight = position.height - tabUsedHeight;
        tabAvailableHeight = Mathf.Max(tabAvailableHeight, 200f);
        
        mainScrollPosition = EditorGUILayout.BeginScrollView(
            mainScrollPosition,
            false, // alwaysShowHorizontal
            true,  // alwaysShowVertical
            GUILayout.Height(tabAvailableHeight)
        );
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Combat Classes", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Add Class", GUILayout.Width(100)))
        {
            editingClassName = "new_class";
            newClassName = "new_class";
            newClassDescription = "";
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
        
        // Add new class form
        if (editingClassName == "new_class")
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Add New Class", EditorStyles.boldLabel);
            
            newClassName = EditorGUILayout.TextField("Class Name:", newClassName);
            newClassDescription = EditorGUILayout.TextField("Description:", newClassDescription);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                if (!string.IsNullOrEmpty(newClassName) && !classData.combat_classes.ContainsKey(newClassName))
                {
                    var newClass = new SPUM_ImprovedClassData.CombatClass
                    {
                        description = newClassDescription,
                        type = new List<string>(),
                        class_tags = new List<string>(),
                        type_negative = new List<string>(),
                        class_negative = new List<string>(),
                        style = new List<string>()
                    };
                    classData.combat_classes[newClassName] = newClass;
                    tagManager.classData = classData;
                    SaveClassData(classData);
                    editingClassName = "";
                }
            }
            if (GUILayout.Button("Cancel"))
            {
                editingClassName = "";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        // Display existing classes
        var classesToRemove = new List<string>();
        foreach (var kvp in classData.combat_classes)
        {
            if (!classFoldouts.ContainsKey(kvp.Key))
                classFoldouts[kvp.Key] = false;
                
            EditorGUILayout.BeginVertical(boxStyle);
            
            EditorGUILayout.BeginHorizontal();
            classFoldouts[kvp.Key] = EditorGUILayout.Foldout(classFoldouts[kvp.Key], kvp.Key.ToUpper(), true);
            
            if (editingClassName != kvp.Key)
            {
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    editingClassName = kvp.Key;
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Class", 
                        $"Are you sure you want to delete '{kvp.Key}'?", "Yes", "No"))
                    {
                        classesToRemove.Add(kvp.Key);
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
            
            if (classFoldouts[kvp.Key])
            {
                if (editingClassName == kvp.Key)
                {
                    // Edit mode
                    kvp.Value.description = EditorGUILayout.TextField("Description:", kvp.Value.description);
                    
                    // Type editing with semantic search
                    EditorGUILayout.LabelField("Type (사용 가능한 장비/아이템):", EditorStyles.miniBoldLabel);
                    DrawTagListWithSearch(kvp.Value.type, $"class_{kvp.Key}_type", "type");
                    
                    // Class Tags editing with semantic search
                    EditorGUILayout.LabelField("Class (전투 스타일):", EditorStyles.miniBoldLabel);
                    DrawTagListWithSearch(kvp.Value.class_tags, $"class_{kvp.Key}_class_tags", "class");
                    
                    // Style editing with semantic search
                    EditorGUILayout.LabelField("Style (시각적 스타일):", EditorStyles.miniBoldLabel);
                    if (kvp.Value.style == null)
                        kvp.Value.style = new List<string>();
                    DrawTagListWithSearch(kvp.Value.style, $"class_{kvp.Key}_style", "style");
                    
                    // Type negative tags editing with semantic search
                    EditorGUILayout.LabelField("Type Negative Tags (장비/아이템 타입):", EditorStyles.miniBoldLabel);
                    if (kvp.Value.type_negative == null)
                        kvp.Value.type_negative = new List<string>();
                    DrawTagListWithSearch(kvp.Value.type_negative, $"class_{kvp.Key}_type_negative", "type");
                    
                    // Class negative tags editing with semantic search
                    EditorGUILayout.LabelField("Class Negative Tags (클래스/스타일):", EditorStyles.miniBoldLabel);
                    if (kvp.Value.class_negative == null)
                        kvp.Value.class_negative = new List<string>();
                    DrawTagListWithSearch(kvp.Value.class_negative, $"class_{kvp.Key}_class_negative", "all");
                    
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Save"))
                    {
                        tagManager.classData = classData;
                        SaveClassData(classData);
                        editingClassName = "";
                    }
                    if (GUILayout.Button("Cancel"))
                    {
                        editingClassName = "";
                        // Reload to discard changes
                        tagManager.LoadImprovedClassData();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // View mode
                    EditorGUILayout.LabelField($"Description: {kvp.Value.description}", EditorStyles.wordWrappedMiniLabel);
                    
                    EditorGUILayout.Space(5);
                    
                    // Type
                    if (kvp.Value.type.Count > 0)
                    {
                        EditorGUILayout.LabelField("Type (사용 가능한 장비/아이템):", EditorStyles.miniBoldLabel);
                        DrawCompactTagList(kvp.Value.type);
                    }
                    
                    // Class Tags
                    if (kvp.Value.class_tags.Count > 0)
                    {
                        EditorGUILayout.LabelField("Class (전투 스타일):", EditorStyles.miniBoldLabel);
                        DrawCompactTagList(kvp.Value.class_tags);
                    }
                    
                    // Style
                    if (kvp.Value.style != null && kvp.Value.style.Count > 0)
                    {
                        EditorGUILayout.LabelField("Style (시각적 스타일):", EditorStyles.miniBoldLabel);
                        DrawCompactTagList(kvp.Value.style);
                    }
                    
                    // Type negative tags
                    if (kvp.Value.type_negative != null && kvp.Value.type_negative.Count > 0)
                    {
                        EditorGUILayout.LabelField("Type Conflicts:", EditorStyles.miniBoldLabel);
                        GUI.color = new Color(1f, 0.3f, 0.3f); // Red
                        DrawCompactTagList(kvp.Value.type_negative, true);
                        GUI.color = Color.white;
                    }
                    
                    // Class negative tags
                    if (kvp.Value.class_negative != null && kvp.Value.class_negative.Count > 0)
                    {
                        EditorGUILayout.LabelField("Class Conflicts:", EditorStyles.miniBoldLabel);
                        GUI.color = new Color(1f, 0.3f, 0.3f); // Red
                        DrawCompactTagList(kvp.Value.class_negative, true);
                        GUI.color = Color.white;
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        // Remove classes marked for deletion
        foreach (var className in classesToRemove)
        {
            classData.combat_classes.Remove(className);
        }
        if (classesToRemove.Count > 0)
        {
            tagManager.classData = classData;
            SaveClassData(classData);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawStylesTab()
    {
        // Main horizontal layout
        EditorGUILayout.BeginHorizontal();
        
        // Left panel - Style Elements with dropdowns
        float leftPanelWidth = Mathf.Clamp(position.width * 0.35f, 300f, 400f);
        EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(leftPanelWidth));
        DrawStyleElementsPanel();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Right panel - Custom Styles
        EditorGUILayout.BeginVertical();
        DrawCustomStylesPanel();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
    }
    
    void DrawStyleElementsPanel()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Style Elements", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Add Category", GUILayout.Width(100)))
        {
            // Add new category dialog
            string newCategoryKey = "new_category";
            string newCategoryName = "New Category";
            
            if (localStyleElementsData != null && !localStyleElementsData.categories.ContainsKey(newCategoryKey))
            {
                localStyleElementsData.categories[newCategoryKey] = new SPUM_ImprovedStyleData.StyleCategory
                {
                    id = newCategoryKey,
                    name = newCategoryName
                };
                SaveStyleElementsData(localStyleElementsData);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
        
        // Use local data instead of tagManager
        if (localStyleElementsData == null || localStyleElementsData.categories == null || localStyleElementsData.categories.Count == 0)
        {
            EditorGUILayout.HelpBox("No style elements data loaded.", MessageType.Warning);
            
            // Add manual load button
            if (GUILayout.Button("Reload Style Elements Data", GUILayout.Height(25)))
            {
                LoadLocalStyleElementsData();
                ShowNotification(new GUIContent("Style elements data reloaded"));
                Repaint();
            }
            
            return;
        }
        
        // Scroll view for categories
        float panelHeight = position.height - 250f;
        panelHeight = Mathf.Max(panelHeight, 200f);
        
        styleElementsCategoryScrollPosition = EditorGUILayout.BeginScrollView(
            styleElementsCategoryScrollPosition,
            false, true,
            GUILayout.Height(panelHeight)
        );
        
        // Track items to delete
        string categoryToDelete = null;
        var elementsToDelete = new Dictionary<string, string>();
        
        // Draw each category as a foldout dropdown
        foreach (var category in localStyleElementsData.categories)
        {
            string foldoutKey = $"cat_{category.Key}";
            bool isExpanded = GetFoldoutState(foldoutKey);
            
            // Category header with count
            EditorGUILayout.BeginHorizontal();
            isExpanded = EditorGUILayout.Foldout(isExpanded, $"{category.Value.name} ({category.Value.styles.Count})", true);
            SetFoldoutState(foldoutKey, isExpanded);
            
            // Edit category name
            if (GUILayout.Button("Edit", GUILayout.Width(40)))
            {
                string newName = EditorInputDialog.Show("Edit Category Name", "Category Name:", category.Value.name);
                if (!string.IsNullOrEmpty(newName))
                {
                    category.Value.name = newName;
                    SaveStyleElementsData(localStyleElementsData);
                }
            }
            
            // Delete category button
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Category", 
                    $"Are you sure you want to delete the category '{category.Value.name}' and all its elements?", "Yes", "No"))
                {
                    categoryToDelete = category.Key;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Add new element button
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(15));
                if (GUILayout.Button("+ Add Element", GUILayout.Width(100)))
                {
                    string newKey = EditorInputDialog.Show("New Element", "Element Key:", "new_element");
                    if (!string.IsNullOrEmpty(newKey) && !category.Value.styles.Any(s => s.id == newKey))
                    {
                        category.Value.styles.Add(new SPUM_ImprovedStyleData.StyleInfo
                        {
                            id = newKey,
                            description = "New element description"
                        });
                        SaveStyleElementsData(localStyleElementsData);
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
                
                // Show all elements in this category
                foreach (var element in category.Value.styles)
                {
                    EditorGUILayout.BeginVertical(boxStyle);
                    
                    // Element header
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {element.id}", EditorStyles.boldLabel, GUILayout.Width(100));
                    
                    // Edit button
                    if (GUILayout.Button("Edit", GUILayout.Width(40)))
                    {
                        editingStyleElementKey = $"{category.Key}_{element.id}";
                    }
                    
                    // Delete button
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Element", 
                            $"Are you sure you want to delete '{element.id}'?", "Yes", "No"))
                        {
                            elementsToDelete[category.Key] = element.id;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    // Show or edit element details
                    if (editingStyleElementKey == $"{category.Key}_{element.id}")
                    {
                        // Edit mode
                        EditorGUI.indentLevel++;
                        
                        element.description = EditorGUILayout.TextField("Description:", element.description);
                        
                        GUI.color = new Color(0.7f, 0.9f, 0.7f); // Light green
                        // Style and negative tags removed - no longer used in SPUM_Style_Elements.json
                        
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Save", GUILayout.Width(60)))
                        {
                            SaveStyleElementsData(localStyleElementsData);
                            editingStyleElementKey = "";
                        }
                        if (GUILayout.Button("Cancel", GUILayout.Width(60)))
                        {
                            editingStyleElementKey = "";
                            tagManager.LoadStyleElementsData(); // Reload to discard changes
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        // View mode
                        EditorGUILayout.LabelField(element.description, EditorStyles.wordWrappedMiniLabel);
                        
                        // Style and negative tags display removed - no longer used
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }
        
        // Delete marked items
        if (categoryToDelete != null)
        {
            localStyleElementsData.categories.Remove(categoryToDelete);
            SaveStyleElementsData(localStyleElementsData);
        }
        
        foreach (var kvp in elementsToDelete)
        {
            if (localStyleElementsData.categories.ContainsKey(kvp.Key))
            {
                var toRemove = localStyleElementsData.categories[kvp.Key].styles.FirstOrDefault(s => s.id == kvp.Value);
                if (toRemove != null)
                {
                    localStyleElementsData.categories[kvp.Key].styles.Remove(toRemove);
                }
            }
        }
        if (elementsToDelete.Count > 0)
        {
            SaveStyleElementsData(localStyleElementsData);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawCustomStylesPanel()
    {
        // Description text at the top
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Custom Styles", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Pre-configured style combinations", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(5);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
        
        var customStyleData = tagManager.GetCustomStyleData();
        if (customStyleData == null)
        {
            customStyleData = new SPUM_CustomStyleData();
        }
        
        // Add new style button
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Custom Style", GUILayout.Height(25)))
        {
            editingStyleName = "new_style";
            newStyleName = "new_style";
            newStyleDescription = "";
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
        
        // Calculate available height for scroll view
        float panelHeight = position.height - 300f;
        panelHeight = Mathf.Max(panelHeight, 200f);
        
        mainScrollPosition = EditorGUILayout.BeginScrollView(
            mainScrollPosition,
            false, true,
            GUILayout.Height(panelHeight)
        );
        
        // Add new custom style form
        if (editingStyleName == "new_style")
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Add New Custom Style", EditorStyles.boldLabel);
            
            newStyleName = EditorGUILayout.TextField("Style ID:", newStyleName);
            
            // Korean name field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name (Korean):", GUILayout.Width(100));
            if (!string.IsNullOrEmpty(newStyleName) && customStyleData.custom_styles.ContainsKey(newStyleName))
            {
                var tempStyle = customStyleData.custom_styles[newStyleName];
                tempStyle.name = EditorGUILayout.TextField(tempStyle.name);
            }
            else
            {
                EditorGUILayout.TextField("");
            }
            EditorGUILayout.EndHorizontal();
            
            newStyleDescription = EditorGUILayout.TextField("Description:", newStyleDescription);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create", GUILayout.Width(80)))
            {
                if (!string.IsNullOrEmpty(newStyleName) && !customStyleData.custom_styles.ContainsKey(newStyleName))
                {
                    customStyleData.custom_styles[newStyleName] = new SPUM_CustomStyleData.CustomStyle
                    {
                        name = newStyleName,
                        description = newStyleDescription,
                        elements = new List<string>(),
                        negative = new List<string>()
                    };
                    tagManager.customStyleData = customStyleData;
                    SaveCustomStyleData(customStyleData);
                    editingStyleName = "";
                }
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                editingStyleName = "";
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        // Display all custom styles
        var stylesToRemove = new List<string>();
        foreach (var style in customStyleData.custom_styles)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            if (editingStyleName == style.Key && editingStyleName != "new_style")
            {
                // Edit mode
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("ID:", GUILayout.Width(30));
                EditorGUILayout.LabelField(style.Key, EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                style.Value.name = EditorGUILayout.TextField("Name (Korean):", style.Value.name);
                style.Value.description = EditorGUILayout.TextField("Description:", style.Value.description);
                
                EditorGUILayout.Space(5);
                
                // Elements
                GUI.color = new Color(0.7f, 0.9f, 0.7f); // Light green
                EditorGUILayout.LabelField("Style Elements:", EditorStyles.miniBoldLabel);
                GUI.color = Color.white;
                if (style.Value.elements == null)
                    style.Value.elements = new List<string>();
                DrawTagListWithSearch(style.Value.elements, $"style_{style.Key}_elements", "style_element");
                
                EditorGUILayout.Space(5);
                
                // Negative tags removed - Custom styles still use negative tags
                GUI.color = new Color(1f, 0.3f, 0.3f); // Red
                EditorGUILayout.LabelField("Negative Tags:", EditorStyles.miniBoldLabel);
                GUI.color = Color.white;
                if (style.Value.negative == null)
                    style.Value.negative = new List<string>();
                DrawTagButtonList(style.Value.negative, true, $"style_{style.Key}_negative", "style_element");
                
                EditorGUILayout.Space(5);
                
                // Save/Cancel buttons
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save", GUILayout.Width(60)))
                {
                    SaveCustomStyleData(customStyleData);
                    editingStyleName = "";
                }
                if (GUILayout.Button("Cancel", GUILayout.Width(60)))
                {
                    editingStyleName = "";
                    tagManager.LoadCustomStyleData(); // Reload to discard changes
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // View mode
                // Style header
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{style.Value.name} ({style.Key})", EditorStyles.boldLabel);
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    editingStyleName = style.Key;
                }
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Style", 
                        $"Are you sure you want to delete the style '{style.Value.name}'?", "Yes", "No"))
                    {
                        stylesToRemove.Add(style.Key);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // Style description
                if (!string.IsNullOrEmpty(style.Value.description))
                {
                    EditorGUILayout.LabelField(style.Value.description, EditorStyles.wordWrappedMiniLabel);
                }
                
                // Elements
                if (style.Value.elements != null && style.Value.elements.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Elements:", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                    GUI.color = new Color(0.7f, 0.9f, 0.7f); // Light green
                    DrawCompactTagList(style.Value.elements);
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
                
                // Negative tags
                if (style.Value.negative != null && style.Value.negative.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Negative:", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                    GUI.color = new Color(1f, 0.3f, 0.3f); // Red
                    DrawCompactTagList(style.Value.negative, true);
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        // Remove styles marked for deletion
        foreach (var styleKey in stylesToRemove)
        {
            customStyleData.custom_styles.Remove(styleKey);
        }
        if (stylesToRemove.Count > 0)
        {
            tagManager.customStyleData = customStyleData;
            SaveCustomStyleData(customStyleData);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// Draw tag list with semantic search functionality
    void DrawTagListWithSearch(List<string> tags, string searchKey, string category)
    {
        // Display existing tags in a compact grid layout
        if (tags.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(60)); // Indent
            EditorGUILayout.BeginVertical();
            
            float maxWidth = EditorGUIUtility.currentViewWidth - 100;
            float currentWidth = 0;
            List<string> tagsToRemove = new List<string>();
            
            EditorGUILayout.BeginHorizontal();
            
            foreach (var tag in tags)
            {
                float tagWidth = GUI.skin.label.CalcSize(new GUIContent(tag)).x + 50;
                
                if (currentWidth + tagWidth > maxWidth && currentWidth > 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    currentWidth = 0;
                }
                
                EditorGUILayout.BeginHorizontal(GUILayout.Width(tagWidth));
                if (GUILayout.Button(tag, tagStyle))
                {
                    // Tag click action if needed
                }
                if (GUILayout.Button("x", removeButtonStyle))
                {
                    tagsToRemove.Add(tag);
                }
                EditorGUILayout.EndHorizontal();
                
                currentWidth += tagWidth;
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            // Remove tags marked for deletion
            foreach (var tag in tagsToRemove)
            {
                tags.Remove(tag);
            }
        }
        
        EditorGUILayout.Space(5);
        
        // Search box
        EditorGUILayout.BeginHorizontal();
        
        if (!searchTerms.ContainsKey(searchKey))
            searchTerms[searchKey] = "";
        
        // Real-time search
        EditorGUI.BeginChangeCheck();
        searchTerms[searchKey] = EditorGUILayout.TextField("Search:", searchTerms[searchKey]);
        
        if (EditorGUI.EndChangeCheck())
        {
            if (!string.IsNullOrEmpty(searchTerms[searchKey]))
            {
                searchResults[searchKey] = SearchTags(searchTerms[searchKey], category, 50);
            }
            else
            {
                searchResults.Remove(searchKey);
            }
        }
        
        // Direct add button
        if (GUILayout.Button("Add", GUILayout.Width(40)))
        {
            if (!string.IsNullOrEmpty(searchTerms[searchKey]) && !tags.Contains(searchTerms[searchKey]))
            {
                tags.Add(searchTerms[searchKey]);
                searchTerms[searchKey] = "";
                searchResults.Remove(searchKey);
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Display search results
        if (searchResults.ContainsKey(searchKey) && searchResults[searchKey].Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Search Results ({searchResults[searchKey].Count})", EditorStyles.miniBoldLabel);
            
            string scrollKey = searchKey + "_scroll";
            if (!searchScrollPositions.ContainsKey(scrollKey))
                searchScrollPositions[scrollKey] = Vector2.zero;
            
            searchScrollPositions[scrollKey] = EditorGUILayout.BeginScrollView(
                searchScrollPositions[scrollKey], 
                GUILayout.Height(Mathf.Min(150, searchResults[searchKey].Count * 25))
            );
            
            foreach (var result in searchResults[searchKey])
            {
                if (tags.Contains(result))
                    continue;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(result, EditorStyles.miniLabel);
                if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    tags.Add(result);
                    searchTerms[searchKey] = "";
                    searchResults.Remove(searchKey);
                    searchScrollPositions.Remove(scrollKey);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
    
    /// <summary>
    /// Search tags based on category and search term
    /// </summary>
    List<string> SearchTags(string searchTerm, string category, int maxResults = 50)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return new List<string>();
        
        var results = new List<string>();
        var searchLower = searchTerm.ToLower();
        
        // Get all available tags based on category
        var availableTags = GetAvailableTagsByCategory(category);
        
        // First pass: exact prefix matches
        foreach (var tag in availableTags)
        {
            if (tag.ToLower().StartsWith(searchLower))
            {
                results.Add(tag);
                if (results.Count >= maxResults) break;
            }
        }
        
        // Second pass: contains matches
        if (results.Count < maxResults)
        {
            foreach (var tag in availableTags)
            {
                if (!results.Contains(tag) && tag.ToLower().Contains(searchLower))
                {
                    results.Add(tag);
                    if (results.Count >= maxResults) break;
                }
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Get available tags by category
    /// </summary>
    List<string> GetAvailableTagsByCategory(string category)
    {
        var tags = new HashSet<string>();
        
        switch (category.ToLower())
        {
            case "style_element":
                // Get all style elements from SPUM_Style_Elements.json
                var styleElementsData = tagManager.GetStyleElementsData();
                if (styleElementsData != null && styleElementsData.categories != null)
                {
                    foreach (var categoryData in styleElementsData.categories.Values)
                    {
                        foreach (var element in categoryData.styles)
                        {
                            tags.Add(element.id);
                        }
                    }
                }
                break;
                
            case "part":
                // Get all unique parts from character data
                foreach (var item in tagManager.GetAllCharacterData())
                {
                    if (!string.IsNullOrEmpty(item.Part))
                        tags.Add(item.Part);
                }
                break;
                
            case "type":
                // Get all unique types from character data
                foreach (var item in tagManager.GetAllCharacterData())
                {
                    if (!string.IsNullOrEmpty(item.Type))
                        tags.Add(item.Type);
                }
                // Add common weapon types
                tags.UnionWith(new[] { "sword", "axe", "hammer", "mace", "spear", "shield", "bow", "staff", "wand", "dagger" });
                // Add common armor types  
                tags.UnionWith(new[] { "plate", "chain_mail", "leather", "robe", "tunic", "helmet", "shoulder", "bracer", "pants", "shoes", "cape", "belt" });
                break;
                
            case "class":
                // Add common class tags
                tags.UnionWith(new[] { "melee", "ranged", "physical", "magical", "damage", "tank", "healing", "support" });
                // Get all unique class tags from character data
                foreach (var item in tagManager.GetAllCharacterData())
                {
                    foreach (var classTag in item.Class)
                    {
                        if (!string.IsNullOrEmpty(classTag))
                            tags.Add(classTag);
                    }
                }
                break;
                
            case "style":
                // Get all style names from style data
                var styleData = tagManager.GetStyleData();
                if (styleData != null && styleData.categories != null)
                {
                    foreach (var styleCategory in styleData.categories.Values)
                    {
                        foreach (var style in styleCategory.styles)
                        {
                            tags.Add(style.id);
                        }
                        // Style tags removed from SPUM_Style_Elements.json
                    }
                }
                // Get all unique style tags from character data
                foreach (var item in tagManager.GetAllCharacterData())
                {
                    foreach (var styleTag in item.Style)
                    {
                        if (!string.IsNullOrEmpty(styleTag))
                            tags.Add(styleTag);
                    }
                }
                break;
                
            case "all":
            default:
                // Include all tags from all categories
                // Get type tags
                foreach (var item in tagManager.GetAllCharacterData())
                {
                    if (!string.IsNullOrEmpty(item.Type))
                        tags.Add(item.Type);
                }
                tags.UnionWith(new[] { "sword", "axe", "hammer", "mace", "spear", "shield", "bow", "staff", "wand", "dagger" });
                tags.UnionWith(new[] { "plate", "chain_mail", "leather", "robe", "tunic", "helmet", "shoulder", "bracer", "pants", "shoes", "cape", "belt" });
                
                // Get class tags
                tags.UnionWith(new[] { "melee", "ranged", "physical", "magical", "damage", "tank", "healing", "support", "heavy", "light" });
                foreach (var item in tagManager.GetAllCharacterData())
                {
                    foreach (var classTag in item.Class)
                    {
                        if (!string.IsNullOrEmpty(classTag))
                            tags.Add(classTag);
                    }
                }
                
                // Get style tags
                var allStyleData = tagManager.GetStyleData();
                if (allStyleData != null && allStyleData.categories != null)
                {
                    foreach (var styleCategory in allStyleData.categories.Values)
                    {
                        foreach (var style in styleCategory.styles)
                        {
                            tags.Add(style.id);
                        }
                        // Style tags removed from SPUM_Style_Elements.json
                    }
                }
                foreach (var item in tagManager.GetAllCharacterData())
                {
                    foreach (var styleTag in item.Style)
                    {
                        if (!string.IsNullOrEmpty(styleTag))
                            tags.Add(styleTag);
                    }
                }
                break;
        }
        
        return tags.OrderBy(t => t).ToList();
    }
    
    /// <summary>
    /// Draw compact tag list for view mode
    /// </summary>
    void DrawTagButtonList(List<string> tags, bool isNegative = false, string searchKey = null, string category = "all")
    {
        if (tags == null) tags = new List<string>();
        
        // Display existing tags in a compact grid layout
        if (tags.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(60)); // Indent
            EditorGUILayout.BeginVertical();
            
            float maxWidth = EditorGUIUtility.currentViewWidth - 100;
            float currentWidth = 0;
            List<string> tagsToRemove = new List<string>();
            
            EditorGUILayout.BeginHorizontal();
            
            foreach (var tag in tags)
            {
                float tagWidth = GUI.skin.label.CalcSize(new GUIContent(tag)).x + 50;
                
                if (currentWidth + tagWidth > maxWidth && currentWidth > 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    currentWidth = 0;
                }
                
                EditorGUILayout.BeginHorizontal(GUILayout.Width(tagWidth));
                
                // Apply color based on tag type
                var originalColor = GUI.backgroundColor;
                if (isNegative)
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // Light red for negative
                else
                    GUI.backgroundColor = new Color(0.8f, 0.9f, 0.8f); // Light green for positive
                    
                if (GUILayout.Button(tag, tagStyle))
                {
                    // Tag click action if needed
                }
                GUI.backgroundColor = Color.white;
                
                if (GUILayout.Button("x", removeButtonStyle))
                {
                    tagsToRemove.Add(tag);
                }
                EditorGUILayout.EndHorizontal();
                
                currentWidth += tagWidth;
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            // Remove tags marked for deletion
            foreach (var tag in tagsToRemove)
            {
                tags.Remove(tag);
            }
        }
        
        EditorGUILayout.Space(5);
        
        // Search box
        EditorGUILayout.BeginHorizontal();
        
        if (searchKey == null)
        {
            searchKey = $"tagbutton_{GetHashCode()}_{category}";
        }
        
        if (!searchTerms.ContainsKey(searchKey))
            searchTerms[searchKey] = "";
        
        // Real-time search
        EditorGUI.BeginChangeCheck();
        searchTerms[searchKey] = EditorGUILayout.TextField("Search:", searchTerms[searchKey]);
        
        if (EditorGUI.EndChangeCheck())
        {
            if (!string.IsNullOrEmpty(searchTerms[searchKey]))
            {
                searchResults[searchKey] = SearchTags(searchTerms[searchKey], category, 50);
            }
            else
            {
                searchResults.Remove(searchKey);
            }
        }
        
        // Direct add button
        if (GUILayout.Button("Add", GUILayout.Width(40)))
        {
            if (!string.IsNullOrEmpty(searchTerms[searchKey]) && !tags.Contains(searchTerms[searchKey]))
            {
                tags.Add(searchTerms[searchKey]);
                searchTerms[searchKey] = "";
                searchResults.Remove(searchKey);
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Display search results
        if (searchResults.ContainsKey(searchKey) && searchResults[searchKey].Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Search Results ({searchResults[searchKey].Count})", EditorStyles.miniBoldLabel);
            
            string scrollKey = searchKey + "_scroll";
            if (!searchScrollPositions.ContainsKey(scrollKey))
                searchScrollPositions[scrollKey] = Vector2.zero;
            
            searchScrollPositions[scrollKey] = EditorGUILayout.BeginScrollView(
                searchScrollPositions[scrollKey], 
                GUILayout.Height(Mathf.Min(150, searchResults[searchKey].Count * 25))
            );
            
            foreach (var result in searchResults[searchKey])
            {
                if (tags.Contains(result))
                    continue;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(result, EditorStyles.miniLabel);
                if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    tags.Add(result);
                    searchTerms[searchKey] = "";
                    searchResults.Remove(searchKey);
                    searchScrollPositions.Remove(scrollKey);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
    
    void DrawTagListWithAddRemove(List<string> tags, string uniqueId)
    {
        if (tags == null) return;
        
        EditorGUILayout.BeginVertical();
        
        // Display existing tags
        for (int i = 0; i < tags.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(tags[i], GUILayout.MinWidth(100));
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                tags.RemoveAt(i);
                break;
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
        
        // Add new tag input
        if (!newTagInputs.ContainsKey(uniqueId))
            newTagInputs[uniqueId] = "";
            
        EditorGUILayout.BeginHorizontal();
        newTagInputs[uniqueId] = EditorGUILayout.TextField(newTagInputs[uniqueId]);
        if (GUILayout.Button("Add", GUILayout.Width(50)))
        {
            if (!string.IsNullOrEmpty(newTagInputs[uniqueId]) && !tags.Contains(newTagInputs[uniqueId]))
            {
                tags.Add(newTagInputs[uniqueId]);
                newTagInputs[uniqueId] = "";
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawCompactTagList(List<string> tags, bool isNegative = false)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(60)); // Indent
        EditorGUILayout.BeginVertical();
        
        float maxWidth = EditorGUIUtility.currentViewWidth - 100;
        float currentWidth = 0;
        
        EditorGUILayout.BeginHorizontal();
        
        // Set color based on tag type
        Color originalColor = GUI.color;
        if (!isNegative)
        {
            GUI.color = new Color(0.3f, 0.8f, 0.3f); // Green for positive tags
        }
        
        foreach (var tag in tags)
        {
            float tagWidth = GUI.skin.label.CalcSize(new GUIContent(tag)).x + 20;
            
            if (currentWidth + tagWidth > maxWidth && currentWidth > 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                currentWidth = 0;
            }
            
            if (GUILayout.Button(tag, tagStyle, GUILayout.Width(tagWidth)))
            {
                // Tag click action if needed
            }
            
            currentWidth += tagWidth + 5;
        }
        
        GUI.color = originalColor;
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// Create a colored texture for button backgrounds
    /// </summary>
    Texture2D MakeColorTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// Get quality color based on quality name
    /// </summary>
    Color GetQualityColor(string quality)
    {
        switch (quality.ToLower())
        {
            case "broken": return new Color(0.5f, 0.5f, 0.5f); // Gray
            case "common": return new Color(0.8f, 0.8f, 0.8f); // Light Gray
            case "rare": return new Color(0.3f, 0.6f, 1f); // Blue
            case "epic": return new Color(0.7f, 0.3f, 1f); // Purple
            case "legendary": return new Color(1f, 0.7f, 0.3f); // Orange
            case "mythic": return new Color(1f, 0.3f, 0.3f); // Red
            default: return Color.white;
        }
    }
    
    Color GetCategoryColor(string category)
    {
        switch (category)
        {
            case "weapon_melee":
            case "weapon_ranged":
            case "weapon_magic":
                return new Color(1f, 0.6f, 0.4f); // Orange for weapons
                
            case "armor_heavy":
            case "armor_light":
            case "armor_cloth":
                return new Color(0.4f, 0.6f, 1f); // Blue for armor
                
            case "helmet":
                return new Color(0.5f, 0.7f, 0.9f); // Light blue for helmet
                
            case "defensive":
                return new Color(0.6f, 0.8f, 0.6f); // Green for defensive
                
            case "accessory":
                return new Color(0.8f, 0.6f, 0.8f); // Purple for accessories
                
            case "expression":
                return new Color(1f, 0.6f, 0.6f); // Pink for expression
                
            case "facial_hair":
                return new Color(0.8f, 0.7f, 0.5f); // Brown for facial hair
                
            case "hairstyle":
                return new Color(0.9f, 0.5f, 0.9f); // Violet for hairstyle
                
            default:
                return Color.gray;
        }
    }
    
    /// <summary>
    /// Extract quality tag from item's style array
    /// </summary>
    string GetItemQuality(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        if (item == null || item.Style == null || item.Style.Length == 0)
            return null;
        
        string[] qualityTags = { "broken", "common", "rare", "epic", "legendary", "mythic" };
        
        foreach (var style in item.Style)
        {
            if (!string.IsNullOrEmpty(style))
            {
                string styleLower = style.ToLower();
                foreach (var quality in qualityTags)
                {
                    if (styleLower == quality)
                        return quality;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Get element color based on element name
    /// </summary>
    Color GetElementColor(string element)
    {
        switch (element.ToLower())
        {
            case "fire": return new Color(1f, 0.3f, 0.3f); // Red
            case "water": return new Color(0.3f, 0.6f, 1f); // Blue
            case "earth": return new Color(0.6f, 0.4f, 0.2f); // Brown
            case "wind": return new Color(0.5f, 1f, 0.5f); // Light Green
            case "light": return new Color(1f, 1f, 0.5f); // Yellow
            case "dark": return new Color(0.3f, 0.2f, 0.5f); // Dark Purple
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// Extract element tags from item's style array
    /// </summary>
    List<string> GetItemElements(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        if (item == null || item.Style == null || item.Style.Length == 0)
            return new List<string>();
        
        string[] elementTags = { "fire", "water", "earth", "wind", "light", "dark" };
        var elements = new List<string>();
        
        foreach (var style in item.Style)
        {
            if (!string.IsNullOrEmpty(style))
            {
                string styleLower = style.ToLower();
                foreach (var element in elementTags)
                {
                    if (styleLower == element)
                    {
                        elements.Add(element);
                    }
                }
            }
        }
        
        return elements;
    }
    
    /// <summary>
    /// Draw a simple icon for an element
    /// </summary>
    void DrawElementIcon(Rect rect, string element)
    {
        Color elementColor = GetElementColor(element);
        Color oldColor = GUI.color;
        GUI.color = elementColor;
        
        switch (element.ToLower())
        {
            case "fire":
                // Draw triangle for fire
                DrawTriangle(rect, true);
                break;
            case "water":
                // Draw circle for water
                DrawCircle(rect);
                break;
            case "earth":
                // Draw square for earth
                DrawSquare(rect);
                break;
            case "wind":
                // Draw curved lines for wind
                DrawWind(rect);
                break;
            case "light":
                // Draw star for light
                DrawStar(rect);
                break;
            case "dark":
                // Draw filled circle for dark
                DrawCircle(rect, true);
                break;
        }
        
        GUI.color = oldColor;
    }
    
    // Simple shape drawing methods
    void DrawTriangle(Rect rect, bool pointUp = true)
    {
        // Draw triangle for fire - use three small rectangles to form triangle shape
        if (pointUp)
        {
            // Fire flame shape
            Rect top = new Rect(rect.center.x - 1, rect.y, 2, rect.height * 0.5f);
            Rect left = new Rect(rect.x, rect.center.y, rect.width * 0.5f, rect.height * 0.5f);
            Rect right = new Rect(rect.center.x, rect.center.y, rect.width * 0.5f, rect.height * 0.5f);
            
            GUI.DrawTexture(top, EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(left, EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(right, EditorGUIUtility.whiteTexture);
        }
        else
        {
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
        }
    }
    
    void DrawCircle(Rect rect, bool filled = false)
    {
        // Draw circle - make it smaller for better circle appearance
        Rect circleRect = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
        
        if (filled)
        {
            // Dark element - filled circle
            GUI.DrawTexture(circleRect, EditorGUIUtility.whiteTexture);
        }
        else
        {
            // Water element - hollow circle (draw as border)
            Rect border = new Rect(circleRect.x - 1, circleRect.y - 1, circleRect.width + 2, circleRect.height + 2);
            GUI.DrawTexture(border, EditorGUIUtility.whiteTexture);
            
            Color oldColor = GUI.color;
            GUI.color = new Color(0.12f, 0.12f, 0.12f); // Editor background color
            GUI.DrawTexture(circleRect, EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;
        }
    }
    
    void DrawSquare(Rect rect)
    {
        // Earth element - solid square
        Rect squareRect = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
        GUI.DrawTexture(squareRect, EditorGUIUtility.whiteTexture);
    }
    
    void DrawWind(Rect rect)
    {
        // Wind element - wavy lines
        Rect line1 = new Rect(rect.x + 1, rect.y + rect.height * 0.2f, rect.width - 2, 2);
        Rect line2 = new Rect(rect.x, rect.y + rect.height * 0.5f, rect.width, 2);
        Rect line3 = new Rect(rect.x + 1, rect.y + rect.height * 0.8f, rect.width - 2, 2);
        
        GUI.DrawTexture(line1, EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(line2, EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(line3, EditorGUIUtility.whiteTexture);
    }
    
    void DrawStar(Rect rect)
    {
        // Light element - star/sun shape with 4 points
        float centerX = rect.center.x;
        float centerY = rect.center.y;
        float size = rect.width * 0.4f;
        
        // Main cross
        Rect horizontal = new Rect(rect.x, centerY - 1, rect.width, 2);
        Rect vertical = new Rect(centerX - 1, rect.y, 2, rect.height);
        
        // Diagonal cross (smaller)
        Rect diag1 = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, 2);
        Rect diag2 = new Rect(rect.x + 2, rect.yMax - 4, rect.width - 4, 2);
        
        GUI.DrawTexture(horizontal, EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(vertical, EditorGUIUtility.whiteTexture);
        
        // Add small diagonal elements for star effect
        Matrix4x4 matrixBackup = GUI.matrix;
        GUIUtility.RotateAroundPivot(45, rect.center);
        GUI.DrawTexture(new Rect(rect.x + 3, centerY - 1, rect.width - 6, 2), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 1, rect.y + 3, 2, rect.height - 6), EditorGUIUtility.whiteTexture);
        GUI.matrix = matrixBackup;
    }
    
    /// <summary>
    /// Get filtered items based on selected filters
    /// </summary>
    List<SPUM_ImprovedTagManager.ImprovedCharacterDataItem> GetFilteredItemsForGenerator()
    {
        // Generate a hash of current filter state
        string currentFilterHash = GenerateFilterHash();
        
        // Check if cache is valid and filter state hasn't changed
        if (filteredItemsCacheValid && cachedFilteredItems != null && currentFilterHash == lastFilterHash)
        {
            return cachedFilteredItems;
        }
        
        var allData = tagManager.GetAllCharacterData();
        
        // Always use AND mode: Apply filters sequentially
        cachedFilteredItems = GetFilteredItemsANDMode(allData);
        lastFilterHash = currentFilterHash;
        filteredItemsCacheValid = true;
        
        return cachedFilteredItems;
    }
    
    string GenerateFilterHash()
    {
        // Create a simple hash based on filter selections
        var hash = new System.Text.StringBuilder();
        hash.Append(string.Join(",", filterSelectedAddons));
        hash.Append("|");
        hash.Append(string.Join(",", filterSelectedThemes));
        hash.Append("|");
        hash.Append(string.Join(",", filterSelectedRaces));
        hash.Append("|");
        hash.Append(string.Join(",", filterSelectedGenders));
        hash.Append("|");
        hash.Append(string.Join(",", filterSelectedClasses));
        hash.Append("|");
        hash.Append(string.Join(",", filterSelectedStyles));
        hash.Append("|");
        hash.Append(string.Join(",", filterSelectedParts));
        hash.Append("|");
        hash.Append(string.Join(",", filterSelectedStyleElements));
        hash.Append("|");
        hash.Append(filterSearchTerm);
        return hash.ToString();
    }
    
    bool CheckRaceGenderMatch(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        // Handle default modes for empty selections
        bool raceMatch;
        if (filterSelectedRaces.Count == 0)
        {
            raceMatch = raceDefaultSelectAll;
        }
        else
        {
            raceMatch = CheckRaceMatch(item);
        }
        
        bool genderMatch;
        if (filterSelectedGenders.Count == 0)
        {
            genderMatch = genderDefaultSelectAll;
        }
        else
        {
            genderMatch = CheckGenderMatch(item);
        }
        
        if (characterAttributesCombineAND)
        {
            return raceMatch && genderMatch;
        }
        else
        {
            // OR mode: Check only filters that apply to this part
            bool passesAnyFilter = false;
            
            // Race filter only applies to Body parts
            if (filterSelectedRaces.Count > 0 && item.Part == "Body")
            {
                passesAnyFilter |= CheckRaceMatch(item);
            }
            
            // Gender filter applies to Hair, FaceHair, Helmet, Armor, Cloth parts
            if (filterSelectedGenders.Count > 0 && 
                (item.Part == "Hair" || item.Part == "FaceHair" || 
                 item.Part == "Helmet" || item.Part == "Armor" || item.Part == "Cloth"))
            {
                passesAnyFilter |= CheckGenderMatch(item);
            }
            
            // If no applicable filters are active, use default select modes
            if (!passesAnyFilter)
            {
                bool hasApplicableFilter = false;
                
                if (filterSelectedRaces.Count > 0 && item.Part == "Body") hasApplicableFilter = true;
                if (filterSelectedGenders.Count > 0 && 
                    (item.Part == "Hair" || item.Part == "FaceHair" || 
                     item.Part == "Helmet" || item.Part == "Armor" || item.Part == "Cloth")) hasApplicableFilter = true;
                
                if (!hasApplicableFilter)
                {
                    // No filters apply to this part, so check default modes
                    if (item.Part == "Body")
                        return raceDefaultSelectAll;
                    else if (item.Part == "Hair" || item.Part == "FaceHair" || 
                             item.Part == "Helmet" || item.Part == "Armor" || item.Part == "Cloth")
                        return genderDefaultSelectAll;
                    else
                        return true; // Parts not affected by race/gender filters
                }
            }
            
            return passesAnyFilter;
        }
    }
    
    // Keep old method for backward compatibility but it's not used anymore
    bool CheckCharacterAttributesMatch(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        // Handle default modes for empty selections
        bool themeMatch;
        if (filterSelectedThemes.Count == 0)
        {
            themeMatch = themeDefaultSelectAll; // If defaultSelectAll=true (All mode), pass all. If false (None mode), match none.
        }
        else
        {
            themeMatch = CheckThemeMatch(item);
        }
        
        bool raceMatch;
        if (filterSelectedRaces.Count == 0)
        {
            raceMatch = raceDefaultSelectAll; // If defaultSelectAll=true (All mode), pass all. If false (None mode), match none.
        }
        else
        {
            raceMatch = CheckRaceMatch(item);
        }
        
        bool genderMatch;
        if (filterSelectedGenders.Count == 0)
        {
            genderMatch = genderDefaultSelectAll; // If defaultSelectAll=true (All mode), pass all. If false (None mode), match none.
        }
        else
        {
            genderMatch = CheckGenderMatch(item);
        }
        
        if (characterAttributesCombineAND)
        {
            return themeMatch && raceMatch && genderMatch;
        }
        else
        {
            // OR mode: Check only filters that apply to this part
            bool passesAnyFilter = false;
            
            // Theme filter applies to all parts
            if (filterSelectedThemes.Count > 0)
            {
                passesAnyFilter |= CheckThemeMatch(item);
            }
            
            // Race filter only applies to Body parts
            if (filterSelectedRaces.Count > 0 && item.Part == "Body")
            {
                passesAnyFilter |= CheckRaceMatch(item);
            }
            
            // Gender filter applies to Hair, FaceHair, Helmet, Armor, Cloth parts
            if (filterSelectedGenders.Count > 0 && 
                (item.Part == "Hair" || item.Part == "FaceHair" || 
                 item.Part == "Helmet" || item.Part == "Armor" || item.Part == "Cloth"))
            {
                passesAnyFilter |= CheckGenderMatch(item);
            }
            
            // If no applicable filters are active, use default select modes
            if (!passesAnyFilter)
            {
                bool hasApplicableFilter = false;
                
                if (filterSelectedThemes.Count > 0) hasApplicableFilter = true;
                if (filterSelectedRaces.Count > 0 && item.Part == "Body") hasApplicableFilter = true;
                if (filterSelectedGenders.Count > 0 && 
                    (item.Part == "Hair" || item.Part == "FaceHair" || 
                     item.Part == "Helmet" || item.Part == "Armor" || item.Part == "Cloth")) hasApplicableFilter = true;
                
                if (!hasApplicableFilter)
                {
                    // No filters apply to this part, so check default modes
                    if (item.Part == "Body")
                        return raceDefaultSelectAll;
                    else if (item.Part == "Hair" || item.Part == "FaceHair" || 
                             item.Part == "Helmet" || item.Part == "Armor" || item.Part == "Cloth")
                        return genderDefaultSelectAll;
                    else
                        return themeDefaultSelectAll;
                }
            }
            
            return passesAnyFilter;
        }
    }
    
    bool CheckCombatVisualStyleMatch(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        bool classMatch = filterSelectedClasses.Count == 0 || CheckClassMatch(item);
        bool styleMatch = filterSelectedStyles.Count == 0 || CheckCustomStyleMatch(item);
        bool styleElementsMatch = filterSelectedStyleElements.Count == 0 || CheckStyleElementsMatch(item);
        
        if (combatVisualStyleCombineAND)
        {
            return classMatch && styleMatch && styleElementsMatch;
        }
        else
        {
            // OR mode: at least one filter must be active and match
            bool hasActiveFilter = filterSelectedClasses.Count > 0 || filterSelectedStyles.Count > 0 || filterSelectedStyleElements.Count > 0;
            if (!hasActiveFilter) return true;
            
            return (filterSelectedClasses.Count > 0 && CheckClassMatch(item)) ||
                   (filterSelectedStyles.Count > 0 && CheckCustomStyleMatch(item)) ||
                   (filterSelectedStyleElements.Count > 0 && CheckStyleElementsMatch(item));
        }
    }
    
    bool CheckThemeMatch(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        if (item.Theme == null || item.Theme.Length == 0) return false;
        
        if (themeFilterUseAND)
        {
            return filterSelectedThemes.All(selectedTheme => item.Theme.Contains(selectedTheme));
        }
        else
        {
            return item.Theme.Any(t => filterSelectedThemes.Contains(t));
        }
    }
    
    bool CheckRaceMatch(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        // Part가 비어있는 경우 포함
        if (string.IsNullOrEmpty(item.Part)) return true;
        
        // Race filter only applies to Body parts
        if (item.Part != "Body") return true;
        
        if (string.IsNullOrEmpty(item.Race)) return true;
        
        if (raceFilterUseAND)
        {
            return filterSelectedRaces.All(selectedRace => item.Race == selectedRace);
        }
        else
        {
            return filterSelectedRaces.Contains(item.Race);
        }
    }
    
    bool CheckGenderMatch(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        // Part가 비어있는 경우 포함
        if (string.IsNullOrEmpty(item.Part)) return true;
        
        // Gender filter applies to Hair, FaceHair, Helmet, Armor, Cloth parts
        if (item.Part != "Hair" && item.Part != "FaceHair" && 
            item.Part != "Helmet" && item.Part != "Armor" && item.Part != "Cloth") return true;
        
        if (string.IsNullOrEmpty(item.Gender)) return true;
        
        if (genderFilterUseAND)
        {
            return filterSelectedGenders.All(selectedGender => item.Gender == selectedGender);
        }
        else
        {
            return filterSelectedGenders.Contains(item.Gender);
        }
    }
    
    bool CheckClassMatch(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        // Part가 비어있는 경우 포함
        if (string.IsNullOrEmpty(item.Part)) return true;
        
        // Class filter only applies to parts other than Body, Eye, Hair, and FaceHair
        if (item.Part == "Body" || item.Part == "Eye" || item.Part == "Hair" || item.Part == "FaceHair") return true;
        
        var classData = tagManager.GetClassData();
        
        if (classFilterUseAND)
        {
            // AND mode: item must match ALL selected classes
            foreach (var selectedClass in filterSelectedClasses)
            {
                if (classData != null && classData.combat_classes.ContainsKey(selectedClass))
                {
                    var classInfo = classData.combat_classes[selectedClass];
                    
                    bool typeMatch = string.IsNullOrEmpty(item.Type) || 
                                   (classInfo.type != null && classInfo.type.Contains(item.Type));
                    
                    bool classMatch = item.Class.Length == 0 || 
                                    (classInfo.class_tags != null && item.Class.Any(c => classInfo.class_tags.Contains(c)));
                    
                    bool typeNegativeCheck = classInfo.type_negative == null || classInfo.type_negative.Count == 0 ||
                                           string.IsNullOrEmpty(item.Type) ||
                                           !classInfo.type_negative.Contains(item.Type);
                    
                    bool classNegativeCheck = classInfo.class_negative == null || classInfo.class_negative.Count == 0 ||
                                            item.Style.Length == 0 ||
                                            !item.Style.Any(s => classInfo.class_negative.Contains(s));
                    
                    if (!(typeMatch && classMatch && typeNegativeCheck && classNegativeCheck))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        else
        {
            // OR mode: item must match ANY selected class
            foreach (var selectedClass in filterSelectedClasses)
            {
                if (classData != null && classData.combat_classes.ContainsKey(selectedClass))
                {
                    var classInfo = classData.combat_classes[selectedClass];
                    
                    bool typeMatch = string.IsNullOrEmpty(item.Type) || 
                                   (classInfo.type != null && classInfo.type.Contains(item.Type));
                    
                    bool classMatch = item.Class.Length == 0 || 
                                    (classInfo.class_tags != null && item.Class.Any(c => classInfo.class_tags.Contains(c)));
                    
                    bool typeNegativeCheck = classInfo.type_negative == null || classInfo.type_negative.Count == 0 ||
                                           string.IsNullOrEmpty(item.Type) ||
                                           !classInfo.type_negative.Contains(item.Type);
                    
                    bool classNegativeCheck = classInfo.class_negative == null || classInfo.class_negative.Count == 0 ||
                                            item.Style.Length == 0 ||
                                            !item.Style.Any(s => classInfo.class_negative.Contains(s));
                    
                    if (typeMatch && classMatch && typeNegativeCheck && classNegativeCheck)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
    
    bool CheckCustomStyleMatch(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        // Part가 비어있는 경우 포함
        if (string.IsNullOrEmpty(item.Part)) return true;
        
        // Style filter only applies to parts other than Body, Eye, Hair, and FaceHair
        if (item.Part == "Body" || item.Part == "Eye" || item.Part == "Hair" || item.Part == "FaceHair") return true;
        
        if (item.Style.Length == 0) return false;
        
        var customStyleData = tagManager.GetCustomStyleData();
        
        if (customStyleFilterUseAND)
        {
            // AND mode: item must match ALL selected custom styles
            foreach (var selectedStyle in filterSelectedStyles)
            {
                if (customStyleData != null && customStyleData.custom_styles.ContainsKey(selectedStyle))
                {
                    var customStyle = customStyleData.custom_styles[selectedStyle];
                    
                    if (customStyle.required_parts != null && customStyle.required_parts.Count > 0)
                    {
                        if (!customStyle.required_parts.Contains(item.Part))
                        {
                            return false;
                        }
                    }
                    
                    bool hasAnyElement = false;
                    if (customStyle.elements != null && customStyle.elements.Count > 0)
                    {
                        foreach (var element in customStyle.elements)
                        {
                            if (item.Style.Contains(element))
                            {
                                hasAnyElement = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        hasAnyElement = true;
                    }
                    
                    bool hasConflict = false;
                    if (customStyle.negative != null && customStyle.negative.Count > 0)
                    {
                        foreach (var negative in customStyle.negative)
                        {
                            if (item.Style.Contains(negative))
                            {
                                hasConflict = true;
                                break;
                            }
                        }
                    }
                    
                    if (!hasAnyElement || hasConflict)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        else
        {
            // OR mode: item must match ANY selected custom style
            foreach (var selectedStyle in filterSelectedStyles)
            {
                if (customStyleData != null && customStyleData.custom_styles.ContainsKey(selectedStyle))
                {
                    var customStyle = customStyleData.custom_styles[selectedStyle];
                    
                    if (customStyle.required_parts != null && customStyle.required_parts.Count > 0)
                    {
                        if (!customStyle.required_parts.Contains(item.Part))
                        {
                            continue;
                        }
                    }
                    
                    bool hasAnyElement = false;
                    if (customStyle.elements != null && customStyle.elements.Count > 0)
                    {
                        foreach (var element in customStyle.elements)
                        {
                            if (item.Style.Contains(element))
                            {
                                hasAnyElement = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        hasAnyElement = true;
                    }
                    
                    bool hasConflict = false;
                    if (customStyle.negative != null && customStyle.negative.Count > 0)
                    {
                        foreach (var negative in customStyle.negative)
                        {
                            if (item.Style.Contains(negative))
                            {
                                hasConflict = true;
                                break;
                            }
                        }
                    }
                    
                    if (hasAnyElement && !hasConflict)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
    
    bool CheckStyleElementsMatch(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item)
    {
        if (item.Style.Length == 0) return false;
        
        if (styleFilterUseAND)
        {
            return filterSelectedStyleElements.All(selectedElement => item.Style.Contains(selectedElement));
        }
        else
        {
            return item.Style.Any(s => filterSelectedStyleElements.Contains(s));
        }
    }
    
    List<SPUM_ImprovedTagManager.ImprovedCharacterDataItem> GetFilteredItemsANDMode(List<SPUM_ImprovedTagManager.ImprovedCharacterDataItem> allData)
    {
        var filtered = allData.AsEnumerable();
        
        // Apply Addon filter first (최상단 필터)
        if (filterSelectedAddons.Count > 0)
        {
            if (addonFilterUseAND)
            {
                filtered = filtered.Where(item => 
                    !string.IsNullOrEmpty(item.Addon) && 
                    filterSelectedAddons.All(selectedAddon => item.Addon == selectedAddon)
                );
            }
            else
            {
                filtered = filtered.Where(item => 
                    !string.IsNullOrEmpty(item.Addon) && 
                    filterSelectedAddons.Contains(item.Addon)
                );
            }
        }
        
        // Apply Race and Gender filters (without Theme)
        bool hasRaceGenderFilters = filterSelectedRaces.Count > 0 || filterSelectedGenders.Count > 0;
        if (hasRaceGenderFilters)
        {
            filtered = filtered.Where(item => CheckRaceGenderMatch(item));
        }
        
        // Apply Combat & Visual Style filters as a group
        bool hasCombatStyleFilters = filterSelectedClasses.Count > 0 || filterSelectedStyles.Count > 0 || filterSelectedStyleElements.Count > 0;
        if (hasCombatStyleFilters)
        {
            filtered = filtered.Where(item => CheckCombatVisualStyleMatch(item));
        }
        
        // Race가 선택된 경우 헬멧, FaceHair, Hair 특별 처리 - 심플한 방식
        if (filterSelectedRaces.Count > 0)
        {
            // Race가 있는데 선택된 Race와 다른 헬멧, FaceHair, Hair들만 제외
            filtered = filtered.Where(item => 
                (item.Part != "Helmet" && item.Part != "FaceHair" && item.Part != "Hair") ||  // 헬멧이나 FaceHair, Hair가 아니거나
                string.IsNullOrEmpty(item.Race) ||  // Race가 없거나
                filterSelectedRaces.Contains(item.Race)  // 선택된 Race와 일치하거나
            );
        }
        
        // Gender가 선택된 경우 Hair, FaceHair, Helmet, Armor, Cloth 특별 처리
        if (filterSelectedGenders.Count > 0)
        {
            // Gender가 있는데 선택된 Gender와 다른 Hair, FaceHair, Helmet, Armor, Cloth들만 제외
            filtered = filtered.Where(item => 
                (item.Part != "Hair" && item.Part != "FaceHair" && 
                 item.Part != "Helmet" && item.Part != "Armor" && item.Part != "Cloth") ||  // 해당 파츠가 아니거나
                string.IsNullOrEmpty(item.Gender) ||  // Gender가 없거나
                filterSelectedGenders.Contains(item.Gender)  // 선택된 Gender와 일치하거나
            );
        }
        
        // Apply Part filter separately (it's in Additional Filters section)
        if (filterSelectedParts.Count > 0)
        {
            if (partFilterUseAND)
            {
                filtered = filtered.Where(item => 
                    !string.IsNullOrEmpty(item.Part) && 
                    filterSelectedParts.All(selectedPart => item.Part == selectedPart)
                );
            }
            else
            {
                filtered = filtered.Where(item => 
                    !string.IsNullOrEmpty(item.Part) && 
                    filterSelectedParts.Contains(item.Part)
                );
            }
        }
        
        // Apply Theme filter last (applies to all parts)
        if (filterSelectedThemes.Count > 0)
        {
            filtered = filtered.Where(item => CheckThemeMatch(item));
        }
        
        return filtered.ToList();
    }
    
    
    /// <summary>
    /// Draw a part section with foldout
    /// </summary>
    void DrawPartSection(string partName, List<SPUM_ImprovedTagManager.ImprovedCharacterDataItem> items)
    {
        // DrawPartSection
        string foldoutKey = $"part_{partName}";
        if (!partFoldouts.ContainsKey(foldoutKey))
            partFoldouts[foldoutKey] = true;
        
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
        
        // Foldout header
        partFoldouts[foldoutKey] = EditorGUILayout.Foldout(partFoldouts[foldoutKey], 
            $"{partName} ({items.Count} items)", true);
        
        if (partFoldouts[foldoutKey])
        {
            // Display items in a grid layout
            // Use EditorGUIUtility.currentViewWidth to get the actual width of the current drawing area
            float currentViewWidth = EditorGUIUtility.currentViewWidth;
            float availableWidth = currentViewWidth - PANEL_WIDTH_OFFSET; // Use offset for manual adjustment
            int columns = Mathf.Max(1, (int)(availableWidth / 100)); // 120px per item (wider spacing for fewer items per row)
            int itemCount = 0;
            
            EditorGUILayout.BeginHorizontal();
            
            foreach (var item in items)
            {
                if (itemCount > 0 && itemCount % columns == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                
                DrawItemButton(item, partName);
                itemCount++;
            }
            
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
    
    /// <summary>
    /// Draw individual item button
    /// </summary>
    void DrawItemButton(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item, string partName)
    {
        bool isSelected = generatedCharacter != null && 
                         generatedCharacter.ContainsKey(partName) && 
                         generatedCharacter[partName].FileName == item.FileName;
        
        bool isEditing = editingItem != null && editingItem.FileName == item.FileName;
        
        // Get item quality for border color
        string quality = GetItemQuality(item);
        Color qualityColor = quality != null ? GetQualityColor(quality) : Color.clear;
        
        // Prepare tooltip
        string tooltip = item.FileName + "\n\n";
        if (!string.IsNullOrEmpty(item.Race)) tooltip += $"Race: {item.Race}\n";
        if (!string.IsNullOrEmpty(item.Gender)) tooltip += $"Gender: {item.Gender}\n";
        if (!string.IsNullOrEmpty(item.Type)) tooltip += $"Type: {item.Type}\n";
        if (item.Theme != null && item.Theme.Length > 0) 
            tooltip += $"Theme: {string.Join(", ", item.Theme)}\n";
        if (item.Class != null && item.Class.Length > 0) 
            tooltip += $"Class: {string.Join(", ", item.Class)}\n";
        if (item.Style != null && item.Style.Length > 0) 
            tooltip += $"Style: {string.Join(", ", item.Style)}";
        
        // Get cached image
        Texture2D thumbnail = GetCachedImage(item, partName);
        GUIContent buttonContent = new GUIContent(thumbnail ?? EditorGUIUtility.whiteTexture, tooltip);
        
        // Button style
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.padding = new RectOffset(1, 1, 1, 1); // 최소 패딩으로 이미지 공간 확보
        
        // Background color for selected items
        Color originalColor = GUI.backgroundColor;
        
        EditorGUILayout.BeginVertical(GUILayout.Width(60), GUILayout.MaxWidth(60));
        
        // Draw quality border if item has quality
        if (quality != null && qualityColor != Color.clear)
        {
            // Get the rect for the button
            Rect buttonRect = GUILayoutUtility.GetRect(50, 50, GUILayout.Width(50), GUILayout.Height(50));
            
            // Draw border (3 pixels thick)
            int borderWidth = 3;
            Rect borderRect = new Rect(buttonRect.x - borderWidth, buttonRect.y - borderWidth, 
                                     buttonRect.width + borderWidth * 2, buttonRect.height + borderWidth * 2);
            
            // Save current color
            Color oldColor = GUI.color;
            GUI.color = qualityColor;
            
            // Draw border using GUI.DrawTexture
            GUI.DrawTexture(borderRect, EditorGUIUtility.whiteTexture);
            
            // Restore color
            GUI.color = oldColor;
            
            // Set background color for selection/editing
            if (isEditing)
            {
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f); // Green for editing
            }
            else if (isSelected)
            {
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f); // Blue for selected
            }
            
            // Draw the button
            if (GUI.Button(buttonRect, buttonContent, buttonStyle))
            {
                // Set editing item for right panel
                // Selected item for editing
                editingItem = item;
                InitializeEditSelections();
                Repaint();
                
                // Log details
                // Item details
                // Part info
                if (item.Theme != null && item.Theme.Length > 0)
                    Debug.Log($"  Theme: {string.Join(", ", item.Theme)}");
                if (item.Class != null && item.Class.Length > 0)
                    Debug.Log($"  Class: {string.Join(", ", item.Class)}");
                if (item.Style != null && item.Style.Length > 0)
                    Debug.Log($"  Style: {string.Join(", ", item.Style)}");
            }
            
            // Draw element icons inside the button (bottom right)
            var elements = GetItemElements(item);
            if (elements.Count > 0)
            {
                float iconSize = 8f;
                float iconPadding = 2f;
                int maxIcons = 4;
                int iconsToShow = Mathf.Min(elements.Count, maxIcons);
                
                // Calculate starting position (bottom right)
                float startX = buttonRect.xMax - (iconSize + iconPadding) * iconsToShow - iconPadding;
                float startY = buttonRect.yMax - iconSize - iconPadding;
                
                // Draw each icon
                for (int i = 0; i < iconsToShow; i++)
                {
                    Rect iconRect = new Rect(startX + i * (iconSize + iconPadding), startY, iconSize, iconSize);
                    DrawElementIcon(iconRect, elements[i]);
                }
            }
        }
        else
        {
            // No quality border - draw button normally
            if (isEditing)
            {
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f); // Green for editing
            }
            else if (isSelected)
            {
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f); // Blue for selected
            }
            
            // Image button (50x50)
            Rect buttonRect = GUILayoutUtility.GetRect(50, 50, GUILayout.Width(50), GUILayout.Height(50));
            if (GUI.Button(buttonRect, buttonContent, buttonStyle))
            {
                // Set editing item for right panel
                // Selected item for editing
                editingItem = item;
                InitializeEditSelections();
                Repaint();
                
                // Log details
                // Item details
                // Part info
                if (item.Theme != null && item.Theme.Length > 0)
                    Debug.Log($"  Theme: {string.Join(", ", item.Theme)}");
                if (item.Class != null && item.Class.Length > 0)
                    Debug.Log($"  Class: {string.Join(", ", item.Class)}");
                if (item.Style != null && item.Style.Length > 0)
                    Debug.Log($"  Style: {string.Join(", ", item.Style)}");
            }
            
            // Draw element icons inside the button (bottom right)
            var elements = GetItemElements(item);
            if (elements.Count > 0)
            {
                float iconSize = 8f;
                float iconPadding = 2f;
                int maxIcons = 4;
                int iconsToShow = Mathf.Min(elements.Count, maxIcons);
                
                // Calculate starting position (bottom right)
                float startX = buttonRect.xMax - (iconSize + iconPadding) * iconsToShow - iconPadding;
                float startY = buttonRect.yMax - iconSize - iconPadding;
                
                // Draw each icon
                for (int i = 0; i < iconsToShow; i++)
                {
                    Rect iconRect = new Rect(startX + i * (iconSize + iconPadding), startY, iconSize, iconSize);
                    DrawElementIcon(iconRect, elements[i]);
                }
            }
        }
        
        
        // File name label (truncated if too long)
        string displayName = item.FileName;
        if (displayName.Length > 15)
        {
            displayName = displayName.Substring(0, 12) + "...";
        }
        EditorGUILayout.LabelField(displayName, EditorStyles.miniLabel, GUILayout.Width(100));
        
        EditorGUILayout.EndVertical();
        
        GUI.backgroundColor = originalColor;
    }
    
    /// <summary>
    /// Update GenerateCharacter to use new filter system
    /// </summary>
    void GenerateCharacter()
    {
        var filteredData = GetFilteredItemsForGenerator();
        generatedCharacter = new Dictionary<string, SPUM_ImprovedTagManager.ImprovedCharacterDataItem>();
        
        // Group by part - dynamically from allCharacterData
        var partGroups = filteredData
            .Where(x => !string.IsNullOrEmpty(x.Part))
            .GroupBy(x => x.Part);
        
        foreach (var group in partGroups)
        {
            var candidates = group.ToList();
            
            // Random selection
            if (candidates.Count > 0)
            {
                var selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                generatedCharacter[group.Key] = selected;
            }
        }
        
        Repaint();
    }
    
    /// <summary>
    /// Multi-select dropdown UI with OR/AND toggle
    /// </summary>
    void DrawMultiSelectDropdownWithToggle(string label, List<string> options, List<string> selected, ref bool expanded, ref bool useAND)
    {
        // Draw label
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.ExpandWidth(false));
        EditorGUILayout.EndHorizontal();
        
        // Draw dropdown button with toggle
        EditorGUILayout.BeginHorizontal();
        
        string displayText = selected.Count == 0 ? "None" : 
                           selected.Count == options.Count ? "All" :
                           selected.Count == 1 ? selected[0] :
                           $"{selected.Count} Selected";
        
        // Calculate dropdown width based on panel width
        float dropdownWidth = leftPanelWidth - 90f; // Account for label and AND/OR button
        if (GUILayout.Button(displayText, EditorStyles.popup, GUILayout.Width(dropdownWidth)))
        {
            expanded = !expanded;
        }
        
        // OR/AND toggle button next to dropdown
        GUI.backgroundColor = useAND ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 1f, 0.4f);
        string filterMode = useAND ? "AND" : "OR";
        if (GUILayout.Button(filterMode, GUILayout.Width(40)))
        {
            useAND = !useAND;
            // Filter mode changed
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        // Draw dropdown content
        if (expanded)
        {
            DrawMultiSelectDropdownContent(label, options, selected);
        }
    }
    
    

    /// <summary>
    /// Multi-select dropdown UI
    /// </summary>
    void DrawMultiSelectDropdown(string label, List<string> options, List<string> selected, ref bool expanded)
    {
        EditorGUILayout.LabelField(label + ":");
        
        // Display selected count
        string displayText = selected.Count == 0 ? "None" : 
                           selected.Count == options.Count ? "All" :
                           selected.Count == 1 ? selected[0] :
                           $"{selected.Count} Selected";
        
        // Dropdown button
        float dropdownWidth = leftPanelWidth - 90f; // Account for label and AND/OR button
        if (GUILayout.Button(displayText, EditorStyles.popup, GUILayout.Width(dropdownWidth)))
        {
            expanded = !expanded;
        }
        
        // Dropdown content
        if (expanded)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Select/Clear all buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All", EditorStyles.miniButton))
            {
                selected.Clear();
                selected.AddRange(options);
            }
            if (GUILayout.Button("None", EditorStyles.miniButton))
            {
                selected.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            // Option list with scroll view if needed
            float maxHeight = 150f; // Maximum height for dropdown
            float itemHeight = EditorGUIUtility.singleLineHeight;
            float contentHeight = options.Count * itemHeight;
            
            if (contentHeight > maxHeight)
            {
                // Use scroll view for long lists
                var scrollKey = $"dropdown_{label}_scroll";
                if (!searchScrollPositions.ContainsKey(scrollKey))
                    searchScrollPositions[scrollKey] = Vector2.zero;
                    
                searchScrollPositions[scrollKey] = EditorGUILayout.BeginScrollView(
                    searchScrollPositions[scrollKey],
                    GUILayout.Height(maxHeight)
                );
            }
            
            // Option list
            foreach (var option in options)
            {
                bool isSelected = selected.Contains(option);
                bool newValue = EditorGUILayout.ToggleLeft(option, isSelected, GUILayout.ExpandWidth(true));
                
                if (newValue != isSelected)
                {
                    if (newValue)
                        selected.Add(option);
                    else
                        selected.Remove(option);
                }
            }
            
            if (contentHeight > maxHeight)
            {
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    /// <summary>
    /// Multi-select dropdown UI with category support for styles
    /// </summary>
    void DrawCategorizedMultiSelectDropdown(string label, Dictionary<string, SPUM_ImprovedStyleData.StyleCategory> categories, List<string> selected, ref bool expanded)
    {
        EditorGUILayout.LabelField(label + ":");
        
        // Display selected count
        int totalOptions = categories.Sum(c => c.Value.styles.Count);
        string displayText = selected.Count == 0 ? "None" : 
                           selected.Count == totalOptions ? "All" :
                           selected.Count == 1 ? selected[0] :
                           $"{selected.Count} Selected";
        
        // Dropdown button
        float dropdownWidth = leftPanelWidth - 90f; // Account for label and AND/OR button
        if (GUILayout.Button(displayText, EditorStyles.popup, GUILayout.Width(dropdownWidth)))
        {
            expanded = !expanded;
        }
        
        // Dropdown content
        if (expanded)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Select/Clear all buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All", EditorStyles.miniButton))
            {
                selected.Clear();
                foreach (var category in categories.Values)
                {
                    selected.AddRange(category.styles.Select(s => s.id));
                }
            }
            if (GUILayout.Button("None", EditorStyles.miniButton))
            {
                selected.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            // Calculate total height for scroll view
            float itemHeight = EditorGUIUtility.singleLineHeight;
            float categoryHeaderHeight = itemHeight * 1.2f;
            float totalHeight = categories.Count * categoryHeaderHeight + totalOptions * itemHeight;
            float maxHeight = 300f; // Increased height for categorized view
            
            if (totalHeight > maxHeight)
            {
                // Use scroll view for long lists
                var scrollKey = $"dropdown_{label}_categorized_scroll";
                if (!searchScrollPositions.ContainsKey(scrollKey))
                    searchScrollPositions[scrollKey] = Vector2.zero;
                    
                searchScrollPositions[scrollKey] = EditorGUILayout.BeginScrollView(
                    searchScrollPositions[scrollKey],
                    GUILayout.Height(maxHeight)
                );
            }
            
            // Display categories and their styles
            foreach (var kvp in categories.OrderBy(c => c.Key))
            {
                var categoryKey = kvp.Key;
                var category = kvp.Value;
                
                // Category header
                EditorGUILayout.Space(2);
                var headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                EditorGUILayout.LabelField($"▸ {category.name}", headerStyle);
                
                // Category options
                foreach (var style in category.styles.OrderBy(s => s.id))
                {
                    var styleName = style.id;
                    var styleInfo = style;
                    
                    EditorGUI.indentLevel++;
                    bool isSelected = selected.Contains(styleName);
                    
                    // Show style name with description tooltip
                    var content = new GUIContent(styleName, styleInfo.description);
                    bool newValue = EditorGUILayout.ToggleLeft(content, isSelected, GUILayout.ExpandWidth(true));
                    
                    if (newValue != isSelected)
                    {
                        if (newValue)
                            selected.Add(styleName);
                        else
                            selected.Remove(styleName);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            if (totalHeight > maxHeight)
            {
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    /// <summary>
    /// Style elements dropdown with OR/AND toggle
    /// </summary>
    void DrawStyleElementsDropdownWithToggle(string label, Dictionary<string, string> styleElements, List<string> selected, ref bool expanded, ref bool useAND)
    {
        // Draw label
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.ExpandWidth(false));
        EditorGUILayout.EndHorizontal();
        
        // Draw dropdown button with OR/AND toggle
        EditorGUILayout.BeginHorizontal();
        
        string displayText = selected.Count == 0 ? "None" : 
                           selected.Count == styleElements.Count ? "All" :
                           selected.Count == 1 ? (styleElements.ContainsKey(selected[0]) ? styleElements[selected[0]] : selected[0]) :
                           $"{selected.Count} Selected";
        
        // Calculate dropdown width based on panel width
        float dropdownWidth = leftPanelWidth - 90f; // Account for label and AND/OR button
        if (GUILayout.Button(displayText, EditorStyles.popup, GUILayout.Width(dropdownWidth)))
        {
            expanded = !expanded;
        }
        
        // OR/AND toggle button next to dropdown
        GUI.backgroundColor = useAND ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 1f, 0.4f);
        string filterMode = useAND ? "AND" : "OR";
        if (GUILayout.Button(filterMode, GUILayout.Width(40)))
        {
            useAND = !useAND;
            // Filter mode changed
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        // Draw dropdown content inline
        if (expanded)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Select All/Clear All buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                selected.Clear();
                selected.AddRange(styleElements.Keys);
            }
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                selected.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Check if local data needs to be loaded
            if (localStyleElementsData == null || localStyleElementsData.categories == null)
            {
                // Loading style elements data
                LoadLocalStyleElementsData();
            }
            
            // Group elements by category for better organization
            if (localStyleElementsData != null && localStyleElementsData.categories != null && localStyleElementsData.categories.Count > 0)
            {
                foreach (var categoryKvp in localStyleElementsData.categories.OrderBy(x => x.Key))
                {
                    var category = categoryKvp.Value;
                    
                    // Category header
                    EditorGUILayout.LabelField($"▼ {category.name}", EditorStyles.miniBoldLabel);
                    EditorGUILayout.Space(2);
                    
                    // Elements in this category
                    foreach (var element in category.styles.OrderBy(x => x.id))
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        bool isSelected = selected.Contains(element.id);
                        bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                        
                        if (newSelected != isSelected)
                        {
                            if (newSelected)
                                selected.Add(element.id);
                            else
                                selected.Remove(element.id);
                        }
                        
                        // Element key with description
                        EditorGUILayout.LabelField(element.id, EditorStyles.label, GUILayout.MinWidth(100));
                        if (!string.IsNullOrEmpty(element.description))
                        {
                            EditorGUILayout.LabelField($"- {element.description}", EditorStyles.miniLabel);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.Space(5);
                }
            }
            
            // Select All / Clear All buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                selected.Clear();
                selected.AddRange(styleElements.Keys);
            }
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                selected.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// Style elements dropdown with category information
    /// </summary>
    void DrawStyleElementsDropdown(string label, Dictionary<string, string> styleElements, List<string> selected, ref bool expanded)
    {
        EditorGUILayout.LabelField(label + ":");
        
        // Display selected count
        string displayText = selected.Count == 0 ? "None" : 
                           selected.Count == styleElements.Count ? "All" :
                           selected.Count == 1 ? (styleElements.ContainsKey(selected[0]) ? styleElements[selected[0]] : selected[0]) :
                           $"{selected.Count} Selected";
        
        // Dropdown button
        float dropdownWidth = leftPanelWidth - 90f; // Account for label and AND/OR button
        if (GUILayout.Button(displayText, EditorStyles.popup, GUILayout.Width(dropdownWidth)))
        {
            expanded = !expanded;
        }
        
        if (expanded)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Check if local data needs to be loaded
            if (localStyleElementsData == null || localStyleElementsData.categories == null)
            {
                // Loading style elements data
                LoadLocalStyleElementsData();
            }
            
            // Group elements by category for better organization
            if (localStyleElementsData != null && localStyleElementsData.categories != null && localStyleElementsData.categories.Count > 0)
            {
                foreach (var categoryKvp in localStyleElementsData.categories.OrderBy(x => x.Key))
                {
                    var category = categoryKvp.Value;
                    
                    // Category header
                    EditorGUILayout.LabelField($"▼ {category.name}", EditorStyles.miniBoldLabel);
                    EditorGUILayout.Space(2);
                    
                    // Elements in this category
                    foreach (var element in category.styles.OrderBy(x => x.id))
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        bool isSelected = selected.Contains(element.id);
                        bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                        
                        if (newSelected != isSelected)
                        {
                            if (newSelected)
                                selected.Add(element.id);
                            else
                                selected.Remove(element.id);
                        }
                        
                        // Element key with description
                        EditorGUILayout.LabelField(element.id, EditorStyles.label, GUILayout.MinWidth(100));
                        if (!string.IsNullOrEmpty(element.description))
                        {
                            EditorGUILayout.LabelField($"- {element.description}", EditorStyles.miniLabel);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.Space(5);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No style elements data available. Check SPUM_Style_Elements.json file.", MessageType.Warning);
                if (GUILayout.Button("Reload Data"))
                {
                    LoadLocalStyleElementsData();
                    Repaint();
                }
            }
            
            // Select All / Clear All buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                selected.Clear();
                selected.AddRange(styleElements.Keys);
            }
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                selected.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
    }
    
    /// <summary>
    /// Custom styles dropdown with OR/AND toggle
    /// </summary>
    void DrawCustomStylesDropdownWithToggle(string label, Dictionary<string, string> customStyles, List<string> selected, ref bool expanded, ref bool useAND)
    {
        // Draw label
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.ExpandWidth(false));
        EditorGUILayout.EndHorizontal();
        
        // Draw dropdown button with OR/AND toggle
        EditorGUILayout.BeginHorizontal();
        
        string displayText = selected.Count == 0 ? "None" : 
                           selected.Count == customStyles.Count ? "All" :
                           selected.Count == 1 ? (customStyles.ContainsKey(selected[0]) ? customStyles[selected[0]] : selected[0]) :
                           $"{selected.Count} Selected";
        
        // Calculate dropdown width based on panel width
        float dropdownWidth = leftPanelWidth - 90f; // Account for label and AND/OR button
        if (GUILayout.Button(displayText, EditorStyles.popup, GUILayout.Width(dropdownWidth)))
        {
            expanded = !expanded;
        }
        
        // OR/AND toggle button next to dropdown
        GUI.backgroundColor = useAND ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 1f, 0.4f);
        string filterMode = useAND ? "AND" : "OR";
        if (GUILayout.Button(filterMode, GUILayout.Width(40)))
        {
            useAND = !useAND;
            // Filter mode changed
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        // Draw dropdown content inline if expanded
        if (expanded)
        {
            EditorGUILayout.BeginVertical("box");
            
            // Select All/Clear All buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                selected.Clear();
                selected.AddRange(customStyles.Keys);
            }
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                selected.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Checkboxes for each style
            foreach (var kvp in customStyles)
            {
                bool isSelected = selected.Contains(kvp.Key);
                EditorGUILayout.BeginHorizontal();
                
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                EditorGUILayout.LabelField($"{kvp.Key} - {kvp.Value}", GUILayout.MinWidth(50));
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        selected.Add(kvp.Key);
                    else
                        selected.Remove(kvp.Key);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// Custom styles dropdown with descriptions
    /// </summary>
    void DrawCustomStylesDropdown(string label, Dictionary<string, string> customStyles, List<string> selected, ref bool expanded)
    {
        EditorGUILayout.LabelField(label + ":");
        
        // Display selected count
        string displayText = selected.Count == 0 ? "None" : 
                           selected.Count == customStyles.Count ? "All" :
                           selected.Count == 1 ? (customStyles.ContainsKey(selected[0]) ? customStyles[selected[0]] : selected[0]) :
                           $"{selected.Count} Selected";
        
        // Dropdown button
        float dropdownWidth = leftPanelWidth - 90f; // Account for label and AND/OR button
        if (GUILayout.Button(displayText, EditorStyles.popup, GUILayout.Width(dropdownWidth)))
        {
            expanded = !expanded;
        }
        
        // Dropdown content
        if (expanded)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Select/Clear all buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All", EditorStyles.miniButton))
            {
                selected.Clear();
                selected.AddRange(customStyles.Keys);
            }
            if (GUILayout.Button("None", EditorStyles.miniButton))
            {
                selected.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            // Get custom style data for descriptions
            var customStyleData = tagManager.GetCustomStyleData();
            
            // Calculate total height for scroll view
            float itemHeight = EditorGUIUtility.singleLineHeight * 2f; // Increased for description
            float totalHeight = customStyles.Count * itemHeight;
            float maxHeight = 300f;
            
            if (totalHeight > maxHeight)
            {
                // Use scroll view for long lists
                var scrollKey = $"dropdown_{label}_custom_scroll";
                if (!searchScrollPositions.ContainsKey(scrollKey))
                    searchScrollPositions[scrollKey] = Vector2.zero;
                
                searchScrollPositions[scrollKey] = EditorGUILayout.BeginScrollView(
                    searchScrollPositions[scrollKey],
                    false, true,
                    GUILayout.Height(maxHeight)
                );
            }
            
            // Options
            foreach (var kvp in customStyles)
            {
                EditorGUILayout.BeginHorizontal();
                
                bool wasSelected = selected.Contains(kvp.Key);
                bool isSelected = EditorGUILayout.ToggleLeft("", wasSelected, GUILayout.Width(20));
                
                // Display name and description
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(kvp.Value, EditorStyles.boldLabel);
                if (customStyleData != null && customStyleData.custom_styles.ContainsKey(kvp.Key))
                {
                    var style = customStyleData.custom_styles[kvp.Key];
                    EditorGUILayout.LabelField(style.description, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                
                if (isSelected != wasSelected)
                {
                    if (isSelected)
                        selected.Add(kvp.Key);
                    else
                        selected.Remove(kvp.Key);
                }
                
                EditorGUILayout.Space(2);
            }
            
            if (totalHeight > maxHeight)
            {
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    
    /// <summary>
    /// Export data to TSV file
    /// </summary>
    void ExportToTSV()
    {
        string path = EditorUtility.SaveFilePanel(
            "Export Character Data to TSV", 
            "", 
            "SPUM_Character_Data.tsv", 
            "tsv"
        );
        
        if (string.IsNullOrEmpty(path)) return;
        
        try
        {
            // Get all data to export
            var dataToExport = tagManager.GetAllCharacterData();
            
            // Build TSV content
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            // Header (same as new_tags format)
            sb.AppendLine("FileName\tPart\tTheme\tRace\tGender\tType\tClass\tProperties\tStyle");
            
            // Data rows
            foreach (var item in dataToExport)
            {
                sb.Append($"{item.FileName}\t");
                sb.Append($"{item.Part}\t");
                sb.Append($"{string.Join(", ", item.Theme ?? new string[0])}\t"); // comma+space like new_tags
                sb.Append($"{item.Race ?? ""}\t");
                sb.Append($"{item.Gender ?? ""}\t");
                sb.Append($"{item.Type ?? ""}\t");
                sb.Append($"{string.Join(", ", item.Class ?? new string[0])}\t"); // comma+space
                sb.Append($"{string.Join(", ", item.Properties ?? new string[0])}\t"); // comma+space
                sb.AppendLine($"{string.Join(", ", item.Style ?? new string[0])}"); // no tab at end
            }
            
            // Write to file with UTF-8 BOM for Excel compatibility
            System.IO.File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));
            
            EditorUtility.DisplayDialog("Export Complete", 
                $"Exported {dataToExport.Count} items to:\n{path}", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Export Failed", 
                $"Failed to export data:\n{e.Message}", "OK");
        }
    }
    
    /// <summary>
    /// Refresh all filter options from updated data
    /// </summary>
    public void RefreshFilterOptions()
    {
        // This method is called after editing tags to ensure filters reflect the latest data
        // Invalidate filter options cache
        filterOptionsCacheValid = false;
        filteredItemsCacheValid = false;
        lastFilterHash = "";
        
        // Clear cached filter options to force refresh
        cachedAvailableAddons = null;
        cachedAvailableThemes = null;
        cachedAvailableRaces = null;
        cachedAvailableGenders = null;
        cachedAvailableClasses = null;
        cachedAvailableStyles = null;
        cachedAvailableParts = null;
        cachedFilteredItems = null;
        
        Repaint();
    }
    
    // Get available filter options from data
    List<string> GetAvailableAddons()
    {
        if (filterOptionsCacheValid && cachedAvailableAddons != null)
            return cachedAvailableAddons;
            
        var addons = new HashSet<string>();
        foreach (var item in tagManager.allCharacterData)
        {
            if (!string.IsNullOrEmpty(item.Addon))
                addons.Add(item.Addon);
        }
        cachedAvailableAddons = addons.OrderBy(x => x).ToList();
        return cachedAvailableAddons;
    }
    
    List<string> GetAvailableThemes()
    {
        if (filterOptionsCacheValid && cachedAvailableThemes != null)
            return cachedAvailableThemes;
            
        var themes = new HashSet<string>();
        foreach (var item in tagManager.allCharacterData)
        {
            if (item.Theme != null)
            {
                foreach (var theme in item.Theme)
                {
                    if (!string.IsNullOrEmpty(theme))
                        themes.Add(theme);
                }
            }
        }
        cachedAvailableThemes = themes.OrderBy(x => x).ToList();
        return cachedAvailableThemes;
    }
    
    List<string> GetAvailableRaces()
    {
        if (filterOptionsCacheValid && cachedAvailableRaces != null)
            return cachedAvailableRaces;
            
        var races = new HashSet<string>();
        foreach (var item in tagManager.allCharacterData)
        {
            if (!string.IsNullOrEmpty(item.Race))
                races.Add(item.Race);
        }
        cachedAvailableRaces = races.OrderBy(x => x).ToList();
        return cachedAvailableRaces;
    }
    
    List<string> GetAvailableGenders()
    {
        if (filterOptionsCacheValid && cachedAvailableGenders != null)
            return cachedAvailableGenders;
            
        var genders = new HashSet<string>();
        foreach (var item in tagManager.allCharacterData)
        {
            if (!string.IsNullOrEmpty(item.Gender))
                genders.Add(item.Gender);
        }
        cachedAvailableGenders = genders.OrderBy(x => x).ToList();
        return cachedAvailableGenders;
    }
    
    List<string> GetAvailableClasses()
    {
        if (filterOptionsCacheValid && cachedAvailableClasses != null)
            return cachedAvailableClasses;
            
        var classes = new List<string>();
        var classData = tagManager.GetClassData();
        if (classData != null && classData.combat_classes != null)
        {
            classes.AddRange(classData.combat_classes.Keys.OrderBy(x => x));
        }
        cachedAvailableClasses = classes;
        return cachedAvailableClasses;
    }
    
    List<string> GetAvailableStyles()
    {
        if (filterOptionsCacheValid && cachedAvailableStyles != null)
            return cachedAvailableStyles;
            
        var styles = new List<string>();
        var customStyleData = tagManager.GetCustomStyleData();
        if (customStyleData != null && customStyleData.custom_styles != null)
        {
            styles.AddRange(customStyleData.custom_styles.Keys);
        }
        cachedAvailableStyles = styles.OrderBy(x => x).ToList();
        return cachedAvailableStyles;
    }
    
    List<string> GetAvailableParts()
    {
        if (filterOptionsCacheValid && cachedAvailableParts != null)
            return cachedAvailableParts;
            
        var parts = new HashSet<string>();
        foreach (var item in tagManager.allCharacterData)
        {
            if (!string.IsNullOrEmpty(item.Part))
                parts.Add(item.Part);
        }
        cachedAvailableParts = parts.OrderBy(x => GetPartOrder(x)).ThenBy(x => x).ToList();
        return cachedAvailableParts;
    }
    
    List<string> GetAvailableStyleElements()
    {
        var elements = new List<string>();
        var styleElementsData = tagManager.GetStyleElementsData();
        if (styleElementsData != null && styleElementsData.categories != null)
        {
            foreach (var category in styleElementsData.categories.Values)
            {
                elements.AddRange(category.styles.Select(s => s.id));
            }
        }
        return elements.OrderBy(x => x).ToList();
    }
    
    Dictionary<string, string> GetStyleElementsWithCategories()
    {
        // Return cached data if valid
        if (styleElementsCacheValid && cachedStyleElementsWithCategories != null)
        {
            return cachedStyleElementsWithCategories;
        }
        
        var elementsDict = new Dictionary<string, string>();
        
        // Check if local data needs to be loaded
        if (localStyleElementsData == null || localStyleElementsData.categories == null)
        {
            LoadLocalStyleElementsData();
        }
        
        // Use local style elements data instead of tagManager
        if (localStyleElementsData != null && localStyleElementsData.categories != null)
        {
            foreach (var categoryKvp in localStyleElementsData.categories)
            {
                var categoryName = categoryKvp.Value.name;
                foreach (var element in categoryKvp.Value.styles)
                {
                    // Format: "element_key" -> "element_key (Category Name)"
                    elementsDict[element.id] = $"{element.id} ({categoryName})";
                }
            }
            
            // Cache the result
            cachedStyleElementsWithCategories = elementsDict;
            styleElementsCacheValid = true;
            
            Debug.Log($"[SPUM] Loaded {elementsDict.Count} style elements for dropdown");
        }
        else
        {
            Debug.LogWarning("[SPUM] No style elements data available for dropdown");
        }
        
        return elementsDict;
    }
    
    void ResetAllFilters()
    {
        filterSelectedClasses.Clear();
        filterSelectedStyles.Clear();
        filterSelectedThemes.Clear();
        filterSelectedRaces.Clear();
        filterSelectedGenders.Clear();
        filterSelectedParts.Clear();
        filterSelectedStyleElements.Clear();
        filterSearchTerm = "";
    }
    
    /// <summary>
    /// Draw the edit panel on the right side
    /// </summary>
    void DrawEditPanel()
    {
        if (editingItem == null)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Select an item to edit", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            return;
        }
        
        // Item selection is already initialized in button click handler
        
        EditorGUILayout.BeginVertical(boxStyle);
        
        // Header
        EditorGUILayout.LabelField("Edit Item Tags", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // Item image and info
        EditorGUILayout.BeginHorizontal();
        
        // Draw item button at 2x scale (100x100 instead of 50x50)
        EditorGUILayout.BeginVertical(GUILayout.Width(110), GUILayout.MaxWidth(110));
        
        // Get item quality for border color
        string quality = GetItemQuality(editingItem);
        Color qualityColor = quality != null ? GetQualityColor(quality) : Color.clear;
        
        // Get cached image
        Texture2D thumbnail = GetCachedImage(editingItem, editingItem.Part);
        GUIContent buttonContent = new GUIContent(thumbnail ?? EditorGUIUtility.whiteTexture);
        
        // Button style
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.padding = new RectOffset(2, 2, 2, 2); // Slightly larger padding for 2x size
        
        // Draw quality border if item has quality
        if (quality != null && qualityColor != Color.clear)
        {
            // Get the rect for the button (2x size: 100x100)
            Rect buttonRect = GUILayoutUtility.GetRect(100, 100, GUILayout.Width(100), GUILayout.Height(100));
            
            // Draw border (6 pixels thick for 2x scale)
            int borderWidth = 6;
            Rect borderRect = new Rect(buttonRect.x - borderWidth, buttonRect.y - borderWidth, 
                                     buttonRect.width + borderWidth * 2, buttonRect.height + borderWidth * 2);
            
            // Save current color
            Color oldColor = GUI.color;
            GUI.color = qualityColor;
            
            // Draw border using GUI.DrawTexture
            GUI.DrawTexture(borderRect, EditorGUIUtility.whiteTexture);
            
            // Restore color
            GUI.color = oldColor;
            
            // Draw the button (non-clickable in edit panel)
            // Scale the image 2x while maintaining aspect ratio
            if (thumbnail != null)
            {
                // Calculate aspect ratio
                float textureAspect = (float)thumbnail.width / thumbnail.height;
                float rectAspect = buttonRect.width / buttonRect.height;
                
                Rect drawRect = buttonRect;
                
                // Adjust rect to maintain aspect ratio
                if (textureAspect > rectAspect)
                {
                    // Texture is wider - fit to width
                    float newHeight = buttonRect.width / textureAspect;
                    float yOffset = (buttonRect.height - newHeight) * 0.5f;
                    drawRect = new Rect(buttonRect.x, buttonRect.y + yOffset, buttonRect.width, newHeight);
                }
                else
                {
                    // Texture is taller - fit to height
                    float newWidth = buttonRect.height * textureAspect;
                    float xOffset = (buttonRect.width - newWidth) * 0.5f;
                    drawRect = new Rect(buttonRect.x + xOffset, buttonRect.y, newWidth, buttonRect.height);
                }
                
                GUI.DrawTextureWithTexCoords(drawRect, thumbnail, new Rect(0, 0, 1, 1), true);
            }
            else
            {
                GUI.Box(buttonRect, "No Image", buttonStyle);
            }
            
            // Draw element icons inside the button (2x scale)
            var elements = GetItemElements(editingItem);
            if (elements.Count > 0)
            {
                float iconSize = 20f; // 2x scale
                float iconPadding = 4f; // 2x scale
                int maxIcons = 3;
                int iconsToShow = Mathf.Min(elements.Count, maxIcons);
                
                float totalWidth = iconsToShow * iconSize + (iconsToShow - 1) * iconPadding;
                float startX = buttonRect.x + buttonRect.width - totalWidth - 4f;
                float startY = buttonRect.y + buttonRect.height - iconSize - 4f;
                
                // Draw each icon
                for (int i = 0; i < iconsToShow; i++)
                {
                    Rect iconRect = new Rect(startX + i * (iconSize + iconPadding), startY, iconSize, iconSize);
                    DrawElementIcon(iconRect, elements[i]);
                }
            }
        }
        else
        {
            // No quality border - draw button normally (2x size)
            Rect buttonRect = GUILayoutUtility.GetRect(100, 100, GUILayout.Width(100), GUILayout.Height(100));
            
            // Scale the image 2x while maintaining aspect ratio
            if (thumbnail != null)
            {
                // Calculate aspect ratio
                float textureAspect = (float)thumbnail.width / thumbnail.height;
                float rectAspect = buttonRect.width / buttonRect.height;
                
                Rect drawRect = buttonRect;
                
                // Adjust rect to maintain aspect ratio
                if (textureAspect > rectAspect)
                {
                    // Texture is wider - fit to width
                    float newHeight = buttonRect.width / textureAspect;
                    float yOffset = (buttonRect.height - newHeight) * 0.5f;
                    drawRect = new Rect(buttonRect.x, buttonRect.y + yOffset, buttonRect.width, newHeight);
                }
                else
                {
                    // Texture is taller - fit to height
                    float newWidth = buttonRect.height * textureAspect;
                    float xOffset = (buttonRect.width - newWidth) * 0.5f;
                    drawRect = new Rect(buttonRect.x + xOffset, buttonRect.y, newWidth, buttonRect.height);
                }
                
                GUI.DrawTextureWithTexCoords(drawRect, thumbnail, new Rect(0, 0, 1, 1), true);
            }
            else
            {
                GUI.Box(buttonRect, "No Image", buttonStyle);
            }
            
            // Draw element icons (2x scale)
            var elements = GetItemElements(editingItem);
            if (elements.Count > 0)
            {
                float iconSize = 20f; // 2x scale
                float iconPadding = 4f; // 2x scale
                int maxIcons = 3;
                int iconsToShow = Mathf.Min(elements.Count, maxIcons);
                
                float totalWidth = iconsToShow * iconSize + (iconsToShow - 1) * iconPadding;
                float startX = buttonRect.x + buttonRect.width - totalWidth - 4f;
                float startY = buttonRect.y + buttonRect.height - iconSize - 4f;
                
                // Draw each icon
                for (int i = 0; i < iconsToShow; i++)
                {
                    Rect iconRect = new Rect(startX + i * (iconSize + iconPadding), startY, iconSize, iconSize);
                    DrawElementIcon(iconRect, elements[i]);
                }
            }
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Info on the right
        EditorGUILayout.BeginVertical();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("File:", GUILayout.Width(40));
        EditorGUILayout.LabelField(editingItem.FileName, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Part:", GUILayout.Width(40));
        EditorGUILayout.LabelField(editingItem.Part, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        
        // Navigation arrows
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("◀", GUILayout.Width(30), GUILayout.Height(25)))
        {
            NavigateToPreviousItem();
        }
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("▶", GUILayout.Width(30), GUILayout.Height(25)))
        {
            NavigateToNextItem();
        }
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Calculate height for edit panel scroll
        float editPanelHeight = position.height - 400f; // Reserve space for header, image, buttons, and tabs
        editPanelHeight = Mathf.Max(editPanelHeight, 300f);
        
        // Scroll view for edit content
        editPanelScrollPosition = EditorGUILayout.BeginScrollView(
            editPanelScrollPosition,
            false, // alwaysShowHorizontal
            true,  // alwaysShowVertical
            GUILayout.Height(editPanelHeight)
        );
        
        // Basic properties
        EditorGUILayout.LabelField("Basic Properties", EditorStyles.boldLabel);
        editThemeString = EditorGUILayout.TextField("Theme:", editThemeString);
        editingItem.Race = EditorGUILayout.TextField("Race:", editingItem.Race);
        editingItem.Gender = EditorGUILayout.TextField("Gender:", editingItem.Gender);
        
        EditorGUILayout.Space(10);
        
        // Draw sections using existing methods
        // These methods will need to be modified to use edit variables instead of popup variables
        DrawEditTypeSection();
        EditorGUILayout.Space(10);
        DrawEditRoleSection();
        EditorGUILayout.Space(10);
        DrawEditQualitySection();
        EditorGUILayout.Space(10);
        DrawEditElementsSection();
        EditorGUILayout.Space(10);
        DrawEditAttackTypeSection();
        EditorGUILayout.Space(10);
        DrawEditDistanceTypeSection();
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        
        // Save/Cancel buttons
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Save", GUILayout.Height(25)))
        {
            SaveEditingItem();
        }
        GUI.backgroundColor = Color.white;
        
        if (GUILayout.Button("Cancel", GUILayout.Height(25)))
        {
            editingItem = null;
            ClearEditSelections();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    void SaveEditingItem()
    {
        if (editingItem == null) return;
        
        // Convert theme string back to array
        editingItem.Theme = ParseTagString(editThemeString);
        
        // Combine roles, attack types, and distance types for Class array
        var classList = new List<string>();
        classList.AddRange(editSelectedRoles);
        classList.AddRange(editSelectedAttackTypes);
        classList.AddRange(editSelectedDistanceTypes);
        editingItem.Class = classList.Count > 0 ? classList.ToArray() : new string[0];
        
        // Combine only style-related selections
        var styleList = new List<string>();
        styleList.AddRange(editSelectedQualities);
        styleList.AddRange(editSelectedElements);
        styleList.AddRange(editSelectedExpressions);
        styleList.AddRange(editSelectedFacialHair);
        styleList.AddRange(editSelectedHairstyles);
        editingItem.Style = styleList.Count > 0 ? styleList.ToArray() : new string[0];
        
        // Update Type
        if (!string.IsNullOrEmpty(editSelectedType))
        {
            editingItem.Type = editSelectedType;
        }
        else
        {
            editingItem.Type = "";
        }
        
        // Update in tag manager
        var tagManager = SPUM_ImprovedTagManager.Instance;
        if (tagManager != null)
        {
            bool updated = tagManager.UpdateCharacterDataItem(editingItem.FileName, editingItem);
            if (updated)
            {
                Debug.Log($"[Edit] Successfully updated tags for: {editingItem.FileName}");
                // OnDataLoaded event will handle refresh automatically
            }
        }
        
        // Refresh the edit panel with saved data
        InitializeEditSelections();
    }
    
    void ClearEditSelections()
    {
        editSelectedRoles.Clear();
        editSelectedQualities.Clear();
        editSelectedElements.Clear();
        editSelectedAttackTypes.Clear();
        editSelectedDistanceTypes.Clear();
        editSelectedExpressions.Clear();
        editSelectedFacialHair.Clear();
        editSelectedHairstyles.Clear();
        editSelectedTypeCategory = "";
        editSelectedType = "";
        editThemeString = "";
    }
    
    void InitializeEditSelections()
    {
        if (editingItem == null) return;
        
        ClearEditSelections();
        
        // Initialize theme
        if (editingItem.Theme != null)
            editThemeString = string.Join(", ", editingItem.Theme);
        
        // Initialize roles, attack types, and distance types from Class array
        if (editingItem.Class != null && editingItem.Class.Length > 0 && localStyleElementsData != null)
        {
            foreach (var classItem in editingItem.Class)
            {
                if (string.IsNullOrEmpty(classItem)) continue;
                
                // Check if it's a role
                if (localStyleElementsData.categories.ContainsKey("role") && 
                    localStyleElementsData.categories["role"].styles.Any(s => s.id == classItem))
                    editSelectedRoles.Add(classItem);
                // Check if it's an attack type
                else if (localStyleElementsData.categories.ContainsKey("attack_type") && 
                    localStyleElementsData.categories["attack_type"].styles.Any(s => s.id == classItem))
                    editSelectedAttackTypes.Add(classItem);
                // Check if it's a distance type
                else if (localStyleElementsData.categories.ContainsKey("distance_type") && 
                    localStyleElementsData.categories["distance_type"].styles.Any(s => s.id == classItem))
                    editSelectedDistanceTypes.Add(classItem);
                else
                    // If not recognized, add to roles by default
                    editSelectedRoles.Add(classItem);
            }
        }
        
        // Initialize styles
        if (editingItem.Style != null && editingItem.Style.Length > 0 && localStyleElementsData != null)
        {
            foreach (var style in editingItem.Style)
            {
                if (string.IsNullOrEmpty(style)) continue;
                
                // Check each category
                if (localStyleElementsData.categories.ContainsKey("quality") && 
                    localStyleElementsData.categories["quality"].styles.Any(s => s.id == style))
                    editSelectedQualities.Add(style);
                else if (localStyleElementsData.categories.ContainsKey("elements") && 
                    localStyleElementsData.categories["elements"].styles.Any(s => s.id == style))
                    editSelectedElements.Add(style);
                else if (localStyleElementsData.categories.ContainsKey("expression") && 
                    localStyleElementsData.categories["expression"].styles.Any(s => s.id == style))
                    editSelectedExpressions.Add(style);
                else if (localStyleElementsData.categories.ContainsKey("facial_hair") && 
                    localStyleElementsData.categories["facial_hair"].styles.Any(s => s.id == style))
                    editSelectedFacialHair.Add(style);
                else if (localStyleElementsData.categories.ContainsKey("hairstyle") && 
                    localStyleElementsData.categories["hairstyle"].styles.Any(s => s.id == style))
                    editSelectedHairstyles.Add(style);
            }
        }
        
        // Initialize type
        if (!string.IsNullOrEmpty(editingItem.Type) && localStyleElementsData != null)
        {
            var typeCategories = GetEditTypeCategories();
            foreach (var category in typeCategories)
            {
                if (category.Value.styles != null)
                {
                    foreach (var type in category.Value.styles)
                    {
                        if (type.id.Equals(editingItem.Type, StringComparison.OrdinalIgnoreCase))
                        {
                            editSelectedTypeCategory = category.Key;
                            editSelectedType = type.id;
                            break;
                        }
                    }
                }
            }
        }
    }
    
    // Helper method to calculate columns based on panel width
    int GetColumnsForPanelWidth(float panelWidth, float buttonWidth)
    {
        // Account for margins and scrollbar
        float availableWidth = panelWidth - 20f; // Reduced margin for better space usage
        int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / buttonWidth));
        return columns;
    }
    
    void DrawEditTypeSection()
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        editTypeFoldout = EditorGUILayout.Foldout(editTypeFoldout, "아이템 타입 (Item Type)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        if (editTypeFoldout)
        {
            // Get type categories
            var typeCategories = GetEditTypeCategories();
            
            if (typeCategories.Count > 0)
            {
                // Category selection
                EditorGUILayout.LabelField("카테고리 선택:", EditorStyles.miniBoldLabel);
                
                // Draw category buttons in rows - dynamic columns based on panel width
                int columns = GetColumnsForPanelWidth(rightPanelWidth, 110f); // 110f for category button width
                int currentColumn = 0;
                
                EditorGUILayout.BeginHorizontal();
                foreach (var category in typeCategories)
                {
                    if (currentColumn >= columns)
                    {
                        currentColumn = 0;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                    
                    bool isSelected = editSelectedTypeCategory == category.Key;
                    
                    // Set button color based on category type
                    Color categoryColor = GetCategoryColor(category.Key);
                    GUI.backgroundColor = isSelected ? categoryColor : Color.white;
                    
                    var content = new GUIContent(category.Value.name ?? category.Key, category.Key);
                    if (GUILayout.Toggle(isSelected, content, "Button", GUILayout.Height(25)))
                    {
                        if (!isSelected)
                        {
                            editSelectedTypeCategory = category.Key;
                            editSelectedType = ""; // Clear type when category changes
                        }
                    }
                    else
                    {
                        if (isSelected)
                        {
                            editSelectedTypeCategory = "";
                            editSelectedType = "";
                        }
                    }
                    
                    GUI.backgroundColor = Color.white;
                    currentColumn++;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(10);
                
                // Type selection based on selected category
                if (!string.IsNullOrEmpty(editSelectedTypeCategory) && typeCategories.ContainsKey(editSelectedTypeCategory))
                {
                    var category = typeCategories[editSelectedTypeCategory];
                    if (category.styles != null && category.styles.Count > 0)
                    {
                        string labelText = (editSelectedTypeCategory == "expression") ? "표정 선택:" :
                                         (editSelectedTypeCategory == "facial_hair") ? "수염 선택:" :
                                         (editSelectedTypeCategory == "hairstyle") ? "헤어스타일 선택:" :
                                         $"{category.name} 타입 선택:";
                        EditorGUILayout.LabelField(labelText, EditorStyles.miniBoldLabel);
                        
                        // Draw type buttons - dynamic columns based on panel width
                        columns = GetColumnsForPanelWidth(rightPanelWidth, 85f); // 85f for type button width
                        currentColumn = 0;
                        
                        EditorGUILayout.BeginHorizontal();
                        foreach (var type in category.styles)
                        {
                            if (currentColumn >= columns)
                            {
                                currentColumn = 0;
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                            }
                            
                            bool isTypeSelected = false;
                            
                            // Special handling for expression, facial_hair, and hairstyle
                            if (editSelectedTypeCategory == "expression")
                            {
                                isTypeSelected = editSelectedExpressions.Contains(type.id);
                            }
                            else if (editSelectedTypeCategory == "facial_hair")
                            {
                                isTypeSelected = editSelectedFacialHair.Contains(type.id);
                            }
                            else if (editSelectedTypeCategory == "hairstyle")
                            {
                                isTypeSelected = editSelectedHairstyles.Contains(type.id);
                            }
                            else
                            {
                                isTypeSelected = editSelectedType == type.id;
                            }
                            
                            GUI.backgroundColor = isTypeSelected ? GetCategoryColor(editSelectedTypeCategory) * 0.8f : Color.white;
                            
                            var typeContent = new GUIContent(type.id, type.description ?? type.id);
                            if (GUILayout.Toggle(isTypeSelected, typeContent, "Button", GUILayout.Height(20)))
                            {
                                if (!isTypeSelected)
                                {
                                    if (editSelectedTypeCategory == "expression")
                                    {
                                        editSelectedExpressions.Clear();
                                        editSelectedExpressions.Add(type.id);
                                        editSelectedType = type.id;
                                    }
                                    else if (editSelectedTypeCategory == "facial_hair")
                                    {
                                        editSelectedFacialHair.Clear();
                                        editSelectedFacialHair.Add(type.id);
                                        editSelectedType = type.id;
                                    }
                                    else if (editSelectedTypeCategory == "hairstyle")
                                    {
                                        editSelectedHairstyles.Clear();
                                        editSelectedHairstyles.Add(type.id);
                                        editSelectedType = type.id;
                                    }
                                    else
                                    {
                                        editSelectedType = type.id;
                                    }
                                }
                            }
                            else
                            {
                                if (isTypeSelected)
                                {
                                    if (editSelectedTypeCategory == "expression")
                                    {
                                        editSelectedExpressions.Remove(type.id);
                                        editSelectedType = "";
                                    }
                                    else if (editSelectedTypeCategory == "facial_hair")
                                    {
                                        editSelectedFacialHair.Remove(type.id);
                                        editSelectedType = "";
                                    }
                                    else if (editSelectedTypeCategory == "hairstyle")
                                    {
                                        editSelectedHairstyles.Remove(type.id);
                                        editSelectedType = "";
                                    }
                                    else
                                    {
                                        editSelectedType = "";
                                    }
                                }
                            }
                            
                            GUI.backgroundColor = Color.white;
                            currentColumn++;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawEditRoleSection()
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        editRoleFoldout = EditorGUILayout.Foldout(editRoleFoldout, "RPG 역할 (RPG Role)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        if (editRoleFoldout)
        {
            if (localStyleElementsData != null && localStyleElementsData.categories.ContainsKey("role"))
            {
                var roleCategory = localStyleElementsData.categories["role"];
                if (roleCategory.styles != null && roleCategory.styles.Count > 0)
                {
                    EditorGUILayout.LabelField("역할 선택 (다중 선택 가능):", EditorStyles.miniBoldLabel);
                    
                    int columns = GetColumnsForPanelWidth(rightPanelWidth, 120f); // 120f for role button width
                    int currentColumn = 0;
                    
                    EditorGUILayout.BeginHorizontal();
                    foreach (var role in roleCategory.styles)
                    {
                        if (currentColumn >= columns)
                        {
                            currentColumn = 0;
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                        }
                        
                        bool isSelected = editSelectedRoles.Contains(role.id);
                        GUI.backgroundColor = isSelected ? new Color(0.5f, 0.8f, 1f) : Color.white;
                        
                        var content = new GUIContent(role.id, role.description ?? role.id);
                        if (GUILayout.Toggle(isSelected, content, "Button", GUILayout.Height(25)))
                        {
                            if (!isSelected)
                                editSelectedRoles.Add(role.id);
                        }
                        else
                        {
                            if (isSelected)
                                editSelectedRoles.Remove(role.id);
                        }
                        
                        GUI.backgroundColor = Color.white;
                        currentColumn++;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawEditQualitySection()
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        editQualityFoldout = EditorGUILayout.Foldout(editQualityFoldout, "등급 (Quality)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        if (editQualityFoldout)
        {
            if (localStyleElementsData != null && localStyleElementsData.categories.ContainsKey("quality"))
            {
                var qualityCategory = localStyleElementsData.categories["quality"];
                if (qualityCategory.styles != null && qualityCategory.styles.Count > 0)
                {
                    EditorGUILayout.LabelField("등급 선택 (다중 선택 가능):", EditorStyles.miniBoldLabel);
                    
                    int columns = GetColumnsForPanelWidth(rightPanelWidth, 120f); // 120f for quality button width
                    int currentColumn = 0;
                    
                    EditorGUILayout.BeginHorizontal();
                    foreach (var quality in qualityCategory.styles)
                    {
                        if (currentColumn >= columns)
                        {
                            currentColumn = 0;
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                        }
                        
                        bool isSelected = editSelectedQualities.Contains(quality.id);
                        Color qualityColor = GetQualityColor(quality.id);
                        GUI.backgroundColor = isSelected ? qualityColor : Color.white;
                        
                        var content = new GUIContent(quality.id, quality.description ?? quality.id);
                        if (GUILayout.Toggle(isSelected, content, "Button", GUILayout.Height(25)))
                        {
                            if (!isSelected)
                                editSelectedQualities.Add(quality.id);
                        }
                        else
                        {
                            if (isSelected)
                                editSelectedQualities.Remove(quality.id);
                        }
                        
                        GUI.backgroundColor = Color.white;
                        currentColumn++;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawEditElementsSection()
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        editElementsFoldout = EditorGUILayout.Foldout(editElementsFoldout, "원소 속성 (Elements)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        if (editElementsFoldout)
        {
            if (localStyleElementsData != null && localStyleElementsData.categories.ContainsKey("elements"))
            {
                var elementsCategory = localStyleElementsData.categories["elements"];
                if (elementsCategory.styles != null && elementsCategory.styles.Count > 0)
                {
                    EditorGUILayout.LabelField("원소 선택 (다중 선택 가능):", EditorStyles.miniBoldLabel);
                    
                    int columns = GetColumnsForPanelWidth(rightPanelWidth, 120f); // 120f for element button width
                    int currentColumn = 0;
                    
                    EditorGUILayout.BeginHorizontal();
                    foreach (var element in elementsCategory.styles)
                    {
                        if (currentColumn >= columns)
                        {
                            currentColumn = 0;
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                        }
                        
                        bool isSelected = editSelectedElements.Contains(element.id);
                        Color elementColor = GetElementColor(element.id);
                        GUI.backgroundColor = isSelected ? elementColor : Color.white;
                        
                        var content = new GUIContent(element.id, element.description ?? element.id);
                        if (GUILayout.Toggle(isSelected, content, "Button", GUILayout.Height(25)))
                        {
                            if (!isSelected)
                                editSelectedElements.Add(element.id);
                        }
                        else
                        {
                            if (isSelected)
                                editSelectedElements.Remove(element.id);
                        }
                        
                        GUI.backgroundColor = Color.white;
                        currentColumn++;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawEditAttackTypeSection()
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        editAttackTypeFoldout = EditorGUILayout.Foldout(editAttackTypeFoldout, "공격 타입 (Attack Type)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        if (editAttackTypeFoldout)
        {
            if (localStyleElementsData != null && localStyleElementsData.categories.ContainsKey("attack_type"))
            {
                var attackTypeCategory = localStyleElementsData.categories["attack_type"];
                if (attackTypeCategory.styles != null && attackTypeCategory.styles.Count > 0)
                {
                    EditorGUILayout.LabelField("공격 타입 선택 (다중 선택 가능):", EditorStyles.miniBoldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    foreach (var type in attackTypeCategory.styles)
                    {
                        bool isSelected = editSelectedAttackTypes.Contains(type.id);
                        GUI.backgroundColor = isSelected ? new Color(1f, 0.6f, 0.6f) : Color.white;
                        
                        var content = new GUIContent(type.id, type.description ?? type.id);
                        if (GUILayout.Toggle(isSelected, content, "Button", GUILayout.Height(25)))
                        {
                            if (!isSelected)
                                editSelectedAttackTypes.Add(type.id);
                        }
                        else
                        {
                            if (isSelected)
                                editSelectedAttackTypes.Remove(type.id);
                        }
                        
                        GUI.backgroundColor = Color.white;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawEditDistanceTypeSection()
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        editDistanceTypeFoldout = EditorGUILayout.Foldout(editDistanceTypeFoldout, "거리 타입 (Distance Type)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        if (editDistanceTypeFoldout)
        {
            if (localStyleElementsData != null && localStyleElementsData.categories.ContainsKey("distance_type"))
            {
                var distanceTypeCategory = localStyleElementsData.categories["distance_type"];
                if (distanceTypeCategory.styles != null && distanceTypeCategory.styles.Count > 0)
                {
                    EditorGUILayout.LabelField("거리 타입 선택 (다중 선택 가능):", EditorStyles.miniBoldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    foreach (var type in distanceTypeCategory.styles)
                    {
                        bool isSelected = editSelectedDistanceTypes.Contains(type.id);
                        GUI.backgroundColor = isSelected ? new Color(0.6f, 1f, 0.6f) : Color.white;
                        
                        var content = new GUIContent(type.id, type.description ?? type.id);
                        if (GUILayout.Toggle(isSelected, content, "Button", GUILayout.Height(25)))
                        {
                            if (!isSelected)
                                editSelectedDistanceTypes.Add(type.id);
                        }
                        else
                        {
                            if (isSelected)
                                editSelectedDistanceTypes.Remove(type.id);
                        }
                        
                        GUI.backgroundColor = Color.white;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    Dictionary<string, SPUM_ImprovedStyleData.StyleCategory> GetEditTypeCategories()
    {
        var typeCategories = new Dictionary<string, SPUM_ImprovedStyleData.StyleCategory>();
        
        if (localStyleElementsData != null && localStyleElementsData.categories != null)
        {
            // Type-related categories and character features
            string[] typeCategoryKeys = {
                "weapon_melee", "weapon_ranged", "weapon_magic",
                "armor_heavy", "armor_light", "armor_cloth",
                "defensive", "accessory", "helmet",
                "expression", "facial_hair", "hairstyle"
            };
            
            foreach (var key in typeCategoryKeys)
            {
                if (localStyleElementsData.categories.ContainsKey(key))
                {
                    typeCategories[key] = localStyleElementsData.categories[key];
                }
            }
        }
        
        return typeCategories;
    }
    
    string[] ParseTagString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new string[0];
        
        var tags = input.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tags.Length; i++)
        {
            tags[i] = tags[i].Trim();
        }
        
        return tags;
    }
    
    
    /// <summary>
    /// Get logical order for parts display
    /// </summary>
    int GetPartOrder(string partName)
    {
        // Define logical order for common parts
        switch (partName?.ToLower())
        {
            case "body": return 0;
            case "eye": return 1;
            case "hair": return 2;
            case "facehair": return 3;
            case "helmet": return 4;
            case "clothes": return 5;
            case "pants": return 6;
            case "shoes": return 7;
            case "weapon": return 8;
            case "shield": return 9;
            case "back": return 10;
            default: return 99; // Any other parts go to the end
        }
    }
    
    /// <summary>
    /// Get cached image for item or load it if not cached
    /// </summary>
    public Texture2D GetCachedImage(SPUM_ImprovedTagManager.ImprovedCharacterDataItem item, string partName)
    {
        if (item == null || string.IsNullOrEmpty(item.FileName))
            return null;
        
        // Clear old cache entries periodically
        if ((DateTime.Now - lastCacheClear) > cacheLifetime)
        {
            imageCache.Clear();
            lastCacheClear = DateTime.Now;
        }
        
        // Create cache key from filename
        string cacheKey = item.FileName;
        
        // Check if already cached
        if (imageCache.ContainsKey(cacheKey))
        {
            return imageCache[cacheKey];
        }
        
        // Try to load the texture using AssetDatabase
        Texture2D preview = null;
        
        try
        {
            // Use wildcard search pattern like ClassTagManager
            string searchPattern = $"*{item.FileName}*";
            
            // Search for textures in SPUM Resources directory
            string[] textureGuids = AssetDatabase.FindAssets($"t:Texture2D {searchPattern}", new[] { "Assets/SPUM/Resources" });
            
            if (textureGuids.Length > 0)
            {
                // Find best matching texture using scoring system
                string bestMatch = null;
                int bestMatchScore = int.MaxValue;
                
                foreach (string guid in textureGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    
                    // Exact name matching has highest priority
                    if (fileName.Equals(item.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        bestMatch = path;
                        break;
                    }
                    
                    // Partial matching with score (shorter difference = better match)
                    if (fileName.IndexOf(item.FileName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        int score = fileName.Length - item.FileName.Length;
                        if (score < bestMatchScore)
                        {
                            bestMatchScore = score;
                            bestMatch = path;
                        }
                    }
                }
                
                // Load the best match
                if (!string.IsNullOrEmpty(bestMatch))
                {
                    preview = AssetDatabase.LoadAssetAtPath<Texture2D>(bestMatch);
                    if (preview != null)
                    {
                        imageCache[cacheKey] = preview;
                    }
                }
            }
            
            // If texture not found, try loading as sprite
            if (preview == null)
            {
                string[] spriteGuids = AssetDatabase.FindAssets($"t:Sprite {searchPattern}", new[] { "Assets/SPUM/Resources" });
                
                if (spriteGuids.Length > 0)
                {
                    // Find best matching sprite using same scoring system
                    string bestMatch = null;
                    int bestMatchScore = int.MaxValue;
                    
                    foreach (string guid in spriteGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                        
                        // Exact name matching has highest priority
                        if (fileName.Equals(item.FileName, StringComparison.OrdinalIgnoreCase))
                        {
                            bestMatch = path;
                            break;
                        }
                        
                        // Partial matching with score
                        if (fileName.IndexOf(item.FileName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int score = fileName.Length - item.FileName.Length;
                            if (score < bestMatchScore)
                            {
                                bestMatchScore = score;
                                bestMatch = path;
                            }
                        }
                    }
                    
                    // Load original texture from sprite
                    if (!string.IsNullOrEmpty(bestMatch))
                    {
                        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(bestMatch);
                        if (texture != null)
                        {
                            preview = texture;
                            imageCache[cacheKey] = preview;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to load image for {item.FileName}: {e.Message}");
        }
        
        return preview;
    }
    
    void DrawMultiSelectDropdownContent(string label, List<string> options, List<string> selected)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        // Select/Clear all buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("All", EditorStyles.miniButton))
        {
            selected.Clear();
            selected.AddRange(options);
        }
        if (GUILayout.Button("None", EditorStyles.miniButton))
        {
            selected.Clear();
        }
        EditorGUILayout.EndHorizontal();
        
        // Option list with scroll view if needed
        float maxHeight = 150f; // Maximum height for dropdown
        float itemHeight = EditorGUIUtility.singleLineHeight;
        float contentHeight = options.Count * itemHeight;
        
        if (contentHeight > maxHeight)
        {
            // Use scroll view for long lists
            var scrollKey = $"dropdown_{label}_scroll";
            if (!searchScrollPositions.ContainsKey(scrollKey))
                searchScrollPositions[scrollKey] = Vector2.zero;
                
            searchScrollPositions[scrollKey] = EditorGUILayout.BeginScrollView(
                searchScrollPositions[scrollKey],
                false, true,
                GUILayout.Height(maxHeight)
            );
        }
        
        // Display all options with toggle
        foreach (var option in options)
        {
            bool isSelected = selected.Contains(option);
            bool newValue = EditorGUILayout.Toggle(option, isSelected);
            
            if (newValue != isSelected)
            {
                if (newValue)
                    selected.Add(option);
                else
                    selected.Remove(option);
            }
        }
        
        if (contentHeight > maxHeight)
        {
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void NavigateToPreviousItem()
    {
        if (editingItem == null || displayedItems.Count <= 1) return;
        
        // Find current item index in displayed items
        int currentIndex = displayedItems.FindIndex(item => item.FileName == editingItem.FileName);
        if (currentIndex == -1) return;
        
        // Navigate to previous item (wrap around)
        int previousIndex = currentIndex - 1;
        if (previousIndex < 0) previousIndex = displayedItems.Count - 1;
        
        editingItem = displayedItems[previousIndex];
        InitializeEditSelections();
        Repaint();
    }
    
    void NavigateToNextItem()
    {
        if (editingItem == null || displayedItems.Count <= 1) return;
        
        // Find current item index in displayed items
        int currentIndex = displayedItems.FindIndex(item => item.FileName == editingItem.FileName);
        if (currentIndex == -1) return;
        
        // Navigate to next item (wrap around)
        int nextIndex = currentIndex + 1;
        if (nextIndex >= displayedItems.Count) nextIndex = 0;
        
        editingItem = displayedItems[nextIndex];
        InitializeEditSelections();
        Repaint();
    }
    
    
    void DrawGoogleSheetTab()
    {
        EditorGUILayout.BeginVertical();
        
        EditorGUILayout.LabelField("Google Sheet Data Management", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        if (tagManager == null)
        {
            EditorGUILayout.HelpBox("Tag Manager reference is missing.", MessageType.Error);
            EditorGUILayout.EndVertical();
            return;
        }
        
        // 초기화 - 처음 열 때만 기본값 설정
        if (!isGoogleSheetConfigInitialized)
        {
            if (string.IsNullOrEmpty(googleSheetConfig.sheetID) && googleSheetConfig.tabs.Count == 0)
            {
                googleSheetConfig.sheetID = DEFAULT_SHEET_ID;
                foreach (var (name, gid) in DEFAULT_TABS)
                {
                    googleSheetConfig.tabs.Add(new GoogleSheetTab { name = name, gid = gid });
                }
            }
            isGoogleSheetConfigInitialized = true;
        }
        
        // Google Sheet 설정
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Google Sheet Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);
        
        // Sheet ID
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sheet ID:", GUILayout.Width(60));
        googleSheetConfig.sheetID = EditorGUILayout.TextField(googleSheetConfig.sheetID);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(3);
        
        // Tabs 설정
        EditorGUILayout.LabelField("Tab Configuration:", EditorStyles.miniBoldLabel);
        
        googleSheetScrollPosition = EditorGUILayout.BeginScrollView(googleSheetScrollPosition, GUILayout.MaxHeight(200));
        
        for (int i = 0; i < googleSheetConfig.tabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField($"{i + 1}:", GUILayout.Width(20));
            EditorGUILayout.LabelField("Name:", GUILayout.Width(40));
            googleSheetConfig.tabs[i].name = EditorGUILayout.TextField(googleSheetConfig.tabs[i].name, GUILayout.Width(150));
            EditorGUILayout.LabelField("GID:", GUILayout.Width(30));
            googleSheetConfig.tabs[i].gid = EditorGUILayout.TextField(googleSheetConfig.tabs[i].gid);
            
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                googleSheetConfig.tabs.RemoveAt(i);
                break;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Tab", GUILayout.Width(80)))
        {
            googleSheetConfig.tabs.Add(new GoogleSheetTab());
        }
        
        if (GUILayout.Button("Reset to Default", GUILayout.Width(120)))
        {
            if (EditorUtility.DisplayDialog("Reset Configuration", 
                "Reset to default Google Sheet configuration?", 
                "Yes", "No"))
            {
                googleSheetConfig.sheetID = DEFAULT_SHEET_ID;
                googleSheetConfig.tabs.Clear();
                foreach (var (name, gid) in DEFAULT_TABS)
                {
                    googleSheetConfig.tabs.Add(new GoogleSheetTab { name = name, gid = gid });
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // Internal Data Operations
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Internal Data Operations", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Class Data", GUILayout.Height(25)))
        {
            tagManager.LoadInternalClassData();
            ShowNotification(new GUIContent("Class data loaded"));
        }
        
        if (GUILayout.Button("Load Style Elements", GUILayout.Height(25)))
        {
            tagManager.LoadInternalStyleElementsData();
            ShowNotification(new GUIContent("Style elements loaded"));
        }
        
        if (GUILayout.Button("Load Custom Styles", GUILayout.Height(25)))
        {
            tagManager.LoadInternalCustomStyleData();
            ShowNotification(new GUIContent("Custom styles loaded"));
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(3);
        
        // 데이터 상태 표시
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Status:", GUILayout.Width(50));
        bool hasClassData = tagManager.classData != null;
        bool hasStyleElements = tagManager.styleElementsData != null;
        bool hasCustomStyles = tagManager.customStyleData != null;
        
        string status = "";
        if (hasClassData) status += "Classes ✓ ";
        if (hasStyleElements) status += "Elements ✓ ";
        if (hasCustomStyles) status += "Styles ✓";
        
        if (string.IsNullOrEmpty(status))
            status = "No internal data loaded";
            
        EditorGUILayout.LabelField(status);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // Google Sheet 데이터 작업
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Google Sheet Data Operations", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);
        
        // 로드 버튼
        EditorGUI.BeginDisabledGroup(isLoadingFromGoogleSheet || string.IsNullOrEmpty(googleSheetConfig.sheetID));
        if (GUILayout.Button("Load Data to Cache", GUILayout.Height(30)))
        {
            LoadDataFromGoogleSheet();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space(3);
        
        // Save to Inspector 버튼
        if (GUILayout.Button("Save to Inspector", GUILayout.Height(25)))
        {
            EditorUtility.SetDirty(tagManager);
            ShowNotification(new GUIContent("Data saved to Inspector"));
        }
        
        EditorGUILayout.Space(3);
        
        // 캐시 삭제 버튼
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Clear Cache", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Cache", 
                "Are you sure you want to clear all cached data?", 
                "Yes", "No"))
            {
                tagManager.allCharacterData.Clear();
                EditorUtility.SetDirty(tagManager);
                ShowNotification(new GUIContent("Cache cleared"));
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // 데이터 요약
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Data Summary", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);
        
        int characterCount = tagManager.allCharacterData?.Count ?? 0;
        EditorGUILayout.LabelField($"Cached Items: {characterCount}");
        
        if (googleSheetConfig.tabs.Count > 0)
        {
            EditorGUILayout.LabelField($"Configured Tabs: {googleSheetConfig.tabs.Count}");
        }
        
        EditorGUILayout.Space(3);
        
        if (isLoadingFromGoogleSheet)
        {
            EditorGUILayout.HelpBox("Loading data from Google Sheet...", MessageType.Info);
        }
        else if (characterCount > 0)
        {
            EditorGUILayout.HelpBox($"Cache contains {characterCount} items. Data will persist in Inspector.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("No data in cache. Load from Google Sheet to populate.", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndVertical();
    }
    
    void LoadDataFromGoogleSheet()
    {
        if (string.IsNullOrEmpty(googleSheetConfig.sheetID) || googleSheetConfig.tabs.Count == 0)
        {
            ShowNotification(new GUIContent("Please configure Sheet ID and tabs"));
            return;
        }
        
        // Unity Editor에서는 동기적으로 실행
        LoadDataFromGoogleSheetAsync();
    }
    
    async void LoadDataFromGoogleSheetAsync()
    {
        isLoadingFromGoogleSheet = true;
        tagManager.allCharacterData.Clear();
        int totalLoaded = 0;
        
        foreach (var tab in googleSheetConfig.tabs)
        {
            if (string.IsNullOrEmpty(tab.gid)) continue;
            
            string url = $"https://docs.google.com/spreadsheets/d/e/{googleSheetConfig.sheetID}/pub?gid={tab.gid}&single=true&output=tsv";
            
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await System.Threading.Tasks.Task.Delay(100);
                }
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    string tsvData = request.downloadHandler.text;
                    int beforeCount = tagManager.allCharacterData.Count;
                    ParseTSVData(tsvData, tab.name);
                    int loadedCount = tagManager.allCharacterData.Count - beforeCount;
                    totalLoaded += loadedCount;
                    Debug.Log($"[SPUM] Loaded {loadedCount} items from {tab.name}");
                }
                else
                {
                    Debug.LogError($"[SPUM] Failed to load from {tab.name}: {request.error}");
                }
            }
        }
        
        isLoadingFromGoogleSheet = false;
        
        if (totalLoaded > 0)
        {
            // 데이터 저장
            EditorUtility.SetDirty(tagManager);
            
            ShowNotification(new GUIContent($"Loaded {totalLoaded} items to cache"));
            Debug.Log($"[SPUM] Total loaded to cache: {totalLoaded} items");
            
            // UI 새로고침
            Repaint();
        }
        else
        {
            ShowNotification(new GUIContent("No data loaded"));
        }
    }
    
    void ParseTSVData(string tsvData, string tabName)
    {
        if (string.IsNullOrEmpty(tsvData)) return;
        
        string[] lines = tsvData.Split('\n');
        if (lines.Length < 2) return;
        
        // 헤더 파싱
        string[] headers = lines[0].Split('\t');
        Dictionary<string, int> headerIndices = new Dictionary<string, int>();
        
        // 헤더 매핑
        for (int i = 0; i < headers.Length; i++)
        {
            string header = headers[i].Trim();
            if (header.Equals("FileName", System.StringComparison.OrdinalIgnoreCase))
                headerIndices["FileName"] = i;
            else if (header.Equals("Part", System.StringComparison.OrdinalIgnoreCase))
                headerIndices["Part"] = i;
            else if (header.Equals("Theme", System.StringComparison.OrdinalIgnoreCase))
                headerIndices["Theme"] = i;
            else if (header.Equals("Race", System.StringComparison.OrdinalIgnoreCase))
                headerIndices["Race"] = i;
            else if (header.Equals("Gender", System.StringComparison.OrdinalIgnoreCase))
                headerIndices["Gender"] = i;
            else if (header.Equals("Type", System.StringComparison.OrdinalIgnoreCase))
                headerIndices["Type"] = i;
            else if (header.Equals("Properties", System.StringComparison.OrdinalIgnoreCase))
                headerIndices["Properties"] = i;
            else if (header.Equals("Class", System.StringComparison.OrdinalIgnoreCase))
                headerIndices["Class"] = i;
            else if (header.Equals("Style", System.StringComparison.OrdinalIgnoreCase))
                headerIndices["Style"] = i;
            else if (header.Equals("Addon", System.StringComparison.OrdinalIgnoreCase))
                headerIndices["Addon"] = i;
        }
        
        // 데이터 파싱
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] values = line.Split('\t');
            
            var item = new SPUM_ImprovedTagManager.ImprovedCharacterDataItem();
            
            // 필수 필드
            if (headerIndices.ContainsKey("FileName") && headerIndices["FileName"] < values.Length)
                item.FileName = values[headerIndices["FileName"]].Trim();
            if (headerIndices.ContainsKey("Part") && headerIndices["Part"] < values.Length)
                item.Part = values[headerIndices["Part"]].Trim();
            
            if (string.IsNullOrEmpty(item.FileName) || string.IsNullOrEmpty(item.Part))
                continue;
            
            // 선택 필드
            if (headerIndices.ContainsKey("Theme") && headerIndices["Theme"] < values.Length)
                item.Theme = ParseCommaSeparated(values[headerIndices["Theme"]]);
            if (headerIndices.ContainsKey("Race") && headerIndices["Race"] < values.Length)
                item.Race = values[headerIndices["Race"]].Trim();
            if (headerIndices.ContainsKey("Gender") && headerIndices["Gender"] < values.Length)
                item.Gender = values[headerIndices["Gender"]].Trim();
            if (headerIndices.ContainsKey("Type") && headerIndices["Type"] < values.Length)
                item.Type = values[headerIndices["Type"]].Trim();
            if (headerIndices.ContainsKey("Properties") && headerIndices["Properties"] < values.Length)
                item.Properties = ParseCommaSeparated(values[headerIndices["Properties"]]);
            if (headerIndices.ContainsKey("Class") && headerIndices["Class"] < values.Length)
                item.Class = ParseCommaSeparated(values[headerIndices["Class"]]);
            if (headerIndices.ContainsKey("Style") && headerIndices["Style"] < values.Length)
                item.Style = ParseCommaSeparated(values[headerIndices["Style"]]);
            if (headerIndices.ContainsKey("Addon") && headerIndices["Addon"] < values.Length)
                item.Addon = values[headerIndices["Addon"]].Trim();
            
            tagManager.allCharacterData.Add(item);
        }
    }
    
    string[] ParseCommaSeparated(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new string[0];
        
        return input.Split(new[] { ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => !string.IsNullOrEmpty(s))
                   .ToArray();
    }
}

// Simple input dialog helper class
public class EditorInputDialog : EditorWindow
{
    private static string inputValue = "";
    private static string labelText = "";
    private static bool confirmed = false;
    
    public static string Show(string title, string label, string defaultValue = "")
    {
        inputValue = defaultValue;
        labelText = label;
        confirmed = false;
        
        var window = GetWindow<EditorInputDialog>(true, title, true);
        window.minSize = new Vector2(300, 100);
        window.maxSize = new Vector2(300, 100);
        window.ShowModal();
        
        return confirmed ? inputValue : null;
    }
    
    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelText, GUILayout.Width(80));
        inputValue = EditorGUILayout.TextField(inputValue);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(20);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("OK", GUILayout.Width(80)))
        {
            confirmed = true;
            Close();
        }
        
        if (GUILayout.Button("Cancel", GUILayout.Width(80)))
        {
            confirmed = false;
            Close();
        }
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
}
