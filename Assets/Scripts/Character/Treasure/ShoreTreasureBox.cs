using UnityEngine;

/// <summary>
/// 岸上宝箱：继承基础 TreasureBox，首次开启后直接替换 Mesh。
/// </summary>
public class ShoreTreasureBox : TreasureBox
{
    private MeshFilter meshFilter;
    [Tooltip("宝箱打开后的网格")]
    [SerializeField] private Mesh openedMesh;

    protected override void Start()
    {
        base.Start();
        meshFilter = GetComponentInChildren<MeshFilter>();
    }

    public override void TryOpen()
    {
        bool wasOpened = opened;
        base.TryOpen();

        if (!wasOpened && opened)
            ApplyOpenedMesh();
    }

    private void ApplyOpenedMesh()
    {
        if (meshFilter == null || openedMesh == null)
            return;

        meshFilter.sharedMesh = openedMesh;
    }
}

