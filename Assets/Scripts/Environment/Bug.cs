using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class Bug : NetworkBehaviour
{

    private NavMeshAgent agent;
    private float roamTimer;
    private const float roamTimerInterval = 3f;
    private Collider playerCollider;

    public NetworkVariable<bool> roaming = new NetworkVariable<bool>();
    public NetworkVariable<bool> scatter = new NetworkVariable<bool>();

    public override void OnNetworkSpawn()
    {
        agent = GetComponentInChildren<NavMeshAgent>();

        agent.Warp(transform.position = new Vector3(transform.position.x + (Random.Range(-0.2f, 0.2f)), transform.position.y, transform.position.z + (Random.Range(-0.2f, 0.2f))));
    }

    private void Update()
    {
        HandleRoam();

        if(scatter.Value)
        {
            HandleScatterServerRpc();
        }
    }
    private void OnTriggerStay(Collider target)
    {
        if (target.tag == "Player" && !scatter.Value)
        {
            playerCollider = target;
            HandleScatterServerRpc();
            FaceTarget();
        }
    }
    private void HandleRoam()
    {
        if(agent == null)
        {
            return;
        }

        var destDistance = Vector3.Distance(transform.position, agent.destination);

        if (roaming.Value)
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
                SetRoamingValueServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void FaceTargetServerRpc()
    {
        var turnTowardNavSteeringTarget = agent.steeringTarget;

        Vector3 direction = (turnTowardNavSteeringTarget - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5);
    }

    private void FaceTarget()
    {
        var turnTowardNavSteeringTarget = agent.steeringTarget;

        Vector3 direction = (turnTowardNavSteeringTarget - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleScatterServerRpc()
    {
        Vector3 direction = transform.position - playerCollider.transform.position;
        direction.Normalize();
        var destination = new Vector3(direction.x + (Random.Range(-4, 4)), direction.y = transform.position.y, direction.z + (Random.Range(-4, 4)));
        FaceTargetServerRpc();
        agent.SetDestination(destination);
        scatter.Value = true;

        if (roaming.Value)
        {
            roaming.Value = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetRoamingValueServerRpc()
    {
        scatter.Value = false;
        roaming.Value = true;
    }
}
