using System.Collections.Generic;
using UnityEngine;

namespace GraphicsCat
{
    public static class MaterialUtils
    {
        public static HashSet<Material> FindAllMaterialsInScene(bool includeInactive = false)
        {
            var materials = new HashSet<Material>();
            var findMode = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;

            foreach (var renderer in Object.FindObjectsByType<Renderer>(findMode, FindObjectsSortMode.None))
            {
                if (renderer is not (MeshRenderer or SkinnedMeshRenderer))
                    continue;

                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            return materials;
        }
    }
}