Shader "Tidefall/Trajectory"
{
    Properties
    {
        _MainCol("Color", Color) = (1,1,1,0.8)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" }  // ��Ⱦ������Ϊ͸��
        LOD 100                         // ϸ�ڼ���
        Cull Off                        // �رձ����޳�
        ZWrite Off                      // �ر����д��
        ZTest Always                    // ��Ȳ�������ͨ��
        Blend SrcAlpha One             // ���ģʽ��ԴAlpha * 1���ӷ���ϣ�

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define G 9.8

            float _LaunchVelocity;

            half4 _MainCol;

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
            
                float _LaunchAngle = 45;  // ����Ƕȹ̶�Ϊ45��
                float InitSpeed = _LaunchVelocity;  // ���ⲿ��ȡ�ķ����ٶ�
            
                // ���Ƕ�ת��Ϊ����
                float angleInRadians = _LaunchAngle * (PI / 180.0);
                // �����ٶ����������跢�䷽����yzƽ�棬yΪ��ֱ����zΪˮƽ����
                float3 velocity = float3(0, sin(angleInRadians), cos(angleInRadians)) * InitSpeed;
            
                // �������ʱ�䣺������ֱ�����˶����������½�ʱ����ȣ���ʱ��Ϊ2 * vy / g
                float timeToHitGround = (2.0 * velocity.y) / G;
            
                // ʹ�ö���ԭʼ��z������Ϊʱ�����ӣ�0��1����ģ��켣�ϵĵ�
                float time = IN.positionOS.z * timeToHitGround;
            
                // ����������λ��
                float3 parabolicPosition;
                parabolicPosition.xz = IN.positionOS.xz;  // x��zʹ��ԭʼ�����x��z�����ڵ����켣�Ŀ��Ⱥ���״��
                // y��������������˶���ʽ��y = vy * t - 0.5 * g * t^2
                parabolicPosition.y = velocity.y * time - 0.5 * G * time * time;
            
                // ������ռ�����ת������βü��ռ�
                OUT.positionCS = TransformObjectToHClip(parabolicPosition);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _MainCol;// ֱ�ӷ��ض������ɫ
            }
            ENDHLSL
        }
    }
    FallBack"Universal Render Pipeline/FallbackError"
}