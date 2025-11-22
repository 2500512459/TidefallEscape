Shader "Hovl/Particles/Blend_TwoSides"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_MainTex("Main Tex", 2D) = "white" {}
		_Mask("Mask", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_SpeedMainTexUVNoiseZW("Speed MainTex U/V + Noise Z/W", Vector) = (0,0,0,0)
		_FrontFacesColor("Front Faces Color", Color) = (0,0.2313726,1,1)
		_BackFacesColor("Back Faces Color", Color) = (0.1098039,0.4235294,1,1)
		_Emission("Emission", Float) = 2
		[Toggle]_UseFresnel("Use Fresnel?", Float) = 1
		[Toggle]_SeparateFresnel("SeparateFresnel", Float) = 0
		_SeparateEmission("Separate Emission", Float) = 2
		_FresnelColor("Fresnel Color", Color) = (1,1,1,1)
		_Fresnel("Fresnel", Float) = 1
		_FresnelEmission("Fresnel Emission", Float) = 1
		[Toggle]_UseCustomData("Use Custom Data?", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _tex4coord( "", 2D ) = "white" {}
	}

	SubShader
	{
		Tags 
		{ 
			"RenderType" = "TransparentCutout"
			"Queue" = "Transparent"
			"RenderPipeline" = "UniversalPipeline"
			"IgnoreProjector" = "True"
		}
		
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
				float4 uv_tex4coord : TEXCOORD1;
				float4 vertexColor : COLOR;
			};

			struct Varyings
			{
				float4 positionHCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float3 viewDirWS : TEXCOORD2;
				float2 uv : TEXCOORD3;
				float4 uv_tex4coord : TEXCOORD4;
				float4 vertexColor : COLOR;
			};

			CBUFFER_START(UnityPerMaterial)
				float _Cutoff;
				float4 _MainTex_ST;
				float4 _Mask_ST;
				float4 _Noise_ST;
				float4 _SpeedMainTexUVNoiseZW;
				float4 _FrontFacesColor;
				float4 _BackFacesColor;
				float _Emission;
				float _UseFresnel;
				float _SeparateFresnel;
				float _SeparateEmission;
				float4 _FresnelColor;
				float _Fresnel;
				float _FresnelEmission;
				float _UseCustomData;
			CBUFFER_END

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_Mask);
			SAMPLER(sampler_Mask);
			TEXTURE2D(_Noise);
			SAMPLER(sampler_Noise);

			Varyings vert(Attributes input)
			{
				Varyings output;
				
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
				
				output.positionHCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
				output.uv = input.uv;
				output.uv_tex4coord = input.uv_tex4coord;
				output.vertexColor = input.vertexColor;
				
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				// 计算世界空间法线和视角方向
				float3 ase_worldNormal = normalize(input.normalWS);
				float3 ase_worldViewDir = normalize(input.viewDirWS);
				
				// 计算Fresnel
				float fresnelNdotV95 = dot(ase_worldNormal, ase_worldViewDir);
				float fresnelNode95 = (0.0 + 1.0 * pow(1.0 - fresnelNdotV95, _Fresnel));
				
				// 计算前面/后面
				float dotResult87 = dot(ase_worldNormal, ase_worldViewDir);
				float remap89 = (1.0 + (sign(dotResult87) - -1.0) * (0.0 - 1.0) / (1.0 - -1.0));
				
				// 计算Fresnel混合颜色
				float oneMinusFresnel = 1.0 - fresnelNode95;
				float4 fresnelColor = _FrontFacesColor * oneMinusFresnel + _FresnelEmission * _FresnelColor * fresnelNode95;
				float4 frontColor = lerp(_FrontFacesColor, fresnelColor, _UseFresnel);
				float4 lerpResult91 = lerp(frontColor, _BackFacesColor, remap89);
				
				// 计算主纹理UV动画
				float2 uv0_MainTex = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 appendResult21 = float2(_SpeedMainTexUVNoiseZW.x, _SpeedMainTexUVNoiseZW.y);
				float4 tex2DNode105 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv0_MainTex + (appendResult21 * _Time.y));
				
				// 计算发射颜色
				float4 separateFresnelColor = lerpResult91 + (_FresnelColor * tex2DNode105 * _SeparateEmission);
				float4 emission1 = lerpResult91 * _Emission * input.vertexColor * input.vertexColor.a * tex2DNode105;
				float4 emission2 = separateFresnelColor * _Emission * input.vertexColor * input.vertexColor.a;
				half3 emission = lerp(emission1, emission2, _SeparateFresnel).rgb;
				
				// 计算遮罩和噪声
				float2 uv_Mask = input.uv * _Mask_ST.xy + _Mask_ST.zw;
				float4 uv0_Noise = input.uv_tex4coord;
				uv0_Noise.xy = input.uv_tex4coord.xy * _Noise_ST.xy + _Noise_ST.zw;
				float2 appendResult22 = float2(_SpeedMainTexUVNoiseZW.z, _SpeedMainTexUVNoiseZW.w);
				float2 noiseUV = uv0_Noise.xy + (_Time.y * appendResult22) + uv0_Noise.w;
				float4 mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv_Mask);
				float4 noise = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, noiseUV);
				float customDataFactor = lerp(1.0, uv0_Noise.z, _UseCustomData);
				float clipValue = (mask * noise * customDataFactor).r;
				clip(clipValue - _Cutoff);
				
				half4 color;
				color.rgb = emission;
				color.a = 1.0;
				
				return color;
			}
			ENDHLSL
		}
	}
}