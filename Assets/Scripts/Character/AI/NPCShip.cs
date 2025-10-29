using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;


public class NPCShip : AICharacter
{
    WaypointNavigator waypointNav;
    protected override void Awake()
    {
        base.Awake();

        waypointNav = GetComponent<WaypointNavigator>();
    }

    protected override void Start()
    {
        base.Start();

        InitAI();
    }

    protected override void Update()
    {
        base.Update();
    }

    private void FixedUpdate()
    {
        brain.Tick();
    }

    void InitAI()
    {
        brain = new BehaviorTreeBuilder(gameObject)
            .Do("Update Ship", () =>
            {
                Vector3 targetPosition = Vector3.zero;

                List<Waypoint> waypoints = WaypointManager.Instance.GetWaypoints();

                if (waypoints != null && waypoints.Count > 0)
                {
                    while (!waypointNav.HasPath)
                    {
                        waypointNav.SetDestination(waypoints[Random.Range(0, waypoints.Count)].Position);
                    }

                    targetPosition = waypointNav.CurrentWaypointPosition;
                }

                Vector3 accel = steeringBehaviors.Arrive(targetPosition);

                if (colsensor)
                {
                    Vector3 accDir = accel.normalized;
                    colsensor.GetCollisionFreeDirection2(accDir, out accDir);
                    accDir *= accel.magnitude;
                    accel = accDir;
                }

                if (separation)
                {
                    accel += separation.GetSteering();
                }

                steeringBehaviors.Steer(accel);
                steeringBehaviors.LookMoveDirection();

                return TaskStatus.Success;
            })
            .Build();
    }
}


