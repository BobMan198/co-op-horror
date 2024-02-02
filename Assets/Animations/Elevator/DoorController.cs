using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Animator leftEDoor;
    public Animator rightEDoor;

    public void OpenElevatorDoors()
    {
        leftEDoor.SetTrigger("DoorOpen");
        rightEDoor.SetTrigger("DoorOpen");
    }

    public void CloseElevatorDoors()
    {
        leftEDoor.SetTrigger("DoorClose");
        rightEDoor.SetTrigger("DoorClose");
    }

    public void ResetElevatorDoors()
    {
        leftEDoor.ResetTrigger("DoorClose");
        leftEDoor.ResetTrigger("DoorOpen");
        rightEDoor.ResetTrigger("DoorClose");
        rightEDoor.ResetTrigger("DoorOpen");
    }
}
