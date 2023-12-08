using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class RelayManager : MonoBehaviour
{
    public Scene gameScene;

    private NetworkObject player;

    public Object playerObject;



    //public const string KEY_START_GAME = "StartGame";


    private async void Start()
    {
       // await UnityServices.InitializeAsync();
//
       // AuthenticationService.Instance.SignedIn += () =>
      //  {
      //      Debug.Log("Signed in" + AuthenticationService.Instance.PlayerId);
      //  };
//
     //   await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void Update()
    {
        DisconnectPlayer();

    }

    private void DisconnectPlayer()
    {
        NetworkManager networkManager = GameObject.FindObjectOfType<NetworkManager>();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();

                GameObject.Destroy(networkManager.gameObject);
                GameObject.Destroy(networkManager.gameObject);

                LobbyManager.Instance.StopGame();

                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);

                //SceneManager.LoadScene("Lobby");

                SceneManager.sceneLoaded += OnSceneLoaded;

                Cleanup();
            }

            if(!NetworkManager.Singleton.IsConnectedClient)
            {
                SceneManager.LoadScene("Lobby");
            }

            if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);

                GameObject.Destroy(networkManager.gameObject);
                GameObject.Destroy(networkManager.gameObject);

                LobbyManager.Instance.LeaveLobby();

                SceneManager.LoadScene("Lobby");

                SceneManager.sceneLoaded += OnSceneLoaded;

                Cleanup();
            }
        }
    }

    private void OnSceneLoaded(Scene Lobby, LoadSceneMode singleMode)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Cleanup();
    }

    void Cleanup()
    {
        //if (NetworkManager.Singleton != null)
       // {
            NetworkManager networkManager = GameObject.FindObjectOfType<NetworkManager>();
            Destroy(NetworkManager.Singleton.gameObject);
            //GameObject.Destroy(networkManager.gameObject);
       // }
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
            Debug.Log("NetworkCallbackService OnClientDisconnectCallback: " + clientId);

            NetworkManager networkManager = GameObject.FindObjectOfType<NetworkManager>();

            GameObject.Destroy(networkManager.gameObject);

            NetworkManager.Singleton.Shutdown();

            SceneManager.LoadScene("Lobby");
    }

    public async Task<string> CreateRelay()
    {
        try
        {
           Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

           string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(joinCode);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            //NetworkManager.Singleton.StartHost();

            return joinCode;

        } catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

   private void DisconnectPlayer(NetworkObject player)
    {
        // Note: If a client invokes this method, it will throw an exception.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            NetworkManager.Singleton.DisconnectClient(player.OwnerClientId);
            SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
    }


    public async void JoinRelay(string joinCode)
    {

        try
        {
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            LoadGame();

        } catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
       // var clientId = request.ClientNetworkId;
       // Debug.Log("Approval check for clientId " + clientId);

        /* you can use this method in your project to customize one of more aspects of the player
         * (I.E: its start position, its character) and to perform additional validation checks. */
       // response.Approved = true;
       // response.CreatePlayerObject = false;
      // // response.Position = GetPlayerSpawnPosition();
      //  Debug.Log("Player object spawning");
    }

    public void LoadGame()
    {
        //Debug.Log("Starting Game!");

        //NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApprovalCallback;

        //NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.StartClient();

        //NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);

        //SceneManager.LoadScene("Game", LoadSceneMode.Single);

        //NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApprovalCallback;

        //NetworkManager.Singleton.StartClient();

       // Debug.Log("Loading game!");

    }

    Vector3 GetPlayerSpawnPosition()
    {
        /*
         * this is just an example, and you change this implementation to make players spawn on specific spawn points
         * depending on other factors (I.E: player's team)
         */
        return new Vector3(Random.Range(-3, 3), 0, Random.Range(-3, 3));
    }

}
