using UnityEngine;

namespace GraphicsCat
{
    public static class GraphicsCatUtils
    {
        public static GameObject GetGraphicsCatRoot()
        {
            var name = "GraphicsCat";
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                GameObject.DontDestroyOnLoad(go);
            }
            return go;
        }
    }
}
