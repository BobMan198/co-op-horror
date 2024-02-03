using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorCloseKeypad : InteractableItem
{
    public ElevatorController elevatorController;
    public override void Interact()
    {
        elevatorController.TryStartElevator();
    }
}
