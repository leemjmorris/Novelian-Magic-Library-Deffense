using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Novelian.Combat;

/// <summary>
/// SkillVFXDatabase 자동 생성 및 편집 에디터 도구
/// Hovl Studio 전체 에셋팩 기반:
/// - AAA Projectiles Vol 1 & Vol 2
/// - 3D Lasers Pack
/// - AOE Magic spells Vol.1
/// - Epic Toon VFX 3
/// - Toon projectiles
/// </summary>
public class SkillVFXDatabaseCreator : EditorWindow
{
    private SkillVFXDatabase database;
    private Vector2 scrollPos;

    // 스킬 ID별 프리팹 매핑 (에디터에서 수정 가능)
    private Dictionary<int, GameObject> vfxPrefabs = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> hitPrefabs = new Dictionary<int, GameObject>();

    // 스킬 정보
    private static readonly SkillInfo[] skillInfos = new SkillInfo[]
    {
        // SingleProjectile (1001-1010)
        new SkillInfo(1001, "자연탄", "SingleProjectile"),
        new SkillInfo(1002, "화염탄", "SingleProjectile"),
        new SkillInfo(1003, "얼음탄", "SingleProjectile"),
        new SkillInfo(1004, "번개탄", "SingleProjectile"),
        new SkillInfo(1005, "바람탄", "SingleProjectile"),
        new SkillInfo(1006, "마법탄", "SingleProjectile"),
        new SkillInfo(1007, "산성탄", "SingleProjectile"),
        new SkillInfo(1008, "어둠탄", "SingleProjectile"),
        new SkillInfo(1009, "태양탄", "SingleProjectile"),
        new SkillInfo(1010, "에너지탄", "SingleProjectile"),

        // ExplosiveProjectile (1101-1103)
        new SkillInfo(1101, "화염폭발탄", "ExplosiveProjectile"),
        new SkillInfo(1102, "산성폭발탄", "ExplosiveProjectile"),
        new SkillInfo(1103, "에너지폭발탄", "ExplosiveProjectile"),

        // BeamRay (1201-1206)
        new SkillInfo(1201, "전기광선", "BeamRay"),
        new SkillInfo(1202, "자연광선", "BeamRay"),
        new SkillInfo(1203, "어둠광선", "BeamRay"),
        new SkillInfo(1204, "마법광선", "BeamRay"),
        new SkillInfo(1205, "붉은광선", "BeamRay"),
        new SkillInfo(1206, "화염광선", "BeamRay"),

        // TargetAOE (1301-1306)
        new SkillInfo(1301, "운석", "TargetAOE"),
        new SkillInfo(1302, "운석2", "TargetAOE"),
        new SkillInfo(1303, "운석우", "TargetAOE"),
        new SkillInfo(1304, "번개낙뢰", "TargetAOE"),
        new SkillInfo(1305, "마법폭발", "TargetAOE"),
        new SkillInfo(1306, "에너지폭발", "TargetAOE"),

        // LinearAOE (1401-1402)
        new SkillInfo(1401, "검기파동", "LinearAOE"),
        new SkillInfo(1402, "마법광선공격", "LinearAOE"),

        // GroundAOE (1501-1502)
        new SkillInfo(1501, "에너지장판", "GroundAOE"),
        new SkillInfo(1502, "물대포장판", "GroundAOE"),

        // Barrier (1601-1602)
        new SkillInfo(1601, "나무방패", "Barrier"),
        new SkillInfo(1602, "얼음벽", "Barrier"),

        // Buff (1701-1702)
        new SkillInfo(1701, "에너지버프", "Buff"),
        new SkillInfo(1702, "나뭇잎버프", "Buff"),
    };

    [MenuItem("Novelian/Skill VFX Database Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillVFXDatabaseCreator>("Skill VFX Editor");
        window.minSize = new Vector2(600, 400);
    }

    [MenuItem("Novelian/Create Skill VFX Database (HOVL)")]
    public static void CreateDatabaseWithHOVL()
    {
        string path = "Assets/SkillVFXDatabase.asset";
        var existing = AssetDatabase.LoadAssetAtPath<SkillVFXDatabase>(path);

        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("덮어쓰기", "기존 SkillVFXDatabase가 있습니다. HOVL 프리팹으로 새로 만드시겠습니까?", "예", "아니오"))
            {
                Selection.activeObject = existing;
                return;
            }
            AssetDatabase.DeleteAsset(path);
        }

