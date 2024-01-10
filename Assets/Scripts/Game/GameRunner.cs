using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameRunner : MonoBehaviour
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

    [ClientRpc]
    public void HandlePointsClientRpc()
    {
        pointsText = GameObject.FindGameObjectWithTag("PointsTextTag").GetComponent<TMP_Text>();
        pointsText.text = $"${dayPoints}";
    }

    [ClientRpc]
    public void HandleDayChangeClientRpc()
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
