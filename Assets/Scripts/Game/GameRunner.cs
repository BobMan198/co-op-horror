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
    private const float poiTimerInterval = 150f;
    private const float viewerTimerInterval = 15f;

    public GameObject monsterPrefab;
    public TMP_Text viewerText;

    private Vector3 monsterSpawn;

    private float timeSinceRecordedEvent;
    public float inGameCounter;

    public TMP_Text pointsText;

    private void Start()
    {
        n_viewers.Value = 100;
    }

    private void Update()
    {
        var playerScripts = FindObjectsOfType<PlayerMovement>();
        playersLoadedIn = playerScripts.ToList().Select(p => p.transform).ToList();

        if (n_inGame.Value == true)
        {
            pointsText = GameObject.FindGameObjectWithTag("PointsTextTag").GetComponent<TMP_Text>();
            pointsText.text = $"${n_daypoints.Value}";
        }

        HandlePOI();
        HandleGameEndDead();
    }

    private void HandlePOI()
    {
        if(!n_inGame.Value)
        {
            return;
        }

        HandlePOITimerServerRpc();
        viewerText = GameObject.FindGameObjectWithTag("viewerText").GetComponent<TMP_Text>();

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

    [ServerRpc]
    public void HandleMonsterSpawnServerRpc()
    {
            monsterSpawn = GameObject.FindGameObjectWithTag("MonsterSpawn").transform.position;

            GameObject monster = Instantiate(monsterPrefab, monsterSpawn, Quaternion.identity);
            monster.GetComponent<NetworkObject>().Spawn();
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
            Debug.Log("Current quota " + n_quota.Value);
            Debug.Log("Current day " + n_day.Value);

            NetworkManager.Singleton.SceneManager.LoadScene("HQ", LoadSceneMode.Single);
            n_inGame.Value = false;
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

}
