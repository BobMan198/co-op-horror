using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Dissonance;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
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
    public static List<SoundMonitor> soundMonitors;

    public NetworkVariable<float> n_points = new NetworkVariable<float>();
    public NetworkVariable<float> n_daypoints = new NetworkVariable<float>();
    public NetworkVariable<float> n_day = new NetworkVariable<float>();
    public NetworkVariable<float> n_quota = new NetworkVariable<float>();
    public NetworkVariable<float> n_viewers = new NetworkVariable<float>();
    public NetworkVariable<float> n_prevpoints = new NetworkVariable<float>();

    public NetworkVariable<bool> n_inGame = new NetworkVariable<bool>();

    public NetworkVariable<bool> textChatIntervalChange = new NetworkVariable<bool>();
    public NetworkVariable<float> textChatType = new NetworkVariable<float>();

    public NetworkVariable<float> n_poiTempPoints = new NetworkVariable<float>();
    public NetworkVariable<float> n_poiPoints = new NetworkVariable<float>();
    public NetworkVariable<float> n_poiTimer = new NetworkVariable<float>();
    public NetworkVariable<float> n_poiCounter = new NetworkVariable<float>();
    public NetworkVariable<float> n_viewerTimer = new NetworkVariable<float>();
    public NetworkVariable<float> n_seedCounter = new NetworkVariable<float>();
    public NetworkVariable<float> n_streamChatInterval = new NetworkVariable<float>();
    private float seedCounter;
    private const float poiTimerInterval = 150f;
    private const float viewerTimerInterval = 15f;

    public static System.Random randomSeed;
    public static System.Random RandomSeed => randomSeed == null ? new System.Random() : randomSeed;
    public static Transform PlayerSpawn;
    public NetworkVariable<int> GameSeed = new NetworkVariable<int>();
    private int clientSeed;
    private bool genClient;

    private bool gameEndDelaying = false;


    public static List<PlayerMovement> PlayerMovementList = new List<PlayerMovement>();
    public static PlayerController LocalPlayer;

    public DungeonCreator dungeonCreator;
    public MonsterSpawn monsterSpawn;

    public static List<CorridorEvent> corridorEvents;

    public static List<CollectibleItem> itemsCollected;

    public TMP_Text viewerText;

    private float timeSinceRecordedEvent;
    public float inGameCounter;

    public TMP_Text pointsText;


    private void Start()
    {
        itemsCollected = new List<CollectibleItem>();
        soundMonitors = new List<SoundMonitor>();
        corridorEvents = new List<CorridorEvent>();
        n_viewers.Value = 30;
        n_streamChatInterval.Value = 3;
    }

    private void Update()
    {
        clientSeed = GameSeed.Value;
        playersLoadedIn = PlayerMovementList.ToList().Select(p => p.transform).ToList();
        alivePlayers = GameObject.FindGameObjectsWithTag("Player").ToList();

        if(playersLoadedIn.Count > 0 && pointsText != null)
        {
            pointsText.text = $"${n_daypoints.Value}";
        }

        if (viewerText != null)
        {
            viewerText.text = "" + n_viewers.Value;
        }
        else
        {
            viewerText = FindObjectOfType<Tablet>().viewerText;
        }

        if (corridorEvents.Count == 0)
        {
            foreach (var corridorEvent in corridorEvents)
            {
                if(corridorEvent.usedEvent)
                {
                    //Do whatever the event is supposed to do
                }
            }
        }

        HandlePOI();
        HandleGameEndDead();
        HandleRoomSeed();
        HandleDungeonRemoval();
        HandleRespawnPlayers();
    }

    private void HandlePOI()
    {
        if(!n_inGame.Value || LocalPlayer.itemPickup.equippedLiveCamera == null)
        {
            return;
        }

        HandlePOITimerServerRpc();

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
        n_viewers.Value += 8;
        n_points.Value -= 1;
        n_poiCounter.Value = 0;
        textChatIntervalChange.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]

    private void LoseViewersServerRpc()
    {
        Debug.Log("Losing Viewers!");
        n_viewers.Value -= 3;
        n_poiCounter.Value = 0;
        textChatIntervalChange.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]

    private void HandleRandomViewerChangeServerRpc()
    {
        var viewChange = Random.Range(-20, 20);
        n_viewers.Value += viewChange;
    }

    private void HandleGameEndDead()
    {

        if(n_inGame.Value && !gameEndDelaying)
        {
            if(!GameObject.FindGameObjectWithTag("Player"))
            {
                StartCoroutine(DelayGameEnd());
                gameEndDelaying = true;
            }
        }
    }

    private IEnumerator DelayGameEnd()
    {
        yield return new WaitForSeconds(2);
        HandleResetServerRpc();
        gameEndDelaying = false;
    }

    public override void OnNetworkSpawn()
    {
        DontDestroyOnLoad(this.gameObject);
        n_quota.Value = STARTINGQUOTA;
        n_day.Value = 1;
        pointsText = LocalPlayer.playerUI.moneyText;

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
        foreach (var item in itemsCollected)
        {
            n_daypoints.Value += item.moneyWorth;
        }

        if (n_daypoints.Value >= n_quota.Value)
        {
            n_day.Value += 1;
            n_points.Value += n_daypoints.Value;
            n_prevpoints.Value = n_daypoints.Value;
            n_daypoints.Value = 0;
            n_quota.Value *= QUOTAMULTIPLIER;
            Debug.Log("Current quota " + n_quota.Value);
            Debug.Log("Current day " + n_day.Value);
            itemsCollected.Clear();
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
            n_quota.Value = STARTINGQUOTA;
            n_inGame.Value = false;
            Debug.Log("Current quota " + n_quota.Value);
            Debug.Log("Current day " + n_day.Value);

            NetworkManager.Singleton.SceneManager.LoadScene("HQ", LoadSceneMode.Single);
            dungeonCreator.DestroyGeneratedDungeon();
            monsterSpawn.DestroyMonsterServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyMonstersServerRpc()
    {
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
        Vector3 spawnPos = DungeonCreator.PlayerSpawnRoom.position;

        Vector3 offset = elevatorPos - spawnPos;
        offset.y = 0;
        dungeonCreator.generatedDungeonParent.transform.position += offset;
    }

    private void HandleDungeonRemoval()
    {
        if(!n_inGame.Value && dungeonCreator.generatedDungeonParent != null)
        {
            HandleDungeonRemovalServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void HandleDungeonRemovalServerRpc()
    {
        dungeonCreator.DestroyGeneratedDungeon();
        HandleDungeonRemovalClientRpc();
    }

    [ClientRpc]
    public void HandleDungeonRemovalClientRpc()
    {
        dungeonCreator.DestroyGeneratedDungeon();
    }

    private void HandleRespawnPlayers()
    {
        var scene = SceneManager.GetSceneByName("HQ");

        if(scene != null)
        {
            if(alivePlayers.Count < playersLoadedIn.Count)
            {
                RespawnPlayersServerRpc();
            }
        }
    }

    [ServerRpc]
    public void KillPlayerServerRpc(NetworkObjectReference player)
    {
        if (!player.TryGet(out NetworkObject networkObject))
        {
            Debug.Log("error");
        }

        networkObject.tag = "DeadPlayer";
        networkObject.gameObject.layer = default;
        var pm = networkObject.GetComponent<PlayerMovement>();
        pm.playerCamera.enabled = false;
        pm.spectatorCamera.gameObject.SetActive(true);
        pm.spectatorCamera.transform.SetParent(null);
        pm.fadeBlack.gameObject.SetActive(true);
        pm.controller.enabled = false;
        pm.audioListener.enabled = false;
        pm.playerStaminaUI.SetActive(false);
        pm.playerItemHolder.SetActive(false);
        GetComponent<ItemPickup>().DropObject2ServerRpc();
        var dissonance = FindObjectOfType<DissonanceComms>();
        dissonance.IsMuted = true;
    }

    [ServerRpc]
    public void RespawnPlayersServerRpc()
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        foreach (var player in playersLoadedIn)
        {
            player.tag = "Player";
            player.gameObject.layer = playerLayer;
            var pm = player.GetComponent<PlayerMovement>();
            pm.playerCamera.enabled = true;
            pm.spectatorCamera.gameObject.SetActive(false);
            pm.spectatorCamera.transform.SetParent(player);
            pm.fadeBlack.gameObject.SetActive(false);
            pm.controller.enabled = true;
            pm.audioListener.enabled = true;
            pm.playerStaminaUI.SetActive(true);
            pm.playerItemHolder.SetActive(true);
            var dissonance = FindObjectOfType<DissonanceComms>();
            dissonance.IsMuted = false;
        }
    }
}
