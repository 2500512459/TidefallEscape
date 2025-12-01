using UnityEngine;
using UnityEditor;
using System.IO;

public class FixIslandAssets : Editor
{
    private const string IslandPrefabsPath = "Assets/Prefabs/Island";
    private const string MeshSaveFolder   = "Assets/GeneratedMeshes/Islands";

    [MenuItem("Tools/Islands/Fix (Bake Mesh + Update Colliders)")]
    static void FixIslands()
    {
        Directory.CreateDirectory(MeshSaveFolder);
        AssetDatabase.Refresh();

        string[] prefabFiles = Directory.GetFiles(IslandPrefabsPath, "*.prefab", SearchOption.AllDirectories);
        int fixedCount = 0;

        foreach (string prefabPath in prefabFiles)
        {
            GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool prefabModified = false;

            MeshFilter[] filters = instance.GetComponentsInChildren<MeshFilter>(true);

            foreach (var mf in filters)
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null)
                    continue;

                // 检测临时 mesh：没有 assetPath → 必须 bake
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))
                {
                    // 复制 mesh（不能直接保存 runtime mesh）
                    Mesh baked = Object.Instantiate(mesh);
                    baked.name = mf.name + "_Baked";

                    string savePath = MeshSaveFolder + "/" + baked.name + ".asset";
                    AssetDatabase.CreateAsset(baked, savePath);

                    // 替换 MeshFilter
                    mf.sharedMesh = baked;
                    EditorUtility.SetDirty(mf);

                    // 替换 MeshCollider
                    MeshCollider collider = mf.GetComponent<MeshCollider>();
                    if (collider)
                    {
                        collider.sharedMesh = baked;
                        EditorUtility.SetDirty(collider);
                    }

                    prefabModified = true;
                }
            }

            if (prefabModified)
            {
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                fixedCount++;
            }

            PrefabUtility.UnloadPrefabContents(instance);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"岛屿 Mesh 修复完成，共 {fixedCount} 个预制体被修复（包含自动 Bake Mesh）");
    }
}
