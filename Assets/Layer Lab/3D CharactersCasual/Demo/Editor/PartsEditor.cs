
using UnityEditor;
using UnityEngine;

namespace Layer_lab._3D_Casual_Character
{
    [CustomEditor(typeof(Parts))]
    public class PartsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var parts = (Parts)target;

            if (GUILayout.Button("SetRandom"))
            {
                parts.AddPartsItem();
            }
        }
    }
}
