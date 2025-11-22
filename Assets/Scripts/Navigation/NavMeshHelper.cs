using UnityEngine;
#if UNITY_AI_NAVIGATION
using Unity.AI.Navigation;
#endif

/// <summary>
/// NavMesh 辅助组件
/// 用于在运行时查找和使用 NavMeshSurface
/// </summary>
public class NavMeshHelper : MonoBehaviour
{
#if UNITY_AI_NAVIGATION
    private static NavMeshSurface navMeshSurface;

    /// <summary>
    /// 获取场景中的 NavMeshSurface（单例模式）
    /// </summary>
    public static NavMeshSurface GetNavMeshSurface()
    {
        if (navMeshSurface == null)
        {
            navMeshSurface = FindObjectOfType<NavMeshSurface>();
        }
        return navMeshSurface;
    }

    /// <summary>
    /// 检查 NavMesh 是否已烘焙
    /// </summary>
    public static bool IsNavMeshBaked()
    {
        var surface = GetNavMeshSurface();
        return surface != null && surface.navMeshData != null;
    }
#endif
}

