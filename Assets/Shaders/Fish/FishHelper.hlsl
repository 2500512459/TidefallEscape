
void GetAnimPosition_float(float3 position, out float3 animPos)
{
    animPos = position;
    //animPos += ((sin(((_Time.w * _AnimationSpeed)
    //                            + (position.z * _Yaw)
    //                            + (position.y * _Roll))) * _Scale)
    //                            * float3(1, 0, 0));

    animPos += sin(_Time.w * _AnimationSpeed + position.z * _Yaw + position.y * _Roll) * _Scale * float3(1, 0, 0);
}