Shader "Hovl/Particles/BlendDistort"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_Flow("Flow", 2D) = "white" {}
		_Mask("Mask", 2D) = "white" {}
		_NormalMap("NormalMap", 2D) = "bump" {}
		_Color("Color", Color) = (0.5,0.5,0.5,1)
		_Distortionpower("Distortion power", Float) = 0
		_SpeedMainTexUVNoiseZW("Speed MainTex U/V + Noise Z/W", Vector) = (0,0,0,0)
		_DistortionSpeedXYPowerZ("Distortion Speed XY Power Z", Vector) = (0,0,0,0)
		_Emission("Emission", Float) = 2
		_Opacity("Opacity", Range( 0 , 3)) = 1
		[Toggle]_Usedepth("Use depth?", Float) = 1
		[Toggle]_Softedges("Soft edges", Float) = 0
		_Depthpower("Depth power", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _tex4coord( "", 2D ) = "white" {}
	}

	SubShader
	{
		Tags 
		{ 
			"RenderType" = "Transparent"
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
			#pragma target 3.5
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
				float4 positionNDC : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float3 viewDirWS : TEXCOORD3;
				float2 uv : TEXCOORD4;
				float4 uv_tex4coord : TEXCOORD5;
				float4 vertexColor : COLOR;
			};

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _Noise_ST;
				float4 _Flow_ST;
				float4 _Mask_ST;
				float4 _NormalMap_ST;
				float4 _Color;
				float _Distortionpower;
				float4 _SpeedMainTexUVNoiseZW;
				float4 _DistortionSpeedXYPowerZ;
				float _Emission;
				float _Opacity;
				float _Usedepth;
				float _Softedges;
				float _Depthpower;
			CBUFFER_END

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_Noise);
			SAMPLER(sampler_Noise);
			TEXTURE2D(_Flow);
			SAMPLER(sampler_Flow);
			TEXTURE2D(_Mask);
			SAMPLER(sampler_Mask);
			TEXTURE2D(_NormalMap);
			SAMPLER(sampler_NormalMap);

			// 计算屏幕空间UV
			float2 CalculateScreenSpaceUV(float4 positionNDC)
			{
				float2 screenUV = positionNDC.xy / positionNDC.w;
				#if UNITY_UV_STARTS_AT_TOP
				screenUV.y = 1.0 - screenUV.y;
				#endif
				return screenUV;
			}

			// 解包法线纹理（简化版UnpackScaleNormal）
			float3 UnpackScaleNormal(float4 packedNormal, float scale)
			{
				float3 normal = UnpackNormal(packedNormal);
				normal.xy *= scale;
				return normal;
			}

			Varyings vert(Attributes input)
			{
				Varyings output;
				
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
				
				output.positionHCS = vertexInput.positionCS;
				output.positionNDC = vertexInput.positionNDC;
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
				// 计算噪声UV动画
				float2 appendResult22 = float2(_SpeedMainTexUVNoiseZW.z, _SpeedMainTexUVNoiseZW.w);
				float2 uv0_NormalMap = input.uv * _NormalMap_ST.xy + _NormalMap_ST.zw;
				float2 panner146 = 1.0 * _Time.y * appendResult22 + uv0_NormalMap;
				
				// 采样法线贴图并应用扭曲
				float4 normalMapSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, panner146);
				float3 normalDistortion = UnpackScaleNormal(normalMapSample, _Distortionpower);
				
				// 计算屏幕空间UV（使用OpaqueTexture代替GrabPass）
				float2 screenUV = CalculateScreenSpaceUV(input.positionNDC);
				float2 distortedScreenUV = screenUV + normalDistortion.xy;
				
				// 采样不透明纹理（URP中代替GrabPass）
				float3 screenColor = SampleSceneColor(distortedScreenUV).rgb;
				float3 temp_output_128_0 = screenColor;
				
				// 计算主纹理UV
				float2 appendResult21 = float2(_SpeedMainTexUVNoiseZW.x, _SpeedMainTexUVNoiseZW.y);
				float2 uv0_MainTex = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 panner107 = 1.0 * _Time.y * appendResult21 + uv0_MainTex;
				
				// 计算Flow纹理
				float2 appendResult100 = float2(_DistortionSpeedXYPowerZ.x, _DistortionSpeedXYPowerZ.y);
				float4 uv0_Flow = input.uv_tex4coord;
				uv0_Flow.xy = input.uv_tex4coord.xy * _Flow_ST.xy + _Flow_ST.zw;
				float2 panner110 = 1.0 * _Time.y * appendResult100 + uv0_Flow.xy;
				
				// 计算Mask
				float2 uv_Mask = input.uv * _Mask_ST.xy + _Mask_ST.zw;
				float Flowpower102 = _DistortionSpeedXYPowerZ.z;
				
				// 计算主纹理UV（应用Flow扭曲）
				float4 flowSample = SAMPLE_TEXTURE2D(_Flow, sampler_Flow, panner110);
				float4 maskSample = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv_Mask);
				float2 mainTexUV = panner107 - ((flowSample * maskSample) * Flowpower102).rg;
				float4 tex2DNode13 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainTexUV);
				
				// 计算噪声
				float2 uv0_Noise = input.uv * _Noise_ST.xy + _Noise_ST.zw;
				float2 panner108 = 1.0 * _Time.y * appendResult22 + uv0_Noise;
				float2 appendResult160 = float2(uv0_Flow.w, 0.0);
				float4 tex2DNode14 = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, panner108 + appendResult160);
				
				// 计算Alpha
				float temp_output_88_0 = (tex2DNode13.a * tex2DNode14.a * _Color.a * input.vertexColor.a * _Opacity);
				
				// 计算发射颜色
				float3 temp_output_140_0 = ((tex2DNode13 * tex2DNode14 * _Color * input.vertexColor) * _Emission * temp_output_88_0).rgb;
				
				// 混合屏幕颜色和发射颜色
				float W158 = uv0_Flow.z;
				float3 lerpResult157 = lerp((temp_output_128_0 + temp_output_140_0), (temp_output_128_0 * temp_output_140_0), W158);
				
				// 计算深度衰减
				float temp_output_151_0 = saturate(temp_output_88_0);
				float2 screenPosNorm = screenUV;
				float rawDepth = SampleSceneDepth(screenPosNorm);
				float sceneZ = LinearEyeDepth(rawDepth, _ZBufferParams);
				float viewZ = -LinearEyeDepth(input.positionNDC.z, UNITY_MATRIX_P);
				float distanceDepth = abs((sceneZ - viewZ) / _Depthpower);
				float depthAlpha = saturate(distanceDepth);
				
				// 计算软边
				float3 ase_worldNormal = normalize(input.normalWS);
				float3 viewDir = normalize(input.viewDirWS);
				float dotResult163 = dot(ase_worldNormal, viewDir);
				float temp_output_185_0 = pow(dotResult163, 3.0) * 5.0;
				float dotResult171 = dot(ase_worldNormal, viewDir);
				float remap178 = (0.0 + (temp_output_185_0 - 0.0) * (1.0 - 0.0) / (-1.0 - 0.0));
				float remap184 = (1.0 + (sign(dotResult171) - -1.0) * (0.0 - 1.0) / (1.0 - -1.0));
				float lerpResult181 = lerp(temp_output_185_0, remap178, remap184);
				float clampResult186 = clamp(lerpResult181, 0.0, 1.0);
				
				// 计算最终Alpha
				float baseAlpha = lerp(temp_output_151_0, (temp_output_151_0 * depthAlpha), _Usedepth);
				float finalAlpha = lerp(baseAlpha, (baseAlpha * clampResult186), _Softedges);
				
				half4 color;
				color.rgb = lerpResult157;
				color.a = finalAlpha;
				
				return color;
			}
			ENDHLSL
		}
	}
}