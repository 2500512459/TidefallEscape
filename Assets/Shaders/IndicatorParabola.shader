Shader"Tidefall/IndicatorParabola"
{
    Properties
    {
        //0~180
        //_LaunchAngle("Launch Angle", Range(0, 180)) = 45
        //_LaunchVelocity("Launch Velocity", Range(0, 100)) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha One

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define G 9.8

            //float _LaunchAngle;
            float _LaunchVelocity;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
    
                float _LaunchAngle = 45;
                float InitSpeed = _LaunchVelocity;
    
                float angleInRadians = _LaunchAngle * (PI / 180.0);
                float3 velocity = float3(0, sin(angleInRadians), cos(angleInRadians));
    
                float timeToHitGround = (2.0 * InitSpeed * velocity.y) / G;

                float time = IN.positionOS.z * timeToHitGround;
                float3 positionOS = IN.positionOS.xyz;
                float3 parabolicPosition;
                parabolicPosition.xz = positionOS.xz;
                parabolicPosition.y = InitSpeed * velocity.y * time - 0.5 * G * time * time;
    
                OUT.positionCS = TransformObjectToHClip(parabolicPosition);
    
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(1, 1, 1, 0.8);
            }
            ENDHLSL
        }
    }
    FallBack"Universal Render Pipeline/FallbackError"
}