#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;

[CustomEditor(typeof(BaseCsvTable<>), true)]
public class CsvTableEditor : Editor
{
    private string csvTextCache = string.Empty;
    private bool showCsvEditor = false;
    private Vector2 scrollPosition;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var table = target.GetType();

        GUILayout.Space(10);
        GUILayout.Label("=== CSV Tools ===", EditorStyles.boldLabel);

        // CSV 편집기 토글
        showCsvEditor = EditorGUILayout.Foldout(showCsvEditor, "CSV 직접 편집", true);

        if (showCsvEditor)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 현재 데이터를 CSV 텍스트로 변환하여 표시
            if (GUILayout.Button("현재 데이터 불러오기"))
            {
                MethodInfo toCSVMethod = table.GetMethod("ToCSV", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (toCSVMethod != null)
                {
                    csvTextCache = (string)toCSVMethod.Invoke(target, null);
                }
            }

            GUILayout.Label("CSV 내용:", EditorStyles.boldLabel);

            // 스크롤 가능한 TextArea
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            csvTextCache = EditorGUILayout.TextArea(csvTextCache, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            GUILayout.Space(5);

            // 편집한 CSV를 데이터에 적용
            if (GUILayout.Button("편집 내용 적용"))
            {
                if (!string.IsNullOrEmpty(csvTextCache))
                {
                    MethodInfo loadMethod = table.GetMethod("LoadFromString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (loadMethod != null)
                    {
                        try
                        {
                            loadMethod.Invoke(target, new object[] { csvTextCache });
                            EditorUtility.SetDirty(target);
                            AssetDatabase.SaveAssets();
                            Debug.Log("CSV 편집 내용이 적용되었습니다.");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"CSV 파싱 실패: {e.InnerException?.Message ?? e.Message}");
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("CSV 불러오기"))
        {
            string path = EditorUtility.OpenFilePanel("CSV 파일 선택", "", "csv");

            if (!string.IsNullOrEmpty(path))
            {
                string csv = File.ReadAllText(path);
                MethodInfo loadMethod = table.GetMethod("LoadFromString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (loadMethod != null)
                {
                    loadMethod.Invoke(target, new object[] { csv });
                }

                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();

                Debug.Log($"CSV 불러오기 완료: {path}");

                // 불러온 내용을 캐시에도 저장
                csvTextCache = csv;
            }
        }

        if (GUILayout.Button("CSV 저장하기"))
        {
            string path = EditorUtility.SaveFilePanel("CSV 저장", "", "Table.csv", "csv");

            if (!string.IsNullOrEmpty(path))
            {
                MethodInfo toCSVMethod = table.GetMethod("ToCSV", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (toCSVMethod != null)
                {
                    string csvText = (string)toCSVMethod.Invoke(target, null);
                    File.WriteAllText(path, csvText);

                    Debug.Log($"CSV 저장 완료: {path}");
                }
            }
        }
    }
}
#endif