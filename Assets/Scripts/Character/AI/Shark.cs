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
        if (attributesModule != null)
        {
           attributesModule.AddAttribute(AttributeType.Hp, 100, 0, 100);
        }
    }

    //target...
    Character GetNearestAttackTargetInView()
    {
        CharacterTypeFilter typeFilter = (actor) => actor is Player;

        List<Character> targets = GetCharactersInView(typeFilter);

        if (targets.Count == 0) return null;

        // 过滤出在水中游泳的玩家
        List<Character> swimmingTargets = new List<Character>();
        foreach (Character target in targets)
        {
            if (IsPlayerSwimming(target))
            {
                swimmingTargets.Add(target);
            }
        }

        if (swimmingTargets.Count == 0) return null;

        swimmingTargets.Sort((actorA, actorB) =>
        {
            float distanceA = Vector3.Distance(actorA.transform.position, transform.position);
            float distanceB = Vector3.Distance(actorB.transform.position, transform.position);

            //Returns the comparison result so that the smaller distance is at the front.
            return distanceA.CompareTo(distanceB);
        });

        return swimmingTargets[0];
    }

    void UpdateAttackTarget()
    {
        if (attackTarget)
        {
            // 如果目标超出视野范围，清除目标
            if (Vector3.Distance(attackTarget.transform.position, transform.position) > viewRadius)
            {
                attackTarget = null;
            }
            // 如果目标玩家不再在水中，清除目标
            else if (!IsPlayerSwimming(attackTarget))
            {
                attackTarget = null;
            }
        }

        if (attackTarget == null) attackTarget = GetNearestAttackTargetInView();
    }

    // 检查玩家是否在水中游泳
    bool IsPlayerSwimming(Character character)
    {
        if (character == null) return false;
        
        // 检查是否是Player类型
        if (character is Player)
        {
            // 获取PlayerCtrl组件来检查isSwimming状态
            PlayerCtrl playerCtrl = character.GetComponent<PlayerCtrl>();
            if (playerCtrl != null)
            {
                return playerCtrl.isSwimming;
            }
        }
        
        return false;
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

        if (attributesModule != null)
        {
           int blood = (int)attributesModule.GetAttributeValue(AttributeType.Hp);

           attributesModule.SetAttributeValue(AttributeType.Hp, blood - damage);

           if (blood - damage <= 0)
           {
               live = false;
               Dying();
           }
        }
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


