using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace XScrollView
{
    /// <summary>
    /// 滚动列表类
    /// @Author B站：码农小徐 
    /// 如有疑问或定制需求可私信联系作者 https://space.bilibili.com/304648178
    /// </summary>
    public class UIXScrollView : MonoBehaviour
    {
        //scrollView info
        private ScrollRect scrollRect;
        private float height;
        private float width;
        public Transform content;
        private RectTransform contentRect;

        //cell info
        public BagScrollViewItem cell;
        private float cellHeight;
        private float cellWidth;
        public float offsetX = 2f;
        public float offsetY = 10f;

        //列表能显示的行列
        private int row;

        private int column;

        //列表最多显示的cell个数
        private int maxShowCellNum;

        //列表显示的cell列表，用于设置位置,只存了0-显示的个数这么多
        private List<BaseXScrollViewItem> showCellItems;

        //总行数
        private int totalRow;

        //上次显示的节点序号
        private int preStartIndex = 0;


        //总数
        public int totalNum = 100;
        
        //事件相关
        public UnityAction<BaseXScrollViewItem, int> updateItemAction;
        
        private UnityAction<int> clickItemAction;

        private void Awake()
        {
            
        }

        private void OnDestroy()
        {
            updateItemAction = null;
            clickItemAction = null;
        }

        public void InitXScrollView(int num)
        {
            totalNum = num;
            
            scrollRect = GetComponent<ScrollRect>();
            var rect = this.GetComponent<RectTransform>().rect;
            height = rect.height;
            width = rect.width;


            var cellRect = cell.GetComponent<RectTransform>().rect;
            cellHeight = cellRect.height;
            cellWidth = cellRect.width;

            //列
            column = Mathf.FloorToInt(width / (cellWidth + offsetX));
            //行
            row = Mathf.CeilToInt(height / (cellHeight + offsetY));
            //最多只创建屏幕显示的个数这么多
            maxShowCellNum = column * (row + 1);
            
            totalRow = Mathf.CeilToInt((float)totalNum / column);

            //隐藏模版cell
            cell.gameObject.SetActive(false);

            //设置content的大小
            contentRect = content.GetComponent<RectTransform>();
            var contentHeight = totalRow * (cellHeight + offsetY); //向上取整，并且乘以每一行的高
            var contentWidth = contentRect.sizeDelta.x;
            var contentSize = new Vector2(contentWidth, contentHeight);
            contentRect.sizeDelta = contentSize;


            //开始创建节点
            StartCoroutine(CreateCell());

            //滚动事件
            scrollRect.onValueChanged.AddListener(ScrollViewOnValueChanged);

        }


        /// <summary>
        /// 添加列表项更新事件
        /// </summary>
        /// <param name="updateAction"></param>
        public void AddUpdateCellAction(UnityAction<BaseXScrollViewItem, int> updateAction)
        {
            updateItemAction += updateAction;
        }
        
        
        /// <summary>
        /// 添加列表项点击事件
        /// </summary>
        /// <param name="clickAction"></param>
        public void AddCellClickAction(UnityAction<int> clickAction)
        {
            clickItemAction += clickAction;
        }


        /// <summary>
        /// 创建格子
        /// </summary>
        /// <returns></returns>
        IEnumerator CreateCell()
        {
            //最多只创建屏幕显示的个数这么多
            var showCell = Mathf.Min(maxShowCellNum, totalNum);
            
            showCellItems = new List<BaseXScrollViewItem>(showCell);
            
            for (int i = 0; i < showCell; i++)
            {
                yield return null;
                
                var index = i;

                var go = GameObject.Instantiate(cell.gameObject, content);
                go.name = $"Cell_{index}";
                var scrollItem = go.GetComponent<BaseXScrollViewItem>();
                
                UpdateCell(scrollItem, index);
                scrollItem.AddButtonClickListener(clickItemAction);
                go.SetActive(true);

                showCellItems.Add(scrollItem);
                
            }
        }


        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="scrollItem"></param>
        /// <param name="index"></param>
        void UpdateCell(BaseXScrollViewItem scrollItem, int index)
        {
            scrollItem.UpdateCellPos(GetCellPos(index));
            
            updateItemAction?.Invoke(scrollItem, index);
        }



        /// <summary>
        /// 获取节点位置
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Vector2 GetCellPos(int index)
        {
            var curColumn = index % column; //当前的列
            var curX = curColumn * (cellWidth + offsetX); //当前的x坐标

            var curRow = index / column; //当前的行
            var curY = -curRow * (cellHeight + offsetY); //当前的y坐标

            var pos = new Vector2(curX, curY);
            return pos;
        }

      

        /// <summary>
        /// 滚动事件
        /// </summary>
        /// <param name="v"></param>
        void ScrollViewOnValueChanged(Vector2 v)
        {
            UpdateScrollView();
        }

        public void UpdateScrollView(bool forceUpdate = false)
        {
            var y = contentRect.anchoredPosition.y;
            //可能会小于0
            y = Mathf.Max(0, y);
            //print(y + ":" + v);
            //拖动的距离，超过一行时，则顶部的一行移动到底部显示
            var moveRow = Mathf.FloorToInt(y / (cellHeight + offsetY));
            //视图范围内,移动的行数+视图显示的行数<=总行数
            if (moveRow >= 0 && (moveRow + row) <= totalRow)
            {
                var startIndex = moveRow * column; //起始序号
                //和上次的起始序号不同才进行刷新
                if (!forceUpdate && startIndex == preStartIndex)
                {
                    return;
                }

                print(startIndex + ":" + preStartIndex);

                //更新所有cell
                for (int i = startIndex; i < startIndex + maxShowCellNum; i++)
                {
                    var index = i;
                    ScrollUpdateCell(index, startIndex);
                }

                preStartIndex = startIndex;
            }
        }

        /// <summary>
        /// 更新节点信息
        /// </summary>
        /// <param name="index"></param>
        /// <param name="startIndex"></param>
        void ScrollUpdateCell(int index, int startIndex)
        {
            var scrollViewItem = showCellItems[index - startIndex];
            //超出总数的不显示
            if (index >= totalNum)
            {
                scrollViewItem.gameObject.SetActive(false);
                return;
            }

            scrollViewItem.gameObject.SetActive(true);

            UpdateCell(scrollViewItem, index);
        }

      

    }
}
