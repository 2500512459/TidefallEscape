#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemDataSO))]
public class ItemDataSOEditor : Editor
{
    private int itemCount;

    private void OnEnable()
    {
        // 查找所有 ItemDataSO 实例（仅编辑器下有效）
        string[] guids = AssetDatabase.FindAssets("t:ItemDataSO");
        itemCount = guids.Length;
    }

    public override void OnInspectorGUI()
    {
        // 绘制默认 Inspector
        base.OnInspectorGUI();

        // 显示提示信息
        EditorGUILayout.HelpBox($"当前已有 {itemCount} 个 ItemDataSO", MessageType.Info);

    }
}
#endif
