using System;
using UnityEngine;

/// <summary>
/// 轮播条目数据类
/// 使用ScriptableObject存储单个轮播项的所有信息
/// 可在Unity编辑器中通过右键菜单创建：UI -> Carousel Entry
/// </summary>
[CreateAssetMenu(fileName = "New Carousel Entry", menuName = "UI/Carousel Entry", order = 0)]
public class CarouselEntry : ScriptableObject
{
    /// <summary>
    /// 轮播项的图片资源
    /// </summary>
    [field:SerializeField] public Sprite EntryGraphic { get; private set; }
    /// <summary>
    /// 轮播项的标题文本
    /// </summary>
    [field:SerializeField] public string Headline { get; private set; }
    /// <summary>
    /// 轮播项的详细描述文本（多行，最多10行）
    /// </summary>
    [field:SerializeField, Multiline(10)] public string Description { get; private set; }
    
    [Header("Interaction")] 
    /// <summary>
    /// 点击该轮播项后要加载的场景名称
    /// </summary>
    [SerializeField] private string levelNameToLoad;
    
    /// <summary>
    /// 执行交互操作
    /// 通过传入的场景加载回调函数加载指定的场景
    /// </summary>
    /// <param name="loadSceneCallback">场景加载回调函数，接受场景名称作为参数</param>
    public void Interact(Action<string> loadSceneCallback)
    {
        if (loadSceneCallback != null && !string.IsNullOrEmpty(levelNameToLoad))
        {
            loadSceneCallback(levelNameToLoad);
        }
    }
}
