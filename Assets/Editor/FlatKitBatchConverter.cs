using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// FlatKit Stylized Surface 쉐이더 일괄 변환 도구
/// 카테고리별 프리셋 지원 + 실제 사용 Material만 분석
/// </summary>
public class FlatKitBatchConverter : EditorWindow
{
    // 카테고리 정의
    public enum MaterialCategory
    {
        Character,      // 플레이어 캐릭터
        Monster,        // 몬스터/적
        Environment,    // 환경/맵/배경
        Uncategorized   // 미분류
    }

    // 카테고리별 프리셋
    [System.Serializable]
    public class CategoryPreset
    {
        public string name;
        public float celShadingSize;
        public float shadowEdgeSize;
        public float flatness;
        public Color shadedColor;
        public bool enableOutline;
        public float outlineWidth;
        public Color outlineColor;

        public CategoryPreset(string name, float cel, float shadow, float flat, Color shaded, bool outline, float outlineW, Color outlineC)
        {
            this.name = name;
            this.celShadingSize = cel;
            this.shadowEdgeSize = shadow;
            this.flatness = flat;
            this.shadedColor = shaded;
            this.enableOutline = outline;
            this.outlineWidth = outlineW;
            this.outlineColor = outlineC;
        }
    }

    private Vector2 scrollPosition;
    private Dictionary<MaterialCategory, List<MaterialConversionInfo>> categorizedMaterials = new Dictionary<MaterialCategory, List<MaterialConversionInfo>>();
    private bool analyzed = false;

    // FlatKit 쉐이더 GUID
    private const string FLATKIT_SHADER_GUID = "bee44b4a58655ee4cbff107302a3e131";
    private const string FLATKIT_OUTLINE_SHADER_GUID = "f7e38193b7f064d7380403618fd8b69e";
    private Shader flatKitShader;
    private Shader flatKitOutlineShader;

    // 카테고리별 프리셋 (기본값)
    private Dictionary<MaterialCategory, CategoryPreset> presets = new Dictionary<MaterialCategory, CategoryPreset>()
    {
        { MaterialCategory.Character, new CategoryPreset(
            "Character",
            0.75f,      // celShadingSize - 강한 셀 쉐이딩
            0.001f,     // shadowEdgeSize - 날카로운 경계
            1.0f,       // flatness - 완전 플랫
            new Color(0.35f, 0.35f, 0.4f, 1f),  // shadedColor
            true,       // enableOutline
            2.5f,       // outlineWidth - 캐릭터는 중간 두께
            Color.black // outlineColor
        )},
        { MaterialCategory.Monster, new CategoryPreset(
            "Monster",
            0.8f,       // celShadingSize - 더 강한 셀 쉐이딩
            0.001f,     // shadowEdgeSize - 날카로운 경계
            1.0f,       // flatness - 완전 플랫
            new Color(0.3f, 0.3f, 0.35f, 1f),   // shadedColor - 더 어두운 그림자
            true,       // enableOutline
            3.0f,       // outlineWidth - 몬스터는 두꺼운 아웃라인
            Color.black // outlineColor
        )},
        { MaterialCategory.Environment, new CategoryPreset(
            "Environment",
            0.6f,       // celShadingSize - 부드러운 셀 쉐이딩
            0.05f,      // shadowEdgeSize - 약간 부드러운 경계
            0.7f,       // flatness - 약간의 그라데이션
            new Color(0.5f, 0.5f, 0.55f, 1f),   // shadedColor - 밝은 그림자
            false,      // enableOutline - 환경은 아웃라인 없음
            1.0f,       // outlineWidth
            Color.black // outlineColor
        )},
        { MaterialCategory.Uncategorized, new CategoryPreset(
            "Uncategorized",
            0.7f,
            0.02f,
            0.8f,
            new Color(0.4f, 0.4f, 0.45f, 1f),
            true,
            2.0f,
            Color.black
        )}
    };

    // 카테고리별 Foldout 상태
    private Dictionary<MaterialCategory, bool> categoryFoldouts = new Dictionary<MaterialCategory, bool>()
    {
        { MaterialCategory.Character, true },
        { MaterialCategory.Monster, true },
        { MaterialCategory.Environment, true },
        { MaterialCategory.Uncategorized, true }
    };

    // 현재 편집 중인 카테고리
    private MaterialCategory currentEditCategory = MaterialCategory.Character;
    private bool showPresetEditor = false;

