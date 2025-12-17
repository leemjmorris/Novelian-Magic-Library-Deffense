#if UNITY_EDITOR

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class ShowIf : ConditionalTag
    {
        protected override string m_beginTag => "BeginShowIf";
        protected override string m_EndTag => "EndShowIf";
    }
}

#endif