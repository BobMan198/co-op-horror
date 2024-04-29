using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SocialPlatforms;

public class RoachKingMovement : MonoBehaviour
{
    public NetworkVariable<bool> isRoaming = new NetworkVariable<bool>();
    public NetworkVariable<bool> isChasing = new NetworkVariable<bool>();
    public NetworkVariable<float> roamTimer = new NetworkVariable<float>();
    private bool startTimer = true;
    private const float roamTimerInterval = 15;
    private DungeonCreator creator;
    public NavMeshAgent roachKingAgent;
    public EnemyLineOfSightChecker los;
    private Vector3 roamPosition;

    private static bool CancelRoam;

    [SerializeField]
    private GameObject c_closestPlayer;

    public NetworkObject n_closestPlayer;
    void Start()
    {
        creator = FindAnyObjectByType<DungeonCreator>();
        roachKingAgent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleRoamServerRpc();
        HandleChaseServerRpc();

        if(CancelRoam == true)
        {
            isRoaming.Value = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleRoamServerRpc()
    {

        if(startTimer && !isRoaming.Value && !isChasing.Value)
        {
            roamTimer.Value += Time.deltaTime;
        }

        if(roamTimer.Value >= roamTimerInterval && !isRoaming.Value && !isChasing.Value)
        {
            int index = Random.Range(0, creator.generatedRooms.Count);
            var room = creator.generatedRooms[index];
            Vector3 roamPos = room.floor.meshRenderer.bounds.center;
            roachKingAgent.speed = 3;
            roachKingAgent.destination = roamPos;
            FaceTarget();
            isRoaming.Value = true;
            startTimer = false;
            roamPosition = roamPos;
        }

        if(Vector3.Distance(transform.position, roamPosition) <= 5)
        {
            isRoaming.Value = false;
            startTimer = true;
            roamTimer.Value = 0;
        }
    }

    [ServerRpc]

    private void HandleChaseServerRpc()
    {
        if(los.InSight)
        {
            Vector3 currentPosition = transform.position;
            float distance = Mathf.Infinity;
            var players = los.PlayersWithVision;
            GameObject closest = null;
            var dest = players.Select(p => p.transform.position);

            isChasing.Value = true;

            if (isChasing.Value)
            {
                    foreach (GameObject player in players)
                    {
                        Vector3 diff = player.transform.position - currentPosition;
                        float curDistance = diff.sqrMagnitude;
                        if (curDistance < distance)
                        {
                            closest = player;
                            distance = curDistance;
                        }
                    }

                    c_closestPlayer = closest.gameObject;
                    roachKingAgent.speed = 6;
                    roachKingAgent.destination = closest.transform.position;
                    FaceTarget();
                    roamTimer.Value = 0;
                if (Vector3.Distance(currentPosition, closest.transform.position) <= 2f)
                {
                    isChasing.Value = false;
                    KillClosestPlayerServerRpc(closest.gameObject);
                    KillClosestPlayerClientRpc();
                }
            }
        }
        else
        {
            isChasing.Value = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void KillClosestPlayerServerRpc(NetworkObjectReference closestPlayer)
    {
        n_closestPlayer = closestPlayer;
        n_closestPlayer.tag = "DeadPlayer";
        n_closestPlayer.gameObject.layer = default;
        c_closestPlayer = n_closestPlayer.gameObject;
    }

    [ClientRpc]
    private void KillClosestPlayerClientRpc()
    {
        c_closestPlayer.tag = "DeadPlayer";
        c_closestPlayer.gameObject.layer = default;
        c_closestPlayer.GetComponent<PlayerMovement>().playerHealth = 0;
    }

    private void FaceTarget()
    {
        var turnTowardNavSteeringTarget = roachKingAgent.steeringTarget;

        Vector3 direction = (turnTowardNavSteeringTarget - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10);
    }

    [ServerRpc(RequireOwnership = false)]
    public void InvestigateDisturbanceServerRpc(Vector3 bugPosition)
    {
        // TODO: prevent destination from being updated more than once per second
        CancelRoam = true;
        roachKingAgent.destination = bugPosition;
        roachKingAgent.speed += 1;
    }
}
