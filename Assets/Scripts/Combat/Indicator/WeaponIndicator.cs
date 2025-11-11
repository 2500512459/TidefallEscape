using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器指示器（自驱动版本）
/// - 自动显示手榴弹抛物线和射程范围
/// - 自动计算鼠标目标点、飞行速度
/// - 按下左键自动发射炮弹
/// </summary>
public class WeaponIndicator : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("用于射线检测的层")]
    public LayerMask mask;

    [Tooltip("发射点")]
    public Transform firePoint;

    [Tooltip("抛物线初始速度（m/s）")]
    public float parabolaInitVelocity = 20f;

    [Tooltip("是否自动隐藏指示器（无输入时）")]
    public bool autoHide = true;

    [Header("材质")]
    [SerializeField] private Material materialBase;
    [SerializeField] private Material materialParabola;

    [Header("炮弹预制体")]
    [SerializeField] private GameObject cannonBallPrefab;

    // 对外属性
    public Vector3 TargetPosition { get; private set; }           // 目标位置
    public float ParabolaRunVelocity { get; private set; }        // 抛物线运行速度

    // 内部成员
    private Transform indicator;          // 抛物线指示器
    private Transform rangeIndicator;     // 范围圆圈
    private MeshRenderer parabolaRenderer; 
    private MaterialPropertyBlock parabolaBlock;  // 材质属性块，用于动态修改材质参数
    private Plane intersectionPlane = new Plane(Vector3.up, Vector3.zero);  // 用于鼠标射线检测的平面
    private Vector3 lastValidPosition = Vector3.zero;  // 上一次有效的鼠标位置
    private float range;  // 武器最大射程
    private bool isAiming;
    private void OnEnable()
    {
        PlayerInput.Instance.IsAttackedEvent += IsAimingChanged;
    }
    private void OnDisable()
    {

    }
    void IsAimingChanged(bool isAiming)
    {
        this.isAiming = isAiming;
    }
    void Start()
    {
        parabolaBlock = new MaterialPropertyBlock();

        // 创建射程指示器（圆环形状，表示武器最大射程范围）
        rangeIndicator = CreateIndicator("Range", IndicatorGeometry.CreateCircleEdgeMesh(0.98f, 1, 60), materialBase);
        rangeIndicator.gameObject.SetActive(false);

        // 创建抛物线指示器（平面网格，用于显示预测的弹道轨迹）
        indicator = CreateIndicator("Grenade", IndicatorGeometry.CreatePlaneMesh(60, 4), materialParabola);
        parabolaRenderer = indicator.GetComponent<MeshRenderer>();
        indicator.gameObject.SetActive(false);

        // 计算并设置武器射程
        SetParabolaInitVel(parabolaInitVelocity);
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
            }
            return;
        }
        // 显示指示器
        indicator.gameObject.SetActive(true);
        rangeIndicator.gameObject.SetActive(true);

        // 更新指示器的位置和旋转
        UpdateTransform();
        // 更新手榴弹抛物线指示器
        UpdateGrenadeIndicator();

        // 当按下鼠标左键时发射炮弹
        if (Input.GetMouseButtonDown(0))
        {
            FireCannon();
        }
    }

    /// <summary>
    /// 更新抛物线指示器
    /// </summary>
    void UpdateGrenadeIndicator()
    {
        Vector3 origin = firePoint.position;
        // 获取鼠标在XZ平面上的交点作为目标位置
        TargetPosition = GetMouseRayIntersectionWithXZPlane(origin);

        // 计算水平方向上的距离
        Vector3 dir = TargetPosition - origin;
        dir.y = 0; // 忽略Y轴差异，只考虑水平距离
        float distance = dir.magnitude;

        // 如果目标距离超过最大射程，则限制在最大射程内
        if (distance > range)
        {
            distance = range;
            TargetPosition = origin + dir.normalized * range;
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
    }

    /// <summary>
    /// 更新指示器的基础变换（位置和旋转）
    /// </summary>
    void UpdateTransform()
    {
        Vector3 origin = firePoint.position;
        indicator.position = origin;
        rangeIndicator.position = origin;
        rangeIndicator.rotation = Quaternion.identity;  // 射程指示器保持世界坐标系的朝向
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
    /// <returns>标准化的发射方向向量</returns>
    Vector3 GetShootDirection()
    {
        Vector3 dir = TargetPosition - firePoint.position;
        dir.y = 0; // 保持水平指向目标
        // 如果距离过近，则直接向前发射
        if (dir.sqrMagnitude < 0.001f) return firePoint.forward;
    
        // 计算基础朝向（水平指向目标）
        Quaternion baseRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        // 加上固定仰角（向上45度）得到最终发射方向
        Quaternion launchRot = baseRot * Quaternion.Euler(-45f, 0, 0);
    
        return launchRot * Vector3.forward;
    }

    /// <summary>
    /// 发射炮弹
    /// </summary>
    void FireCannon()
    {
        // 如果没有设置炮弹预制体则不执行
        if (cannonBallPrefab == null) return;

        // 在发射点位置实例化炮弹
        GameObject obj = Instantiate(cannonBallPrefab, firePoint.position, Quaternion.identity);
        CannonBall ball = obj.GetComponent<CannonBall>();
        // 如果炮弹有CannonBall脚本，则设置速度和发射方向
        if (ball != null)
        {
            ball.speed = ParabolaRunVelocity;
            ball.Launch(GetShootDirection());
        }
    }
}