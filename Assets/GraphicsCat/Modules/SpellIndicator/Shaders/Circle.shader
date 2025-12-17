Shader "GraphicsCat/SpellIndicator/Circle"
{
    Properties
    {
        // BlendModes
        // 0 Zero
        // 1 One
        // 2 DstColor
        // 3 SrcColor
        // 4 OneMinusDstColor
        // 5 SrcAlpha
        // 6 OneMinusSrcColor
        // 7 DstAlpha
        // 8 OneMinusDstAlpha
        // 9 SrcAlphaSaturate
        // 10 OneMinusSrcAlpha

        [BeginFoldout(Blending Mode)]
            [Enum(UnityEngine.Rendering.BlendMode)] _CustomSrcBlend("Src Blend", Float) = 5
            [Enum(UnityEngine.Rendering.BlendMode)] _CustomDstBlend("Dst Blend", Float) = 10
        [EndFoldout]

        [BeginFoldout(Main Settings)]
            [FloatRange(0, 20)] _MainRadius("Radius", Range(0, 20)) = 3
            [FloatRange(0, 1)] _MainAlpha("Alpha", Range(0, 1)) = 1
            [FloatRange(0, 1)] _MainAlphaFalloff("Alpha Falloff", Range(0, 1)) = 0
        [EndFoldout]

        [BeginFoldout(Base Settings)]
            [BeginMiniTextureWithColor]
                [NoScaleOffset] _BaseTex("Texture", 2D) = "white" {}
                _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1)
            [EndMiniTextureWithColor]
            [FloatRange(0, 5)] _BaseIntensity("Intensity", Range(0, 5)) = 1
            [FloatRange(0, 1)] _BaseRadius("Radius", Range(0, 1)) = 1
            [FloatRange(0, 1)]_BaseAngle("Angle", Range(0, 1)) = 1
            [FloatRange(0, 1)]_BaseRotationSpeed("Rotation Speed", Range(0, 1)) = 0.5
        [EndFoldout]

        [BeginFoldout(Base Boundary)]
            [Toggle(BASE_BOUNDARY_ON)] _BaseBoundaryOn("Enable", Float) = 0
            [BeginEnableIf(_BaseBoundaryOn, Equal, 1)]        
                _BaseBoundaryColor("Color", Color) = (1, 1, 1, 1)
                [FloatRange(0, 5)] _BaseBoundaryIntensity("Intensity", Range(0, 5.0)) = 1
                [FloatRange(0, 0.1)] _BaseBoundaryThickness("Thickness", Range(0, 0.1)) = 0.01
            [EndEnableIf]
        [EndFoldout]

        [BeginFoldout(Fill Settings)]
            [Toggle(FILL_ON)] _FillOn("Enable", Float) = 1
            [BeginEnableIf(_FillOn, Equal, 1)]        
                [MiniTexture][NoScaleOffset] _FillTex("Texture", 2D) = "white" {}
                _FillColor("Color", Color) = (1, 1, 1, 1)
                [FloatRange(0, 5)] _FillIntensity("Intensity", Range(0, 5)) = 1
                [FloatRange(0, 10)] _FillAlphaFalloff("Alpha Falloff", Range(0, 10)) = 0
                [FloatRange(0, 1)] _FillProgress("Progress", Range(0, 1)) = 0.8
                [FloatRange(0, 1)] _FillAngle("Angle", Range(0, 1)) = 1
            [EndEnableIf]
        [EndFoldout]

        [BeginFoldout(Fill Boundary)]
            [Toggle(FILL_BOUNDARY_ON)] _FillBoundaryOn("Enable", Range(0, 1)) = 0
            [BeginEnableIf(_FillBoundaryOn, Equal, 1)]        
                _FillBoundaryColor("Color", Color) = (1, 1, 1, 1)
                _FillBoundaryIntensity("Intensity", Range(0, 5)) = 1
                _FillBoundaryThickness("Thickness", Range(0, 0.1)) = 0.01
            [EndEnableIf]
        [EndFoldout]

        [Separator]
        [HideInInspector] _("", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        ZWrite Off

        Pass
        {
            Offset -1, -1
            Blend [_CustomSrcBlend] [_CustomDstBlend]

            HLSLPROGRAM

            #pragma multi_compile_fog

            #pragma multi_compile _ BASE_BOUNDARY_ON
            #pragma multi_compile _ FILL_ON
            #pragma multi_compile _ FILL_BOUNDARY_ON

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvRotated : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            TEXTURE2D(_BaseTex);
            SAMPLER(sampler_BaseTex);
            
            TEXTURE2D(_FillTex);
            SAMPLER(sampler_FillTex);

            CBUFFER_START(UnityPerMaterial)
                half _MainRadius;
                half _MainAlpha;
                half _MainAlphaFalloff;

                half4 _BaseColor;
                half _BaseIntensity;
                half _BaseRadius;
                half _BaseAngle;
                half _BaseRotationSpeed;

                half4 _FillColor;
                half _FillIntensity;
                half _FillAlphaFalloff;
                half _FillProgress;
                half _FillAngle;
        
                half4 _BaseBoundaryColor;
                half _BaseBoundaryIntensity;
                half _BaseBoundaryThickness;

                half4 _FillBoundaryColor;
                half _FillBoundaryIntensity;
                half _FillBoundaryThickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float4 positionOS = input.positionOS;
                positionOS.xz *= _MainRadius * 2;
                output.positionCS = TransformObjectToHClip(positionOS.xyz);

                output.uv = input.uv;

                half rot = _Time.y * _BaseRotationSpeed * 0.5;
                half2 center = half2(0.5, 0.5);
                half2 rotatedUV = input.uv - center;
                half s = sin(rot);
                half c = cos(rot);
                rotatedUV = half2(rotatedUV.x * c - rotatedUV.y * s, rotatedUV.x * s + rotatedUV.y * c);
                output.uvRotated = rotatedUV + center;

                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half2 center = input.uv - half2(0.5, 0.5);
                half dist = length(center) * 2.0;
    
                half4 baseTexColor = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, input.uvRotated);
                half4 fillTexColor = SAMPLE_TEXTURE2D(_FillTex, sampler_FillTex, input.uvRotated);
    
                half fillRadius = _BaseRadius * _FillProgress;
                half fillRadiusMask = step(dist, (float)fillRadius);
                half angle = atan2(center.y, center.x);
                half halfFillTotalAngle = _FillAngle * PI;
                half angleDiff = abs(angle + PI/2.0);
                angleDiff = min(angleDiff, 2.0 * PI - angleDiff);
                half fillAngleMask = step(angleDiff, (float)halfFillTotalAngle);
                half fillMask = fillRadiusMask * fillAngleMask;

                half baseRadiusMask = step(dist, (float)_BaseRadius);
                half baseAngleHalfTotal = _BaseAngle * PI;
                half baseAngleMask = step(angleDiff, (float)baseAngleHalfTotal);
                half baseMask = baseRadiusMask * baseAngleMask;

                half4 baseColor;
                baseColor.rgb = _BaseColor.rgb * baseTexColor.rgb * _BaseIntensity;
                baseColor.a = _BaseColor.a * baseTexColor.a;

                half4 finalColor = lerp(0, baseColor, baseMask);

                #ifdef FILL_ON
                {
                    half4 fillColor;
                    fillColor.rgb = _FillColor.rgb * _FillIntensity * fillTexColor.rgb;
                    fillColor.a = _FillColor.a * fillTexColor.a;

                    half fillAlphaFalloff = saturate(dist/fillRadius);
                    fillColor.a *= pow(fillAlphaFalloff, _FillAlphaFalloff);

                    fillColor *= fillMask;

                    finalColor.rgb = fillColor.rgb * fillColor.a + finalColor.rgb * (1 - fillColor.a);
                    finalColor.a = fillColor.a * (1 - finalColor.a) + finalColor.a;
                }
                #endif

                #ifdef BASE_BOUNDARY_ON
                {
                    half baseRadialBoundaryMask = step(dist, _BaseRadius) * (1 - step(dist, _BaseRadius - _BaseBoundaryThickness));
                    half baseAngularBoundaryCheck = step(abs(abs(angleDiff) - baseAngleHalfTotal), _BaseBoundaryThickness / dist);
                    half baseRadialBoundaryFactor = 1.0 - smoothstep(0.95, 1.0, _BaseAngle);
                    half baseAngularBoundaryMask = step(dist, _BaseRadius) * baseAngularBoundaryCheck * baseRadialBoundaryFactor;
                    half baseBoundaryMask = max(baseRadialBoundaryMask, baseAngularBoundaryMask) * baseMask;
        
                    half4 baseBoundaryColor = half4(_BaseBoundaryColor.rgb * _BaseBoundaryIntensity, _BaseBoundaryColor.a);
                    finalColor.rgb = lerp(finalColor.rgb, baseBoundaryColor.rgb, baseBoundaryMask);
                    finalColor.a = lerp(finalColor.a, baseBoundaryColor.a, baseBoundaryMask);
                }
                #endif

                #ifdef FILL_BOUNDARY_ON
                {
                    half fillRadialBoundaryMask = step(dist, fillRadius) * (1 - step(dist, fillRadius - _FillBoundaryThickness));
                    half fillAngularBoundaryCheck = step(abs(abs(angleDiff) - halfFillTotalAngle), _FillBoundaryThickness / dist);
                    half fillRadialBoundaryFactor = 1.0 - smoothstep(0.95, 1.0, _FillAngle);
                    half fillAngularBoundaryMask = step(dist, fillRadius) * fillAngularBoundaryCheck * fillRadialBoundaryFactor;
                    half fillBoundaryMask = max(fillRadialBoundaryMask, fillAngularBoundaryMask) * fillMask;
        
                    half4 fillBoundaryColor = half4(_FillBoundaryColor.rgb * _FillBoundaryIntensity, _FillBoundaryColor.a);
                    finalColor.rgb = lerp(finalColor.rgb, fillBoundaryColor.rgb, fillBoundaryMask);
                    finalColor.a = lerp(finalColor.a, fillBoundaryColor.a, fillBoundaryMask);
                }
                #endif

                finalColor.a *= _MainAlpha;

                half mainAlphaFalloff = dist / (_MainAlphaFalloff + 1e-3);
                mainAlphaFalloff = min(mainAlphaFalloff, 1);
                mainAlphaFalloff *= mainAlphaFalloff;
                finalColor.a *= mainAlphaFalloff;

                float3 foggedRGB = MixFogColor(finalColor.rgb, unity_FogColor.rgb, input.fogFactor);
                finalColor = half4(foggedRGB, finalColor.a);

                return finalColor;
            }

            ENDHLSL
        }
    }

    CustomEditor "GraphicsCat.MarkupShaderGUI"
}