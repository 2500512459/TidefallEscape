using UnityEngine;

// 通用的前向安全探测组件：
// - 将本脚本挂在角色的"前方"空对象上（或直接挂在主体上也可）
// - 提供 IsForwardUnsafe() API：若前方无地面或地面低于水面则返回 true
// - 在选中物体时用 Gizmos 绘制探测射线（绿=安全，红=不安全）
public class ForwardSafetyProbe : MonoBehaviour
{
	[Header("Layers")]
	[SerializeField] private LayerMask groundLayer = ~0;	// 地面层（用于射线命中）

	[Header("Probe")]
	[SerializeField] private float forwardProbeDistance = 1.0f;	// 前向探测的水平距离（以自身 forward 为方向）
	[SerializeField] private float probeStartHeight = 1.0f;		// 起点相对当前位置的上抬高度
	[SerializeField] private float probeDownDistance = 4.0f;		// 向下射线长度

	[Header("Water")]
	[SerializeField] private float waterClearance = 0.05f;		// 允许略高于水面的容差

	// 计算探测的起点（以本组件所在物体为参照系）
	private Vector3 GetProbeStart()
	{
		Vector3 forwardFlat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
		return transform.position + forwardFlat * forwardProbeDistance + Vector3.up * probeStartHeight;
	}

	// 对外 API：前方是否不安全（无地面或低于水面）
	public bool IsForwardUnsafe()
	{
		Vector3 start = GetProbeStart();
		bool hitGround = Physics.Raycast(start, Vector3.down, out RaycastHit hit, probeDownDistance, groundLayer, QueryTriggerInteraction.Ignore);
		if (!hitGround)
		{
			return true;
		}

		// 若有水系统则与水位比较
		if (Water.Instance != null)
		{
			float waterHeight = Water.Instance.GetWaterHeight(hit.point);
			if (hit.point.y <= waterHeight + waterClearance)
			{
				return true;
			}
		}
		return false;
	}

	// 可选：尝试采样前方地面命中信息
	public bool TrySampleGround(out RaycastHit hitInfo)
	{
		Vector3 start = GetProbeStart();
		return Physics.Raycast(start, Vector3.down, out hitInfo, probeDownDistance, groundLayer, QueryTriggerInteraction.Ignore);
	}

	// 可视化（选中时显示）
	private void OnDrawGizmosSelected()
	{
		Vector3 start = GetProbeStart();
		bool hitGround = Physics.Raycast(start, Vector3.down, out RaycastHit hit, probeDownDistance, groundLayer, QueryTriggerInteraction.Ignore);

		bool unsafeAhead = !hitGround;
		if (!unsafeAhead && Water.Instance != null)
		{
			float waterHeight = Water.Instance.GetWaterHeight(hit.point);
			unsafeAhead = hit.point.y <= waterHeight + waterClearance;
		}

		Gizmos.color = unsafeAhead ? Color.red : Color.green;
		Gizmos.DrawLine(start, start + Vector3.down * probeDownDistance);
		if (hitGround)
		{
			Gizmos.DrawSphere(hit.point, 0.05f);
		}
	}
}


