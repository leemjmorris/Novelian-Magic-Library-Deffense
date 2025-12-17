using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using NovelianMagicLibraryDefense.Events;

/// <summary>
/// 몬스터 프리팹에 필요한 컴포넌트들을 자동으로 추가하고 설정하는 에디터 도구
/// 사용법: 폴더를 지정하고 "Setup Folder Prefabs" 버튼 클릭
/// </summary>
public class MonsterPrefabSetup : EditorWindow
{
    // 설정값들
    private MonsterEvents monsterEvents;
    private RuntimeAnimatorController animatorController;
    private DefaultAsset targetFolder;
    private string targetFolderPath = "";

    // 기본 스탯 (필요시 수정)
    private float maxHealth = 10000f;
    private float moveSpeed = 10f;
    private float damage = 10f;
    private float attackInterval = 0.7f;
    private float attackRange = 2f;

    // Collider 설정
    private Vector3 colliderCenter = new Vector3(0f, 1.5f, 0f);
    private Vector3 colliderSize = new Vector3(1.5f, 1.5f, 1f);

    // Layer 설정 (7 = Monster 레이어)
    private int monsterLayer = 7;

    // 하위 폴더 포함 여부
    private bool includeSubfolders = true;

    [MenuItem("Tools/Monster/Setup Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<MonsterPrefabSetup>("Monster Prefab Setup");
    }

    private void OnEnable()
    {
        // MonsterEvents 에셋 자동 로드
        monsterEvents = AssetDatabase.LoadAssetAtPath<MonsterEvents>(
            "Assets/Scenes/ScriptableObjects/Events/MonsterEvents.asset");
    }

    private void OnGUI()
    {
        GUILayout.Label("Monster Prefab Setup Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 폴더 설정
        EditorGUILayout.LabelField("Target Folder", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Prefab Folder", targetFolder, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck() && targetFolder != null)
        {
            targetFolderPath = AssetDatabase.GetAssetPath(targetFolder);
        }

        if (!string.IsNullOrEmpty(targetFolderPath))
        {
            EditorGUILayout.LabelField("Path:", targetFolderPath);

            // 폴더 내 프리팹 개수 미리보기
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { targetFolderPath });
            EditorGUILayout.LabelField($"프리팹 개수: {prefabGuids.Length}개");
        }

        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);

        GUILayout.Space(10);

        // 필수 에셋 설정
        EditorGUILayout.LabelField("Required Assets", EditorStyles.boldLabel);
        monsterEvents = (MonsterEvents)EditorGUILayout.ObjectField(
            "Monster Events", monsterEvents, typeof(MonsterEvents), false);
        animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
            "Animator Controller (Optional)", animatorController, typeof(RuntimeAnimatorController), false);

        GUILayout.Space(10);

        // 기본 스탯 설정
        EditorGUILayout.LabelField("Default Stats", EditorStyles.boldLabel);
        maxHealth = EditorGUILayout.FloatField("Max Health", maxHealth);
        moveSpeed = EditorGUILayout.FloatField("Move Speed", moveSpeed);
        damage = EditorGUILayout.FloatField("Damage", damage);
        attackInterval = EditorGUILayout.FloatField("Attack Interval", attackInterval);
        attackRange = EditorGUILayout.FloatField("Attack Range", attackRange);

        GUILayout.Space(10);

        // Collider 설정
        EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);
        colliderCenter = EditorGUILayout.Vector3Field("Center", colliderCenter);
        colliderSize = EditorGUILayout.Vector3Field("Size", colliderSize);

        GUILayout.Space(10);

        // Layer 설정
        monsterLayer = EditorGUILayout.LayerField("Monster Layer", monsterLayer);

        GUILayout.Space(20);

        // 폴더 기반 실행 버튼
        GUI.enabled = monsterEvents != null && !string.IsNullOrEmpty(targetFolderPath);
        if (GUILayout.Button("Setup Folder Prefabs", GUILayout.Height(50)))
        {
            SetupFolderPrefabs();
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        // 경고 메시지
        if (monsterEvents == null)
        {
            EditorGUILayout.HelpBox("MonsterEvents 에셋을 할당해주세요!", MessageType.Warning);
        }
        if (string.IsNullOrEmpty(targetFolderPath))
        {
            EditorGUILayout.HelpBox("프리팹이 있는 폴더를 드래그해서 놓으세요!", MessageType.Info);
        }
    }

    private void SetupFolderPrefabs()
    {
        if (string.IsNullOrEmpty(targetFolderPath))
        {
            EditorUtility.DisplayDialog("Error", "폴더를 지정해주세요!", "확인");
            return;
        }

        // 폴더 내 모든 프리팹 찾기
        string[] searchFolders = includeSubfolders
            ? new[] { targetFolderPath }
            : new[] { targetFolderPath };

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchFolders);

