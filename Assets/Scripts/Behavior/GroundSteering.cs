using UnityEngine;
using System.Collections.Generic;

// 基于 CharacterController 的地面转向/移动行为
// 要求存在 CharacterController
[RequireComponent(typeof(CharacterController))]
public class GroundSteering : MonoBehaviour
{
	[Header("General")]
	public float maxVelocity = 3.5f;        // 最大移动速度
	public float maxAcceleration = 10f;     // 最大加速度
	public float turnSpeed = 20f;           // 转向速度

	[Header("Arrive")]
	public float targetRadius = 0.005f;     // 到达目标点的判定半径
	public float slowRadius = 1f;           // 开始减速的半径范围
	public float timeToTarget = 0.1f;       // 到达目标的时间参数

	private CharacterController controller;
	private Vector3 currentVelocity;        // 显式管理速度
	private const float k_HorizontalEpsilon = 0.02f;

	void Awake()
	{
		controller = GetComponent<CharacterController>();
	}

	// 应用“加速度”推进速度，并用 CharacterController 移动
	public void Steer(Vector3 linearAcceleration)
	{
		currentVelocity += linearAcceleration * Time.deltaTime;
		if (currentVelocity.magnitude > maxVelocity)
		{
			currentVelocity = currentVelocity.normalized * maxVelocity;
		}
		// Y 方向交由重力或其他系统管理，这里仅保持水平分量
		Vector3 move = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
		if (move.sqrMagnitude < k_HorizontalEpsilon * k_HorizontalEpsilon)
		{
			move = Vector3.zero;
			currentVelocity.x = 0f;
			currentVelocity.z = 0f;
		}
		controller.Move(move * Time.deltaTime);
	}

	// 直接设置目标速度（可用于外部融合后速度）
	public void SetVelocity(Vector3 desiredVelocity)
	{
		currentVelocity = Vector3.ClampMagnitude(desiredVelocity, maxVelocity);
		Vector3 move = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
		if (move.sqrMagnitude < k_HorizontalEpsilon * k_HorizontalEpsilon)
		{
			move = Vector3.zero;
			currentVelocity.x = 0f;
			currentVelocity.z = 0f;
		}
		controller.Move(move * Time.deltaTime);
	}

	public Vector3 GetVelocity()
	{
		return currentVelocity;
	}

	// 清除水平速度，避免在贴脸时被法线分解后产生绕圈
	public void ClearHorizontalVelocity()
	{
		currentVelocity.x = 0f;
		currentVelocity.z = 0f;
	}

	public Vector3 Seek(Vector3 targetPosition, float maxSeekAccel)
	{
		Vector3 acceleration = targetPosition - transform.position;
		acceleration.y = 0f;
		acceleration.Normalize();
		acceleration *= maxSeekAccel;
		return acceleration;
	}

	public Vector3 Seek(Vector3 targetPosition)
	{
		return Seek(targetPosition, maxAcceleration);
	}

	public void LookMoveDirection()
	{
		Vector3 direction = currentVelocity;
		LookAtDirection(direction);
	}

	public void LookAtDirection(Vector3 direction)
	{
		direction.y = 0f;
		direction.Normalize();
		if (direction.sqrMagnitude > 0.001f)
		{
			float toRotation = -1 * (Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg) + 90;
			float rotation = Mathf.LerpAngle(transform.rotation.eulerAngles.y, toRotation, Time.deltaTime * turnSpeed);
			transform.rotation = Quaternion.Euler(0, rotation, 0);
		}
	}

	public void LookAtDirection(float toRotation)
	{
		float rotation = Mathf.LerpAngle(transform.rotation.eulerAngles.y, toRotation, Time.deltaTime * turnSpeed);
		transform.rotation = Quaternion.Euler(0, rotation, 0);
	}

	public Vector3 Arrive(Vector3 targetPosition)
	{
		Debug.DrawLine(transform.position, targetPosition, Color.green, 0f, false);

		Vector3 toTarget = targetPosition - transform.position;
		toTarget.y = 0f;
		float dist = toTarget.magnitude;
		if (dist < targetRadius)
		{
			currentVelocity.x = 0f;
			currentVelocity.z = 0f;
			return Vector3.zero;
		}

		float targetSpeed = dist > slowRadius ? maxVelocity : maxVelocity * (dist / slowRadius);
		Vector3 targetVelocity = toTarget.normalized * targetSpeed;
		Vector3 acceleration = (targetVelocity - currentVelocity) * (1f / Mathf.Max(0.0001f, timeToTarget));
		if (acceleration.magnitude > maxAcceleration)
		{
			acceleration = acceleration.normalized * maxAcceleration;
		}
		return acceleration;
	}
}


