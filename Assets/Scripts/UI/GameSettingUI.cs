using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameSettingUI : MonoBehaviour
{
    private LoadManager loadManager;
    [SerializeField] private GameObject SettingPanel;
    [SerializeField] private GameObject KeySettingPanel;
    [SerializeField] private GameObject MusicSettingPanel;
    [SerializeField] private GameObject MenuSettingPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button KeySettingButton;
    [SerializeField] private Button MusicSettingButton;
    [SerializeField] private Button MenuSettingButton;
    private void Awake()
    {
        // 按钮事件
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        KeySettingButton.onClick.AddListener(OpenKeySettingPanel);
        MusicSettingButton.onClick.AddListener(OpenMusicSettingPanel);
        MenuSettingButton.onClick.AddListener(OpenMenuSettingPanel);
    }

    private void Start()
    {
        loadManager = LoadManager.Instance;
    }

    private void OnEnable()
    {
        // 订阅 Esc 输入事件
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.EscPressedEvent += OnEscPressed;
        }
    }

    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏 / 空引用
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.EscPressedEvent -= OnEscPressed;
        }
    }

    public void ShowPanel()
    {
        SettingPanel.SetActive(true);
        SwitchPanel(KeySettingPanel);

        PlayerInput.Instance.DisableAllInputsExcept(PlayerInput.Instance.SettingInput);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HidePanel()
    {
        SettingPanel.SetActive(false);
        PlayerInput.Instance.EnableAllInputs();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettingPanel()
    {
        SettingPanel.SetActive(true);
        SwitchPanel(KeySettingPanel);
    }

    private void CloseSettingPanel()
    {
        SettingPanel.SetActive(false);
        PlayerInput.Instance.EnableAllInputs();
    }

    private void OpenKeySettingPanel()
    {
        SwitchPanel(KeySettingPanel);
    }

    private void OpenMusicSettingPanel()
    {
        SwitchPanel(MusicSettingPanel);
    }
    private void OpenMenuSettingPanel()
    {
        SwitchPanel(MenuSettingPanel);
    }
    public void BackToMenu()
    {
        loadManager.LoadScene("MenuScene");
        CloseSettingPanel();
    }

    // 切换面板的通用方法，方便后续扩展
    private void SwitchPanel(GameObject targetPanel)
    {
        KeySettingPanel.SetActive(targetPanel == KeySettingPanel);
        MusicSettingPanel.SetActive(targetPanel == MusicSettingPanel);
        MenuSettingPanel.SetActive(targetPanel == MenuSettingPanel);
        // 后续增加新的面板时，只需在这里添加一行即可：
        // NewPanel.SetActive(targetPanel == NewPanel);
    }

    /// <summary>
    /// 当前是否处于主菜单场景（MenuScene）
    /// </summary>
    private bool IsInMenuScene()
    {
        return SceneManager.GetActiveScene().name == "MenuScene";
    }

    /// <summary>
    /// 关闭按钮点击逻辑：菜单场景只关 UI，游戏场景则走 HidePanel（恢复输入等）
    /// </summary>
    private void OnCloseButtonClicked()
    {
        if (IsInMenuScene())
        {
            CloseSettingPanel();
        }
        else
        {
            HidePanel();
        }
    }

    /// <summary>
    /// Esc 输入事件回调：
    /// - MenuScene：OpenSettingPanel / CloseSettingPanel
    /// - 其他场景：ShowPanel / HidePanel（带输入锁定）
    /// </summary>
    private void OnEscPressed()
    {
        if (IsInMenuScene())
        {
            if (!SettingPanel.activeSelf)
            {
                OpenSettingPanel();
            }
            else
            {
                CloseSettingPanel();
            }
        }
        else
        {
            if (!SettingPanel.activeSelf)
            {
                ShowPanel();
            }
            else
            {
                HidePanel();
            }
        }
    }
}
