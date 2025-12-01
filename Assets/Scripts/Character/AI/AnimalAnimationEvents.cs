using UnityEngine;

public class AnimalAnimationEvents : MonoBehaviour
{
    private Wolf wolfController;

    private void Start()
    {
        // 尝试获取父物体或自身上的 Wolf 组件
        wolfController = GetComponentInParent<Wolf>();
        if (wolfController == null)
        {
            wolfController = GetComponent<Wolf>();
        }
    }

    // 动画事件：攻击
    public void Attack()
    {
        if (wolfController != null)
        {
            wolfController.OnAttackHitCheck();
        }
    }
}

