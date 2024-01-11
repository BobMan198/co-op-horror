using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReverbColliderChecker : MonoBehaviour
{
    public bool inReverbZone;
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "ReverbCollider")
        {
            inReverbZone = true;
        }
        else
        {
            inReverbZone = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        inReverbZone = false;
    }
}
