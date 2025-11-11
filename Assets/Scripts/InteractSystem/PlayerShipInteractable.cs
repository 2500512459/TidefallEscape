using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShipInteractable : BaseInteractable
{
    public Transform BoardingPoint;

    public override void Interact(Character player)
    {
        if (BoardingPoint != null)
        {
            player.transform.position = BoardingPoint.position;
        }
    }
}
