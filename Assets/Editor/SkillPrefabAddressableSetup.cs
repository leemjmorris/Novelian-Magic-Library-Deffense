// SkillPrefabAddressableSetup.cs
// Hovl Studio 스킬 이펙트 프리팹을 Addressable에 등록하는 에디터 도구

using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.IO;
using System.Collections.Generic;

public class SkillPrefabAddressableSetup : EditorWindow
{
    private const string EFFECTS_PATH_SCRIPT_BASED = "Assets/SpecialSkillsEffectsPack/AllEffects/EffectsSet_2(ScriptBased)/Effects";
    private const string EFFECTS_PATH_NOT_SCRIPT = "Assets/SpecialSkillsEffectsPack/AllEffects/EffectsSet_1(NotScriptBased)/Effects";
    private const string ADDRESSABLE_GROUP_NAME = "SkillEffects";

    private Vector2 scrollPos;
    private List<PrefabInfo> foundPrefabs = new List<PrefabInfo>();
    private bool showScriptBased = true;
    private bool showNotScriptBased = true;

    private class PrefabInfo
    {
        public string path;
        public string name;
        public string addressableKey;
        public bool isScriptBased;
        public bool isRegistered;
        public bool selected;
    }

    [MenuItem("Tools/Skill System/Addressable Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillPrefabAddressableSetup>("Skill Addressable Setup");
        window.minSize = new Vector2(600, 500);
        window.ScanPrefabs();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("스킬 이펙트 Addressable 설정", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Hovl Studio 스킬 이펙트 프리팹을 Addressable에 등록합니다.\n" +
            "등록된 프리팹은 'Effects/프리팹이름' 형태의 주소로 로드할 수 있습니다.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 필터
        EditorGUILayout.BeginHorizontal();
        showScriptBased = EditorGUILayout.Toggle("ScriptBased", showScriptBased, GUILayout.Width(150));
        showNotScriptBased = EditorGUILayout.Toggle("NotScriptBased", showNotScriptBased, GUILayout.Width(150));

        if (GUILayout.Button("스캔", GUILayout.Width(80)))
        {
            ScanPrefabs();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 통계
        int registeredCount = 0;
        int selectedCount = 0;
        foreach (var prefab in foundPrefabs)
        {
            if (prefab.isRegistered) registeredCount++;
            if (prefab.selected) selectedCount++;
        }
        EditorGUILayout.LabelField($"총 {foundPrefabs.Count}개 프리팹 / 등록됨: {registeredCount}개 / 선택됨: {selectedCount}개");

        EditorGUILayout.Space(5);

        // 전체 선택/해제
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("미등록만 선택", GUILayout.Width(100)))
        {
            foreach (var prefab in foundPrefabs)
            {
                if (!prefab.isRegistered)
                    prefab.selected = true;
            }
        }
        if (GUILayout.Button("전체 선택", GUILayout.Width(80)))
        {
            foreach (var prefab in foundPrefabs)
                prefab.selected = true;
        }
        if (GUILayout.Button("전체 해제", GUILayout.Width(80)))
        {
            foreach (var prefab in foundPrefabs)
                prefab.selected = false;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 프리팹 목록
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        {
            foreach (var prefab in foundPrefabs)
            {
                // 필터 적용
                if (prefab.isScriptBased && !showScriptBased) continue;
                if (!prefab.isScriptBased && !showNotScriptBased) continue;

                EditorGUILayout.BeginHorizontal("box");
                {
                    // 선택 체크박스
                    prefab.selected = EditorGUILayout.Toggle(prefab.selected, GUILayout.Width(20));

                    // 등록 상태
                    if (prefab.isRegistered)
                    {
                        GUI.color = Color.green;
                        GUILayout.Label("●", GUILayout.Width(15));
                        GUI.color = Color.white;
                    }
                    else
                    {
                        GUI.color = Color.gray;
                        GUILayout.Label("○", GUILayout.Width(15));
                        GUI.color = Color.white;
                    }

                    // 타입
                    string typeLabel = prefab.isScriptBased ? "[S]" : "[N]";
                    GUILayout.Label(typeLabel, GUILayout.Width(30));

                    // 이름
                    GUILayout.Label(prefab.name, GUILayout.Width(250));

                    // Addressable 주소
                    GUILayout.Label(prefab.addressableKey, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // 액션 버튼
        EditorGUILayout.BeginHorizontal();
        {
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("선택된 프리팹 등록", GUILayout.Height(35)))
            {
                RegisterSelectedPrefabs();
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("모든 등록 해제", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("확인", "모든 스킬 이펙트 Addressable 등록을 해제하시겠습니까?", "예", "아니오"))
                {
                    UnregisterAllPrefabs();
                }
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // CSV 업데이트 버튼
        if (GUILayout.Button("CSV prefab_address 자동 업데이트", GUILayout.Height(30)))
        {
            UpdateCSVPrefabAddresses();
        }
    }

    private void ScanPrefabs()
    {
        foundPrefabs.Clear();

        var settings = AddressableAssetSettingsDefaultObject.Settings;

        // ScriptBased 프리팹 스캔
        if (Directory.Exists(EFFECTS_PATH_SCRIPT_BASED))
        {
            ScanDirectory(EFFECTS_PATH_SCRIPT_BASED, true, settings);
        }

        // NotScriptBased 프리팹 스캔
        if (Directory.Exists(EFFECTS_PATH_NOT_SCRIPT))
        {
            ScanDirectory(EFFECTS_PATH_NOT_SCRIPT, false, settings);
        }

        Debug.Log($"[SkillPrefabAddressableSetup] {foundPrefabs.Count}개 프리팹 스캔 완료");
    }

    private void ScanDirectory(string basePath, bool isScriptBased, AddressableAssetSettings settings)
    {
        string[] effectFolders = Directory.GetDirectories(basePath);

        foreach (string folder in effectFolders)
        {
            string folderName = Path.GetFileName(folder);

            // 각 이펙트 폴더 내의 프리팹 찾기
            string[] prefabFiles = Directory.GetFiles(folder, "*.prefab", SearchOption.TopDirectoryOnly);

            foreach (string prefabPath in prefabFiles)
            {
                string assetPath = prefabPath.Replace("\\", "/");
                string prefabName = Path.GetFileNameWithoutExtension(assetPath);

                // Addressable 키 생성: "Effects/Effect_01_EnergyStrike"
                string addressableKey = $"Effects/{prefabName}";

                // 등록 상태 확인
                bool isRegistered = false;
                if (settings != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    var entry = settings.FindAssetEntry(guid);
                    isRegistered = entry != null;
                }

                foundPrefabs.Add(new PrefabInfo
                {
                    path = assetPath,
                    name = prefabName,
                    addressableKey = addressableKey,
                    isScriptBased = isScriptBased,
                    isRegistered = isRegistered,
                    selected = !isRegistered
                });
            }
        }
    }

    private void RegisterSelectedPrefabs()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[SkillPrefabAddressableSetup] Addressable Settings를 찾을 수 없습니다.");
            return;
        }

        // 그룹 찾기 또는 생성
        var group = settings.FindGroup(ADDRESSABLE_GROUP_NAME);
        if (group == null)
        {
            group = settings.CreateGroup(ADDRESSABLE_GROUP_NAME, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            Debug.Log($"[SkillPrefabAddressableSetup] '{ADDRESSABLE_GROUP_NAME}' 그룹 생성");
        }

        int count = 0;
        foreach (var prefab in foundPrefabs)
        {
            if (!prefab.selected) continue;
            if (prefab.isRegistered) continue;

            string guid = AssetDatabase.AssetPathToGUID(prefab.path);
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);

            if (entry != null)
            {
                entry.address = prefab.addressableKey;
                prefab.isRegistered = true;
                prefab.selected = false;
                count++;
            }
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SkillPrefabAddressableSetup] {count}개 프리팹 Addressable 등록 완료");
        ScanPrefabs();
    }

    private void UnregisterAllPrefabs()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;

        var group = settings.FindGroup(ADDRESSABLE_GROUP_NAME);
        if (group == null) return;

        // 그룹의 모든 엔트리 제거
        var entries = new List<AddressableAssetEntry>(group.entries);
        foreach (var entry in entries)
        {
            settings.RemoveAssetEntry(entry.guid);
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log("[SkillPrefabAddressableSetup] 모든 등록 해제 완료");
        ScanPrefabs();
    }

    private void UpdateCSVPrefabAddresses()
    {
        string csvPath = "Assets/Data/CSV/Skill/MainSkillTable.csv";
        string fullPath = Path.Combine(Application.dataPath, csvPath.Substring(7));

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[SkillPrefabAddressableSetup] CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        // 등록된 프리팹 딕셔너리 생성
        var addressMap = new Dictionary<string, string>();
        foreach (var prefab in foundPrefabs)
        {
            if (prefab.isRegistered)
            {
                // 프리팹 이름에서 키워드 추출 (예: Effect_01_EnergyStrike → EnergyStrike)
                string keyword = ExtractKeyword(prefab.name);
                addressMap[keyword.ToLower()] = prefab.addressableKey;
            }
        }

        Debug.Log($"[SkillPrefabAddressableSetup] {addressMap.Count}개 프리팹 주소 매핑됨");
        EditorUtility.DisplayDialog("CSV 업데이트",
            $"CSV의 prefab_address 컬럼을 수동으로 업데이트하세요.\n\n" +
            $"등록된 프리팹: {addressMap.Count}개\n" +
            $"주소 형식: Effects/Effect_XX_Name",
            "확인");
    }

    private string ExtractKeyword(string prefabName)
    {
        // Effect_01_EnergyStrike → EnergyStrike
        string[] parts = prefabName.Split('_');
        if (parts.Length >= 3)
        {
            return string.Join("", parts, 2, parts.Length - 2);
        }
        return prefabName;
    }
}
