#if !defined(MATERIAL_INCLUDED)
#define MATERIAL_INCLUDED

TEXTURE2D(_FoamAlbedo);
SAMPLER(sampler_FoamAlbedo);
//TEXTURE2D(_FoamBubble);
//SAMPLER(sampler_FoamBubble);
TEXTURE2D(_ContactFoamTexture);
SAMPLER(sampler_ContactFoamTexture);

TEXTURE2D(_CausticsTexture);
SAMPLER(sampler_CausticsTexture);

// Surface textures
TEXTURE2D(_AbsorptionScatteringRamp); 
SAMPLER(sampler_AbsorptionScatteringRamp);

//wave cascades
SAMPLER(sampler_linear_clamp);

TEXTURE2D(_NormalBase); 
SAMPLER(sampler_NormalBase);
TEXTURE2D(_NormalDetail); 
SAMPLER(sampler_NormalDetail);

CBUFFER_START(UnityPerMaterial)
//scale
float _GeometryScale;

float _LOD_scale;
float _SSSBase;
float _SSSScale;
float _SSSStrength;

// foam
//float _FoamBiasLOD0;
//float _FoamBiasLOD1;
//float _FoamBiasLOD2;
float _FoamScale;
float _ContactFoam;

//reflection
float _ReflectionStrength;

// depth
half _MaxDepth;

//fade
float _NormalFadeFar;
float _FoamFadeFar;
CBUFFER_END

#endif