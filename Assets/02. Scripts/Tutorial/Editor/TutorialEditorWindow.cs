#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.AddressableAssets;
using Tutorial;

namespace TutorialEditor
{
    /// <summary>
    /// 튜토리얼 관리 에디터 윈도우
    /// </summary>
    public class TutorialEditorWindow : EditorWindow
    {
        private List<TutorialSequence> allSequences = new List<TutorialSequence>();
        private Vector2 listScrollPosition;
        private Vector2 detailScrollPosition;
        private TutorialSequence selectedSequence;
        private int selectedTab = 0;
        private string[] tabs = { "Sequences", "Quick Create", "Settings" };

        // Quick Create
        private string newTutorialId = "";
        private string newTutorialName = "";
        private string newDescription = "";

        // Settings
        private TutorialEvents tutorialEvents;

        // CSV 캐시 (에디터용)
        private static Dictionary<int, string> tutorialTextCache = new Dictionary<int, string>();
        private static bool csvLoaded = false;

        // 캐릭터 데이터 캐시
        private static Dictionary<int, CharacterCsvData> characterDataCache = new Dictionary<int, CharacterCsvData>();
        private static Dictionary<int, string> stringTableCache = new Dictionary<int, string>();
        private static bool characterCsvLoaded = false;

        // Voice 미리듣기
        private AudioSource previewAudioSource;

        // 캐릭터 CSV 데이터 구조
        private class CharacterCsvData
        {
            public int CharacterId;
            public int NameId;
            public int PathId;
        }

        [MenuItem("Tools/Tutorial Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<TutorialEditorWindow>("Tutorial Editor");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            RefreshSequenceList();
            FindTutorialEvents();
            LoadTutorialCSV();
            LoadCharacterCSV();
        }

        private void OnDisable()
        {
            // 미리듣기 오디오 정리
            if (previewAudioSource != null)
            {
                DestroyImmediate(previewAudioSource.gameObject);
                previewAudioSource = null;
            }
        }

        private void LoadTutorialCSV()
        {
            if (csvLoaded && tutorialTextCache.Count > 0)
                return;

            tutorialTextCache.Clear();

            string csvPath = "Assets/Data/CSV/Tutorial/TutorialTable.csv";
            if (!File.Exists(csvPath))
            {
                Debug.LogWarning($"[TutorialEditor] CSV 파일을 찾을 수 없습니다: {csvPath}");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(csvPath);

                // 3행 헤더 형식: 1행(한글), 2행(영문), 3행(타입) → 4행부터 데이터
                for (int i = 3; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // CSV 파싱 (쉼표로 분리, 큰따옴표 처리)
                    var values = ParseCSVLine(line);
                    if (values.Count >= 4)
                    {
                        if (int.TryParse(values[0], out int textId))
                        {
                            string text = values[3].Trim('"');
                            tutorialTextCache[textId] = text;
                        }
                    }
                }

                csvLoaded = true;
                Debug.Log($"[TutorialEditor] CSV 로드 완료: {tutorialTextCache.Count}개 텍스트");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TutorialEditor] CSV 로드 실패: {e.Message}");
            }
        }

