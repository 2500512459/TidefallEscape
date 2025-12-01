using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(Rigidbody))]
public class PlayerShipCtrl : MonoBehaviour
{
    public InventoryContext setContext = InventoryContext.Default;  // 当前场景上下文
    public PlayerCamera PlayerCamera;
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
    private bool isDriving = false;                        // 是否正在驾驶

    private Rigidbody rigidbodyComponent;

    // 当前可交互的宝箱集合（由触发器自动维护）
    private readonly List<TreasureBox> nearbyBoxes = new();
    // 当前高亮的宝箱（最近的那个）
    private TreasureBox highlightedBox;

    // 当前可交互的商店集合
    private readonly List<ShopShip> nearbyShops = new();
    private ShopShip highlightedShop;
    private SphereCollider trigger;
    private GameObject currentPlayer;   // 当前玩家
    private ShipRudderInteractable shipRudderInteractable; // 舵轮交互对象引用
    [SerializeField] private GameObject DrivingModel;
    [SerializeField] public Transform DrivingPos;
    [SerializeField] private Transform CameraPos;

    private void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        PlayerCamera.target = CameraPos;
        if(PlayerCamera.cameraMode != PlayerCamera.CameraMode.ThirdPerson)
            PlayerCamera.SwitchCamera();
        
        // 设置开船状态
        PlayerCamera.isSailing = true;

        // 启用时添加触发器
        if (trigger == null)
        {
            trigger = GetComponent<SphereCollider>();
            if (trigger == null)
                trigger = gameObject.AddComponent<SphereCollider>();
        }

        trigger.isTrigger = true;
        trigger.radius = lootDetectRadius;
        trigger.enabled = true;

