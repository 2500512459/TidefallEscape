#ifndef SAMPLING_INCLUDED
#define SAMPLING_INCLUDED

#include "Material.hlsl"

float3 SampleDisplacement(float2 worldUV, float3 worldUVDeviders, float3 lods)
{
	float3 displacement = 0;
	displacement += _Displacement_c0.SampleLevel(sampler_Displacement_c0, float3(worldUV / worldUVDeviders.x, 0), 0) * lods.x;
#if defined(MID) || defined(CLOSE)
	displacement += _Displacement_c1.SampleLevel(sampler_Displacement_c1, float3(worldUV / worldUVDeviders.y, 0), 0) * lods.y;
#endif
#if defined(CLOSE)
	displacement += _Displacement_c2.SampleLevel(sampler_Displacement_c2, float3(worldUV / worldUVDeviders.z, 0), 0) * lods.z;
#endif
	return displacement;
}

float3 SampleNormal(float2 worldUV, float3 worldUVDeviders, float3 lods)
{
	float4 derivatives = _Derivatives_c0.SampleLevel(sampler_Derivatives_c0, worldUV / worldUVDeviders.x, 0) * lods.x;
#if defined(MID) || defined(CLOSE)
	derivatives += _Derivatives_c1.SampleLevel(sampler_Derivatives_c0, worldUV / worldUVDeviders.y, 0) * lods.y;
#endif

#if defined(CLOSE)
	derivatives += _Derivatives_c2.SampleLevel(sampler_Derivatives_c2, worldUV / worldUVDeviders.z, 0) * lods.z;
#endif

	float2 slope = float2(derivatives.x / (1 + derivatives.z),
		derivatives.y / (1 + derivatives.w));
	float3 worldNormal = normalize(float3(-slope.x, 1, -slope.y));
	return worldNormal;
}

float SampleFoamCoverage(float2 worldUV, float3 worldUVDeviders, float3 foamLODScales)
{
	float _FoamBiasLOD0 = 0.44;
	float _FoamBiasLOD1 = 0.4;
	float _FoamBiasLOD2 = 1.5; // 2.72

	float coverage = 0;
#if defined(CLOSE)
	float jacobian = _Turbulence_c0.SampleLevel(sampler_Turbulence_c0, worldUV / worldUVDeviders.x, 0).x * foamLODScales.x
		+ _Turbulence_c1.SampleLevel(sampler_Turbulence_c1, worldUV / worldUVDeviders.y, 0).x * foamLODScales.y
		+ _Turbulence_c2.SampleLevel(sampler_Turbulence_c2, worldUV / worldUVDeviders.z, 0).x * foamLODScales.z;
	coverage = min(1, max(0, (-jacobian + _FoamBiasLOD2) * _FoamScale));
	
	//coverage = 0;//todo test
#elif defined(MID)
	float jacobian = /*_Turbulence_c0.SampleLevel(sampler_Turbulence_c0, worldUV / worldUVDeviders.x, 0).x * foamLODScales.x
		+ */_Turbulence_c1.SampleLevel(sampler_Turbulence_c1, worldUV / worldUVDeviders.y, 0).x * foamLODScales.y;
	coverage = min(1, max(0, (-jacobian + _FoamBiasLOD1) * _FoamScale));
#else
	float jacobian = _Turbulence_c0.SampleLevel(sampler_Turbulence_c0, worldUV / worldUVDeviders.x, 0).x * foamLODScales.x;
	coverage = min(1, max(0, (-jacobian + _FoamBiasLOD0) * _FoamScale));
#endif
	return coverage;
}
#endif // SAMPLING_INCLUDED