        private void LoadCharacterCSV()
        {
            if (characterCsvLoaded && characterDataCache.Count > 0)
                return;

            characterDataCache.Clear();
            stringTableCache.Clear();

            // StringTable 로드
            string stringTablePath = "Assets/Data/CSV/StringTable.csv";
            if (File.Exists(stringTablePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(stringTablePath);
                    for (int i = 1; i < lines.Length; i++) // 헤더 스킵
                    {
                        string line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        int commaIndex = line.IndexOf(',');
                        if (commaIndex > 0)
                        {
                            string idStr = line.Substring(0, commaIndex);
                            string text = line.Substring(commaIndex + 1);
                            if (int.TryParse(idStr, out int id))
                            {
                                stringTableCache[id] = text.Trim('"');
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[TutorialEditor] StringTable 로드 실패: {e.Message}");
                }
            }

            // CharacterTable 로드
            string characterTablePath = "Assets/Data/CSV/Character/CharacterTable.csv";
            if (File.Exists(characterTablePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(characterTablePath);
                    for (int i = 1; i < lines.Length; i++) // 헤더 스킵
                    {
                        string line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var values = line.Split(',');
                        if (values.Length >= 3)
                        {
                            if (int.TryParse(values[0], out int charId) &&
                                int.TryParse(values[1], out int nameId) &&
                                int.TryParse(values[2], out int pathId))
                            {
                                characterDataCache[charId] = new CharacterCsvData
                                {
                                    CharacterId = charId,
                                    NameId = nameId,
                                    PathId = pathId
                                };
                            }
                        }
                    }

                    characterCsvLoaded = true;
                    Debug.Log($"[TutorialEditor] 캐릭터 데이터 로드 완료: {characterDataCache.Count}개");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[TutorialEditor] CharacterTable 로드 실패: {e.Message}");
                }
            }
        }

        private string GetCharacterName(int characterId)
        {
            if (characterDataCache.TryGetValue(characterId, out var data))
            {
                if (stringTableCache.TryGetValue(data.NameId, out string name))
                {
                    return name;
                }
            }
            return null;
        }

        private string GetCharacterIllustKey(int characterId)
        {
            if (characterDataCache.TryGetValue(characterId, out var data))
            {
                // Path_ID를 일러스트 키로 사용
                return data.PathId.ToString();
            }
            return null;
        }

        private void AutoFillCharacterData(CharacterDisplayInfo character)
        {
            if (character.CharacterId <= 0)
                return;

            // 이름 자동 채우기
            string name = GetCharacterName(character.CharacterId);
            if (!string.IsNullOrEmpty(name))
            {
                character.CharacterName = name;
            }

            // 일러스트 키 자동 채우기
            string illustKey = GetCharacterIllustKey(character.CharacterId);
            if (!string.IsNullOrEmpty(illustKey))
            {
                character.IllustrationKey = illustKey;
            }

            EditorUtility.SetDirty(selectedSequence);
            Debug.Log($"[TutorialEditor] 캐릭터 데이터 자동 로드: ID={character.CharacterId}, 이름={character.CharacterName}, 일러스트={character.IllustrationKey}");
        }

        private List<string> ParseCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            result.Add(current);
            return result;
        }

        private void FindTutorialEvents()
        {
            string[] guids = AssetDatabase.FindAssets("t:TutorialEvents");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                tutorialEvents = AssetDatabase.LoadAssetAtPath<TutorialEvents>(path);
            }
        }

        private void CreateTutorialEvents()
        {
            string folder = "Assets/ScriptableObjects/Tutorial";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                {
                    AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
                }
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Tutorial");
            }

            string path = $"{folder}/TutorialEvents.asset";

            tutorialEvents = CreateInstance<TutorialEvents>();
            AssetDatabase.CreateAsset(tutorialEvents, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TutorialEditor] TutorialEvents created at {path}");

            // 자동으로 모든 View에 연결
            AssignTutorialEventsToAllViews();

            EditorGUIUtility.PingObject(tutorialEvents);
        }

        private void AssignTutorialEventsToAllViews()
        {
            if (tutorialEvents == null)
            {
                Debug.LogWarning("[TutorialEditor] TutorialEvents가 없습니다.");
                return;
            }

            // TutorialCanvas 프리팹 찾기
            string[] prefabGuids = AssetDatabase.FindAssets("TutorialCanvas t:Prefab");
            if (prefabGuids.Length == 0)
            {
                Debug.LogWarning("[TutorialEditor] TutorialCanvas 프리팹을 찾을 수 없습니다.");
                return;
            }

            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogWarning("[TutorialEditor] TutorialCanvas 프리팹 로드 실패");
                return;
            }

            // 프리팹 편집 모드
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            int assignedCount = 0;

            // FullDialogView
            var fullDialog = prefabRoot.GetComponentInChildren<FullDialogView>(true);
            if (fullDialog != null)
            {
                var so = new SerializedObject(fullDialog);
                var prop = so.FindProperty("tutorialEvents");
                if (prop != null)
                {
                    prop.objectReferenceValue = tutorialEvents;
                    so.ApplyModifiedProperties();
                    assignedCount++;
                }
            }

            // CompactDialogView
            var compactDialog = prefabRoot.GetComponentInChildren<CompactDialogView>(true);
            if (compactDialog != null)
            {
                var so = new SerializedObject(compactDialog);
                var prop = so.FindProperty("tutorialEvents");
                if (prop != null)
                {
                    prop.objectReferenceValue = tutorialEvents;
                    so.ApplyModifiedProperties();
                    assignedCount++;
                }
            }

