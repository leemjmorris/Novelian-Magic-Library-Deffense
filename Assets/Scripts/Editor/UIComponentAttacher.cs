using UnityEngine;
using UnityEditor;
using NovelianMagicLibraryDefense.UI;

namespace NovelianMagicLibraryDefense.Editor
{
    /// <summary>
    /// LMJ: Attaches new UI components to renamed UI Canvas elements
    /// Usage: Window → UI Tools → Attach UI Components
    /// </summary>
    public class UIComponentAttacher : EditorWindow
    {
        [MenuItem("Window/UI Tools/Attach UI Components")]
        public static void ShowWindow()
        {
            GetWindow<UIComponentAttacher>("UI Component Attacher");
        }

        private void OnGUI()
        {
            GUILayout.Label("UI Component Attacher", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("This will attach new UI script components");
            GUILayout.Label("to the renamed UI Canvas elements.");
            GUILayout.Label("Old components will be removed.");
            GUILayout.Space(10);

            if (GUILayout.Button("Attach Components", GUILayout.Height(30)))
            {
                AttachComponents();
            }
        }

        private void AttachComponents()
        {
            GameObject uiCanvas = GameObject.Find("UI Canvas");
            if (uiCanvas == null)
            {
                EditorUtility.DisplayDialog("Error", "UI Canvas not found in the scene!", "OK");
                Debug.LogError("[UIComponentAttacher] UI Canvas not found!");
                return;
            }

            Debug.Log("[UIComponentAttacher] Starting component attachment...");

            // 1. Attach GameHUD to InGameHUD
            AttachGameHUD(uiCanvas.transform);

            // 2. Attach GameSpeedController to GameSpeedButton
            AttachGameSpeedController(uiCanvas.transform);

            // 3. Attach SettingsPanel component
            AttachSettingsPanel(uiCanvas.transform);

            // 4. Attach CharacterSelectPanel component
            AttachCharacterSelectPanel(uiCanvas.transform);

            // 5. Attach LevelUpCardPanel component
            AttachLevelUpCardPanel(uiCanvas.transform);

            Debug.Log("[UIComponentAttacher] Component attachment completed!");

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );

            EditorUtility.DisplayDialog("Success",
                "UI components attached successfully!\nCheck the Console for details.",
                "OK");
        }

        private void AttachGameHUD(Transform root)
        {
            Transform inGameHUD = FindChildRecursive(root, "InGameHUD");
            if (inGameHUD == null)
            {
                Debug.LogWarning("[UIComponentAttacher] InGameHUD not found!");
                return;
            }

            // Remove old components
            var oldComponents = inGameHUD.GetComponents<MonoBehaviour>();
            foreach (var comp in oldComponents)
            {
                if (comp != null && comp.GetType().Name != "SafeArea")
                {
                    DestroyImmediate(comp);
                }
            }

            // Add GameHUD component
            var gameHUD = inGameHUD.gameObject.AddComponent<GameHUD>();
            Debug.Log("[UIComponentAttacher] Added GameHUD to InGameHUD");

            // Try to auto-assign references
            Transform monsterCountText = FindChildRecursive(inGameHUD, "MonsterCountText");
            Transform waveTimerText = FindChildRecursive(inGameHUD, "WaveTimerText");
            Transform wallHealthSlider = FindChildRecursive(inGameHUD, "WallHealthSlider");
            Transform expSlider = FindChildRecursive(inGameHUD, "ExperienceSlider");

            // Note: Cannot auto-assign SerializeField references via script
            // User will need to manually assign these in the Inspector
            Debug.Log("[UIComponentAttacher] GameHUD added. Please manually assign references in Inspector:");
            if (monsterCountText) Debug.Log("  - remainingMonstersText → MonsterCountText");
            if (waveTimerText) Debug.Log("  - waveTimerDisplay → WaveTimerText");
            if (wallHealthSlider) Debug.Log("  - wallHealthSlider → WallHealthSlider");
            if (expSlider) Debug.Log("  - experienceSlider → ExperienceSlider");
        }

