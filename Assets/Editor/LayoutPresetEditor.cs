using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 레이아웃 프리셋 에디터 도구 (자가 완결형 맵 프리팹 시스템)
/// Issue #420: 스테이지별 맵 레이아웃 프리셋 시스템
///
/// 자가 완결형 시스템:
/// - 그리드, 카메라, 스폰 위치 등 모든 설정은 맵 프리팹 내부 마커 컴포넌트에 저장
/// - CSV는 맵 프리팹 키만 저장 (Layout_ID, Layout_Name, Map_Prefab_Key)
/// - 맵 수정 시 CSV 동기화 불필요
///
/// 기능:
/// - 현재 씬의 오브젝트 위치를 수집하여 맵 프리팹에 마커 컴포넌트로 저장
/// - CSV에서 프리셋을 Load하여 씬에 맵 프리팹 적용
/// - 스테이지별 Layout_Type 할당 및 저장
/// </summary>
public class LayoutPresetEditor : EditorWindow
{
    // CSV 경로
    private const string CSV_PATH = "Assets/Data/CSV/LayoutPresetTable.csv";
    private const string STAGE_CSV_PATH = "Assets/Data/CSV/Stage/StageTable.csv";

    // EditorPrefs 키 (씬 오브젝트 참조 저장용)
    private const string PREF_KEY_PREFIX = "LayoutPresetEditor_";
    private const string PREF_MAP_PREFAB = PREF_KEY_PREFIX + "MapPrefab";

    // 프리셋 정보
    private int layoutId = 1;
    private string layoutName = "NewLayout";
    private string mapPrefabKey = "GrassMapTop60";

    // 맵 프리팹 참조
    private GameObject mapPrefab;

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

    // 마커 정보 표시용
    private bool showMarkerInfo = false;
    private GridMarker detectedGridMarker;
    private CameraTarget detectedCameraTarget;

