#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GraphicsCat
{
    [InitializeOnLoad]
    public static class LayerAutoCompleter
    {
        static LayerAutoCompleter()
        {
            EditorApplication.delayCall += () =>
            {
                CompleteEmptyLayers();
            };
        }

        public static void CompleteEmptyLayers()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            bool madeChanges = false;

            // Process all layers (0-31)
            for (int i = 0; i < 32; i++)
            {
                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);

                // Auto-name if empty
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    layerProp.stringValue = "Layer" + i;
                    madeChanges = true;
                    Logger.Log($"Auto-named Layer {i} as 'Layer{i}'");
                }
            }

            if (madeChanges)
            {
                tagManager.ApplyModifiedProperties();
                Logger.Log("All empty layers have been auto-named!");
            }
            else
            {
                // Debug.Log("All layers already have names. No changes made.");
            }
        }
    }
}
#endif