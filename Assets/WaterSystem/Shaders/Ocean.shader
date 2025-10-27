Shader "TechFusion/TidefallEscape/Ocean"
{
    Properties
    {
        _FoamAlbedo("Foam", 2D) = "white" {}
		_FoamBubble("Bubble", 2D) = "white" {}
		_NormalBase("Normal Base", 2D) = "white" {}
		_NormalDetail("Normal Detail", 2D) = "white" {}
        _ContactFoamTexture("Contact Foam", 2D) = "white" {}
		_CausticsTexture("Caustics", 2D) = "white" {}
    }

    SubShader
    {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull [_Cull]
			ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex OceanMainVert
            #pragma fragment OceanMainFrag

			#pragma multi_compile_fog
			#pragma multi_compile _ MID CLOSE
            
			#define _REFLECTION_PLANARREFLECTION
			#define _MAIN_LIGHT_SHADOWS
			#define _MAIN_LIGHT_SHADOWS_CASCADE

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float viewDepth     : TEXCOORD1;
                float4 positionNDC  : TEXCOORD2;
                float2 worldUV      : TEXCOORD3;
                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                float4 shadowCoord  : TEXCOORD4;
                #endif
				float4 additionalData: TEXCOORD5;	// x = distance to surface, y = distance to surface
				//float4 lodScales	: TEXCOORD6;
				float3 worldNormal : TEXCOORD7;
};

			#pragma multi_compile _PRID_ONE _PRID_TWO _PRID_THREE _PRID_FOUR
			#include "PlanarReflections.hlsl"

			//#include "Sampling.hlsl"
			#include "Material.hlsl"
			#include "GerstnerWaves.hlsl"
			#include "Cascade.hlsl"
			#include "Foam.hlsl"
			#include "Surface.hlsl"

			

            Varyings OceanMainVert(Attributes input)
            {
                Varyings output;

				output.positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;
				output.worldUV = output.positionWS.xz;

				float3 viewVector = output.positionWS - _WorldSpaceCameraPos;
				float viewDist = length(viewVector);

				float3 displacement = 0;
				

				half4 screenUV = ComputeScreenPos(TransformWorldToHClip(output.positionWS));
				screenUV.xyz /= screenUV.w;
				
				float3 normal;

				//Gerstner
				WaveStruct wave;
				SampleWaves(output.positionWS, 1, wave);
				displacement += wave.position;
				output.worldNormal = wave.normal;
	
				//Todo Dynamic displacement
				displacement += SampleWaveDisplacement(output.worldUV).xyz;
				output.positionWS += displacement;

				//output.lodScales = float4(foam, 1, 1, 1);
	
				float3 positionOS = TransformWorldToObject(output.positionWS + displacement);
				VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
				output.viewDepth = -positionInputs.positionVS.z;
				output.positionNDC = positionInputs.positionNDC;
				output.positionHCS = positionInputs.positionCS;
    
#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
				output.shadowCoord = GetShadowCoord(positionInputs);
#endif
				output.additionalData = AdditionalData(output.positionWS, displacement.y);
				return output;
            }

			float2 GetWaves(float2 coords, half scale, half amplitude, half speed, float2 dir) {
				float time = _Time.y * speed * 2;
    
				float2 offsetCoords = (coords + dir * time) / scale;
    
				float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalBase, sampler_NormalBase, offsetCoords));
    
				return normal.xy * amplitude * 0.5;
			}

			half4 OceanMainFrag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
			{
				half2 screenUV = input.positionNDC.xy / input.positionNDC.w;
				bool isUnderwater = !isFrontFace;
	
				float3 worldNormal = input.worldNormal;
	
				// multiple overlapping waves
				float3 WaveScale = float3(20.18, 11.8, 8.63);
				float3 WaveAmplitude = float3(0.5, 0.35, 0.3);
				float3 WaveSpeed = float3(0.81, 0.75, 0.5);
				float2 nrm = GetWaves(input.worldUV, WaveScale.x, WaveAmplitude.x, WaveSpeed.x, half2(1,0.5));
				nrm += GetWaves(input.worldUV, WaveScale.y, WaveAmplitude.y, WaveSpeed.y, half2(-1,-0.5));
				nrm += GetWaves(input.worldUV, WaveScale.z, WaveAmplitude.z, WaveSpeed.z, half2(0.5,-1));

				nrm /= 3;

				worldNormal += SampleWaveNormal(input.worldUV, 0).xyz;
				worldNormal = normalize(worldNormal);

				worldNormal.xz += nrm;
				worldNormal = normalize(worldNormal);


				float3 foamLODScales = 1;//todo

				float3 viewDir = _WorldSpaceCameraPos - input.positionWS;
				float viewDist = length(viewDir);
				viewDir = viewDir / viewDist;
	
				float coverage = ContactFoam(input.worldUV, screenUV, input.viewDepth);
				
				coverage += WaveFoamCoverage(input.additionalData.z, worldNormal);
	
				coverage = saturate(coverage);
				
				float normalFadeFactor = 0;//saturate(viewDist / _NormalFadeFar);
				float3 normal = worldNormal;//lerp(worldNormal, float3(0, 1, 0), normalFadeFactor);
				
				float foamFadeFactor = saturate(viewDist / _FoamFadeFar);
				coverage = lerp(coverage, 0, foamFadeFactor);
				half4 foam = float4(GetFoamAlbedo(input.worldUV, coverage), 1);
				float4 oceanColor = WaterShading(screenUV, input.positionWS, 
#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
									input.shadowCoord,
#endif
									normal, viewDir, input.additionalData, foam, isUnderwater);

				// Fog
				float viewZ = input.viewDepth;
				if (!isUnderwater)
				{
					//todo : atmospheric fog
#if (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
					float nearToFarZ = max(viewZ - _ProjectionParams.y, 0);
					half fogFactor = ComputeFogFactorZ0ToFar(nearToFarZ);
					#else
					half fogFactor = 0;
					#endif

					oceanColor.rgb = MixFog(oceanColor.rgb, fogFactor);
				}
				else
				{
					// underwater - do depth fog
					//oceanColor.rgb = lerp(oceanColor.rgb, Ocean_DeepScatterColor, saturate(1. - exp(-Ocean_FogDensity * viewZ)));
				}

				return oceanColor;
			}
            ENDHLSL
        }
    }
}