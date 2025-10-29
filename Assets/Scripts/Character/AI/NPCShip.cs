using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;

/// <summary>
/// NPCShip��AI ��ֻ�߼�
/// - �̳��� AICharacter���߱�ͨ�� AI ��Ϊ������
/// - ʹ�� WaypointNavigator Ѱ·ϵͳ����·������
/// - ʹ�� SteeringBehaviors ʵ��ת�����ƶ�����
/// - ʹ�� Fluid Behavior Tree ʵ�� AI ��Ϊ����
/// </summary>
public class NPCShip : AICharacter
{
    // �� NPC ���ϵ�·���������������·���㵼����
    WaypointNavigator waypointNav;


    /// <summary>
    /// Awake���ڶ��󴴽�ʱ���ã��� Start ���磩
    /// ���ڳ�ʼ����Ҫ�������
    /// </summary>
    protected override void Awake()
    {
        base.Awake();  // ���ø��� AICharacter �� Awake��һ���ʼ�� steeringBehaviors��colsensor��separation �ȣ�

        // ��ȡ������ͬһ�����ϵ� WaypointNavigator ���
        waypointNav = GetComponent<WaypointNavigator>();
    }


    /// <summary>
    /// Start���ڳ�������ʱ����
    /// </summary>
    protected override void Start()
    {
        base.Start();  // ���ø��� Start�����ܳ�ʼ�� AI �����򴫸�����

        // ��ʼ����Ϊ��
        InitAI();
    }


    /// <summary>
    /// Update��ÿ֡���ã��˴�δ�����߼�������������£�
    /// </summary>
    protected override void Update()
    {
        base.Update();
    }


    /// <summary>
    /// FixedUpdate������֡���£�ͨ�������ƶ���������㣩
    /// ������������Ϊ����brain.Tick��ÿִ֡��һ�ξ����߼���
    /// </summary>
    private void FixedUpdate()
    {
        brain.Tick(); // ִ�е�ǰ AI ��Ϊ���߼�
    }


    /// <summary>
    /// ��ʼ�� AI ��Ϊ����
    /// ���� AI ��ֻ����Ҫ��Ϊ�߼�����һ���򵥵ġ����´�״̬������
    /// </summary>
    void InitAI()
    {
        brain = new BehaviorTreeBuilder(gameObject)
            // ======================== [����ڵ�] =========================
            .Do("Update Ship", () =>
            {
                // ��ʼ��Ŀ��λ�ã�Ĭ��ֵΪ�㣩
                Vector3 targetPosition = Vector3.zero;

                // �� WaypointManager ��ȡ���������е�·����
                List<Waypoint> waypoints = WaypointManager.Instance.GetWaypoints();

                // ���·������ڣ������ѡ��һ��Ŀ����е���
                if (waypoints != null && waypoints.Count > 0)
                {
                    // ����ǰû��·���������ѡȡһ��·������Ϊ��Ŀ�ĵ�
                    while (!waypointNav.HasPath)
                    {
                        waypointNav.SetDestination(
                            waypoints[Random.Range(0, waypoints.Count)].Position
                        );
                    }

                    // ��ȡ��ǰ·������һ��Ŀ���λ��
                    targetPosition = waypointNav.CurrentWaypointPosition;
                }

                // ʹ�� steeringBehaviors �ġ������Ϊ������ٶ�����
                Vector3 accel = steeringBehaviors.Arrive(targetPosition);


                // =================== [��ײ����������] ====================
                // ����ǰ��ӵ����ײ�������colsensor�������������ٶȷ��򣬱ܿ��ϰ�
                if (colsensor)
                {
                    // ��ȡ���ٶȷ���
                    Vector3 accDir = accel.normalized;

                    // ͨ����ײ���������㰲ȫ����GetCollisionFreeDirection2��
                    colsensor.GetCollisionFreeDirection2(accDir, out accDir);

                    // ����ԭ�����ٶȴ�С����ֹ�ٶ�ͻ�䣩
                    accDir *= accel.magnitude;

                    // ����������ļ��ٶ�
                    accel = accDir;
                }


                // =================== [������Ϊ����] ====================
                // �������� separation��Ⱥ�������Ϊ����
                // ����ԭ�м��ٶȻ����ϵ��ӷ����������������������ص���
                if (separation)
                {
                    accel += separation.GetSteering();
                }


                // =================== [����ִ���ƶ���ת��] ====================
                // ������õ��ܼ��ٶȴ��ݸ� Steering ϵͳ�����ƶ�����
                steeringBehaviors.Steer(accel);

                // �ô�ֻ���ƶ�����ƽ��ת��
                steeringBehaviors.LookMoveDirection();

                // ����ɹ�����Ϊ��Ҫ�󷵻� TaskStatus��
                return TaskStatus.Success;
            })
            // ============================================================
            .Build(); // ������������Ϊ���ṹ
    }
}
