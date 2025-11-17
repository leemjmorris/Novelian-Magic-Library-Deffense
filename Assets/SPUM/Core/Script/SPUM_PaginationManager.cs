using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
public partial class SPUM_PaginationManager : MonoBehaviour
{
    public GameObject itemPrefab;
    public Transform contentParent;
    public Button prevButton;
    public Button nextButton;
    public Button ConvertAllButton;
    public Button ApplyPresetAllButton;
    public Button CloseButton;
    public Toggle BatchModeButton;
    public Toggle SelectAllToggleButton;
    public Text pageText;
    public SPUM_Manager SPUM_Manager;
    public SPUM_AnimationManager SPUM_AnimationManager;
    private SortedDictionary<int, SPUM_Prefabs> sortedItems = new SortedDictionary<int, SPUM_Prefabs>();
    private Queue<int> deletedIndexes = new Queue<int>();
    private int nextAvailableIndex = 0;
    private int currentPage = 1;
    private int itemsPerPage = 10;
    public GameObject PreviewPanel;
    void Start()
    {
        LoadPrefabs();
        // 버튼 이벤트 설정
        prevButton.onClick.AddListener(PrevPage);
        nextButton.onClick.AddListener(NextPage);
        ConvertAllButton.onClick.AddListener(ConvertAll);
        ApplyPresetAllButton.onClick.AddListener(ApplyPresetAll);
        // 초기 페이지 표시
        BatchModeButton.isOn = false;
        //DisplayPage();
    }
    public void ConvertAll()
    {
        List<SPUM_Prefabs> convertedPrefabs = new List<SPUM_Prefabs>();
        List<int> indicesToRemove = new List<int>();

        foreach (var kvp in sortedItems)
        {
            int prefabIndex = kvp.Key;
            SPUM_Prefabs savedPrefab = kvp.Value;
            Debug.Log(savedPrefab._code + " " + savedPrefab.EditChk);
            if(!savedPrefab.EditChk) continue;
            bool needsConversion = savedPrefab._version < SPUM_Manager._version || 
                                !SPUM_Manager.fileHandler.ValidateSpumFile(savedPrefab, SPUM_Manager).Item2.Count.Equals(0) || savedPrefab.EditChk;

            if (needsConversion)
            {
                SPUM_Manager.MissingPackageNames.Clear();
                var tuple = SPUM_Manager.fileHandler.ValidateSpumFile(savedPrefab, SPUM_Manager);
                SPUM_Manager.DebugList.Clear();

                switch (tuple.Item1)
                {
                    case 2:
                        SPUM_Manager.DebugList.AddRange(SPUM_Manager.ReSyncSpumElementDataList(tuple.Item2));
                        break;
                    case 1:
                        SPUM_Manager.DebugList.AddRange(tuple.Item2);
                        break;
                }

                var distinctPackageList = SPUM_Manager.MissingPackageNames.Distinct().ToList();
                if (distinctPackageList.Count.Equals(0))
                {
                    var UnitType = savedPrefab.UnitType.Equals("") ? "Unit" : savedPrefab.UnitType;
                    SPUM_Manager.SetUnitConverter(UnitType);
                    savedPrefab.PopulateAnimationLists();
                    var newPrefab = SPUM_Manager.SaveConvertPrefabs(savedPrefab);
                    
                    //SPUM_Manager.MoveOldPrefabBackup(savedPrefab);
                    convertedPrefabs.Add(newPrefab);
                    indicesToRemove.Add(prefabIndex);
                }
            }
        }

        // Remove old prefabs and add new ones
        foreach (int index in indicesToRemove)
        {
            DeleteUnit(index, sortedItems[index]);
        }

        foreach (var newPrefab in convertedPrefabs)
        {
            AddNewPrefab(newPrefab);
            newPrefab.EditChk = false;
        }
        SPUM_Manager.NewMake();
        DisplayPage();
    }
    public void ApplyPresetAll()
    {
        var presetDropdown = SPUM_AnimationManager.presetDropdown;
        var presetData = SPUM_AnimationManager.SPUM_PresetData;
        int selectedIndex = presetDropdown.value;
        if (selectedIndex >= 0 && selectedIndex < presetData.Presets.Count)
        {
            SPUM_Preset selectedPreset = presetData.Presets[selectedIndex];
            Debug.Log($"Applying preset: {selectedPreset.UnitType} - {selectedPreset.PresetName}");
            ApplyPresetToAll(selectedPreset);
        }
        else
        {
            Debug.LogError("Invalid preset selection");
        }
    }
    public void ApplyPresetToAll(SPUM_Preset preset)
    {
        List<SPUM_Prefabs> updatedPrefabs = new List<SPUM_Prefabs>();
        List<int> indicesToRemove = new List<int>();

        foreach (var kvp in sortedItems)
        {
            int prefabIndex = kvp.Key;
            SPUM_Prefabs savedPrefab = kvp.Value;
        
            if(!savedPrefab.EditChk) continue;
            // 프리셋 적용
            savedPrefab.spumPackages = preset.Packages;
            savedPrefab.PopulateAnimationLists();
            // 필요한 경우 추가적인 업데이트 로직
            savedPrefab._version = SPUM_Manager._version;
            savedPrefab.EditChk = false;

            // 새로운 프리팹으로 저장
            // var UnitType = savedPrefab.UnitType.Equals("") ? "Unit" : savedPrefab.UnitType;
            // SPUM_Manager.SetUnitConverter(UnitType);
            // var newPrefab = SPUM_Manager.SaveConvertPrefabs(savedPrefab);
            // updatedPrefabs.Add(newPrefab);
            // indicesToRemove.Add(prefabIndex);
            Debug.Log(savedPrefab._code);
            //EditorUtility.SetDirty(savedPrefab);
        }
            //AssetDatabase.SaveAssets();
 
        // 이전 프리팹 제거 및 새 프리팹 추가
        // foreach (int index in indicesToRemove)
        // {
        //     DeleteUnit(index, sortedItems[index]);
        // }

        // foreach (var newPrefab in updatedPrefabs)
        // {
        //     AddNewPrefab(newPrefab);
        // }

        SPUM_Manager.NewMake();
        DisplayPage();
    }

