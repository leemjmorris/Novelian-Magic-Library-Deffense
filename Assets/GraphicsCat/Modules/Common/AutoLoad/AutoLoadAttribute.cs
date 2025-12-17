using UnityEditor;
using UnityEngine;

namespace GraphicsCat
{
    // Allow this attribute to be used on fields only
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class AutoLoadAttribute : PropertyAttribute
    {
        // Store the asset GUID for reliable loading
        public string assetGUID;

        // Add a constructor that allows a path to be passed in for initial setup.
        // The drawer will convert this path to a GUID.
        public AutoLoadAttribute(string path)
        {
#if UNITY_EDITOR
            this.assetGUID = AssetDatabase.AssetPathToGUID(path);
#endif
        }
    }
}


