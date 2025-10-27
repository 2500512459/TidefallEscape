using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ȫ�� UI ������������������ʾ��������رա�
/// ����� Inspector ���ֶ��󶨣����趯̬���ء�
/// </summary>
public class UIManger : MonoSingleton<UIManger>
{
    [Header("������Ҫ�����UI��壨�ֶ��󶨣�")]
    public Transform Root;

    [Header("������Ҫ�����UI��壨�ֶ��󶨣�")]
    public List<UIPanelBase> panels = new List<UIPanelBase>();

    /// <summary> �����ֵ䣺���� �� ���ʵ�� </summary>
    private Dictionary<Type, UIPanelBase> panelDict = new Dictionary<Type, UIPanelBase>();

    protected override void Awake()
    {
        // ��ʼ��ʱע���������
        foreach (var panel in panels)
        {
            if (panel == null) continue;
            var type = panel.GetType();
            if (!panelDict.ContainsKey(type))
            {
                panelDict.Add(type, panel);
                panel.OnInit();
                panel.OnHide(); // Ĭ������
            }
        }
    }

    /// <summary>
    /// ��ʾָ�����͵����
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

        Debug.LogWarning($"[UIManager] δ����壺{type.Name}");
        return null;
    }

    /// <summary>
    /// ����ָ�����͵����
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
    /// �رղ�����ָ����壨һ�㲻���ã�
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
    /// �ر��������
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
