using System.Collections.Generic;
using UnityEngine;

namespace GraphicsCat
{
    public static class GameObjectUtils
    {
        public static T[] FindObjectsByType<T>() where T : Object
        {
            return GameObject.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        public static List<GameObject> FindGameObjectsWithMeshRenderer(bool includeInactive = false)
        {
            FindObjectsInactive findObjectsInactive = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            var renderers = Object.FindObjectsByType<Renderer>(findObjectsInactive, FindObjectsSortMode.None);

            var gos = new List<GameObject>(renderers.Length);
            foreach (var renderer in renderers)
            {
                if (renderer is MeshRenderer or SkinnedMeshRenderer)
                    gos.Add(renderer.gameObject);
            }
            return gos;
        }
    }
}

