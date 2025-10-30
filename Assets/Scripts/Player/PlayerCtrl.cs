using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCtrl : MonoBehaviour
{
    public InventoryContext setContext = InventoryContext.Default;  // 当前场景上下文

    [Header("交互检测参数")]
    public float lootDetectRadius = 10f;     // 探测范围（与 SphereCollider 半径一致）

    [Header("动力参数")]
    [SerializeField] private float maxImpetus = 2000f;          // 动力系数
    [SerializeField] private float backwardSpeedFactor = 0.5f;  // 后退系数
    [SerializeField] private float turningFactor = 1.0f;        // 转向系数
    [SerializeField] private float boostValue = 2.0f;                  // 加速倍率
    private float verticalImpetus = 0f;                         // 键盘上下输入
    private float horizontalImpetus = 0f;                       // 键盘左右输入
    private float force = 0f;                                   // 当前施加动力

    private bool isBoosting = false;                            // 是否加速

    private Rigidbody rigidbodyComponent;

    // 当前可交互的宝箱集合（由触发器自动维护）
    private readonly List<TreasureBox> nearbyBoxes = new();
    // 当前高亮的宝箱（最近的那个）
    private TreasureBox highlightedBox;

    private void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();

        // 注册输入事件
        InputManager.Instance.OpenInventoryEvent += TryOpenInventory;
        InputManager.Instance.LootPressedEvent += TryOpenTreasureBox;

        // 确保存在检测用的 SphereCollider
        SphereCollider trigger = GetComponent<SphereCollider>();
        if (trigger == null)
            trigger = gameObject.AddComponent<SphereCollider>();

        trigger.isTrigger = true;
        trigger.radius = lootDetectRadius;
    }

    private void FixedUpdate()
    {
        // ===================== 移动逻辑 =====================
        force = 0;

        // 基础推力
        float currentImpetus = maxImpetus;
        if (isBoosting)
            currentImpetus *= boostValue;

        if (verticalImpetus > 0)
            force = verticalImpetus * currentImpetus;
        else if (verticalImpetus < 0)
            force = verticalImpetus * currentImpetus * backwardSpeedFactor;

        rigidbodyComponent.AddRelativeForce(Vector3.forward * force);

        // ===================== 转向逻辑 =====================
        float rotationAngle = horizontalImpetus * turningFactor;
        if (verticalImpetus < 0)
            rotationAngle *= -1; // 倒车时反向转向

        Quaternion currentRotation = rigidbodyComponent.rotation;
        Vector3 angle = currentRotation.eulerAngles;
        angle.y += rotationAngle * Time.fixedDeltaTime * 50f;
        angle.y %= 360.0f;

        rigidbodyComponent.MoveRotation(Quaternion.Euler(angle));
    }

    private void Update()
    {
        if (!InputManager.Instance.isInventoryOpen)
        {
            // 读取移动输入
            verticalImpetus = InputManager.Instance.inputMove.y;
            horizontalImpetus = InputManager.Instance.inputMove.x;
            isBoosting = InputManager.Instance.isBoosting;
            // 检测最近宝箱
            UpdateNearestTreasure();
        }
    }

    // ===================== Tab键 打开/关闭背包 =====================
    public void TryOpenInventory(bool isOpen)
    {
        if (isOpen)
        {
            InventoryManager.Instance.currenContext = setContext;
            UIManger.Instance.ShowPanel<InventoryPanel>();
        }
        else
        {
            UIManger.Instance.HidePanel<InventoryPanel>();
        }
    }

    // ===================== F键 打开宝箱 =====================
    private void TryOpenTreasureBox()
    {
        if (highlightedBox == null) return;

        Debug.Log($"打开最近的宝箱：{highlightedBox.name}");
        highlightedBox.TryOpen();

        InputManager.Instance.isInventoryOpen = true;
        InputManager.Instance.isLootOpen = true;
    }

    // ===================== 更新最近宝箱显示提示 =====================
    private void UpdateNearestTreasure()
    {
        if (nearbyBoxes.Count == 0)
        {
            if (highlightedBox != null)
            {
                highlightedBox.HideHint();
                highlightedBox = null;
            }
            return;
        }

        TreasureBox nearest = null;
        float minDist = float.MaxValue;
        Vector3 playerPos = transform.position;

        foreach (var box in nearbyBoxes)
        {
            if (box == null || !box.gameObject.activeInHierarchy) continue;
            float dist = Vector3.Distance(playerPos, box.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = box;
            }
        }

        if (highlightedBox != nearest)
        {
            if (highlightedBox != null)
                highlightedBox.HideHint();

            highlightedBox = nearest;

            if (highlightedBox != null)
                highlightedBox.ShowHint();
        }
    }

    // ===================== Trigger 检测（进入/离开） =====================
    private void OnTriggerEnter(Collider other)
    {
        var box = other.GetComponent<TreasureBox>();
        if (box != null && !nearbyBoxes.Contains(box))
            nearbyBoxes.Add(box);
    }

    private void OnTriggerExit(Collider other)
    {
        var box = other.GetComponent<TreasureBox>();
        if (box != null)
        {
            nearbyBoxes.Remove(box);
            if (highlightedBox == box)
            {
                highlightedBox.HideHint();
                highlightedBox = null;
            }
        }
    }
}
