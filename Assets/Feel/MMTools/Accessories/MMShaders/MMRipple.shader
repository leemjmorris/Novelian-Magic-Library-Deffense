// URP Compatible - Force Recompile 2025-12-19
Shader "MoreMountains/MMRipple"
{
    Properties
    {
        _RippleAlpha("Ripple Alpha", Float) = 1
        _RippleIntensity("Ripple Intensity", Float) = 1
        _Hue("Hue", Color) = (1, 1, 1, 1)
        _NormalMap("Normal Map", 2D) = "white" {}
        _Density("Soft Particles Factor", Range(0, 3)) = 1
    }

    // URP SubShader
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+1" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct appdata_t
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                half4 color : COLOR;
                float2 normalMapUV : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float _RippleAlpha;
                float _RippleIntensity;
                half4 _Hue;
                float _Density;
            CBUFFER_END

            v2f vert(appdata_t v)
            {
                v2f o;
                o.position = TransformObjectToHClip(v.vertex.xyz);
                o.screenPos = ComputeScreenPos(o.position);
                o.color = v.color;
                o.normalMapUV = v.texcoord;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Screen UV 계산
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // Normal map에서 ripple 값 추출
                half4 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.normalMapUV);
                half3 ripple = normalTex.xyz * 2.0 - 1.0;

                // Ripple distortion 적용
                float2 distortedUV = screenUV + ripple.xy / ripple.z * _RippleIntensity * i.color.a * 0.1;

                // Scene color 샘플링
                half3 backgroundColor = SampleSceneColor(distortedUV);

                half4 result = half4(backgroundColor, 1.0) * _Hue;
                result.a = _RippleAlpha;
                return result;
            }
            ENDHLSL
        }
    }

    Fallback "Particles/Standard Unlit"
}
