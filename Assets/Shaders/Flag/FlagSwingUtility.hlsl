void SwingPosition_float(float3 position, float swing, out float3 result)
{
    // 旗帜摆动：基于x和y位置创建波浪效果
    half sinOffsetX = position.x * _FlagSwingFrequence;
    half sinOffsetY = position.y * _FlagSwingFrequence * 0.5; // 垂直方向的频率稍低
    half t = _Time.x * _FlagSwingSpeed;
    
    // 主要摆动方向（z轴，垂直于旗帜面）
    half swingZ = sin(t + sinOffsetX + sinOffsetY) * _FlagSwingAmplitude * swing;
    
    // 添加一些x方向的轻微摆动，让旗帜更自然
    half swingX = sin(t * 0.7 + sinOffsetX * 1.2) * _FlagSwingAmplitude * swing * 0.3;
    
    position.z += swingZ;
    position.x += swingX;
    result = position;
}
void SwingPosition_half(float3 position, float swing, out float3 result)
{
    SwingPosition_float(position, swing, result);
}
void SwingStrength_float(float2 uv, out float strength)
{
    // 旗帜效果：左侧固定（uv.x = 0），右侧自由摆动（uv.x = 1）
    // 使用uv.x，让左侧强度为0（固定），右侧强度为1（完全摆动）
    float horizontalStrength = uv.x;
    
    // 可选：添加一些垂直方向的衰减，让上下边缘稍微固定一些
    float verticalDist = abs(uv.y - 0.5) * 2.0; // 0到1，中心为0，边缘为1
    float verticalAttenuation = 1.0 - verticalDist * 0.2; // 边缘衰减20%
    
    strength = saturate(horizontalStrength * verticalAttenuation);
}
void SwingStrength_half(float2 uv, out half strength)
{
    SwingStrength_float(uv, strength);
}