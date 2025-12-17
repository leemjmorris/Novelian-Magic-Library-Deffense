#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GraphicsCat
{
    public static class AssetDatabaseUtils
    {
        public static T LoadFirstAsset<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length > 0)
            {
                var guid = guids[0];
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return null;
        }

        public static List<T> LoadAssets<T>() where T : Object
        {
            var assets = new List<T>();
            
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            for (int i = 0; i < guids.Length; ++i)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                assets.Add(AssetDatabase.LoadAssetAtPath<T>(path));
            }

            return assets;
        }

        public static T[] LoadAssetsByFullName<T>(string fullName) where T : Object
        {
            // Get all assets of the specified type
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name} {fullName}");

            return guids.Select(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                return new { path, fileName };
            })
            .Where(x => x.fileName == fullName) // Exact name match
            .Select(x => AssetDatabase.LoadAssetAtPath<T>(x.path))
            .Where(x => x != null) // Filter out any failed loads
            .ToArray();
        }

        public static T LoadFirstAssetByFullName<T>(string fullName) where T : Object
        {
            var assets = LoadAssetsByFullName<T>(fullName);
            if (assets.Length > 0)
                return assets[0];
            return null;
        }

        /// <summary>
        /// Finds assets of any type that exactly match the given full name
        /// </summary>
        public static Object[] LoadAssetsByFullName(string fullName)
        {
            string[] guids = AssetDatabase.FindAssets(""); // All assets

            return guids.Select(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                return new { path, fileName };
            })
            .Where(x => x.fileName == fullName)
            .Select(x => AssetDatabase.LoadMainAssetAtPath(x.path))
            .Where(x => x != null)
            .ToArray();
        }
    }
}

#endif