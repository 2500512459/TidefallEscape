using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class LoadManager : MonoBehaviour
{
    public static LoadManager Instance { get; private set; }
    public Animator animator;
    public GameObject loadingPanel;
    public Slider loadingSlider;
    public TextMeshProUGUI loadingText;
    
    private bool isLoading = false;
    private Coroutine loadCoroutine;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        // 使用DontDestroyOnLoad确保场景切换时对象不被销毁
        DontDestroyOnLoad(this.gameObject);
    }
    
    private void OnEnable()
    {
        // 场景加载完成后，重新查找 UI 组件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景加载完成后，按照层级结构重新查找 UI 组件
        // 结构：FadeCanvas -> FadePanel (Animator) 和 loadingPanel -> loadingSlider, loadingText
        FindFadeCanvasComponents();
        
        // 重置加载状态
        isLoading = false;
    }
    
    /// <summary>
    /// 查找 FadeCanvas 及其子组件的引用
    /// </summary>
    private void FindFadeCanvasComponents()
    {
        // 查找 FadeCanvas
        GameObject fadeCanvas = GameObject.Find("FadeCanvas");
        if (fadeCanvas == null)
        {
            return;
        }
        
        // 查找 FadePanel (挂载了 Animator)
        Transform fadePanelTransform = fadeCanvas.transform.Find("FadePanel");
        if (fadePanelTransform != null)
        {
            animator = fadePanelTransform.GetComponent<Animator>();
        }
        
        // 查找 loadingPanel
        Transform loadingPanelTransform = fadeCanvas.transform.Find("loadingPanel");
        if (loadingPanelTransform != null)
        {
            loadingPanel = loadingPanelTransform.gameObject;
            
            // 查找 loadingSlider (loadingPanel 的子物体)
            loadingSlider = loadingPanelTransform.GetComponentInChildren<Slider>();
            
            // 查找 loadingText (loadingPanel 的子物体)
            loadingText = loadingPanelTransform.GetComponentInChildren<TextMeshProUGUI>();
        }
    }
    
    public void LoadScene(string sceneName)
    {
        // 防止重复加载
        if (isLoading)
            return;
                
        // 如果当前场景名称相同，不重复加载
        if (SceneManager.GetActiveScene().name == sceneName)
            return;
        
        // 停止之前的加载协程（如果有）
        if (loadCoroutine != null)
            StopCoroutine(loadCoroutine);
        
        isLoading = true;
        
        // 如果 UI 组件为空，尝试查找
        if (loadingPanel == null || animator == null)
            FindFadeCanvasComponents();
        
        // 确保 UI 组件存在
        if (loadingPanel == null || animator == null)
        {
            isLoading = false;
            return;
        }
        
        // 先不显示loadingPanel，等屏幕暗下来后再显示
        loadingPanel.SetActive(false);
        if (loadingSlider != null)
            loadingSlider.value = 0;
            
        if (loadingText != null)
            loadingText.text = "0%";
        
        loadCoroutine = StartCoroutine(LoadSceneRoutine(sceneName));
    }
    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // 禁用玩家输入，防止场景切换时的输入泄漏
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.DisableControlInput();
        }
        
        // 先触发淡出动画（End），等待淡出完成
        animator.SetTrigger("End");
        // 等待End动画完成（根据动画器配置，End动画应该是淡出效果）
        yield return new WaitForSeconds(1f);

        // 屏幕暗下来后，显示loadingPanel（背景是黑色，与End动画结束后的颜色一致）
        loadingPanel.SetActive(true);

        // 淡出完成后，开始加载场景
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (!op.isDone)
        {
            loadingSlider.value = op.progress;
            loadingText.text = $"{op.progress * 100}%";
            
            if (op.progress >= 0.9f)
            {
                loadingSlider.value = 1;
                loadingText.text = "鼠标点击继续";
                if (Input.GetMouseButtonDown(0))
                {
                    // 激活场景，新场景的FadePanel会自动从Start状态开始（淡入效果）
                    op.allowSceneActivation = true;
                }
                yield return null;
            }
            yield return null;
        }
    }
}
