using System.Collections.Generic;
using UnityEngine.Rendering;

namespace GraphicsCat
{
    public static class LightModeIds
    {
        public static readonly List<ShaderTagId> renderObjectsList = new List<ShaderTagId>()
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit")
            };

        public static readonly ShaderTagId[] renderObjectsArray = renderObjectsList.ToArray();
    }
}