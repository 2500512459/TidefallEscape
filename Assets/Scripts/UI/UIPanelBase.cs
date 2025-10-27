using UnityEngine;

/// <summary>
/// ����UI���Ļ��࣬�ṩͨ�õ��������ڽӿ�
/// </summary>
public abstract class UIPanelBase : MonoBehaviour
{
    /// <summary> ��ǰ����Ƿ񼤻���ʾ </summary>
    public bool IsVisible => gameObject.activeSelf;

    /// <summary> ��ʼ���߼���������һ�Σ� </summary>
    public virtual void OnInit() { }

    /// <summary> �����ʱ���� </summary>
    public virtual void OnShow()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary> �������ʱ���� </summary>
    public virtual void OnHide()
    {
        gameObject.SetActive(false);
    }

    /// <summary> ����ǰ���� </summary>
    public virtual void OnClose()
    {
        Destroy(gameObject);
    }
}