            // HighlightView
            var highlightView = prefabRoot.GetComponentInChildren<HighlightView>(true);
            if (highlightView != null)
            {
                var so = new SerializedObject(highlightView);
                var prop = so.FindProperty("tutorialEvents");
                if (prop != null)
                {
                    prop.objectReferenceValue = tutorialEvents;
                    so.ApplyModifiedProperties();
                    assignedCount++;
                }
            }

            // 프리팹 저장
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log($"[TutorialEditor] TutorialEvents를 {assignedCount}개의 View에 자동 연결했습니다.");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            // CSV 로드 버튼 (항상 표시)
            if (GUILayout.Button("CSV 로드", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                csvLoaded = false;
                tutorialTextCache.Clear();
                LoadTutorialCSV();
                Debug.Log($"[TutorialEditor] CSV 로드 완료: {tutorialTextCache.Count}개 텍스트");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            switch (selectedTab)
            {
                case 0:
                    DrawSequencesTab();
                    break;
                case 1:
                    DrawQuickCreateTab();
                    break;
                case 2:
                    DrawSettingsTab();
                    break;
            }
        }

        private void DrawSequencesTab()
        {
            EditorGUILayout.BeginHorizontal();

            // 왼쪽: 시퀀스 리스트
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            DrawSequenceList();
            EditorGUILayout.EndVertical();

            // 구분선
            EditorGUILayout.BeginVertical(GUILayout.Width(1));
            GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(1));
            EditorGUILayout.EndVertical();

            // 오른쪽: 상세 정보
            EditorGUILayout.BeginVertical();
            DrawSequenceDetails();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSequenceList()
        {
            EditorGUILayout.LabelField("Tutorial Sequences", EditorStyles.boldLabel);

            if (GUILayout.Button("Refresh", GUILayout.Height(25)))
            {
                RefreshSequenceList();
            }

            EditorGUILayout.Space(5);

            listScrollPosition = EditorGUILayout.BeginScrollView(listScrollPosition);

            foreach (var seq in allSequences)
            {
                if (seq == null) continue;

                bool isSelected = selectedSequence == seq;
                GUI.backgroundColor = isSelected ? Color.cyan : Color.white;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // 완료 상태 표시
                bool isCompleted = seq.IsCompleted();
                GUILayout.Label(isCompleted ? "✓" : "○", GUILayout.Width(20));

                // 시퀀스 이름
                if (GUILayout.Button(string.IsNullOrEmpty(seq.TutorialName) ? seq.TutorialId : seq.TutorialName,
                    EditorStyles.label, GUILayout.ExpandWidth(true)))
                {
                    selectedSequence = seq;
                    Selection.activeObject = seq;
                }

                // 스텝 수
                GUILayout.Label($"({seq.Steps.Count})", GUILayout.Width(30));

                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("+ New Sequence", GUILayout.Height(30)))
            {
                selectedTab = 1; // Quick Create 탭으로 이동
            }
        }

        // Step 편집용 변수
        private int selectedStepIndex = -1;
        private bool isEditingStep = false;

        private void DrawSequenceDetails()
        {
            if (selectedSequence == null)
            {
                EditorGUILayout.HelpBox("Select a tutorial sequence from the list", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Tutorial: {selectedSequence.TutorialName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ID: {selectedSequence.TutorialId}", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition);

            // 기본 정보
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Basic Info", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Steps: {selectedSequence.Steps.Count}");
            EditorGUILayout.LabelField($"Can Skip: {selectedSequence.CanSkip}");
            EditorGUILayout.LabelField($"Completed: {selectedSequence.IsCompleted()}");

            if (!string.IsNullOrEmpty(selectedSequence.Description))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Description:");
                EditorGUILayout.LabelField(selectedSequence.Description, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Steps 편집 섹션
            DrawStepsEditor();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // 버튼
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select in Project", GUILayout.Height(30)))
            {
                Selection.activeObject = selectedSequence;
                EditorGUIUtility.PingObject(selectedSequence);
            }

            if (GUILayout.Button("Open Inspector", GUILayout.Height(30)))
            {
                Selection.activeObject = selectedSequence;
            }

            if (selectedSequence.IsCompleted())
            {
                if (GUILayout.Button("Reset Progress", GUILayout.Height(30)))
                {
                    selectedSequence.ResetCompletion();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStepsEditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 헤더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tutorial Steps", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(20)))
            {
                AddNewStep();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (selectedSequence.Steps.Count == 0)
            {
                EditorGUILayout.HelpBox("No steps. Click + to add a step.", MessageType.Info);
            }
            else
            {
                // Steps 리스트
                for (int i = 0; i < selectedSequence.Steps.Count; i++)
                {
                    DrawStepItem(i);
                }
            }

            EditorGUILayout.EndVertical();

            // 선택된 Step 상세 편집
            if (selectedStepIndex >= 0 && selectedStepIndex < selectedSequence.Steps.Count)
            {
                EditorGUILayout.Space(10);
                DrawStepDetailEditor(selectedStepIndex);
            }
        }

        private void DrawStepItem(int index)
        {
            var step = selectedSequence.Steps[index];
            bool isSelected = selectedStepIndex == index;

            GUI.backgroundColor = isSelected ? new Color(0.3f, 0.6f, 1f) : Color.white;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 순서 변경 버튼
            EditorGUILayout.BeginVertical(GUILayout.Width(20));
            EditorGUI.BeginDisabledGroup(index == 0);
            if (GUILayout.Button("▲", GUILayout.Width(20), GUILayout.Height(15)))
            {
                MoveStep(index, -1);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(index == selectedSequence.Steps.Count - 1);
            if (GUILayout.Button("▼", GUILayout.Width(20), GUILayout.Height(15)))
            {
                MoveStep(index, 1);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            // Step 정보 (클릭으로 선택)
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();

            // 번호
            GUILayout.Label($"#{index + 1}", EditorStyles.boldLabel, GUILayout.Width(30));

            // StepType
            GUILayout.Label(step.StepType.ToString(), GUILayout.Width(80));

            // TextId
            GUILayout.Label($"ID: {step.TextId}", GUILayout.Width(80));

            // 텍스트 미리보기
            string preview = GetTextPreview(step.TextId);
            if (GUILayout.Button(preview, EditorStyles.label, GUILayout.ExpandWidth(true)))
            {
                selectedStepIndex = isSelected ? -1 : index;
            }

            EditorGUILayout.EndHorizontal();

            // AdvanceType 표시
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(35);
            GUILayout.Label($"진행: {step.AdvanceType}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // 삭제 버튼
            if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Delete Step", $"Step #{index + 1}을 삭제하시겠습니까?", "삭제", "취소"))
                {
                    RemoveStep(index);
                }
            }

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = Color.white;
        }

        private void DrawStepDetailEditor(int index)
        {
            var step = selectedSequence.Steps[index];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Step #{index + 1} 상세 편집", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            // 기본 설정
            EditorGUILayout.LabelField("기본 설정", EditorStyles.miniBoldLabel);

            step.StepType = (TutorialStepType)EditorGUILayout.EnumPopup("Step Type", step.StepType);
            step.TextId = EditorGUILayout.IntField("Text ID", step.TextId);

            // 텍스트 미리보기 (전체 텍스트)
            string textPreview = GetTextPreview(step.TextId, false);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("텍스트 미리보기:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(textPreview, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            // Voice Key + 미리듣기 버튼
            EditorGUILayout.BeginHorizontal();
            step.VoiceKey = EditorGUILayout.TextField("Voice Key", step.VoiceKey);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(step.VoiceKey));
            if (GUILayout.Button("▶", GUILayout.Width(25)))
            {
                PlayVoicePreview(step.VoiceKey);
            }
            if (GUILayout.Button("■", GUILayout.Width(25)))
            {
                StopVoicePreview();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 진행 설정
            EditorGUILayout.LabelField("진행 조건", EditorStyles.miniBoldLabel);

            step.AdvanceType = (TutorialAdvanceType)EditorGUILayout.EnumPopup("Advance Type", step.AdvanceType);

            if (step.AdvanceType == TutorialAdvanceType.Auto)
            {
                step.AutoAdvanceDelay = EditorGUILayout.FloatField("Auto Delay (초)", step.AutoAdvanceDelay);
            }
            else if (step.AdvanceType == TutorialAdvanceType.WaitForEvent)
            {
                step.CompleteEventKey = EditorGUILayout.TextField("Event Key", step.CompleteEventKey);
            }
            else if (step.AdvanceType == TutorialAdvanceType.WaitForTargetClick)
            {
                DrawTargetPathSelector(step);
            }

            // Highlight 타입일 때도 Target 선택 UI 표시
            if (step.StepType == TutorialStepType.Highlight)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("하이라이트 대상", EditorStyles.miniBoldLabel);
                DrawTargetPathSelector(step);
            }

            // FullDialog 타입일 때 캐릭터 설정 표시
            if (step.StepType == TutorialStepType.FullDialog)
            {
                EditorGUILayout.Space(10);
                DrawCharacterSettings(step);
            }

            EditorGUILayout.Space(10);

            // 게임 제어
            EditorGUILayout.LabelField("게임 제어", EditorStyles.miniBoldLabel);

            step.PauseGame = EditorGUILayout.Toggle("Pause Game", step.PauseGame);
            step.ResumeGameOnComplete = EditorGUILayout.Toggle("Resume On Complete", step.ResumeGameOnComplete);
            step.DimBackground = EditorGUILayout.Toggle("Dim Background", step.DimBackground);
            step.StartDelay = EditorGUILayout.FloatField("Start Delay", step.StartDelay);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedSequence);
            }

            EditorGUILayout.Space(5);

            // 닫기 버튼
            if (GUILayout.Button("편집 완료", GUILayout.Height(25)))
            {
                selectedStepIndex = -1;
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndVertical();
        }

        private void AddNewStep()
        {
            Undo.RecordObject(selectedSequence, "Add Tutorial Step");

            var newStep = new TutorialStep
            {
                StepType = TutorialStepType.FullDialog,
                AdvanceType = TutorialAdvanceType.OnTouch,
                TextId = 0,
                AutoAdvanceDelay = 2f,
                ResumeGameOnComplete = true
            };

            selectedSequence.Steps.Add(newStep);
            selectedStepIndex = selectedSequence.Steps.Count - 1;

            EditorUtility.SetDirty(selectedSequence);
            AssetDatabase.SaveAssets();
        }

        private void RemoveStep(int index)
        {
            Undo.RecordObject(selectedSequence, "Remove Tutorial Step");

            selectedSequence.Steps.RemoveAt(index);

            if (selectedStepIndex >= selectedSequence.Steps.Count)
            {
                selectedStepIndex = selectedSequence.Steps.Count - 1;
            }

            EditorUtility.SetDirty(selectedSequence);
            AssetDatabase.SaveAssets();
        }

        private void MoveStep(int index, int direction)
        {
            int newIndex = index + direction;
            if (newIndex < 0 || newIndex >= selectedSequence.Steps.Count)
                return;

            Undo.RecordObject(selectedSequence, "Move Tutorial Step");

            var step = selectedSequence.Steps[index];
            selectedSequence.Steps.RemoveAt(index);
            selectedSequence.Steps.Insert(newIndex, step);

            selectedStepIndex = newIndex;

            EditorUtility.SetDirty(selectedSequence);
            AssetDatabase.SaveAssets();
        }

        private void DrawQuickCreateTab()
        {
            EditorGUILayout.LabelField("Create New Tutorial Sequence", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            newTutorialId = EditorGUILayout.TextField("Tutorial ID", newTutorialId);
            newTutorialName = EditorGUILayout.TextField("Tutorial Name", newTutorialName);
            newDescription = EditorGUILayout.TextField("Description", newDescription);

            EditorGUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newTutorialId));

            if (GUILayout.Button("Create", GUILayout.Height(30)))
            {
                CreateNewSequence();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(20);

            EditorGUILayout.HelpBox(
                "Tutorial sequences are saved as ScriptableObjects.\n" +
                "After creation, select the sequence to add steps.",
                MessageType.Info
            );
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("Tutorial System Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Tutorial Events 관리
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Tutorial Events", EditorStyles.miniBoldLabel);

            if (tutorialEvents != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField("Current Events", tutorialEvents, typeof(TutorialEvents), false);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = tutorialEvents;
                    EditorGUIUtility.PingObject(tutorialEvents);
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Auto Assign to All Views", GUILayout.Height(25)))
                {
                    AssignTutorialEventsToAllViews();
                }

                EditorGUILayout.HelpBox("버튼을 누르면 TutorialCanvas 프리팹의 모든 View에 자동 연결됩니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("TutorialEvents가 없습니다. 생성해주세요.", MessageType.Warning);

                if (GUILayout.Button("Create TutorialEvents", GUILayout.Height(30)))
                {
                    CreateTutorialEvents();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // CSV 관리
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("CSV 텍스트 데이터", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"로드된 텍스트: {tutorialTextCache.Count}개", EditorStyles.miniLabel);

            if (GUILayout.Button("CSV 다시 로드", GUILayout.Width(100)))
            {
                csvLoaded = false;
                tutorialTextCache.Clear();
                LoadTutorialCSV();
                Debug.Log($"[TutorialEditor] CSV 다시 로드 완료: {tutorialTextCache.Count}개 텍스트");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("텍스트 미리보기가 안 보이면 CSV 다시 로드를 눌러주세요.", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Debug Options
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Debug Options", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Reset All Tutorial Progress"))
            {
                if (EditorUtility.DisplayDialog("Reset Progress",
                    "Are you sure you want to reset all tutorial progress?",
                    "Yes", "Cancel"))
                {
                    foreach (var seq in allSequences)
                    {
                        seq?.ResetCompletion();
                    }
                    Debug.Log("[TutorialEditor] All tutorial progress reset");
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Statistics
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Statistics", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Total Sequences: {allSequences.Count}");

            int totalSteps = 0;
            int completedCount = 0;
            foreach (var seq in allSequences)
            {
                if (seq == null) continue;
                totalSteps += seq.Steps.Count;
                if (seq.IsCompleted()) completedCount++;
            }

            EditorGUILayout.LabelField($"Total Steps: {totalSteps}");
            EditorGUILayout.LabelField($"Completed: {completedCount}/{allSequences.Count}");
            EditorGUILayout.EndVertical();
        }

        private void RefreshSequenceList()
        {
            allSequences.Clear();

            string[] guids = AssetDatabase.FindAssets("t:TutorialSequence");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sequence = AssetDatabase.LoadAssetAtPath<TutorialSequence>(path);
                if (sequence != null)
                {
                    allSequences.Add(sequence);
                }
            }

            // ID로 정렬
            allSequences.Sort((a, b) => string.Compare(a.TutorialId, b.TutorialId));
        }

        private void CreateNewSequence()
        {
            string folder = "Assets/ScriptableObjects/Tutorials";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                {
                    AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
                }
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Tutorials");
            }

            string path = $"{folder}/{newTutorialId}.asset";

            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog("Error", "A sequence with this ID already exists", "OK");
                return;
            }

            var newSequence = CreateInstance<TutorialSequence>();
            newSequence.TutorialId = newTutorialId;
            newSequence.TutorialName = newTutorialName;
            newSequence.Description = newDescription;
            newSequence.CompletionSaveKey = $"Tutorial_{newTutorialId}_Completed";

            AssetDatabase.CreateAsset(newSequence, path);
            AssetDatabase.SaveAssets();

            // 초기화
            newTutorialId = "";
            newTutorialName = "";
            newDescription = "";

            RefreshSequenceList();
            selectedSequence = newSequence;
            selectedTab = 0;

            Selection.activeObject = newSequence;
        }

        private string GetTextPreview(int textId, bool truncate = true)
        {
            if (textId <= 0)
                return "(텍스트 ID를 입력하세요)";

            // 에디터 CSV 캐시에서 먼저 확인
            if (tutorialTextCache.TryGetValue(textId, out string cachedText))
            {
                if (truncate && cachedText.Length > 50)
                    return cachedText.Substring(0, 50) + "...";
                return cachedText;
            }

            // 런타임 CSVLoader 확인 (플레이 모드)
            if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
            {
                var data = CSVLoader.Instance.GetData<TutorialData>(textId);
                if (data != null)
                {
                    string text = data.Text;
                    if (truncate && text.Length > 50)
                        text = text.Substring(0, 50) + "...";
                    return text;
                }
            }

            return $"(ID {textId}: 텍스트를 찾을 수 없음)";
        }

        private void PlayVoicePreview(string voiceKey)
        {
            if (string.IsNullOrEmpty(voiceKey))
            {
                Debug.LogWarning("[TutorialEditor] Voice Key가 비어있습니다.");
                return;
            }

            // AudioSource 생성
            if (previewAudioSource == null)
            {
                var go = new GameObject("TutorialEditorAudioPreview");
                go.hideFlags = HideFlags.HideAndDontSave;
                previewAudioSource = go.AddComponent<AudioSource>();
            }

            // Addressables로 오디오 로드
            Addressables.LoadAssetAsync<AudioClip>(voiceKey).Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    if (previewAudioSource != null)
                    {
                        previewAudioSource.clip = handle.Result;
                        previewAudioSource.Play();
                        Debug.Log($"[TutorialEditor] 음성 재생: {voiceKey}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[TutorialEditor] 음성 로드 실패: {voiceKey}");
                }
            };
        }

        private void StopVoicePreview()
        {
            if (previewAudioSource != null && previewAudioSource.isPlaying)
            {
                previewAudioSource.Stop();
            }
        }

        /// <summary>
        /// 캐릭터 설정 UI (FullDialog 전용)
        /// </summary>
        private void DrawCharacterSettings(TutorialStep step)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 헤더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("캐릭터 설정", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(18)))
            {
                step.Characters.Add(new CharacterDisplayInfo
                {
                    CharacterId = 0,
                    CharacterName = "",
                    IllustrationKey = "",
                    DisplayState = CharacterDisplayState.Active,
                    Position = step.Characters.Count
                });
                EditorUtility.SetDirty(selectedSequence);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (step.Characters.Count == 0)
            {
                EditorGUILayout.HelpBox("캐릭터가 없습니다. + 버튼으로 추가하세요.", MessageType.Info);
            }
            else
            {
                // 캐릭터 리스트
                for (int i = 0; i < step.Characters.Count; i++)
                {
                    DrawCharacterItem(step, i);
                }

                EditorGUILayout.Space(5);

                // 화자 선택
                string[] speakerOptions = new string[step.Characters.Count];
                for (int i = 0; i < step.Characters.Count; i++)
                {
                    var c = step.Characters[i];
                    speakerOptions[i] = string.IsNullOrEmpty(c.CharacterName)
                        ? $"캐릭터 {i + 1} (ID: {c.CharacterId})"
                        : c.CharacterName;
                }

                step.SpeakerIndex = EditorGUILayout.Popup("화자 (Speaker)", step.SpeakerIndex, speakerOptions);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 개별 캐릭터 항목 UI
        /// </summary>
        private void DrawCharacterItem(TutorialStep step, int index)
        {
            var character = step.Characters[index];
            bool isSpeaker = step.SpeakerIndex == index;

            GUI.backgroundColor = isSpeaker ? new Color(0.5f, 0.8f, 0.5f) : Color.white;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 헤더
            EditorGUILayout.BeginHorizontal();
            string label = isSpeaker ? $"★ 캐릭터 {index + 1} (화자)" : $"캐릭터 {index + 1}";
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();

            // 삭제 버튼
            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(16)))
            {
                step.Characters.RemoveAt(index);
                if (step.SpeakerIndex >= step.Characters.Count)
                    step.SpeakerIndex = Mathf.Max(0, step.Characters.Count - 1);
                EditorUtility.SetDirty(selectedSequence);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = Color.white;
                return;
            }
            EditorGUILayout.EndHorizontal();

            // 캐릭터 ID + 자동 로드 버튼
            EditorGUILayout.BeginHorizontal();
            int newCharId = EditorGUILayout.IntField("캐릭터 ID", character.CharacterId);

            // ID가 변경되었거나 자동 로드 버튼 클릭 시
            if (newCharId != character.CharacterId)
            {
                character.CharacterId = newCharId;
                AutoFillCharacterData(character);
            }

            if (GUILayout.Button("자동", GUILayout.Width(40)))
            {
                AutoFillCharacterData(character);
            }
            EditorGUILayout.EndHorizontal();

            // 이름 (자동 로드 또는 직접 입력)
            EditorGUILayout.BeginHorizontal();
            character.CharacterName = EditorGUILayout.TextField("이름", character.CharacterName);

            // 자동 로드된 이름 미리보기
            string autoName = GetCharacterName(character.CharacterId);
            if (!string.IsNullOrEmpty(autoName) && autoName != character.CharacterName)
            {
                EditorGUILayout.LabelField($"({autoName})", EditorStyles.miniLabel, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();

            // 일러스트 키
            EditorGUILayout.BeginHorizontal();
            character.IllustrationKey = EditorGUILayout.TextField("일러스트 Key", character.IllustrationKey);

            // 자동 로드된 키 미리보기
            string autoKey = GetCharacterIllustKey(character.CharacterId);
            if (!string.IsNullOrEmpty(autoKey) && autoKey != character.IllustrationKey)
            {
                EditorGUILayout.LabelField($"({autoKey})", EditorStyles.miniLabel, GUILayout.Width(60));
            }
            EditorGUILayout.EndHorizontal();

            // 표시 상태
            character.DisplayState = (CharacterDisplayState)EditorGUILayout.EnumPopup("표시 상태", character.DisplayState);

            // 위치
            string[] positionLabels = { "왼쪽", "중앙", "오른쪽" };
            character.Position = EditorGUILayout.Popup("위치", character.Position, positionLabels);

            EditorGUILayout.EndVertical();

            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// 일러스트 선택 팝업
        /// </summary>
        private void ShowIllustrationSelector(CharacterDisplayInfo character)
        {
            // 간단한 팝업으로 키 입력 도움
            string key = EditorInputDialog.Show(
                "일러스트 Addressable Key",
                "캐릭터 일러스트의 Addressables 키를 입력하세요.\n예: Character_001_Illust",
                character.IllustrationKey
            );

            if (!string.IsNullOrEmpty(key))
            {
                character.IllustrationKey = key;
                EditorUtility.SetDirty(selectedSequence);
            }
        }

        /// <summary>
        /// Target Path 선택 UI (경로 입력 + 드래그앤드롭 둘 다 지원)
        /// </summary>
        private void DrawTargetPathSelector(TutorialStep step)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Target 선택", EditorStyles.miniBoldLabel);

            // 방법 1: 경로 직접 입력
            EditorGUILayout.BeginHorizontal();
            step.HighlightTargetPath = EditorGUILayout.TextField("경로 입력", step.HighlightTargetPath);

            // 현재 경로로 오브젝트 찾기 버튼
            if (!string.IsNullOrEmpty(step.HighlightTargetPath))
            {
                if (GUILayout.Button("찾기", GUILayout.Width(40)))
                {
                    var found = GameObject.Find(step.HighlightTargetPath);
                    if (found != null)
                    {
                        Selection.activeObject = found;
                        EditorGUIUtility.PingObject(found);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("오브젝트 없음",
                            $"경로에 해당하는 오브젝트를 찾을 수 없습니다:\n{step.HighlightTargetPath}", "확인");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 방법 2: Hierarchy에서 드래그앤드롭
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("또는 드래그:", GUILayout.Width(70));

            GameObject dragTarget = EditorGUILayout.ObjectField(
                null,
                typeof(GameObject),
                true,
                GUILayout.ExpandWidth(true)
            ) as GameObject;

            // 드래그앤드롭으로 오브젝트를 받으면 경로 자동 설정
            if (dragTarget != null)
            {
                string hierarchyPath = GetHierarchyPath(dragTarget.transform);
                step.HighlightTargetPath = hierarchyPath;

                // RectTransform이 있으면 직접 참조도 설정
                var rectTransform = dragTarget.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    step.HighlightTarget = rectTransform;
                }

                Debug.Log($"[TutorialEditor] Target 경로 자동 설정: {hierarchyPath}");
                EditorUtility.SetDirty(selectedSequence);
            }
            EditorGUILayout.EndHorizontal();

            // 현재 설정 상태 표시
            if (!string.IsNullOrEmpty(step.HighlightTargetPath))
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);
                EditorGUILayout.LabelField($"✓ 설정됨: {step.HighlightTargetPath}", EditorStyles.miniLabel);

                // 경로 초기화 버튼
                if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(15)))
                {
                    step.HighlightTargetPath = "";
                    step.HighlightTarget = null;
                    EditorUtility.SetDirty(selectedSequence);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Transform의 전체 Hierarchy 경로 반환
        /// </summary>
        private string GetHierarchyPath(Transform target)
        {
            if (target == null) return "";

            string path = target.name;
            Transform parent = target.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        [MenuItem("Tools/Tutorial Editor/Reload CSV")]
        public static void ReloadCSV()
        {
            csvLoaded = false;
            tutorialTextCache.Clear();
            Debug.Log("[TutorialEditor] CSV 캐시 초기화됨. 다음 열기 시 다시 로드됩니다.");
        }
    }

    /// <summary>
    /// 간단한 입력 다이얼로그
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        private string inputText = "";
        private string message = "";
        private string result = null;
        private bool confirmed = false;

        public static string Show(string title, string message, string defaultValue = "")
        {
            var window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window.message = message;
            window.inputText = defaultValue ?? "";
            window.minSize = new Vector2(350, 120);
            window.maxSize = new Vector2(350, 120);

            window.ShowModal();

            return window.confirmed ? window.result : null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(10);

            inputText = EditorGUILayout.TextField(inputText);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("취소", GUILayout.Width(80)))
            {
                confirmed = false;
                Close();
            }

            if (GUILayout.Button("확인", GUILayout.Width(80)))
            {
                confirmed = true;
                result = inputText;
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
