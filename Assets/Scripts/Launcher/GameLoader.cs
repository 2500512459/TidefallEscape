using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Launcher
{
    public class GameLoader : MonoBehaviour
    {
        // 需要补充元数据的 AOT 程序集
        // 第一次需要调用HybridCLR的生成工具生成，生成后将其放到热更新目录下，然后加到Addressables中
        public static List<string> aotMetaAssemblyFiles { get; } = new List<string>()
        {
            "mscorlib.dll",
            "System.dll",
            "System.Core.dll",
        };

        // 需要加载的热更新程序集列表
        public List<string> hotUpdateAssemblyFiles = new List<string>()
        {
            "HotUpdate.dll",
        };

        public string entrySceneName = "MenuScene"; // 更新完成后进入的场景

        [Header("UI Components")]
        [SerializeField] private HotUpdateView _hotUpdateView;

        void Start()
        {
            // 自动查找UI，防止Inspector未赋值导致无效果
            if (_hotUpdateView == null)
            {
                _hotUpdateView = FindObjectOfType<HotUpdateView>();
            }            
            StartCoroutine(LoadGame());
        }

        IEnumerator LoadGame()
        {
            if (_hotUpdateView != null)
            {
                _hotUpdateView.Show(true);
                _hotUpdateView.RefreshUI(0, "正在初始化游戏...");
            }

            Debug.Log("GameLoader: Starting...");

            int totalSteps = 1 + aotMetaAssemblyFiles.Count + hotUpdateAssemblyFiles.Count;
            int currentStep = 0;

            // 1. 初始化Addressables
            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            currentStep++;
            if (_hotUpdateView != null)
            {
                float progress = (float)currentStep / totalSteps;
                _hotUpdateView.RefreshUI(progress, $"初始化完成 {(int)(progress * 100)}%");
            }
            
            Debug.Log("GameLoader: Addressables Initialized");

            // 2. 加载AOT元数据
            // 这些DLL帮助HybridCLR在AOT代码中工作
            foreach (var aotDll in aotMetaAssemblyFiles)
            {
                // Addressable地址通常只是DLL名称（例如"mscorlib.dll"），不需要.bytes后缀
                // 严格匹配Addressables组中的地址
                string assetPath = aotDll; 
                var handle = Addressables.LoadAssetAsync<TextAsset>(assetPath);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    byte[] dllBytes = handle.Result.bytes;
                    // 加载元数据
                    var mode = HybridCLR.HomologousImageMode.SuperSet;
                    HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
                    Debug.Log($"GameLoader: Loaded AOT Metadata for {aotDll}");
                }
                else
                {
                    Debug.LogError($"GameLoader: Failed to load AOT dll: {assetPath}. Error: {handle.OperationException}");
                }

                currentStep++;
                if (_hotUpdateView != null)
                {
                    float progress = (float)currentStep / totalSteps;
                    _hotUpdateView.RefreshUI(progress, $"加载系统资源... {(int)(progress * 100)}%");
                }
            }

            // 3. 加载热更新程序集
            foreach (var dllName in hotUpdateAssemblyFiles)
            {
                string assetPath = dllName;
                var handle = Addressables.LoadAssetAsync<TextAsset>(assetPath);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    byte[] dllBytes = handle.Result.bytes;
                    Assembly hotUpdateAss = Assembly.Load(dllBytes);
                    Debug.Log($"GameLoader: Loaded HotUpdate Assembly {dllName}");
                }
                else
                {
                    Debug.LogError($"GameLoader: Failed to load HotUpdate dll: {assetPath}. Error: {handle.OperationException}");
                }

                currentStep++;
                if (_hotUpdateView != null)
                {
                    float progress = (float)currentStep / totalSteps;   // 当前进度
                    _hotUpdateView.RefreshUI(progress, $"加载游戏资源... {(int)(progress * 100)}%");
                }
            }

            // 4. 检查远程资源并按需下载，然后进入游戏场景
            yield return FetchRemoteLabelDownloadSize();
        }

        /// <summary>
        /// 检查远程 Addressables 中指定标签是否有需要下载的内容
        /// </summary>
        private IEnumerator FetchRemoteLabelDownloadSize()
        {
            const string label = "All";

            if (_hotUpdateView != null)
            {
                _hotUpdateView.RefreshUI(1f / 3f, "正在检查更新...");
            }

            var sizeHandle = Addressables.GetDownloadSizeAsync(label);
            yield return sizeHandle;

            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"GameLoader: GetDownloadSizeAsync 失败, label: {label}, Error: {sizeHandle.OperationException}");
                Addressables.Release(sizeHandle);
                // 获取失败时直接进入游戏，避免卡在启动界面
                LoadGameScene();
                yield break;
            }

            long downloadSize = sizeHandle.Result;
            Addressables.Release(sizeHandle);

            if (downloadSize <= 0)
            {
                Debug.Log("GameLoader: 更新完成，直接进入游戏场景。");
                if (_hotUpdateView != null)
                {
                    _hotUpdateView.RefreshUI(1f, "更新完成，正在进入游戏...");
                }
                LoadGameScene();
                yield break;
            }

            Debug.Log($"GameLoader: 检测到需要下载的资源大小: {downloadSize / (1024f * 1024f):F2} MB, label: {label}");

            // 有更新时，启动下载协程
            yield return DownloadDependencies(label);

            // 下载完成后进入游戏
            LoadGameScene();
        }

        /// <summary>
        /// 下载指定标签的所有依赖，并实时更新进度条
        /// </summary>
        private IEnumerator DownloadDependencies(string label)
        {
            var downloadHandle = Addressables.DownloadDependenciesAsync(label);

            while (!downloadHandle.IsDone)
            {
                var status = downloadHandle.GetDownloadStatus();
                float percent = status.Percent; // 0-1
                float downloadedMB = status.DownloadedBytes / (1024f * 1024f);
                float totalMB = status.TotalBytes / (1024f * 1024f);

                if (_hotUpdateView != null)
                {
                    // 文本显示：已下载MB / 总MB
                    _hotUpdateView.RefreshUI(
                        percent,
                        $"正在下载更新... {downloadedMB:F2}MB / {totalMB:F2}MB"
                    );
                }

                yield return null;
            }

            if (downloadHandle.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError($"GameLoader: DownloadDependenciesAsync 失败, label: {label}, Error: {downloadHandle.OperationException}");
            }
            else
            {
                if (_hotUpdateView != null)
                {
                    _hotUpdateView.RefreshUI(1f, "更新完成，正在进入游戏...");
                }
                Debug.Log("GameLoader: 资源更新完成。");
            }

            Addressables.Release(downloadHandle);
        }

        /// <summary>
        /// 进入游戏场景
        /// </summary>
        private void LoadGameScene()
        {
            Debug.Log($"GameLoader: Loading Scene {entrySceneName}...");
            Addressables.LoadSceneAsync(entrySceneName);
        }
    }
}

