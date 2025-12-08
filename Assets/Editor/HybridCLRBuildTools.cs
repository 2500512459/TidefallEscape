using System.IO;
using UnityEditor;
using UnityEngine;

public class HybridCLRBuildTools
{
    [MenuItem("HybridCLR/Build/Copy DLLs to Addressables")]
    public static void CopyDlls()
    {
        string targetDir = "Assets/HotUpdateResources/Dlls";
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string buildTargetName = target.ToString(); 

        // 1. Copy HotUpdate DLLs
        // We assume "HotUpdate" is the main hot update assembly.
        string[] hotUpdateAssemblies = new string[] { "HotUpdate" }; 

        string hotUpdateSourceDir = $"HybridCLRData/HotUpdateDlls/{buildTargetName}";
        
        foreach (var dll in hotUpdateAssemblies)
        {
            string dllPath = $"{hotUpdateSourceDir}/{dll}.dll";
            string destPath = $"{targetDir}/{dll}.dll.bytes";
            if (File.Exists(dllPath))
            {
                 File.Copy(dllPath, destPath, true);
                 Debug.Log($"[HybridCLRBuildTools] Copied {dll}.dll to {destPath}");
            }
            else 
            {
                 Debug.LogError($"[HybridCLRBuildTools] HotUpdate dll not found: {dllPath}. Please run HybridCLR -> CompileDll first.");
            }
        }
        
        // 2. Copy AOT Metadata DLLs
        // These correspond to the list in GameLoader.cs
        string[] aotAssemblies = new string[] 
        { 
            "mscorlib", 
            "System", 
            "System.Core" 
        };
        
        string aotSourceDir = $"HybridCLRData/AssembliesPostIl2CppStrip/{buildTargetName}";

        foreach (var dll in aotAssemblies)
        {
             string dllFileName = dll + ".dll";
             string src = $"{aotSourceDir}/{dllFileName}";
             string dest = $"{targetDir}/{dllFileName}.bytes";
             
             if (File.Exists(src))
             {
                 File.Copy(src, dest, true);
                 Debug.Log($"[HybridCLRBuildTools] Copied AOT dll {dllFileName} to {dest}");
             }
             else
             {
                 Debug.LogWarning($"[HybridCLRBuildTools] AOT dll not found: {src}. This is expected if you haven't built the player yet. Make sure to build the player to generate stripped AOT dlls.");
             }
        }
        
        AssetDatabase.Refresh();
    }
}

