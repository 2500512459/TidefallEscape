using UnityEngine;

public class PlayerWallDetector : MonoBehaviour
{
    [Header("检测参数")]
    [SerializeField] float detectDistance = 0.5f;     // 检测前方距离
    [Tooltip("墙顶检测最大高度")]
    [SerializeField] float maxClimbCheckHeight = 3f;  // 墙顶检测最大高度
    [Tooltip("爬上去的高度阈值")]
    [SerializeField] float climbThreshold = 0.3f;     // 爬上去的高度阈值
    [Tooltip("脚部检测的偏移高度")]
    [SerializeField] float footRayOffset = 0.3f;   // 脚部检测的偏移高度
    [Tooltip("矮墙最大高度（可跨越）")]
    [SerializeField] float stepMaxHeight = 0.8f;  // 矮墙最大高度（可跨越）
    [Tooltip("判定墙面的最小表面角度，低于该角度视为坡面/地面")]
    [Range(0f, 90f)]
    [SerializeField] float minWallSurfaceAngle = 70f;
    [Tooltip("墙体层级")]
    [SerializeField] LayerMask wallLayer;             // 墙体层级
    [Tooltip("船墙体层级")]
    [SerializeField] LayerMask shipWallLayer;             // 船墙体层级
    public Vector3 WallTopPoint { get; private set; }
    /// <summary>
    /// 是否正贴着墙体
    /// </summary>
    public bool IsTouchingWall
    {
        get
        {
            return Physics.Raycast(transform.position, transform.forward, detectDistance, wallLayer, QueryTriggerInteraction.Ignore);
        }
    }
    /// <summary>
    /// 是否可以翻越（墙顶高度差 <= climbThreshold）
    /// </summary>
    public bool IsClimbOver
    {
        get
        {
            float wallTopY = GetWallTopHeight();
            if (wallTopY == Mathf.Infinity) return false; // 没检测到墙顶

            //高射线检测到墙，且顶部低于可翻越高度
            if (IsTouchingWall)
            {
                float heightDiff = wallTopY - transform.position.y;
                if (heightDiff <= climbThreshold && heightDiff > 0f)
                {
                    return true;
                }
            }

            // 脚部射线检测到矮墙（跨岸边用）
            if (IsLowWall)
            {
                Vector3 footOrigin = transform.position + Vector3.down * footRayOffset;
                float heightDiff = wallTopY - footOrigin.y;
                if (heightDiff <= stepMaxHeight && heightDiff > 0f)
                {
                    return true;
                }
            }

            return false;
        }
    }
    /// <summary>
    /// 脚部的前向检测（解决矮墙/岸边挡住无法站立问题）
    /// </summary>
    public bool IsLowWall
    {
        get
        {
            Vector3 origin = transform.position + Vector3.down * footRayOffset;
            if (Physics.Raycast(origin, transform.forward, out var hit, detectDistance, wallLayer))
            {
                float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (surfaceAngle >= minWallSurfaceAngle)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 获取墙顶的世界坐标高度（y值）
    /// </summary>
    public float GetWallTopHeight()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        bool hitWall = false;

        // 优先检测正前方是否有墙体（身体位置）
        if (Physics.Raycast(transform.position, transform.forward, out hit, detectDistance, wallLayer, QueryTriggerInteraction.Ignore))
        {
            hitWall = true;
        }
        // 如果身体位置没检测到，但脚部检测到了矮墙，则从脚部位置检测
        else if (IsLowWall)
        {
            Vector3 footOrigin = transform.position + Vector3.down * footRayOffset;
            if (Physics.Raycast(footOrigin, transform.forward, out hit, detectDistance, wallLayer, QueryTriggerInteraction.Ignore))
            {
                hitWall = true;
            }
        }

        if (hitWall)
        {
            Vector3 wallPoint = hit.point;

            // 从命中点往上发射，检测顶部
            RaycastHit topHit;
            Vector3 upwardOrigin = wallPoint + Vector3.up * 0.1f;

            if (!Physics.Raycast(upwardOrigin, Vector3.up, out topHit, maxClimbCheckHeight, wallLayer, QueryTriggerInteraction.Ignore))
            {
                // 上方没有继续的墙体，从上往下检测确认顶部位置
                if (Physics.Raycast(upwardOrigin + Vector3.up * maxClimbCheckHeight, Vector3.down, out topHit, maxClimbCheckHeight * 2, wallLayer, QueryTriggerInteraction.Ignore))
                {
                    WallTopPoint = topHit.point;
                    return topHit.point.y;
                }
            }
        }

        return Mathf.Infinity;
    }

    void OnDrawGizmos()
    {
        // 绘制墙检测（前方检测）
        Gizmos.color = Application.isPlaying ? (IsTouchingWall ? Color.green : Color.red) : Color.yellow;
        Vector3 end = transform.position + transform.forward * detectDistance;
        Gizmos.DrawLine(transform.position, end);
        Gizmos.DrawSphere(end, 0.02f);

        // 绘制墙顶检测（命中的墙顶位置）
        if (Application.isPlaying)
        {
            float topY = GetWallTopHeight();
            if (topY != Mathf.Infinity)
            {
                Gizmos.color = IsClimbOver ? Color.cyan : Color.gray;
                Vector3 pos = new Vector3(transform.position.x, topY, transform.position.z) + transform.forward * detectDistance;
                Gizmos.DrawSphere(pos, 0.05f);
            }
        }

        // 脚部射线检测（命中=绿 | 未命中=蓝）
        Vector3 footOrigin = transform.position + Vector3.down * footRayOffset;

        if (Application.isPlaying && IsLowWall)
            Gizmos.color = Color.green;      // 命中矮墙 → 绿色
        else
            Gizmos.color = Color.blue;       // 未命中 → 蓝色

        Gizmos.DrawLine(footOrigin, footOrigin + transform.forward * detectDistance);

        // ===============================
        // 绘制可跨越的最大高度（黄色）
        // ===============================
        Gizmos.color = Color.yellow;
        Vector3 climbMaxStart = footOrigin;  // 从脚部位置开始
        Vector3 climbMaxEnd = footOrigin + Vector3.up * stepMaxHeight;
        Gizmos.DrawLine(climbMaxStart, climbMaxEnd);
    }

}
