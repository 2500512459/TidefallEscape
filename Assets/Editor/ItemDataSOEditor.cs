#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemDataSO))]
public class ItemDataSOEditor : Editor
{
    private int itemCount;

    private void OnEnable()
    {
        // �������� ItemDataSO ʵ�������༭������Ч��
        string[] guids = AssetDatabase.FindAssets("t:ItemDataSO");
        itemCount = guids.Length;
    }

    public override void OnInspectorGUI()
    {
        // ����Ĭ�� Inspector
        base.OnInspectorGUI();

        // ��ʾ��ʾ��Ϣ
        EditorGUILayout.HelpBox($"��ǰ���� {itemCount} �� ItemDataSO", MessageType.Info);

    }
}
#endif
