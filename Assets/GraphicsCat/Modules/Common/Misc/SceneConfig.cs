using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GraphicsCat
{
    [ExecuteAlways]
    public class SceneConfig : MonoBehaviour
    {
        [Range(0, 1000)]
        public float shadowDistance = 100f;

        void OnEnable()
        {
            OnValidate();
        }

        void Start()
        {
            OnValidate();
        }

        void OnValidate()
        {
            if (isActiveAndEnabled == false)
                return;

            var currentRP = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (currentRP != null)
                currentRP.shadowDistance = shadowDistance;
        }
    }
}
