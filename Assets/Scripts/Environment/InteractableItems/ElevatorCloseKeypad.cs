using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorCloseKeypad : InteractableItem
{
    public AudioSource closeDoorSource;
    public ElevatorController elevatorController;
    public override void Interact()
    {
        elevatorController.TryStartElevatorServerRpc();
        closeDoorSource.Play();
    }
}
