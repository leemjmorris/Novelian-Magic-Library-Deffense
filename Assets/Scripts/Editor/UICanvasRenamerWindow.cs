using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NovelianMagicLibraryDefense.Editor
{
    /// <summary>
    /// LMJ: Editor window to rename UI Canvas elements
    /// Usage: Window → UI Tools → Rename UI Canvas Elements
    /// </summary>
    public class UICanvasRenamerWindow : EditorWindow
    {
        [MenuItem("Window/UI Tools/Rename UI Canvas Elements")]
        public static void ShowWindow()
        {
            GetWindow<UICanvasRenamerWindow>("UI Renamer");
        }

        private void OnGUI()
        {
            GUILayout.Label("UI Canvas Element Renamer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("This will rename all UI Canvas elements to");
            GUILayout.Label("more descriptive and consistent names.");
            GUILayout.Space(10);

            if (GUILayout.Button("Rename UI Elements", GUILayout.Height(30)))
            {
                RenameUIElements();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Show Rename Plan"))
            {
                ShowRenamePlan();
            }
        }

        private void ShowRenamePlan()
        {
            Debug.Log("=== UI Canvas Rename Plan ===");
            Debug.Log("TopPanel → TopHUDPanel");
            Debug.Log("InfoPanel → GameInfoRow");
            Debug.Log("MobRemainCountPanel → MonsterCountDisplay");
            Debug.Log("SpeedControlArea → GameSpeedRow");
            Debug.Log("PreferencesParent → SettingsPanel");
            Debug.Log("StartCardPanel → CharacterSelectionPanel");
            Debug.Log("LevelUpPanel → LevelUpCardPanel");
            Debug.Log("WinAndLosePanel → GameResultPanel");
            Debug.Log("... and many more (see script for full list)");
        }

        private void RenameUIElements()
        {
            // Find UI Canvas
            GameObject uiCanvas = GameObject.Find("UI Canvas");
            if (uiCanvas == null)
            {
                EditorUtility.DisplayDialog("Error", "UI Canvas not found in the scene!", "OK");
                Debug.LogError("[UICanvasRenamer] UI Canvas not found!");
                return;
            }

            Debug.Log("[UICanvasRenamer] Starting UI element renaming...");

            // Define rename mappings
            var renameMappings = new Dictionary<string, string>()
            {
                // Main UI Structure
                {"SafeArea", "InGameHUD"},
                {"UpRail", "TopHUDPanel"},
                {"x2 Rail", "GameSpeedRow"},
                {"Hud", "GameInfoRow"},

                // Monster Count
                {"MonsterCount", "MonsterCountDisplay"},
                {"MonsterCountImage", "MonsterCountIcon"},
                {"MonsterCountText", "MonsterCountText"},

                // Wave Timer
                {"TImeText", "WaveTimerDisplay"},
                {"WaveImage", "WaveTimerIcon"},
                {"WaveTime", "WaveTimerText"},

                // Settings Button
                {"Setting", "SettingsButtonContainer"},
                {"SettingObj", "SettingsButtonArea"},
                {"PreferencesBtn", "SettingsButton"},
                {"Image", "SettingsButtonIcon"},

                // Game Speed
                {"GameObject", "GameSpeedButton"},
                {"Image (1)", "GameSpeedBackground"},
                {"speed", "GameSpeedOutline"},
                {"SpeedText (TMP)", "GameSpeedText"},

                // Center Panel
                {"CenterPanel", "CenterHUDPanel"},
                {"Column", "WallHealthRow"},
                {"MapPanel", "HealthAndExpContainer"},
                {"ExpSlider", "ExperienceSlider"},
                {"WallPanel", "WallHealthContainer"},
                {"HP Slider", "WallHealthSlider"},

                // StartCardPanel
                {"StartCardPanel", "CharacterSelectionPanel"},
                {"Start 20s Text", "SelectionTimerText"},

                // LevelUpPanel
                {"LevelUpPanel", "LevelUpCardPanel"},
                {"20s Time", "LevelUpTimerText"},

                // WinAndLosePanel (GameResultPanel)
                {"WinAndLosePanel", "GameResultPanel"},
                {"WinPanel", "VictoryPanel"},
                {"LosePanel", "DefeatPanel"},
                {"Info", "ResultInfo"},
                {"Win", "VictoryTitle"},
                {"Lose", "DefeatTitle"},
                {"Buttion", "ActionButtons"},
                {"Rsult", "ResultsContainer"},
                {"Lank", "RankDisplay"},
                {"Reward", "RewardDisplay"},
                {"Mainmenu", "MainMenuButton"},
                {"MainMenu", "MainMenuButton"},
                {"NextStage", "NextStageButton"},

                // Preferences
                {"Preferences", "SettingsPanel"},
                {"GameObject (1)", "SettingsButtons"},
                {"FullSound", "MasterVolumeLabel"},
                {"FullSoundSlider", "MasterVolumeSlider"},
                {"BGMSlider", "BGMVolumeSlider"},
                {"SoundEffectSlider", "SFXVolumeSlider"},
                {"Continue", "ContinueButton"},
            };

            // Rename elements recursively
            int renamedCount = RenameRecursive(uiCanvas.transform, renameMappings);

            // Remove unnecessary nesting - Column (중복 계층 제거)
            RemoveUnnecessaryNesting(uiCanvas.transform, "CenterHUDPanel", "WallHealthRow");

            Debug.Log($"[UICanvasRenamer] Successfully renamed {renamedCount} UI elements!");

            // Mark scene as dirty to save changes
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );

            EditorUtility.DisplayDialog("Success",
                $"Successfully renamed {renamedCount} UI elements!\nCheck the Console for details.",
                "OK");
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
