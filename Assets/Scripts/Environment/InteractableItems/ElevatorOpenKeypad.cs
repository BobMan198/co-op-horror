using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorOpenKeypad : InteractableItem
{
    public override void Interact()
    {
        var doorController = FindObjectOfType<DoorController>();
        doorController.OpenElevatorDoors(false);
    }
}
