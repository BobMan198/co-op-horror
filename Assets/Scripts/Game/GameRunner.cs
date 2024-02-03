using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameRunner : NetworkBehaviour
{
    [SerializeField]
    private float QUOTAMULTIPLIER = 1.5f;
    [SerializeField]
    private float STARTINGQUOTA = 30f;

    public List<Transform> playersLoadedIn;
    public List<GameObject> alivePlayers;

    public NetworkVariable<float> n_points = new NetworkVariable<float>();
    public NetworkVariable<float> n_daypoints = new NetworkVariable<float>();
    public NetworkVariable<float> n_day = new NetworkVariable<float>();
    public NetworkVariable<float> n_quota = new NetworkVariable<float>();
    public NetworkVariable<float> n_viewers = new NetworkVariable<float>();
    public NetworkVariable<float> n_prevpoints = new NetworkVariable<float>();

    public NetworkVariable<bool> n_inGame = new NetworkVariable<bool>();

    public NetworkVariable<float> n_poiTempPoints = new NetworkVariable<float>();
    public NetworkVariable<float> n_poiPoints = new NetworkVariable<float>();
    public NetworkVariable<float> n_poiTimer = new NetworkVariable<float>();
    public NetworkVariable<float> n_poiCounter = new NetworkVariable<float>();
    public NetworkVariable<float> n_viewerTimer = new NetworkVariable<float>();
    public NetworkVariable<float> n_seedCounter = new NetworkVariable<float>();
    private float seedCounter;
    private const float poiTimerInterval = 150f;
    private const float viewerTimerInterval = 15f;

    public static System.Random randomSeed;
    public static System.Random RandomSeed => randomSeed == null ? new System.Random() : randomSeed;
    public static Transform PlayerSpawn;
    public NetworkVariable<int> GameSeed = new NetworkVariable<int>();
    private int clientSeed;
    private bool genClient;


    public static List<PlayerMovement> PlayerMovementList = new List<PlayerMovement>();
    public static PlayerController LocalPlayer;

    public DungeonCreator dungeonCreator;
    public MonsterSpawn monsterSpawn;

    
    public TMP_Text viewerText;

    private float timeSinceRecordedEvent;
    public float inGameCounter;

    public TMP_Text pointsText;


    private void Start()
    {
        n_viewers.Value = 100;
    }

    private void Update()
    {
        clientSeed = GameSeed.Value;
        playersLoadedIn = PlayerMovementList.ToList().Select(p => p.transform).ToList();
        alivePlayers = GameObject.FindGameObjectsWithTag("Player").ToList();


        if (n_inGame.Value == true)
        {
            pointsText = LocalPlayer.playerUI.moneyText;
            pointsText.text = $"${n_daypoints.Value}";
        }

        HandlePOI();
        HandleGameEndDead();
        HandleRoomSeed();
    }

    private void HandlePOI()
    {
        if(!n_inGame.Value || LocalPlayer.itemPickup.equippedLiveCamera == null)
        {
            return;
        }

        HandlePOITimerServerRpc();
        viewerText = LocalPlayer.itemPickup.equippedLiveCamera.viewerText;

        //if poi temp points is >= 2 then points goes up
        if (n_poiTempPoints.Value >= 2)
        {
            HandleTempPointsServerRpc();
        }

        //adds viewers
        if(n_poiPoints.Value == 1) 
        {
            AddViewersServerRpc();
        }

        //if poi counter is >= 8 then lose viewers and reset counter
        if(n_poiCounter.Value >= 2)
        {
            LoseViewersServerRpc();
        }

        //picking random number after 10 seconds and adding that to viewer count
        if(n_viewerTimer.Value >= viewerTimerInterval)
        {
            HandleRandomViewerChangeServerRpc();
        }

        viewerText.text = "" + n_viewers.Value;
    }

    [ServerRpc(RequireOwnership = false)]

    private void HandleTempPointsServerRpc()
    {
        n_poiPoints.Value = 1;
        n_poiTempPoints.Value = 0;
    }

    [ServerRpc(RequireOwnership = false)]

    private void HandleAfterTimerServerRpc()
    {
        n_poiTempPoints.Value = 0;
        n_poiTimer.Value = 0;
        n_poiCounter.Value += 1;
    }

    [ServerRpc(RequireOwnership = false)]

    private void AddViewersServerRpc()
    {
        Debug.Log("Adding Viewers!");
        n_viewers.Value += 120;
        n_points.Value -= 1;
        n_poiCounter.Value = 0;
    }

    [ServerRpc(RequireOwnership = false)]

    private void LoseViewersServerRpc()
    {
        Debug.Log("Losing Viewers!");
        n_viewers.Value -= 35;
        n_poiCounter.Value = 0;
    }

    [ServerRpc(RequireOwnership = false)]

    private void HandleRandomViewerChangeServerRpc()
    {
        var viewChange = Random.Range(-20, 20);
        n_viewers.Value += viewChange;
    }

    private void HandleGameEndDead()
    {
        if(n_inGame.Value)
        {
            if(!GameObject.FindGameObjectWithTag("Player"))
            {
                HandleResetServerRpc();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        DontDestroyOnLoad(this.gameObject);
        n_quota.Value = STARTINGQUOTA;
        n_day.Value = 1;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }
    }

        private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        //StartCoroutine(StartChangingNetworkVariable());
    }

    [ServerRpc(RequireOwnership = false)]
    public void HandleDayChangeServerRpc()
    {
        if (n_daypoints.Value >= n_quota.Value)
        {
            n_day.Value += 1;
            n_points.Value += n_daypoints.Value;
            n_prevpoints.Value = n_daypoints.Value;
            n_daypoints.Value = 0;
            n_quota.Value *= QUOTAMULTIPLIER;
            Debug.Log("Current quota " + n_quota.Value);
            Debug.Log("Current day " + n_day.Value);
        }
        else
        {
            HandleResetServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void HandleResetServerRpc()
    {
            n_day.Value = 1;
            n_daypoints.Value = 0;
            n_prevpoints.Value = n_daypoints.Value;
            n_quota.Value = 0;
            n_inGame.Value = false;
            Debug.Log("Current quota " + n_quota.Value);
            Debug.Log("Current day " + n_day.Value);

            NetworkManager.Singleton.SceneManager.LoadScene("HQ", LoadSceneMode.Single);
            dungeonCreator.DestroyAllChildren();
            monsterSpawn.DestroyMonsterServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandlePOITimerServerRpc()
    {
        n_poiTimer.Value += Time.deltaTime;

        if (n_poiTimer.Value >= poiTimerInterval)
        {
            HandleAfterTimerServerRpc();
        }
    }
    private void HandleRoomSeed()
    {
        if(n_inGame.Value && seedCounter <= 0 && IsServer)
        {
            GenerateRoomSeedServerRpc();
            seedCounter++;
        }

        if (n_inGame.Value && seedCounter == 1 && IsClient && genClient)
        {
            StartCoroutine(wait3());
            //GenerateRoomSeedClientRpc();
            genClient = false;
            seedCounter++;
        }

        if (!n_inGame.Value)
        {
            seedCounter = 0;
            genClient = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void GenerateRoomSeedServerRpc()
    {
        var rng = new System.Random();
        GameSeed.Value = rng.Next(0, 1000000);
        randomSeed = new System.Random(GameSeed.Value);
        clientSeed = GameSeed.Value;
       // dungeonCreator.CreateDungeon();
        genClient = true;
    }

    private IEnumerator wait3()
    {
        yield return new WaitForSeconds(1);

        GenerateRoomSeedClientRpc();
    }

    [ClientRpc]
    public void GenerateRoomSeedClientRpc()
    { 
        clientSeed = GameSeed.Value;
        randomSeed = new System.Random(clientSeed);
        dungeonCreator.CreateDungeon();
        Debug.LogError(GameSeed.Value);

        // offset dungeon to align with elevator
        Vector3 elevatorPos = ElevatorController.elevatorPosition;
        Vector3 spawnPos = dungeonCreator.PlayerSpawnRoom.position;

        Vector3 offset = elevatorPos - spawnPos;
        offset.y = 0;
        dungeonCreator.transform.position += offset;
    }
}
