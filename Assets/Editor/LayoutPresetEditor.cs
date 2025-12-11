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
        EditorGUILayout.LabelField("Scene References (Drag & Drop)", EditorStyles.boldLabel);
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

        EditorGUI.indentLevel--;
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

        // UI 정보 업데이트
        layoutId = preset.Layout_ID;
        layoutName = preset.Layout_Name;
        mapPrefabKey = preset.Map_Prefab_Key;

        Debug.Log($"[LayoutPresetEditor] Preset '{preset.Layout_Name}' applied to scene!");
        EditorUtility.DisplayDialog("Success", $"Preset '{preset.Layout_Name}' applied to scene!", "OK");
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
                    Map_Prefab_Key = values[26]
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

        // 헤더
        sb.AppendLine("Layout_ID,Layout_Name,Grid_Rows,Grid_Columns,Grid_Center_X,Grid_Center_Y,Grid_Center_Z,Grid_Spacing_X,Grid_Spacing_Z,Row_Gap,Protection_Pos_X,Protection_Pos_Y,Protection_Pos_Z,Camera_Pos_X,Camera_Pos_Y,Camera_Pos_Z,Camera_Rot_X,Camera_Rot_Y,Camera_Rot_Z,Spawn_Area_Count,Spawn_1_X,Spawn_1_Y,Spawn_1_Z,Spawn_2_X,Spawn_2_Y,Spawn_2_Z,Map_Prefab_Key");

        // 데이터
        foreach (var data in dataList)
        {
            sb.AppendLine($"{data.Layout_ID},{data.Layout_Name},{data.Grid_Rows},{data.Grid_Columns},{data.Grid_Center_X},{data.Grid_Center_Y},{data.Grid_Center_Z},{data.Grid_Spacing_X},{data.Grid_Spacing_Z},{data.Row_Gap},{data.Protection_Pos_X},{data.Protection_Pos_Y},{data.Protection_Pos_Z},{data.Camera_Pos_X},{data.Camera_Pos_Y},{data.Camera_Pos_Z},{data.Camera_Rot_X},{data.Camera_Rot_Y},{data.Camera_Rot_Z},{data.Spawn_Area_Count},{data.Spawn_1_X},{data.Spawn_1_Y},{data.Spawn_1_Z},{data.Spawn_2_X},{data.Spawn_2_Y},{data.Spawn_2_Z},{data.Map_Prefab_Key}");
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
