using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GraphicsCat
{
    public static class URPUtils
    {


        public static UniversalRenderPipelineAsset GetCurrentRenderPipelineInstance()
        {
#if UNITY_EDITOR
            var pipelinePath = AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline);
            if (string.IsNullOrEmpty(pipelinePath) == false)
            {
                var pipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                var pipelineInstance = UniversalRenderPipelineAsset.Instantiate(pipelineAsset); // Do not modify local asset, so make a copy
                pipelineInstance.name = "PipelineInstance";
                QualitySettings.renderPipeline = pipelineInstance;
            }
#endif
            return GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        }

#if UNITY_EDITOR
        public static void AddRendererFeatureToDefaultRenderer<T>() where T : ScriptableRendererFeature
        {
            var rendererData = GetDefaultRendererData();
            if (rendererData == null)
                return;

            T rendererFeature = FindRendererFeature<T>(rendererData);
            if (rendererFeature != null)
                return;

            rendererFeature = ScriptableObject.CreateInstance<T>();
            rendererFeature.name = typeof(T).Name;
            rendererData.rendererFeatures.Add(rendererFeature);

            EditorUtility.SetDirty(rendererData);
            AssetDatabase.AddObjectToAsset(rendererFeature, rendererData);
            AssetDatabase.SaveAssets();
        }
#endif

        public static UniversalAdditionalCameraData GetMainCameraData()
        {
            var cam = Camera.main;
            if (cam == null)
                return null;

            return cam.GetUniversalAdditionalCameraData();
        }

        public static UniversalRendererData GetDefaultRendererData()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
                return null;

            var defaultRendererIndex = (int)ReflectionUtils.GetFieldValue(urpAsset, "m_DefaultRendererIndex");
            return GetRendererData(defaultRendererIndex);
        }

        public static UniversalRendererData GetRendererData(int index)
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
                return null;

            var rendererDataList = ReflectionUtils.GetFieldValue(urpAsset, "m_RendererDataList") as ScriptableRendererData[];
            if (rendererDataList?.Length > index)
                return rendererDataList[index] as UniversalRendererData;

            return null;
        }

        public static UniversalRendererData GetRendererData(Camera targetCamera)
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
                return null;

            var rendererDataList = ReflectionUtils.GetFieldValue(urpAsset, "m_RendererDataList") as ScriptableRendererData[];
            if (rendererDataList == null)
                return null;

            var cameraData = targetCamera.GetUniversalAdditionalCameraData();
            for (int rendererIndex = 0, count = rendererDataList.Length; rendererIndex < count; ++rendererIndex)
            {
                var rendererInstance = urpAsset.GetRenderer(rendererIndex);
                if (cameraData.scriptableRenderer == rendererInstance)
                {
                    if (rendererDataList?.Length > rendererIndex)
                        return rendererDataList[rendererIndex] as UniversalRendererData;
                }
            }
            return null;
        }

        public static T FindRendererFeature<T>(Camera targetCamera) where T : ScriptableRendererFeature
        {
            var cameraData = targetCamera.GetUniversalAdditionalCameraData();
            var currentRenderer = cameraData.scriptableRenderer as UniversalRenderer;
            var renderFeatures = ReflectionUtils.GetPropertyValue(currentRenderer, "rendererFeatures") as List<ScriptableRendererFeature>;
            foreach (var feature in renderFeatures)
            {
                if (feature is T targetFeature)
                    return targetFeature;
            }
            return null;
        }

        public static T FindRendererFeature<T>(ScriptableRendererData rendererData) where T : ScriptableRendererFeature
        {
            if (rendererData == null)
                return null;

            for (int i = 0, count = rendererData.rendererFeatures.Count; i < count; ++i)
            {
                var feature = rendererData.rendererFeatures[i];
                if (feature is T)
                    return (T)feature;
            }

            return null;
        }
    }
}
