using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Dissonance.Integrations.Unity_NFGO.Demo;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField]
    private TMP_Text startGameRayText;

    [SerializeField] Vector3 myHands;
    [SerializeField] private GameObject itemHolder;
    private bool hasItem;

    public PickupItem ClosestPickupItem { get; private set; }
    List<PickupItem> _pickupItems = new List<PickupItem>();

    private GameObject heldItem;
    private NetworkObject lastPickupItemNetObj;
    private NetworkObject m_PickedUpObject;
    private GameObject itemOnFloor;
    private GameObject hitObject;

    public GameObject voiceChatHolder;
    

    public NetworkVariable<bool> isObjectPickedUp = new NetworkVariable<bool>();

    private void Start()
    {
        hasItem = false;
    }
    void Update()
    {
        ItemRay();
        StartGameRay();
        //RotateItem();
    }

    private void Awake()
    {
    }

    private void StartGameRay()
    {
        var ray = new Ray(cameraObject.transform.position, cameraObject.transform.forward);
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, Range))
        {
            if(hit.collider.CompareTag("StartGameTag"))
            {
                startGameRayText.gameObject.SetActive(true);
                if(Input.GetKey(KeyCode.E))
                {
                    StartGameRayServerRpc();
                }
            }
            else
            {
                startGameRayText.gameObject.SetActive(false);
            }
        }
    }

    [ServerRpc]
    public void StartGameRayServerRpc()
    {
        Debug.Log("Loading Game Scene");
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        voiceChatHolder.SetActive(true);
        //StartCoroutine(LoadYourAsyncScene());
    }

    IEnumerator LoadYourAsyncScene()
    {
        //NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Game");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        voiceChatHolder.SetActive(true);
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
                    hit.transform.gameObject.tag = "InHand";
                    hasItem = true;
                    pickupUI.gameObject.SetActive(false);

                    PickupObject();
                }
            }
        }
        else
        {
            pickupUI.gameObject.SetActive(false);
        }

        if (Input.GetKeyDown("g") && hasItem == true)
        {
            //DropHeldItemClientRpc();
            //DropHeldItemServerRpc(myHands.transform.position, myHands.transform.rotation);
            //myHands.GetComponentInChildren<Rigidbody>().isKinematic = false;

            //myHands.transform.GetChild(0).parent = null;
            hit.transform.gameObject.tag = "Item";
            DropObjectServerRpc();

            hasItem = false;
        }
    }

    public void PickupObject()
    {
        var netObj = hitObject.gameObject.GetComponent<PickupItem>().NetworkObject.NetworkObjectId;
        // Netcode is a server driven SDK. Shared objects like ingredients need to be interacted with using ServerRPCs. Therefore, there
        // will be a delay between the button press and the reparenting.
        // This delay could be hidden with some animations/sounds/VFX that would be triggered here.
        PickupObjectServerRpc(netObj);
    }

    [ServerRpc]
    public void PickupObjectServerRpc(ulong objToPickupID)
    {
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objToPickupID, out var objectToPickup);
        if (objectToPickup == null || objectToPickup.transform.parent != null) return; // object already picked up, server authority says no

        if (objectToPickup.TryGetComponent(out NetworkObject networkObject) && networkObject.TrySetParent(transform))
        {
            myHands = itemHolder.transform.localPosition;
            var pickUpObjectRigidbody = objectToPickup.GetComponent<Rigidbody>();
            pickUpObjectRigidbody.isKinematic = true;
            pickUpObjectRigidbody.interpolation = RigidbodyInterpolation.None;
            objectToPickup.GetComponent<NetworkTransform>().InLocalSpace = true;
            objectToPickup.transform.localRotation = itemHolder.transform.localRotation;
            objectToPickup.transform.localPosition = myHands;
            objectToPickup.tag = "InHand";
            //objectToPickup.gameObject.tag = "InHand";
            //objectToPickup.GetComponent<PickupItem>().itemDespawned += ItemDespawned;
            isObjectPickedUp.Value = true;
            m_PickedUpObject = objectToPickup;
            InHandItemTagClientRpc();
        }
    }

    [ClientRpc]

    public void InHandItemTagClientRpc()
    {
        m_PickedUpObject.tag = "InHand";
    }

    [ClientRpc]

    public void ItemTagClientRpc()
    {
        hitObject.tag = "Item";
        m_PickedUpObject = null;
    }
    public void RotateItem()
    {
        if (isObjectPickedUp.Value)
        {
            m_PickedUpObject.transform.rotation = itemHolder.transform.rotation;
        }
    }