        private void AttachGameSpeedController(Transform root)
        {
            Transform speedButton = FindChildRecursive(root, "GameSpeedButton");
            if (speedButton == null)
            {
                Debug.LogWarning("[UIComponentAttacher] GameSpeedButton not found!");
                return;
            }

            // Remove old components (except Button)
            var oldComponents = speedButton.GetComponents<MonoBehaviour>();
            foreach (var comp in oldComponents)
            {
                if (comp != null && comp.GetType().Name != "Button")
                {
                    DestroyImmediate(comp);
                }
            }

            // Add GameSpeedController
            var speedController = speedButton.gameObject.AddComponent<GameSpeedController>();
            Debug.Log("[UIComponentAttacher] Added GameSpeedController to GameSpeedButton");

            Transform speedText = FindChildRecursive(speedButton, "GameSpeedText");
            Debug.Log("[UIComponentAttacher] GameSpeedController added. Please manually assign:");
            Debug.Log("  - speedButton → GameSpeedButton's Button component");
            if (speedText) Debug.Log("  - speedText → GameSpeedText");
        }

        private void AttachSettingsPanel(Transform root)
        {
            Transform settingsPanel = FindChildRecursive(root, "SettingsPanel");
            if (settingsPanel == null)
            {
                Debug.LogWarning("[UIComponentAttacher] SettingsPanel not found!");
                return;
            }

            // Remove old components
            var oldComponents = settingsPanel.GetComponents<MonoBehaviour>();
            foreach (var comp in oldComponents)
            {
                if (comp != null && comp.GetType().Name != "RectTransform")
                {
                    DestroyImmediate(comp);
                }
            }

            // Add SettingsPanel component
            var panel = settingsPanel.gameObject.AddComponent<SettingsPanel>();
            Debug.Log("[UIComponentAttacher] Added SettingsPanel component");

            Transform settingsButton = FindChildRecursive(root, "SettingsButton");
            Transform continueButton = FindChildRecursive(settingsPanel, "ContinueButton");

            Debug.Log("[UIComponentAttacher] SettingsPanel added. Please manually assign:");
            Debug.Log("  - panel → SettingsPanel itself");
            if (settingsButton) Debug.Log("  - openButton → SettingsButton");
            if (continueButton) Debug.Log("  - closeButton → ContinueButton");
        }

        private void AttachCharacterSelectPanel(Transform root)
        {
            Transform charPanel = FindChildRecursive(root, "CharacterSelectionPanel");
            if (charPanel == null)
            {
                Debug.LogWarning("[UIComponentAttacher] CharacterSelectionPanel not found!");
                return;
            }

            // Remove old components
            var oldComponents = charPanel.GetComponents<MonoBehaviour>();
            foreach (var comp in oldComponents)
            {
                if (comp != null && comp.GetType().Name != "RectTransform")
                {
                    DestroyImmediate(comp);
                }
            }

            // Add CharacterSelectPanel component
            var panel = charPanel.gameObject.AddComponent<CharacterSelectPanel>();
            Debug.Log("[UIComponentAttacher] Added CharacterSelectPanel component");

            Transform cardContainer = FindChildRecursive(charPanel, "SkillCard");

            Debug.Log("[UIComponentAttacher] CharacterSelectPanel added. Please manually assign:");
            Debug.Log("  - panel → CharacterSelectionPanel itself");
            if (cardContainer) Debug.Log("  - cardContainer → SkillCard");
            Debug.Log("  - characterCardPrefab → Create a card prefab first");
        }

        private void AttachLevelUpCardPanel(Transform root)
        {
            Transform levelUpPanel = FindChildRecursive(root, "LevelUpCardPanel");
            if (levelUpPanel == null)
            {
                Debug.LogWarning("[UIComponentAttacher] LevelUpCardPanel not found!");
                return;
            }

            // Remove old components
            var oldComponents = levelUpPanel.GetComponents<MonoBehaviour>();
            foreach (var comp in oldComponents)
            {
                if (comp != null && comp.GetType().Name != "RectTransform")
                {
                    DestroyImmediate(comp);
                }
            }

            // Add LevelUpCardPanel component
            var panel = levelUpPanel.gameObject.AddComponent<LevelUpCardPanel>();
            Debug.Log("[UIComponentAttacher] Added LevelUpCardPanel component");

            Transform cardContainer = FindChildRecursive(levelUpPanel, "SkillCard");

            Debug.Log("[UIComponentAttacher] LevelUpCardPanel added. Please manually assign:");
            Debug.Log("  - panel → LevelUpCardPanel itself");
            if (cardContainer) Debug.Log("  - cardContainer → SkillCard");
            Debug.Log("  - levelUpCardPrefab → Create a card prefab first");
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
    }
}
