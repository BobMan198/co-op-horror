using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TVText : MonoBehaviour
{

    private GameRunner gameRunner;

    [SerializeField]
    private TMP_Text dayText;
    [SerializeField]
    private TMP_Text revenueText;
    [SerializeField]
    private TMP_Text quotaText;
    [SerializeField]
    private TMP_Text totalRevenueText;
    void Start()
    {
        gameRunner = FindAnyObjectByType<GameRunner>();
    }

    void Update()
    {
        HandleTVTextServerRpc();
    }

    [ServerRpc]
    private void HandleTVTextServerRpc()
    {
        dayText.text = "Day: " +
            "  " + gameRunner.day;
        revenueText.text = "Revenue Made:" +
            "  " + gameRunner.dayPoints;
        quotaText.text = "Next Expected Revenue: " +
            " " + gameRunner.quota;
        totalRevenueText.text = "$" + gameRunner.points;
    }
}
