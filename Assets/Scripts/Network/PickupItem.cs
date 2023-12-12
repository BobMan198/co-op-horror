using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PickupItem : NetworkBehaviour
{
    public NetworkVariable<bool> isPickedUp;
    public string ItemName;
    public GameObject ItemPrefab;
    public GameObject HeldItemPrefab;
}
