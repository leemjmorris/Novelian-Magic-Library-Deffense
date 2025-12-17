using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NovelianMagicLibraryDefense.Editor
{
    /// <summary>
    /// LMJ: Utility to rename UI Canvas elements to more descriptive names
    /// Usage: Right-click on UI Canvas → Rename UI Elements
    /// </summary>
    public static class UICanvasRenamer
    {
        [MenuItem("Tools/Rename UI Canvas Elements")]
        private static void RenameUIElements()
        {
            // Find UI Canvas
            GameObject uiCanvas = GameObject.Find("UI Canvas");
            if (uiCanvas == null)
            {
                Debug.LogError("[UICanvasRenamer] UI Canvas not found!");
                return;
            }

            Debug.Log("[UICanvasRenamer] Starting UI element renaming...");

            // Define rename mappings
            var renameMappings = new Dictionary<string, string>()
            {
                // InGameHUD - TopPanel
                {"TopPanel", "TopHUDPanel"},
                {"InfoPanel", "GameInfoRow"},
                {"MobRemainCountPanel", "MonsterCountDisplay"},
                {"MobRemainCountIMG", "MonsterCountIcon"},
                {"MobCountTxt", "MonsterCountText"},
                {"TimePanel", "WaveTimerDisplay"},
                {"WaveTimeIMG", "WaveTimerIcon"},
                {"WaveTimeTxt", "WaveTimerText"},
                {"PreferencePanel", "SettingsButtonContainer"},
                {"PreferenceBtnArea", "SettingsButtonArea"},
                {"PreferencesBtn", "SettingsButton"},
                {"PreferenceBtnIMG", "SettingsButtonIcon"},
                {"PreferenceBtnTxt", "SettingsButtonText"},

                // InGameHUD - SpeedControl
                {"SpeedControlArea", "GameSpeedRow"},
                {"SpeedControlBtn", "GameSpeedButton"},
                {"SpeedControlBG", "GameSpeedBackground"},
                {"SpeedcontrolOutline", "GameSpeedOutline"},
                {"SpeedBtnTxt", "GameSpeedText"},

                // InGameHUD - CenterPanel
                {"CenterPanel", "CenterHUDPanel"},
                {"MapPanel", "WallHealthContainer"},
                {"HpSlider", "WallHealthSlider"},

                // StartCardPanel
                {"StartCardPanel", "CharacterSelectionPanel"},
                {"Start 20s Text", "SelectionTimerText"},

                // LevelUpPanel
                {"LevelUpPanel", "LevelUpCardPanel"},
                {"20s Time", "LevelUpTimerText"},

                // WinAndLosePanel
                {"WinAndLosePanel", "GameResultPanel"},
                {"WinPanel", "VictoryPanel"},
                {"LosePanel", "DefeatPanel"},
                {"Buttion", "ActionButtons"},
                {"Rsult", "ResultsContainer"},
                {"Lank", "RankDisplay"},
                {"Mainmenu", "MainMenuButton"},
                {"MainMenu", "MainMenuButton"},
                {"NextStage", "NextStageButton"},

                // PreferencesParent
                {"PreferencesParent", "SettingsPanel"},
                {"Preferences", "SettingsTitle"},
                {"FullSound", "MasterVolumeLabel"},
                {"FullSoundSlider", "MasterVolumeSlider"},
                {"BGMSlider", "BGMVolumeSlider"},
                {"SoundEffectSlider", "SFXVolumeSlider"},
                {"Continue", "ContinueButton"},
            };

            // Rename elements recursively
            int renamedCount = RenameRecursive(uiCanvas.transform, renameMappings);

            // Remove unnecessary nesting - CenterArea
            RemoveUnnecessaryNesting(uiCanvas.transform, "CenterHUDPanel", "CenterArea");

            Debug.Log($"[UICanvasRenamer] Successfully renamed {renamedCount} UI elements!");

            // Mark scene as dirty to save changes
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );
        }

        private static int RenameRecursive(Transform parent, Dictionary<string, string> mappings)
        {
            int count = 0;

            foreach (Transform child in parent)
            {
                // Check if this object should be renamed
                if (mappings.ContainsKey(child.name))
                {
                    string oldName = child.name;
                    string newName = mappings[oldName];
                    child.name = newName;
                    Debug.Log($"[UICanvasRenamer] Renamed: '{oldName}' → '{newName}'");
                    count++;
                }

                // Recursively rename children
                count += RenameRecursive(child, mappings);
            }

            return count;
        }

        private static void RemoveUnnecessaryNesting(Transform root, string parentName, string childToRemove)
        {
            Transform parent = FindChildRecursive(root, parentName);
            if (parent == null)
            {
                Debug.LogWarning($"[UICanvasRenamer] Parent '{parentName}' not found for nesting removal");
                return;
            }

            Transform unnecessaryChild = parent.Find(childToRemove);
            if (unnecessaryChild == null)
            {
                Debug.LogWarning($"[UICanvasRenamer] Child '{childToRemove}' not found under '{parentName}'");
                return;
            }

            // Move all grandchildren to parent
            List<Transform> grandchildren = new List<Transform>();
            foreach (Transform grandchild in unnecessaryChild)
            {
                grandchildren.Add(grandchild);
            }

            foreach (Transform grandchild in grandchildren)
            {
                grandchild.SetParent(parent, false);
            }

            // Delete the unnecessary middle layer
            Object.DestroyImmediate(unnecessaryChild.gameObject);
            Debug.Log($"[UICanvasRenamer] Removed unnecessary nesting: '{childToRemove}' under '{parentName}'");
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
