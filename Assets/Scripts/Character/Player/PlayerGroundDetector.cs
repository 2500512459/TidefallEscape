using UnityEngine;

public class PlayerGroundDetector : MonoBehaviour
{
    [Header("检测参数")]
    [SerializeField] float detectRadius = 0.2f;       // 球体半径
    [SerializeField] float detectDistance = 0.1f;     // 向下偏移距离（球心离地面距离）
    [SerializeField] LayerMask groundLayer;
    [SerializeField] LayerMask shipLayer;

    /// <summary>
    /// 是否在地面上（基于球体检测）
    /// </summary>
    public bool IsGrounded
    {
        get
        {
            Vector3 checkPos = transform.position + Vector3.down * detectDistance;
            return Physics.CheckSphere(checkPos, detectRadius, groundLayer, QueryTriggerInteraction.Ignore);
        }
    }

    /// <summary>
    /// 尝试获取船体地面的命中点（比如用于角色吸附到船面）
    /// </summary>
    public bool TryGetShipGroundPoint(out Vector3 shipPoint)
    {
        Vector3 start = transform.position;
        // 用SphereCast来检测船体下方的命中点
        if (Physics.SphereCast(start, detectRadius, Vector3.down, out RaycastHit hit, detectDistance * 2f, shipLayer, QueryTriggerInteraction.Ignore))
        {
            shipPoint = hit.point;
            return true;
        }

        shipPoint = Vector3.zero;
        return false;
    }

    void OnDrawGizmos()
    {
        // 画出检测球体位置
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Vector3 checkPos = transform.position + Vector3.down * detectDistance;
        Gizmos.DrawWireSphere(checkPos, detectRadius);

        // 可视化船面检测（可选）
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (detectDistance * 2f));
    }
}
