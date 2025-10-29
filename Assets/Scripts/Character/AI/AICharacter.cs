using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleverCrow.Fluid.BTs.Trees; // �ⲿAI��Ϊ���������

/// <summary>
/// AICharacter��AI��ɫ���ࣩ
/// �̳���Character����չ��AI��ص���������Ϊ��
/// - ��Ұ��Χ
/// - ������Χ
/// - ��Ϊ����BehaviorTree��
/// - ����ת���������Ϊ���
/// </summary>
public class AICharacter : Character
{
    // AI��Ұ�뾶���ܸ�֪�������루���ڼ�⸽��Ŀ�꣩
    [Range(0.1f, 100)]
    public float viewRadius = 10;

    // �����뾶���ܽ��й����ľ�����ֵ
    [Range(0.1f, 100)]
    public float attackRadius = 1;

    // AI����Ϊ���߼����������ⲿ��� Fluid Behavior Tree��
    [SerializeField]
    protected BehaviorTree brain;

    // ����AI�˶����
    protected SteeringBehaviors steeringBehaviors;   // �����ƶ������������ں�
    protected WanderBehavior wander;                 // �������
    protected PursueBehavior pursue;                 // ׷��Ŀ��
    protected CollisionSensor colsensor;             // ��ײ��֪����ֹײǽ��
    protected SeparationBehavior separation;         // ������Ϊ������������AI�ص���

    // ���״̬��ʶ
    protected bool live = true;

    /// <summary>
    /// ��ʼ��AI��ɫ����������AI��Ϊ�����
    /// </summary>
    protected override void Start()
    {
        base.Start();

        // ����AI��Ϊ������
        steeringBehaviors = GetComponent<SteeringBehaviors>();
        wander = GetComponent<WanderBehavior>();
        pursue = GetComponent<PursueBehavior>();
        colsensor = GetComponent<CollisionSensor>();
        separation = GetComponent<SeparationBehavior>();
    }

    /// <summary>
    /// ��ȡ��Χ�ڵ����н�ɫ
    /// ���޹��ˣ�
    /// </summary>
    public List<Character> GetCharactersInView()
    {
        if (CharacterManager.Instance)
        {
            return CharacterManager.Instance.GetCharactersWithinRange(this, transform.position, viewRadius);
        }

        // ���޹��������򷵻ؿ��б�
        return new List<Character>();
    }

    /// <summary>
    /// ��ȡ��Χ�ڵ����н�ɫ�������͹�������
    /// ���磺ֻ�����˻����
    /// </summary>
    public List<Character> GetCharactersInView(CharacterTypeFilter typeFilter)
    {
        if (CharacterManager.Instance)
        {
            return CharacterManager.Instance.GetCharactersWithinRange(this, transform.position, viewRadius, typeFilter);
        }

        return new List<Character>();
    }
}
