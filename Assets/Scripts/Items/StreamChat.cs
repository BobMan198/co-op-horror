using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Random = UnityEngine.Random;

public class StreamChat : NetworkBehaviour
{
    public GameRunner gameRunner;
    private TMP_Text streamChat;

    public GameObject chatPanel, textObject;
    private const float maxMessages = 8;
    //public TMP_Text streamChatTextBox;

    public List<string> idleMessages = new List<string>();
    public List<string> recordingMessages = new List<string>();
    public List<string> monsterMessages = new List<string>();

    private float chatTimer;
    private void Start()
    {
        var iM = idleMessages;
        var rM = recordingMessages;
        var mM = monsterMessages;


        //Idle Chat Messages
        iM.Add("Made a sammich :)");
        iM.Add("shit’s boring");
        iM.Add("tell us a story");
        iM.Add("where are you?");
        iM.Add("are yall single?");
        iM.Add("I’m back.");
        iM.Add("Hey everyone! I’m new here! <3");
        iM.Add("Go back to grapevine.");
        iM.Add("dis place sos carry");
        iM.Add("What do you all think about the new Hyperionalion XII?");
        iM.Add("Shut up nerd.");
        iM.Add("who asked?");
        iM.Add("Hey everyone, gonna lurk for you all. Yell when you need me.");

        //Recording Event Chat Messages
        rM.Add("What was that?");
        rM.Add("Yo whats that?");
        rM.Add("Huh");
        rM.Add("uhhhhh");
        rM.Add("??????");
        rM.Add("?");
        rM.Add("the fuck?");
        rM.Add("fake");
        rM.Add("fake encounters");
        rM.Add("not real");
        rM.Add("Holy shit???");
        rM.Add("No way");
        rM.Add("can you get closer?");
        rM.Add("touch it >:)");
        rM.Add("planted");
        rM.Add("is this fake?");
        rM.Add("yall need to LEAVE!!!!!");
        rM.Add("gtfo");
        rM.Add("what did i miss?");

        //Monster Encounter Chat Messages

        mM.Add("RUNNNN");
        mM.Add("You guys still think this shit is real? Its so fucking fake.");
        mM.Add("!!!!!");
        mM.Add("THe FUCK??");
        mM.Add("CALL 911");
        mM.Add("oh no");
        mM.Add("Lol");
    }

    private void Update()
    {
        // if(gameRunner.n_inGame.Value == false)
        //{
        //    return;
        // }

        if(gameRunner.textChatIntervalChange.Value == true)
        {
            ChangeRatioServerRpc();
        }

        if(streamChat == null)
        {
            return;
        }
        chatTimer += Time.deltaTime;

        if (chatTimer >= gameRunner.n_streamChatInterval.Value)
        {
            //chatPanel = GameObject.FindGameObjectWithTag("TabletContentUI");
            streamChat = chatPanel.GetComponentInChildren<TMP_Text>();

            if (streamChat == null)
            {
                return;
            }

            string lB = "\n";

            if (gameRunner.textChatType.Value == 0)
            {
                string randomIdleMessage = idleMessages[Random.Range(0, idleMessages.Count)];
                AddMessage(randomIdleMessage + lB);
                chatTimer = 0;
            }
            else if(gameRunner.textChatType.Value == 1)
            {
                string randomRecordingMessage = recordingMessages[Random.Range(0, recordingMessages.Count)];
                AddMessage(randomRecordingMessage + lB);
                chatTimer = 0;
            }
            else if(gameRunner.textChatType.Value == 2)
            {
                string randomMonsterMessage = monsterMessages[Random.Range(0, monsterMessages.Count)];
                AddMessage(randomMonsterMessage + lB);
                chatTimer = 0;
            }
        }
    }


    Queue<string> messages = new();

    private void AddMessage(string message)
    {
        messages.Enqueue(message);

        while (messages.Count > maxMessages) messages.Dequeue();

        Display();
    }

    void Display()
    {
        StringBuilder sb = new();
        foreach (string message in messages) sb.Append(message);

        streamChat.text = sb.ToString();
    }

    [ServerRpc]
    private void ChangeRatioServerRpc()
    {
        var deduct = gameRunner.n_viewers.Value *= 0.01f;
        gameRunner.n_streamChatInterval.Value -= deduct;
        gameRunner.textChatIntervalChange.Value = false;
    }
}
