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
        dayText.text = "<b>Day:<br></b>" + gameRunner.day;
        revenueText.text = "<b>Revenue Made:<br></b>" + gameRunner.dayPoints;
        quotaText.text = "<b><size=80%>Next Expected Revenue:<br></b><size=100%>" + gameRunner.quota;
        totalRevenueText.text = "<b>Banked<br></b>$" + gameRunner.points;
    }
}
