using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorridorEvent : MonoBehaviour
{

    public bool usedEvent;
    public Vector3 corridorEventPosition;
    private void Start()
    {
        GameRunner.corridorEvents.Add(this);
    }
    private void OnTriggerStay(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            corridorEventPosition = transform.position;
            usedEvent = true;
        }
    }
}
