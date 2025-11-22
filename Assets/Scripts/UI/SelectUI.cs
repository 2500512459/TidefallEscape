using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 职业选择UI管理器：处理职业选择按钮点击，更新描述面板和激活对应的角色面板
/// </summary>
public class SelectUI : MonoBehaviour
{
    [System.Serializable]
    public class ProfessionData
    {
        public ProfessionType professionType;
        [TextArea(3, 10)]
        public string description;        // 职业描述
        public GameObject characterPanel; // 对应的职业角色面板
    }

    [Header("职业选择按钮")]
    [SerializeField]
    [Tooltip("船员按钮")]
    private Button crewmanButton;

    [SerializeField]
    [Tooltip("瞭望员按钮")]
    private Button lookoutButton;

    [SerializeField]
    [Tooltip("船长按钮")]
    private Button captainButton;

    [SerializeField]
    [Tooltip("船工按钮")]
    private Button shipwrightButton;

    [SerializeField]
    [Tooltip("确认选择按钮")]
    private Button selectButton;

    [Header("场景配置")]
    [SerializeField]
    [Tooltip("点击确认后加载的场景名称")]
    private string nextSceneName = "GameScene";

    [Header("UI组件")]
    [SerializeField]
    [Tooltip("右侧描述面板文本")]
    private TextMeshProUGUI descriptionText;
    [SerializeField]
    [Tooltip("右侧职业名称文本")]
    private TextMeshProUGUI professionNameText;
    private Dictionary<ProfessionType, string> professionNameDict = new Dictionary<ProfessionType, string>()
    {
        { ProfessionType.Crewman, "船员" },
        { ProfessionType.Lookout, "瞭望员" },
        { ProfessionType.Captain, "船长" },
        { ProfessionType.Shipwright, "船工" }
    };

    [Header("职业数据")]
    [SerializeField]
    [Tooltip("职业数据配置")]
    private ProfessionData[] professionDataArray;


    private ProfessionType currentSelectedProfession = ProfessionType.Crewman;

    private void Awake()
    {
        // 绑定按钮点击事件
        if (crewmanButton != null)
        {
            crewmanButton.onClick.AddListener(() => OnProfessionButtonClicked(ProfessionType.Crewman));
        }

        if (lookoutButton != null)
        {
            lookoutButton.onClick.AddListener(() => OnProfessionButtonClicked(ProfessionType.Lookout));
        }

        if (captainButton != null)
        {
            captainButton.onClick.AddListener(() => OnProfessionButtonClicked(ProfessionType.Captain));
        }

        if (shipwrightButton != null)
        {
            shipwrightButton.onClick.AddListener(() => OnProfessionButtonClicked(ProfessionType.Shipwright));
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(() => ConfirmSelectionAndLoadScene(nextSceneName));
        }
    }

    private void Start()
    {
        // 初始化时显示第一个职业的信息
        OnProfessionButtonClicked(ProfessionType.Crewman);
    }

    /// <summary>
    /// 职业选择按钮点击事件
    /// </summary>
    /// <param name="professionType">选择的职业类型</param>
    public void OnProfessionButtonClicked(ProfessionType professionType)
    {
        currentSelectedProfession = professionType;

        // 1. 先关闭所有已配置的角色面板（使用HashSet去重，防止重复操作同一个对象）
        if (professionDataArray != null)
        {
            HashSet<GameObject> uniquePanels = new HashSet<GameObject>();
            foreach (var data in professionDataArray)
            {
                if (data.characterPanel != null)
                {
                    uniquePanels.Add(data.characterPanel);
                }
            }

            foreach (var panel in uniquePanels)
            {
                if (panel.activeSelf)
                {
                    panel.SetActive(false);
                }
            }
        }

        // 查找对应的职业数据
        ProfessionData selectedData = GetProfessionData(professionType);
        if (selectedData == null)
        {
            Debug.LogWarning($"未找到职业类型 {professionType} 的数据配置！");
            return;
        }

        // 2. 再开启当前选中的面板，确保触发OnEnable（用于Timeline的Play On Awake）
        if (selectedData.characterPanel != null)
        {
            selectedData.characterPanel.SetActive(true);
        }

        // 更新描述面板
        UpdateDescriptionPanel(selectedData.description);
    }

    /// <summary>
    /// 更新描述面板
    /// </summary>
    /// <param name="description">职业描述文本</param>
    private void UpdateDescriptionPanel(string description)
    {
        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
        else
        {
            Debug.LogWarning("描述面板文本组件未分配！");
        }
        if (professionNameText != null)
        {
            professionNameText.text = professionNameDict[currentSelectedProfession];
        }
        else
        {
            Debug.LogWarning("职业名称文本组件未分配！");
        }
    }

    /// <summary>
    /// 根据职业类型获取对应的职业数据
    /// </summary>
    /// <param name="professionType">职业类型</param>
    /// <returns>职业数据，如果未找到则返回null</returns>
    private ProfessionData GetProfessionData(ProfessionType professionType)
    {
        if (professionDataArray == null || professionDataArray.Length == 0)
        {
            Debug.LogWarning("职业数据数组未配置！");
            return null;
        }

        foreach (ProfessionData data in professionDataArray)
        {
            if (data.professionType == professionType)
            {
                return data;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取当前选择的职业类型
    /// </summary>
    /// <returns>当前选择的职业类型</returns>
    public ProfessionType GetCurrentSelectedProfession()
    {
        return currentSelectedProfession;
    }

    /// <summary>
    /// 确认选择职业并加载场景
    /// </summary>
    /// <param name="sceneName">要加载的场景名称</param>
    public void ConfirmSelectionAndLoadScene(string sceneName)
    {
        // 保存选择的职业到PlayerDataManager
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetSelectedProfession(currentSelectedProfession);
        }
        else
        {
            Debug.LogWarning("PlayerDataManager实例不存在，无法保存职业选择！");
        }

        // 加载场景
        if (LoadManager.Instance != null)
        {
            LoadManager.Instance.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("LoadManager实例不存在，无法加载场景！");
        }
    }
}
