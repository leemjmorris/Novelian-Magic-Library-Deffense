#if UNITY_EDITOR

using UnityEditor;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class Separator : Tag
    {
        protected override string m_beginTag => "Separator";

        public override void OnBegin(MarkupShaderGUI.Context context)
        {
            DrawHelper.DrawSeparator();
        }
    }
}

#endif