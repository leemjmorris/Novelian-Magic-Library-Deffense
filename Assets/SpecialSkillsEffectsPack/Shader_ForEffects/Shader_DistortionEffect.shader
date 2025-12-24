// URP Compatible - Force Recompile 2025-12-19
Shader "GAPH Custom Shader/Distortion Effect" {
	Properties {
		_TintColor ("Tint Color", Color) = (1,1,1,1)
		_Mask ("Mask",2D) = "black"{}
		_NormalMap ("Normalmap", 2D) = "bump" {}
		_DistortFactor ("Distortion", Float) = 10
		_InvFade ("Soft Particles Factor", Range(0,10)) = 1.0
	}

	SubShader{
		// URP: GrabPass 대신 _CameraOpaqueTexture 사용
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		Lighting Off
		ZWrite Off

		Pass{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_particles

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord: TEXCOORD0;
				half4 color : COLOR;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD0;
				float2 uvnormal : TEXCOORD1;
				float2 uvmask : TEXCOORD2;
				half4 color : COLOR;
			};

			TEXTURE2D(_Mask);
			SAMPLER(sampler_Mask);
			TEXTURE2D(_NormalMap);
			SAMPLER(sampler_NormalMap);

			CBUFFER_START(UnityPerMaterial)
				half4 _TintColor;
				float _DistortFactor;
				float4 _NormalMap_ST;
				float4 _Mask_ST;
				float _InvFade;
			CBUFFER_END

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.screenPos = ComputeScreenPos(o.vertex);
				o.color = v.color;
				o.uvnormal = TRANSFORM_TEX(v.texcoord, _NormalMap);
				o.uvmask = TRANSFORM_TEX(v.texcoord, _Mask);
				return o;
			}

			half4 frag( v2f i ) : SV_Target
			{
				// Normal map 샘플링
				half4 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uvnormal);
				half2 normal = normalTex.rg * 2.0 - 1.0;

				// Screen UV 계산
				float2 screenUV = i.screenPos.xy / i.screenPos.w;

				// Distortion 적용
				float2 distortValue = normal * _DistortFactor * 0.01;
				screenUV += distortValue;

				// URP의 _CameraOpaqueTexture 샘플링
				half4 distort = half4(SampleSceneColor(screenUV), 1.0);

				// Mask 샘플링
				half4 mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.uvmask);

				half4 res = distort;
				res.a = _TintColor.a * i.color.a * mask.a;
				return res;
			}
			ENDHLSL
		}
	}

	Fallback "Particles/Standard Unlit"
}
