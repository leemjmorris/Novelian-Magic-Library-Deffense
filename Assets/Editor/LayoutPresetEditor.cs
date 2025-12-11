using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 레이아웃 프리셋 에디터 도구
/// Issue #420: 스테이지별 맵 레이아웃 프리셋 시스템
///
/// 기능:
/// - 현재 씬의 오브젝트 위치를 수집하여 CSV로 Export
/// - CSV에서 프리셋을 Load하여 씬에 적용
/// - 스테이지별 Layout_Type 할당 및 저장
/// </summary>
public class LayoutPresetEditor : EditorWindow
{
    // CSV 경로
    private const string CSV_PATH = "Assets/Data/CSV/LayoutPresetTable.csv";
    private const string STAGE_CSV_PATH = "Assets/Data/CSV/StageTable.csv";

    // EditorPrefs 키 (씬 오브젝트 참조 저장용)
    private const string PREF_KEY_PREFIX = "LayoutPresetEditor_";
    private const string PREF_CPM = PREF_KEY_PREFIX + "CharacterPlacementManager";
    private const string PREF_PROTECTION = PREF_KEY_PREFIX + "ProtectionObj";
    private const string PREF_CAMERA = PREF_KEY_PREFIX + "CinemachineCamera";
    private const string PREF_SPAWN1 = PREF_KEY_PREFIX + "SpawnArea1";
    private const string PREF_SPAWN2 = PREF_KEY_PREFIX + "SpawnArea2";
    private const string PREF_PROTECTION2 = PREF_KEY_PREFIX + "ProtectionObj2";
    private const string PREF_DUAL_DEFENSE = PREF_KEY_PREFIX + "EnableDualDefense";
    private const string PREF_GRID2_OFFSET = PREF_KEY_PREFIX + "Grid2CenterOffset";
    private const string PREF_MAP_OBJECT = PREF_KEY_PREFIX + "MapObject";

    // 프리셋 정보
    private int layoutId = 1;
    private string layoutName = "NewLayout";
    private string mapPrefabKey = "Map_Default";

    // 씬 오브젝트 참조
    private CharacterPlacementManager characterPlacementManager;
    private Transform protectionObj;
    private CinemachineCamera cinemachineCamera;
    private Transform spawnArea1;
    private Transform spawnArea2;

    // Dual Defense Layout (양방향 방어)
    private bool enableDualDefense = false;
    private Transform protectionObj2;
    private Vector3 grid2CenterOffset = Vector3.zero;
    private bool isLoadingPreset = false;  // Load 중 자동 설정/해제 방지용

    // Map Object (Terrain + 장식물)
    private GameObject mapObject;

    // 수집된 값 표시용
    private bool showCollectedValues = false;
    private LayoutPresetData collectedData;

    // Load용
    private List<LayoutPresetData> loadedPresets = new List<LayoutPresetData>();
    private int selectedPresetIndex = 0;
    private string[] presetNames;

    // 스크롤
    private Vector2 scrollPos;

    // Stage Layout Assignment
    private List<StageLayoutInfo> stageLayoutInfos = new List<StageLayoutInfo>();
    private bool showStageAssignment = true;
    private Vector2 stageScrollPos;

    // Stage Layout Info 구조체
    private class StageLayoutInfo
    {
        public int Stage_ID;
        public int Stage_Name_ID;
        public int Chapter_Number;
        public int Wave_1_ID;
        public int Wave_2_ID;
        public int Wave_3_ID;
        public int Wave_4_ID;
        public float Time_Limit;
        public float Barrier_HP;
        public int Reward_Group_ID;
        public int AP_Cost_ID;
        public int AP_Cost;
        public int Layout_Type;
    }

