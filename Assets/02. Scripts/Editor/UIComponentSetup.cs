using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using NovelianMagicLibraryDefense.UI;

namespace NovelianMagicLibraryDefense.Editor
{
    /// <summary>
    /// LMJ: Complete UI component setup with automatic reference assignment
    /// Usage: Window → UI Tools → Setup UI Components (Complete)
    /// </summary>
    public class UIComponentSetup : EditorWindow
    {
        [MenuItem("Window/UI Tools/Setup UI Components (Complete)")]
        public static void ShowWindow()
        {
            GetWindow<UIComponentSetup>("Complete UI Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Complete UI Component Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("This will:");
            GUILayout.Label("1. Remove old UI components");
            GUILayout.Label("2. Attach new UI scripts");
            GUILayout.Label("3. Auto-assign all references");
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Make sure UI elements are renamed first!", MessageType.Info);
            GUILayout.Space(5);

            if (GUILayout.Button("Setup All Components", GUILayout.Height(40)))
            {
                SetupAllComponents();
            }
        }

        private void SetupAllComponents()
        {
            GameObject uiCanvas = GameObject.Find("UI Canvas");
            if (uiCanvas == null)
            {
                EditorUtility.DisplayDialog("Error", "UI Canvas not found!", "OK");
                return;
            }

            Debug.Log("=== [UIComponentSetup] Starting complete UI setup ===");

            try
            {
                // Setup each component
                SetupGameHUD(uiCanvas.transform);
                SetupGameSpeedController(uiCanvas.transform);
                SetupSettingsPanel(uiCanvas.transform);
                SetupCharacterSelectPanel(uiCanvas.transform);
                SetupLevelUpCardPanel(uiCanvas.transform);
                SetupGameResultPanel(uiCanvas.transform);

                // Mark scene dirty
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                );

                Debug.Log("=== [UIComponentSetup] Setup completed successfully! ===");
                EditorUtility.DisplayDialog("Success",
                    "UI components setup completed!\nCheck Console for details.",
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIComponentSetup] Error: {e.Message}");
                EditorUtility.DisplayDialog("Error",
                    $"Setup failed: {e.Message}\nCheck Console for details.",
                    "OK");
            }
        }

        private void SetupGameHUD(Transform root)
        {
            Debug.Log("[UIComponentSetup] Setting up GameHUD...");

            Transform inGameHUD = FindChildRecursive(root, "InGameHUD");
            if (inGameHUD == null)
            {
                Debug.LogWarning("  ✗ InGameHUD not found!");
                return;
            }

            // Remove old components except SafeArea
            RemoveOldComponents(inGameHUD.gameObject, new[] { "SafeArea", "VerticalLayoutGroup" });

            // Add GameHUD if not present
            GameHUD gameHUD = inGameHUD.GetComponent<GameHUD>();
            if (gameHUD == null)
            {
                gameHUD = inGameHUD.gameObject.AddComponent<GameHUD>();
                Debug.Log("  ✓ Added GameHUD component");
            }

            // Auto-assign references using SerializedObject
            SerializedObject so = new SerializedObject(gameHUD);

            // Find and assign references
            AssignTextComponent(so, "remainingMonstersText", FindChildRecursive(inGameHUD, "MonsterCountText"));
            AssignTextComponent(so, "waveTimerDisplay", FindChildRecursive(inGameHUD, "WaveTimerText"));
            AssignSlider(so, "wallHealthSlider", FindChildRecursive(inGameHUD, "WallHealthSlider"));
            AssignTextComponent(so, "wallHealthText", FindChildRecursive(inGameHUD, "WallHealthSlider"));
            AssignSlider(so, "experienceSlider", FindChildRecursive(inGameHUD, "ExperienceSlider"));

            so.ApplyModifiedProperties();
            Debug.Log("  ✓ GameHUD references assigned");
        }

