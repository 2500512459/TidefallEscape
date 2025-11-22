Shader "Hovl/Particles/VolumeLaser"
{
	Properties
	{
		[HideInInspector]_StartPoint("StartPoint", Vector) = (0,1,0,0)
		_StartDistance("Start Distance", Float) = 2
		_StartRound("Start Round", Float) = 6
		[Toggle]_UseEndRound("Use End Round", Float) = 1
		[HideInInspector]_EndPoint("EndPoint", Vector) = (-10,1,0,0)
		_EndDistance("End Distance", Float) = 2
		_EndRound("End Round", Float) = 6
		_Distance("Distance", Float) = 10
		_MainTex("MainTex", 2D) = "white" {}
		_DissolveNoise("Dissolve Noise", 2D) = "white" {}
		_MainTexTilingXYNoiseTilingZW("MainTex Tiling XY Noise Tiling ZW", Vector) = (1,1,1,1)
		_SpeedMainTexUVNoiseZW("Speed MainTex U/V + Noise Z/W", Vector) = (0,0,0,0)
		_Emission("Emission", Float) = 2
		_Color("Color", Color) = (1,1,1,1)
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Dissolve("Dissolve", Range( 0 , 1)) = 1
		_VertexPower("Vertex Power", Float) = 0.3
		_TextureVertexPower("Texture Vertex Power", Float) = 0.2
		[HideInInspector]_Scale("Scale", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
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
				float4 vertexColor : COLOR;
			};

			struct Varyings
			{
				float4 positionHCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float2 uv : TEXCOORD1;
				float4 vertexColor : COLOR;
			};

			CBUFFER_START(UnityPerMaterial)
				float4 _StartPoint;
				float _StartDistance;
				float _StartRound;
				float _UseEndRound;
				float4 _EndPoint;
				float _EndDistance;
				float _EndRound;
				float4 _MainTex_ST;
				float4 _MainTexTilingXYNoiseTilingZW;
				float4 _SpeedMainTexUVNoiseZW;
				float _Emission;
				float4 _Color;
				float _Cutoff;
				float _Dissolve;
				float _VertexPower;
				float _TextureVertexPower;
				float _Scale;
				float _Distance;
			CBUFFER_END

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_DissolveNoise);
			SAMPLER(sampler_DissolveNoise);

			Varyings vert(Attributes input)
			{
				Varyings output;
				
				// 计算世界空间位置和法线
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
				
				// 计算距离（与原代码完全一致的逻辑）
				float temp_output_3_0 = distance(_StartPoint, float4(positionWS, 0.0));
				float StartPoint83 = temp_output_3_0;
				
				// TFHCRemapNode: 将[0,1]映射到[0,-1]，然后加上_StartDistance
				// 原公式: (0.0 + (StartPoint83 - 0.0) * (-1.0 - 0.0) / (1.0 - 0.0)) + _StartDistance
				// 简化: -StartPoint83 + _StartDistance (假设StartPoint83在[0,1]范围内)
				float remap37 = (0.0 + (StartPoint83 - 0.0) * (-1.0 - 0.0) / (1.0 - 0.0));
				float clampResult10 = clamp(remap37 + _StartDistance, 0.0, _StartDistance);
				
				// 计算结束点距离
				float myVarName106 = distance(float4(positionWS, 0.0), _EndPoint);
				// TFHCRemapNode: 将[0,1]映射到[0,-1]
				float remap107 = (0.0 + (myVarName106 - 0.0) * (-1.0 - 0.0) / (1.0 - 0.0));
				float clampResult109 = clamp(remap107 + _EndDistance, 0.0, _EndDistance);
				
				// 重新映射到[0,1]范围用于pow计算
				float remap11 = (0.0 + (clampResult10 - 0.0) * (1.0 - 0.0) / (_StartDistance - 0.0));
				float remap110 = (0.0 + (clampResult109 - 0.0) * (1.0 - 0.0) / (_EndDistance - 0.0));
				
				// 计算圆度
				float startRoundFactor = pow(remap11, _StartRound);
				float endRoundFactor = pow(remap110, _EndRound);
				float temp_output_15_0 = max(startRoundFactor, lerp(0.0, endRoundFactor, _UseEndRound));
				
				// 计算UV动画（用于顶点偏移的纹理采样）
				float2 appendResult46 = float2(_MainTexTilingXYNoiseTilingZW.x, _MainTexTilingXYNoiseTilingZW.y);
				float2 appendResult57 = float2(_SpeedMainTexUVNoiseZW.x, _SpeedMainTexUVNoiseZW.y);
				float2 appendResult40 = float2(input.uv.x, temp_output_3_0);
				float2 panner48 = 1.0 * _Time.y * appendResult57 + appendResult40;
				
				// 使用lod采样主纹理
				float4 tex2DNode32 = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, appendResult46 * panner48, 0.0);
				
				// 计算顶点偏移
				float vertexOffset = (1.0 - (temp_output_15_0 + (tex2DNode32.r * (1.0 - temp_output_15_0) * _TextureVertexPower))) * 2.0;
				float3 offset = vertexOffset * normalWS * _VertexPower * _Scale;
				
				// 应用顶点偏移
				positionWS += offset;
				
				output.positionHCS = TransformWorldToHClip(positionWS);
				output.positionWS = positionWS;
				output.uv = input.uv;
				output.vertexColor = input.vertexColor;
				
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				// 计算世界空间位置距离
				float3 positionWS = input.positionWS;
				float temp_output_3_0 = distance(_StartPoint, float4(positionWS, 0.0));
				
				// 计算UV
				float2 appendResult46 = float2(_MainTexTilingXYNoiseTilingZW.x, _MainTexTilingXYNoiseTilingZW.y);
				float2 appendResult57 = float2(_SpeedMainTexUVNoiseZW.x, _SpeedMainTexUVNoiseZW.y);
				float2 appendResult40 = float2(input.uv.x, temp_output_3_0);
				float2 panner48 = 1.0 * _Time.y * appendResult57 + appendResult40;
				
				// 采样主纹理
				float4 tex2DNode32 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, appendResult46 * panner48);
				
				// 计算发射颜色
				half3 emission = (_Color.rgb * input.vertexColor.rgb * _Emission * tex2DNode32.rgb);
				
				// 计算溶解
				float StartPoint83 = temp_output_3_0;
				float ifLocalVar82 = (StartPoint83 >= _Distance) ? 0.0 : 1.0;
				
				// 计算溶解噪声
				float2 appendResult94 = float2(_MainTexTilingXYNoiseTilingZW.z, _MainTexTilingXYNoiseTilingZW.w);
				float2 appendResult122 = float2(_SpeedMainTexUVNoiseZW.z, _SpeedMainTexUVNoiseZW.w);
				float2 panner123 = 1.0 * _Time.y * appendResult122 + appendResult40;
				float dissolveNoise = SAMPLE_TEXTURE2D(_DissolveNoise, sampler_DissolveNoise, appendResult94 * panner123).r;
				float clampResult101 = clamp(dissolveNoise + 0.05, 0.0, 1.0);
				
				// 计算alpha裁剪（与原代码完全一致）
				// TFHCRemapNode: 将_Dissolve从[0,1]映射到[1.0, 0.49]，使用clampResult101作为除法因子
				// 原公式: (1.0 + (_Dissolve - 0.0) * (0.49 - 1.0) / (clampResult101 - 0.0))
				float remap103 = 1.0 + (_Dissolve - 0.0) * (0.49 - 1.0) / (clampResult101 - 0.0);
				float clipValue = ifLocalVar82 * remap103;
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