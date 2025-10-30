using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace XScrollView
{
    /// <summary>
    /// 滚动列表item基类
    /// @Author B站：码农小徐 
    /// 如有疑问或定制需求可私信联系作者 https://space.bilibili.com/304648178
    /// 需继承此类
    /// </summary>
    public abstract class BaseXScrollViewItem : MonoBehaviour
    {
        /// <summary>
        /// 列表项序号
        /// </summary>
        public int index;
        
        /// <summary>
        /// 点击事件
        /// </summary>
        private UnityAction<int> btnClickAction;
        
        private RectTransform rectTransform;

        [SerializeField]
        private Button button;
        
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                    rectTransform = GetComponent<RectTransform>();
                return rectTransform;
            }
        }
        
        void Awake()
        {
            if (button == null)
            {
                button = GetComponentInChildren<Button>();
            }

            button.onClick.AddListener(() =>
            {
                print($"btnOnClick_{index}");
                btnClickAction?.Invoke(index);
            });

        }
        
        private void OnDestroy()
        {
            btnClickAction = null;
        }

        /// <summary>
        /// 添加点击事件
        /// </summary>
        /// <param name="btnClick"></param>
        public void AddButtonClickListener(UnityAction<int> btnClick)
        {
            btnClickAction += btnClick;
        }
        
        /// <summary>
        /// 更新位置
        /// </summary>
        /// <param name="pos"></param>
        public void UpdateCellPos(Vector2 pos)
        {
            RectTransform.anchoredPosition = pos;
        }

        /// <summary>
        /// 更新信息
        /// </summary>
        /// <param name="_index"></param>
        /// <param name="data"></param>
        public virtual void UpdateCellInfo(int _index, IXScrollViewItemData data)
        {
            index = _index;
        }
        
        
        
    }
}