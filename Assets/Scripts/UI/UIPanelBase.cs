using UnityEngine;

/// <summary>
/// 所有UI面板的基类，提供通用的生命周期接口
/// </summary>
public abstract class UIPanelBase : MonoBehaviour
{
    /// <summary> 当前面板是否激活显示 </summary>
    public bool IsVisible => gameObject.activeSelf;

    /// <summary> 初始化逻辑（仅调用一次） </summary>
    public virtual void OnInit() { }

    /// <summary> 打开面板时调用 </summary>
    public virtual void OnShow()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary> 隐藏面板时调用 </summary>
    public virtual void OnHide()
    {
        gameObject.SetActive(false);
    }

    /// <summary> 销毁前清理 </summary>
    public virtual void OnClose()
    {
        Destroy(gameObject);
    }
}
