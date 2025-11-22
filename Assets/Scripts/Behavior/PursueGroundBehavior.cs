using UnityEngine;

// 基于 GroundSteering 的追逐行为
// 不强制刚体，优先使用 Transform/CharacterController 信息
[RequireComponent(typeof(GroundSteering))]
public class PursueGroundBehavior : MonoBehaviour
{
	public float maxPrediction = 1f;

	private GroundSteering ground;
	private Vector3 lastTargetPos;
	private bool hasLastTargetPos = false;

	void Awake()
	{
		ground = GetComponent<GroundSteering>();
	}

	// 允许传入 Transform（可选传入 Rigidbody 以获得更准确的目标速度）
	public Vector3 GetSteering(Transform target, Rigidbody targetRb = null)
	{
		if (target == null)
			return Vector3.zero;

		// 预测时间
		float speed = new Vector3(ground.GetVelocity().x, 0f, ground.GetVelocity().z).magnitude;
		Vector3 displacement = target.position - transform.position;
		displacement.y = 0f;
		float distance = displacement.magnitude;

		float prediction;
		if (speed <= Mathf.Epsilon || speed <= distance / Mathf.Max(0.0001f, maxPrediction))
			prediction = maxPrediction;
		else
			prediction = distance / speed;

		// 估计目标速度
		Vector3 targetVelocity = Vector3.zero;
		if (targetRb != null)
		{
			targetVelocity = targetRb.velocity;
			targetVelocity.y = 0f;
		}
		else
		{
			if (hasLastTargetPos)
			{
				targetVelocity = (target.position - lastTargetPos) / Mathf.Max(Time.deltaTime, 0.0001f);
				targetVelocity.y = 0f;
			}
			lastTargetPos = target.position;
			hasLastTargetPos = true;
		}

		Vector3 explicitTarget = target.position + targetVelocity * prediction;
		explicitTarget.y = transform.position.y; // 保持水平追踪
		return ground.Seek(explicitTarget);
	}
}


