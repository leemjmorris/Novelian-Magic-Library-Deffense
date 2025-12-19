using UnityEngine;
using UnityEditor;

/// <summary>
/// 몬스터 프리팹의 BoxCollider를 모델 크기에 맞게 자동 조정하는 에디터 툴
/// </summary>
public class MonsterColliderFitter : EditorWindow
{
    private float sizeMultiplierXZ = 0.8f; // XZ 크기 배율 (1.0 = 정확히 맞춤, 0.8 = 80% 크기)
    private float sizeMultiplierY = 2.0f; // Y 크기 배율 (2.0 = 모델 높이의 2배)
    private float heightOffset = 0f; // Y축 오프셋
    private bool includeChildren = true; // 자식 오브젝트의 Renderer도 포함
    private DefaultAsset targetFolder; // 드래그 앤 드롭용 폴더

    [MenuItem("Tools/Monster Collider Fitter")]
    public static void ShowWindow()
    {
        GetWindow<MonsterColliderFitter>("Monster Collider Fitter");
    }

    private void OnGUI()
    {
        GUILayout.Label("몬스터 콜라이더 자동 조정", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sizeMultiplierXZ = EditorGUILayout.Slider("XZ 크기 배율", sizeMultiplierXZ, 0.1f, 2f);
        sizeMultiplierY = EditorGUILayout.Slider("Y 크기 배율 (높이)", sizeMultiplierY, 0.1f, 3f);
        heightOffset = EditorGUILayout.FloatField("높이 오프셋", heightOffset);
        includeChildren = EditorGUILayout.Toggle("자식 Renderer 포함", includeChildren);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "사용법:\n" +
            "1. Project 창에서 몬스터 프리팹들을 선택 → '선택한 프리팹 조정'\n" +
            "2. 또는 아래에 폴더를 드래그 → '폴더 내 모든 프리팹 조정'",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("선택한 프리팹 조정", GUILayout.Height(30)))
        {
            FitSelectedPrefabs();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        // 폴더 드래그 앤 드롭 영역
        EditorGUILayout.LabelField("폴더 지정 (드래그 앤 드롭)", EditorStyles.boldLabel);
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "타겟 폴더",
            targetFolder,
            typeof(DefaultAsset),
            false);

        // 폴더가 유효한지 확인
        if (targetFolder != null)
        {
            string folderPath = AssetDatabase.GetAssetPath(targetFolder);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                EditorGUILayout.HelpBox("폴더만 드래그할 수 있습니다!", MessageType.Warning);
                targetFolder = null;
            }
            else
            {
                EditorGUILayout.LabelField($"경로: {folderPath}", EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.Space();

        GUI.enabled = targetFolder != null;
        if (GUILayout.Button("폴더 내 모든 프리팹 조정", GUILayout.Height(30)))
        {
            FitAllPrefabsInFolder();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("선택한 씬 오브젝트 조정 (테스트용)", GUILayout.Height(25)))
        {
            FitSelectedSceneObjects();
        }
    }

    private void FitSelectedPrefabs()
    {
        Object[] selectedObjects = Selection.objects;
        int count = 0;

        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // 프리팹 편집 모드로 열기
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            if (FitCollider(prefabRoot))
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                count++;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료", $"{count}개의 프리팹 콜라이더를 조정했습니다.", "확인");
    }

    private void FitAllPrefabsInFolder()
    {
        if (targetFolder == null)
        {
            EditorUtility.DisplayDialog("오류", "타겟 폴더를 먼저 지정해주세요.", "확인");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("오류", "유효한 폴더가 아닙니다.", "확인");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // Monster 또는 BossMonster 컴포넌트가 있는지 확인
            if (prefab.GetComponent<Monster>() == null && prefab.GetComponent<BossMonster>() == null)
                continue;

            // 프리팹 편집 모드로 열기
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);

            if (FitCollider(prefabRoot))
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                count++;
                Debug.Log($"[MonsterColliderFitter] 조정 완료: {path}");
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료", $"{count}개의 몬스터 프리팹 콜라이더를 조정했습니다.", "확인");
    }

    private void FitSelectedSceneObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int count = 0;

        foreach (GameObject obj in selectedObjects)
        {
            if (FitCollider(obj))
            {
                EditorUtility.SetDirty(obj);
                count++;
            }
        }

        EditorUtility.DisplayDialog("완료", $"{count}개의 오브젝트 콜라이더를 조정했습니다.", "확인");
    }

    private bool FitCollider(GameObject obj)
    {
        BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogWarning($"[MonsterColliderFitter] BoxCollider 없음: {obj.name}");
            return false;
        }

        // 모든 Renderer의 bounds 계산
        Bounds bounds = CalculateBounds(obj);
        if (bounds.size == Vector3.zero)
        {
            Debug.LogWarning($"[MonsterColliderFitter] Renderer 없음: {obj.name}");
            return false;
        }

        // BoxCollider 크기 및 중심 설정
        Vector3 localCenter = obj.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = bounds.size;

        // 스케일 적용 (월드 스케일 → 로컬 스케일)
        Vector3 scale = obj.transform.lossyScale;
        localSize.x /= scale.x;
        localSize.y /= scale.y;
        localSize.z /= scale.z;

        // 배율 적용 (XZ와 Y 따로)
        localSize.x *= sizeMultiplierXZ;
        localSize.z *= sizeMultiplierXZ;
        localSize.y *= sizeMultiplierY;

        // 높이 오프셋 적용
        localCenter.y += heightOffset;

        boxCollider.center = localCenter;
        boxCollider.size = localSize;

        Debug.Log($"[MonsterColliderFitter] {obj.name}: center={localCenter}, size={localSize}");
        return true;
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers;
        if (includeChildren)
        {
            renderers = obj.GetComponentsInChildren<Renderer>();
        }
        else
        {
            renderers = obj.GetComponents<Renderer>();
        }

        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.zero);

        Bounds bounds = new Bounds();
        bool first = true;

        foreach (Renderer renderer in renderers)
        {
            // ParticleSystem 등은 제외
            if (renderer is ParticleSystemRenderer) continue;
            if (!renderer.enabled) continue;

            if (first)
            {
                bounds = renderer.bounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }
}
