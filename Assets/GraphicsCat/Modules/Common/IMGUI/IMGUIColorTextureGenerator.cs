using UnityEngine;
using System.Collections.Generic;

namespace GraphicsCat
{
    public static class IMGUIColorTextureGenerator
    {
        static Dictionary<string, Texture2D> m_CachedTextures = new Dictionary<string, Texture2D>();

        public static Texture2D GetTexture(Color color, int size = 4)
        {
            string key = $"{color.r:F2},{color.g:F2},{color.b:F2},{color.a:F2}";

            if (m_CachedTextures.TryGetValue(key, out Texture2D cachedTexture))
                return cachedTexture;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            int alphaHeight = Mathf.CeilToInt(size * 0.25f);

            Color solidAlphaColor = new Color(color.a, color.a, color.a, 1.0f);
            for (int y = 0; y < alphaHeight; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = solidAlphaColor;
                }
            }

            for (int y = alphaHeight; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, 1.0f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            texture.filterMode = FilterMode.Point;

            m_CachedTextures.Add(key, texture);
            return texture;
        }

        public static void ClearCache()
        {
            foreach (var texture in m_CachedTextures.Values)
                Object.DestroyImmediate(texture);
            m_CachedTextures.Clear();
        }
    }
}