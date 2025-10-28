using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureCtrl : MonoBehaviour
{
    [Header("物品生成库")]
    public LootContainerSO lootContainer;

    [Header("提示UI")]
    public TreasureHintUI HintUI;

    [Header("检测参数")]
    [Tooltip("小于此距离时显示UI")]
    public float detectDistance = 5f;

    private bool isUIVisible = false;
    private Transform player;

    private void Start()
    {
        // 优先使用 ShipDynamics 缓存的 Transform
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
                Debug.LogWarning("[TreasureCtrl] 未找到 Player 对象，请在 Inspector 中手动指定。");
        }

        // 初始化UI
        if (HintUI != null)
            HintUI.Init(transform);  // 将宝箱的Transform传入
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

        // 按键交互（只有在接近时才生效）
        if (inRange && Input.GetKeyDown(KeyCode.F))
        {
            OnTreasureOpened();
        }
    }

    private void OnTreasureOpened()
    {
        Debug.Log($"打开宝箱：{name}");
        // TODO: 在这里加入开宝箱动画、掉落逻辑、音效、禁用再次交互等
    }

#if UNITY_EDITOR
    // 在 Scene 视图中绘制检测范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectDistance);
    }
#endif
}
