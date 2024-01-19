using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
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

    public GameObject monsterPrefab;

    private Vector3 monsterSpawn;

    private float timeSinceRecordedEvent;
    public float inGameCounter;

    public TMP_Text pointsText;

    private void Update()
    {
        var playerScripts = FindObjectsOfType<PlayerMovement>();
        playersLoadedIn = playerScripts.ToList().Select(p => p.transform).ToList();

        if (n_inGame.Value == true)
        {
            pointsText = GameObject.FindGameObjectWithTag("PointsTextTag").GetComponent<TMP_Text>();
            pointsText.text = $"${n_daypoints.Value}";
        }

    }

    [ServerRpc]
    public void HandleMonsterSpawnServerRpc()
    {
            monsterSpawn = GameObject.FindGameObjectWithTag("MonsterSpawn").transform.position;

            GameObject monster = Instantiate(monsterPrefab, monsterSpawn, Quaternion.identity);
            monster.GetComponent<NetworkObject>().Spawn();
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
    }

}
