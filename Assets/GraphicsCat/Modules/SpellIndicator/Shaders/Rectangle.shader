Shader "GraphicsCat/SpellIndicator/Rectangle"
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
            [FloatRange(0.1, 20)] _MainWidth("Width", Range(0.1, 20)) = 5
            [FloatRange(0.1, 20)] _MainHeight("Height", Range(0.1, 20)) = 10
            [FloatRange(0, 1)] _MainAlpha("Alpha", Range(0, 1)) = 1
            [FloatRange(0, 1)] _MainAlphaFalloff("Alpha Falloff", Range(0, 1)) = 0.5
        [EndFoldout]

        [BeginFoldout(Base Settings)]
            [BeginMiniTextureWithColor]
                [NoScaleOffset] _BaseTex("Texture", 2D) = "white" {}
                _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1)
            [EndMiniTextureWithColor]
            [FloatRange(0, 5)] _BaseIntensity("Intensity", Range(0, 5)) = 1
        [EndFoldout]

        [BeginFoldout(Base Boundary)]
            [Toggle(BASE_BOUNDARY_ON)] _BaseBoundaryOn("Enable", Float) = 0
            [BeginEnableIf(_BaseBoundaryOn, Equal, 1)]
                _BaseBoundaryColor("Color", Color) = (1, 1, 1, 1)
                [FloatRange(0, 5)] _BaseBoundaryIntensity("Intensity", Range(0, 5)) = 1
                [FloatRange(0, 0.1)] _BaseBoundaryThickness("Thickness", Range(0, 0.1)) = 0.01
            [EndEnableIf]
        [EndFoldout]

        [BeginFoldout(Fill Settings)]
            [Toggle(FILL_ON)] _FillOn("Enable", Float) = 1
            [BeginEnableIf(_FillOn, Equal, 1)]
                [MiniTexture][NoScaleOffset] _FillTex("Texture", 2D) = "white" {}
                _FillColor("Color", Color) = (1, 1, 1, 1)
                [FloatRange(0, 5)] _FillIntensity("Intensity", Range(0, 5)) = 1
                [FloatRange(0, 10)] _FillAlphaFalloff("Alpha Falloff", Range(0, 10)) = 1
                [FloatRange(0, 1)] _FillProgress("Progress", Range(0, 1)) = 0.5
            [EndEnableIf]
        [EndFoldout]
        
        [BeginFoldout(Fill Boundary)]
            [Toggle(FILL_BOUNDARY_ON)] _FillBoundaryOn("Enable", Float) = 0
            [BeginEnableIf(_FillBoundaryOn, Equal, 1)]
                _FillBoundaryColor("Color", Color) = (1, 1, 1, 1)
                [FloatRange(0, 5)] _FillBoundaryIntensity("Intensity", Range(0, 5)) = 1
                [FloatRange(0, 0.1)] _FillBoundaryThickness("Thickness", Range(0, 0.1)) = 0.01
            [EndEnableIf]
        [EndFoldout]
        
        [BeginFoldout(Nine Slice)]
            [Toggle(SLICED_ON)] _SlicedOn("Enable", Float) = 0
            [BeginEnableIf(_SlicedOn, Equal, 1)]
                [FloatRange(0, 10)] _BorderSize_Top("BorderSize - Top", Range(0, 10)) = 0.25
                [FloatRange(0, 10)] _BorderSize_Bottom("BorderSize - Bottom", Range(0, 10)) = 0.25
                [FloatRange(0, 10)] _BorderSize_Left("BorderSize - Left", Range(0, 10)) = 0.25
                [FloatRange(0, 10)] _BorderSize_Right("BorderSize - Right", Range(0, 10)) = 0.25
                [FloatRange(0, 1)] _BorderUV_Top("BorderUV - Top", Range(0, 1)) = 0.25
                [FloatRange(0, 1)] _BorderUV_Bottom("BorderUV - Bottom", Range(0, 1)) = 0.25
                [FloatRange(0, 1)] _BorderUV_Left("BorderUV - Left", Range(0, 1)) = 0.25
                [FloatRange(0, 1)] _BorderUV_Right("BorderUV - Right", Range(0, 1)) = 0.25
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

            #pragma multi_compile _ SLICED_ON
            #pragma multi_compile _ FILL_ON
            #pragma multi_compile _ BASE_BOUNDARY_ON
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
                float fogFactor : TEXCOORD1;
            };
            
            TEXTURE2D(_BaseTex);
            SAMPLER(sampler_BaseTex);

            TEXTURE2D(_FillTex);
            SAMPLER(sampler_FillTex);

            CBUFFER_START(UnityPerMaterial)
                half _MainWidth;
                half _MainHeight;
                half _MainAlpha;
                half _MainAlphaFalloff;

                half4 _BaseTex_ST;
                half4 _BaseColor;
                half _BaseIntensity;

                half4 _BaseBoundaryColor;
                half _BaseBoundaryIntensity;
                half _BaseBoundaryThickness;
           
                half4 _FillColor;
                half _FillIntensity;
                half _FillAlphaFalloff;
                half _FillProgress;

                half4 _FillBoundaryColor;
                half _FillBoundaryIntensity;
                half _FillBoundaryThickness;

                half _BorderSize_Top;
                half _BorderSize_Bottom;
                half _BorderSize_Left;
                half _BorderSize_Right;
                half _BorderUV_Top;
                half _BorderUV_Bottom;
                half _BorderUV_Left;
                half _BorderUV_Right;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float4 positionOS = input.positionOS;
                positionOS.x *= _MainWidth;
                positionOS.z *= _MainHeight;
                output.positionCS = TransformObjectToHClip(positionOS.xyz);
                
                output.uv = input.uv;
                
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }
            
            half CalculateBorderTopBottom(half2 uv)
            {
                half topBorderBegin = (_MainHeight - _BorderSize_Top);
                half bottomBorderEnd = _BorderSize_Bottom;

                half current = uv.y * _MainHeight;
                if (current > topBorderBegin)
                {
                    half percent = (current - topBorderBegin) / _BorderSize_Top;
                    return lerp(1 - _BorderUV_Top, 1, percent);
                }
                else if (current < bottomBorderEnd) 
                {
                    half percent = current/bottomBorderEnd;
                    return lerp(0, _BorderUV_Bottom, percent);
                }
                else
                {
                    half percent = (current - bottomBorderEnd) / max(topBorderBegin - bottomBorderEnd, 1e-5);
                    return lerp(_BorderUV_Bottom, 1 - _BorderUV_Top, percent);
                }
            }

            half CalculateBorderLeftRight(half2 uv)
            {
                half rightBorderBegin = (_MainWidth - _BorderSize_Right);
                half leftBorderEnd = _BorderSize_Left;

                half current = uv.x * _MainWidth;
                if (current > rightBorderBegin)
                {
                    half percent = (current - rightBorderBegin) / max(_BorderSize_Right, 1e-5);
                    return lerp(1 - _BorderUV_Right, 1, percent);
                }
                else if (current < leftBorderEnd) 
                {
                    half percent = current/leftBorderEnd;
                    return lerp(0, _BorderUV_Left, percent);
                }
                else
                {
                    half percent = (current - leftBorderEnd) / max(rightBorderBegin - leftBorderEnd, 1e-5);
                    return lerp(_BorderUV_Left, 1 - _BorderUV_Right, percent);
                }
            }

            float CalculateMipLevel(float2 uv, float textureWidth, float textureHeight)
            {
                float2 dx = ddx(uv);
                float2 dy = ddy(uv);
                float maxDerivative = max(dot(dx, dx), dot(dy, dy));
                return 0.5 * log2(maxDerivative * textureWidth * textureHeight);
            }

            half CalculateBaseBoundaryMask(half2 uv)
            {
                half boundaryMask = 0;
                
                half2 areaSize = half2(_MainWidth, _MainHeight);
                half2 areaCenterUV = 0.5;
                half2 distanceToCenter = abs(uv.xy - areaCenterUV) * areaSize;
                half2 boundaryMaskXY = step(areaSize * 0.5, distanceToCenter + _BaseBoundaryThickness);
                boundaryMask = max(boundaryMaskXY.x, boundaryMaskXY.y);
                
                return boundaryMask;
            }

            half CalculateFillBoundaryMask(half2 uv)
            {
                half boundaryMask = 0;
                
                half2 areaSize = half2(_MainWidth, _FillProgress * _MainHeight);
                half2 areaCenterUV = half2(0.5, _FillProgress * 0.5);
                half2 distanceToCenter = abs(uv.xy - areaCenterUV) * half2(_MainWidth, _MainHeight);
                half2 boundaryMaskXY = step(areaSize * 0.5, distanceToCenter + _FillBoundaryThickness);
                boundaryMask = max(boundaryMaskXY.x, boundaryMaskXY.y);

                half isInsideArea = step(uv.y, _FillProgress);
                boundaryMask = min(boundaryMask, isInsideArea);
                
                return boundaryMask;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half2 uv = input.uv;
                half4 baseTexColor; 
                half4 fillTexColor;

                #ifdef SLICED_ON
                    float nineSliceUVx = CalculateBorderLeftRight(uv);
                    float nineSliceUVy = CalculateBorderTopBottom(uv);
                    float2 nineSliceUV = float2(nineSliceUVx, nineSliceUVy);
                    half mipLevel = CalculateMipLevel(uv, _BaseTex_ST.x, _BaseTex_ST.y);
                    baseTexColor = SAMPLE_TEXTURE2D_LOD(_BaseTex, sampler_BaseTex, nineSliceUV, mipLevel);
                    fillTexColor = SAMPLE_TEXTURE2D_LOD(_FillTex, sampler_FillTex, nineSliceUV, mipLevel);
                #else
                    baseTexColor = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, uv);
                    fillTexColor = SAMPLE_TEXTURE2D(_FillTex, sampler_FillTex, uv);
                #endif
                
                half fillMask = step(uv.y, _FillProgress);
                
                half4 baseColor;
                baseColor.rgb  = _BaseColor.rgb * baseTexColor.rgb * _BaseIntensity;
                baseColor.a = _BaseColor.a * baseTexColor.a;

                half4 finalColor = baseColor;

                #ifdef FILL_ON
                {
					half4 fillColor;
                    fillColor.rgb = _FillColor.rgb * _FillIntensity * fillTexColor.rgb;
                    fillColor.a =  _FillColor.a * fillTexColor.a;

                    half fillAlphaFalloff = uv.y/_FillProgress;
                    fillAlphaFalloff = clamp(fillAlphaFalloff, 1e-5, 1);
                    fillColor.a *= pow(fillAlphaFalloff, _FillAlphaFalloff);

                    fillColor *= fillMask;
                    
                    // fill color on top
                    finalColor.rgb = fillColor.rgb * fillColor.a + finalColor.rgb * (1 - fillColor.a);
                    finalColor.a = fillColor.a * (1 - finalColor.a) + finalColor.a;

                    // base color on top
                    // finalColor.rgb = finalColor.rgb * finalColor.a + fillColor.rgb * (1 - finalColor.a);
                    // finalColor.a = finalColor.a * (1 - fillColor.a) + fillColor.a;
                }
                #endif

                #ifdef BASE_BOUNDARY_ON
                {
                    half baseBoundaryMask = CalculateBaseBoundaryMask(input.uv);
                    half4 baseBoundaryColor = half4(_BaseBoundaryColor.rgb * _BaseBoundaryIntensity, _BaseBoundaryColor.a);
                    finalColor = lerp(finalColor, baseBoundaryColor, baseBoundaryMask);
                }
                #endif

                #ifdef FILL_BOUNDARY_ON
                {
                    half fillBoundaryMask = CalculateFillBoundaryMask(input.uv);
                    half4 fillBoundaryColor = half4(_FillBoundaryColor.rgb * _FillBoundaryIntensity, _FillBoundaryColor.a);
                    finalColor = lerp(finalColor, fillBoundaryColor, fillBoundaryMask);
                }
                #endif

                finalColor.a *= _MainAlpha;

                half mainAlphaFalloff = uv.y / (_MainAlphaFalloff + 1e-3);
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