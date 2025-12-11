using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


/// <summary>
/// This EditorWindow script shows a popup to remind the user to select a UI Toolkit theme
/// when the UI Builder window is opened. The script uses PlayerPrefs to track whether the
/// popup has been shown so that it only appears once.
/// </summary>
[InitializeOnLoad]
public class PopUpAboutThemes : EditorWindow
{
    const string k_PopUpKey = "PopUpThemesShown";
    const string k_UIBuilderTitle = "UI Builder";
    const string k_AssetXML = "Assets/ReadMe/PopUpAboutThemes/PopUpAboutThemes.uxml";


    static PopUpAboutThemes()
    {
        EditorApplication.update += CheckEditorChanges;
        InitializePopUpKey();
    }

    private static void InitializePopUpKey()
    {
        if (!PlayerPrefs.HasKey(k_PopUpKey))
        {
            PlayerPrefs.SetInt(k_PopUpKey, 0);
            PlayerPrefs.Save();
        }
    }

    static void CheckEditorChanges()
    {
        if (!PlayerPrefs.HasKey(k_PopUpKey))
        {
            if (PlayerPrefs.GetInt(k_PopUpKey) == 0)
            {
                EditorWindow currentWindow = EditorWindow.focusedWindow;

                if (currentWindow != null && currentWindow.titleContent != null && currentWindow.titleContent.text == k_UIBuilderTitle)
                {
                    PlayerPrefs.SetInt(k_PopUpKey, 1);
                    PlayerPrefs.Save();

                    ShowPopUp();
                    EditorApplication.update -= CheckEditorChanges;
                }
            }
        }

    }
    [MenuItem("Read Me/Themes and screen ratios")]
    static void ShowPopUp()
    {
        PopUpAboutThemes wnd = GetWindow<PopUpAboutThemes>();
        wnd.titleContent = new GUIContent("UI Toolkit themes");
        VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_AssetXML);

        if (uiAsset != null)
        {
            VisualElement ui = uiAsset.Instantiate();
            wnd.rootVisualElement.Insert(0, ui);
        }
        else
        {
            Debug.LogWarning("[PopUpAboutThemes] ShowPopUp: VisualTreeAsset not found.");
        }
    }
}