    [MenuItem("Tools/Layout Preset Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<LayoutPresetEditor>("Layout Preset Editor");
        window.minSize = new Vector2(450, 700);
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

    private void SaveReferences()
    {
        if (mapPrefab != null)
        {
            string path = AssetDatabase.GetAssetPath(mapPrefab);
            EditorPrefs.SetString(PREF_MAP_PREFAB, path);
        }
        else
        {
            EditorPrefs.DeleteKey(PREF_MAP_PREFAB);
        }
    }

    private void LoadSavedReferences()
    {
        string path = EditorPrefs.GetString(PREF_MAP_PREFAB, "");
        if (!string.IsNullOrEmpty(path))
        {
            mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Layout Preset Editor (Self-Contained)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "자가 완결형 맵 프리팹 시스템:\n" +
            "- 그리드, 카메라, 스폰 위치는 맵 프리팹 내부 마커에 저장\n" +
            "- CSV는 맵 프리팹 키만 저장 (3컬럼)\n" +
            "- 맵 수정 시 CSV 동기화 불필요",
            MessageType.Info
        );
        EditorGUILayout.Space(10);

        DrawBasicInfo();
        EditorGUILayout.Space(10);

        DrawMapPrefabSection();
        EditorGUILayout.Space(10);

        DrawMarkerInfoSection();
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
        mapPrefabKey = EditorGUILayout.TextField("Map Prefab Key (Addressable)", mapPrefabKey);

        EditorGUI.indentLevel--;
    }

    private void DrawMapPrefabSection()
    {
        EditorGUILayout.LabelField("Map Prefab (Self-Contained)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        mapPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Map Prefab",
            mapPrefab,
            typeof(GameObject),
            false // 프로젝트 에셋만
        );

        if (mapPrefab != null)
        {
            // Addressable 키 자동 추출
            string extractedKey = ExtractAddressableKey(mapPrefab);
            if (!string.IsNullOrEmpty(extractedKey))
            {
                EditorGUILayout.LabelField($"Addressable Key: {extractedKey}", EditorStyles.miniLabel);
                if (mapPrefabKey != extractedKey)
                {
                    if (GUILayout.Button("Use Detected Key", GUILayout.Width(120)))
                    {
                        mapPrefabKey = extractedKey;
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("이 프리팹은 Addressables에 등록되지 않았습니다.", MessageType.Warning);
            }

            // 마커 컴포넌트 존재 여부 확인
            EditorGUILayout.Space(5);
            CheckMarkersInPrefab();
        }

        EditorGUI.indentLevel--;
    }

    private void CheckMarkersInPrefab()
    {
        if (mapPrefab == null) return;

        GridMarker gridMarker = mapPrefab.GetComponentInChildren<GridMarker>(true);
        CameraTarget cameraTarget = mapPrefab.GetComponentInChildren<CameraTarget>(true);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Marker Components:", EditorStyles.boldLabel);

        // GridMarker
        if (gridMarker != null)
        {
            EditorGUILayout.LabelField($"GridMarker: {gridMarker.gridRows}x{gridMarker.gridColumns}, Pos: {gridMarker.transform.localPosition}", EditorStyles.label);
            detectedGridMarker = gridMarker;
        }
        else
        {
            EditorGUILayout.LabelField("GridMarker: NOT FOUND", EditorStyles.label);
            detectedGridMarker = null;
        }

        // CameraTarget
        if (cameraTarget != null)
        {
            EditorGUILayout.LabelField($"CameraTarget: Pos: {cameraTarget.transform.localPosition}", EditorStyles.label);
            detectedCameraTarget = cameraTarget;
        }
        else
        {
            EditorGUILayout.LabelField("CameraTarget: NOT FOUND", EditorStyles.label);
            detectedCameraTarget = null;
        }

        // SpawnArea 확인 (Tag 기반)
        var spawners = mapPrefab.GetComponentsInChildren<NovelianMagicLibraryDefense.Spawners.MonsterSpawner>(true);
        EditorGUILayout.LabelField($"MonsterSpawners: {spawners.Length} found", EditorStyles.label);

        // Wall 확인
        var walls = mapPrefab.GetComponentsInChildren<Wall>(true);
        EditorGUILayout.LabelField($"Walls: {walls.Length} found", EditorStyles.label);

        EditorGUILayout.EndVertical();

        // 마커 추가 버튼
        if (gridMarker == null || cameraTarget == null)
        {
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Add Missing Markers to Prefab", GUILayout.Height(25)))
            {
                AddMissingMarkersToPrefab();
            }
        }
    }

    private void DrawMarkerInfoSection()
    {
        showMarkerInfo = EditorGUILayout.Foldout(showMarkerInfo, "Marker Details", true, EditorStyles.foldoutHeader);

        if (!showMarkerInfo || mapPrefab == null) return;

        EditorGUI.indentLevel++;

        if (detectedGridMarker != null)
        {
            EditorGUILayout.LabelField("GridMarker Settings:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Rows: {detectedGridMarker.gridRows}");
            EditorGUILayout.LabelField($"  Columns: {detectedGridMarker.gridColumns}");
            EditorGUILayout.LabelField($"  Spacing X: {detectedGridMarker.gridSpacingX}");
            EditorGUILayout.LabelField($"  Spacing Z: {detectedGridMarker.gridSpacingZ}");
            EditorGUILayout.LabelField($"  Row Gap: {detectedGridMarker.rowGap}");
            EditorGUILayout.LabelField($"  Position: {detectedGridMarker.transform.localPosition}");
        }

        EditorGUILayout.Space(5);

        if (detectedCameraTarget != null)
        {
            EditorGUILayout.LabelField("CameraTarget Settings:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Position: {detectedCameraTarget.transform.localPosition}");
            EditorGUILayout.LabelField($"  Rotation: {detectedCameraTarget.transform.localEulerAngles}");
        }

        EditorGUI.indentLevel--;
    }

    private void DrawExportSection()
    {
        EditorGUILayout.LabelField("Export to CSV", EditorStyles.boldLabel);

        if (GUILayout.Button("Export Layout to CSV", GUILayout.Height(30)))
        {
            ExportToCSV();
        }

        EditorGUILayout.HelpBox(
            "CSV에는 Layout_ID, Layout_Name, Map_Prefab_Key만 저장됩니다.\n" +
            "그리드/카메라/스폰 설정은 맵 프리팹 내부 마커에서 읽습니다.",
            MessageType.Info
        );
    }

    private void DrawLoadSection()
    {
        EditorGUILayout.LabelField("Load from CSV", EditorStyles.boldLabel);

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
        if (GUILayout.Button("Load Preset Info", GUILayout.Height(30)))
        {
            LoadPresetInfo();
        }
        if (GUILayout.Button("Refresh Presets", GUILayout.Height(30)))
        {
            LoadPresetsFromCSV();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStageLayoutAssignmentSection()
    {
        showStageAssignment = EditorGUILayout.Foldout(showStageAssignment, "Stage Layout Assignment", true, EditorStyles.foldoutHeader);

        if (!showStageAssignment) return;

        EditorGUILayout.Space(5);

        if (loadedPresets.Count == 0)
        {
            EditorGUILayout.HelpBox("No layout presets available. Export some presets first.", MessageType.Warning);
            return;
        }

        if (stageLayoutInfos.Count == 0)
        {
            EditorGUILayout.HelpBox("No stages found in StageTable.csv", MessageType.Info);
            if (GUILayout.Button("Refresh Stages"))
            {
                LoadStageLayoutInfos();
            }
            return;
        }

        // 프리셋 이름 배열 생성
        string[] layoutOptions = new string[loadedPresets.Count];
        int[] layoutIds = new int[loadedPresets.Count];
        for (int i = 0; i < loadedPresets.Count; i++)
        {
            layoutOptions[i] = $"{loadedPresets[i].Layout_ID}: {loadedPresets[i].Layout_Name}";
            layoutIds[i] = loadedPresets[i].Layout_ID;
        }

        // 스테이지 목록
        stageScrollPos = EditorGUILayout.BeginScrollView(stageScrollPos, GUILayout.Height(200));

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Stage ID", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("Chapter", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        for (int i = 0; i < stageLayoutInfos.Count; i++)
        {
            var stage = stageLayoutInfos[i];

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(stage.Stage_ID.ToString(), GUILayout.Width(80));
            EditorGUILayout.LabelField($"Ch.{stage.Chapter_Number}", GUILayout.Width(60));

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
            if (layoutIds[i] == layoutType) return i;
        }
        return 0;
    }

    #endregion

    #region Export

    private void ExportToCSV()
    {
        if (string.IsNullOrEmpty(mapPrefabKey))
        {
            EditorUtility.DisplayDialog("Error", "Map Prefab Key를 입력해주세요.", "OK");
            return;
        }

        // 기존 CSV 읽기
        List<LayoutPresetData> existingData = new List<LayoutPresetData>();
        if (File.Exists(CSV_PATH))
        {
            existingData = ReadCSV();
        }

        // 새 데이터 생성
        var newData = new LayoutPresetData
        {
            Layout_ID = layoutId,
            Layout_Name = layoutName,
            Map_Prefab_Key = mapPrefabKey
        };

        // 동일 ID가 있으면 덮어쓰기, 없으면 추가
        int existingIndex = existingData.FindIndex(x => x.Layout_ID == newData.Layout_ID);
        if (existingIndex >= 0)
        {
            existingData[existingIndex] = newData;
            Debug.Log($"[LayoutPresetEditor] Updated existing preset ID: {newData.Layout_ID}");
        }
        else
        {
            existingData.Add(newData);
            Debug.Log($"[LayoutPresetEditor] Added new preset ID: {newData.Layout_ID}");
        }

        // CSV 쓰기
        WriteCSV(existingData);

        AssetDatabase.Refresh();
        LoadPresetsFromCSV();

        EditorUtility.DisplayDialog("Success", $"Preset '{newData.Layout_Name}' exported to CSV!\n\nMap Prefab Key: {newData.Map_Prefab_Key}", "OK");
    }

    #endregion

    #region Load

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

    private void LoadPresetInfo()
    {
        if (loadedPresets.Count == 0 || selectedPresetIndex >= loadedPresets.Count)
        {
            EditorUtility.DisplayDialog("Error", "No preset selected!", "OK");
            return;
        }

        LayoutPresetData preset = loadedPresets[selectedPresetIndex];

        // UI 정보 업데이트
        layoutId = preset.Layout_ID;
        layoutName = preset.Layout_Name;
        mapPrefabKey = preset.Map_Prefab_Key;

        // Map Prefab 찾기
        FindMapPrefabByKey(preset.Map_Prefab_Key);

        Debug.Log($"[LayoutPresetEditor] Loaded preset: {preset.Layout_Name}");
        EditorUtility.DisplayDialog("Loaded", $"Preset '{preset.Layout_Name}' info loaded.\n\nMap Prefab Key: {preset.Map_Prefab_Key}", "OK");
    }

    private void FindMapPrefabByKey(string key)
    {
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;

        foreach (var group in settings.groups)
        {
            if (group == null) continue;

            foreach (var entry in group.entries)
            {
                if (entry.address == key)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    return;
                }
            }
        }

        Debug.LogWarning($"[LayoutPresetEditor] Map prefab with key '{key}' not found in Addressables.");
    }

    #endregion

    #region Marker Management

    private void AddMissingMarkersToPrefab()
    {
        if (mapPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Map Prefab을 먼저 선택해주세요.", "OK");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(mapPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog("Error", "프로젝트 에셋이 아닙니다.", "OK");
            return;
        }

        // 프리팹 편집 모드 진입
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        bool modified = false;

        // GridMarker 추가
        if (prefabRoot.GetComponentInChildren<GridMarker>(true) == null)
        {
            GameObject gridMarkerObj = new GameObject("GridMarker");
            gridMarkerObj.transform.SetParent(prefabRoot.transform);
            gridMarkerObj.transform.localPosition = new Vector3(0, 0, 20);
            gridMarkerObj.AddComponent<GridMarker>();
            // Tag는 Unity Editor에서 수동으로 설정해야 함
            modified = true;
            Debug.Log("[LayoutPresetEditor] Added GridMarker to prefab");
        }

        // CameraTarget 추가
        if (prefabRoot.GetComponentInChildren<CameraTarget>(true) == null)
        {
            GameObject cameraTargetObj = new GameObject("CameraTarget");
            cameraTargetObj.transform.SetParent(prefabRoot.transform);
            cameraTargetObj.transform.localPosition = new Vector3(0, 30, 20);
            cameraTargetObj.transform.localEulerAngles = new Vector3(90, 0, 0);
            cameraTargetObj.AddComponent<CameraTarget>();
            modified = true;
            Debug.Log("[LayoutPresetEditor] Added CameraTarget to prefab");
        }

        // 변경사항 저장
        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            EditorUtility.DisplayDialog("Success",
                "마커 컴포넌트가 추가되었습니다.\n\n" +
                "Unity Editor에서 Tag를 설정해주세요:\n" +
                "- GridMarker: 'GridMarker' 태그\n" +
                "- CameraTarget: 'CameraTarget' 태그",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "모든 마커가 이미 존재합니다.", "OK");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);

        // 프리팹 다시 로드
        mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
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
            if (values.Length < 3) continue;

            try
            {
                var data = new LayoutPresetData
                {
                    Layout_ID = int.Parse(values[0]),
                    Layout_Name = values[1],
                    Map_Prefab_Key = values[2]
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

        // 헤더 (3컬럼만)
        sb.AppendLine("Layout_ID,Layout_Name,Map_Prefab_Key");

        // 데이터
        foreach (var data in dataList)
        {
            sb.AppendLine($"{data.Layout_ID},{data.Layout_Name},{data.Map_Prefab_Key}");
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

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');
            if (values.Length < 13) continue;

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

    private string ExtractAddressableKey(GameObject prefab)
    {
        if (prefab == null) return null;

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath)) return null;

        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return null;

        var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
        return entry?.address;
    }

    #endregion
}
