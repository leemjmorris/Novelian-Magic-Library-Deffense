#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GraphicsCat
{
    [CustomPropertyDrawer(typeof(AutoLoadAttribute))]
    public class AutoLoadDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AutoLoadAttribute autoLoad = attribute as AutoLoadAttribute;

            EditorGUI.PropertyField(position, property, label);

            if (property.objectReferenceValue == null)
            {
                if (!string.IsNullOrEmpty(autoLoad.assetGUID))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(autoLoad.assetGUID);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        System.Type fieldType = fieldInfo.FieldType;
                        Object loadedAsset = AssetDatabase.LoadAssetAtPath(assetPath, fieldType);
                        if (loadedAsset != null)
                        {
                            property.objectReferenceValue = loadedAsset;
                            Debug.Log($"Auto-loaded asset '{loadedAsset.name}' for field '{property.displayName}' using GUID.");
                        }
                        else
                        {
                            Debug.LogWarning($"AutoLoadDrawer: Failed to load asset with GUID '{autoLoad.assetGUID}'.");
                        }
                    }
                }
            }

            if (property.objectReferenceValue != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(property.objectReferenceValue);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                autoLoad.assetGUID = guid;
            }
        }
    }
}

#endif