using UnityEngine;

namespace GraphicsCat
{
    public static class MaterialExtensions
    {
        public static void SetKeyword(this Material mat, string keyword, bool val)
        {
            if (val == true)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }
    }
}