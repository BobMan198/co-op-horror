using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadlampCheck : MonoBehaviour
{
    public bool readyToToggleHeadLamp;

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "HeadLamp")
        {
            readyToToggleHeadLamp = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "HeadLamp")
        {
            readyToToggleHeadLamp = false;
        }
    }
}
