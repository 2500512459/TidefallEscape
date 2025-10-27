Shader"TechFusion/TidefallEscape/AdditionalWave"
{
	Properties
	{
		_BaseMap("Wave Shape", 2D) = "white" {}

		// Blend mode values
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend mode", Float) = 0.0
		// Blend mode values
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend mode", Float) = 0.0
		// Will set "_INVERT_ON" shader keyword when set
		[Toggle] _Invert("Invert?", Float) = 0
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
		//Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
		ZWrite Off
		ZTest Off
		Blend[_SrcBlend][_DstBlend]
		Cull Off
		LOD 100

		Pass
		{
			Name "Additional Wave"
			Tags{"LightMode" = "Additional Displacement"}
			HLSLPROGRAM
			#pragma vertex AdditionalVertex
			#pragma fragment AdditionalFragment
			#pragma shader_feature _INVERT_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float4 position : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv : TEXCOORD0;
				float4 color : TEXCOORD1;
				float4 positionCS : SV_POSITION;
};

			struct FragmentOutput
			{
				half4 displacement : SV_Target0;
			};

			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

			Varyings AdditionalVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
	
				output.uv = input.texcoord;
				output.color = input.color;
				output.positionCS = TransformObjectToHClip(input.position.xyz);

				return output;
			}

			FragmentOutput AdditionalFragment(Varyings input)
			{
				FragmentOutput output;

				float2 uv = input.uv;
	
				half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv)/* * input.color.x*/ * 0.1;
				output.displacement = texColor;

				return output;
			}
			ENDHLSL
		}
	}
}
