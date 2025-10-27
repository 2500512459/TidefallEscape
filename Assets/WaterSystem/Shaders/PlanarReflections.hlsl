#ifndef PLANAR_REFLECTIONS_INCLUDED
#define PLANAR_REFLECTIONS_INCLUDED

#ifdef _PRID_ONE
    TEXTURE2D(_PlanarReflectionsTex1);
    SAMPLER(sampler_PlanarReflectionsTex1);
    half4 SamplePlanarReflections(float2 screenUV)
    {
        float2 uv = screenUV;
        uv.x = 1 - uv.x;
        return SAMPLE_TEXTURE2D_LOD(_PlanarReflectionsTex1, sampler_PlanarReflectionsTex1, uv, 1);
    }
#elif _PRID_TWO
    TEXTURE2D(_PlanarReflectionsTex2);
    SAMPLER(sampler_PlanarReflectionsTex2);
    half4 SamplePlanarReflections(float2 screenUV)
    {
        float2 uv = screenUV;
        uv.x = 1 - uv.x;
        return SAMPLE_TEXTURE2D(_PlanarReflectionsTex2, sampler_PlanarReflectionsTex2, uv);
    }
#elif _PRID_THREE
    TEXTURE2D(_PlanarReflectionsTex3);
    SAMPLER(sampler_PlanarReflectionsTex3);
    half4 SamplePlanarReflections(float2 screenUV)
    {
        float2 uv = screenUV;
        uv.x = 1 - uv.x;
        return SAMPLE_TEXTURE2D(_PlanarReflectionsTex3, sampler_PlanarReflectionsTex3, uv);
    }
#elif _PRID_FOUR
    TEXTURE2D(_PlanarReflectionsTex4);
    SAMPLER(sampler_PlanarReflectionsTex4);
    half4 SamplePlanarReflections(float2 screenUV)
    {
        float2 uv = screenUV;
        uv.x = 1 - uv.x;
        return SAMPLE_TEXTURE2D(_PlanarReflectionsTex4, sampler_PlanarReflectionsTex4, uv);
    }
#endif

#endif // PLANAR_REFLECTIONS_INCLUDED