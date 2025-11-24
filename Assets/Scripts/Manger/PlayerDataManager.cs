using UnityEngine;

/// <summary>
/// 玩家数据管理器：保存玩家选择的职业等数据，使用DontDestroyOnLoad确保场景切换时数据不丢失
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public ProfessionType SelectedProfession = ProfessionType.Crewman;

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
}