        // 注册输入事件
        PlayerInput.Instance.OnInteractionEvent += HandleInteract;
        PlayerInput.Instance.OpenInventoryEvent += TryOpenInventory;
        PlayerInput.Instance.LootPressedEvent += TryOpenTreasureBox;
    }


    private void OnDisable()
    {
        // 设置开船状态为false
        PlayerCamera.isSailing = false;
        
        // 禁用时移除或关闭触发器
        if (trigger != null)
            trigger.enabled = false;
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.OnInteractionEvent -= HandleInteract;
            PlayerInput.Instance.OpenInventoryEvent -= TryOpenInventory;
            PlayerInput.Instance.LootPressedEvent -= TryOpenTreasureBox;
        }
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
        if (!PlayerInput.Instance.isInventoryOpen)
        {
            // 读取移动输入
            verticalImpetus = PlayerInput.Instance.AxesY;
            horizontalImpetus = PlayerInput.Instance.AxesX;
            isBoosting = PlayerInput.Instance.Sprint;
            // 检测最近交互对象
            UpdateNearestTreasure();
            UpdateNearestShop();
        }
    }
    
    private void HandleInteract()
    {
        ExitControl();
    }

    /// <summary>
    /// 进入驾驶状态：由交互逻辑调用
    /// </summary>
    public void EnterControl(GameObject player, ShipRudderInteractable rudderInteractable = null)
    {
        currentPlayer = player;
        shipRudderInteractable = rudderInteractable; // 保存引用以便退出时关闭模型
        // 可在这里启用驾驶UI等
        InteractHintUI.Instance.ShowHint("停止驾驶", "E");
        isDriving = true;

        // 通知 CharacterManager：距离计算的中心切换为船
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.playerTransform = transform;
        }
    }

    /// <summary>
    /// 退出驾驶状态：还原到玩家控制
    /// </summary>
    public void ExitControl()
    {
        if (!isDriving)
            return;
        isDriving = false;
        
        // 先隐藏"停止驾驶"提示
        InteractHintUI.Instance.HideHint();
        
        // 禁用自己（关闭船控制）
        enabled = false;
        rigidbodyComponent.velocity = Vector3.zero; // 停止船只移动
        // 禁用武器指示器
        var weaponInDicator = GetComponentInParent<WeaponIndicator>();
        if (weaponInDicator != null)
        {
            weaponInDicator.enabled = false;
        }
        // 冻结船只旋转
        rigidbodyComponent.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (DrivingModel != null)
        {
            DrivingModel.SetActive(false);
        }
        
        // 关闭职业模型
        if (shipRudderInteractable != null)
        {
            shipRudderInteractable.DisableAllModels();
        }

        // 恢复玩家对象
        if (currentPlayer != null)
        {
            currentPlayer.transform.position = DrivingPos.position;
            currentPlayer.SetActive(true);

            // 通知 CharacterManager：距离计算的中心切换回玩家
            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.playerTransform = currentPlayer.transform;
            }
            
            // 根据职业设置相机目标，使用 PlayerCamera 中已配置的引用字段
            if (PlayerCamera != null)
            {
                Transform targetTransform = null;
                
                if(PlayerDataManager.Instance.SelectedProfession == ProfessionType.Crewman)
                {
                    targetTransform = PlayerCamera.CrewmanCameraPosition;
                }
                else if(PlayerDataManager.Instance.SelectedProfession == ProfessionType.Lookout)
                {
                    targetTransform = PlayerCamera.LookoutCameraPosition;
                }
                else if(PlayerDataManager.Instance.SelectedProfession == ProfessionType.Captain)
                {
                    targetTransform = PlayerCamera.CaptainCameraPosition;
                }
                else if(PlayerDataManager.Instance.SelectedProfession == ProfessionType.Shipwright)
                {
                    targetTransform = PlayerCamera.ShipwrightCameraPosition;
                }
                
                // 设置相机目标（如果找到了有效的 Transform）
                if (targetTransform != null)
                {
                    PlayerCamera.target = targetTransform;
                }
                else
                {
                    Debug.LogWarning($"无法找到 {PlayerDataManager.Instance.SelectedProfession} 职业的相机位置，请检查 PlayerCamera 组件配置或 Player 预制体结构");
                }
            }
            
            var playerComponent = currentPlayer.GetComponent<Player>();
            if (playerComponent != null)
            {
                playerComponent.SetVitalityBarVisible(true);
            }
        }
    }
    // ===================== Tab键 打开/关闭背包 =====================
    public void TryOpenInventory(bool isOpen)
    {
        // 优先检查商店是否打开
        var shopPanel = ShopUI.Instance;
        if (shopPanel != null && shopPanel.IsVisible)
        {
            highlightedShop.HideShopUI();
            return;
        }

        if (isOpen)
        {
            InventoryManager.Instance.currenContext = setContext;
            InventoryUI.Instance?.ShowPanel();
        }
        else
        {
            InventoryUI.Instance?.HidePanel();
        }
    }

    // ===================== F键 打开宝箱 =====================
    private void TryOpenTreasureBox()
    {
        // 优先商店
        if (highlightedShop != null)
        {
            var shopPanel = ShopUI.Instance;
            if (shopPanel == null)
            {
                Debug.LogWarning("ShopUI 实例不存在，无法交互");
                return;
            }

            if (shopPanel.IsVisible)
            {
                highlightedShop.HideShopUI();
                PlayerInput.Instance.EnableAllInputs();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                highlightedShop.ShowShopUI();
                PlayerInput.Instance.DisableAllInputsExcept(PlayerInput.Instance.OpenEventInput);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }
        
        // 检查是否已经打开了宝箱界面（如果是则关闭）
        if (PlayerInput.Instance.isLootOpen)
        {
            PlayerInput.Instance.isInventoryOpen = false;
            PlayerInput.Instance.isLootOpen = false;
            InventoryUI.Instance?.HidePanel();
            // 恢复输入
            PlayerInput.Instance.EnableAllInputs();
            // 恢复鼠标锁定
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 恢复 Context
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.RestoreContext();
            }
            
            // 记录当前播放进度到宝箱
            if (highlightedBox != null)
            {
                highlightedBox.lastPlayedMaskIndex = StorageItem.GetCurrentPlayedIndex();
            }

            // 清理宝箱遮罩播放队列，防止下一个宝箱动画出错
            StorageItem.ClearPlayedMaskRecords();

            // 重新显示提示
            if (highlightedBox != null)
                highlightedBox.ShowHint();

            return;
        }

        // 然后宝箱
        if (highlightedBox != null)
        {
            Debug.Log($"打开最近的宝箱：{highlightedBox.name}");
            highlightedBox.TryOpen();
            highlightedBox.HideHint();
    
            PlayerInput.Instance.isInventoryOpen = true;
            PlayerInput.Instance.isLootOpen = true;
            PlayerInput.Instance.DisableAllInputsExcept(PlayerInput.Instance.OpenInventoryInput, PlayerInput.Instance.OpenEventInput);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
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
    // ===================== 更新最近商店船只 =====================
    private void UpdateNearestShop()
    {
        if (nearbyShops.Count == 0)
        {
            if (highlightedShop != null)
            {
                highlightedShop.HideHint();
                highlightedShop = null;
            }
            return;
        }

        ShopShip nearest = null;
        float minDist = float.MaxValue;
        Vector3 playerPos = transform.position;

        foreach (var shop in nearbyShops)
        {
            if (shop == null || !shop.gameObject.activeInHierarchy) continue;
            float dist = Vector3.Distance(playerPos, shop.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = shop;
            }
        }

        if (highlightedShop != nearest)
        {
            if (highlightedShop != null)
                highlightedShop.HideHint();

            highlightedShop = nearest;

            if (highlightedShop != null)
                highlightedShop.ShowHint();
        }
    }
    // ===================== Trigger 检测（进入/离开） =====================
    private void OnTriggerEnter(Collider other)
    {
        var box = other.GetComponent<TreasureBox>();
        if (box != null && !nearbyBoxes.Contains(box))
            nearbyBoxes.Add(box);

        var shop = other.GetComponent<ShopShip>();
        if (shop != null && !nearbyShops.Contains(shop))
            nearbyShops.Add(shop);
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

        var shop = other.GetComponent<ShopShip>();
        if (shop != null)
        {
            nearbyShops.Remove(shop);
            if (highlightedShop == shop)
            {
                highlightedShop.HideHint();
                highlightedShop = null;
            }
        }
    }
}
