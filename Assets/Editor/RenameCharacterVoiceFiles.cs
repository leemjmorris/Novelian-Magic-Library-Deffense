using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class RenameCharacterVoiceFiles : EditorWindow
{
    [MenuItem("Tools/Audio/Rename Character Voice Files")]
    public static void RenameFiles()
    {
        var renameMap = new Dictionary<string, string>
        {
            { "그믐_1", "Greum_1" },
            { "그믐_2", "Greum_2" },
            { "념령_1", "Nyemryeong_1" },
            { "념령_2", "Nyemryeong_2" },
            { "누리_1", "Nuri_1" },
            { "누리_2", "Nuri_2" },
            { "도담_1", "Dodam_1" },
            { "도담_2", "Dodam_2" },
            { "라티오_1", "Ratio_1" },
            { "라티오_2", "Ratio_2" },
            { "루도_1", "Rudo_1" },
            { "루도_2", "Rudo_2" },
            { "루멘_1", "Rumen_1" },
            { "루멘_2", "Rumen_2" },
            { "리브라_1", "Libra_1" },
            { "리브라_2", "Libra_2" },
            { "미르_1", "Mir_1" },
            { "미르_2", "Mir_2" },
            { "베리타_1", "Verita_1" },
            { "베리타_2", "Verita_2" },
            { "세린_1", "Serin_1" },
            { "세린_2", "Serin_2" },
            { "위스퍼_1", "Whisper_1" },
            { "위스퍼_2", "Whisper_2" },
            { "이오_1", "Io_1" },
            { "이오_2", "Io_2" },
            { "이테르_1", "Iter_1" },
            { "이테르_2", "Iter_2" },
            { "코르_1", "Cor_1" },
            { "코르_2", "Cor_2" },
            { "키_1", "Ki_1" },
            { "키_2", "Ki_2" },
            { "테네브라_1", "Tenebra_1" },
            { "테네브라_2", "Tenebra_2" },
            { "토비_1", "Tobi_1" },
            { "토비_2", "Tobi_2" },
            { "트레일_1", "Trail_1" },
            { "트레일_2", "Trail_2" },
            { "펀치_1", "Punch_1" },
            { "펀치_2", "Punch_2" }
        };

        string folderPath = "Assets/Audio/0. 캐릭터대사";
        int successCount = 0;
        int failCount = 0;

        foreach (var pair in renameMap)
        {
            string oldPath = $"{folderPath}/{pair.Key}.wav";
            string newPath = $"{folderPath}/{pair.Value}.wav";

            if (File.Exists(oldPath))
            {
                string result = AssetDatabase.RenameAsset(oldPath, pair.Value);
                if (string.IsNullOrEmpty(result))
                {
                    Debug.Log($"<color=green>[SUCCESS]</color> {pair.Key}.wav -> {pair.Value}.wav");
                    successCount++;
                }
                else
                {
                    Debug.LogError($"[FAIL] {pair.Key}.wav: {result}");
                    failCount++;
                }
            }
            else
            {
                Debug.LogWarning($"[NOT FOUND] {oldPath}");
                failCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"<color=cyan>[COMPLETE]</color> Renamed {successCount} files. Failed: {failCount}");
    }
}