    // 미리보기 관련
    private MaterialConversionInfo selectedPreviewMaterial;
    private PreviewRenderUtility previewRenderUtility;
    private Material previewMaterialConverted;
    private Mesh previewMesh;
    private Vector2 previewRotation = Vector2.zero;
    private float previewRotationZ = 0f;
    private bool autoRotate = true;
    private float autoRotateSpeed = 30f;
    private float previewZoom = 2f;
    private double lastFrameTime;

    private class MaterialConversionInfo
    {
        public Material material;
        public string path;
        public string currentShader;
        public bool isSelected = true;
        public bool hasTexture;
        public Texture2D mainTexture;
        public Mesh associatedMesh;
        public string meshPath;
        public MaterialCategory category;
        public List<string> usedByPrefabs = new List<string>(); // 어떤 프리팹에서 사용되는지
    }

    [MenuItem("Tools/FlatKit/Batch Converter (Category)")]
    public static void ShowWindow()
    {
        var window = GetWindow<FlatKitBatchConverter>("FlatKit Batch Converter");
        window.minSize = new Vector2(900, 700);
    }

    private void OnEnable()
    {
        // FlatKit 쉐이더 로드
        string shaderPath = AssetDatabase.GUIDToAssetPath(FLATKIT_SHADER_GUID);
        if (!string.IsNullOrEmpty(shaderPath))
        {
            flatKitShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        }
        if (flatKitShader == null)
        {
            flatKitShader = Shader.Find("FlatKit/Stylized Surface");
        }

        string outlineShaderPath = AssetDatabase.GUIDToAssetPath(FLATKIT_OUTLINE_SHADER_GUID);
        if (!string.IsNullOrEmpty(outlineShaderPath))
        {
            flatKitOutlineShader = AssetDatabase.LoadAssetAtPath<Shader>(outlineShaderPath);
        }
        if (flatKitOutlineShader == null)
        {
            flatKitOutlineShader = Shader.Find("FlatKit/Stylized Surface With Outline");
        }

        InitPreviewUtility();

        // 카테고리 딕셔너리 초기화
        foreach (MaterialCategory cat in System.Enum.GetValues(typeof(MaterialCategory)))
        {
            if (!categorizedMaterials.ContainsKey(cat))
            {
                categorizedMaterials[cat] = new List<MaterialConversionInfo>();
            }
        }
    }

    private void OnDisable()
    {
        CleanupPreview();
    }

