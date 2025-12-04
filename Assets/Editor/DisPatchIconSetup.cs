using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/// <summary>
/// DisPatch 폴더의 파견 보상 아이템 이미지들을 Addressables에 자동 등록
/// Menu: Tools/Addressables/Setup DisPatch Icons
/// </summary>
public class DisPatchIconSetup : EditorWindow
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

    [MenuItem("Tools/Addressables/Setup DisPatch Icons")]
    public static void SetupDisPatchIcons()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[DisPatchIconSetup] Addressable Asset Settings not found!");
            return;
        }

        Debug.Log("[DisPatchIconSetup] DisPatch 아이콘 Addressables 등록 시작...");

        // Sprite 그룹 가져오기 또는 생성
        var spriteGroup = CreateOrGetGroup(settings, "Sprite");

        // DisPatch 폴더의 모든 PNG 파일 찾기
        string[] pngGuids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Image/DisPatch" });

        int addedCount = 0;
        foreach (string guid in pngGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            // 파일명에서 이미지 ID 추출 (예: "09100_None_MagicPaper" -> "09100")
            Match match = Regex.Match(fileName, @"^(\d+)");
            if (!match.Success)
            {
                Debug.LogWarning($"[DisPatchIconSetup] ID를 추출할 수 없음: {fileName}");
                continue;
            }

            string imageId = match.Groups[1].Value;

            // 이미지 ID를 실제 Item_ID로 변환
            string itemId = ImageToItemIdMap.ContainsKey(imageId) ? ImageToItemIdMap[imageId] : imageId;
            string addressKey = $"ItemIcon_{itemId}";

            // Addressables에 등록
            var entry = settings.CreateOrMoveEntry(guid, spriteGroup, false, false);
            if (entry != null)
            {
                entry.address = addressKey;
                addedCount++;
                Debug.Log($"[DisPatchIconSetup] 등록 완료: {addressKey} <- {path}");
            }
        }

        // 저장
        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(settings);

        Debug.Log($"[DisPatchIconSetup] 완료! {addedCount}개 아이콘 등록됨");
        Debug.Log("[DisPatchIconSetup] Sprite 그룹 총 엔트리 수: " + spriteGroup.entries.Count);
    }

    /// <summary>
    /// 그룹 생성 또는 기존 그룹 반환
    /// </summary>
    private static AddressableAssetGroup CreateOrGetGroup(AddressableAssetSettings settings, string groupName)
    {
        var group = settings.FindGroup(groupName);

        if (group == null)
        {
            group = settings.CreateGroup(groupName, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            Debug.Log($"[DisPatchIconSetup] 새 그룹 생성: {groupName}");
        }
        else
        {
            Debug.Log($"[DisPatchIconSetup] 기존 그룹 사용: {groupName}");
        }

        return group;
    }
}
