using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NovelianMagicLibraryDefense.Editor
{
    /// <summary>
    /// LMJ: Renames GameResultPanel's child elements to meaningful names
    /// Usage: Window → UI Tools → Rename GameResultPanel Elements
    /// </summary>
    public class GameResultPanelRenamer : EditorWindow
    {
        [MenuItem("Window/UI Tools/Rename GameResultPanel Elements")]
        public static void ShowWindow()
        {
            GetWindow<GameResultPanelRenamer>("GameResultPanel Renamer");
        }

        private void OnGUI()
        {
            GUILayout.Label("GameResultPanel Element Renamer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("This will rename all GameResultPanel");
            GUILayout.Label("child elements to meaningful names.");
            GUILayout.Space(10);

            if (GUILayout.Button("Rename GameResultPanel Elements", GUILayout.Height(30)))
            {
                RenameGameResultPanelElements();
            }
        }

        private void RenameGameResultPanelElements()
        {
            // Find UI Canvas
            GameObject uiCanvas = GameObject.Find("UI Canvas");
            if (uiCanvas == null)
            {
                EditorUtility.DisplayDialog("Error", "UI Canvas not found in the scene!", "OK");
                Debug.LogError("[GameResultPanelRenamer] UI Canvas not found!");
                return;
            }

            Debug.Log("[GameResultPanelRenamer] Starting GameResultPanel element renaming...");

            Transform gameResultPanel = FindChildRecursive(uiCanvas.transform, "GameResultPanel");
            if (gameResultPanel == null)
            {
                EditorUtility.DisplayDialog("Error", "GameResultPanel not found!", "OK");
                Debug.LogError("[GameResultPanelRenamer] GameResultPanel not found!");
                return;
            }

            int renamedCount = 0;

            // === Victory Panel ===
            Transform victoryPanel = FindChildRecursive(gameResultPanel, "VictoryPanel");
            if (victoryPanel != null)
            {
                renamedCount += RenameVictoryPanelElements(victoryPanel);
            }

            // === Defeat Panel ===
            Transform defeatPanel = FindChildRecursive(gameResultPanel, "DefeatPanel");
            if (defeatPanel != null)
            {
                renamedCount += RenameDefeatPanelElements(defeatPanel);
            }

            Debug.Log($"[GameResultPanelRenamer] Successfully renamed {renamedCount} elements!");

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );

            EditorUtility.DisplayDialog("Success",
                $"Successfully renamed {renamedCount} GameResultPanel elements!",
                "OK");
        }

        private int RenameVictoryPanelElements(Transform victoryPanel)
        {
            int count = 0;

            // Victory Title area
            Transform victoryTitle = FindDirectChild(victoryPanel, "ResultInfo/VictoryTitle");
            if (victoryTitle != null)
            {
                // Rename background image
                Transform bg = FindDirectChild(victoryTitle, "GameSpeedBackground");
                if (bg != null)
                {
                    bg.name = "VictoryTitleBackground";
                    count++;
                    Debug.Log($"  ✓ Renamed: VictoryTitle/GameSpeedBackground → VictoryTitleBackground");
                }

                // Rename text
                Transform text = FindDirectChild(victoryTitle, "Text (TMP)");
                if (text != null)
                {
                    text.name = "VictoryTitleText";
                    count++;
                    Debug.Log($"  ✓ Renamed: VictoryTitle/Text (TMP) → VictoryTitleText");
                }
            }

            // Results Container - Rank Display
            Transform rankDisplay = FindDirectChild(victoryPanel, "ResultInfo/ResultsContainer/RankDisplay");
            if (rankDisplay != null)
            {
                Transform rankContainer = FindDirectChild(rankDisplay, "GameSpeedButton");
                if (rankContainer != null)
                {
                    rankContainer.name = "RankInfoContainer";
                    count++;
                    Debug.Log($"  ✓ Renamed: RankDisplay/GameSpeedButton → RankInfoContainer");

                    // Rename rank texts
                    Transform rankText = FindDirectChild(rankContainer, "Text (TMP) (2)");
                    if (rankText != null)
                    {
                        rankText.name = "RankText";
                        count++;
                    }

                    Transform stageText = FindDirectChild(rankContainer, "Text (TMP) (1)");
                    if (stageText != null)
                    {
                        stageText.name = "StageNameText";
                        count++;
                    }

                    Transform timeText = FindDirectChild(rankContainer, "Text (TMP)");
                    if (timeText != null)
                    {
                        timeText.name = "ClearTimeText";
                        count++;
                    }
                }
            }

            // Reward Display
            Transform reward = FindDirectChild(victoryPanel, "ResultInfo/ResultsContainer/RewardDisplay");
            if (reward != null)
            {
                Transform rewardText = FindDirectChild(reward, "Text (TMP)");
                if (rewardText != null)
                {
                    rewardText.name = "RewardText";
                    count++;
                    Debug.Log($"  ✓ Renamed: RewardDisplay/Text (TMP) → RewardText");
                }
            }

            // Action Buttons
            count += RenameActionButtons(victoryPanel);

            return count;
        }

        private int RenameDefeatPanelElements(Transform defeatPanel)
        {
            int count = 0;

            // Defeat Title area
            Transform defeatTitle = FindDirectChild(defeatPanel, "ResultInfo/DefeatTitle");
            if (defeatTitle != null)
            {
                // Rename icon
                Transform icon = FindDirectChild(defeatTitle, "SettingsButtonIcon");
                if (icon != null)
                {
                    icon.name = "DefeatIcon";
                    count++;
                    Debug.Log($"  ✓ Renamed: DefeatTitle/SettingsButtonIcon → DefeatIcon");
                }

                // Rename text
                Transform text = FindDirectChild(defeatTitle, "Text (TMP)");
                if (text != null)
                {
                    text.name = "DefeatTitleText";
                    count++;
                    Debug.Log($"  ✓ Renamed: DefeatTitle/Text (TMP) → DefeatTitleText");
                }
            }

            // Results Container - Rank Display
            Transform rankDisplay = FindDirectChild(defeatPanel, "ResultInfo/ResultsContainer/RankDisplay");
            if (rankDisplay != null)
            {
                Transform rankContainer = FindDirectChild(rankDisplay, "GameSpeedButton");
                if (rankContainer != null)
                {
                    rankContainer.name = "RankInfoContainer";
                    count++;
                    Debug.Log($"  ✓ Renamed: RankDisplay/GameSpeedButton → RankInfoContainer");

                    // Rename defeat texts
                    Transform rankText = FindDirectChild(rankContainer, "RankText ");
                    if (rankText != null)
                    {
                        rankText.name = "RankText";
                        count++;
                    }

                    Transform stageText = FindDirectChild(rankContainer, "StageText");
                    if (stageText == null)
                    {
                        stageText = FindDirectChild(rankContainer, "Text");
                    }
                    if (stageText != null)
                    {
                        stageText.name = "StageNameText";
                        count++;
                    }

                    Transform timeText = FindDirectChild(rankContainer, "Text ");
                    if (timeText != null)
                    {
                        timeText.name = "SurvivalTimeText";
                        count++;
                    }
                }
            }

            // Action Buttons
            count += RenameActionButtons(defeatPanel);

            return count;
        }

        private int RenameActionButtons(Transform panel)
        {
            int count = 0;

            Transform actionButtons = FindDirectChild(panel, "ActionButtons");
            if (actionButtons != null)
            {
                // Main Menu Button text
                Transform mainMenuBtn = FindDirectChild(actionButtons, "MainMenuButton");
                if (mainMenuBtn != null)
                {
                    Transform mainMenuText = FindDirectChild(mainMenuBtn, "Text (TMP)");
                    if (mainMenuText != null)
                    {
                        mainMenuText.name = "MainMenuButtonText";
                        count++;
                    }
                }

                // Next/Retry Button text
                Transform nextBtn = FindDirectChild(actionButtons, "NextStageButton");
                if (nextBtn != null)
                {
                    Transform nextText = FindDirectChild(nextBtn, "Text (TMP)");
                    if (nextText != null)
                    {
                        nextText.name = "NextStageButtonText";
                        count++;
                    }
                }
            }

            return count;
        }

        private Transform FindChildRecursive(Transform parent, string name)
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

        private Transform FindDirectChild(Transform parent, string path)
        {
            if (parent == null) return null;

            string[] parts = path.Split('/');
            Transform current = parent;

            foreach (string part in parts)
            {
                Transform found = null;
                foreach (Transform child in current)
                {
                    if (child.name == part)
                    {
                        found = child;
                        break;
                    }
                }

                if (found == null)
                {
                    return null;
                }

                current = found;
            }

            return current;
        }
    }
}
