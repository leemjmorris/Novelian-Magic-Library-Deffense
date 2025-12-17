#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class RenderSettings : Tag
    {
        protected override string m_beginTag => "BeginRenderSettings";
        protected override string m_EndTag => "EndRenderSettings";

        enum SurfaceType { Opaque, Transparent }
        readonly string _SURFACE_TYPE_OPAQUE = "_SURFACE_TYPE_OPAQUE";
        readonly string _SURFACE_TYPE_TRANSPARENT = "_SURFACE_TYPE_TRANSPARENT";
        readonly string k_SurfaceTypeName = "_SURFACE_TYPE";
        readonly string[] k_SurfaceTypeOptions = System.Enum.GetNames(typeof(SurfaceType));
        readonly int[] k_SurfaceTypeValues = (int[])System.Enum.GetValues(typeof(SurfaceType));
        SurfaceType? m_SurfaceType;
        bool m_SurfaceTypeChanged;

        readonly string _ALPHATEST_ON = "_ALPHATEST_ON";
        readonly string k_AlphaClippingName = "_AlphaClipping";
        bool? m_AlphaClipping;
        readonly string k_CutoffName = "_Cutoff";
        float? m_Cutoff;

        readonly string k_PreserveSpecular = "_PreserveSpecular";
        bool? m_PreserveSpecular;

        readonly string k_CullModeName = "_Cull";
        readonly string[] k_CullModeOptions = System.Enum.GetNames(typeof(CullMode));
        readonly int[] k_CullModeValues = (int[])System.Enum.GetValues(typeof(CullMode));
        CullMode? m_CullMode;

        readonly string k_ZTestName = "_ZTest";
        readonly string[] k_ZTestOptions = System.Enum.GetNames(typeof(CompareFunction));
        int[] k_ZTestValues = (int[])System.Enum.GetValues(typeof(CompareFunction));
        CompareFunction? m_ZTest;

        enum ZWriteControl { Auto, Off, On }
        readonly string k_ZWriteControlName = "_ZWriteControl";
        readonly string[] k_ZWriteControlOptions = System.Enum.GetNames(typeof(ZWriteControl));
        readonly int[] k_ZWriteControlValues = (int[])System.Enum.GetValues(typeof(ZWriteControl));
        readonly string k_ZWriteName = "_ZWrite";
        ZWriteControl? m_ZWriteControl;

        enum BlendControl { Auto, Custom, Alpha, Premultiply, Additive, Multiply }
        readonly string k_BlendControlName = "_BlendControl";
        readonly string[] k_BlendControlOptions = System.Enum.GetNames(typeof(BlendControl));
        readonly int[] k_BlendControlValues = (int[])System.Enum.GetValues(typeof(BlendControl));
        BlendControl? m_BlendControl;

        readonly string k_SrcBlendName = "_SrcBlend";
        readonly string k_DstBlendName = "_DstBlend";
        readonly string k_AlphaSrcBlendName = "_AlphaSrcBlend";
        readonly string k_AlphaDstBlendName = "_AlphaDstBlend";
        readonly string[] k_BlendFactorOptions = System.Enum.GetNames(typeof(BlendMode));
        readonly int[] k_BlendFactorValues = (int[])System.Enum.GetValues(typeof(BlendMode));
        BlendMode? m_SrcBlend, m_DstBlend, m_AlphaSrcBlend, m_AlphaDstBlend;

        readonly string _RECEIVE_SHADOWS_OFF = "_RECEIVE_SHADOWS_OFF";
        readonly string k_CastShadowsName = "_CastShadows";
        readonly string k_ReceiveShadowsName = "_ReceiveShadows";
        bool? m_CastShadows;
        bool? m_ReceiveShadows;

        enum QueueControl { Auto, Custom }
        readonly string k_QueueControlName = "_QueueControl";
        QueueControl? m_QueueControl;
        readonly string k_QueueOffsetName = "_QueueOffset";
        int? m_QueueOffset;

        MarkupShaderGUI.Context m_Context;
        UnityEngine.Object[] m_Targets; 
        bool m_PropChanged = false;

        public void InitNewMaterials(MarkupShaderGUI.Context context)
        {
            m_Context = context;
            if (m_Context.materialEditor.targets != m_Targets)
            {
                m_Targets = m_Context.materialEditor.targets;
                CollectPropertyValues();
                UpdateMaterials();
            }
        }

        public override void OnBegin(MarkupShaderGUI.Context context)
        {
            m_PropChanged = false;

            DrawSurfaceType();
            DrawAlphaClipping();
            DrawPreserveSpecular();

            DrawHelper.DrawSeparator();
            DrawCullMode();
            DrawZTest();
            DrawZWrite();
            DrawBlendMode();

            DrawHelper.DrawSeparator();
            DrawShadows();

            DrawHelper.DrawSeparator();
            DrawRenderQueue();

            if (m_PropChanged)
                UpdateMaterials();

            DrawHelper.DrawSeparator();
            context.materialEditor.EnableInstancingField();
            context.materialEditor.DoubleSidedGIField();
        }

        void CollectPropertyValues()
        {
            m_SurfaceType = GetMixedEnum<SurfaceType>(k_SurfaceTypeName);
            m_AlphaClipping = GetMixedBool(k_AlphaClippingName);
            m_PreserveSpecular = GetMixedBool(k_PreserveSpecular);

            m_Cutoff = GetMixedFloat(k_CutoffName);
            m_CullMode = GetMixedEnum<CullMode>(k_CullModeName);
            m_ZTest = GetMixedEnum<CompareFunction>(k_ZTestName);
            m_ZWriteControl = GetMixedEnum<ZWriteControl>(k_ZWriteControlName);
            m_BlendControl = GetMixedEnum<BlendControl>(k_BlendControlName);
            m_SrcBlend = GetMixedEnum<BlendMode>(k_SrcBlendName);
            m_DstBlend = GetMixedEnum<BlendMode>(k_DstBlendName);
            m_AlphaSrcBlend = GetMixedEnum<BlendMode>(k_AlphaSrcBlendName);
            m_AlphaDstBlend = GetMixedEnum<BlendMode>(k_AlphaDstBlendName);

            m_CastShadows = GetMixedBool(k_CastShadowsName);
            m_ReceiveShadows = GetMixedBool(k_ReceiveShadowsName);

            m_QueueControl = GetMixedEnum<QueueControl>(k_QueueControlName);
            m_QueueOffset = GetMixedInt(k_QueueOffsetName);
        }

        void DrawSurfaceType()
        {
            DrawEnum("Surface Type", k_SurfaceTypeName, ref m_SurfaceType, SurfaceType.Opaque);
        }

        void DrawAlphaClipping()
        {
            DrawToggle("Alpha Clipping", k_AlphaClippingName, ref m_AlphaClipping, false);

            if (m_AlphaClipping.HasValue && m_AlphaClipping.Value)
            {
                EditorGUI.indentLevel += 1;
                DrawFloatRange("Threshold", k_CutoffName, ref m_Cutoff, 0.5f, 0.0f, 1.0f);
                EditorGUI.indentLevel -= 1;
            }
        }

        void DrawPreserveSpecular()
        {
            DrawToggle("Preserve Specular", k_PreserveSpecular, ref m_PreserveSpecular, true);
        }

        void DrawCullMode()
        {
            DrawEnum("Cull Mode", k_CullModeName, ref m_CullMode, CullMode.Back);
        }

        void DrawZTest()
        {
            DrawEnum("Depth Test", k_ZTestName, ref m_ZTest, CompareFunction.LessEqual);
        }

        void DrawZWrite()
        {
            DrawEnum("Depth Write", k_ZWriteControlName, ref m_ZWriteControl, ZWriteControl.Auto);
        }

        void DrawBlendMode()
        {
            DrawEnum("Blend Control", k_BlendControlName, ref m_BlendControl, BlendControl.Auto);

            EditorGUI.indentLevel += 1;

            if (m_BlendControl.HasValue)
            {
                GUI.enabled = (m_BlendControl == BlendControl.Custom);
                DrawEnum("Src Blend", k_SrcBlendName, ref m_SrcBlend, BlendMode.One);
                DrawEnum("Dst Blend", k_DstBlendName, ref m_DstBlend, BlendMode.Zero);
                DrawEnum("Alpha Src Blend", k_AlphaSrcBlendName, ref m_AlphaSrcBlend, BlendMode.One);
                DrawEnum("Alpha Dst Blend", k_AlphaDstBlendName, ref m_AlphaDstBlend, BlendMode.Zero);
                GUI.enabled = true;
            }

            EditorGUI.indentLevel -= 1;
        }

        void DrawShadows()
        {
            DrawToggle("Cast Shadows", k_CastShadowsName, ref m_CastShadows, true);
            DrawToggle("Receive Shadows", k_ReceiveShadowsName, ref m_ReceiveShadows, true);
        }

        void DrawRenderQueue()
        {
            DrawEnum("Queue Control", k_QueueControlName, ref m_QueueControl, QueueControl.Auto);

            if (m_QueueControl.HasValue)
            {
                GUI.enabled = (m_QueueControl.Value == QueueControl.Auto);
                DrawIntRange("Offset", k_QueueOffsetName, ref m_QueueOffset, 0, -50, 50);

                GUI.enabled = (m_QueueControl.Value == QueueControl.Custom);
                m_Context.materialEditor.RenderQueueField();

                GUI.enabled = true;
            }
        }

        void DrawIntRange(string displayName, string propName, ref int? propValue, int defaultValue, int min, int max)
        {
            EditorGUI.showMixedValue = !propValue.HasValue;

            try
            {
                using (var _ = new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    // EditorGUILayout.LabelField(displayName, GUILayout.ExpandWidth(false));
                    // var newValue = EditorGUILayout.IntSlider(propValue ?? defaultValue, min, max, GUILayout.ExpandWidth(true));

                    GUILayout.Label(displayName, GUILayout.ExpandWidth(false));
                    var newValue = (int)GUILayout.HorizontalSlider(propValue ?? defaultValue, min, max, GUILayout.ExpandWidth(true));
                    newValue = IMGUIUtils.IntField(newValue);

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_PropChanged = true;

                        propValue = newValue;
                        foreach (var material in m_Context.materials)
                            material.SetInt(propName, propValue.Value);
                    }
                }
            }
            catch
            {
                EditorGUI.showMixedValue = false;
            }
        }

        void DrawFloatRange(string displayName, string propName, ref float? propValue, float defaultValue, float min, float max)
        {
            EditorGUI.showMixedValue = !propValue.HasValue;

            try
            {
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.Slider(displayName, propValue ?? defaultValue, min, max);
                if (EditorGUI.EndChangeCheck())
                {
                    m_PropChanged = true;

                    propValue = newValue;
                    foreach (var material in m_Context.materials)
                        material.SetFloat(propName, propValue.Value);
                }
            }
            catch
            {
                EditorGUI.showMixedValue = false;
            }
        }

        void DrawEnum<T>(string displayName, string propName, ref T? propValue, T defaultValue) where T : struct, System.Enum
        {
            EditorGUI.showMixedValue = !propValue.HasValue;

            try
            {
                EditorGUI.BeginChangeCheck();
                var newValue = (T)EditorGUILayout.EnumPopup(displayName, propValue ?? defaultValue);
                if (EditorGUI.EndChangeCheck())
                {
                    m_PropChanged = true;

                    propValue = newValue;
                    var intValue = Convert.ToInt32(propValue.Value);
                    foreach (var material in m_Context.materials)
                        material.SetInt(propName, intValue);
                }
            }
            catch
            {
                EditorGUI.showMixedValue = false;
            }
        }

        void DrawToggle(string displayName, string propName, ref bool? propValue, bool defaultValue)
        {
            EditorGUI.showMixedValue = !propValue.HasValue;

            try
            {
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.Toggle(displayName, propValue ?? defaultValue);
                if (EditorGUI.EndChangeCheck())
                {
                    m_PropChanged = true;

                    propValue = newValue;
                    var intValue = propValue.Value ? 1 : 0;
                    foreach (var material in m_Context.materials)
                        material.SetInt(propName, intValue);
                }
            }
            catch
            {
                EditorGUI.showMixedValue = false;
            }
        }

        void UpdateMaterials()
        {
            UpdateRenderType();
            UpdateZWrite();
            UpdateBlendFactors();
            UpdateRenderQueue();
            UpdateShadows();
        }

        void UpdateRenderType()
        {
            foreach (var material in m_Context.materials)
            {
                var surfaceType = (SurfaceType)material.GetInt(k_SurfaceTypeName);
                var isOpaque = (surfaceType == SurfaceType.Opaque);
                var isTransparent = (surfaceType == SurfaceType.Transparent);

                var isAlphaClipping = (material.GetInt(k_AlphaClippingName) == 1);

                material.SetKeyword(_SURFACE_TYPE_OPAQUE, isOpaque);
                material.SetKeyword(_SURFACE_TYPE_TRANSPARENT, isTransparent);
                material.SetKeyword(_ALPHATEST_ON, isAlphaClipping);

                if (isAlphaClipping) // alpha clipping first
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                else if (isTransparent)
                    material.SetOverrideTag("RenderType", "Transparent");
                else
                    material.SetOverrideTag("RenderType", ""); // Empty string resets the value
            }
        }

        void UpdateZWrite()
        {
            foreach (var material in m_Context.materials)
            {
                var zWriteMode = (ZWriteControl)material.GetInt(k_ZWriteControlName);

                var oldZWrite = material.GetInt(k_ZWriteName);
                var newZWrite = 0;

                switch (zWriteMode)
                {
                    case ZWriteControl.Auto:
                        var srufaceType = (SurfaceType)material.GetInt(k_SurfaceTypeName);
                        newZWrite = (srufaceType == SurfaceType.Transparent) ? 0 : 1;
                        break;
                    case ZWriteControl.Off: newZWrite = 0; break;
                    case ZWriteControl.On: newZWrite = 1; break;
                }

                if (oldZWrite != newZWrite)
                    material.SetInt(k_ZWriteName, newZWrite);
            }
        }

        void UpdateBlendFactors()
        {
            foreach (var material in m_Context.materials)
            {
                var blendingMode = (BlendControl)material.GetInt(k_BlendControlName);

                var srcBlend = (BlendMode)material.GetInt(k_SrcBlendName);
                var dstBlend = (BlendMode)material.GetInt(k_DstBlendName);
                var alphaSrcBlend = (BlendMode)material.GetInt(k_AlphaSrcBlendName);
                var alphaDstBlend = (BlendMode)material.GetInt(k_AlphaDstBlendName);

                var isAuto = (blendingMode == BlendControl.Auto);
                if (isAuto)
                {
                    var surfaceType = (SurfaceType)material.GetInt(k_SurfaceTypeName);
                    if (surfaceType == SurfaceType.Opaque)
                    {
                        srcBlend = BlendMode.One;
                        dstBlend = BlendMode.Zero;
                        alphaSrcBlend = BlendMode.One;
                        alphaDstBlend = BlendMode.Zero;
                    }
                    else if (surfaceType == SurfaceType.Transparent)
                    {
                        srcBlend = BlendMode.SrcAlpha;
                        dstBlend = BlendMode.OneMinusSrcAlpha;
                        alphaSrcBlend = BlendMode.One;
                        alphaDstBlend = dstBlend;
                    }
                }

                var isAlpha = (blendingMode == BlendControl.Alpha);
                if (isAlpha)
                {
                    srcBlend = BlendMode.SrcAlpha;
                    dstBlend = BlendMode.OneMinusSrcAlpha;
                    alphaSrcBlend = BlendMode.One;
                    alphaDstBlend = dstBlend;
                }

                var isPremultiply = (blendingMode == BlendControl.Premultiply);
                if (isPremultiply)
                {
                    srcBlend = BlendMode.One;
                    dstBlend = BlendMode.OneMinusSrcAlpha;
                    alphaSrcBlend = srcBlend;
                    alphaDstBlend = dstBlend;
                }

                var isAdditive = (blendingMode == BlendControl.Additive);
                if (isAdditive)
                {
                    srcBlend = BlendMode.SrcAlpha;
                    dstBlend = BlendMode.One;
                    alphaSrcBlend = BlendMode.One;
                    alphaDstBlend = dstBlend;
                }

                var isMultiply = (blendingMode == BlendControl.Multiply);
                if (isMultiply)
                {
                    srcBlend = BlendMode.DstColor;
                    dstBlend = BlendMode.Zero;
                    alphaSrcBlend = BlendMode.Zero;
                    alphaDstBlend = BlendMode.One;
                }

                SetBlendingFactors(material, srcBlend, dstBlend, alphaSrcBlend, alphaDstBlend);
            }
        }

        void UpdateShadows()
        {
            foreach (var material in m_Context.materials)
            {
                var castShadows = material.GetInt(k_CastShadowsName);
                material.SetShaderPassEnabled("ShadowCaster", (castShadows == 1));

                var receiveShadows = material.GetInt(k_ReceiveShadowsName);
                material.SetKeyword(_RECEIVE_SHADOWS_OFF, (receiveShadows != 1));
            }
        }

        void UpdateRenderQueue()
        {
            foreach (var material in m_Context.materials)
            {
                var queueControl = (QueueControl)material.GetInt(k_QueueControlName);
                if (queueControl == QueueControl.Auto)
                {
                    var surfaceType = (SurfaceType)material.GetInt(k_SurfaceTypeName);
                    if (surfaceType == SurfaceType.Transparent)
                        material.renderQueue = (int)RenderQueue.Transparent;
                    else
                    {
                        var isAlphaClipping = (material.GetInt(k_AlphaClippingName) == 1);
                        if (isAlphaClipping)
                            material.renderQueue = (int)RenderQueue.AlphaTest;
                        else
                            material.renderQueue = (int)RenderQueue.Geometry;
                    }

                    var queueOffset = material.GetInt(k_QueueOffsetName);
                    material.renderQueue += queueOffset;
                }
            }
        }

        void SetBlendingFactors(Material material, BlendMode colorSrc, BlendMode colorDst, BlendMode alphaSrc, BlendMode alphaDst)
        {
            material.SetInt("_SrcBlend", (int)colorSrc);
            material.SetInt("_DstBlend", (int)colorDst);
            material.SetInt("_AlphaSrcBlend", (int)alphaSrc);
            material.SetInt("_AlphaDstBlend", (int)alphaDst);
        }

        T? GetMixedEnum<T>(string prop) where T : struct
        {
            var first = m_Context.materials[0].GetInt(prop);
            if (m_Context.materials.All(m => m.GetInt(prop) == first))
                return (T)(object)first;
            return null;
        }

        bool? GetMixedBool(string prop)
        {
            var first = m_Context.materials[0].GetInt(prop);
            if (m_Context.materials.All(m => m.GetInt(prop) == first))
                return first == 1;
            return null;
        }

        int? GetMixedInt(string prop)
        {
            var first = m_Context.materials[0].GetInt(prop);
            if (m_Context.materials.All(m => m.GetInt(prop) == first))
                return first;
            return null;
        }

        float? GetMixedFloat(string prop)
        {
            var first = m_Context.materials[0].GetFloat(prop);
            if (m_Context.materials.All(m => m.GetFloat(prop) == first))
                return first;
            return null;
        }
    }
}

#endif
