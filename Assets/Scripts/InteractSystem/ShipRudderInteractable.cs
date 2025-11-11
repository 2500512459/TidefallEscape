using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipRudderInteractable : BaseInteractable
{
    [Header("关联对象")]
    [SerializeField] private GameObject playerShip; // 船控制脚本所在对象
    [SerializeField] private GameObject shipDriverModel;       // 船上驾驶员模型

    private GameObject currentPlayer;
    public override void Interact(Character player)
    {
        InteractHintUI.Instance.HideHint();
        currentPlayer = player.gameObject;
        currentPlayer.SetActive(false);

        if (shipDriverModel != null)
        {
            shipDriverModel.SetActive(true);
        }

        if (playerShip != null)
        {
            var shipCtrl = playerShip.GetComponent<PlayerShipCtrl>();
            if (shipCtrl != null)
            {
                shipCtrl.enabled = true;
                shipCtrl.EnterControl(currentPlayer); // 传递玩家引用（便于退出控制）
            }

            var weaponInDicator = playerShip.GetComponent<WeaponIndicator>();
            if (weaponInDicator != null)
            {
                weaponInDicator.enabled = true;
            }

            var rb = playerShip.GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.None;
        }
    }
}
