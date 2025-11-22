using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

/// <summary>
/// 菜单UI管理器：处理开始游戏按钮点击，切换面板并播放Timeline
/// </summary>
public class MenuUI : MonoBehaviour
{
    [Header("UI面板")]
    [SerializeField]
    [Tooltip("菜单面板")]
    private GameObject menuPanel;

    [SerializeField]
    [Tooltip("选择面板")]
    private GameObject selectPanel;

    [Header("场景对象")]
    [SerializeField]
    [Tooltip("场景对象")]
    private GameObject[] sceneObjects;


    /// <summary>
    /// 开始游戏按钮点击事件
    /// </summary>
    public void OnStartGameButtonClicked()
    {
        // 关闭菜单面板
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // 打开选择面板
        if (selectPanel != null)
        {
            selectPanel.SetActive(true);
        }

        // 使能角色选择场景对象
        if (sceneObjects != null)
        {
            foreach (GameObject sceneObject in sceneObjects)
            {
                sceneObject.SetActive(true);
            }
        }
    }
}

