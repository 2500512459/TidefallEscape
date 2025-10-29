using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Character����ɫ���ࣩ
/// ���пɱ���������Ľ�ɫ���󣨰��������AI����Ӧ�̳д��ࡣ
/// �ṩ��ɫע�ᡢ���塢��������ͳһ��ʼ���߼���
/// </summary>
public class Character : MonoBehaviour
{
    // ��ɫ�������������ã����ڲ��Ž�ɫ������
    protected Animator animator;

    // ��ɫ����������ã���������������ѧ���ƣ�
    protected Rigidbody rgBody;

    /// <summary>
    /// Unity�������ڣ�Awake()
    /// ������Start֮ǰ���ã�����������������ʼ�������չ�������չ��
    /// </summary>
    protected virtual void Awake()
    {

    }

    /// <summary>
    /// Unity�������ڣ�Start()
    /// ��Awake֮����ã������������߼������չ�������չ��
    /// </summary>
    protected virtual void Start()
    {

    }

    /// <summary>
    /// Unity�������ڣ�OnEnable()
    /// ��GameObject����ʱ�Զ����ã�
    /// - ��ȫ�� CharacterManager ע���Լ�
    /// - ��ȡ���������Rigidbody��Animator��
    /// </summary>
    protected virtual void OnEnable()
    {
        // ��ȡ����������ʵ��
        CharacterManager manager = CharacterManager.Instance;

        if (manager != null)
        {
            // ������ע�ᵽȫ�ֽ�ɫ�б���
            manager.Register(this);
        }
        else
        {
            Debug.Log("CharacterManager is Null!");
        }

        // �������
        rgBody = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Unity�������ڣ�OnDisable()
    /// ��GameObject���û�����ʱ�Զ����ã�
    /// - ��CharacterManager��ע�ᣨ��ֹ�ڴ������
    /// </summary>
    protected virtual void OnDisable()
    {
        CharacterManager manager = CharacterManager.Instance;

        if (manager != null)
        {
            manager.Unregister(this);
        }
    }

    /// <summary>
    /// Unity�������ڣ�Update()
    /// ÿ֡���ã������าдʵ���߼���
    /// </summary>
    protected virtual void Update()
    {

    }

    /// <summary>
    /// ��ȡ��ɫ��������
    /// </summary>
    public Rigidbody GetRigidBody() { return rgBody; }
}
