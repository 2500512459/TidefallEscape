using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XScrollView;

/// <summary>
/// 背包界面
/// @Author B站：码农小徐 
/// 如有疑问或定制需求可私信联系作者 https://space.bilibili.com/304648178
/// </summary>
public class BagUI : MonoBehaviour
{
    //总数
    public int totalNum = 100;
    
    private UIXScrollView uixScrollView;
    
    
    //所有的cell列表，用于储存所有cell的数据
    private List<BagScrollViewItemData> cellsDataList;
    
    
    /// <summary>
    /// 当前选中的节点信息数据
    /// </summary>
    public int curSelectIndex = 0;
    //public Image selectIcon;
    //public TextMeshProUGUI selectItemName;
    //public TextMeshProUGUI selectNum;
    
    // Start is called before the first frame update
    void Start()
    {
        //模拟获取数据的方法
        //在实际开发中，数据应该是从服务器或者是配置表读取出来的
        cellsDataList = new List<BagScrollViewItemData>();
        for (int i = 0; i < totalNum; i++)
        {
            var cellData = CreateCellData(i);
            cellsDataList.Add(cellData);
        }
        //初始化列表
        uixScrollView = GetComponentInChildren<UIXScrollView>();
        uixScrollView.InitXScrollView(totalNum);
        uixScrollView.AddUpdateCellAction(OnUpdateScrollItemAction);
        uixScrollView.AddCellClickAction(OnClickScrollItemAction);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// 创建节点数据
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    BagScrollViewItemData CreateCellData(int index)
    {
        var data = new BagScrollViewItemData()
        {
            icon = $"Icon/icon{index % 51 + 1}",
            num = Random.Range(1, 999),
        };
        return data;
    }
    
    /// <summary>
    /// 获取节点数据
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    BagScrollViewItemData GetItemData(int index)
    {
        return cellsDataList[index];
    }




    /// <summary>
    /// 更新选中信息
    /// </summary>
    //private void UpdateSelectItemInfo()
    //{
    //    var data = GetItemData(curSelectIndex);
    //    selectNum.text = data.num.ToString();
    //    selectItemName.text = $"道具名字_{curSelectIndex}";
    //    selectIcon.sprite = Resources.Load<Sprite>(data.icon);
    //}


    /// <summary>
    /// 更新item信息
    /// </summary>
    private void OnUpdateScrollItemAction(BaseXScrollViewItem item, int index)
    {

        var data = GetItemData(index);
        item.UpdateCellInfo(index, data);

        var bagScrollViewItem = item as BagScrollViewItem;
        bagScrollViewItem.UpdateCellSelect(index == curSelectIndex);
    }


    /// <summary>
    /// 点击item事件
    /// </summary>
    /// <param name="index"></param>
    private void OnClickScrollItemAction(int index)
    {
        if (index == curSelectIndex)
        {
            return;
        }

        curSelectIndex = index;

        //UpdateSelectItemInfo();

        //uixScrollView.UpdateScrollView(true);

    }
}


/// <summary>
/// 背包item道具数据类
/// </summary>
public class BagScrollViewItemData : IXScrollViewItemData
{
    /// <summary>
    /// 道具id
    /// </summary>
    public int id;
    
    /// <summary>
    /// 道具icon
    /// </summary>
    public string icon;
    
    /// <summary>
    /// 道具数量
    /// </summary>
    public int num;
}
