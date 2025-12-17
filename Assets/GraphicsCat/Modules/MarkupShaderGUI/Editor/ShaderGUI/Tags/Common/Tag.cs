#if UNITY_EDITOR

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class Tag
    {
        public bool inScope = false;
        public bool state = false;

        protected virtual string m_beginTag => "BeginTag";
        protected virtual string m_EndTag => "EndTag";

        public void Process(MarkupShaderGUI.Context context)
        {
            if (context.attribute.StartsWith(m_beginTag))
            {
                inScope = true;
                OnBegin(context);
            }

            if (context.attribute.StartsWith(m_EndTag))
            {
                OnEnd();
                inScope = false;
            }
        }

        public virtual void OnBegin(MarkupShaderGUI.Context context) { }
        public virtual void OnEnd() { }
    }
}

#endif