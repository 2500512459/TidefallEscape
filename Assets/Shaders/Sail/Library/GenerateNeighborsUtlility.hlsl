void GenerateNeighbors_float(float3 position, float3 normal, out float3 neighbor1, out float3 neighbor2)
{
    float d = normal.x * position.x + normal.y * position.y + normal.z * position.z;
    float offset = 0.05; //some small value
    float3 positionAuxiliary;
    if (normal.x == 0)
    {
        float x1 = position.x + offset;
        positionAuxiliary = float3(x1, position.y, position.z);
    }
    else
    {
        float y1 = position.y + offset;
        float x1 = (d - normal.y * y1 - normal.z * position.z) / normal.x;
        positionAuxiliary = float3(x1, y1, position.z);
    }
    
    float3 vector1 = normalize(positionAuxiliary - position);
    float3 vector2 = normalize(cross(normalize(normal), vector1));
    
    float vectorScale = 0.01;
    neighbor1 = position + vector1 * vectorScale;
    neighbor2 = position + vector2 * vectorScale;
}

void GenerateNeighbors_half(float3 position, float3 normal, out float3 neighbor1, out float3 neighbor2)
{
    GenerateNeighbors_float(position, normal, neighbor1, neighbor2);
}