        private void SetupGameSpeedController(Transform root)
        {
            Debug.Log("[UIComponentSetup] Setting up GameSpeedController...");

            Transform speedButton = FindChildRecursive(root, "GameSpeedButton");
            if (speedButton == null)
            {
                Debug.LogWarning("  ✗ GameSpeedButton not found!");
                return;
            }

            // Remove old components except Button
            RemoveOldComponents(speedButton.gameObject, new[] { "Button", "Image" });

            // Add GameSpeedController
            GameSpeedController controller = speedButton.GetComponent<GameSpeedController>();
            if (controller == null)
            {
                controller = speedButton.gameObject.AddComponent<GameSpeedController>();
                Debug.Log("  ✓ Added GameSpeedController component");
            }

            // Auto-assign references
            SerializedObject so = new SerializedObject(controller);

            var button = speedButton.GetComponent<Button>();
            if (button != null)
            {
                so.FindProperty("speedButton").objectReferenceValue = button;
            }

            AssignTextComponent(so, "speedText", FindChildRecursive(speedButton, "GameSpeedText"));

            so.ApplyModifiedProperties();
            Debug.Log("  ✓ GameSpeedController references assigned");
        }

        private void SetupSettingsPanel(Transform root)
        {
            Debug.Log("[UIComponentSetup] Setting up SettingsPanel...");

            Transform settingsPanel = FindChildRecursive(root, "SettingsPanel");
            if (settingsPanel == null)
            {
                Debug.LogWarning("  ✗ SettingsPanel not found!");
                return;
            }

            // Remove old components
            RemoveOldComponents(settingsPanel.gameObject, new[] { "RectTransform" });

            // Add SettingsPanel component
            SettingsPanel panel = settingsPanel.GetComponent<SettingsPanel>();
            if (panel == null)
            {
                panel = settingsPanel.gameObject.AddComponent<SettingsPanel>();
                Debug.Log("  ✓ Added SettingsPanel component");
            }

            // Auto-assign references
            SerializedObject so = new SerializedObject(panel);

            so.FindProperty("panel").objectReferenceValue = settingsPanel.gameObject;

            Transform settingsButton = FindChildRecursive(root, "SettingsButton");
            if (settingsButton != null)
            {
                var btn = settingsButton.GetComponent<Button>();
                if (btn != null)
                {
                    so.FindProperty("openButton").objectReferenceValue = btn;
                }
            }

            Transform continueButton = FindChildRecursive(settingsPanel, "ContinueButton");
            if (continueButton != null)
            {
                var btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    so.FindProperty("closeButton").objectReferenceValue = btn;
                }
            }

            so.ApplyModifiedProperties();
            Debug.Log("  ✓ SettingsPanel references assigned");
        }

        private void SetupCharacterSelectPanel(Transform root)
        {
            Debug.Log("[UIComponentSetup] Setting up CharacterSelectPanel...");

            Transform charPanel = FindChildRecursive(root, "CharacterSelectionPanel");
            if (charPanel == null)
            {
                Debug.LogWarning("  ✗ CharacterSelectionPanel not found!");
                return;
            }

            // Remove old components
            RemoveOldComponents(charPanel.gameObject, new[] { "RectTransform" });

            // Add CharacterSelectPanel
            CharacterSelectPanel panel = charPanel.GetComponent<CharacterSelectPanel>();
            if (panel == null)
            {
                panel = charPanel.gameObject.AddComponent<CharacterSelectPanel>();
                Debug.Log("  ✓ Added CharacterSelectPanel component");
            }

            // Auto-assign references
            SerializedObject so = new SerializedObject(panel);

            so.FindProperty("panel").objectReferenceValue = charPanel.gameObject;

            Transform cardContainer = FindChildRecursive(charPanel, "SkillCard");
            if (cardContainer != null)
            {
                so.FindProperty("cardContainer").objectReferenceValue = cardContainer;
            }

            so.ApplyModifiedProperties();
            Debug.Log("  ✓ CharacterSelectPanel references assigned");
            Debug.Log("  ⚠ Note: characterCardPrefab needs manual assignment");
        }

