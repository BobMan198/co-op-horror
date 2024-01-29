
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using static UnityEngine.GraphicsBuffer;

public class Bug : NetworkBehaviour
{

    private NavMeshAgent agent;
    private float roamTimer;
    private const float roamTimerInterval = 3f;
    private Collider playerCollider;
    private CockroachManager cockroachManager;
    private RoachKingMovement kingMovement;

    

    public bool scatter;
    public bool roaming;

    public override void OnNetworkSpawn()
    {
        agent = GetComponentInChildren<NavMeshAgent>();
        agent.Warp(transform.position = new Vector3(transform.position.x + (Random.Range(-0.2f, 0.2f)), transform.position.y, transform.position.z + (Random.Range(-0.2f, 0.2f))));
        cockroachManager = FindObjectOfType<CockroachManager>();
    }
    private void Update()
    {
        if(IsServer)
        {
            HandleRoam();
        }

        Scene currentScene = SceneManager.GetActiveScene();

        string sceneName = currentScene.name;

        if (sceneName == "HQ")
        {
            Destroy(gameObject);
        }
    }
    private void OnTriggerStay(Collider target)
    {
        if(IsServer)
        {
            if (target.tag == "Player" && !scatter)
            {
                playerCollider = target;
                HandleScatterServerRpc();
                FaceTarget();
            }
        }
    }
    private void HandleRoam()
    {
        if (agent == null)
        {
            return;
        }
        var destDistance = Vector3.Distance(transform.position, agent.destination);

        if (roaming)
        {
            var destination = new Vector3(transform.position.x + (Random.Range(-0.1f, 0.1f)), transform.position.y, transform.position.z + (Random.Range(-0.1f, 0.1f)));
            agent.SetDestination(destination);
            FaceTarget();
        }
        if (destDistance <= 0.3f)
        {
            roamTimer += Time.deltaTime;

            var roamRandomInterval = 1;
            if (roamTimer >= roamRandomInterval)
            {
                roamRandomInterval = Random.Range(1, 2);
                roamTimer = 0;
                SetRoamingValue();
            }
        }
    }
    private void FaceTarget()
    {
        var turnTowardNavSteeringTarget = agent.steeringTarget;

        Vector3 direction = (turnTowardNavSteeringTarget - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10);
    }

    [ServerRpc]
    private void HandleScatterServerRpc()
    {
        Vector3 destination = transform.position;
        destination.x += Random.Range(-4, 4);
        destination.z += Random.Range(-4, 4);
        FaceTarget();
        agent.SetDestination(destination);
        scatter = true;
        cockroachManager.cockroachsStressed.Value++;
        RoachKingMovement.InvestigateDisturbanceServerRpc(transform.position);
        if (roaming)
        {
            roaming = false;
        }
    }

    private void SetRoamingValue()
    {
        scatter = false;
        roaming = true;
    }
}