    private void InitPreviewUtility()
    {
        if (previewRenderUtility == null)
        {
            previewRenderUtility = new PreviewRenderUtility();
            previewRenderUtility.cameraFieldOfView = 30f;
            previewRenderUtility.camera.nearClipPlane = 0.1f;
            previewRenderUtility.camera.farClipPlane = 100f;
            previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            previewRenderUtility.camera.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
            previewRenderUtility.lights[0].intensity = 1.2f;
            previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50, -30, 0);
        }
    }

    private void CleanupPreview()
    {
        if (previewMaterialConverted != null)
        {
            DestroyImmediate(previewMaterialConverted);
            previewMaterialConverted = null;
        }
        if (previewRenderUtility != null)
        {
            previewRenderUtility.Cleanup();
            previewRenderUtility = null;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("FlatKit Batch Converter", EditorStyles.boldLabel);
        GUILayout.Label("카테고리별 프리셋 + 실제 사용 Material만 분석", EditorStyles.miniLabel);
        EditorGUILayout.Space(10);

        if (flatKitShader == null)
        {
            EditorGUILayout.HelpBox("FlatKit Stylized Surface 쉐이더를 찾을 수 없습니다!", MessageType.Error);
            return;
        }

        // 메인 레이아웃
        EditorGUILayout.BeginHorizontal();

        // 좌측 패널 (카테고리별 Material 리스트)
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.55f));
        DrawLeftPanel();
        EditorGUILayout.EndVertical();

        // 우측 패널 (프리셋 설정 + 미리보기)
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.43f));
        DrawRightPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // 자동 회전
        if (autoRotate && selectedPreviewMaterial != null)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - lastFrameTime);
            lastFrameTime = currentTime;
            previewRotation.y += autoRotateSpeed * deltaTime;
            previewRotation.x = Mathf.Sin((float)currentTime * 0.5f) * 15f;
            Repaint();
        }
    }

    private void DrawLeftPanel()
    {
        // 분석 버튼
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("Analyze Used Materials", GUILayout.Height(35)))
        {
            AnalyzeUsedMaterials();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("지정된 폴더의 Prefab에서 사용되는 Material만 분석합니다.\n(Character / Monster / Environment 카테고리별)", MessageType.Info);

        EditorGUILayout.Space(10);

        if (!analyzed)
        {
            EditorGUILayout.HelpBox("'Analyze Used Materials' 버튼을 클릭하여 분석을 시작하세요.", MessageType.Info);
            return;
        }

        // 카테고리별 통계
        int totalCount = categorizedMaterials.Values.Sum(list => list.Count);
        EditorGUILayout.LabelField($"Total Materials: {totalCount}", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // 카테고리별 리스트
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (MaterialCategory category in System.Enum.GetValues(typeof(MaterialCategory)))
        {
            if (!categorizedMaterials.ContainsKey(category)) continue;

            var materials = categorizedMaterials[category];
            if (materials.Count == 0) continue;

            DrawCategorySection(category, materials);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // 변환 버튼
        int selectedCount = categorizedMaterials.Values.Sum(list => list.Count(m => m.isSelected));
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button($"Convert {selectedCount} Materials", GUILayout.Height(40)))
        {
            ConvertSelectedMaterials();
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawCategorySection(MaterialCategory category, List<MaterialConversionInfo> materials)
    {
        // 카테고리 색상
        Color categoryColor = GetCategoryColor(category);
        GUI.backgroundColor = categoryColor;

        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = Color.white;

        // 헤더
        EditorGUILayout.BeginHorizontal();

        categoryFoldouts[category] = EditorGUILayout.Foldout(categoryFoldouts[category],
            $"{category} ({materials.Count})", true, EditorStyles.foldoutHeader);

        // 전체 선택/해제
        if (GUILayout.Button("All", GUILayout.Width(35)))
        {
            foreach (var m in materials) m.isSelected = true;
        }
        if (GUILayout.Button("None", GUILayout.Width(40)))
        {
            foreach (var m in materials) m.isSelected = false;
        }

        // 프리셋 편집 버튼
        if (GUILayout.Button("Edit Preset", GUILayout.Width(75)))
        {
            currentEditCategory = category;
            showPresetEditor = true;
        }

        EditorGUILayout.EndHorizontal();

        if (categoryFoldouts[category])
        {
            EditorGUI.indentLevel++;

            // ToList()로 복사본 생성 - 카테고리 변경 시 Collection modified 오류 방지
            foreach (var info in materials.ToList())
            {
                DrawMaterialItem(info, category);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawMaterialItem(MaterialConversionInfo info, MaterialCategory category)
    {
        bool isPreviewSelected = (selectedPreviewMaterial == info);
        bool isFlatKitApplied = info.currentShader.StartsWith("FlatKit");

        // 이미 FlatKit 적용된 경우 배경색 변경
        if (isFlatKitApplied)
        {
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f, 0.5f); // 연한 녹색
        }

        EditorGUILayout.BeginHorizontal(isPreviewSelected ? "selectionRect" : "box");
        GUI.backgroundColor = Color.white;

        // 선택 체크박스
        info.isSelected = EditorGUILayout.Toggle(info.isSelected, GUILayout.Width(20));

        // 텍스처 미리보기
        if (info.mainTexture != null)
        {
            GUILayout.Box(info.mainTexture, GUILayout.Width(24), GUILayout.Height(24));
        }
        else
        {
            GUILayout.Box("", GUILayout.Width(24), GUILayout.Height(24));
        }

        // Material 정보
        EditorGUILayout.BeginVertical();

        // 이름 + FlatKit 적용 여부 표시
        if (isFlatKitApplied)
        {
            EditorGUILayout.LabelField($"{info.material.name} [FK]", EditorStyles.boldLabel);
        }
        else
        {
            EditorGUILayout.LabelField(info.material.name, EditorStyles.boldLabel);
        }

        // 사용처 표시
        if (info.usedByPrefabs.Count > 0)
        {
            string usedBy = string.Join(", ", info.usedByPrefabs.Take(2));
            if (info.usedByPrefabs.Count > 2) usedBy += $" +{info.usedByPrefabs.Count - 2}";
            EditorGUILayout.LabelField($"Used by: {usedBy}", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();

        // 카테고리 변경 드롭다운
        var newCategory = (MaterialCategory)EditorGUILayout.EnumPopup(info.category, GUILayout.Width(90));
        if (newCategory != info.category)
        {
            ChangeMaterialCategory(info, newCategory);
        }

        // Preview 버튼
        GUI.backgroundColor = isPreviewSelected ? Color.cyan : Color.white;
        if (GUILayout.Button("Preview", GUILayout.Width(55), GUILayout.Height(24)))
        {
            SelectMaterialForPreview(info);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRightPanel()
    {
        // 프리셋 에디터
        if (showPresetEditor)
        {
            DrawPresetEditor();
        }

        EditorGUILayout.Space(10);

        // 미리보기
        DrawPreviewPanel();
    }

    private void DrawPresetEditor()
    {
        EditorGUILayout.LabelField($"Preset: {currentEditCategory}", EditorStyles.boldLabel);

        var preset = presets[currentEditCategory];

        EditorGUI.BeginChangeCheck();

        preset.celShadingSize = EditorGUILayout.Slider("Self Shading Size", preset.celShadingSize, 0f, 1f);
        preset.shadowEdgeSize = EditorGUILayout.Slider("Shadow Edge Size", preset.shadowEdgeSize, 0f, 0.5f);
        preset.flatness = EditorGUILayout.Slider("Flatness", preset.flatness, 0f, 1f);
        preset.shadedColor = EditorGUILayout.ColorField("Shaded Color", preset.shadedColor);

        EditorGUILayout.Space(5);
        preset.enableOutline = EditorGUILayout.Toggle("Enable Outline", preset.enableOutline);
        if (preset.enableOutline)
        {
            preset.outlineWidth = EditorGUILayout.Slider("Outline Width", preset.outlineWidth, 0.1f, 5f);
            preset.outlineColor = EditorGUILayout.ColorField("Outline Color", preset.outlineColor);
        }

        if (EditorGUI.EndChangeCheck())
        {
            // 미리보기 업데이트
            if (selectedPreviewMaterial != null && selectedPreviewMaterial.category == currentEditCategory)
            {
                UpdatePreviewMaterial();
            }
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Close", GUILayout.Height(25)))
        {
            showPresetEditor = false;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    private void DrawPreviewPanel()
    {
        EditorGUILayout.LabelField("3D Preview", EditorStyles.boldLabel);

        if (selectedPreviewMaterial == null)
        {
            EditorGUILayout.HelpBox("Material 리스트에서 'Preview' 버튼을 클릭하세요.", MessageType.Info);
            return;
        }

        // 미리보기 컨트롤
        EditorGUILayout.BeginHorizontal();
        autoRotate = EditorGUILayout.Toggle("Auto Rotate", autoRotate, GUILayout.Width(100));
        if (GUILayout.Button("Reset View", GUILayout.Width(80)))
        {
            previewRotation = Vector2.zero;
            previewRotationZ = 0f;
            previewZoom = 2f;
        }
        EditorGUILayout.EndHorizontal();

        previewZoom = EditorGUILayout.Slider("Zoom", previewZoom, 0.5f, 5f);

        if (!autoRotate)
        {
            EditorGUILayout.HelpBox("Left Drag: Rotate XY | Right Drag: Rotate Z | Scroll: Zoom", MessageType.None);
        }

        EditorGUILayout.Space(5);

        // 비교 라벨
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Original", EditorStyles.centeredGreyMiniLabel, GUILayout.Width((position.width * 0.43f - 20) / 2));
        GUILayout.Label("After Conversion", EditorStyles.centeredGreyMiniLabel, GUILayout.Width((position.width * 0.43f - 20) / 2));
        EditorGUILayout.EndHorizontal();

        // 3D 미리보기
        float previewSize = (position.width * 0.43f - 30) / 2;
        float previewHeight = Mathf.Min(previewSize, 200);

        EditorGUILayout.BeginHorizontal();
        Rect originalRect = GUILayoutUtility.GetRect(previewSize, previewHeight);
        DrawMaterialPreview(originalRect, selectedPreviewMaterial.material, false);

        Rect convertedRect = GUILayoutUtility.GetRect(previewSize, previewHeight);
        DrawMaterialPreview(convertedRect, previewMaterialConverted, true);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Material 정보
        EditorGUILayout.LabelField("Material Info", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Name:", selectedPreviewMaterial.material.name);
        EditorGUILayout.LabelField("Category:", selectedPreviewMaterial.category.ToString());
        EditorGUILayout.LabelField("Shader:", selectedPreviewMaterial.currentShader);
        EditorGUI.indentLevel--;

        // 아웃라인 안내
        var preset = presets[selectedPreviewMaterial.category];
        if (preset.enableOutline)
        {
            EditorGUILayout.HelpBox("※ Outline은 Scene/Game에서만 확인 가능", MessageType.Info);
        }

        // Scene 테스트 버튼
        GUI.backgroundColor = new Color(0.5f, 0.7f, 1f);
        if (GUILayout.Button("Test in Scene View", GUILayout.Height(28)))
        {
            CreateSceneTestObject();
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawMaterialPreview(Rect rect, Material material, bool isConverted)
    {
        if (material == null || previewRenderUtility == null)
        {
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
            return;
        }

        Event e = Event.current;
        if (rect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDrag)
            {
                if (e.button == 0)
                {
                    previewRotation.y -= e.delta.x * 0.5f;
                    previewRotation.x += e.delta.y * 0.5f;
                    previewRotation.x = Mathf.Clamp(previewRotation.x, -89f, 89f);
                    autoRotate = false;
                    e.Use();
                    Repaint();
                }
                else if (e.button == 1)
                {
                    previewRotationZ -= e.delta.x * 0.5f;
                    autoRotate = false;
                    e.Use();
                    Repaint();
                }
            }
            else if (e.type == EventType.ScrollWheel)
            {
                previewZoom = Mathf.Clamp(previewZoom + e.delta.y * 0.1f, 0.5f, 5f);
                e.Use();
                Repaint();
            }
        }

        previewRenderUtility.BeginPreview(rect, GUIStyle.none);

        Mesh meshToRender = GetPreviewMesh();
        if (meshToRender != null)
        {
            Bounds bounds = meshToRender.bounds;
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float distance = maxSize * previewZoom;

            previewRenderUtility.camera.transform.position = new Vector3(0, bounds.center.y, -distance);
            previewRenderUtility.camera.transform.LookAt(bounds.center);

            Quaternion rotation = Quaternion.Euler(previewRotation.x, previewRotation.y, previewRotationZ);
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);

            previewRenderUtility.DrawMesh(meshToRender, matrix, material, 0);
            previewRenderUtility.camera.Render();
        }

        Texture resultTexture = previewRenderUtility.EndPreview();
        GUI.DrawTexture(rect, resultTexture, ScaleMode.StretchToFill, false);
        Handles.DrawSolidRectangleWithOutline(rect, Color.clear, isConverted ? Color.green : Color.gray);
    }

    private Mesh GetPreviewMesh()
    {
        if (selectedPreviewMaterial?.associatedMesh != null)
        {
            return selectedPreviewMaterial.associatedMesh;
        }

        if (previewMesh == null)
        {
            GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            previewMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(tempSphere);
        }
        return previewMesh;
    }

    private void AnalyzeUsedMaterials()
    {
        // 초기화
        foreach (var cat in categorizedMaterials.Keys.ToList())
        {
            categorizedMaterials[cat].Clear();
        }
        selectedPreviewMaterial = null;

        var usedMaterials = new Dictionary<Material, MaterialConversionInfo>();

        EditorUtility.DisplayProgressBar("Analyzing", "Scanning prefab folders...", 0.3f);

        // 지정된 폴더들에서 Material 수집 (카테고리 자동 지정)
        CollectMaterialsFromPrefabs(usedMaterials);

        EditorUtility.DisplayProgressBar("Analyzing", "Filtering materials...", 0.8f);

        // 카테고리별 리스트에 추가
        foreach (var info in usedMaterials.Values)
        {
            // 파티클/이펙트/UI 등 제외 (FlatKit 적용 대상 아님)
            string shaderLower = info.currentShader.ToLower();
            if (shaderLower.Contains("particle") || shaderLower.Contains("vfx") ||
                shaderLower.Contains("effect") || shaderLower.Contains("unlit") ||
                shaderLower.Contains("skybox") || shaderLower.Contains("ui"))
            {
                continue;
            }

            // 카테고리별 리스트에 추가 (이미 FlatKit 적용된 것도 포함)
            categorizedMaterials[info.category].Add(info);
        }

        EditorUtility.ClearProgressBar();
        analyzed = true;

        int total = categorizedMaterials.Values.Sum(list => list.Count);
        Debug.Log($"[FlatKit Converter] Found {total} materials to convert");
        Debug.Log($"  - Character: {categorizedMaterials[MaterialCategory.Character].Count}");
        Debug.Log($"  - Monster: {categorizedMaterials[MaterialCategory.Monster].Count}");
        Debug.Log($"  - Environment: {categorizedMaterials[MaterialCategory.Environment].Count}");
        Debug.Log($"  - Uncategorized: {categorizedMaterials[MaterialCategory.Uncategorized].Count}");
    }

    // 카테고리별 폴더 정의
    private static readonly Dictionary<MaterialCategory, string[]> CategoryFolders = new Dictionary<MaterialCategory, string[]>()
    {
        { MaterialCategory.Character, new string[] { "Assets/Character Prefabs" } },
        { MaterialCategory.Monster, new string[] { "Assets/03. Prefabs/Monster/Prefabs", "Assets/Training_dummy" } },
        { MaterialCategory.Environment, new string[] { "Assets/polyperfect" } }
    };

    private void CollectMaterialsFromPrefabs(Dictionary<Material, MaterialConversionInfo> dict)
    {
        // 카테고리별로 폴더 스캔
        foreach (var categoryEntry in CategoryFolders)
        {
            MaterialCategory category = categoryEntry.Key;
            string[] folders = categoryEntry.Value;

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                // Prefab 검색
                var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                foreach (var guid in prefabGuids)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null) continue;

                    string prefabName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
                    CollectMaterialsFromGameObject(prefab, dict, prefabName, category);
                }

                // Material 직접 검색 (폴더 내 Material 파일들)
                var matGuids = AssetDatabase.FindAssets("t:Material", new[] { folder });
                foreach (var guid in matGuids)
                {
                    string matPath = AssetDatabase.GUIDToAssetPath(guid);
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null) continue;

                    // 이미 등록되어 있으면 스킵
                    if (dict.ContainsKey(mat)) continue;

                    // 파티클/이펙트 쉐이더 제외
                    if (mat.shader != null)
                    {
                        string shaderLower = mat.shader.name.ToLower();
                        if (shaderLower.Contains("particle") || shaderLower.Contains("vfx") ||
                            shaderLower.Contains("effect") || shaderLower.Contains("skybox"))
                        {
                            continue;
                        }
                    }

                    dict[mat] = new MaterialConversionInfo
                    {
                        material = mat,
                        path = matPath,
                        currentShader = mat.shader != null ? mat.shader.name : "None",
                        hasTexture = HasMainTexture(mat),
                        mainTexture = GetMainTexture(mat),
                        category = category  // 폴더 기반으로 카테고리 직접 지정
                    };
                    dict[mat].usedByPrefabs.Add($"[{folder}]");
                }
            }
        }
    }

    private void CollectMaterialsFromGameObject(GameObject obj, Dictionary<Material, MaterialConversionInfo> dict, string sourceName, MaterialCategory category)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null) continue;

                string matPath = AssetDatabase.GetAssetPath(mat);
                if (string.IsNullOrEmpty(matPath)) continue;

                if (!dict.ContainsKey(mat))
                {
                    dict[mat] = new MaterialConversionInfo
                    {
                        material = mat,
                        path = matPath,
                        currentShader = mat.shader != null ? mat.shader.name : "None",
                        hasTexture = HasMainTexture(mat),
                        mainTexture = GetMainTexture(mat),
                        category = category  // 폴더 기반으로 카테고리 직접 지정
                    };
                }

                if (!dict[mat].usedByPrefabs.Contains(sourceName))
                {
                    dict[mat].usedByPrefabs.Add(sourceName);
                }

                // 메시 연결
                if (dict[mat].associatedMesh == null)
                {
                    if (renderer is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                    {
                        dict[mat].associatedMesh = smr.sharedMesh;
                    }
                    else if (renderer is MeshRenderer)
                    {
                        var mf = renderer.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            dict[mat].associatedMesh = mf.sharedMesh;
                        }
                    }
                }
            }
        }
    }

    private void ChangeMaterialCategory(MaterialConversionInfo info, MaterialCategory newCategory)
    {
        // 이전 카테고리에서 제거
        categorizedMaterials[info.category].Remove(info);

        // 새 카테고리로 이동
        info.category = newCategory;
        categorizedMaterials[newCategory].Add(info);

        // 미리보기 업데이트
        if (selectedPreviewMaterial == info)
        {
            UpdatePreviewMaterial();
        }
    }

    private void SelectMaterialForPreview(MaterialConversionInfo info)
    {
        selectedPreviewMaterial = info;
        currentEditCategory = info.category;
        UpdatePreviewMaterial();
    }

    private void UpdatePreviewMaterial()
    {
        if (selectedPreviewMaterial == null || flatKitShader == null) return;

        if (previewMaterialConverted != null)
        {
            DestroyImmediate(previewMaterialConverted);
        }

        previewMaterialConverted = new Material(selectedPreviewMaterial.material);
        previewMaterialConverted.name = selectedPreviewMaterial.material.name + "_Preview";

        ApplyPresetToMaterial(previewMaterialConverted, selectedPreviewMaterial.category);
    }

    private void ApplyPresetToMaterial(Material mat, MaterialCategory category)
    {
        var preset = presets[category];

        // 기존 텍스처 저장
        Texture mainTex = GetMainTextureFromMaterial(mat);
        Texture emissionTex = null;
        Color baseColor = Color.white;
        Color emissionColor = Color.black;

        if (mat.HasProperty("_BaseColor"))
            baseColor = mat.GetColor("_BaseColor");
        else if (mat.HasProperty("_Color"))
            baseColor = mat.GetColor("_Color");

        if (mat.HasProperty("_EmissionMap"))
            emissionTex = mat.GetTexture("_EmissionMap");

        if (mat.HasProperty("_EmissionColor"))
            emissionColor = mat.GetColor("_EmissionColor");

        // 쉐이더 변경
        if (preset.enableOutline && flatKitOutlineShader != null)
        {
            mat.shader = flatKitOutlineShader;
        }
        else
        {
            mat.shader = flatKitShader;
        }

        // 기본 설정
        mat.SetColor("_BaseColor", baseColor);

        if (mainTex != null)
        {
            mat.SetTexture("_BaseMap", mainTex);
            mat.SetFloat("_TextureImpact", 1.0f);
        }

        if (emissionTex != null)
        {
            mat.SetTexture("_EmissionMap", emissionTex);
            mat.SetColor("_EmissionColor", emissionColor);
        }

        // 프리셋 값 적용
        mat.SetFloat("_CelPrimaryMode", 1);
        mat.SetFloat("_SelfShadingSize", preset.celShadingSize);
        mat.SetFloat("_ShadowEdgeSize", preset.shadowEdgeSize);
        mat.SetFloat("_Flatness", preset.flatness);
        mat.SetColor("_ColorDim", preset.shadedColor);

        // Outline 설정
        if (preset.enableOutline)
        {
            mat.SetFloat("_OutlineEnabled", 1);
            mat.EnableKeyword("DR_OUTLINE_ON");
            mat.SetFloat("_OutlineWidth", preset.outlineWidth);
            mat.SetColor("_OutlineColor", preset.outlineColor);
            mat.SetFloat("_OutlineScale", 1.0f);
            mat.SetFloat("_OutlineSpace", 0);
        }
        else
        {
            mat.SetFloat("_OutlineEnabled", 0);
            mat.DisableKeyword("DR_OUTLINE_ON");
        }

        mat.SetFloat("_LightContribution", 0.5f);
        mat.SetFloat("_SpecularEnabled", 0);
        mat.SetFloat("_RimEnabled", 0);
        mat.SetFloat("_GradientEnabled", 0);
    }

    private void ConvertSelectedMaterials()
    {
        var allSelected = categorizedMaterials.Values
            .SelectMany(list => list)
            .Where(m => m.isSelected)
            .ToList();

        if (allSelected.Count == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "변환할 Material을 선택해주세요.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Confirm Conversion",
            $"{allSelected.Count}개의 Material을 카테고리별 프리셋으로 변환합니다.\n\n" +
            "이 작업은 되돌릴 수 없습니다. 계속하시겠습니까?",
            "Convert", "Cancel"))
        {
            return;
        }

        int successCount = 0;
        int failCount = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < allSelected.Count; i++)
            {
                var info = allSelected[i];

                EditorUtility.DisplayProgressBar("Converting Materials",
                    $"[{info.category}] {info.material.name}",
                    (float)i / allSelected.Count);

                try
                {
                    ApplyPresetToMaterial(info.material, info.category);
                    EditorUtility.SetDirty(info.material);
                    successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to convert {info.material.name}: {e.Message}");
                    failCount++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"[FlatKit Converter] Conversion complete: {successCount} success, {failCount} failed");
        EditorUtility.DisplayDialog("Conversion Complete",
            $"변환 완료!\n\n성공: {successCount}\n실패: {failCount}",
            "OK");

        // 변환된 항목 제거
        foreach (var cat in categorizedMaterials.Keys.ToList())
        {
            categorizedMaterials[cat].RemoveAll(m => m.isSelected);
        }
        selectedPreviewMaterial = null;
    }

    private void CreateSceneTestObject()
    {
        if (selectedPreviewMaterial == null)
        {
            EditorUtility.DisplayDialog("No Material", "먼저 Material을 선택해주세요.", "OK");
            return;
        }

        if (previewMaterialConverted == null)
        {
            EditorUtility.DisplayDialog("No Preview", "미리보기 Material이 없습니다.", "OK");
            return;
        }

        CleanupSceneTestObject();

        Mesh meshToUse = selectedPreviewMaterial.associatedMesh;
        GameObject testObj;

        if (meshToUse != null)
        {
            testObj = new GameObject($"[FlatKit Test] {selectedPreviewMaterial.material.name}");
            MeshFilter mf = testObj.AddComponent<MeshFilter>();
            mf.sharedMesh = meshToUse;
            MeshRenderer mr = testObj.AddComponent<MeshRenderer>();
            Material testMaterial = new Material(previewMaterialConverted);
            testMaterial.name = selectedPreviewMaterial.material.name + "_Test";
            mr.sharedMaterial = testMaterial;
        }
        else
        {
            testObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            testObj.name = $"[FlatKit Test] {selectedPreviewMaterial.material.name}";
            Material testMaterial = new Material(previewMaterialConverted);
            testMaterial.name = selectedPreviewMaterial.material.name + "_Test";
            testObj.GetComponent<MeshRenderer>().sharedMaterial = testMaterial;

            var collider = testObj.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            Vector3 spawnPos = sceneView.camera.transform.position + sceneView.camera.transform.forward * 3f;
            testObj.transform.position = spawnPos;
            Selection.activeGameObject = testObj;
            sceneView.FrameSelected();
        }
        else
        {
            testObj.transform.position = Vector3.zero;
        }

        Undo.RegisterCreatedObjectUndo(testObj, "Create FlatKit Test Object");
    }

    private void CleanupSceneTestObject()
    {
        var existingTestObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.StartsWith("[FlatKit Test]"))
            .ToArray();

        foreach (var obj in existingTestObjects)
        {
            DestroyImmediate(obj);
        }
    }

    private Color GetCategoryColor(MaterialCategory category)
    {
        switch (category)
        {
            case MaterialCategory.Character: return new Color(0.6f, 0.8f, 1f);
            case MaterialCategory.Monster: return new Color(1f, 0.7f, 0.7f);
            case MaterialCategory.Environment: return new Color(0.7f, 1f, 0.7f);
            default: return new Color(0.9f, 0.9f, 0.9f);
        }
    }

    private bool HasMainTexture(Material mat)
    {
        return GetMainTexture(mat) != null;
    }

    private Texture2D GetMainTexture(Material mat)
    {
        string[] textureProps = { "_MainTex", "_BaseMap", "_Albedo", "_Diffuse" };
        foreach (var prop in textureProps)
        {
            if (mat.HasProperty(prop))
            {
                var tex = mat.GetTexture(prop) as Texture2D;
                if (tex != null) return tex;
            }
        }
        return null;
    }

    private Texture GetMainTextureFromMaterial(Material mat)
    {
        string[] textureProps = { "_MainTex", "_BaseMap", "_Albedo", "_Diffuse" };
        foreach (var prop in textureProps)
        {
            if (mat.HasProperty(prop))
            {
                var tex = mat.GetTexture(prop);
                if (tex != null) return tex;
            }
        }
        return null;
    }
}
