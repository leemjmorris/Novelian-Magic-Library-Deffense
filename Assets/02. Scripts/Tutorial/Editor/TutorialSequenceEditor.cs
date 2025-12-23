#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Tutorial;

namespace TutorialEditor
{
    /// <summary>
    /// TutorialSequence ScriptableObject 커스텀 에디터
    /// </summary>
    [CustomEditor(typeof(TutorialSequence))]
    public class TutorialSequenceEditor : Editor
    {
        private TutorialSequence sequence;
        private ReorderableList stepsList;
        private int selectedStepIndex = -1;

        private SerializedProperty stepsProperty;
        private SerializedProperty tutorialIdProperty;
        private SerializedProperty tutorialNameProperty;
        private SerializedProperty descriptionProperty;
        private SerializedProperty completionSaveKeyProperty;
        private SerializedProperty canSkipProperty;
        private SerializedProperty nextTutorialIdProperty;

        private bool showStepDetails = true;
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            sequence = (TutorialSequence)target;

            tutorialIdProperty = serializedObject.FindProperty("TutorialId");
            tutorialNameProperty = serializedObject.FindProperty("TutorialName");
            descriptionProperty = serializedObject.FindProperty("Description");
            stepsProperty = serializedObject.FindProperty("Steps");
            completionSaveKeyProperty = serializedObject.FindProperty("CompletionSaveKey");
            canSkipProperty = serializedObject.FindProperty("CanSkip");
            nextTutorialIdProperty = serializedObject.FindProperty("NextTutorialId");

            SetupReorderableList();
        }

