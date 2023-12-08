using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameRunner : MonoBehaviour
{
    public float points;

    public TMP_Text pointsText;
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        pointsText.text = "$" + points;
    }

}
