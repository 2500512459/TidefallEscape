using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image 颜色渐变组件
/// 支持多个方向的颜色渐变效果
/// </summary>
[RequireComponent(typeof(Image))]
public class ImageGradualChange : BaseMeshEffect
{
    /// <summary>
    /// 渐变方向枚举
    /// </summary>
    public enum GradientDirection
    {
        TopToBottom,    // 从上到下
        BottomToTop,    // 从下到上
        LeftToRight,    // 从左到右
        RightToLeft,    // 从右到左
        TopLeftToBottomRight,  // 从左上到右下
        TopRightToBottomLeft,  // 从右上到左下
        BottomLeftToTopRight,  // 从左下到右上
        BottomRightToTopLeft   // 从右下到左上
    }

    [Header("渐变设置")]
    [SerializeField] private GradientDirection direction = GradientDirection.TopToBottom;
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = Color.black;
    [SerializeField] private bool useGradient = true;

    private Image image;

    protected override void Awake()
    {
        base.Awake();
        image = GetComponent<Image>();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!useGradient || image == null)
            return;

        UIVertex vertex = new UIVertex();
        Rect rect = image.rectTransform.rect;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            // 获取顶点在 RectTransform 本地坐标系中的位置
            // vertex.position 是相对于 RectTransform 的本地坐标
            Vector2 localPos = new Vector2(vertex.position.x, vertex.position.y);
            
            // 计算渐变因子 (0 到 1)
            float t = CalculateGradientFactor(localPos, rect);

            // 根据渐变因子插值颜色，同时保留原始颜色的 alpha 通道
            Color finalColor = Color.Lerp(startColor, endColor, t);
            finalColor.a *= vertex.color.a; // 保留原始 alpha
            vertex.color = finalColor;

            vh.SetUIVertex(vertex, i);
        }
    }

    /// <summary>
    /// 根据方向和顶点位置计算渐变因子
    /// </summary>
    private float CalculateGradientFactor(Vector2 localPos, Rect rect)
    {
        float t = 0f;

        switch (direction)
        {
            case GradientDirection.TopToBottom:
                // 从上到下：y 值越大，t 越小
                t = 1f - Mathf.InverseLerp(rect.yMin, rect.yMax, localPos.y);
                break;

            case GradientDirection.BottomToTop:
                // 从下到上：y 值越大，t 越大
                t = Mathf.InverseLerp(rect.yMin, rect.yMax, localPos.y);
                break;

            case GradientDirection.LeftToRight:
                // 从左到右：x 值越大，t 越大
                t = Mathf.InverseLerp(rect.xMin, rect.xMax, localPos.x);
                break;

            case GradientDirection.RightToLeft:
                // 从右到左：x 值越大，t 越小
                t = 1f - Mathf.InverseLerp(rect.xMin, rect.xMax, localPos.x);
                break;

            case GradientDirection.TopLeftToBottomRight:
                // 从左上到右下：对角线
                float diagonal1 = Vector2.Distance(localPos, new Vector2(rect.xMin, rect.yMax));
                float maxDiagonal1 = Vector2.Distance(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMax, rect.yMin));
                t = diagonal1 / maxDiagonal1;
                break;

            case GradientDirection.TopRightToBottomLeft:
                // 从右上到左下：对角线
                float diagonal2 = Vector2.Distance(localPos, new Vector2(rect.xMax, rect.yMax));
                float maxDiagonal2 = Vector2.Distance(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMin));
                t = diagonal2 / maxDiagonal2;
                break;

            case GradientDirection.BottomLeftToTopRight:
                // 从左下到右上：对角线
                float diagonal3 = Vector2.Distance(localPos, new Vector2(rect.xMin, rect.yMin));
                float maxDiagonal3 = Vector2.Distance(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMax));
                t = diagonal3 / maxDiagonal3;
                break;

            case GradientDirection.BottomRightToTopLeft:
                // 从右下到左上：对角线
                float diagonal4 = Vector2.Distance(localPos, new Vector2(rect.xMax, rect.yMin));
                float maxDiagonal4 = Vector2.Distance(new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMin, rect.yMax));
                t = diagonal4 / maxDiagonal4;
                break;
        }

        return Mathf.Clamp01(t);
    }

    /// <summary>
    /// 设置渐变方向
    /// </summary>
    public void SetDirection(GradientDirection newDirection)
    {
        if (direction != newDirection)
        {
            direction = newDirection;
            if (image != null)
            {
                image.SetVerticesDirty();
            }
        }
    }

    /// <summary>
    /// 设置起始颜色
    /// </summary>
    public void SetStartColor(Color color)
    {
        startColor = color;
        if (image != null)
        {
            image.SetVerticesDirty();
        }
    }

    /// <summary>
    /// 设置结束颜色
    /// </summary>
    public void SetEndColor(Color color)
    {
        endColor = color;
        if (image != null)
        {
            image.SetVerticesDirty();
        }
    }

    /// <summary>
    /// 设置渐变颜色
    /// </summary>
    public void SetGradientColors(Color start, Color end)
    {
        startColor = start;
        endColor = end;
        if (image != null)
        {
            image.SetVerticesDirty();
        }
    }

    /// <summary>
    /// 启用/禁用渐变效果
    /// </summary>
    public void SetGradientEnabled(bool enabled)
    {
        useGradient = enabled;
        if (image != null)
        {
            image.SetVerticesDirty();
        }
    }

    /// <summary>
    /// 刷新渐变效果
    /// </summary>
    public void Refresh()
    {
        if (image != null)
        {
            image.SetVerticesDirty();
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (image != null)
        {
            image.SetVerticesDirty();
        }
    }
#endif
}