        var database = ScriptableObject.CreateInstance<SkillVFXDatabase>();
        SerializedObject so = new SerializedObject(database);
        SerializedProperty entriesProp = so.FindProperty("entries");
        entriesProp.ClearArray();

        // HOVL 경로 - 전체 에셋 팩
        string vol1Projectiles = "Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(transform)/";
        string vol1Hits = "Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/";
        string vol2 = "Assets/Hovl Studio/AAA Projectiles Vol 2/Prefabs/";
        string lasers = "Assets/Hovl Studio/3D Lasers Pack/Prefabs/";
        string aoe = "Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/";
        string epicToon = "Assets/Hovl Studio/Epic Toon VFX 3/Prefabs/";
        // Toon projectiles 경로 (필요 시 사용)
        // string toon = "Assets/Hovl Studio/Toon projectiles/Prefabs/";

        var mappings = new List<PrefabMapping>
        {
            // SingleProjectile (1001-1010) - AAA Projectiles Vol 1 & Vol 2
            new PrefabMapping(1001, vol2 + "Projectile 1 nature.prefab", vol2 + "Hit 1.prefab"),
            new PrefabMapping(1002, vol2 + "Projectile 4 fire.prefab", vol2 + "Hit 4.prefab"),
            new PrefabMapping(1003, vol2 + "Projectile 5 ice.prefab", vol2 + "Hit 5.prefab"),
            new PrefabMapping(1004, vol2 + "Projectile 3 electro.prefab", vol2 + "Hit 3.prefab"),
            new PrefabMapping(1005, vol2 + "Projectile 7 wind.prefab", vol2 + "Hit 7.prefab"),
            new PrefabMapping(1006, vol2 + "Projectile 6 magic.prefab", vol2 + "Hit 6.prefab"),
            new PrefabMapping(1007, vol2 + "Projectile 10 acid.prefab", vol2 + "Hit 10.prefab"),
            new PrefabMapping(1008, vol2 + "Projectile 20 black.prefab", vol2 + "Hit 20.prefab"),
            new PrefabMapping(1009, vol2 + "Projectile 19 sun.prefab", vol2 + "Hit 19.prefab"),
            new PrefabMapping(1010, vol2 + "Projectile 8 energy.prefab", vol2 + "Hit 8.prefab"),

            // ExplosiveProjectile (1101-1103)
            new PrefabMapping(1101, vol1Projectiles + "Projectile 19 circle bomb.prefab", vol1Hits + "Hit 25 orange explosion.prefab"),
            new PrefabMapping(1102, vol1Projectiles + "Projectile 12 slime.prefab", vol1Hits + "Hit 24 green explosion.prefab"),
            new PrefabMapping(1103, vol1Projectiles + "Projectile 17 nova violet.prefab", vol1Hits + "Hit 17 nova violet.prefab"),

            // BeamRay (1201-1206) - 3D Lasers Pack
            new PrefabMapping(1201, lasers + "Laser beam 2 electro.prefab", aoe + "Lightning hit.prefab"),
            new PrefabMapping(1202, lasers + "Laser beam 1 nature.prefab", aoe + "Magic hit.prefab"),
            new PrefabMapping(1203, lasers + "Laser beam 3 dark fire.prefab", aoe + "Magic hit.prefab"),
            new PrefabMapping(1204, lasers + "Laser beam 5 magic.prefab", aoe + "Magic hit.prefab"),
            new PrefabMapping(1205, lasers + "Laser beam 4 red.prefab", aoe + "Magic hit.prefab"),
            new PrefabMapping(1206, lasers + "Laser beam 10 fire.prefab", aoe + "Meteor hit.prefab"),

            // TargetAOE (1301-1306) - AOE Magic spells Vol.1
            new PrefabMapping(1301, aoe + "Meteor.prefab", aoe + "Meteor hit.prefab"),
            new PrefabMapping(1302, aoe + "Meteor 2.prefab", aoe + "Meteor hit 2.prefab"),
            new PrefabMapping(1303, aoe + "Meteor shower.prefab", aoe + "Meteor hit.prefab"),
            new PrefabMapping(1304, aoe + "Lightning strike.prefab", aoe + "Lightning hit.prefab"),
            new PrefabMapping(1305, aoe + "Magic attack.prefab", aoe + "Magic hit.prefab"),
            new PrefabMapping(1306, aoe + "Energy explosion.prefab", aoe + "Magic hit.prefab"),

            // LinearAOE (1401-1402) - Epic Toon VFX 3
            new PrefabMapping(1401, epicToon + "SwordAOE.prefab", epicToon + "Sword crater.prefab"),
            new PrefabMapping(1402, epicToon + "Magic ray attack.prefab", epicToon + "Magic ray.prefab"),

            // GroundAOE (1501-1502) - Epic Toon VFX 3
            new PrefabMapping(1501, epicToon + "Energy shockwave.prefab", epicToon + "Energy buff.prefab"),
            new PrefabMapping(1502, epicToon + "Water hit.prefab", epicToon + "Water hit.prefab"),

            // Barrier (1601-1602) - Epic Toon VFX 3
            new PrefabMapping(1601, epicToon + "Wood shield.prefab", null),
            new PrefabMapping(1602, epicToon + "Ice wall.prefab", null),

            // Buff (1701-1702) - Epic Toon VFX 3 & AOE
            new PrefabMapping(1701, epicToon + "Energy buff.prefab", null),
            new PrefabMapping(1702, aoe + "Leaves buff.prefab", null),
        };