        if (prefabGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "폴더에 프리팹이 없습니다!", "확인");
            return;
        }

        // 확인 다이얼로그
        if (!EditorUtility.DisplayDialog("Monster Prefab Setup",
            $"{prefabGuids.Length}개의 프리팹을 설정하시겠습니까?", "예", "아니오"))
        {
            return;
        }

        int successCount = 0;
        int failCount = 0;

        // 프로그레스 바 표시
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            string prefabName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            EditorUtility.DisplayProgressBar("Monster Prefab Setup",
                $"Processing: {prefabName} ({i + 1}/{prefabGuids.Length})",
                (float)i / prefabGuids.Length);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            try
            {
                SetupMonsterPrefab(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                successCount++;
                Debug.Log($"[MonsterPrefabSetup] {prefabName} 설정 완료!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MonsterPrefabSetup] {prefabName} 설정 실패: {e.Message}");
                failCount++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Monster Prefab Setup",
            $"완료!\n성공: {successCount}개\n실패: {failCount}개", "확인");
    }

    private void SetupSelectedPrefabs()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int successCount = 0;
        int skipCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            // 프리팹인지 확인
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab"))
            {
                Debug.LogWarning($"[MonsterPrefabSetup] {obj.name}은(는) 프리팹이 아닙니다. 스킵합니다.");
                skipCount++;
                continue;
            }

            // 프리팹 수정 시작
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            try
            {
                SetupMonsterPrefab(prefabRoot);

                // 프리팹 저장
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                successCount++;
                Debug.Log($"[MonsterPrefabSetup] {obj.name} 설정 완료!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MonsterPrefabSetup] {obj.name} 설정 실패: {e.Message}");
            }
            finally
            {
                // 프리팹 콘텐츠 언로드
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        EditorUtility.DisplayDialog("Monster Prefab Setup",
            $"완료!\n성공: {successCount}개\n스킵: {skipCount}개", "확인");

        AssetDatabase.Refresh();
    }

    private void SetupMonsterPrefab(GameObject prefabRoot)
    {
        // 1. Tag와 Layer 설정
        prefabRoot.tag = "Monster";
        SetLayerRecursively(prefabRoot, monsterLayer);

        // 2. Animator 컴포넌트 (이미 있을 수 있음)
        Animator animator = prefabRoot.GetComponent<Animator>();
        if (animator == null)
        {
            animator = prefabRoot.AddComponent<Animator>();
        }
        if (animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }

        // 3. Rigidbody 추가
        Rigidbody rb = prefabRoot.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = prefabRoot.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 4. BoxCollider 추가
        BoxCollider boxCollider = prefabRoot.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = prefabRoot.AddComponent<BoxCollider>();
        }
        boxCollider.center = colliderCenter;
        boxCollider.size = colliderSize;
        boxCollider.isTrigger = false;

        // 5. NavMeshAgent 추가
        NavMeshAgent navAgent = prefabRoot.GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = prefabRoot.AddComponent<NavMeshAgent>();
        }
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = 120f;
        navAgent.acceleration = 8f;
        navAgent.stoppingDistance = 0.5f;
        navAgent.radius = 0.5f;
        navAgent.height = 2f;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // 6. MonsterMove 스크립트 추가
        MonsterMove monsterMove = prefabRoot.GetComponent<MonsterMove>();
        if (monsterMove == null)
        {
            monsterMove = prefabRoot.AddComponent<MonsterMove>();
        }

        // MonsterMove 필드 설정 (SerializedObject 사용)
        SerializedObject monsterMoveSO = new SerializedObject(monsterMove);
        monsterMoveSO.FindProperty("navAgent").objectReferenceValue = navAgent;
        monsterMoveSO.FindProperty("monsterAnimator").objectReferenceValue = animator;
        monsterMoveSO.ApplyModifiedProperties();

        // 7. Monster 스크립트 추가
        Monster monster = prefabRoot.GetComponent<Monster>();
        if (monster == null)
        {
            monster = prefabRoot.AddComponent<Monster>();
        }

        // Monster 필드 설정 (SerializedObject 사용)
        SerializedObject monsterSO = new SerializedObject(monster);
        monsterSO.FindProperty("monsterEvents").objectReferenceValue = monsterEvents;
        monsterSO.FindProperty("monsterAnimator").objectReferenceValue = animator;
        monsterSO.FindProperty("monsterMove").objectReferenceValue = monsterMove;
        monsterSO.FindProperty("rb").objectReferenceValue = rb;
        monsterSO.FindProperty("collider3D").objectReferenceValue = boxCollider;
        monsterSO.FindProperty("moveSpeed").floatValue = moveSpeed;
        monsterSO.FindProperty("damage").floatValue = damage;
        monsterSO.FindProperty("attackInterval").floatValue = attackInterval;
        monsterSO.FindProperty("attackRange").floatValue = attackRange;
        monsterSO.FindProperty("maxHealth").floatValue = maxHealth;
        monsterSO.ApplyModifiedProperties();
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
