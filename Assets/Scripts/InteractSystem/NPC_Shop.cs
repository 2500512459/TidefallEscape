using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Shop : BaseInteractable
{
    public ShopType shopType = ShopType.WeaponShop;
    public override void Interact(Character player)
    {
        if (ShopUI.Instance.IsVisible)
        {
            ShopUI.Instance.HidePanel();
            InteractHintUI.Instance.HideHint();
            PlayerInput.Instance.EnableAllInputs();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            ShopUI.Instance.ShowPanel(shopType);
            InteractHintUI.Instance.ShowHint("进入商店", key);
            PlayerInput.Instance.DisableAllInputsExcept(PlayerInput.Instance.playerInputAction.Control.InteractionEvent);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

    }
}