        int index = 0;
        int successCount = 0;
        int failCount = 0;

        foreach (var mapping in mappings)
        {
            entriesProp.InsertArrayElementAtIndex(index);
            SerializedProperty entry = entriesProp.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("skillId").intValue = mapping.skillId;

            // VFX Prefab 로드
            if (!string.IsNullOrEmpty(mapping.vfxPath))
            {
                GameObject vfx = AssetDatabase.LoadAssetAtPath<GameObject>(mapping.vfxPath);
                entry.FindPropertyRelative("vfxPrefab").objectReferenceValue = vfx;
                if (vfx != null) successCount++;
                else
                {
                    failCount++;
                    Debug.LogWarning($"[SkillVFXDatabase] VFX not found: {mapping.vfxPath}");
                }
            }

            // Hit Prefab 로드
            if (!string.IsNullOrEmpty(mapping.hitPath))
            {
                GameObject hit = AssetDatabase.LoadAssetAtPath<GameObject>(mapping.hitPath);
                entry.FindPropertyRelative("hitPrefab").objectReferenceValue = hit;
            }

            index++;
        }

        so.ApplyModifiedProperties();
        AssetDatabase.CreateAsset(database, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = database;
        Debug.Log($"[SkillVFXDatabase] Created with HOVL prefabs: {successCount} success, {failCount} failed");
    }

