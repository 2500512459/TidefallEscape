#ifndef SURFACE_INCLUDED
#define SURFACE_INCLUDED

#define SHADOWS_SCREEN 0

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

// Screen Effects textures

SAMPLER(sampler_ScreenTextures_linear_clamp);
SAMPLER(sampler_ScreenTextures_point_clamp);
#if defined(_REFLECTION_PLANARREFLECTION)
TEXTURE2D(_PlanarReflectionTexture);
#endif

SAMPLER(sampler_CameraOpaqueTexture_linear_clamp);

#include "Volume.hlsl"
#include "Lighting.hlsl"
#include "Underwater.hlsl"


float AdjustedDepth(half2 uvs, half4 additionalData)
{
    float rawD = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_ScreenTextures_point_clamp, uvs);
	float d = LinearEyeDepth(rawD, _ZBufferParams);
	
    return d * additionalData.x - additionalData.y;
}

float2 WaterDepth(float3 posWS, half4 additionalData, half2 screenUVs)// x = seafloor depth, y = water depth
{
	float2 outDepth = 0;
	outDepth.x = AdjustedDepth(screenUVs, additionalData);
	float wd = 0;// WaterTextureDepth(posWS);
	outDepth.y = wd + posWS.y;
	return outDepth;
}

float WaterDepthLinear(float3 posWS, float surfaceDistance, half2 screenUVs)
{
	float rawD = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_ScreenTextures_linear_clamp, screenUVs);
	float d = LinearEyeDepth(rawD, _ZBufferParams);
	return max(d - surfaceDistance, 0);
}

float3 Refraction(bool isUnderwater, float2 distortion, float depth, float depthMulti)
{
    float3 output = SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture_linear_clamp, distortion, 0 /*depth * 0.25*/).rgb;
    
    if (!isUnderwater)
    {
        output += ApplyCaustics(distortion);
        output *= Absorption((depth) * depthMulti);   
    }
    
	return output;
}

float2 DistortionUVs(float depth, float distance, float3 normalWS)
{
    half3 viewNormal = mul((float3x3)GetWorldToHClipMatrix(), -normalWS).xyz;
    float distortRate = 0.1 * saturate(1 - distance / 50);
    return viewNormal.xz * saturate((depth) * 0.2) * distortRate; // * saturate(depth * 0.0001) * saturate(50.0 / distance - 0.2);
}

half4 AdditionalData(float3 postionWS, float SSS)
{
    half4 data = half4(0.0, 0.0, 0.0, 0.0);
    float3 viewPos = TransformWorldToView(postionWS);
	data.x = length(viewPos / viewPos.z);// distance to surface
    data.y = length(GetCameraPositionWS().xyz - postionWS); // local position in camera space
	data.z = SSS;
	return data;
}

// Fragment for water
half4 WaterShading(half2 screenUV, float3 positionWS, 
#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
float4 shadowCoord,
#endif
float3 normal, float3 viewDir, half4 additionalData, half4 foam, bool isUnderwater)
{
	// Depth
	float2 depth = WaterDepth(positionWS, additionalData, screenUV);
	half depthMulti = 1 / _MaxDepth;
	//half waterDepthView = WaterDepthLinear(positionWS, additionalData.y, screenUV);

	normal = normalize(normal);

	if (isUnderwater) normal = -normal;

    // Distortion
    half2 distortionDelta = DistortionUVs(depth.x, additionalData.y, normal);
    half2 distortion;
    distortion = screenUV + distortionDelta;
    float d = depth.x;
    depth.x = AdjustedDepth(distortion, additionalData);
    
    distortion = depth.x < 0 ? screenUV : screenUV + distortionDelta;
    depth.x = depth.x < 0 ? d : depth.x;
    
    
    // Fresnel
        half fresnelTerm = CalculateFresnelTerm(normal, viewDir);

	// Lighting
//#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
//	Light mainLight = GetMainLight(shadowCoord);
//#else
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
//#endif
    //half shadow = SoftShadows(float3(screenUV, 0), positionWS, viewDir, depth.x);
    half shadow = lerp(0.5, 1, mainLight.shadowAttenuation);
    half3 GI = SampleSH(normal);

    BRDFData brdfData;
    half alpha = 1;
    InitializeBRDFData(half3(0, 0, 0), 0, half3(1, 1, 1), 0.98, alpha, brdfData);// * saturate(1 - additionalData.y / 10000)
    float3 spec = (DirectBRDF(brdfData, normal, mainLight.direction, viewDir) - brdfData.diffuse) * shadow * mainLight.color;

    // SSS
    half3 directLighting = brdfData.diffuse * mainLight.color;
	//towards sun
	//wave height sss
    float sssWaveFactor = max((additionalData.z * _SSSScale + _SSSBase), 0) * _SSSStrength * 2;
    directLighting += saturate(pow(saturate(dot(viewDir, -mainLight.direction)), 3)) * sssWaveFactor * mainLight.color;
    half3 sss = directLighting * shadow + GI;

    
#ifdef _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, positionWS);
        spec += LightingPhysicallyBased(brdfData, light, normal, viewDir);
        sss += light.distanceAttenuation * light.color;
    }
#endif

    sss *= Scattering(depth.x * depthMulti);

	// Reflections
    half3 reflection = SampleReflections(isUnderwater, normal, viewDir, distortion, 0.5) * _ReflectionStrength;

	// Refraction
    half3 refraction = Refraction(isUnderwater, distortion, depth.x, depthMulti);

    half3 waterColor = lerp(refraction, reflection, fresnelTerm) + sss + spec + /*saturate*/(foam.xyz);
    
    //shallow fade
    //waterColor = lerp(refraction, waterColor, saturate(depth.x / 5));
    
	// Do compositing
    //float3 comp = waterColor;
    
    return float4(waterColor, 1);
}

#endif // SURFACE_INCLUDED