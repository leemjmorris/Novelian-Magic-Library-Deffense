using UnityEngine;
using UnityEditor;
using Novelian.Combat;

/// <summary>
/// 캐릭터 프리팹에 필요한 컴포넌트들을 자동으로 추가하고 설정하는 에디터 도구
/// 사용법: Tools > Character > Setup Prefabs 메뉴 클릭
/// Issue #447 - 개별 캐릭터 프리팹 시스템
/// </summary>
public class CharacterPrefabSetup : EditorWindow
{
    // 설정값들
    private RuntimeAnimatorController animatorController;
    private DefaultAsset targetFolder;
    private string targetFolderPath = "";

    // CapsuleCollider 설정 (Character_01 기준)
    private Vector3 colliderCenter = new Vector3(0f, 0.9f, 0f);
    private float colliderRadius = 0.4f;
    private float colliderHeight = 1.8f;

    // Layer 설정 (6 = Character 레이어)
    private int characterLayer = 6;

    [MenuItem("Tools/Character/Setup Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<CharacterPrefabSetup>("Character Prefab Setup");
    }

    private void OnEnable()
    {
        // Animator Controller 자동 로드
        animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/CharacterAnimatorController.controller");
    }

    private void OnGUI()
    {
        GUILayout.Label("Character Prefab Setup Tool", EditorStyles.boldLabel);
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

        GUILayout.Space(10);

        // 필수 에셋 설정
        EditorGUILayout.LabelField("Required Assets", EditorStyles.boldLabel);
        animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
            "Animator Controller", animatorController, typeof(RuntimeAnimatorController), false);

        GUILayout.Space(10);

        // CapsuleCollider 설정
        EditorGUILayout.LabelField("CapsuleCollider Settings", EditorStyles.boldLabel);
        colliderCenter = EditorGUILayout.Vector3Field("Center", colliderCenter);
        colliderRadius = EditorGUILayout.FloatField("Radius", colliderRadius);
        colliderHeight = EditorGUILayout.FloatField("Height", colliderHeight);

        GUILayout.Space(10);

        // Layer 설정
        characterLayer = EditorGUILayout.LayerField("Character Layer", characterLayer);

        GUILayout.Space(20);

        // 폴더 기반 실행 버튼
        GUI.enabled = !string.IsNullOrEmpty(targetFolderPath);
        if (GUILayout.Button("Setup Folder Prefabs", GUILayout.Height(50)))
        {
            SetupFolderPrefabs();
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        // 경고 메시지
        if (animatorController == null)
        {
            EditorGUILayout.HelpBox("Animator Controller를 할당해주세요!", MessageType.Warning);
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
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { targetFolderPath });

        if (prefabGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "폴더에 프리팹이 없습니다!", "확인");
            return;
        }

        // 확인 다이얼로그
        if (!EditorUtility.DisplayDialog("Character Prefab Setup",
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

            EditorUtility.DisplayProgressBar("Character Prefab Setup",
                $"Processing: {prefabName} ({i + 1}/{prefabGuids.Length})",
                (float)i / prefabGuids.Length);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            try
            {
                SetupCharacterPrefab(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                successCount++;
                Debug.Log($"[CharacterPrefabSetup] {prefabName} 설정 완료!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CharacterPrefabSetup] {prefabName} 설정 실패: {e.Message}");
                failCount++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Character Prefab Setup",
            $"완료!\n성공: {successCount}개\n실패: {failCount}개", "확인");
    }

    private void SetupCharacterPrefab(GameObject prefabRoot)
    {
        // 1. Tag와 Layer 설정
        prefabRoot.tag = "Character";
        SetLayerRecursively(prefabRoot, characterLayer);

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
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.constraints = RigidbodyConstraints.FreezePositionX |
                        RigidbodyConstraints.FreezePositionY |
                        RigidbodyConstraints.FreezePositionZ |
                        RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationY |
                        RigidbodyConstraints.FreezeRotationZ;

        // 4. CapsuleCollider 추가 (드래그 앤 드롭 감지용)
        CapsuleCollider capsuleCollider = prefabRoot.GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = prefabRoot.AddComponent<CapsuleCollider>();
        }
        capsuleCollider.center = colliderCenter;
        capsuleCollider.radius = colliderRadius;
        capsuleCollider.height = colliderHeight;
        capsuleCollider.direction = 1; // Y-Axis
        capsuleCollider.isTrigger = true;

        // 5. Character 스크립트 추가
        Character character = prefabRoot.GetComponent<Character>();
        if (character == null)
        {
            character = prefabRoot.AddComponent<Character>();
        }

        // Character 필드 설정 (SerializedObject 사용)
        SerializedObject characterSO = new SerializedObject(character);

        // characterObj 필드에 자기 자신 할당
        var characterObjProp = characterSO.FindProperty("characterObj");
        if (characterObjProp != null)
        {
            characterObjProp.objectReferenceValue = prefabRoot;
        }

        // characterAnimator 필드에 Animator 할당
        var animatorProp = characterSO.FindProperty("characterAnimator");
        if (animatorProp != null)
        {
            animatorProp.objectReferenceValue = animator;
        }

        characterSO.ApplyModifiedProperties();
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
