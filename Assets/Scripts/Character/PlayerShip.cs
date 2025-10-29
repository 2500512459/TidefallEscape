using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家船（继承 Character）
/// 周期性检测周围 TreasureBox，并只让最近的一个显示提示（其余隐藏）
/// </summary>
public class PlayerShip : Character
{
    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }
}
