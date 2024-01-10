using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkManagerSpawner : NetworkBehaviour
{

    [SerializeField]
    private GameObject networkManager;
    private void Awake()
    {
        DontDestroyOnLoad(this);

        GameObject network = Instantiate(networkManager, Vector3.zero, Quaternion.identity);
        network.GetComponent<NetworkObject>().Spawn();
    }

    private IEnumerator wait()
    {
        yield return new WaitForSeconds(2);

        networkManager.tag = ("NetworkManager");
    }
}