    [MenuItem("Tools/Layout Preset Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<LayoutPresetEditor>("Layout Preset Editor");
        window.minSize = new Vector2(400, 600);
    }

    private void OnEnable()
    {
        LoadPresetsFromCSV();
        LoadStageLayoutInfos();
        LoadSavedReferences();
    }

    private void OnDisable()
    {
        SaveReferences();
    }

    /// <summary>
    /// 씬 오브젝트 참조를 EditorPrefs에 저장
    /// GlobalObjectId를 사용하여 씬 오브젝트를 문자열로 저장
    /// </summary>
    private void SaveReferences()
    {
        SaveObjectReference(PREF_CPM, characterPlacementManager);
        SaveObjectReference(PREF_PROTECTION, protectionObj);
        SaveObjectReference(PREF_CAMERA, cinemachineCamera);
        SaveObjectReference(PREF_SPAWN1, spawnArea1);
        SaveObjectReference(PREF_SPAWN2, spawnArea2);
        SaveObjectReference(PREF_PROTECTION2, protectionObj2);
        SaveObjectReference(PREF_MAP_OBJECT, mapObject);

        EditorPrefs.SetBool(PREF_DUAL_DEFENSE, enableDualDefense);
        EditorPrefs.SetString(PREF_GRID2_OFFSET, $"{grid2CenterOffset.x},{grid2CenterOffset.y},{grid2CenterOffset.z}");
    }

    /// <summary>
    /// EditorPrefs에서 씬 오브젝트 참조 복원
    /// </summary>
    private void LoadSavedReferences()
    {
        characterPlacementManager = LoadObjectReference<CharacterPlacementManager>(PREF_CPM);
        protectionObj = LoadObjectReference<Transform>(PREF_PROTECTION);
        cinemachineCamera = LoadObjectReference<CinemachineCamera>(PREF_CAMERA);
        spawnArea1 = LoadObjectReference<Transform>(PREF_SPAWN1);
        spawnArea2 = LoadObjectReference<Transform>(PREF_SPAWN2);
        protectionObj2 = LoadObjectReference<Transform>(PREF_PROTECTION2);
        mapObject = LoadObjectReference<GameObject>(PREF_MAP_OBJECT);

        enableDualDefense = EditorPrefs.GetBool(PREF_DUAL_DEFENSE, false);

        string offsetStr = EditorPrefs.GetString(PREF_GRID2_OFFSET, "0,0,0");
        string[] parts = offsetStr.Split(',');
        if (parts.Length == 3)
        {
            float.TryParse(parts[0], out float x);
            float.TryParse(parts[1], out float y);
            float.TryParse(parts[2], out float z);
            grid2CenterOffset = new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// 오브젝트 참조를 GlobalObjectId로 저장
    /// </summary>
    private void SaveObjectReference(string key, Object obj)
    {
        if (obj != null)
        {
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            EditorPrefs.SetString(key, id.ToString());
        }
        else
        {
            EditorPrefs.DeleteKey(key);
        }
    }

    /// <summary>
    /// GlobalObjectId에서 오브젝트 참조 복원
    /// </summary>
    private T LoadObjectReference<T>(string key) where T : Object
    {
        string idString = EditorPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(idString)) return null;

        if (GlobalObjectId.TryParse(idString, out GlobalObjectId id))
        {
            Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
            if (obj is T typedObj)
            {
                return typedObj;
            }
            // Transform 타입 요청 시 GameObject에서 Transform 추출
            else if (typeof(T) == typeof(Transform) && obj is GameObject go)
            {
                return go.transform as T;
            }
        }
        return null;
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Layout Preset Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        DrawBasicInfo();
        EditorGUILayout.Space(10);

        DrawReferences();
        EditorGUILayout.Space(10);

        DrawDualDefenseSection();
        EditorGUILayout.Space(10);

        DrawExportSection();
        EditorGUILayout.Space(20);

        DrawLoadSection();
        EditorGUILayout.Space(20);

        DrawStageLayoutAssignmentSection();

        EditorGUILayout.EndScrollView();
    }

    #region UI Sections

    private void DrawBasicInfo()
    {
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        layoutId = EditorGUILayout.IntField("Layout ID", layoutId);
        layoutName = EditorGUILayout.TextField("Layout Name", layoutName);
        mapPrefabKey = EditorGUILayout.TextField("Map Prefab Key", mapPrefabKey);

        EditorGUI.indentLevel--;
    }

    private void DrawReferences()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Scene References (Auto-saved)", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear All", GUILayout.Width(80)))
        {
            ClearAllReferences();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel++;

        characterPlacementManager = (CharacterPlacementManager)EditorGUILayout.ObjectField(
            "CharacterPlacementManager",
            characterPlacementManager,
            typeof(CharacterPlacementManager),
            true
        );

        protectionObj = (Transform)EditorGUILayout.ObjectField(
            "ProtectionObj",
            protectionObj,
            typeof(Transform),
            true
        );

        cinemachineCamera = (CinemachineCamera)EditorGUILayout.ObjectField(
            "CinemachineCamera",
            cinemachineCamera,
            typeof(CinemachineCamera),
            true
        );

        spawnArea1 = (Transform)EditorGUILayout.ObjectField(
            "SpawnArea1 (Required)",
            spawnArea1,
            typeof(Transform),
            true
        );

        spawnArea2 = (Transform)EditorGUILayout.ObjectField(
            "SpawnArea2 (Optional)",
            spawnArea2,
            typeof(Transform),
            true
        );

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Map Object (Terrain + Decorations)", EditorStyles.miniBoldLabel);

        mapObject = (GameObject)EditorGUILayout.ObjectField(
            "Map Object",
            mapObject,
            typeof(GameObject),
            true
        );

        // Map Prefab Key 자동 추출 표시
        if (mapObject != null)
        {
            string extractedKey = ExtractAddressableKeyFromMapObject();
            if (!string.IsNullOrEmpty(extractedKey))
            {
                EditorGUILayout.LabelField($"  → Detected Key: {extractedKey}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("Map Object에서 Addressable Key를 추출할 수 없습니다. Map Prefab Key를 직접 입력해주세요.", MessageType.Warning);
            }
        }

        EditorGUILayout.Space(5);

        // ProtectionObj2는 Dual Defense Section에서 관리하므로 여기서는 표시만
        if (protectionObj2 != null)
        {
            EditorGUILayout.LabelField($"ProtectionObj2: {protectionObj2.name} (Set in Dual Defense section)");
        }

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 모든 참조 초기화 및 EditorPrefs에서 삭제
    /// </summary>
    private void ClearAllReferences()
    {
        characterPlacementManager = null;
        protectionObj = null;
        cinemachineCamera = null;
        spawnArea1 = null;
        spawnArea2 = null;
        protectionObj2 = null;
        mapObject = null;
        enableDualDefense = false;
        grid2CenterOffset = Vector3.zero;

        // EditorPrefs에서도 삭제
        EditorPrefs.DeleteKey(PREF_CPM);
        EditorPrefs.DeleteKey(PREF_PROTECTION);
        EditorPrefs.DeleteKey(PREF_CAMERA);
        EditorPrefs.DeleteKey(PREF_SPAWN1);
        EditorPrefs.DeleteKey(PREF_SPAWN2);
        EditorPrefs.DeleteKey(PREF_PROTECTION2);
        EditorPrefs.DeleteKey(PREF_MAP_OBJECT);
        EditorPrefs.DeleteKey(PREF_DUAL_DEFENSE);
        EditorPrefs.DeleteKey(PREF_GRID2_OFFSET);

        Debug.Log("[LayoutPresetEditor] All references cleared");
    }

    private void DrawDualDefenseSection()
    {
        EditorGUILayout.LabelField("Dual Defense Layout (양방향 방어)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        // 토글
        bool prevEnableDualDefense = enableDualDefense;
        enableDualDefense = EditorGUILayout.Toggle("Enable Dual Defense", enableDualDefense);

        // 토글이 켜질 때 자동 생성 (Load 중에는 스킵)
        if (enableDualDefense && !prevEnableDualDefense && !isLoadingPreset)
        {
            AutoSetupDualDefense();
        }

        // 토글이 꺼질 때 자동 정리 (Load 중에는 스킵)
        if (!enableDualDefense && prevEnableDualDefense && !isLoadingPreset)
        {
            AutoCleanupDualDefense();
        }

        if (enableDualDefense)
        {
            EditorGUILayout.HelpBox(
                "양방향 방어 모드:\n" +
                "- Protection 2개 (상단/하단)\n" +
                "- Character Grid 2개 (상단/하단)\n" +
                "- 몬스터는 가장 가까운 Wall 공격\n" +
                "- 하나라도 파괴되면 게임 오버",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            // Protection 2 참조
            protectionObj2 = (Transform)EditorGUILayout.ObjectField(
                "ProtectionObj 2 (하단)",
                protectionObj2,
                typeof(Transform),
                true
            );

            // Grid 2 중심 위치
            grid2CenterOffset = EditorGUILayout.Vector3Field("Grid 2 Center Offset", grid2CenterOffset);

            EditorGUILayout.Space(10);

            // 수동 설정 버튼들
            EditorGUILayout.LabelField("Auto Setup", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create ProtectionObj 2", GUILayout.Height(25)))
            {
                CreateProtectionObj2();
            }
            if (GUILayout.Button("Auto Assign to Managers", GUILayout.Height(25)))
            {
                AutoAssignToManagers();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Full Auto Setup (Create + Assign)", GUILayout.Height(30)))
            {
                AutoSetupDualDefense();
            }
        }

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// Dual Defense 전체 자동 설정
    /// </summary>
    private void AutoSetupDualDefense()
    {
        // 1. ProtectionObj2 생성 (없으면)
        if (protectionObj2 == null)
        {
            CreateProtectionObj2();
        }

        // 2. Manager들에 자동 할당
        AutoAssignToManagers();

        Debug.Log("[LayoutPresetEditor] Dual Defense auto setup completed!");
    }

    /// <summary>
    /// ProtectionObj2 자동 생성 (ProtectionObj1 복제)
    /// </summary>
    private void CreateProtectionObj2()
    {
        if (protectionObj == null)
        {
            EditorUtility.DisplayDialog("Error", "ProtectionObj (1번)을 먼저 할당해주세요.", "OK");
            return;
        }

        // 기존 ProtectionObj2 찾기
        GameObject existingObj2 = GameObject.Find("ProtectionObj_2");
        if (existingObj2 != null)
        {
            protectionObj2 = existingObj2.transform;
            Debug.Log("[LayoutPresetEditor] Found existing ProtectionObj_2");
            return;
        }

        // ProtectionObj1 복제
        GameObject newProtectionObj2 = Instantiate(protectionObj.gameObject);
        newProtectionObj2.name = "ProtectionObj_2";

        // 위치 설정 (ProtectionObj1의 반대편)
        Vector3 pos1 = protectionObj.position;
        // 기본: Z축 반전 + 약간의 오프셋
        newProtectionObj2.transform.position = new Vector3(pos1.x, pos1.y, -pos1.z + 20f);

        // Wall 컴포넌트가 있으면 WallEvents도 새로 생성/할당
        Wall wall2Component = newProtectionObj2.GetComponent<Wall>();
        if (wall2Component != null)
        {
            // WallEvents2 ScriptableObject 찾기 또는 생성
            SetupWallEvents2(wall2Component);
        }

        // Undo 지원
        Undo.RegisterCreatedObjectUndo(newProtectionObj2, "Create ProtectionObj 2");

        protectionObj2 = newProtectionObj2.transform;
        EditorUtility.SetDirty(newProtectionObj2);

        Debug.Log($"[LayoutPresetEditor] Created ProtectionObj_2 at {newProtectionObj2.transform.position}");
        EditorUtility.DisplayDialog("Success", "ProtectionObj_2가 생성되었습니다!\n위치를 조정한 후 Collect & Export 해주세요.", "OK");
    }

    /// <summary>
    /// WallEvents2 설정
    /// </summary>
    private void SetupWallEvents2(Wall wall2Component)
    {
        // 기존 WallEvents2 찾기
        string[] guids = AssetDatabase.FindAssets("WallEvents2 t:WallEvents");
        NovelianMagicLibraryDefense.Events.WallEvents wallEvents2Asset = null;

        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            wallEvents2Asset = AssetDatabase.LoadAssetAtPath<NovelianMagicLibraryDefense.Events.WallEvents>(path);
        }

        // 없으면 새로 생성
        if (wallEvents2Asset == null)
        {
            // WallEvents1 찾아서 복제
            string[] wallEventsGuids = AssetDatabase.FindAssets("WallEvents t:WallEvents");
            if (wallEventsGuids.Length > 0)
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(wallEventsGuids[0]);
                var sourceAsset = AssetDatabase.LoadAssetAtPath<NovelianMagicLibraryDefense.Events.WallEvents>(sourcePath);

                if (sourceAsset != null)
                {
                    // 새 WallEvents2 생성
                    wallEvents2Asset = ScriptableObject.CreateInstance<NovelianMagicLibraryDefense.Events.WallEvents>();
                    string newPath = sourcePath.Replace("WallEvents.asset", "WallEvents2.asset");
                    if (newPath == sourcePath)
                    {
                        newPath = "Assets/ScriptableObjects/Events/WallEvents2.asset";
                    }
                    AssetDatabase.CreateAsset(wallEvents2Asset, newPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[LayoutPresetEditor] Created WallEvents2 at {newPath}");
                }
            }
        }

        // Wall2에 WallEvents2 할당
        if (wallEvents2Asset != null)
        {
            var serializedWall = new SerializedObject(wall2Component);
            var wallEventsProperty = serializedWall.FindProperty("wallEvents");
            if (wallEventsProperty != null)
            {
                wallEventsProperty.objectReferenceValue = wallEvents2Asset;
                serializedWall.ApplyModifiedProperties();
                Debug.Log("[LayoutPresetEditor] WallEvents2 assigned to Wall2");
            }
        }
    }

    /// <summary>
    /// StageManager, StageStateManager에 자동 할당
    /// </summary>
    private void AutoAssignToManagers()
    {
        if (protectionObj2 == null)
        {
            EditorUtility.DisplayDialog("Error", "ProtectionObj2가 없습니다. 먼저 생성해주세요.", "OK");
            return;
        }

        // Wall2 컴포넌트 가져오기
        Wall wall2Component = protectionObj2.GetComponent<Wall>();

        // WallEvents2 찾기
        NovelianMagicLibraryDefense.Events.WallEvents wallEvents2Asset = null;
        string[] guids = AssetDatabase.FindAssets("WallEvents2 t:WallEvents");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            wallEvents2Asset = AssetDatabase.LoadAssetAtPath<NovelianMagicLibraryDefense.Events.WallEvents>(path);
        }

        int assignedCount = 0;

        // StageManager 찾아서 할당
        var stageManager = FindFirstObjectByType<NovelianMagicLibraryDefense.Managers.StageManager>();
        if (stageManager != null)
        {
            var serializedSM = new SerializedObject(stageManager);

            var protectionObj2Prop = serializedSM.FindProperty("protectionObj2");
            if (protectionObj2Prop != null)
            {
                protectionObj2Prop.objectReferenceValue = protectionObj2;
            }

            var wallComponent2Prop = serializedSM.FindProperty("wallComponent2");
            if (wallComponent2Prop != null && wall2Component != null)
            {
                wallComponent2Prop.objectReferenceValue = wall2Component;
            }

            serializedSM.ApplyModifiedProperties();
            EditorUtility.SetDirty(stageManager);
            assignedCount++;
            Debug.Log("[LayoutPresetEditor] Assigned to StageManager");
        }

        // StageStateManager 찾아서 할당
        var stageStateManager = FindFirstObjectByType<NovelianMagicLibraryDefense.Managers.StageStateManager>();
        if (stageStateManager != null)
        {
            var serializedSSM = new SerializedObject(stageStateManager);

            var wall2Prop = serializedSSM.FindProperty("wall2");
            if (wall2Prop != null && wall2Component != null)
            {
                wall2Prop.objectReferenceValue = wall2Component;
            }

            var wallEvents2Prop = serializedSSM.FindProperty("wallEvents2");
            if (wallEvents2Prop != null && wallEvents2Asset != null)
            {
                wallEvents2Prop.objectReferenceValue = wallEvents2Asset;
            }

            serializedSSM.ApplyModifiedProperties();
            EditorUtility.SetDirty(stageStateManager);
            assignedCount++;
            Debug.Log("[LayoutPresetEditor] Assigned to StageStateManager");
        }

        if (assignedCount > 0)
        {
            EditorUtility.DisplayDialog("Success",
                $"{assignedCount}개의 Manager에 Dual Defense 참조가 할당되었습니다.\n\n" +
                "- StageManager: protectionObj2, wallComponent2\n" +
                "- StageStateManager: wall2, wallEvents2",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Warning", "씬에서 StageManager/StageStateManager를 찾을 수 없습니다.", "OK");
        }
    }

    private new T FindFirstObjectByType<T>() where T : Object
    {
        return Object.FindFirstObjectByType<T>();
    }

    /// <summary>
    /// Dual Defense 자동 정리 (토글 OFF 시)
    /// </summary>
    private void AutoCleanupDualDefense()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Dual Defense 비활성화",
            "Dual Defense를 비활성화합니다.\n\n" +
            "다음 중 하나를 선택하세요:\n" +
            "- 'Delete': ProtectionObj_2 삭제 + Manager 참조 해제\n" +
            "- 'Keep': ProtectionObj_2 비활성화만 (삭제 안 함)\n" +
            "- 'Cancel': 취소",
            "Delete",
            "Keep"
        );

        if (confirm)
        {
            // Delete 선택: 완전 삭제
            DeleteProtectionObj2();
        }
        else
        {
            // Keep 선택: 비활성화만
            DisableProtectionObj2();
        }

        // Manager 참조 해제
        ClearManagerReferences();
    }

    /// <summary>
    /// ProtectionObj_2 삭제
    /// </summary>
    private void DeleteProtectionObj2()
    {
        if (protectionObj2 != null)
        {
            Undo.DestroyObjectImmediate(protectionObj2.gameObject);
            protectionObj2 = null;
            Debug.Log("[LayoutPresetEditor] ProtectionObj_2 deleted");
        }

        // 씬에서 ProtectionObj_2 찾아서 삭제
        GameObject existingObj2 = GameObject.Find("ProtectionObj_2");
        if (existingObj2 != null)
        {
            Undo.DestroyObjectImmediate(existingObj2);
            Debug.Log("[LayoutPresetEditor] Found and deleted ProtectionObj_2 from scene");
        }
    }

    /// <summary>
    /// ProtectionObj_2 비활성화만
    /// </summary>
    private void DisableProtectionObj2()
    {
        if (protectionObj2 != null)
        {
            Undo.RecordObject(protectionObj2.gameObject, "Disable ProtectionObj 2");
            protectionObj2.gameObject.SetActive(false);
            Debug.Log("[LayoutPresetEditor] ProtectionObj_2 disabled (not deleted)");
        }
    }

    /// <summary>
    /// Manager들의 Dual Defense 참조 해제
    /// </summary>
    private void ClearManagerReferences()
    {
        // StageManager 참조 해제
        var stageManager = FindFirstObjectByType<NovelianMagicLibraryDefense.Managers.StageManager>();
        if (stageManager != null)
        {
            var serializedSM = new SerializedObject(stageManager);

            var protectionObj2Prop = serializedSM.FindProperty("protectionObj2");
            if (protectionObj2Prop != null)
            {
                protectionObj2Prop.objectReferenceValue = null;
            }

            var wallComponent2Prop = serializedSM.FindProperty("wallComponent2");
            if (wallComponent2Prop != null)
            {
                wallComponent2Prop.objectReferenceValue = null;
            }

            serializedSM.ApplyModifiedProperties();
            EditorUtility.SetDirty(stageManager);
            Debug.Log("[LayoutPresetEditor] StageManager references cleared");
        }

        // StageStateManager 참조 해제
        var stageStateManager = FindFirstObjectByType<NovelianMagicLibraryDefense.Managers.StageStateManager>();
        if (stageStateManager != null)
        {
            var serializedSSM = new SerializedObject(stageStateManager);

            var wall2Prop = serializedSSM.FindProperty("wall2");
            if (wall2Prop != null)
            {
                wall2Prop.objectReferenceValue = null;
            }

            var wallEvents2Prop = serializedSSM.FindProperty("wallEvents2");
            if (wallEvents2Prop != null)
            {
                wallEvents2Prop.objectReferenceValue = null;
            }

            serializedSSM.ApplyModifiedProperties();
            EditorUtility.SetDirty(stageStateManager);
            Debug.Log("[LayoutPresetEditor] StageStateManager references cleared");
        }

        // CharacterPlacementManager splitGridMode 해제
        if (characterPlacementManager != null)
        {
            var cpmSerializedObj = new SerializedObject(characterPlacementManager);
            cpmSerializedObj.FindProperty("splitGridMode").boolValue = false;
            cpmSerializedObj.ApplyModifiedProperties();
            EditorUtility.SetDirty(characterPlacementManager);
            Debug.Log("[LayoutPresetEditor] CharacterPlacementManager splitGridMode disabled");
        }

        // Grid 2 Center 초기화
        grid2CenterOffset = Vector3.zero;

        EditorUtility.DisplayDialog("완료", "Dual Defense가 비활성화되었습니다.", "OK");
    }

    private void DrawExportSection()
    {
        EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Collect Current Values", GUILayout.Height(30)))
        {
            CollectCurrentValues();
        }
        if (GUILayout.Button("Export to CSV", GUILayout.Height(30)))
        {
            ExportToCSV();
        }
        EditorGUILayout.EndHorizontal();

        // 수집된 값 표시
        if (showCollectedValues && collectedData != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Collected Values:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"Grid: {collectedData.Grid_Rows}x{collectedData.Grid_Columns}");
            EditorGUILayout.LabelField($"Grid Center: ({collectedData.Grid_Center_X}, {collectedData.Grid_Center_Y}, {collectedData.Grid_Center_Z})");
            EditorGUILayout.LabelField($"Grid Spacing: ({collectedData.Grid_Spacing_X}, {collectedData.Grid_Spacing_Z})");
            EditorGUILayout.LabelField($"Protection Pos: ({collectedData.Protection_Pos_X}, {collectedData.Protection_Pos_Y}, {collectedData.Protection_Pos_Z})");
            EditorGUILayout.LabelField($"Camera Pos: ({collectedData.Camera_Pos_X}, {collectedData.Camera_Pos_Y}, {collectedData.Camera_Pos_Z})");
            EditorGUILayout.LabelField($"Camera Rot: ({collectedData.Camera_Rot_X}, {collectedData.Camera_Rot_Y}, {collectedData.Camera_Rot_Z})");
            EditorGUILayout.LabelField($"Spawn Areas: {collectedData.Spawn_Area_Count}");
            EditorGUILayout.LabelField($"Spawn1: ({collectedData.Spawn_1_X}, {collectedData.Spawn_1_Y}, {collectedData.Spawn_1_Z})");
            if (collectedData.Spawn_Area_Count == 2)
            {
                EditorGUILayout.LabelField($"Spawn2: ({collectedData.Spawn_2_X}, {collectedData.Spawn_2_Y}, {collectedData.Spawn_2_Z})");
            }
            EditorGUILayout.LabelField($"Map Prefab: {collectedData.Map_Prefab_Key}");

            // Dual Defense 정보 표시
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Dual Defense:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Protection Count: {collectedData.Protection_Count}");
            EditorGUILayout.LabelField($"Grid Split Mode: {(collectedData.Grid_Split_Mode == 1 ? "Enabled" : "Disabled")}");
            if (collectedData.Protection_Count >= 2)
            {
                EditorGUILayout.LabelField($"Protection 2 Pos: ({collectedData.Protection_2_Pos_X}, {collectedData.Protection_2_Pos_Y}, {collectedData.Protection_2_Pos_Z})");
                EditorGUILayout.LabelField($"Grid 2 Center: ({collectedData.Grid_2_Center_X}, {collectedData.Grid_2_Center_Y}, {collectedData.Grid_2_Center_Z})");
            }

            EditorGUI.indentLevel--;
        }
    }

    private void DrawLoadSection()
    {
        EditorGUILayout.LabelField("Load", EditorStyles.boldLabel);

        if (loadedPresets.Count == 0)
        {
            EditorGUILayout.HelpBox("No presets found in CSV. Export one first.", MessageType.Info);
            if (GUILayout.Button("Refresh Presets"))
            {
                LoadPresetsFromCSV();
            }
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Select Preset:", GUILayout.Width(100));
        selectedPresetIndex = EditorGUILayout.Popup(selectedPresetIndex, presetNames);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load & Apply to Scene", GUILayout.Height(30)))
        {
            LoadAndApplyToScene();
        }
        if (GUILayout.Button("Refresh Presets", GUILayout.Height(30)))
        {
            LoadPresetsFromCSV();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStageLayoutAssignmentSection()
    {
        // 접기/펼치기
        showStageAssignment = EditorGUILayout.Foldout(showStageAssignment, "Stage Layout Assignment", true, EditorStyles.foldoutHeader);

        if (!showStageAssignment) return;

        EditorGUILayout.Space(5);

        // 프리셋이 없으면 안내 메시지
        if (loadedPresets.Count == 0)
        {
            EditorGUILayout.HelpBox("No layout presets available. Export some presets first.", MessageType.Warning);
            return;
        }

        // 스테이지가 없으면 안내 메시지
        if (stageLayoutInfos.Count == 0)
        {
            EditorGUILayout.HelpBox("No stages found in StageTable.csv", MessageType.Info);
            if (GUILayout.Button("Refresh Stages"))
            {
                LoadStageLayoutInfos();
            }
            return;
        }

        // 프리셋 이름 배열 생성 (드롭다운용)
        string[] layoutOptions = new string[loadedPresets.Count];
        int[] layoutIds = new int[loadedPresets.Count];
        for (int i = 0; i < loadedPresets.Count; i++)
        {
            layoutOptions[i] = $"{loadedPresets[i].Layout_ID}: {loadedPresets[i].Layout_Name}";
            layoutIds[i] = loadedPresets[i].Layout_ID;
        }

        // 스테이지 목록 (스크롤 가능)
        stageScrollPos = EditorGUILayout.BeginScrollView(stageScrollPos, GUILayout.Height(200));

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Stage ID", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("Chapter", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // 구분선
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 각 스테이지 행
        for (int i = 0; i < stageLayoutInfos.Count; i++)
        {
            var stage = stageLayoutInfos[i];

            EditorGUILayout.BeginHorizontal();

            // Stage ID
            EditorGUILayout.LabelField(stage.Stage_ID.ToString(), GUILayout.Width(80));

            // Chapter Number
            EditorGUILayout.LabelField($"Ch.{stage.Chapter_Number}", GUILayout.Width(60));

            // Layout 드롭다운
            int currentLayoutIndex = GetLayoutIndex(stage.Layout_Type, layoutIds);
            int newLayoutIndex = EditorGUILayout.Popup(currentLayoutIndex, layoutOptions);

            if (newLayoutIndex != currentLayoutIndex && newLayoutIndex >= 0 && newLayoutIndex < layoutIds.Length)
            {
                stage.Layout_Type = layoutIds[newLayoutIndex];
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        // 저장 버튼
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Stage Layouts", GUILayout.Height(30)))
        {
            SaveStageLayoutInfos();
        }

        if (GUILayout.Button("Refresh Stages", GUILayout.Height(30)))
        {
            LoadStageLayoutInfos();
        }

        EditorGUILayout.EndHorizontal();
    }

    private int GetLayoutIndex(int layoutType, int[] layoutIds)
    {
        for (int i = 0; i < layoutIds.Length; i++)
        {
            if (layoutIds[i] == layoutType)
            {
                return i;
            }
        }
        return 0; // 기본값: 첫 번째 레이아웃
    }

    #endregion

    #region Collect & Export

    private void CollectCurrentValues()
    {
        if (!ValidateReferences())
        {
            return;
        }

        // Map Object에서 Addressable Key 자동 추출 시도
        string extractedMapKey = ExtractAddressableKeyFromMapObject();
        if (!string.IsNullOrEmpty(extractedMapKey))
        {
            mapPrefabKey = extractedMapKey;
            Debug.Log($"[LayoutPresetEditor] Map Prefab Key auto-extracted: {mapPrefabKey}");
        }

        collectedData = new LayoutPresetData
        {
            Layout_ID = layoutId,
            Layout_Name = layoutName,
            Map_Prefab_Key = mapPrefabKey
        };

        // CharacterPlacementManager에서 그리드 정보 수집
        var cpmSerializedObj = new SerializedObject(characterPlacementManager);
        collectedData.Grid_Rows = cpmSerializedObj.FindProperty("gridRows").intValue;
        collectedData.Grid_Columns = cpmSerializedObj.FindProperty("gridColumns").intValue;
        collectedData.Grid_Spacing_X = cpmSerializedObj.FindProperty("gridSpacingX").floatValue;
        collectedData.Grid_Spacing_Z = cpmSerializedObj.FindProperty("gridSpacingZ").floatValue;
        collectedData.Row_Gap = cpmSerializedObj.FindProperty("rowGap").floatValue;

        var gridCenterOffset = cpmSerializedObj.FindProperty("gridCenterOffset").vector3Value;
        collectedData.Grid_Center_X = gridCenterOffset.x;
        collectedData.Grid_Center_Y = gridCenterOffset.y;
        collectedData.Grid_Center_Z = gridCenterOffset.z;

        // ProtectionObj 위치
        collectedData.Protection_Pos_X = protectionObj.position.x;
        collectedData.Protection_Pos_Y = protectionObj.position.y;
        collectedData.Protection_Pos_Z = protectionObj.position.z;

        // Camera 위치/회전
        collectedData.Camera_Pos_X = cinemachineCamera.transform.position.x;
        collectedData.Camera_Pos_Y = cinemachineCamera.transform.position.y;
        collectedData.Camera_Pos_Z = cinemachineCamera.transform.position.z;
        collectedData.Camera_Rot_X = cinemachineCamera.transform.eulerAngles.x;
        collectedData.Camera_Rot_Y = cinemachineCamera.transform.eulerAngles.y;
        collectedData.Camera_Rot_Z = cinemachineCamera.transform.eulerAngles.z;

        // SpawnArea
        collectedData.Spawn_Area_Count = spawnArea2 != null ? 2 : 1;
        collectedData.Spawn_1_X = spawnArea1.position.x;
        collectedData.Spawn_1_Y = spawnArea1.position.y;
        collectedData.Spawn_1_Z = spawnArea1.position.z;

        if (spawnArea2 != null)
        {
            collectedData.Spawn_2_X = spawnArea2.position.x;
            collectedData.Spawn_2_Y = spawnArea2.position.y;
            collectedData.Spawn_2_Z = spawnArea2.position.z;
        }
        else
        {
            collectedData.Spawn_2_X = 0;
            collectedData.Spawn_2_Y = 0;
            collectedData.Spawn_2_Z = 0;
        }

        // Dual Defense 데이터 수집
        if (enableDualDefense)
        {
            collectedData.Protection_Count = 2;
            collectedData.Grid_Split_Mode = 1;

            // Protection 2 위치
            if (protectionObj2 != null)
            {
                collectedData.Protection_2_Pos_X = protectionObj2.position.x;
                collectedData.Protection_2_Pos_Y = protectionObj2.position.y;
                collectedData.Protection_2_Pos_Z = protectionObj2.position.z;
            }
            else
            {
                // 기본값: Protection 1의 반대편 (Z축 반전)
                collectedData.Protection_2_Pos_X = collectedData.Protection_Pos_X;
                collectedData.Protection_2_Pos_Y = collectedData.Protection_Pos_Y;
                collectedData.Protection_2_Pos_Z = -collectedData.Protection_Pos_Z + 20f; // 반대편 추정
            }

            // Grid 2 중심 위치 (CharacterPlacementManager에서 읽기)
            var cpmGrid2Offset = cpmSerializedObj.FindProperty("grid2CenterOffset").vector3Value;
            collectedData.Grid_2_Center_X = cpmGrid2Offset.x;
            collectedData.Grid_2_Center_Y = cpmGrid2Offset.y;
            collectedData.Grid_2_Center_Z = cpmGrid2Offset.z;

            // 에디터 변수도 동기화
            grid2CenterOffset = cpmGrid2Offset;
        }
        else
        {
            collectedData.Protection_Count = 1;
            collectedData.Grid_Split_Mode = 0;
            collectedData.Protection_2_Pos_X = 0;
            collectedData.Protection_2_Pos_Y = 0;
            collectedData.Protection_2_Pos_Z = 0;
            collectedData.Grid_2_Center_X = 0;
            collectedData.Grid_2_Center_Y = 0;
            collectedData.Grid_2_Center_Z = 0;
        }

        showCollectedValues = true;
        Debug.Log("[LayoutPresetEditor] Values collected successfully!");
    }

    private void ExportToCSV()
    {
        if (collectedData == null)
        {
            EditorUtility.DisplayDialog("Error", "Please collect values first!", "OK");
            return;
        }

        // 기존 CSV 읽기
        List<LayoutPresetData> existingData = new List<LayoutPresetData>();
        bool fileExists = File.Exists(CSV_PATH);

        if (fileExists)
        {
            existingData = ReadCSV();
        }

        // 동일 ID가 있으면 덮어쓰기, 없으면 추가
        int existingIndex = existingData.FindIndex(x => x.Layout_ID == collectedData.Layout_ID);
        if (existingIndex >= 0)
        {
            existingData[existingIndex] = collectedData;
            Debug.Log($"[LayoutPresetEditor] Updated existing preset ID: {collectedData.Layout_ID}");
        }
        else
        {
            existingData.Add(collectedData);
            Debug.Log($"[LayoutPresetEditor] Added new preset ID: {collectedData.Layout_ID}");
        }

        // CSV 쓰기
        WriteCSV(existingData);

        AssetDatabase.Refresh();
        LoadPresetsFromCSV();

        EditorUtility.DisplayDialog("Success", $"Preset '{collectedData.Layout_Name}' exported to CSV!", "OK");
    }

    #endregion

    #region Load & Apply

    private void LoadPresetsFromCSV()
    {
        loadedPresets.Clear();

        if (!File.Exists(CSV_PATH))
        {
            presetNames = new string[0];
            return;
        }

        loadedPresets = ReadCSV();

        presetNames = new string[loadedPresets.Count];
        for (int i = 0; i < loadedPresets.Count; i++)
        {
            presetNames[i] = $"{loadedPresets[i].Layout_ID}: {loadedPresets[i].Layout_Name}";
        }

        if (selectedPresetIndex >= loadedPresets.Count)
        {
            selectedPresetIndex = 0;
        }
    }

    private void LoadAndApplyToScene()
    {
        if (loadedPresets.Count == 0 || selectedPresetIndex >= loadedPresets.Count)
        {
            EditorUtility.DisplayDialog("Error", "No preset selected!", "OK");
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        LayoutPresetData preset = loadedPresets[selectedPresetIndex];

        // CharacterPlacementManager에 그리드 정보 적용
        var cpmSerializedObj = new SerializedObject(characterPlacementManager);
        cpmSerializedObj.FindProperty("gridRows").intValue = preset.Grid_Rows;
        cpmSerializedObj.FindProperty("gridColumns").intValue = preset.Grid_Columns;
        cpmSerializedObj.FindProperty("gridSpacingX").floatValue = preset.Grid_Spacing_X;
        cpmSerializedObj.FindProperty("gridSpacingZ").floatValue = preset.Grid_Spacing_Z;
        cpmSerializedObj.FindProperty("rowGap").floatValue = preset.Row_Gap;
        cpmSerializedObj.FindProperty("gridCenterOffset").vector3Value = new Vector3(
            preset.Grid_Center_X,
            preset.Grid_Center_Y,
            preset.Grid_Center_Z
        );
        cpmSerializedObj.ApplyModifiedProperties();
        EditorUtility.SetDirty(characterPlacementManager);

        // ProtectionObj 위치 적용
        Undo.RecordObject(protectionObj, "Apply Layout Preset");
        protectionObj.position = new Vector3(
            preset.Protection_Pos_X,
            preset.Protection_Pos_Y,
            preset.Protection_Pos_Z
        );
        EditorUtility.SetDirty(protectionObj);

        // Camera 위치/회전 적용
        Undo.RecordObject(cinemachineCamera.transform, "Apply Layout Preset");
        cinemachineCamera.transform.position = new Vector3(
            preset.Camera_Pos_X,
            preset.Camera_Pos_Y,
            preset.Camera_Pos_Z
        );
        cinemachineCamera.transform.eulerAngles = new Vector3(
            preset.Camera_Rot_X,
            preset.Camera_Rot_Y,
            preset.Camera_Rot_Z
        );
        EditorUtility.SetDirty(cinemachineCamera);

        // SpawnArea1 위치 적용
        Undo.RecordObject(spawnArea1, "Apply Layout Preset");
        spawnArea1.position = new Vector3(
            preset.Spawn_1_X,
            preset.Spawn_1_Y,
            preset.Spawn_1_Z
        );
        EditorUtility.SetDirty(spawnArea1);

        // SpawnArea2 위치 적용 (있는 경우)
        if (spawnArea2 != null && preset.Spawn_Area_Count == 2)
        {
            Undo.RecordObject(spawnArea2, "Apply Layout Preset");
            spawnArea2.position = new Vector3(
                preset.Spawn_2_X,
                preset.Spawn_2_Y,
                preset.Spawn_2_Z
            );
            EditorUtility.SetDirty(spawnArea2);
        }

        // Dual Defense 적용 (isLoadingPreset 플래그로 자동 설정/해제 방지)
        isLoadingPreset = true;
        enableDualDefense = preset.Grid_Split_Mode == 1;
        grid2CenterOffset = new Vector3(
            preset.Grid_2_Center_X,
            preset.Grid_2_Center_Y,
            preset.Grid_2_Center_Z
        );
        isLoadingPreset = false;

        // CharacterPlacementManager에 Dual Defense 설정 적용
        cpmSerializedObj.FindProperty("splitGridMode").boolValue = enableDualDefense;
        cpmSerializedObj.FindProperty("grid2CenterOffset").vector3Value = grid2CenterOffset;
        cpmSerializedObj.ApplyModifiedProperties();
        EditorUtility.SetDirty(characterPlacementManager);

        // Protection 2 위치 적용 (있는 경우)
        if (enableDualDefense && protectionObj2 != null)
        {
            Undo.RecordObject(protectionObj2, "Apply Layout Preset");
            protectionObj2.position = new Vector3(
                preset.Protection_2_Pos_X,
                preset.Protection_2_Pos_Y,
                preset.Protection_2_Pos_Z
            );
            protectionObj2.gameObject.SetActive(true);
            EditorUtility.SetDirty(protectionObj2);
        }
        else if (protectionObj2 != null)
        {
            // Dual Defense가 아닐 때 Protection 2 비활성화
            protectionObj2.gameObject.SetActive(false);
        }

        // UI 정보 업데이트
        layoutId = preset.Layout_ID;
        layoutName = preset.Layout_Name;
        mapPrefabKey = preset.Map_Prefab_Key;

        // Map Object 교체 (프리팹 키가 다른 경우)
        SwapMapObjectByPrefabKey(preset.Map_Prefab_Key);

        string dualDefenseStatus = enableDualDefense ? " (Dual Defense)" : "";
        Debug.Log($"[LayoutPresetEditor] Preset '{preset.Layout_Name}'{dualDefenseStatus} applied to scene!");
        EditorUtility.DisplayDialog("Success", $"Preset '{preset.Layout_Name}'{dualDefenseStatus} applied to scene!", "OK");
    }

    /// <summary>
    /// Map Prefab Key에 해당하는 프리팹을 찾아 씬의 Map Object 교체
    /// </summary>
    private void SwapMapObjectByPrefabKey(string targetMapPrefabKey)
    {
        if (string.IsNullOrEmpty(targetMapPrefabKey))
        {
            Debug.LogWarning("[LayoutPresetEditor] Map Prefab Key가 비어있습니다.");
            return;
        }

        // 현재 맵의 키 확인
        string currentMapKey = ExtractAddressableKeyFromMapObject();
        if (currentMapKey == targetMapPrefabKey)
        {
            Debug.Log($"[LayoutPresetEditor] Map '{targetMapPrefabKey}' is already loaded, skipping swap.");
            return;
        }

        // Addressables에서 해당 키의 프리팹 찾기
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[LayoutPresetEditor] Addressables Settings not found.");
            return;
        }

        // 모든 그룹에서 해당 키를 가진 에셋 찾기
        GameObject targetPrefab = null;
        foreach (var group in settings.groups)
        {
            if (group == null) continue;

            foreach (var entry in group.entries)
            {
                if (entry.address == targetMapPrefabKey)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    break;
                }
            }
            if (targetPrefab != null) break;
        }

        if (targetPrefab == null)
        {
            Debug.LogWarning($"[LayoutPresetEditor] Map prefab with key '{targetMapPrefabKey}' not found in Addressables.");
            return;
        }

        // 현재 맵 오브젝트가 없으면 스킵
        if (mapObject == null)
        {
            Debug.LogWarning("[LayoutPresetEditor] Map Object가 설정되지 않았습니다.");
            return;
        }

        // 현재 맵의 Transform 정보 저장
        Vector3 position = mapObject.transform.position;
        Quaternion rotation = mapObject.transform.rotation;
        Vector3 scale = mapObject.transform.localScale;
        Transform parent = mapObject.transform.parent;
        int siblingIndex = mapObject.transform.GetSiblingIndex();

        // 기존 맵 삭제
        Undo.DestroyObjectImmediate(mapObject);

        // 새 맵 인스턴스 생성
        GameObject newMap = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
        newMap.transform.position = position;
        newMap.transform.rotation = rotation;
        newMap.transform.localScale = scale;
        newMap.transform.SetParent(parent);
        newMap.transform.SetSiblingIndex(siblingIndex);
        newMap.name = targetPrefab.name;

        Undo.RegisterCreatedObjectUndo(newMap, "Swap Map Object");

        // mapObject 참조 업데이트
        mapObject = newMap;
        EditorUtility.SetDirty(newMap);

        Debug.Log($"[LayoutPresetEditor] Map swapped to '{targetMapPrefabKey}' successfully!");
    }

    #endregion

    #region CSV Read/Write

    private List<LayoutPresetData> ReadCSV()
    {
        List<LayoutPresetData> result = new List<LayoutPresetData>();

        string[] lines = File.ReadAllLines(CSV_PATH, Encoding.UTF8);
        if (lines.Length < 2) return result;

        // 헤더 스킵, 데이터 파싱
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');
            if (values.Length < 27) continue;

            try
            {
                var data = new LayoutPresetData
                {
                    Layout_ID = int.Parse(values[0]),
                    Layout_Name = values[1],
                    Grid_Rows = int.Parse(values[2]),
                    Grid_Columns = int.Parse(values[3]),
                    Grid_Center_X = float.Parse(values[4]),
                    Grid_Center_Y = float.Parse(values[5]),
                    Grid_Center_Z = float.Parse(values[6]),
                    Grid_Spacing_X = float.Parse(values[7]),
                    Grid_Spacing_Z = float.Parse(values[8]),
                    Row_Gap = float.Parse(values[9]),
                    Protection_Pos_X = float.Parse(values[10]),
                    Protection_Pos_Y = float.Parse(values[11]),
                    Protection_Pos_Z = float.Parse(values[12]),
                    Camera_Pos_X = float.Parse(values[13]),
                    Camera_Pos_Y = float.Parse(values[14]),
                    Camera_Pos_Z = float.Parse(values[15]),
                    Camera_Rot_X = float.Parse(values[16]),
                    Camera_Rot_Y = float.Parse(values[17]),
                    Camera_Rot_Z = float.Parse(values[18]),
                    Spawn_Area_Count = int.Parse(values[19]),
                    Spawn_1_X = float.Parse(values[20]),
                    Spawn_1_Y = float.Parse(values[21]),
                    Spawn_1_Z = float.Parse(values[22]),
                    Spawn_2_X = float.Parse(values[23]),
                    Spawn_2_Y = float.Parse(values[24]),
                    Spawn_2_Z = float.Parse(values[25]),
                    Map_Prefab_Key = values[26],
                    // Dual Defense 컬럼 (27~34) - 기본값 제공
                    Protection_Count = values.Length > 27 ? int.Parse(values[27]) : 1,
                    Protection_2_Pos_X = values.Length > 28 ? float.Parse(values[28]) : 0,
                    Protection_2_Pos_Y = values.Length > 29 ? float.Parse(values[29]) : 0,
                    Protection_2_Pos_Z = values.Length > 30 ? float.Parse(values[30]) : 0,
                    Grid_Split_Mode = values.Length > 31 ? int.Parse(values[31]) : 0,
                    Grid_2_Center_X = values.Length > 32 ? float.Parse(values[32]) : 0,
                    Grid_2_Center_Y = values.Length > 33 ? float.Parse(values[33]) : 0,
                    Grid_2_Center_Z = values.Length > 34 ? float.Parse(values[34]) : 0
                };
                result.Add(data);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LayoutPresetEditor] Failed to parse line {i}: {e.Message}");
            }
        }

        return result;
    }

    private void WriteCSV(List<LayoutPresetData> dataList)
    {
        StringBuilder sb = new StringBuilder();

        // 헤더 (Dual Defense 컬럼 포함)
        sb.AppendLine("Layout_ID,Layout_Name,Grid_Rows,Grid_Columns,Grid_Center_X,Grid_Center_Y,Grid_Center_Z,Grid_Spacing_X,Grid_Spacing_Z,Row_Gap,Protection_Pos_X,Protection_Pos_Y,Protection_Pos_Z,Camera_Pos_X,Camera_Pos_Y,Camera_Pos_Z,Camera_Rot_X,Camera_Rot_Y,Camera_Rot_Z,Spawn_Area_Count,Spawn_1_X,Spawn_1_Y,Spawn_1_Z,Spawn_2_X,Spawn_2_Y,Spawn_2_Z,Map_Prefab_Key,Protection_Count,Protection_2_Pos_X,Protection_2_Pos_Y,Protection_2_Pos_Z,Grid_Split_Mode,Grid_2_Center_X,Grid_2_Center_Y,Grid_2_Center_Z");

        // 데이터 (Dual Defense 컬럼 포함)
        foreach (var data in dataList)
        {
            sb.AppendLine($"{data.Layout_ID},{data.Layout_Name},{data.Grid_Rows},{data.Grid_Columns},{data.Grid_Center_X},{data.Grid_Center_Y},{data.Grid_Center_Z},{data.Grid_Spacing_X},{data.Grid_Spacing_Z},{data.Row_Gap},{data.Protection_Pos_X},{data.Protection_Pos_Y},{data.Protection_Pos_Z},{data.Camera_Pos_X},{data.Camera_Pos_Y},{data.Camera_Pos_Z},{data.Camera_Rot_X},{data.Camera_Rot_Y},{data.Camera_Rot_Z},{data.Spawn_Area_Count},{data.Spawn_1_X},{data.Spawn_1_Y},{data.Spawn_1_Z},{data.Spawn_2_X},{data.Spawn_2_Y},{data.Spawn_2_Z},{data.Map_Prefab_Key},{data.Protection_Count},{data.Protection_2_Pos_X},{data.Protection_2_Pos_Y},{data.Protection_2_Pos_Z},{data.Grid_Split_Mode},{data.Grid_2_Center_X},{data.Grid_2_Center_Y},{data.Grid_2_Center_Z}");
        }

        File.WriteAllText(CSV_PATH, sb.ToString(), Encoding.UTF8);
    }

    #endregion

    #region Stage Layout CSV Read/Write

    private void LoadStageLayoutInfos()
    {
        stageLayoutInfos.Clear();

        if (!File.Exists(STAGE_CSV_PATH))
        {
            Debug.LogWarning($"[LayoutPresetEditor] StageTable.csv not found at: {STAGE_CSV_PATH}");
            return;
        }

        string[] lines = File.ReadAllLines(STAGE_CSV_PATH, Encoding.UTF8);
        if (lines.Length < 2) return;

        // 헤더 스킵, 데이터 파싱
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');
            if (values.Length < 13) continue; // Stage_ID ~ Layout_Type (13 columns)

            try
            {
                var info = new StageLayoutInfo
                {
                    Stage_ID = int.Parse(values[0]),
                    Stage_Name_ID = int.Parse(values[1]),
                    Chapter_Number = int.Parse(values[2]),
                    Wave_1_ID = int.Parse(values[3]),
                    Wave_2_ID = int.Parse(values[4]),
                    Wave_3_ID = int.Parse(values[5]),
                    Wave_4_ID = int.Parse(values[6]),
                    Time_Limit = float.Parse(values[7]),
                    Barrier_HP = float.Parse(values[8]),
                    Reward_Group_ID = int.Parse(values[9]),
                    AP_Cost_ID = int.Parse(values[10]),
                    AP_Cost = int.Parse(values[11]),
                    Layout_Type = int.Parse(values[12])
                };
                stageLayoutInfos.Add(info);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LayoutPresetEditor] Failed to parse StageTable line {i}: {e.Message}");
            }
        }

        Debug.Log($"[LayoutPresetEditor] Loaded {stageLayoutInfos.Count} stages from StageTable.csv");
    }

    private void SaveStageLayoutInfos()
    {
        if (stageLayoutInfos.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No stage data to save!", "OK");
            return;
        }

        StringBuilder sb = new StringBuilder();

        // 헤더
        sb.AppendLine("Stage_ID,Stage_Name_ID,Chapter_Number,Wave_1_ID,Wave_2_ID,Wave_3_ID,Wave_4_ID,Time_Limit,Barrier_HP,Reward_Group_ID,AP_Cost_ID,AP_Cost,Layout_Type");

        // 데이터
        for (int i = 0; i < stageLayoutInfos.Count; i++)
        {
            var stage = stageLayoutInfos[i];
            sb.AppendLine($"{stage.Stage_ID},{stage.Stage_Name_ID},{stage.Chapter_Number},{stage.Wave_1_ID},{stage.Wave_2_ID},{stage.Wave_3_ID},{stage.Wave_4_ID},{stage.Time_Limit},{stage.Barrier_HP},{stage.Reward_Group_ID},{stage.AP_Cost_ID},{stage.AP_Cost},{stage.Layout_Type}");
        }

        File.WriteAllText(STAGE_CSV_PATH, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"[LayoutPresetEditor] Saved {stageLayoutInfos.Count} stages to StageTable.csv");
        EditorUtility.DisplayDialog("Success", $"Stage layouts saved to StageTable.csv!", "OK");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Map Object에서 Addressable Key 추출
    /// 프리팹 인스턴스의 경우 원본 프리팹의 Addressable 키를 찾음
    /// </summary>
    private string ExtractAddressableKeyFromMapObject()
    {
        if (mapObject == null) return null;

        // 1. 프리팹 인스턴스인지 확인하고 원본 프리팹 경로 찾기
        string prefabPath = null;

        // 씬 오브젝트인 경우 원본 프리팹 찾기
        if (PrefabUtility.IsPartOfPrefabInstance(mapObject))
        {
            GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(mapObject);
            if (prefabAsset != null)
            {
                prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
            }
        }
        // 프로젝트 에셋인 경우 직접 경로 찾기
        else if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mapObject)))
        {
            prefabPath = AssetDatabase.GetAssetPath(mapObject);
        }

        if (string.IsNullOrEmpty(prefabPath)) return null;

        // 2. Addressables에서 해당 에셋의 키 찾기
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return null;

        var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
        if (entry != null)
        {
            return entry.address;
        }

        // 3. Addressable에 등록되지 않은 경우 프리팹 이름 반환 (수동 입력 필요)
        return null;
    }

    #endregion

    #region Validation

    private bool ValidateReferences()
    {
        List<string> missing = new List<string>();

        if (characterPlacementManager == null) missing.Add("CharacterPlacementManager");
        if (protectionObj == null) missing.Add("ProtectionObj");
        if (cinemachineCamera == null) missing.Add("CinemachineCamera");
        if (spawnArea1 == null) missing.Add("SpawnArea1");

        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("Missing References",
                $"Please assign the following:\n- {string.Join("\n- ", missing)}",
                "OK");
            return false;
        }

        return true;
    }

    #endregion
}