    [MenuItem("Novelian/Create Empty Skill VFX Database")]
    public static void CreateEmptyDatabase()
    {
        string path = "Assets/SkillVFXDatabase.asset";
        var existing = AssetDatabase.LoadAssetAtPath<SkillVFXDatabase>(path);

        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("덮어쓰기", "기존 SkillVFXDatabase가 있습니다. 새로 만드시겠습니까?", "예", "아니오"))
            {
                Selection.activeObject = existing;
                return;
            }
        }

        var database = ScriptableObject.CreateInstance<SkillVFXDatabase>();
        SerializedObject so = new SerializedObject(database);
        SerializedProperty entriesProp = so.FindProperty("entries");
        entriesProp.ClearArray();

        for (int i = 0; i < skillInfos.Length; i++)
        {
            entriesProp.InsertArrayElementAtIndex(i);
            SerializedProperty entry = entriesProp.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("skillId").intValue = skillInfos[i].id;
            entry.FindPropertyRelative("vfxPrefab").objectReferenceValue = null;
            entry.FindPropertyRelative("hitPrefab").objectReferenceValue = null;
        }

        so.ApplyModifiedProperties();

        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.CreateAsset(database, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = database;
        Debug.Log($"[SkillVFXDatabase] Created empty database with {skillInfos.Length} entries");
    }

    private void OnEnable()
    {
        LoadDatabase();
    }

    private void LoadDatabase()
    {
        database = AssetDatabase.LoadAssetAtPath<SkillVFXDatabase>("Assets/SkillVFXDatabase.asset");

        vfxPrefabs.Clear();
        hitPrefabs.Clear();

        if (database != null)
        {
            SerializedObject so = new SerializedObject(database);
            SerializedProperty entriesProp = so.FindProperty("entries");

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty entry = entriesProp.GetArrayElementAtIndex(i);
                int skillId = entry.FindPropertyRelative("skillId").intValue;
                var vfx = entry.FindPropertyRelative("vfxPrefab").objectReferenceValue as GameObject;
                var hit = entry.FindPropertyRelative("hitPrefab").objectReferenceValue as GameObject;

                vfxPrefabs[skillId] = vfx;
                hitPrefabs[skillId] = hit;
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Skill VFX Database Editor (HOVL)", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUI.BeginChangeCheck();
        database = EditorGUILayout.ObjectField("Database", database, typeof(SkillVFXDatabase), false) as SkillVFXDatabase;
        if (EditorGUI.EndChangeCheck())
        {
            LoadDatabase();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("HOVL 프리팹으로 생성", GUILayout.Height(30)))
        {
            CreateDatabaseWithHOVL();
            LoadDatabase();
        }
        if (GUILayout.Button("빈 데이터베이스 생성", GUILayout.Height(30)))
        {
            CreateEmptyDatabase();
            LoadDatabase();
        }
        if (GUILayout.Button("새로고침", GUILayout.Height(30)))
        {
            LoadDatabase();
        }
        EditorGUILayout.EndHorizontal();

        if (database == null)
        {
            EditorGUILayout.HelpBox("SkillVFXDatabase를 선택하거나 새로 생성하세요.\n\nHovl Studio 에셋 팩:\n- AAA Projectiles Vol 1 & 2\n- 3D Lasers Pack\n- AOE Magic spells Vol.1\n- Epic Toon VFX 3\n- Toon projectiles", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID", GUILayout.Width(50));
        EditorGUILayout.LabelField("스킬명", GUILayout.Width(100));
        EditorGUILayout.LabelField("타입", GUILayout.Width(120));
        EditorGUILayout.LabelField("VFX Prefab", GUILayout.MinWidth(150));
        EditorGUILayout.LabelField("Hit Prefab", GUILayout.MinWidth(150));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        string currentType = "";

        foreach (var info in skillInfos)
        {
            if (info.type != currentType)
            {
                currentType = info.type;
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"━━━ {currentType} ━━━", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(info.id.ToString(), GUILayout.Width(50));
            EditorGUILayout.LabelField(info.name, GUILayout.Width(100));
            EditorGUILayout.LabelField(info.type, GUILayout.Width(120));

            EditorGUI.BeginChangeCheck();
            vfxPrefabs.TryGetValue(info.id, out GameObject vfx);
            vfx = EditorGUILayout.ObjectField(vfx, typeof(GameObject), false, GUILayout.MinWidth(150)) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                vfxPrefabs[info.id] = vfx;
            }

            EditorGUI.BeginChangeCheck();
            hitPrefabs.TryGetValue(info.id, out GameObject hit);
            hit = EditorGUILayout.ObjectField(hit, typeof(GameObject), false, GUILayout.MinWidth(150)) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                hitPrefabs[info.id] = hit;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("변경사항 저장", GUILayout.Height(40)))
        {
            SaveDatabase();
        }
        GUI.backgroundColor = Color.white;
    }

    private void SaveDatabase()
    {
        if (database == null)
        {
            Debug.LogError("[SkillVFXDatabase] Database is null!");
            return;
        }

        SerializedObject so = new SerializedObject(database);
        SerializedProperty entriesProp = so.FindProperty("entries");
        entriesProp.ClearArray();

        int index = 0;
        foreach (var info in skillInfos)
        {
            entriesProp.InsertArrayElementAtIndex(index);
            SerializedProperty entry = entriesProp.GetArrayElementAtIndex(index);

            entry.FindPropertyRelative("skillId").intValue = info.id;

            vfxPrefabs.TryGetValue(info.id, out GameObject vfx);
            entry.FindPropertyRelative("vfxPrefab").objectReferenceValue = vfx;

            hitPrefabs.TryGetValue(info.id, out GameObject hit);
            entry.FindPropertyRelative("hitPrefab").objectReferenceValue = hit;

            index++;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SkillVFXDatabase] Saved {index} entries");
    }

    private struct SkillInfo
    {
        public int id;
        public string name;
        public string type;

        public SkillInfo(int id, string name, string type)
        {
            this.id = id;
            this.name = name;
            this.type = type;
        }
    }

    private struct PrefabMapping
    {
        public int skillId;
        public string vfxPath;
        public string hitPath;

        public PrefabMapping(int id, string vfx, string hit)
        {
            skillId = id;
            vfxPath = vfx;
            hitPath = hit;
        }
    }
}
