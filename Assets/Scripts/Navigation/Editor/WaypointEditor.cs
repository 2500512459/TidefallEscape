//using UnityEngine;
//using UnityEditor;

//namespace OceanQuest
//{
//    // 确保这个类继承自Editor类，并且使用CustomEditor属性指定它用于Waypoint组件
//    [CustomEditor(typeof(Waypoint))]
//    public class WaypointEditor : UnityEditor.Editor
//    {
//        public override void OnInspectorGUI()
//        {
//            // 首先绘制默认的Inspector GUI
//            DrawDefaultInspector();

//            // 添加一个按钮，如果按钮被点击
//            if (GUILayout.Button("AutoGenerateConnections"))
//            {
//                // 获取当前选中的Waypoint组件
//                Waypoint waypoint = (Waypoint)target;

//                // 调用AutoGenerateConnections方法
//                Undo.RecordObject(waypoint, "AutoGenerateConnections"); // 记录撤销操作
//                if (WaypointManager.Instance != null)
//                {
//                    waypoint.AutoGenerateConnections(WaypointManager.Instance.GetWaypoints().ToArray());
//                }
                
//                EditorUtility.SetDirty(waypoint); // 标记对象为脏，以便保存更改
//            }
//        }
//    }
//}