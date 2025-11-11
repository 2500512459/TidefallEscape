using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackCheck : MonoBehaviour
{
    [Header("攻击检测设置")]
    [SerializeField] Transform attackOrigin;      // 攻击起点（一般设为武器前方）
    [SerializeField] float attackRange = 1.5f;    // 攻击范围半径
    [SerializeField, Range(0, 360f)] float attackAngle = 120f;  // 扇形角度
    [SerializeField] LayerMask targetMask;        // 可攻击的目标层（敌人）

    /// <summary>
    /// 检测并对范围内的敌人造成伤害
    /// （可在动画事件或攻击状态中调用）
    /// </summary>
    public void Attack(int damage = 0)
    {
        Debug.Log("执行了Attack判定");
        Vector3 origin = attackOrigin ? attackOrigin.position : transform.position;
        Vector3 forward = attackOrigin ? attackOrigin.forward : transform.forward;

        // 检测半径内的所有碰撞体
        Collider[] hits = Physics.OverlapSphere(origin, attackRange, targetMask, QueryTriggerInteraction.Ignore);
        Debug.Log($"检测到 {hits.Length} 个目标");
        foreach (var hit in hits)
        {
            // 计算与前方夹角是否在扇形范围内
            Vector3 dir = hit.transform.position - origin;
            dir.y = 0; // 扁平化
            float angleToTarget = Vector3.Angle(forward, dir);

            if (angleToTarget <= attackAngle * 0.5f)
            {
                // 命中有效
                Character target = hit.GetComponent<Character>();
                if (target != null)
                {
                    Debug.Log($"命中目标: {target.name}，造成 {damage} 点伤害");
                    EventManager.Raise<DamageMessage>(new DamageMessage(damage, target));
                }
            }
        }
    }

    // 在 Scene 视图中可视化攻击范围
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = attackOrigin ? attackOrigin.position : transform.position;
        Vector3 forward = attackOrigin ? attackOrigin.forward : transform.forward;

        // 扇形的起始两条边
        Quaternion leftRot = Quaternion.AngleAxis(-attackAngle / 2f, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(attackAngle / 2f, Vector3.up);

        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);

        // 绘制扇形边界
        Gizmos.DrawLine(origin, origin + leftDir * attackRange);
        Gizmos.DrawLine(origin, origin + rightDir * attackRange);

        // 绘制扇形弧线（用折线近似）
        int segments = 20;
        Vector3 lastPoint = origin + leftDir * attackRange;
        for (int i = 1; i <= segments; i++)
        {
            float lerpAngle = -attackAngle / 2f + (attackAngle / segments) * i;
            Vector3 nextDir = Quaternion.AngleAxis(lerpAngle, Vector3.up) * forward;
            Vector3 nextPoint = origin + nextDir * attackRange;
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }

        // 绘制圆心
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(origin, 0.05f);
    }
}
