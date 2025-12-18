using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.UI;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Issue #476 - 도전던전 버튼 자동 설정 에디터
/// 메뉴: Tools > Boss Dungeon > Setup Buttons
/// </summary>
public class BossDungeonSetupEditor : EditorWindow
{
    private Transform contentParent;
    private ScrollRect scrollRect;
    private GameObject scrollManagerTarget;

    [MenuItem("Tools/Boss Dungeon/Setup Buttons")]
    public static void ShowWindow()
    {
        GetWindow<BossDungeonSetupEditor>("Boss Dungeon Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("도전던전 버튼 자동 설정", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "1. Content Parent: 버튼들이 있는 Content 오브젝트\n" +
            "2. Scroll Rect: Scroll View 오브젝트\n" +
            "3. Scroll Manager Target: BossDungeonScrollManager를 붙일 오브젝트",
            MessageType.Info);

        GUILayout.Space(10);

        contentParent = (Transform)EditorGUILayout.ObjectField(
            "Content Parent", contentParent, typeof(Transform), true);

        scrollRect = (ScrollRect)EditorGUILayout.ObjectField(
            "Scroll Rect", scrollRect, typeof(ScrollRect), true);

        scrollManagerTarget = (GameObject)EditorGUILayout.ObjectField(
            "Scroll Manager Target", scrollManagerTarget, typeof(GameObject), true);

        GUILayout.Space(20);

        if (GUILayout.Button("1. 버튼에 BossDungeonButton 추가", GUILayout.Height(30)))
        {
            SetupButtons();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("2. BossDungeonScrollManager 설정", GUILayout.Height(30)))
        {
            SetupScrollManager();
        }

        GUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("전체 자동 설정 (1 + 2)", GUILayout.Height(40)))
        {
            SetupButtons();
            SetupScrollManager();
        }
        GUI.backgroundColor = Color.white;
    }

    private void SetupButtons()
    {
        if (contentParent == null)
        {
            EditorUtility.DisplayDialog("오류", "Content Parent를 설정해주세요!", "확인");
            return;
        }

        // Inspector 선택 해제 (Inspector 업데이트 충돌 방지)
        Selection.activeObject = null;

        int setupCount = 0;
        List<Transform> buttonTransforms = new List<Transform>();
        List<StageButton> stageButtonsToRemove = new List<StageButton>();

        // Content 하위의 모든 자식 찾기
        foreach (Transform child in contentParent)
        {
            buttonTransforms.Add(child);
        }

        // 이름 기준 정렬 (n-1, n-2, ... n-10 순서)
        buttonTransforms.Sort((a, b) =>
        {
            int numA = ExtractNumber(a.name);
            int numB = ExtractNumber(b.name);
            return numA.CompareTo(numB);
        });

        // Undo 그룹 시작
        Undo.SetCurrentGroupName("Setup Boss Dungeon Buttons");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (Transform buttonTransform in buttonTransforms)
        {
            // 버튼 컴포넌트 찾기 (자식 포함)
            Button button = buttonTransform.GetComponentInChildren<Button>();
            if (button == null)
            {
                Debug.LogWarning($"[BossDungeonSetup] {buttonTransform.name}에서 Button을 찾을 수 없습니다.");
                continue;
            }

            GameObject buttonObj = button.gameObject;

            // 기존 StageButton에서 레퍼런스 복사할 준비
            StageButton existingStageButton = buttonObj.GetComponent<StageButton>();

            // 복사할 레퍼런스 임시 저장
            Image stageImage = null;
            Object stageNumberText = null;
            GameObject lockOverlay = null;
            GameObject lockIcon = null;
            Image starImage = null;
            Image overlayImage = null;

            if (existingStageButton != null)
            {
                // StageButton에서 SerializedObject로 필드 값 읽기
                SerializedObject soStage = new SerializedObject(existingStageButton);
                stageImage = soStage.FindProperty("stageImage").objectReferenceValue as Image;
                stageNumberText = soStage.FindProperty("stageNumberText").objectReferenceValue;
                lockOverlay = soStage.FindProperty("lockOverlay").objectReferenceValue as GameObject;
                lockIcon = soStage.FindProperty("lockIcon").objectReferenceValue as GameObject;
                starImage = soStage.FindProperty("starImage").objectReferenceValue as Image;
                overlayImage = soStage.FindProperty("overlayImage").objectReferenceValue as Image;

                // 나중에 제거할 리스트에 추가
                stageButtonsToRemove.Add(existingStageButton);
            }

            // BossDungeonButton 추가 (없으면)
            BossDungeonButton dungeonButton = buttonObj.GetComponent<BossDungeonButton>();
            if (dungeonButton == null)
            {
                dungeonButton = Undo.AddComponent<BossDungeonButton>(buttonObj);
            }

            // BossDungeonButton에 값 설정
            int floorIndex = ExtractNumber(buttonTransform.name);
            SerializedObject soDungeon = new SerializedObject(dungeonButton);

            soDungeon.FindProperty("floorIndex").intValue = floorIndex;

            // StageButton에서 복사한 레퍼런스 설정
            if (stageImage != null)
                soDungeon.FindProperty("stageImage").objectReferenceValue = stageImage;
            if (stageNumberText != null)
                soDungeon.FindProperty("stageNumberText").objectReferenceValue = stageNumberText;
            if (lockOverlay != null)
                soDungeon.FindProperty("lockOverlay").objectReferenceValue = lockOverlay;
            if (lockIcon != null)
                soDungeon.FindProperty("lockIcon").objectReferenceValue = lockIcon;
            if (starImage != null)
                soDungeon.FindProperty("starImage").objectReferenceValue = starImage;
            if (overlayImage != null)
                soDungeon.FindProperty("overlayImage").objectReferenceValue = overlayImage;

            // button 필드도 설정
            soDungeon.FindProperty("button").objectReferenceValue = button;

            soDungeon.ApplyModifiedProperties();

            // 기존 OnClick 이벤트 모두 제거
            button.onClick.RemoveAllListeners();

            // 기존 Persistent 리스너 모두 제거
            int listenerCount = button.onClick.GetPersistentEventCount();
            for (int i = listenerCount - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            }

            // 새 Persistent 이벤트 추가
            UnityAction action = dungeonButton.OnDungeonButtonClicked;
            UnityEventTools.AddPersistentListener(button.onClick, action);

            EditorUtility.SetDirty(buttonObj);
            setupCount++;
            Debug.Log($"[BossDungeonSetup] {buttonObj.name}에 BossDungeonButton 설정 완료 (floorIndex={floorIndex})");
        }

        // 모든 설정 완료 후 StageButton 제거
        foreach (StageButton sb in stageButtonsToRemove)
        {
            if (sb != null)
            {
                Undo.DestroyObjectImmediate(sb);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.SetDirty(contentParent.gameObject);
        EditorUtility.DisplayDialog("완료",
            $"{setupCount}개 버튼에 BossDungeonButton 설정 완료!\n" +
            $"StageButton {stageButtonsToRemove.Count}개 제거됨", "확인");
    }

    private void SetupScrollManager()
    {
        if (scrollManagerTarget == null)
        {
            EditorUtility.DisplayDialog("오류", "Scroll Manager Target을 설정해주세요!", "확인");
            return;
        }

        if (contentParent == null)
        {
            EditorUtility.DisplayDialog("오류", "Content Parent를 설정해주세요!", "확인");
            return;
        }

        // BossDungeonScrollManager 추가 (없으면)
        BossDungeonScrollManager scrollManager = scrollManagerTarget.GetComponent<BossDungeonScrollManager>();
        if (scrollManager == null)
        {
            scrollManager = scrollManagerTarget.AddComponent<BossDungeonScrollManager>();
        }

        // SerializedObject로 private 필드 접근
        SerializedObject serializedManager = new SerializedObject(scrollManager);

        // scrollRect 설정
        if (scrollRect != null)
        {
            SerializedProperty scrollRectProp = serializedManager.FindProperty("scrollRect");
            scrollRectProp.objectReferenceValue = scrollRect;
        }

        // dungeonButtons 리스트 설정
        SerializedProperty buttonListProp = serializedManager.FindProperty("dungeonButtons");
        buttonListProp.ClearArray();

        List<BossDungeonButton> buttons = new List<BossDungeonButton>();
        foreach (Transform child in contentParent)
        {
            BossDungeonButton btn = child.GetComponentInChildren<BossDungeonButton>();
            if (btn != null)
            {
                buttons.Add(btn);
            }
        }

        // 이름 기준 정렬
        buttons.Sort((a, b) =>
        {
            int numA = ExtractNumber(a.transform.parent.name);
            int numB = ExtractNumber(b.transform.parent.name);
            return numA.CompareTo(numB);
        });

        // 리스트에 추가
        for (int i = 0; i < buttons.Count; i++)
        {
            buttonListProp.InsertArrayElementAtIndex(i);
            buttonListProp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        }

        serializedManager.ApplyModifiedProperties();
        EditorUtility.SetDirty(scrollManagerTarget);

        EditorUtility.DisplayDialog("완료",
            $"BossDungeonScrollManager 설정 완료!\n" +
            $"- ScrollRect: {(scrollRect != null ? "연결됨" : "미연결")}\n" +
            $"- 버튼 수: {buttons.Count}개",
            "확인");
    }

    /// <summary>
    /// 이름에서 숫자 추출 (n-1GameObject → 1)
    /// </summary>
    private int ExtractNumber(string name)
    {
        string numStr = "";
        bool foundDash = false;

        foreach (char c in name)
        {
            if (c == '-')
            {
                foundDash = true;
                continue;
            }

            if (foundDash && char.IsDigit(c))
            {
                numStr += c;
            }
            else if (foundDash && !char.IsDigit(c) && numStr.Length > 0)
            {
                break;
            }
        }

        if (int.TryParse(numStr, out int result))
        {
            return result;
        }

        return 999; // 숫자 없으면 맨 뒤로
    }
}
