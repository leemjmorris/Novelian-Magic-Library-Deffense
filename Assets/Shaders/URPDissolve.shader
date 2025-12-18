Shader "Custom/URPDissolve"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _NoiseMap("Noise Map", 2D) = "white" {}
        _DissolveAmount("Dissolve Amount", Range(0, 1)) = 0
        _EdgeWidth("Edge Width", Range(0, 0.2)) = 0.05
        _EdgeColor("Edge Color", Color) = (1, 0.5, 0, 1)
        [Toggle] _UseEmission("Use Emission", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NoiseMap_ST;
                half4 _BaseColor;
                half4 _EdgeColor;
                half _DissolveAmount;
                half _EdgeWidth;
                half _UseEmission;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample textures
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, input.uv * _NoiseMap_ST.xy).r;

                // Dissolve calculation
                half dissolveThreshold = _DissolveAmount;
                half edgeThreshold = dissolveThreshold + _EdgeWidth;

                // Clip pixels below threshold
                clip(noise - dissolveThreshold);

                // Calculate edge glow
                half edgeFactor = 1.0 - saturate((noise - dissolveThreshold) / _EdgeWidth);
                half3 finalColor = lerp(baseColor.rgb, _EdgeColor.rgb * 2.0, edgeFactor * _UseEmission);

                // Calculate alpha for smooth edge
                half alpha = baseColor.a * saturate((noise - dissolveThreshold) * 10.0);

                // Apply fog
                finalColor = MixFog(finalColor, input.fogCoord);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

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
            };

            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NoiseMap_ST;
                half4 _BaseColor;
                half4 _EdgeColor;
                half _DissolveAmount;
                half _EdgeWidth;
                half _UseEmission;
            CBUFFER_END

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                half noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, input.uv * _NoiseMap_ST.xy).r;
                clip(noise - _DissolveAmount);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
