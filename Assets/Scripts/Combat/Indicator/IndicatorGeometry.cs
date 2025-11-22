using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 指示器几何体工具类，用于创建各种形状的网格
/// </summary>
public class IndicatorGeometry
{
    /// <summary>
    /// 创建一个四边形网格（用于简单的平面指示器）
    /// </summary>
    /// <returns>四边形网格对象</returns>
    public static Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        // 定义四边形的四个顶点（按顺时针顺序排列）
        List<Vector3> vertices = new List<Vector3>
        {
            new Vector3(-0.5f, 0, 0), // 左下角(LB)
            new Vector3(-0.5f, 0, 1), // 右下角(RB)
            new Vector3(0.5f, 0, 1),  // 右上角(RU)
            new Vector3(0.5f, 0, 0)   // 左上角(LU)
        };
        mesh.vertices = vertices.ToArray();
        
        // 设置UV坐标（纹理映射坐标）
        Vector2[] uv = new Vector2[vertices.Count];
        uv[0] = new Vector2(0, 0); // 左下角对应纹理左下角
        uv[1] = new Vector2(1, 0); // 右下角对应纹理右下角
        uv[2] = new Vector2(1, 1); // 右上角对应纹理右上角
        uv[3] = new Vector2(0, 1); // 左上角对应纹理左上角
        mesh.uv = uv;
        
