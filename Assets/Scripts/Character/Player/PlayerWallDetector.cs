using UnityEngine;

public class PlayerWallDetector : MonoBehaviour
{
    [Header("检测参数")]
    [SerializeField] float detectDistance = 0.5f;     // 检测前方距离
    [SerializeField] float maxClimbCheckHeight = 3f;  // 墙顶检测最大高度
    [SerializeField] float climbThreshold = 0.3f;     // 爬上去的高度阈值
    [SerializeField] LayerMask wallLayer;             // 墙体层级
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

            float heightDiff = wallTopY - transform.position.y;
            return IsTouchingWall && heightDiff <= climbThreshold && heightDiff > 0f;
        }
    }

    /// <summary>
    /// 获取墙顶的世界坐标高度（y值）
    /// </summary>
    public float GetWallTopHeight()
    {
        RaycastHit hit;

        // 检测正前方是否有墙体
        if (Physics.Raycast(transform.position, transform.forward, out hit, detectDistance, wallLayer, QueryTriggerInteraction.Ignore))
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
        // 绘制墙检测
        Gizmos.color = Application.isPlaying ? (IsTouchingWall ? Color.green : Color.red) : Color.yellow;
        Vector3 end = transform.position + transform.forward * detectDistance;
        Gizmos.DrawLine(transform.position, end);
        Gizmos.DrawSphere(end, 0.02f);

        // 绘制墙顶检测
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
    }
}
