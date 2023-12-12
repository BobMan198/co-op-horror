using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ItemPickup : NetworkBehaviour
{
    [SerializeField]
    private Transform cameraObject;
    [SerializeField]
    private float Range;
    [SerializeField]
    private TMP_Text pickupUI;
    [SerializeField]
    private TMP_Text debugText;

    [SerializeField] Transform myHands;
    private bool hasItem;

    public PickupItem ClosestPickupItem { get; private set; }
    List<PickupItem> _pickupItems = new List<PickupItem>();

    private GameObject heldItem;
    private NetworkObject lastPickupItemNetObj;
    private GameObject itemOnFloor;
    private GameObject hitObject;

    private void Start()
    {
        hasItem = false;
    }
    void Update()
    {
        ItemRay();
    }

    private void ItemRay()
    {
        var ray = new Ray(cameraObject.transform.position, cameraObject.transform.forward);
        RaycastHit hit;


        if (Physics.Raycast(ray, out hit, Range))
        {
            if (hasItem == false && hit.collider.CompareTag("Item"))
            {
                debugText.text = hit.collider.gameObject.name;
                hitObject = hit.collider.gameObject;

                Debug.Log(itemOnFloor);
                pickupUI.gameObject.SetActive(true);
                if (Input.GetKeyDown("e"))
                {
                   // hit.transform.GetComponent<Rigidbody>().isKinematic = true; 
                   // hit.transform.position = myHands.transform.position;
                  //  hit.transform.rotation = myHands.transform.rotation;
                   // hit.transform.parent = myHands.transform;
                    hasItem = true;
                    pickupUI.gameObject.SetActive(false);

                    AttachPickupItem();
                }
            }
        }
        else
        {
            pickupUI.gameObject.SetActive(false);
        }

        if (Input.GetKeyDown("g") && hasItem == true) 
        {
            myHands.GetComponentInChildren<Rigidbody>().isKinematic = false;

            myHands.transform.GetChild(0).parent = null; 

            hasItem = false;
        }
    }

    public void AttachPickupItem()
    {
        if (!IsOwner) return;
        AttachPickupItemServerRpc();
    }

    [ServerRpc]
    public void AttachPickupItemServerRpc()
    {

            itemOnFloor = hitObject;
            ClosestPickupItem = itemOnFloor.GetComponent<PickupItem>();

            _pickupItems.Remove(ClosestPickupItem);
            //DropHeldItem(transform.position, transform.rotation);

            var netObj = itemOnFloor.GetComponent<NetworkObject>();
            ClosestPickupItem.isPickedUp.Value = true;
            AttachPickupItemClientRpc(netObj.NetworkObjectId);
            lastPickupItemNetObj = netObj;
    }

    [ClientRpc]
    public void AttachPickupItemClientRpc(ulong objectId)
    {
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var objToPickup);
        lastPickupItemNetObj = objToPickup;
        var pickup = objToPickup.GetComponent<PickupItem>();

        //Destroy the previously held item if there is one
        Destroy(heldItem);

        var instantiatedItem = Instantiate(pickup.HeldItemPrefab);

        instantiatedItem.transform.SetParent(myHands);

       // switch (pickup)
       // {
       //     case pickup.:
       //         instantiatedItem.transform.SetParent(javelinSocket);
       //         break;
       //     default:
        //        Debug.LogError("Unimplemented pickup type: " + pickup.pickupType);
        //        break;
                //case PickupType.bow:
                //    break;
                //case PickupType.rock:
                //    break;
                //case PickupType.branch:
                //    break;
      //  }
        instantiatedItem.transform.localPosition = myHands.localPosition;
        instantiatedItem.transform.localRotation = myHands.localRotation;
        instantiatedItem.transform.GetComponent<Rigidbody>().isKinematic = true;
        heldItem = instantiatedItem;
        TogglePickupVisibilityClientRpc();
    }

    [ClientRpc]
    public void TogglePickupVisibilityClientRpc()
    {
        lastPickupItemNetObj.gameObject.SetActive(!lastPickupItemNetObj.gameObject.activeInHierarchy);
    }

    public PickupItem GetLastPickupItem()
    {
        return lastPickupItemNetObj?.GetComponent<PickupItem>();
    }

    public GameObject GetHeldItem()
    {
        return heldItem;
    }
}
