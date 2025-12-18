// URP Compatible - Force Recompile 2025-12-19
Shader "Hovl/Particles/Distortion"
{
	Properties
	{
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		_NormalMap("Normal Map", 2D) = "bump" {}
		_Distortionpower("Distortion power", Float) = 1
		[Toggle]_Enablesimpleopacity("Enable simple opacity", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	// URP SubShader
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "RenderPipeline"="UniversalPipeline" }
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask RGB
		Cull Off
		Lighting Off
		ZWrite Off
		ZTest LEqual

		Pass {
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
				float4 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half4 color : COLOR;
				float4 texcoord : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
			};

			TEXTURE2D(_NormalMap);
			SAMPLER(sampler_NormalMap);

			CBUFFER_START(UnityPerMaterial)
				float _InvFade;
				float4 _NormalMap_ST;
				float _Distortionpower;
				float _Enablesimpleopacity;
			CBUFFER_END

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.screenPos = ComputeScreenPos(o.vertex);
				o.color = v.color;
				o.texcoord = v.texcoord;
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				// Screen UV 계산
				float2 screenUV = i.screenPos.xy / i.screenPos.w;

				// Normal map 샘플링
				float2 uv_NormalMap = i.texcoord.xy * _NormalMap_ST.xy + _NormalMap_ST.zw;
				half4 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv_NormalMap);
				half3 normal = normalTex.xyz * 2.0 - 1.0;

				// Distortion 계산
				float distortPower = _Distortionpower / 1000.0;
				float opacityMult = _Enablesimpleopacity > 0.5 ? 1.0 : i.color.a;
				float2 distort = normal.xy * distortPower * opacityMult;

				// Distorted screen color
				float2 distortedUV = screenUV - distort;
				half3 sceneColor = SampleSceneColor(distortedUV);

				// Alpha 계산
				float normalStrength = (abs(normal.r) + abs(normal.g)) * 30.0 - 0.3;
				float alpha = saturate(normalStrength);
				float finalAlpha = _Enablesimpleopacity > 0.5 ? i.color.a : 1.0;
				alpha *= finalAlpha;

				return half4(saturate(sceneColor), alpha);
			}
			ENDHLSL
		}
	}

	Fallback "Particles/Standard Unlit"
}