        // 定义三角形索引（两个三角形组成一个四边形）
        // 顺时针定义三角形顶点以确保正面朝上
        int[] triangles = { 0, 2, 1, 0, 3, 2 };
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // 重新计算法线以正确处理光照
        return mesh;
    }
    
    /// <summary>
    /// 创建一个平面网格（用于抛物线轨迹指示器）
    /// </summary>
    /// <param name="widthSegments">宽度方向上的分段数</param>
    /// <param name="heightSegments">高度方向上的分段数</param>
    /// <returns>平面网格对象</returns>
    public static Mesh CreatePlaneMesh(int widthSegments, int heightSegments)
    {
        Mesh mesh = new Mesh();
        // 计算每个网格单元的尺寸
        float width = 1f / widthSegments;
        float height = 1f / heightSegments;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        // 生成顶点数据（逐行生成）
        for (int y = 0; y < heightSegments + 1; y++)
        {
            for (int x = 0; x < widthSegments; x++)
            {
                // 顶点位置从(-0.5, 0, 0)到(0.5, 0, 1)分布
                Vector3 vertex = new Vector3(-0.5f + y * height, 0, x * width);
                vertices.Add(vertex);
            }
        }
        
        // 生成三角形索引数据（通过相邻顶点形成网格）
        for (int y = 0; y < heightSegments; y++)
        {
            for (int x = 0; x < widthSegments - 1; x++)
            {
                // 计算当前网格单元四个顶点的索引
                int topLeft = y * widthSegments + x;
                int topRight = topLeft + 1;
                int bottomLeft = (y + 1) * widthSegments + x;
                int bottomRight = bottomLeft + 1;
                
                // 添加两个三角形构成一个矩形单元
                triangles.Add(topLeft);
                triangles.Add(bottomLeft);
                triangles.Add(topRight);
                triangles.Add(topRight);
                triangles.Add(bottomLeft);
                triangles.Add(bottomRight);
            }
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
    
    /// <summary>
    /// 创建圆形网格（用于范围指示器）
    /// </summary>
    /// <param name="radius">圆的半径</param>
    /// <param name="segments">圆周分段数（影响圆的光滑度）</param>
    /// <returns>圆形网格对象</returns>
    public static Mesh CreateCircleMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        
        // 添加圆心顶点
        vertices.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f)); // 圆心UV坐标设为中心点
        
        // 围绕圆心生成扇形顶点
        for (int i = 0; i <= segments; i++)
        {
            // 计算当前顶点的角度（弧度）
            float angle = ((float)i / segments) * 360f * Mathf.Deg2Rad;
            // 计算顶点位置（基于角度和半径）
            Vector3 vertex = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            vertices.Add(vertex);
            // 计算UV坐标（将圆形映射到正方形纹理）
            uvs.Add(new Vector2((Mathf.Cos(angle) + 1) * 0.5f, (Mathf.Sin(angle) + 1) * -0.5f + 1));
        }
        
        // 通过中心点和周边点构建三角形（扇形填充）
        for (int i = 1; i < segments; i++)
        {
            triangles.Add(0);      // 圆心
            triangles.Add(i);      // 当前点
            triangles.Add(i + 1);  // 下一个点
        }
        // 连接最后一个三角形回到起点
        triangles.Add(0);
        triangles.Add(segments);
        triangles.Add(1);
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
    
    /// <summary>
    /// 创建圆环边缘网格（用于表示范围边界）
    /// </summary>
    /// <param name="innerRadius">内圆半径</param>
    /// <param name="outerRadius">外圆半径</param>
    /// <param name="segments">分段数</param>
    /// <returns>圆环边缘网格对象</returns>
    public static Mesh CreateCircleEdgeMesh(float innerRadius, float outerRadius, int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        // 计算每段对应的角度
        float anglePerSegment = (2 * Mathf.PI) / segments;
        
        // 存储内外圆顶点的索引
        List<int> innerCircleVertices = new List<int>();
        List<int> outerCircleVertices = new List<int>();
        
        // 生成内外圆顶点
        for (int i = 0; i <= segments; i++)
        {
            float angle = anglePerSegment * i;
            // 计算内外圆上的顶点位置
            Vector3 innerVertex = new Vector3(innerRadius * Mathf.Cos(angle), 0, innerRadius * Mathf.Sin(angle));
            Vector3 outerVertex = new Vector3(outerRadius * Mathf.Cos(angle), 0, outerRadius * Mathf.Sin(angle));
            
            int innerIndex = vertices.Count;
            int outerIndex = innerIndex + 1;
            vertices.Add(innerVertex);
            vertices.Add(outerVertex);
            
            // 设置UV坐标（横向展开纹理）
            uvs.Add(new Vector2((float)i / (float)segments, 1));
            uvs.Add(new Vector2((float)i / (float)segments, 0));
            
            innerCircleVertices.Add(innerIndex);
            outerCircleVertices.Add(outerIndex);
        }
        
        // 构建连接内外圆的四边形面片
        for (int i = 0; i < segments; i++)
        {
            // 获取当前和下一个顶点的索引
            int innerCurrent = innerCircleVertices[i];
            int innerNext = innerCircleVertices[(i + 1) % segments];
            int outerCurrent = outerCircleVertices[i];
            int outerNext = outerCircleVertices[(i + 1) % segments];
            
            // 添加两个三角形构成一个四边形
            triangles.Add(innerCurrent);
            triangles.Add(outerCurrent);
            triangles.Add(innerNext);
            triangles.Add(outerCurrent);
            triangles.Add(outerNext);
            triangles.Add(innerNext);
        }
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
    
    /// <summary>
    /// 创建扇形边缘网格（用于表示扇形范围边界）
    /// </summary>
    /// <param name="innerRadius">内圆半径</param>
    /// <param name="outerRadius">外圆半径</param>
    /// <param name="startAngle">起始角度（度，0度为前方，逆时针为正）</param>
    /// <param name="endAngle">结束角度（度）</param>
    /// <param name="segments">分段数</param>
    /// <returns>扇形边缘网格对象</returns>
    public static Mesh CreateSectorEdgeMesh(float innerRadius, float outerRadius, float startAngle, float endAngle, int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        // 将角度转换为弧度
        float startAngleRad = startAngle * Mathf.Deg2Rad;
        float endAngleRad = endAngle * Mathf.Deg2Rad;
        float angleRange = endAngleRad - startAngleRad;
        float anglePerSegment = angleRange / segments;
        
        // 存储内外圆顶点的索引
        List<int> innerCircleVertices = new List<int>();
        List<int> outerCircleVertices = new List<int>();
        
        // 生成扇形内外圆顶点
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngleRad + anglePerSegment * i;
            // 计算内外圆上的顶点位置
            Vector3 innerVertex = new Vector3(innerRadius * Mathf.Cos(angle), 0, innerRadius * Mathf.Sin(angle));
            Vector3 outerVertex = new Vector3(outerRadius * Mathf.Cos(angle), 0, outerRadius * Mathf.Sin(angle));
            
            int innerIndex = vertices.Count;
            int outerIndex = innerIndex + 1;
            vertices.Add(innerVertex);
            vertices.Add(outerVertex);
            
            // 设置UV坐标（横向展开纹理）
            float normalizedAngle = (float)i / (float)segments;
            uvs.Add(new Vector2(normalizedAngle, 1));
            uvs.Add(new Vector2(normalizedAngle, 0));
            
            innerCircleVertices.Add(innerIndex);
            outerCircleVertices.Add(outerIndex);
        }
        
        // 构建连接内外圆的四边形面片
        for (int i = 0; i < segments; i++)
        {
            // 获取当前和下一个顶点的索引
            int innerCurrent = innerCircleVertices[i];
            int innerNext = innerCircleVertices[i + 1];
            int outerCurrent = outerCircleVertices[i];
            int outerNext = outerCircleVertices[i + 1];
            
            // 添加两个三角形构成一个四边形
            triangles.Add(innerCurrent);
            triangles.Add(outerCurrent);
            triangles.Add(innerNext);
            triangles.Add(outerCurrent);
            triangles.Add(outerNext);
            triangles.Add(innerNext);
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// 创建扇形轮廓网格（包含圆弧和两侧直线）
    /// </summary>
    /// <param name="radius">外圆半径</param>
    /// <param name="thickness">线条宽度</param>
    /// <param name="startAngle">起始角度</param>
    /// <param name="endAngle">结束角度</param>
    /// <param name="segments">圆弧分段数</param>
    /// <returns>扇形轮廓网格</returns>
    public static Mesh CreateSectorOutlineMesh(float radius, float thickness, float startAngle, float endAngle, int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float innerRadius = radius - thickness;
        float outerRadius = radius;
        float halfThickness = thickness * 0.5f;

        // 1. 生成圆弧部分 (复用 CreateSectorEdgeMesh 的逻辑)
        float startAngleRad = startAngle * Mathf.Deg2Rad;
        float endAngleRad = endAngle * Mathf.Deg2Rad;
        float angleRange = endAngleRad - startAngleRad;
        float anglePerSegment = angleRange / segments;

        List<int> innerCircleVertices = new List<int>();
        List<int> outerCircleVertices = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngleRad + anglePerSegment * i;
            Vector3 innerVertex = new Vector3(innerRadius * Mathf.Cos(angle), 0, innerRadius * Mathf.Sin(angle));
            Vector3 outerVertex = new Vector3(outerRadius * Mathf.Cos(angle), 0, outerRadius * Mathf.Sin(angle));

            int innerIndex = vertices.Count;
            int outerIndex = innerIndex + 1;
            vertices.Add(innerVertex);
            vertices.Add(outerVertex);

            float normalizedAngle = (float)i / segments;
            uvs.Add(new Vector2(normalizedAngle, 1));
            uvs.Add(new Vector2(normalizedAngle, 0));

            innerCircleVertices.Add(innerIndex);
            outerCircleVertices.Add(outerIndex);
        }

        // 构建圆弧三角形
        for (int i = 0; i < segments; i++)
        {
            int innerCurrent = innerCircleVertices[i];
            int innerNext = innerCircleVertices[i + 1];
            int outerCurrent = outerCircleVertices[i];
            int outerNext = outerCircleVertices[i + 1];

            triangles.Add(innerCurrent);
            triangles.Add(outerCurrent);
            triangles.Add(innerNext);

            triangles.Add(outerCurrent);
            triangles.Add(outerNext);
            triangles.Add(innerNext);
        }

        // 2. 生成起始边 (Start Side)
        // 从中心到外圆，沿着 startAngle 方向
        AddRadialLine(vertices, triangles, uvs, Vector3.zero, outerRadius, startAngleRad, halfThickness);

        // 3. 生成结束边 (End Side)
        // 从中心到外圆，沿着 endAngle 方向
        AddRadialLine(vertices, triangles, uvs, Vector3.zero, outerRadius, endAngleRad, halfThickness);

        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    // 辅助方法：添加径向直线
    private static void AddRadialLine(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 center, float length, float angleRad, float halfWidth)
    {
        Vector3 dir = new Vector3(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad));
        Vector3 perp = new Vector3(-Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad));

        // 四个顶点：近端左右，远端左右
        Vector3 p0 = center - perp * halfWidth;
        Vector3 p1 = center + perp * halfWidth;
        Vector3 p2 = center + dir * length - perp * halfWidth;
        Vector3 p3 = center + dir * length + perp * halfWidth;

        int startIndex = vertices.Count;
        vertices.Add(p0);
        vertices.Add(p1);
        vertices.Add(p2);
        vertices.Add(p3);

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));

        // 两个三角形组成矩形 (0-1-2, 2-1-3)
        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 1);

        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);
        triangles.Add(startIndex + 1);
    }
}