    public void LoadPrefabs(){
        LoadItems();
        DisplayPage();

        BatchModeButton.isOn = false;
    }
    void LoadItems()
    {
        if(sortedItems.Count> 0) return;
        sortedItems.Clear();
        deletedIndexes.Clear();
        nextAvailableIndex = 0;
        var SavedPrefabs = SPUM_Manager.fileHandler.Load(); // Resources.LoadAll<SPUM_Prefabs>("");
        foreach (var prefab in SavedPrefabs)
        {
            AddItemToSortedDictionary(prefab);
        }
    }
    void AddItemToSortedDictionary(SPUM_Prefabs prefab)
    {
        int index;
        if (deletedIndexes.Count > 0)
        {
            index = deletedIndexes.Dequeue();
        }
        else
        {
            index = nextAvailableIndex++;
        }
        sortedItems[index] = prefab;
    }
    public void AddNewPrefab(SPUM_Prefabs newPrefab)
    {
        AddItemToSortedDictionary(newPrefab);
        DisplayPage();
    }
    public void DeleteUnit(int index, SPUM_Prefabs prefab)
    {
        if (sortedItems.Remove(index))
        {
            deletedIndexes.Enqueue(index);
        }
        SPUM_Manager.DeleteUnit(prefab);
    }
    void DisplayPage()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        SelectAllToggleButton.onValueChanged.RemoveAllListeners();
        SelectAllToggleButton.isOn = false;

        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, sortedItems.Count); 
        int i = 0;
        foreach (var kvp in sortedItems)
        {
            if (i >= startIndex && i < endIndex)
            {

            GameObject PreviewElement = Instantiate(itemPrefab, contentParent);
            Text UnitName = PreviewElement.GetComponentInChildren<Text>();
            SPUM_Prefabs PreviewData = PreviewElement.GetComponentInChildren<SPUM_Prefabs>(); 
            SPUM_Prefabs SavedPrefab = kvp.Value;
            int prefabIndex = kvp.Key;
            PreviewData.spumPackages = SavedPrefab.spumPackages;
            PreviewData.ImageElement = SavedPrefab.ImageElement;

            var UnitType = SavedPrefab.UnitType.Equals("") ? "Unit" : SavedPrefab.UnitType;

            foreach (Transform child in PreviewData.transform)
            {
                child.gameObject.SetActive(child.name.Contains(UnitType));
            }
            var anim = PreviewData.GetComponentInChildren<Animator>();
            PreviewData._anim = anim;

            PreviewData.UnitType = UnitType;
            PreviewData.PopulateAnimationLists();
            PreviewData.OverrideControllerInit();
            if(contentParent.gameObject.activeInHierarchy) PreviewData.PlayAnimation(PlayerState.IDLE, 0);

            var matchingTables = PreviewElement.GetComponentsInChildren<SPUM_MatchingList>();
            bool isInvalidPath = false;
            var allMatchingElements = matchingTables.SelectMany(mt => mt.matchingTables).ToList();
            foreach (var matchingElement in allMatchingElements)
            {
                var matchingTypeElement = SavedPrefab.ImageElement.FirstOrDefault(ie => 
                (ie.UnitType == matchingElement.UnitType)
                && (ie.PartType == matchingElement.PartType)
                //&& ie.Index == matchingElement.Index
                && (ie.Dir == matchingElement.Dir)
                && (ie.Structure == matchingElement.Structure) 
                && ie.PartSubType == matchingElement.PartSubType
                );
                //Debug.Log(matchingTypeElement != null);
                if (matchingTypeElement != null)
                {
                    var LoadSprite = SPUM_Manager.LoadSpriteFromMultiple(matchingTypeElement.ItemPath , matchingTypeElement.Structure);
                    isInvalidPath = LoadSprite == null;
                    matchingElement.renderer.sprite = LoadSprite;
                    matchingElement.renderer.maskInteraction = (SpriteMaskInteraction)matchingTypeElement.MaskIndex;
                    matchingElement.renderer.color = matchingTypeElement.Color; 
                    matchingElement.ItemPath = matchingTypeElement.ItemPath;
                    matchingElement.MaskIndex = matchingTypeElement.MaskIndex;
                    matchingElement.Color = matchingTypeElement.Color;
                }
            }
            var ButtonPanel = PreviewElement.GetComponentInChildren<SPUM_LoadPrefabPanel>();
            ButtonPanel.UnitCodeText.text = SavedPrefab._code;

            ButtonPanel.CheckedButton.isOn = SavedPrefab.EditChk;
            ButtonPanel.CheckedButton.onValueChanged.AddListener((On) => {
                SavedPrefab.EditChk = On;
            });
            SelectAllToggleButton.onValueChanged.AddListener((On) => {
                if (ButtonPanel != null && ButtonPanel.CheckedButton != null)
                {
                    ButtonPanel.CheckedButton.isOn = On;
                }
            });
            BatchModeButton.onValueChanged.AddListener((On) => {
                if (ButtonPanel != null && ButtonPanel.CheckedButton != null)
                {
                    ButtonPanel.CheckedButton.gameObject.SetActive(On);
                    ButtonPanel.DeleteButton.gameObject.SetActive(!On);
                    ButtonPanel.SelectButton.gameObject.SetActive(!On);
                    CloseButton.gameObject.SetActive(!On);
                }
            });
            if (ButtonPanel != null && ButtonPanel.CheckedButton != null)
            {
                ButtonPanel.CheckedButton.gameObject.SetActive(BatchModeButton.isOn);
                ButtonPanel.DeleteButton.gameObject.SetActive(!BatchModeButton.isOn);
                ButtonPanel.SelectButton.gameObject.SetActive(!BatchModeButton.isOn);
            }
            ButtonPanel.SelectButton.onClick.AddListener(()=>
            {
                SPUM_Manager.EditPrefab = SavedPrefab;
                SPUM_Manager.UIManager.LoadButtonSet(true);
                SPUM_Manager.ItemLoadButtonActive(SavedPrefab.ImageElement);
                SPUM_Manager.ItemResetAll();
                SPUM_Manager.SetType(SavedPrefab.UnitType);
                SPUM_Manager.SetSprite(SavedPrefab.ImageElement);
                SPUM_Manager.PreviewPrefab.spumPackages = SavedPrefab.spumPackages;
                
                SPUM_Manager.PreviewPrefab._version = SavedPrefab._version;
                SPUM_Manager.PreviewPrefab._code = SavedPrefab._code;
                SPUM_Manager.UIManager._loadObjCanvas.SetActive(false);

                SPUM_Manager.animationManager.PlayFirstAnimation();
            });

            ButtonPanel.DeleteButton.onClick.AddListener(()=> {
                DeleteUnit(prefabIndex, SavedPrefab);
                DisplayPage();
            });
            bool isOldVersion =  SavedPrefab._version < SPUM_Manager._version;
            
            bool isInvalidClipPath = false;
            var ClipList = SavedPrefab.spumPackages.SelectMany(package => package.SpumAnimationData).ToList();
            
            foreach (var clip in ClipList)
            {
                isInvalidClipPath = SPUM_Manager.ValidateAnimationClips(clip);
                if(!isInvalidClipPath) break;
            }

            bool IsActive = isOldVersion || isInvalidPath || !isInvalidClipPath;
            ButtonPanel.ConvertButton.transform.parent.gameObject.SetActive(IsActive);
            ButtonPanel.SelectButton.image.enabled = !IsActive;


            ButtonPanel.ConvertButton.onClick.AddListener(()=> {
                SPUM_Manager.MissingPackageNames.Clear();
                var tupple = SPUM_Manager.fileHandler.ValidateSpumFile(SavedPrefab, SPUM_Manager);
                SPUM_Manager.DebugList.Clear();
                SPUM_Manager.UIManager.ConvertView.PrefabVersion.text = $"Prefab Version {SavedPrefab._version}";

                
                switch (tupple.Item1)
                {
                    case 2:
                        Debug.Log("2.0 Convert");
                        var data = SPUM_Manager.ReSyncSpumElementDataList(tupple.Item2);
                        SPUM_Manager.DebugList.AddRange(data);
                       

                    break;
                    case 1:
                        Debug.Log("1.0 Convert");
                        var data2 = tupple.Item2;
                        SPUM_Manager.DebugList.AddRange(data2);
                    break;
                    default:
                    break;
                }
                SPUM_Manager.SetUnitConverter(UnitType);
                var DistinctPackageList = SPUM_Manager.MissingPackageNames.Distinct().ToList();
                ConvertAllButton.interactable = IsActive;
                SPUM_Manager.UIManager.ConvertView.Convert.interactable = DistinctPackageList.Count.Equals(0);

                SPUM_Manager.UIManager.ConvertView.Convert.onClick.AddListener(()=>{
                    var newPrefab = SPUM_Manager.SaveConvertPrefabs(SavedPrefab);
                    //SPUM_Manager.MoveOldPrefabBackup(SavedPrefab);
       
                    DeleteUnit(prefabIndex, SavedPrefab);
                    AddNewPrefab(newPrefab);
                    DisplayPage();
                    });
                
                PreviewPanel.SetActive(true);
                //LoadPrefabs();
            });
            }
            i++;
            if (i >= endIndex) break;
        }

        // 페이지 텍스트 업데이트
        pageText.text = $"Page {currentPage} / {Mathf.Ceil(sortedItems.Count / (float)itemsPerPage)}";

        // 버튼 활성화/비활성화
        prevButton.interactable = (currentPage > 1);
        nextButton.interactable = (endIndex < sortedItems.Count);
    }

    void PrevPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            DisplayPage();
        }
    }

    void NextPage()
    {
        if (currentPage < Mathf.CeilToInt(sortedItems.Count / (float)itemsPerPage))
        {
            currentPage++;
            DisplayPage();
        }
    }
}