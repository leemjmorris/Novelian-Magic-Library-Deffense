
using UnityEditor;
using UnityEngine;

namespace Layer_lab._3D_Casual_Character
{
    [CustomEditor(typeof(CharacterBase))]
    public class CharacterBaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            CharacterBase character = (CharacterBase)target;

            if (GUILayout.Button("SetRandom"))
            {
                character.SetRandom();
            }

            // if (GUILayout.Button("SavePrefab"))
            // {
            //     character.SavePrefab();
            // }
        }
    }
}
