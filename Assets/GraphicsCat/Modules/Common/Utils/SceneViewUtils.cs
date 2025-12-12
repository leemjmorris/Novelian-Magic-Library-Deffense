using UnityEditor;

namespace GraphicsCat
{
    public class SceneViewUtils
    {
        public static void RepaintAll()
        {
#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }
    }
}
