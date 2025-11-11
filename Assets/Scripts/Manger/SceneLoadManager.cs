using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景加载管理器（使用协程版本）
/// </summary>
public class SceneLoadManager : MonoSingleton<SceneLoadManager>
{
    public FadePanel fadePanel;          // 淡入淡出面板
    private AssetReference currentScene; // 当前加载的场景引用

    [Header("场景资源引用")]
    public AssetReference menu;          // 菜单场景资源引用
    public AssetReference home;          // 安全区场景资源引用
    protected override void Awake()
    {
        StartCoroutine(LoadMenu());
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    private IEnumerator LoadSceneRoutine()
    {
        AsyncOperationHandle<SceneInstance> handle = currentScene.LoadSceneAsync(LoadSceneMode.Additive);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            fadePanel.FadeOut(0.6f);
            SceneManager.SetActiveScene(handle.Result.Scene);
        }
        else
        {
            Debug.LogError($"场景加载失败：{currentScene.AssetGUID}");
        }
    }

    /// <summary>
    /// 异步卸载当前活动场景
    /// </summary>
    private IEnumerator UnloadSceneRoutine()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.isLoaded)
        {
            fadePanel.FadeIn(0.8f);
            yield return new WaitForSeconds(0.45f);

            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(activeScene);
            yield return unloadOp;
        }
    }

    /// <summary>
    /// 加载菜单场景
    /// </summary>
    public IEnumerator LoadMenu()
    {
        if (currentScene != null)
        {
            yield return StartCoroutine(UnloadSceneRoutine());
        }

        currentScene = menu;
        yield return StartCoroutine(LoadSceneRoutine());
    }

    /// <summary>
    /// 加载安全区场景
    /// </summary>
    public IEnumerator LoadHome()
    {
        if (currentScene != null)
        {
            yield return StartCoroutine(UnloadSceneRoutine());
        }

        currentScene = home;
        yield return StartCoroutine(LoadSceneRoutine());
    }

    /// <summary>
    /// 对外暴露的调用接口
    /// </summary>
    public void SwitchToMenu()
    {
        StartCoroutine(LoadMenu());
    }

    public void SwitchToHome()
    {
        StartCoroutine(LoadHome());
    }
}
