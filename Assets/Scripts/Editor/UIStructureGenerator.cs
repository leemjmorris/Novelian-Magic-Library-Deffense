using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace NovelianMagicLibraryDefense.Editor
{
    /// <summary>
    /// LMJ: Editor tool to generate UI structure with auto-layout components
    /// Usage: Right-click on Canvas → Generate UI Structure
    /// </summary>
    public static class UIStructureGenerator
    {
        [MenuItem("GameObject/Generate UI Structure", false, 0)]
        private static void GenerateUIStructure()
        {
            // Find Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[UIStructureGenerator] Canvas not found! Create a Canvas first.");
                return;
            }

            Transform canvasTransform = canvas.transform;

            // Clear existing children (optional - comment out if you want to keep existing UI)
            // ClearChildren(canvasTransform);

            // Create UI structure
            CreateHUD(canvasTransform);
            CreatePanels(canvasTransform);

            Debug.Log("[UIStructureGenerator] UI structure generated successfully!");
        }

        /// <summary>
        /// Create HUD (Top and Middle areas with game info)
        /// Structure:
        /// - TopPanel (상단 바)
        ///   - UpperRow: MonsterCount, WaveTimer, Spacer, SettingsButton
        ///   - LowerRow: SpeedButton (aligned with SettingsButton)
        /// - MidPanel (중간 바, 가장 넓음)
        ///   - WallHealthArea (left side)
        /// </summary>
        private static void CreateHUD(Transform parent)
        {
            // Main HUD Container
            GameObject hud = CreateUIObject("HUD", parent);
            RectTransform hudRect = hud.GetComponent<RectTransform>();
            SetAnchor(hudRect, AnchorPreset.StretchAll);
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            // === TOP PANEL ===
            GameObject topPanel = CreateUIObject("TopPanel", hud.transform);
            RectTransform topPanelRect = topPanel.GetComponent<RectTransform>();
            SetAnchor(topPanelRect, AnchorPreset.TopStretch);
            topPanelRect.sizeDelta = new Vector2(0, 120); // Height: 120 (two rows)

            VerticalLayoutGroup topPanelLayout = topPanel.AddComponent<VerticalLayoutGroup>();
            topPanelLayout.childForceExpandWidth = true;
            topPanelLayout.childForceExpandHeight = false;
            topPanelLayout.childControlHeight = true;
            topPanelLayout.padding = new RectOffset(20, 20, 20, 10);
            topPanelLayout.spacing = 10;

            // Upper Row (위 구역)
            GameObject upperRow = CreateUIObject("UpperRow", topPanel.transform);
            RectTransform upperRowRect = upperRow.GetComponent<RectTransform>();
            upperRowRect.sizeDelta = new Vector2(0, 50);

            HorizontalLayoutGroup upperRowLayout = upperRow.AddComponent<HorizontalLayoutGroup>();
            upperRowLayout.childForceExpandWidth = false;
            upperRowLayout.childForceExpandHeight = true;
            upperRowLayout.spacing = 20;
            upperRowLayout.childAlignment = TextAnchor.MiddleLeft;

            // Upper Row children
            CreateLayoutElement(CreateUIObject("MonsterCountArea", upperRow.transform), minWidth: 200, preferredHeight: 50);
            CreateLayoutElement(CreateUIObject("WaveTimerArea", upperRow.transform), minWidth: 200, preferredHeight: 50);
            GameObject spacer = CreateUIObject("Spacer", upperRow.transform);
            CreateLayoutElement(spacer, flexibleWidth: 1); // Flexible spacer
            CreateLayoutElement(CreateUIObject("SettingsButtonArea", upperRow.transform), minWidth: 100, preferredHeight: 50);

            // Lower Row (아래 구역) - SpeedButton only
            GameObject lowerRow = CreateUIObject("LowerRow", topPanel.transform);
            RectTransform lowerRowRect = lowerRow.GetComponent<RectTransform>();
            lowerRowRect.sizeDelta = new Vector2(0, 50);

            HorizontalLayoutGroup lowerRowLayout = lowerRow.AddComponent<HorizontalLayoutGroup>();
            lowerRowLayout.childForceExpandWidth = false;
            lowerRowLayout.childForceExpandHeight = true;
            lowerRowLayout.spacing = 20;
            lowerRowLayout.childAlignment = TextAnchor.MiddleRight; // Right align
            lowerRowLayout.padding = new RectOffset(0, 0, 0, 0);

            // Spacer to push SpeedButton to the right (same position as SettingsButton)
            GameObject lowerSpacer = CreateUIObject("Spacer", lowerRow.transform);
            CreateLayoutElement(lowerSpacer, flexibleWidth: 1);

            // SpeedButton (same size as SettingsButton, aligned below it)
            GameObject speedButtonArea = CreateUIObject("SpeedButtonArea", lowerRow.transform);
            CreateLayoutElement(speedButtonArea, minWidth: 100, preferredHeight: 50);
            speedButtonArea.AddComponent<NovelianMagicLibraryDefense.UI.GameSpeedController>();

            // === MID PANEL (가장 넓은 구역) ===
            GameObject midPanel = CreateUIObject("MidPanel", hud.transform);
            RectTransform midPanelRect = midPanel.GetComponent<RectTransform>();
            SetAnchor(midPanelRect, AnchorPreset.MiddleLeft);
            midPanelRect.anchoredPosition = new Vector2(20, 0); // Left side with padding
            midPanelRect.sizeDelta = new Vector2(400, 60); // Wide enough for health bar

            // WallHealthArea (left aligned)
            GameObject wallHealthArea = CreateUIObject("WallHealthArea", midPanel.transform);
            RectTransform wallHealthRect = wallHealthArea.GetComponent<RectTransform>();
            SetAnchor(wallHealthRect, AnchorPreset.StretchAll);
            wallHealthRect.offsetMin = Vector2.zero;
            wallHealthRect.offsetMax = Vector2.zero;

            // Add GameHUD component to main HUD
            hud.AddComponent<NovelianMagicLibraryDefense.UI.GameHUD>();
        }

        /// <summary>
        /// Create Panels (Settings, Character Select, Level Up)
        /// </summary>
        private static void CreatePanels(Transform parent)
        {
            // Settings Panel (Center, full screen overlay)
            GameObject settingsPanel = CreateUIObject("SettingsPanel", parent);
            RectTransform settingsRect = settingsPanel.GetComponent<RectTransform>();
            SetAnchor(settingsRect, AnchorPreset.StretchAll);
            settingsRect.offsetMin = Vector2.zero;
            settingsRect.offsetMax = Vector2.zero;
            settingsPanel.SetActive(false); // Hidden by default

            // Settings Panel Content
            GameObject settingsContent = CreateUIObject("SettingsContent", settingsPanel.transform);
            RectTransform settingsContentRect = settingsContent.GetComponent<RectTransform>();
            SetAnchor(settingsContentRect, AnchorPreset.MiddleCenter);
            settingsContentRect.sizeDelta = new Vector2(600, 800);

            VerticalLayoutGroup settingsLayout = settingsContent.AddComponent<VerticalLayoutGroup>();
            settingsLayout.childForceExpandWidth = true;
            settingsLayout.childForceExpandHeight = false;
            settingsLayout.spacing = 20;
            settingsLayout.padding = new RectOffset(40, 40, 40, 40);

            // Settings children (placeholders)
            CreateLayoutElement(CreateUIObject("TitleArea", settingsContent.transform), minHeight: 80);
            CreateLayoutElement(CreateUIObject("SettingsOptionsArea", settingsContent.transform), flexibleHeight: 1);
            CreateLayoutElement(CreateUIObject("ButtonsArea", settingsContent.transform), minHeight: 100);

            // Add SettingsPanel component
            settingsPanel.AddComponent<NovelianMagicLibraryDefense.UI.SettingsPanel>();

            // Character Select Panel
            GameObject charSelectPanel = CreateUIObject("CharacterSelectPanel", parent);
            RectTransform charSelectRect = charSelectPanel.GetComponent<RectTransform>();
            SetAnchor(charSelectRect, AnchorPreset.StretchAll);
            charSelectRect.offsetMin = Vector2.zero;
            charSelectRect.offsetMax = Vector2.zero;
            charSelectPanel.SetActive(false);

            // Character Select Content
            GameObject charSelectContent = CreateUIObject("CharacterSelectContent", charSelectPanel.transform);
            RectTransform charSelectContentRect = charSelectContent.GetComponent<RectTransform>();
            SetAnchor(charSelectContentRect, AnchorPreset.MiddleCenter);
            charSelectContentRect.sizeDelta = new Vector2(1200, 800);

            VerticalLayoutGroup charSelectLayout = charSelectContent.AddComponent<VerticalLayoutGroup>();
            charSelectLayout.childForceExpandWidth = true;
            charSelectLayout.childForceExpandHeight = false;
            charSelectLayout.spacing = 30;
            charSelectLayout.padding = new RectOffset(50, 50, 50, 50);

            // Character Select children
            CreateLayoutElement(CreateUIObject("TitleArea", charSelectContent.transform), minHeight: 100);

            GameObject cardContainer = CreateUIObject("CardContainer", charSelectContent.transform);
            CreateLayoutElement(cardContainer, flexibleHeight: 1);
            HorizontalLayoutGroup cardLayout = cardContainer.AddComponent<HorizontalLayoutGroup>();
            cardLayout.spacing = 30;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = true;

            CreateLayoutElement(CreateUIObject("ButtonsArea", charSelectContent.transform), minHeight: 100);

            // Add CharacterSelectPanel component
            charSelectPanel.AddComponent<NovelianMagicLibraryDefense.UI.CharacterSelectPanel>();

            // Level Up Card Panel
            GameObject levelUpPanel = CreateUIObject("LevelUpCardPanel", parent);
            RectTransform levelUpRect = levelUpPanel.GetComponent<RectTransform>();
            SetAnchor(levelUpRect, AnchorPreset.StretchAll);
            levelUpRect.offsetMin = Vector2.zero;
            levelUpRect.offsetMax = Vector2.zero;
            levelUpPanel.SetActive(false);

            // Level Up Content
            GameObject levelUpContent = CreateUIObject("LevelUpContent", levelUpPanel.transform);
            RectTransform levelUpContentRect = levelUpContent.GetComponent<RectTransform>();
            SetAnchor(levelUpContentRect, AnchorPreset.MiddleCenter);
            levelUpContentRect.sizeDelta = new Vector2(1000, 600);

            VerticalLayoutGroup levelUpLayout = levelUpContent.AddComponent<VerticalLayoutGroup>();
            levelUpLayout.childForceExpandWidth = true;
            levelUpLayout.childForceExpandHeight = false;
            levelUpLayout.spacing = 20;
            levelUpLayout.padding = new RectOffset(40, 40, 40, 40);

            // Level Up children
            CreateLayoutElement(CreateUIObject("TitleArea", levelUpContent.transform), minHeight: 80);

            GameObject levelUpCardContainer = CreateUIObject("CardContainer", levelUpContent.transform);
            CreateLayoutElement(levelUpCardContainer, flexibleHeight: 1);
            HorizontalLayoutGroup levelUpCardLayout = levelUpCardContainer.AddComponent<HorizontalLayoutGroup>();
            levelUpCardLayout.spacing = 30;
            levelUpCardLayout.childForceExpandWidth = true;
            levelUpCardLayout.childForceExpandHeight = true;

            // Add LevelUpCardPanel component
            levelUpPanel.AddComponent<NovelianMagicLibraryDefense.UI.LevelUpCardPanel>();
        }

        #region Helper Methods

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            return obj;
        }

        private static LayoutElement CreateLayoutElement(GameObject obj, float minWidth = -1, float minHeight = -1,
            float preferredWidth = -1, float preferredHeight = -1, float flexibleWidth = -1, float flexibleHeight = -1)
        {
            LayoutElement element = obj.AddComponent<LayoutElement>();
            if (minWidth >= 0) element.minWidth = minWidth;
            if (minHeight >= 0) element.minHeight = minHeight;
            if (preferredWidth >= 0) element.preferredWidth = preferredWidth;
            if (preferredHeight >= 0) element.preferredHeight = preferredHeight;
            if (flexibleWidth >= 0) element.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0) element.flexibleHeight = flexibleHeight;
            return element;
        }

        private static void SetAnchor(RectTransform rect, AnchorPreset preset)
        {
            switch (preset)
            {
                case AnchorPreset.TopLeft:
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);
                    break;
                case AnchorPreset.TopCenter:
                    rect.anchorMin = new Vector2(0.5f, 1);
                    rect.anchorMax = new Vector2(0.5f, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    break;
                case AnchorPreset.TopRight:
                    rect.anchorMin = new Vector2(1, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(1, 1);
                    break;
                case AnchorPreset.MiddleLeft:
                    rect.anchorMin = new Vector2(0, 0.5f);
                    rect.anchorMax = new Vector2(0, 0.5f);
                    rect.pivot = new Vector2(0, 0.5f);
                    break;
                case AnchorPreset.MiddleCenter:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPreset.MiddleRight:
                    rect.anchorMin = new Vector2(1, 0.5f);
                    rect.anchorMax = new Vector2(1, 0.5f);
                    rect.pivot = new Vector2(1, 0.5f);
                    break;
                case AnchorPreset.BottomLeft:
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0, 0);
                    break;
                case AnchorPreset.BottomCenter:
                    rect.anchorMin = new Vector2(0.5f, 0);
                    rect.anchorMax = new Vector2(0.5f, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    break;
                case AnchorPreset.BottomRight:
                    rect.anchorMin = new Vector2(1, 0);
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(1, 0);
                    break;
                case AnchorPreset.HorizontalStretchTop:
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    break;
                case AnchorPreset.HorizontalStretchMiddle:
                    rect.anchorMin = new Vector2(0, 0.5f);
                    rect.anchorMax = new Vector2(1, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPreset.HorizontalStretchBottom:
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    break;
                case AnchorPreset.VerticalStretchLeft:
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 0.5f);
                    break;
                case AnchorPreset.VerticalStretchCenter:
                    rect.anchorMin = new Vector2(0.5f, 0);
                    rect.anchorMax = new Vector2(0.5f, 1);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPreset.VerticalStretchRight:
                    rect.anchorMin = new Vector2(1, 0);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(1, 0.5f);
                    break;
                case AnchorPreset.StretchAll:
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPreset.TopStretch:
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    break;
            }
        }

        private static void ClearChildren(Transform parent)
        {
            int childCount = parent.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        #endregion

        private enum AnchorPreset
        {
            TopLeft, TopCenter, TopRight,
            MiddleLeft, MiddleCenter, MiddleRight,
            BottomLeft, BottomCenter, BottomRight,
            HorizontalStretchTop, HorizontalStretchMiddle, HorizontalStretchBottom,
            VerticalStretchLeft, VerticalStretchCenter, VerticalStretchRight,
            StretchAll, TopStretch
        }
    }
}