        private void SetupLevelUpCardPanel(Transform root)
        {
            Debug.Log("[UIComponentSetup] Setting up LevelUpCardPanel...");

            Transform levelUpPanel = FindChildRecursive(root, "LevelUpCardPanel");
            if (levelUpPanel == null)
            {
                Debug.LogWarning("  ✗ LevelUpCardPanel not found!");
                return;
            }

            // Remove old components
            RemoveOldComponents(levelUpPanel.gameObject, new[] { "RectTransform" });

            // Add LevelUpCardPanel
            LevelUpCardPanel panel = levelUpPanel.GetComponent<LevelUpCardPanel>();
            if (panel == null)
            {
                panel = levelUpPanel.gameObject.AddComponent<LevelUpCardPanel>();
                Debug.Log("  ✓ Added LevelUpCardPanel component");
            }

            // Auto-assign references
            SerializedObject so = new SerializedObject(panel);

            so.FindProperty("panel").objectReferenceValue = levelUpPanel.gameObject;

            Transform cardContainer = FindChildRecursive(levelUpPanel, "SkillCard");
            if (cardContainer != null)
            {
                so.FindProperty("cardContainer").objectReferenceValue = cardContainer;
            }

            so.ApplyModifiedProperties();
            Debug.Log("  ✓ LevelUpCardPanel references assigned");
            Debug.Log("  ⚠ Note: levelUpCardPrefab needs manual assignment");
        }

        private void SetupGameResultPanel(Transform root)
        {
            Debug.Log("[UIComponentSetup] Setting up GameResultPanel...");

            Transform gameResultPanel = FindChildRecursive(root, "GameResultPanel");
            if (gameResultPanel == null)
            {
                Debug.LogWarning("  ✗ GameResultPanel not found!");
                return;
            }

            // Remove old components
            RemoveOldComponents(gameResultPanel.gameObject, new[] { "RectTransform" });

            // Add GameResultPanel component
            GameResultPanel panel = gameResultPanel.GetComponent<GameResultPanel>();
            if (panel == null)
            {
                panel = gameResultPanel.gameObject.AddComponent<GameResultPanel>();
                Debug.Log("  ✓ Added GameResultPanel component");
            }

            // Auto-assign references
            SerializedObject so = new SerializedObject(panel);

            // Main panel references
            so.FindProperty("panel").objectReferenceValue = gameResultPanel.gameObject;

            Transform victoryPanel = FindChildRecursive(gameResultPanel, "VictoryPanel");
            if (victoryPanel != null)
            {
                so.FindProperty("victoryPanel").objectReferenceValue = victoryPanel.gameObject;
            }

            Transform defeatPanel = FindChildRecursive(gameResultPanel, "DefeatPanel");
            if (defeatPanel != null)
            {
                so.FindProperty("defeatPanel").objectReferenceValue = defeatPanel.gameObject;
            }

            // Victory text references (will need to find specific text elements)
            // Note: The exact text element names need to be identified from the hierarchy
            Debug.Log("  ⚠ Note: Victory/Defeat text elements need manual assignment");
            Debug.Log("  ⚠ Note: Button references need manual assignment");

            so.ApplyModifiedProperties();
            Debug.Log("  ✓ GameResultPanel references assigned");
        }

        #region Helper Methods

        private void RemoveOldComponents(GameObject obj, string[] keepComponents)
        {
            var components = obj.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;

                string typeName = comp.GetType().Name;
                bool shouldKeep = false;

                foreach (string keep in keepComponents)
                {
                    if (typeName == keep)
                    {
                        shouldKeep = true;
                        break;
                    }
                }

                if (!shouldKeep)
                {
                    DestroyImmediate(comp);
                }
            }
        }

        private void AssignTextComponent(SerializedObject so, string propertyName, Transform target)
        {
            if (target == null) return;

            var tmp = target.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                so.FindProperty(propertyName).objectReferenceValue = tmp;
                Debug.Log($"  ✓ Assigned {propertyName} → {target.name}");
            }
        }

        private void AssignSlider(SerializedObject so, string propertyName, Transform target)
        {
            if (target == null) return;

            var slider = target.GetComponent<Slider>();
            if (slider != null)
            {
                so.FindProperty(propertyName).objectReferenceValue = slider;
                Debug.Log($"  ✓ Assigned {propertyName} → {target.name}");
            }
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

        #endregion
    }
}
