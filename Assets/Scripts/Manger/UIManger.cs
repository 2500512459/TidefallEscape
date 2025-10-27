using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局 UI 管理器：负责面板的显示、隐藏与关闭。
/// 面板在 Inspector 中手动绑定，无需动态加载。
/// </summary>
public class UIManger : MonoSingleton<UIManger>
{
    [Header("所有需要管理的UI面板（手动绑定）")]
    public Transform Root;

    [Header("所有需要管理的UI面板（手动绑定）")]
    public List<UIPanelBase> panels = new List<UIPanelBase>();

    /// <summary> 缓存字典：类型 → 面板实例 </summary>
    private Dictionary<Type, UIPanelBase> panelDict = new Dictionary<Type, UIPanelBase>();

    protected override void Awake()
    {
        // 初始化时注册所有面板
        foreach (var panel in panels)
        {
            if (panel == null) continue;
            var type = panel.GetType();
            if (!panelDict.ContainsKey(type))
            {
                panelDict.Add(type, panel);
                panel.OnInit();
                panel.OnHide(); // 默认隐藏
            }
        }
    }

    /// <summary>
    /// 显示指定类型的面板
    /// </summary>
    public T ShowPanel<T>(params object[] args) where T : UIPanelBase
    {
        var type = typeof(T);
        if (panelDict.TryGetValue(type, out UIPanelBase panel))
        {
            panel.OnShow();
            panel.gameObject.SetActive(true);
            return panel as T;
        }

        Debug.LogWarning($"[UIManager] 未绑定面板：{type.Name}");
        return null;
    }

    /// <summary>
    /// 隐藏指定类型的面板
    /// </summary>
    public void HidePanel<T>() where T : UIPanelBase
    {
        var type = typeof(T);
        if (panelDict.TryGetValue(type, out UIPanelBase panel))
        {
            panel.OnHide();
            panel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 关闭并销毁指定面板（一般不常用）
    /// </summary>
    public void ClosePanel<T>() where T : UIPanelBase
    {
        var type = typeof(T);
        if (panelDict.TryGetValue(type, out UIPanelBase panel))
        {
            panel.OnClose();
            panel.gameObject.SetActive(false);
            panelDict.Remove(type);
        }
    }

    /// <summary>
    /// 关闭所有面板
    /// </summary>
    public void CloseAllPanels()
    {
        foreach (var kvp in panelDict)
        {
            kvp.Value.OnClose();
            kvp.Value.gameObject.SetActive(false);
        }
    }
}
