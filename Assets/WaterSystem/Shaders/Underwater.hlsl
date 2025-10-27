#if !defined(UNDERWATER_INCLUDED)
#define UNDERWATER_INCLUDED

half3 ApplyCaustics(half2 screenUV)
{
    float2 UV = screenUV;

    // Sample the depth from the Camera depth texture.
#if UNITY_REVERSED_Z
    real depth = SampleSceneDepth(UV);
#else
    // Adjust Z to match NDC for OpenGL ([-1, 1])
    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
#endif

    // Reconstruct the world space positions.
    float3 worldPosition = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);
    
    //shadow
    float backGroundShadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(worldPosition));
    
    float2 depthRange = float2(-50, -1);
    float decayRange = 5;
    
    float worldY = worldPosition.y;
    
    //caustics intensity
    float distanceToMin = abs(worldY - depthRange.x);
    float distanceToMax = abs(worldY - depthRange.y);
    
    float intensity = (worldY > depthRange.x && worldY < depthRange.y) ? saturate(min(distanceToMin, distanceToMax) / decayRange) : 0.0;
    intensity *= backGroundShadow;
    float scale = lerp(1, 0.2, saturate(-worldY / 500.0));
    
    float4 causticsUV_ST1 = float4(0.1, 0.1, 0.2, 0);
    float4 causticsUV_ST2 = float4(0.1, 0.1, 0, 0.3);
    
    float causticsSpeed1 = 0.08;
    float causticsSpeed2 = 0.03;
    
    float2 causticsUV1 = worldPosition.xz * scale * causticsUV_ST1.xy + causticsUV_ST1.zw;
    causticsUV1 += causticsSpeed1 * _Time.y;
    
    float2 causticsUV2 = worldPosition.xz * scale * causticsUV_ST2.xy + causticsUV_ST2.zw;
    causticsUV2 += causticsSpeed2 * _Time.y;
    
    float4 causticsColor1 = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, causticsUV1);
    float4 causticsColor2 = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, causticsUV2);
    
    return min(causticsColor1.xyz, causticsColor2.xyz) * intensity * 5;
}

#endif