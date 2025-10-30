using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XScrollView;
    
/// <summary>
/// 背包滚动列表item类
/// @Author B站：码农小徐 
/// 如有疑问或定制需求可私信联系作者 https://space.bilibili.com/304648178
/// </summary>
public class BagScrollViewItem : BaseXScrollViewItem
{
    /// <summary>
    /// 道具数量
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI text;
    
    /// <summary>
    /// 道具icon
    /// </summary>
    [SerializeField]
    private Image icon;
    
    /// <summary>
    /// 道具选中效果
    /// </summary>
    [SerializeField] 
    private GameObject selectNode;
    

    public TextMeshProUGUI Text
    {
        get
        {
            if (text == null)
                text = GetComponentInChildren<TextMeshProUGUI>();
            return text;
        }
    }
    
    public Image Icon
    {
        get
        {
            if (icon == null)
                icon = GetComponentInChildren<Image>();
            return icon;
        }
    }

    public override void UpdateCellInfo(int _index, IXScrollViewItemData data)
    {
        base.UpdateCellInfo(_index, data);
        var itemData = data as BagScrollViewItemData;
        Text.text = itemData.num.ToString();
        Icon.sprite = Resources.Load<Sprite>(itemData.icon);
    }

    

    public void UpdateCellSelect(bool select)
    {
        selectNode.SetActive(select);
    }


    
}

