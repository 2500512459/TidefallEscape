using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 玩家数据管理器：保存玩家选择的职业等数据，使用DontDestroyOnLoad确保场景切换时数据不丢失
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public ProfessionType SelectedProfession = ProfessionType.Crewman;

    // 拥有的船只类型列表
    public System.Collections.Generic.List<ShipType> OwnedShips = new System.Collections.Generic.List<ShipType>();

    // 当前船只类型（可为空，表示未选择船只）
    public ShipType CurrentShipType = ShipType.None;

    public InventoryContext currentContext = InventoryContext.Default;
    
    // 全局对局时间，用于跨场景保持时间
    public float CurrentMatchTime = 0f;

    [Header("货币数据")]
    public CurrencyDataSO currencyData;

    /// <summary>
    /// 当前金币数量（从 CurrencyDataSO 中获取）
    /// </summary>
    public int CurrentGold
    {
        get
        {
            if (currencyData == null)
            {
                Debug.LogWarning("PlayerDataManager 未绑定 CurrencyDataSO，CurrentGold 返回 0");
                return 0;
            }

            return currencyData.GoldCoinAmount;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        // 使用DontDestroyOnLoad确保场景切换时对象不被销毁
        DontDestroyOnLoad(this.gameObject);
    }

    public void UpdateContextBasedOnScene(string sceneName)
    {
        if (sceneName == "HomeScene")
        {
            currentContext = InventoryContext.Home;
            // 回到主页时重置对局时间
            CurrentMatchTime = 0f;
        }
        else
        {
            currentContext = InventoryContext.Default;
        }
        Debug.Log($"[PlayerDataManager] 场景 {sceneName} 加载，InventoryContext 设置为: {currentContext}");
    }

    /// <summary>
    /// 恢复上下文（根据当前场景重置）
    /// </summary>
    public void RestoreContext()
    {
        UpdateContextBasedOnScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 设置选择的职业
    /// </summary>
    /// <param name="profession">职业类型</param>
    public void SetSelectedProfession(ProfessionType profession)
    {
        SelectedProfession = profession;
        Debug.Log($"玩家选择的职业已设置为: {profession}");
    }

    /// <summary>
    /// 获取选择的职业
    /// </summary>
    /// <returns>当前选择的职业类型</returns>
    public ProfessionType GetSelectedProfession()
    {
        return SelectedProfession;
    }

    [System.Serializable]
    public struct ProfessionData
    {
        public ProfessionType professionType;
        [Header("资源配置")]
        public GameObject characterPrefab; // 模型预制体
        public GameObject weaponPrefab;    // 武器预制体
        [Header("挂点名称配置")]
        public string hipSocketName;       // 腰部挂点名称 (例如: Hips/WeaponSlot)/背后挂点名称 (例如: Back/WeaponSlot)
        public string handSocketName;      // 手部挂点名称 (例如: RightHand/WeaponSlot)
        [Header("特效配置")]
        public ParticleSystem effect1;     // 攻击特效1
        public ParticleSystem effect2;     // 攻击特效2
        public ParticleSystem effect3;     // 攻击特效3
    }

    public System.Collections.Generic.List<ProfessionData> professionDataList = new System.Collections.Generic.List<ProfessionData>();

    /// <summary>
    /// 根据职业类型获取配置数据
    /// </summary>
    public ProfessionData GetProfessionData(ProfessionType type)
    {
        return professionDataList.Find(x => x.professionType == type);
    }

    /// <summary>
    /// 新增拥有的船只类型
    /// </summary>
    /// <param name="shipType">船只类型</param>
    public void AddOwnedShip(ShipType shipType)
    {
        if (!OwnedShips.Contains(shipType))
        {
            OwnedShips.Add(shipType);
            Debug.Log($"已新增船只类型: {shipType}");
        }
        else
        {
            Debug.LogWarning($"船只类型 {shipType} 已存在，无需重复添加");
        }
    }

    /// <summary>
    /// 移除拥有的船只类型
    /// </summary>
    /// <param name="shipType">船只类型</param>
    public void RemoveOwnedShip(ShipType shipType)
    {
        if (OwnedShips.Contains(shipType))
        {
            OwnedShips.Remove(shipType);
            Debug.Log($"已移除船只类型: {shipType}");
            
            // 如果移除的是当前船只，清空当前船只类型
            if (CurrentShipType == shipType)
            {
                CurrentShipType = ShipType.None;
                Debug.Log("当前船只类型已清空");
            }
        }
        else
        {
            Debug.LogWarning($"船只类型 {shipType} 不存在，无法移除");
        }
    }

    /// <summary>
    /// 设置当前船只类型
    /// </summary>
    /// <param name="shipType">船只类型</param>
    public void SetCurrentShipType(ShipType shipType)
    {
        // 检查是否拥有该船只类型
        if (!OwnedShips.Contains(shipType))
        {
            Debug.LogWarning($"玩家未拥有船只类型 {shipType}，无法设置为当前船只");
            return;
        }

        CurrentShipType = shipType;
        Debug.Log($"当前船只类型已设置为: {shipType}");
    }

    /// <summary>
    /// 增加玩家金币数量（写入 CurrencyDataSO）
    /// </summary>
    /// <param name="amount">要增加的金币数量（必须为非负数）</param>
    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"尝试增加负数金币: {amount}");
            return;
        }

        if (currencyData == null)
        {
            Debug.LogWarning("PlayerDataManager 未绑定 CurrencyDataSO，无法增加金币");
            return;
        }

        currencyData.AddGoldCoins(amount);
        Debug.Log($"金币增加 {amount}，当前金币: {CurrentGold}");
    }

    /// <summary>
    /// 在 Inspector 右键菜单中测试用，固定增加 1000 金币
    /// </summary>
    [ContextMenu("Add 1000 Gold")]
    private void AddGoldByContextMenu()
    {
        AddGold(1000);
    }
}
