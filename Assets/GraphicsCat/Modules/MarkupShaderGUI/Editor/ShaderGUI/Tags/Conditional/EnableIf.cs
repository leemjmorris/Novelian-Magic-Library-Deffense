#if UNITY_EDITOR

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class EnableIf : ConditionalTag
    {
        protected override string m_beginTag => "BeginEnableIf";
        protected override string m_EndTag => "EndEnableIf";
    }
}

#endif