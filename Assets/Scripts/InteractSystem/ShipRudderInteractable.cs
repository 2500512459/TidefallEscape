using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipRudderInteractable : BaseInteractable
{
    [Header("关联对象")]
    [SerializeField] private GameObject playerShip; // 船控制脚本所在对象
    [Header("船员模型")]
    [SerializeField] private GameObject CrewmanModel;       
    [Header("瞭望员模型")]
    [SerializeField] private GameObject LookoutModel;
    [Header("船长模型")]
    [SerializeField] private GameObject CaptainModel;
    [Header("船工模型")]
    [SerializeField] private GameObject ShipwrightModel;

    private GameObject currentPlayer;
    public override void Interact(Character player)
    {
        Debug.Log("ShipRudderInteractable: Interact");
        InteractHintUI.Instance.HideHint();
        currentPlayer = player.gameObject;
        var playerComponent = currentPlayer.GetComponent<Player>();
        if (playerComponent != null)
        {
            playerComponent.SetVitalityBarVisible(false);
        }
        currentPlayer.SetActive(false);

        if (PlayerDataManager.Instance.SelectedProfession == ProfessionType.Crewman)
        {
            CrewmanModel.SetActive(true);
        }
        else if (PlayerDataManager.Instance.SelectedProfession == ProfessionType.Lookout)
        {
            LookoutModel.SetActive(true);
        }
        else if (PlayerDataManager.Instance.SelectedProfession == ProfessionType.Captain)
        {
            CaptainModel.SetActive(true);
        }
        else if (PlayerDataManager.Instance.SelectedProfession == ProfessionType.Shipwright)
        {
            ShipwrightModel.SetActive(true);
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
