using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// DisPatch 폴더 이미지 파일명의 앞 숫자를 Item_ID로 변경
/// Menu: Tools/Rename DisPatch Icons
/// </summary>
public class RenameDispatchIcons : EditorWindow
{
    // 이미지 파일명(09xxx) → 실제 Item_ID 매핑
    private static readonly Dictionary<string, string> ImageToItemIdMap = new Dictionary<string, string>
    {
        { "09100", "10101" }, // 희미종이
        { "09101", "10102" }, // 응축종이
        { "09102", "10103" }, // 비범종이
        { "09103", "10104" }, // 신성종이
        { "09104", "10105" }, // 고대종이
        { "09105", "10106" }, // 잉크
        { "09106", "10207" }, // 로맨스페이지
        { "09107", "10208" }, // 코미디페이지
        { "09108", "10209" }, // 모험페이지
        { "09109", "10210" }, // 공포페이지
        { "09110", "10211" }, // 추리페이지
        { "09111", "10313" }, // 클립
        { "09112", "10114" }, // 룬석
    };

    [MenuItem("Tools/Rename DisPatch Icons")]
    public static void RenameIcons()
    {
        Debug.Log("[RenameDispatchIcons] 파일명 변경 시작...");

        string[] pngGuids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Image/DisPatch" });
        int renamedCount = 0;

        foreach (string guid in pngGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string directory = Path.GetDirectoryName(path);

            // 파일명에서 이미지 ID 추출 (예: "09100_None_MagicPaper" -> "09100")
            Match match = Regex.Match(fileName, @"^(\d+)(.*)");
            if (!match.Success)
            {
                Debug.LogWarning($"[RenameDispatchIcons] ID를 추출할 수 없음: {fileName}");
                continue;
            }

            string imageId = match.Groups[1].Value;
            string restOfName = match.Groups[2].Value;

            // 매핑된 Item_ID가 있으면 변경
            if (ImageToItemIdMap.TryGetValue(imageId, out string itemId))
            {
                string newFileName = itemId + restOfName + extension;
                string newPath = Path.Combine(directory, newFileName).Replace("\\", "/");

                // 이미 같은 이름이면 스킵
                if (path == newPath)
                {
                    Debug.Log($"[RenameDispatchIcons] 이미 변경됨: {fileName}");
                    continue;
                }

                string result = AssetDatabase.RenameAsset(path, itemId + restOfName);
                if (string.IsNullOrEmpty(result))
                {
                    renamedCount++;
                    Debug.Log($"[RenameDispatchIcons] 변경 완료: {fileName} -> {itemId}{restOfName}");
                }
                else
                {
                    Debug.LogError($"[RenameDispatchIcons] 변경 실패: {fileName} - {result}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RenameDispatchIcons] 완료! {renamedCount}개 파일 이름 변경됨");
    }
}
