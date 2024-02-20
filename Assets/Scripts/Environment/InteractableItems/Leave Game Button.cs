using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaveGameButton : InteractableItem
{
    public override void Interact()
    {
        LeaveMapServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void LeaveMapServerRpc()
    {
        var rrps = FindObjectsOfType<RecordRemotePlayers>();
        var monsterspawn = FindAnyObjectByType<MonsterSpawn>();

        Debug.Log("Leaving Map!");
        NetworkManager.Singleton.SceneManager.LoadScene("HQ", LoadSceneMode.Single);
        GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameRunner>().HandleDayChangeServerRpc();
        monsterspawn.DestroyMonsterServerRpc();
        monsterspawn.n_monsterSpawned.Value = false;

        foreach (var rrp in rrps)
        {
            rrp.DeleteAudioFiles();
        }
    }
}
