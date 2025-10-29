using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;


public class Shark : AICharacter
{
    private Character attackTarget = null;
    private Vector3 moveToPosition = Vector3.zero;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        InitAI();
        InitAttributes();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        live = true;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (live)
            UpdateAttackTarget();
    }

    private void FixedUpdate()
    {
        if (!live) return;

        brain.Tick();
    }

    void InitAI()
    {
        brain = new BehaviorTreeBuilder(gameObject)
            .Selector()
                .Sequence("Attack Tree")
                    .Condition("Have Attack Target", () => { return HaveAttackTarget(); })
                    .Selector("Want to Attack")
                        .Sequence("尝试攻击")
                            .Condition("In Attack Range", () => { return IsInAttackRange(attackTarget); })
                            .Do("Attack", () =>
                            {
                                DoAttack(attackTarget);
                                return TaskStatus.Success;
                            })
                            .End()
                        .Do("Pursuit", () =>
                        {
                            DoPursuit(attackTarget);
                            return TaskStatus.Success;
                        })
                        .End()
                    .End()
                .Do("Wander", () =>
                {
                    DoWander();
                    return TaskStatus.Success;
                })
            .Build();
    }

    void InitAttributes()
    {
        //if (attributesModule != null)
        //{
        //    attributesModule.AddAttribute(AttributeType.AT_Blood, 100, 0, 100);
        //}
    }

    //target...
    Character GetNearestAttackTargetInView()
    {
        CharacterTypeFilter typeFilter = (actor) => actor is PlayerShip;

        List<Character> targets = GetCharactersInView(typeFilter);

        if (targets.Count == 0) return null;

        targets.Sort((actorA, actorB) =>
        {
            float distanceA = Vector3.Distance(actorA.transform.position, transform.position);
            float distanceB = Vector3.Distance(actorB.transform.position, transform.position);

            //Returns the comparison result so that the smaller distance is at the front.
            return distanceA.CompareTo(distanceB);
        });

        return targets[0];
    }

    void UpdateAttackTarget()
    {
        if (attackTarget)
        {
            if (Vector3.Distance(attackTarget.transform.position, transform.position) > viewRadius)
            {
                attackTarget = null;
            }
        }

        if (attackTarget == null) attackTarget = GetNearestAttackTargetInView();
    }

    bool HaveAttackTarget()
    {
        return attackTarget != null;
    }

    bool IsInAttackRange(Character actor)
    {
        if (attackTarget == null) return false;

        if (Vector3.Distance(actor.transform.position, transform.position) < attackRadius)
        {
            return true;
        }

        return false;
    }

    void DoWander()
    {
        //Debug.Log("Wandering!");
        if (animator != null) { animator.SetBool("Attack", false); }

        Vector3 accel = wander.GetSteering();

        if (colsensor)
        {
            Vector3 accDir = accel.normalized;
            colsensor.GetCollisionFreeDirection2(accDir, out accDir);
            accDir *= accel.magnitude;
            accel = accDir;
        }

        steeringBehaviors.Steer(accel);
        steeringBehaviors.LookMoveDirection();
    }

    void DoAttack(Character actor)
    {
        if (actor == null) return;
        //Debug.Log("Attacking!");

        if (animator != null) { animator.SetBool("Attack", true); }

        steeringBehaviors.Steer(Vector3.zero);
        steeringBehaviors.LookAtDirection(actor.transform.position - transform.position);
    }

    void DoPursuit(Character actor)
    {
        if (actor == null) return;
        //Debug.Log("Pursuiting!");
        if (animator != null) { animator.SetBool("Attack", false); }

        Vector3 accel = pursue.GetSteering(actor.GetRigidBody());

        steeringBehaviors.Steer(accel);
        steeringBehaviors.LookMoveDirection();
    }

    public void DoDamage(int damage)
    {
        if (!live) return;

        //if (attributesModule != null)
        //{
        //    int blood = (int)attributesModule.GetAttributeValue(AttributeType.AT_Blood);

        //    attributesModule.SetAttributeValue(AttributeType.AT_Blood, blood - damage);

        //    if (blood - damage <= 0)
        //    {
        //        live = false;
        //        Dying();
        //    }
        //}
    }

    void Alive()
    {
        animator.speed = 1;

        if (rgBody != null)
        {
            //todo fix steering
            //rgBody.Gravity = false;
        }
    }

    void Dying()
    {
        Vector3 oldRot = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(oldRot.x, oldRot.y, 180);

        animator.speed = 0;

        //todo fix steering
        //if (aiRigidbody != null)
        //{
        //    aiRigidbody.Gravity = true;
        //}

        //gameObject.Recycle();
        StartCoroutine(DelayedRecycle());
    }

    IEnumerator DelayedRecycle()
    {
        yield return new WaitForSeconds(5);

        gameObject.Recycle();
    }
}


