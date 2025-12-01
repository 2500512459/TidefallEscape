using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 射程指示器类型
/// </summary>
public enum RangeIndicatorType
{
    Circle,  // 圆形
    Sector   // 扇形
}

/// <summary>
/// 武器指示器（自驱动版本）
/// - 自动显示榴弹抛物线和射程范围
/// - 自动计算鼠标目标点、飞行速度
/// - 按下左键自动发射炮弹
/// </summary>
public class WeaponIndicator : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("用于射线检测的层")]
    public LayerMask mask;

    [Tooltip("发射点（中心，圆形模式时使用）")]
    public Transform firePoint;

    [Tooltip("发射点（左侧，扇形模式时使用）")]
    public Transform firePointLeft;
    [Tooltip("发射点（右侧，扇形模式时使用）")]
    public Transform firePointRight;


    [Tooltip("射程指示器类型")]
    public RangeIndicatorType rangeIndicatorType = RangeIndicatorType.Circle;

    [Tooltip("扇形角度范围（度，扇形模式时使用）")]
    public float sectorAngle = 90f;

    [Tooltip("抛物线初始速度（m/s）")]
    public float parabolaInitVelocity = 20f;

    [Tooltip("是否自动隐藏指示器（无输入时）")]
    public bool autoHide = true;

    [Header("材质")]
    [SerializeField] private Material materialBase;
    [SerializeField] private Material materialParabola;

    [Header("炮弹预制体")]
    [SerializeField] private GameObject cannonBallPrefab;  // 普通弹预制体
    [SerializeField] private GameObject armorPiercingCannonBallPrefab;  // 穿甲弹预制体

    [Header("炮弹物品数据")]
    [SerializeField] private ItemDataSO normalCannonballData;
    [SerializeField] private ItemDataSO armorPiercingCannonballData;

    [Header("炮库")]
    [Tooltip("存放炮弹的库存")]
    [SerializeField] private InventoryDataSO cannonAmmoInventory;

    // 对外属性
    public Vector3 TargetPosition { get; private set; }           // 目标位置
    public float ParabolaRunVelocity { get; private set; }        // 抛物线运行速度

    // 内部成员
    private Transform indicator;          // 抛物线指示器
    private Transform rangeIndicator;     // 当前使用的范围指示器（圆形或左侧扇形）
    private Transform rangeIndicatorRight; // 当前使用的右侧扇形指示器（仅扇形模式使用）
    
    // 预创建的指示器引用
    private Transform circleRef;
    private Transform sectorLeftRef;
    private Transform sectorRightRef;

    private MeshRenderer parabolaRenderer; 
    private MaterialPropertyBlock parabolaBlock;  // 材质属性块，用于动态修改材质参数
    private Plane intersectionPlane = new Plane(Vector3.up, Vector3.zero);  // 用于鼠标射线检测的平面
    private Vector3 lastValidPosition = Vector3.zero;  // 上一次有效的鼠标位置
    private float range;  // 武器最大射程
    private bool isAiming;
    private void OnEnable()
    {
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.IsAttackedEvent += IsAimingChanged;
            PlayerInput.Instance.OnSwitchWeaponEvent += OnSwitchWeapon;
        }
    }
    private void OnDisable()
    {
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.IsAttackedEvent -= IsAimingChanged;
            PlayerInput.Instance.OnSwitchWeaponEvent -= OnSwitchWeapon;
        }
    }
    void IsAimingChanged(bool isAiming)
    {
        this.isAiming = isAiming;
    }
    void Start()
    {
        parabolaBlock = new MaterialPropertyBlock();

        // 1. 创建圆形指示器引用
        circleRef = CreateIndicator("RangeCircle", IndicatorGeometry.CreateCircleEdgeMesh(0.98f, 1, 60), materialBase);
        circleRef.gameObject.SetActive(false);

        // 2. 创建扇形指示器引用
        float halfAngle = sectorAngle * 0.5f;
        sectorLeftRef = CreateIndicator("RangeSectorLeft", IndicatorGeometry.CreateSectorOutlineMesh(1f, 0.02f, 90 - halfAngle, 90 + halfAngle, 30), materialBase);
        sectorLeftRef.gameObject.SetActive(false);
            
        if (firePointRight != null)
        {
            sectorRightRef = CreateIndicator("RangeSectorRight", IndicatorGeometry.CreateSectorOutlineMesh(1f, 0.02f, 90 - halfAngle, 90 + halfAngle, 30), materialBase);
            sectorRightRef.gameObject.SetActive(false);
        }

        // 3. 初始化当前指示器状态
        UpdateIndicatorTypeState();

        // 创建抛物线指示器（平面网格，用于显示预测的弹道轨迹）
        indicator = CreateIndicator("Grenade", IndicatorGeometry.CreatePlaneMesh(60, 4), materialParabola);
        parabolaRenderer = indicator.GetComponent<MeshRenderer>();
        indicator.gameObject.SetActive(false);

        // 计算并设置武器射程
        SetParabolaInitVel(parabolaInitVelocity);
    }

    void UpdateIndicatorTypeState()
    {
        // 隐藏所有引用
        if (circleRef) circleRef.gameObject.SetActive(false);
        if (sectorLeftRef) sectorLeftRef.gameObject.SetActive(false);
        if (sectorRightRef) sectorRightRef.gameObject.SetActive(false);

        // 根据当前类型分配 active 的引用
        if (rangeIndicatorType == RangeIndicatorType.Circle)
        {
            rangeIndicator = circleRef;
            rangeIndicatorRight = null; 
        }
        else
        {
            rangeIndicator = sectorLeftRef;
            rangeIndicatorRight = sectorRightRef;
        }
        // Update() 中会根据 isAiming 处理它们的显示/隐藏
    }

    void OnSwitchWeapon()
    {
        rangeIndicatorType = rangeIndicatorType == RangeIndicatorType.Circle ? RangeIndicatorType.Sector : RangeIndicatorType.Circle;
        UpdateIndicatorTypeState();
    }

    void Update()
    {
        // 检查是否正在瞄准（通过输入管理器判断）
        if (!isAiming)
        {
            // 如果没有瞄准且启用了自动隐藏，则隐藏所有指示器
            if (autoHide)
            {
                indicator.gameObject.SetActive(false);
                rangeIndicator.gameObject.SetActive(false);
                if (rangeIndicatorRight != null)
                    rangeIndicatorRight.gameObject.SetActive(false);
            }
            return;
        }
        // 显示指示器
        indicator.gameObject.SetActive(true);
        rangeIndicator.gameObject.SetActive(true);
        if (rangeIndicatorRight != null)
            rangeIndicatorRight.gameObject.SetActive(true);

        // 更新指示器的位置和旋转
        UpdateTransform();
        // 更新手榴弹抛物线指示器
        UpdateGrenadeIndicator();

        // 当按下鼠标左键时发射炮弹
        if (PlayerInput.Instance.Fire)
        {
            FireCannon();
        }
    }

    /// <summary>
    /// 更新抛物线指示器
    /// </summary>
    void UpdateGrenadeIndicator()
    {
        // 确定当前应该基于哪个发射点
        Transform currentOriginTransform = firePoint;
        Vector3 targetPosRaw = GetMouseRayIntersectionWithXZPlane(currentOriginTransform != null ? currentOriginTransform.position : transform.position);

        if (rangeIndicatorType == RangeIndicatorType.Sector)
        {
            // 如果是扇形模式，选择最近的发射点
            if (firePointLeft != null && firePointRight != null)
            {
                float distToLeft = Vector3.Distance(targetPosRaw, firePointLeft.position);
                float distToRight = Vector3.Distance(targetPosRaw, firePointRight.position);
                currentOriginTransform = distToLeft < distToRight ? firePointLeft : firePointRight;
            }
            else if (firePointLeft != null)
                currentOriginTransform = firePointLeft;
            else if (firePointRight != null)
                currentOriginTransform = firePointRight;
        }

        if (currentOriginTransform == null) currentOriginTransform = transform;
        Vector3 origin = currentOriginTransform.position;

        // 重新获取基于选定发射点的交点（虽然通常差别不大，但为了精确性）
        TargetPosition = GetMouseRayIntersectionWithXZPlane(origin);

        // 计算水平方向上的向量和距离
        Vector3 dir = TargetPosition - origin;
        dir.y = 0;
        float distance = dir.magnitude;
        
        // 如果目标距离超过最大射程，则限制在最大射程内
        if (distance > range)
        {
            distance = range;
            TargetPosition = origin + dir.normalized * range;
            // 更新方向向量
            dir = TargetPosition - origin; 
        }

        // 扇形角度限制逻辑
        if (rangeIndicatorType == RangeIndicatorType.Sector)
        {
            // 将方向转换为发射点的局部空间
            Vector3 localDir = currentOriginTransform.InverseTransformDirection(dir);
            // 计算角度（Atan2(x, z) 返回 0 表示 +Z(前)，90 表示 +X(右)）
            float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            
            float halfAngle = sectorAngle * 0.5f;
            // 直接限制在 [-half, half] 范围内（即限制在局部前方扇区）
            // 因为我们已经统一了指示器网格朝向局部前方，并且假定发射点 Transform 已经旋转到了目标朝向
            float clampedAngle = Mathf.Clamp(angle, -halfAngle, halfAngle);

            // 如果角度被修正了，重新计算目标位置
            if (!Mathf.Approximately(angle, clampedAngle))
            {
                // 根据修正后的角度重构局部方向向量
                float rad = clampedAngle * Mathf.Deg2Rad;
                Vector3 newLocalDir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * distance; // 保持原距离
                
                // 转回世界空间
                Vector3 newWorldDir = currentOriginTransform.TransformDirection(newLocalDir);
                TargetPosition = origin + newWorldDir;
                dir = newWorldDir; // 更新dir用于后续LookAt
            }
        }

        // 根据目标距离计算实际发射速度（距离越远速度越快）
        ParabolaRunVelocity = parabolaInitVelocity * Mathf.Sqrt(distance / range);

        // 更新抛物线材质的速度参数，用于着色器计算抛物线形状
        parabolaBlock.SetFloat("_LaunchVelocity", ParabolaRunVelocity);
        parabolaRenderer.SetPropertyBlock(parabolaBlock);

        // 调整抛物线指示器的变换
        indicator.position = origin;
        indicator.localScale = new Vector3(0.1f, 1, distance);  // X轴缩放控制宽度，Z轴缩放控制长度
        indicator.LookAt(TargetPosition);  // 朝向目标点
        rangeIndicator.localScale = Vector3.one * range;  // 设置射程指示器大小为最大射程

        if (rangeIndicatorRight != null)
        {
            rangeIndicatorRight.localScale = Vector3.one * range;
        }
    }

    /// <summary>
    /// 更新指示器的基础变换（位置和旋转）
    /// </summary>
    void UpdateTransform()
    {
        // 更新抛物线指示器位置（暂时跟随中心发射点，如果需要跟随最近发射点可进一步优化）
        if (firePoint != null)
        {
            indicator.position = firePoint.position;
        }
        else if (firePointLeft != null) // 如果没有中心点，尝试使用左侧点
        {
             indicator.position = firePointLeft.position;
        }

        if (rangeIndicatorType == RangeIndicatorType.Circle)
        {
            // 圆形模式：跟随中心发射点，保持世界朝向（不随船旋转）
            if (firePoint != null)
            {
                rangeIndicator.position = firePoint.position;
            }
            rangeIndicator.rotation = Quaternion.identity;
        }
        else // Sector
        {
            // 扇形模式：左右指示器分别跟随各自的发射点，并跟随发射点的旋转
            
            // 1. 更新左侧扇形指示器
            if (firePointLeft != null)
            {
                rangeIndicator.position = firePointLeft.position;
                rangeIndicator.rotation = firePointLeft.rotation;
            }
            else if (firePoint != null)
            {
                rangeIndicator.position = firePoint.position;
                rangeIndicator.rotation = firePoint.rotation;
            }

            // 2. 更新右侧扇形指示器
            if (rangeIndicatorRight != null)
            {
                if (firePointRight != null)
                {
                    rangeIndicatorRight.position = firePointRight.position;
                    rangeIndicatorRight.rotation = firePointRight.rotation;
                }
                else if (firePoint != null)
                {
                    rangeIndicatorRight.position = firePoint.position;
                    rangeIndicatorRight.rotation = firePoint.rotation;
                }
            }
        }
    }

    /// <summary>
    /// 获取鼠标射线与XZ平面的交点
    /// </summary>
    /// <param name="point">参考点，用于确定XZ平面的高度</param>
    /// <returns>交点位置</returns>
    Vector3 GetMouseRayIntersectionWithXZPlane(Vector3 point)
    {
        // 从主摄像机向鼠标位置发射射线
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // 设置XZ平面（法线为Y轴向上，通过指定点）
        intersectionPlane.SetNormalAndPosition(Vector3.up, point);
        // 计算射线与平面的交点
        if (intersectionPlane.Raycast(ray, out float enter))
        {
            lastValidPosition = ray.GetPoint(enter);
            return lastValidPosition;
        }
        // 如果没有交点，返回上次的有效位置
        return lastValidPosition;
    }

    /// <summary>
    /// 根据初速度计算并设置武器射程
    /// </summary>
    /// <param name="velocity">抛射初速度</param>
    void SetParabolaInitVel(float velocity)
    {
        float G = 9.8f;  // 重力加速度
        // 根据抛体运动公式计算最大射程（45度角发射时最远）
        range = velocity * velocity / G;
        parabolaInitVelocity = velocity;
    }

    /// <summary>
    /// 创建指示器游戏对象
    /// </summary>
    /// <param name="name">对象名称</param>
    /// <param name="mesh">网格</param>
    /// <param name="mat">材质</param>
    /// <returns>指示器的Transform组件</returns>
    Transform CreateIndicator(string name, Mesh mesh, Material mat)
    {
        GameObject obj = new GameObject(name);
        obj.layer = gameObject.layer;
        var mf = obj.AddComponent<MeshFilter>();   // 网格过滤器，用于存储网格数据
        mf.mesh = mesh;
        var mr = obj.AddComponent<MeshRenderer>(); // 网格渲染器，用于渲染网格
        mr.sharedMaterial = mat;
        obj.transform.SetParent(transform);        // 设置为当前对象的子对象
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localEulerAngles = Vector3.zero;
        return obj.transform;
    }

    /// <summary>
    /// 计算发射方向（带45°仰角）
    /// </summary>
    /// <param name="origin">发射源点位置</param>
    /// <returns>标准化的发射方向向量</returns>
    Vector3 GetShootDirection(Vector3 origin)
    {
        Vector3 dir = TargetPosition - origin;
        dir.y = 0; // 保持水平指向目标
        // 如果距离过近，则直接向前发射
        if (dir.sqrMagnitude < 0.001f) return firePoint != null ? firePoint.forward : transform.forward;
    
        // 计算基础朝向（水平指向目标）
        Quaternion baseRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        // 加上固定仰角（向上45度）得到最终发射方向
        Quaternion launchRot = baseRot * Quaternion.Euler(-45f, 0, 0);
    
        return launchRot * Vector3.forward;
    }

    /// <summary>
    /// 从炮库中查找并获取一个炮弹
    /// </summary>
    /// <returns>找到的炮弹ItemStack，如果没找到则返回null</returns>
    ItemStack GetCannonballFromInventory()
    {
        if (cannonAmmoInventory == null || cannonAmmoInventory.items == null)
            return null;

        // 遍历炮库，查找第一个类型为Cannonball的物品
        foreach (var stack in cannonAmmoInventory.items)
        {
            if (stack != null && stack.item != null && stack.item.type == ItemType.Cannonball && stack.count > 0)
            {
                return stack;
            }
        }
        return null;
    }

    /// <summary>
    /// 根据炮弹类型获取对应的预制体
    /// </summary>
    /// <param name="cannonballType">炮弹类型</param>
    /// <returns>对应的预制体，如果未找到则返回普通弹预制体</returns>
    GameObject GetCannonballPrefab(CannonballType cannonballType)
    {
        switch (cannonballType)
        {
            case CannonballType.Normal:
                return cannonBallPrefab;
            case CannonballType.ArmorPiercing:
                return armorPiercingCannonBallPrefab != null ? armorPiercingCannonBallPrefab : cannonBallPrefab;
            default:
                return cannonBallPrefab;
        }
    }

    /// <summary>
    /// 从炮库中消耗一个炮弹
    /// </summary>
    /// <param name="item">要消耗的炮弹ItemDataSO</param>
    void ConsumeCannonball(ItemDataSO item)
    {
        if (cannonAmmoInventory == null || item == null)
            return;

        // 查找该物品在炮库中的位置
        for (int i = 0; i < cannonAmmoInventory.items.Count; i++)
        {
            var stack = cannonAmmoInventory.items[i];
            if (stack != null && stack.item == item && stack.count > 0)
            {
                // 减少数量
                stack.count--;
                // 如果数量为0，移除该物品
                if (stack.count <= 0)
                {
                    cannonAmmoInventory.items.RemoveAt(i);
                }
                // 通知库存更新
                InventoryManager.Instance.OnInventoryChanged(cannonAmmoInventory.type);
                break;
            }
        }
    }

    /// <summary>
    /// 发射炮弹
    /// </summary>
    void FireCannon()
    {
        // 检查炮库中是否有炮弹
        ItemStack cannonballStack = GetCannonballFromInventory();
        if (cannonballStack == null || cannonballStack.item == null)
        {
            Debug.LogWarning("[WeaponIndicator] 炮库中没有炮弹！");
            return;
        }

        // 根据炮弹类型选择对应的预制体
        GameObject prefabToUse = GetCannonballPrefab(cannonballStack.item.cannonballType);
        if (prefabToUse == null)
        {
            Debug.LogWarning("[WeaponIndicator] 未找到对应的炮弹预制体！");
            return;
        }

        // 根据模式选择发射点
        Transform currentFirePoint = firePoint;
        if (rangeIndicatorType == RangeIndicatorType.Sector)
        {
            // 扇形模式：选择距离目标最近的发射点
            if (firePointLeft != null && firePointRight != null)
            {
                float distToLeft = Vector3.Distance(TargetPosition, firePointLeft.position);
                float distToRight = Vector3.Distance(TargetPosition, firePointRight.position);
                currentFirePoint = distToLeft < distToRight ? firePointLeft : firePointRight;
            }
            else if (firePointLeft != null)
            {
                currentFirePoint = firePointLeft;
            }
            else if (firePointRight != null)
            {
                currentFirePoint = firePointRight;
            }
        }
        
        if (currentFirePoint == null) return;

        // 在发射点位置实例化炮弹
        GameObject obj = Instantiate(prefabToUse, currentFirePoint.position, Quaternion.identity);

        // 设置伤害源所有者 (玩家船只)
        // 假设 WeaponIndicator 是挂在玩家船只的子物体上，或者直接挂在船上
        GameObject owner = transform.root.gameObject; // 或者 GetComponentInParent<PlayerShip>()?.gameObject
        DamageVolume dv = obj.GetComponent<DamageVolume>();
        if (dv == null) dv = obj.GetComponentInChildren<DamageVolume>();
        if (dv != null)
        {
            dv.Setup(owner);
        }

        CannonBall ball = obj.GetComponent<CannonBall>();
        // 如果炮弹有CannonBall脚本，则设置速度和发射方向
        if (ball != null)
        {
            ball.speed = ParabolaRunVelocity;
            ball.Launch(GetShootDirection(currentFirePoint.position));
        }

        // 消耗一个炮弹
        ConsumeCannonball(cannonballStack.item);
    }

    public void LoadingCannon(int count = 10, CannonballType cannonballType = CannonballType.Normal)
    {
        if (cannonAmmoInventory == null)
        {
            Debug.LogWarning("[WeaponIndicator] 炮库 InventoryDataSO 未赋值！");
            return;
        }

        ItemDataSO itemToAdd = null;
        switch (cannonballType)
        {
            case CannonballType.Normal:
                itemToAdd = normalCannonballData;
                break;
            case CannonballType.ArmorPiercing:
                itemToAdd = armorPiercingCannonballData;
                break;
        }

        if (itemToAdd == null)
        {
            Debug.LogWarning($"[WeaponIndicator] 未找到类型为 {cannonballType} 的炮弹 ItemDataSO！请在 Inspector 中赋值。");
            return;
        }

        // AddItem 会处理堆叠和空位，并触发 OnInventoryChanged
        // 注意：InventoryDataSO.AddItem 需要传入 inventory type，这里使用 cannonAmmoInventory 自身的 type
        bool success = cannonAmmoInventory.AddItem(itemToAdd, count, cannonAmmoInventory.type);
        if (success)
        {
            Debug.Log($"[WeaponIndicator] 已装填 {count} 发 {itemToAdd.itemName}");
        }
        else
        {
            Debug.LogWarning($"[WeaponIndicator] 装填失败！{itemToAdd.itemName} (可能是库存已满)");
        }
    }

    [ContextMenu("Load Normal Cannonball (x10)")]
    private void DebugLoadNormalCannon()
    {
        LoadingCannon(10, CannonballType.Normal);
    }

    [ContextMenu("Load AP Cannonball (x10)")]
    private void DebugLoadAPCannon()
    {
        LoadingCannon(10, CannonballType.ArmorPiercing);
    }
}