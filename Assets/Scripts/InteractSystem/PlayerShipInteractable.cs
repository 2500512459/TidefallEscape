using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShipInteractable : BaseInteractable
{
    public Transform BoardingPoint;
    [Header("关联对象")]
    [SerializeField] private GameObject playerShip; // 船控制脚本所在对象，用于检查是否已在船上

    public override void Interact(Character player)
    {
        if (BoardingPoint != null)
        {
            player.transform.position = BoardingPoint.position;
        }
    }
    
    public override void OnFocus(Character player)
    {
        // 如果玩家已经在船上（PlayerShipCtrl 启用），不显示登船提示
        if (playerShip != null)
        {
            var shipCtrl = playerShip.GetComponent<PlayerShipCtrl>();
            if (shipCtrl != null && shipCtrl.enabled)
            {
                // 玩家已经在船上，不显示登船提示
                return;
            }
        }
        base.OnFocus(player);
    }
}
