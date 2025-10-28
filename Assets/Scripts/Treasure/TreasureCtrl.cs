using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureCtrl : MonoBehaviour
{
    [Header("��Ʒ���ɿ�")]
    public LootContainerSO lootContainer;

    [Header("��ʾUI")]
    public TreasureHintUI HintUI;

    [Header("������")]
    [Tooltip("С�ڴ˾���ʱ��ʾUI")]
    public float detectDistance = 5f;

    private bool isUIVisible = false;
    private Transform player;

    private void Start()
    {
        // ����ʹ�� ShipDynamics ����� Transform
        if (PlayerCtrl.PlayerTransform != null)
        {
            player = PlayerCtrl.PlayerTransform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("[TreasureCtrl] δ�ҵ� Player �������� Inspector ���ֶ�ָ����");
        }

        // ��ʼ��UI
        if (HintUI != null)
            HintUI.Init(transform);  // �������Transform����
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        bool inRange = distance <= detectDistance;

        if (inRange && !isUIVisible)
        {
            isUIVisible = true;
            HintUI.ShowUI();
        }
        else if (!inRange && isUIVisible)
        {
            isUIVisible = false;
            HintUI.HideUI();
        }

        // ����������ֻ���ڽӽ�ʱ����Ч��
        if (inRange && Input.GetKeyDown(KeyCode.F))
        {
            OnTreasureOpened();
        }
    }

    private void OnTreasureOpened()
    {
        Debug.Log($"�򿪱��䣺{name}");
        // TODO: ��������뿪���䶯���������߼�����Ч�������ٴν�����
    }

#if UNITY_EDITOR
    // �� Scene ��ͼ�л��Ƽ�ⷶΧ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectDistance);
    }
#endif
}
