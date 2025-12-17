#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class HelpBox : DrawerBase
    {
        private MessageType m_MessageType = MessageType.None;

        public HelpBox()
        {
        }

        public HelpBox(string messageType)
        {
            switch (messageType)
            {
                case "Info": m_MessageType = MessageType.Info; break;
                case "Warning": m_MessageType = MessageType.Warning; break;
                case "Error": m_MessageType = MessageType.Error; break;
            }
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            var message = prop.displayName;
            EditorGUILayout.HelpBox(message, m_MessageType);
        }
    }
}

#endif