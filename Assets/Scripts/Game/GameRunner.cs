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

    public float points;
    public float dayPoints;
    public float day = 1;
    public float quota;
    public float viewers;
    private NetworkVariable<float> n_points = new NetworkVariable<float>();
    private NetworkVariable<float> n_daypoints = new NetworkVariable<float>();
    private NetworkVariable<float> n_day = new NetworkVariable<float>();
    private NetworkVariable<float> n_quota = new NetworkVariable<float>();
    private NetworkVariable<float> n_viewers = new NetworkVariable<float>();

    private float timeSinceRecordedEvent;

    public TMP_Text pointsText;
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        quota = STARTINGQUOTA;
    }

    private void Update()
    {
        var playerScripts = FindObjectsOfType<PlayerMovement>();
        playersLoadedIn = playerScripts.ToList().Select(p => p.transform).ToList();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }
    }

        private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        StartCoroutine(StartChangingNetworkVariable());
    }

    private IEnumerator StartChangingNetworkVariable()
    {
        var updateFrequency = new WaitForSeconds(0.5f);

        n_points.Value = points;
        n_day.Value = day;
        n_daypoints.Value = dayPoints;
        n_quota.Value = quota;
        n_viewers.Value = quota;

        yield return updateFrequency;

        NetworkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
    }
    public void HandlePoints()
    {
        pointsText = GameObject.FindGameObjectWithTag("PointsTextTag").GetComponent<TMP_Text>();
        pointsText.text = $"${dayPoints}";
    }
    public void HandleDayChange()
    {
        if (dayPoints >= quota)
        {
            day += 1;
            points += dayPoints;
            dayPoints = 0;
            quota *= QUOTAMULTIPLIER;
            Debug.Log("Current quota " + quota);
            Debug.Log("Current day " + day);
        }
    }

}