        private void SetupReorderableList()
        {
            stepsList = new ReorderableList(serializedObject, stepsProperty, true, true, true, true);

            stepsList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, $"Tutorial Steps ({stepsProperty.arraySize})", EditorStyles.boldLabel);
            };

            stepsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = stepsProperty.GetArrayElementAtIndex(index);
                var stepType = element.FindPropertyRelative("StepType");
                var textId = element.FindPropertyRelative("TextId");

                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                // 스텝 번호
                float numberWidth = 30f;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, numberWidth, rect.height), $"#{index + 1}");

                // 스텝 타입
                float typeWidth = 100f;
                EditorGUI.LabelField(
                    new Rect(rect.x + numberWidth, rect.y, typeWidth, rect.height),
                    ((TutorialStepType)stepType.enumValueIndex).ToString(),
                    EditorStyles.miniLabel
                );

                // 텍스트 ID
                float idWidth = 80f;
                EditorGUI.LabelField(
                    new Rect(rect.x + numberWidth + typeWidth, rect.y, idWidth, rect.height),
                    $"ID: {textId.intValue}",
                    EditorStyles.miniLabel
                );

                // 텍스트 미리보기
                string previewText = GetTextPreview(textId.intValue);
                float previewWidth = rect.width - numberWidth - typeWidth - idWidth - 10f;
                EditorGUI.LabelField(
                    new Rect(rect.x + numberWidth + typeWidth + idWidth, rect.y, previewWidth, rect.height),
                    previewText,
                    EditorStyles.miniLabel
                );
            };

            stepsList.onSelectCallback = (ReorderableList list) =>
            {
                selectedStepIndex = list.index;
            };

            stepsList.onAddCallback = (ReorderableList list) =>
            {
                int index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = index;

                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                // 기본값 설정
                element.FindPropertyRelative("StepType").enumValueIndex = 0;
                element.FindPropertyRelative("TextId").intValue = 0;
                element.FindPropertyRelative("AdvanceType").enumValueIndex = 0;
                element.FindPropertyRelative("DimBackground").boolValue = true;

                selectedStepIndex = index;
            };

            stepsList.elementHeight = EditorGUIUtility.singleLineHeight + 6;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 시퀀스 정보
            EditorGUILayout.LabelField("Tutorial Sequence Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tutorialIdProperty);
            EditorGUILayout.PropertyField(tutorialNameProperty);
            EditorGUILayout.PropertyField(descriptionProperty);

            EditorGUILayout.Space(10);

            // 완료 설정
            EditorGUILayout.LabelField("Completion Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(completionSaveKeyProperty);
            EditorGUILayout.PropertyField(canSkipProperty);
            EditorGUILayout.PropertyField(nextTutorialIdProperty);

            EditorGUILayout.Space(10);

            // 스텝 리스트
            stepsList.DoLayoutList();

            EditorGUILayout.Space(10);

            // 선택된 스텝 상세 정보
            if (selectedStepIndex >= 0 && selectedStepIndex < stepsProperty.arraySize)
            {
                DrawStepDetails(selectedStepIndex);
            }

            EditorGUILayout.EndScrollView();

            // 유틸리티 버튼
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset Completion", GUILayout.Height(25)))
            {
                sequence.ResetCompletion();
                Debug.Log($"[TutorialEditor] Reset completion for: {sequence.TutorialId}");
            }

            if (GUILayout.Button("Validate", GUILayout.Height(25)))
            {
                ValidateSequence();
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStepDetails(int index)
        {
            showStepDetails = EditorGUILayout.Foldout(showStepDetails, $"Step #{index + 1} Details", true);

            if (!showStepDetails)
                return;

            EditorGUI.indentLevel++;

            var element = stepsProperty.GetArrayElementAtIndex(index);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 기본 설정
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(element.FindPropertyRelative("StepType"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("TextId"));

            // 텍스트 미리보기
            int textId = element.FindPropertyRelative("TextId").intValue;
            string preview = GetTextPreview(textId);
            EditorGUILayout.HelpBox($"Text Preview: {preview}", MessageType.None);

            EditorGUILayout.PropertyField(element.FindPropertyRelative("VoiceKey"));

            EditorGUILayout.Space(5);

            // 진행 조건
            EditorGUILayout.LabelField("Advance Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(element.FindPropertyRelative("AdvanceType"));

            var advanceType = (TutorialAdvanceType)element.FindPropertyRelative("AdvanceType").enumValueIndex;

            if (advanceType == TutorialAdvanceType.Auto)
            {
                EditorGUILayout.PropertyField(element.FindPropertyRelative("AutoAdvanceDelay"));
            }
            else if (advanceType == TutorialAdvanceType.WaitForEvent)
            {
                EditorGUILayout.PropertyField(element.FindPropertyRelative("CompleteEventKey"));
            }

            EditorGUILayout.Space(5);

            // 하이라이트 설정
            var stepType = (TutorialStepType)element.FindPropertyRelative("StepType").enumValueIndex;
            if (stepType == TutorialStepType.Highlight || advanceType == TutorialAdvanceType.WaitForTargetClick)
            {
                EditorGUILayout.LabelField("Highlight Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(element.FindPropertyRelative("HighlightTargetPath"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("HighlightTarget"));
            }

            EditorGUILayout.Space(5);

            // 캐릭터 설정
            if (stepType == TutorialStepType.FullDialog)
            {
                EditorGUILayout.LabelField("Character Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(element.FindPropertyRelative("Characters"), true);
                EditorGUILayout.PropertyField(element.FindPropertyRelative("SpeakerIndex"));
            }

            EditorGUILayout.Space(5);

            // 게임 제어
            EditorGUILayout.LabelField("Game Control", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(element.FindPropertyRelative("PauseGame"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("ResumeGameOnComplete"));

            EditorGUILayout.Space(5);

            // 특수 연출
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(element.FindPropertyRelative("DimBackground"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("StartDelay"));

            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
        }

        private string GetTextPreview(int textId)
        {
            if (textId <= 0)
                return "(No text ID)";

            // CSVLoader에서 텍스트 로드 시도
            if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
            {
                var data = CSVLoader.Instance.GetData<TutorialData>(textId);
                if (data != null)
                {
                    string text = data.Text;
                    if (text.Length > 50)
                        text = text.Substring(0, 50) + "...";
                    return text;
                }
            }

            return $"(Text ID: {textId} - Load CSV to preview)";
        }

        private void ValidateSequence()
        {
            List<string> warnings = new List<string>();
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(sequence.TutorialId))
            {
                errors.Add("Tutorial ID is empty");
            }

            if (sequence.Steps.Count == 0)
            {
                warnings.Add("No steps defined");
            }

            for (int i = 0; i < sequence.Steps.Count; i++)
            {
                var step = sequence.Steps[i];

                if (step.TextId <= 0)
                {
                    warnings.Add($"Step #{i + 1}: No text ID defined");
                }

                if (step.StepType == TutorialStepType.Highlight)
                {
                    if (step.HighlightTarget == null && string.IsNullOrEmpty(step.HighlightTargetPath))
                    {
                        warnings.Add($"Step #{i + 1}: Highlight type but no target defined");
                    }
                }

                if (step.AdvanceType == TutorialAdvanceType.WaitForEvent)
                {
                    if (string.IsNullOrEmpty(step.CompleteEventKey))
                    {
                        errors.Add($"Step #{i + 1}: WaitForEvent but no event key defined");
                    }
                }
            }

            // 결과 출력
            if (errors.Count == 0 && warnings.Count == 0)
            {
                Debug.Log($"[TutorialEditor] Validation passed for: {sequence.TutorialId}");
            }
            else
            {
                foreach (var error in errors)
                {
                    Debug.LogError($"[TutorialEditor] {error}");
                }
                foreach (var warning in warnings)
                {
                    Debug.LogWarning($"[TutorialEditor] {warning}");
                }
            }
        }
    }
}
#endif
