void SwingPosition_float(float3 position, float swing, out float3 result)
{
    half sinOff = position.x + position.y + position.z;
    half t = -_Time.x * _SwingSpeed;
    
    position.z += sin(t + sinOff * _SwingFrequence) * _SwingAmplitude * swing;
    
    result = position;
}

void SwingPosition_half(float3 position, float swing, out float3 result)
{
    SwingPosition_float(position, swing, result);
}

void SwingStrength_float(float2 uv, out float strength)
{
    float2 dist = uv - float2(0.5, 0.5);
    float ratio = length(dist) / 0.8;
    
    strength = saturate(1 - ratio);
}

void SwingStrength_half(float2 uv, out float strength)
{
    SwingStrength_float(uv, strength);
}