[ServerRpc]
    public void DropObjectServerRpc()
    {
        if (m_PickedUpObject != null)
        {
            // can be null if enter drop zone while carrying
            m_PickedUpObject.transform.parent = null;
            var pickedUpObjectRigidbody = m_PickedUpObject.GetComponent<Rigidbody>();
            pickedUpObjectRigidbody.isKinematic = false;
            pickedUpObjectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            m_PickedUpObject.GetComponent<NetworkTransform>().InLocalSpace = false;
            //ItemTagClientRpc();
            //m_PickedUpObject.gameObject.tag = "Item";
            m_PickedUpObject.tag = "Item";
        }

        isObjectPickedUp.Value = false;
    }

    public void AttachPickupItem()
    {
        if (!IsOwner) return;
        AttachPickupItemServerRpc(hitObject.GetComponent<PickupItem>().NetworkObjectId);
    }

    [ServerRpc]
    public void AttachPickupItemServerRpc(ulong objectId)
    {
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var objToPickup);
        if (objToPickup == null || objToPickup.transform.parent != null) return;


        if (objToPickup.TryGetComponent(out NetworkObject networkObject) && networkObject.TrySetParent(transform))
        {
            //m_PickedUpObject = networkObject;
            //objToPickup.transform.position = myHands.transform.position;
           // objToPickup.transform.rotation = myHands.transform.rotation;
            objToPickup.GetComponent<Rigidbody>().isKinematic = true;
            objToPickup.GetComponent<PickupItem>().isPickedUp.Value = true;
           // objToPickup.TrySetParent(myHands.transform);
            AttachPickupItemClientRpc(objToPickup.NetworkObjectId);
            lastPickupItemNetObj = networkObject;
        }
    }

    void ItemDespawned()
    {
        m_PickedUpObject = null;
        isObjectPickedUp.Value = true;
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

       // instantiatedItem.transform.SetParent(myHands);

       // instantiatedItem.transform.localPosition = myHands.localPosition;
       // instantiatedItem.transform.localRotation = myHands.localRotation;
       // instantiatedItem.transform.GetComponent<Rigidbody>().isKinematic = true;
        heldItem = instantiatedItem;
        lastPickupItemNetObj.gameObject.SetActive(!lastPickupItemNetObj.gameObject.activeInHierarchy);
        //TogglePickupVisibilityClientRpc();
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



    [ServerRpc]
    private void DropHeldItemServerRpc(Vector3 position, Quaternion rotation)
    {
        if (lastPickupItemNetObj == null) { return; }

        lastPickupItemNetObj.transform.SetParent(null);
        lastPickupItemNetObj.transform.position = position;
        lastPickupItemNetObj.transform.rotation = rotation;
        lastPickupItemNetObj.GetComponent<PickupItem>().isPickedUp.Value = false;
        lastPickupItemNetObj.transform.GetComponent<Rigidbody>().isKinematic = false;
        //lastPickupItemNetObj.gameObject.SetActive(!lastPickupItemNetObj.gameObject.activeInHierarchy);
        lastPickupItemNetObj.gameObject.SetActive(true);
        Destroy(heldItem);
        //TogglePickupVisibilityClientRpc();
    }


    [ClientRpc]
    public void DropObjectClientRpc()
    {
        //m_PickedUpObject.gameObject.SetActive(true);
        //m_PickedUpObject.transform.position = myHands.transform.position;
        //m_PickedUpObject.transform.rotation = myHands.transform.rotation;
        //m_PickedUpObject.GetComponent<PickupItem>().isPickedUp.Value = false;
        //m_PickedUpObject.transform.GetComponent<Rigidbody>().isKinematic = false;
        //m_PickedUpObject.transform.parent = null;
       // m_PickedUpObject = null;
        Destroy(heldItem);
        //hitObject.gameObject.SetActive(true);
        //heldItem.gameObject.SetActive(false);

        //m_PickedUpObject.transform.position = myHands.transform.position;
        //m_PickedUpObject.transform.rotation = myHands.transform.rotation;
        //m_PickedUpObject.GetComponent<PickupItem>().isPickedUp.Value = false;
        //m_PickedUpObject.GetComponent<Rigidbody>().isKinematic = false;
        //m_PickedUpObject = null;
        TogglePickupVisibilityClientRpc();
    }
}
