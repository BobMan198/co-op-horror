using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeMonkey.Utils;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : NetworkBehaviour
{
    public LayerMask HideableLayers;
    public EnemyLineOfSightChecker LineOfSightChecker;
    public NavMeshAgent Agent;
    [Range(-1, 1)]
    [Tooltip("Lower is a better hiding spot")]
    public float HideSensitivity = 0;
    [Range(1, 75)]
    public float MinPlayerDistance = 15f;
    [Range(0.01f, 1f)]
    public float UpdateFrequency = 0.25f;
    [Range(1f, 100f)]
    public float ColliderTest = 20f;

    private float resetEncountersTimer = 0f;
    private const float resetEncountersInterval = 15f;
    private float flashlightTimer = 0f;
    private const float flashlightInterval = 0.3f;
    private float flashlightOnTimer = 0f;
    private const float flashlightOnInterval = 0.3f;
    private float lastHideTimer = 0f;
    private const float lastHideInterval = 40f;

    public float chaseSpeed;
    public float catchDistance;

    [SerializeField]
    private List<GameObject> flashLights;

    [SerializeField]
    private List<GameObject> playerAloneColliders;

    [SerializeField]
    private List<GameObject> players;

    [SerializeField]
    public NetworkVariable<float> hideCounter = new NetworkVariable<float>();

    public NetworkVariable<bool> isHiding = new NetworkVariable<bool>();
    public NetworkVariable<bool> HideCoolDown = new NetworkVariable<bool>();
    public NetworkVariable<bool> Rage = new NetworkVariable<bool>();
    public NetworkVariable<bool> flashFlicker = new NetworkVariable<bool>();


    public NetworkObject n_closestPlayer;
    public GameObject c_closestPlayer;

    private Transform targetPlayer;

    public AudioSource neckSnapSound = null;

    private Collider[] Colliders = new Collider[100];

    private Vector3 destination;


    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Rage.Value = false;

        var playerLights = GameObject.FindGameObjectsWithTag("Flashlight");
        var playerAloneCollidersObject = GameObject.FindGameObjectsWithTag("AloneCollider");
        var playerObjects = GameObject.FindGameObjectsWithTag("Player");


        flashLights = playerLights.ToList().Select(p => p.gameObject).ToList();
        playerAloneColliders = playerAloneCollidersObject.ToList().Select(p => p.gameObject).ToList();
        players = playerObjects.ToList().Select(p => p.gameObject).ToList();
        

        // LineOfSightChecker.OnGainSight += HandleGainSight;
        //LineOfSightChecker.OnLoseSight += HandleLoseSight;
    }

    private void Update()
    {
        HideServerRpc();
        IfHidingServerRpc();
        HandleSwapOffHideServerRpc();
        HandleRageServerRpc();
        HandleEncounterTimerServerRpc();
        HandleAlonePlayersServerRpc();
        HandleFLashlightFlickerServerRpc();
        HandleMoodServerRpc();
        HandleClosestPlayerClientRpc();
        HandleRoamServerRpc();

        if (isHiding.Value == true)
        {
            lastHideTimer += Time.deltaTime;
        }
        else
        {
            lastHideTimer = 0;
        }

    }

    [ServerRpc(RequireOwnership = false)]

    private void HandleMoodServerRpc()
    {
        if(Rage.Value == true)
        {
            isHiding.Value = false;
        }

        if(isHiding.Value == true)
        {
            Rage.Value = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleFLashlightFlickerServerRpc()
    {
        flashlightTimer += Time.deltaTime;

        foreach (var flashlight in players)
        {
            var light = flashlight.GetComponent<Flashlight>();

            if (flashlightTimer >= flashlightInterval && flashFlicker.Value && isHiding.Value)
            {
                light.FlashFlickerOnClientRpc();
                flashlightTimer = 0;
            }
            if (flashlightTimer < flashlightInterval && flashFlicker.Value)
            {
                flashlightOnTimer += Time.deltaTime;
                if (flashlightOnTimer >= flashlightInterval && flashFlicker.Value)
                {
                    light.FlashFlickerOffClientRpc();
                    flashlightOnTimer = 0;
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleAlonePlayersServerRpc()
    {
        Vector3 currentPosition = transform.position;
        float distance = Mathf.Infinity;

        foreach (var player in playerAloneColliders)
        {
            if(player.GetComponent<AloneCollider>().playerAlone == true)
            {
                var alonePlayerTransform = player.GetComponentInParent<Transform>();

                Agent.destination = alonePlayerTransform.position;

                MoveEnemyServerRpc();

                if (Vector3.Distance(currentPosition, alonePlayerTransform.position) <= 2f)
                {
                    player.GetComponent<AloneCollider>().playerAlone = false; 
                }
            }
        }

    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleEncounterTimerServerRpc()
    {
        resetEncountersTimer += Time.deltaTime;

        if (isHiding.Value)
        {
            resetEncountersTimer = 0;
            return;
        }

        if (resetEncountersTimer >= resetEncountersInterval && !Rage.Value && hideCounter.Value >= 2)
        {
            hideCounter.Value -= 2;
            resetEncountersTimer = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleSwapOffHideServerRpc()
    {
        if(hideCounter.Value >= 10)
        {
            HideCoolDown.Value = true;
            hideCounter.Value = 0;
            Rage.Value = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]

    private void IfHidingServerRpc()
    {
        if (!Agent.isOnNavMesh)
        {
            return;
        }

        if (Agent.remainingDistance < 0.5f && !Agent.pathPending)
        {
            isHiding.Value = false;
            flashFlicker.Value = false;
            foreach (var flashlight in flashLights)
            {
                flashlight.SetActive(true);
            }
        }

        if (!LineOfSightChecker.InSight)
        {
            flashFlicker.Value = false;
            foreach (var flashlight in flashLights)
            {
                flashlight.SetActive(true);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]

    private void HandleRageServerRpc()
    {
        Vector3 currentPosition = transform.position;
        float distance = Mathf.Infinity;
        var player = LineOfSightChecker.PlayersWithVision;
        Transform closest = null;
        var dest = player.Select(p => p.position);

        if (Rage.Value && HideCoolDown.Value && !isHiding.Value)
        {

            if(LineOfSightChecker.InSight)
            {
                foreach(Transform pl in player)
                {
                    Vector3 diff = pl.transform.position - currentPosition;
                    float curDistance = diff.sqrMagnitude;
                    if (curDistance < distance)
                    {
                        closest = pl;
                        distance = curDistance;
                    }
                }

                c_closestPlayer = closest.gameObject;
                Agent.speed = chaseSpeed;
                Agent.destination = closest.position;
                FaceTarget();
                //MoveEnemyServerRpc();
            }
        }

        if(c_closestPlayer == null)
        {
            return;
        }

        if (Rage.Value == true && Vector3.Distance(currentPosition, closest.position) <= 2f)
        {
            //closestPlayer = closest;
            Rage.Value = false;
            HideCoolDown.Value = false;
            KillClosestPlayerServerRpc(closest.gameObject);
            KillClosestPlayerClientRpc();
            HideAfterKillServerRpc();
            if (neckSnapSound.isPlaying) return;

            neckSnapSound.Play();
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

    [ClientRpc]
    private void HandleClosestPlayerClientRpc()
    {
        Vector3 currentPosition = transform.position;
        float distance = Mathf.Infinity;
        var player = LineOfSightChecker.PlayersWithVision;
        Transform closest = null;
        var dest = player.Select(p => p.position);

        if (LineOfSightChecker.InSight)
        {
            foreach (Transform pl in player)
            {
                Vector3 diff = pl.transform.position - currentPosition;
                float curDistance = diff.sqrMagnitude;
                if (curDistance < distance)
                {
                    closest = pl;
                    distance = curDistance;
                }
            }

            c_closestPlayer = closest.gameObject;
        }
    }

    [ServerRpc(RequireOwnership = false)]

    private void HideServerRpc()
    {
        if(HideCoolDown.Value || Rage.Value)
        {
            return;
        }

        if (!LineOfSightChecker.InSight || isHiding.Value)
        {
            return;
        }

        for (int i = 0; i < Colliders.Length; i++)
        {
            Colliders[i] = null;
        }
        
        int hits = Physics.OverlapSphereNonAlloc(Agent.transform.position, LineOfSightChecker.Collider.radius, Colliders, HideableLayers);

        Vector3 averagePlayerPosition = GetAverageVector(LineOfSightChecker.Players.Select(p => p.position).ToList());

        int hitReduction = 0;
        for (int i = 0; i < hits; i++)
        {
            if (Vector3.Distance(Colliders[i].transform.position, averagePlayerPosition) < MinPlayerDistance)
            {
                Colliders[i] = null;
                hitReduction++;
            }
        }
        hits -= hitReduction;

        System.Array.Sort(Colliders, ColliderArraySortComparer);

        for (int i = 0; i < hits; i++)
        {
            if (NavMesh.SamplePosition(Colliders[i].transform.position, out NavMeshHit hit, ColliderTest, Agent.areaMask))
            {
                if (!NavMesh.FindClosestEdge(hit.position, out hit, Agent.areaMask))
                {
                    Debug.LogError($"Unable to find edge close to {hit.position}");
                }

                float Dot = Vector3.Dot(hit.normal, (averagePlayerPosition - hit.position).normalized);

                //if (Dot < HideSensitivity)
                
                    if (NavMesh.SamplePosition(Colliders[i].transform.position - (averagePlayerPosition - hit.position).normalized * ColliderTest, out NavMeshHit hit2, ColliderTest, Agent.areaMask))
                    {
                        if (!NavMesh.FindClosestEdge(hit2.position, out hit2, Agent.areaMask))
                        {
                            Debug.LogError($"Unable to find edge close to {hit2.position}");
                        }

                        if (Vector3.Dot(hit2.normal, (averagePlayerPosition - hit2.position).normalized) < HideSensitivity)
                        {
                            destination = hit2.position;
                            Agent.destination = hit2.position;
                            //MoveEnemyServerRpc();
                            Debug.Log("Found NAV destination!2");
                            flashFlicker.Value = true;
                            hideCounter.Value++;
                            isHiding.Value = true;
                            break;
                        }
                    }
                    else
                    {
                        if (Vector3.Dot(hit.normal, (averagePlayerPosition - hit.position).normalized) < HideSensitivity)
                        {
                            //if (NavMesh.SamplePosition(Colliders[i].transform.position - (averagePlayerPosition - hit.position).normalized * ColliderTest, out NavMeshHit hit, ColliderTest, Agent.areaMask))
                            //{
                                Agent.SetDestination(hit.position);
                                Debug.Log("Found NAV destination!");
                                flashFlicker.Value = true;
                            //}
                            //hideCounter++;
                            isHiding.Value = true;
                            break;

                        }
                    }
                }
            else
            {
                Debug.LogError($"Unable to find NavMesh near object {Colliders[i].name} at {Colliders[i].transform.position}");
            }
        }
    }

    private Vector3 GetAverageVector(List<Vector3> positions)
    {
        if (positions.Count == 0)
            return Vector3.zero;

        float x = 0f;
        float y = 0f;
        float z = 0f;

        foreach (Vector3 pos in positions)
        {
            x += pos.x;
            y += pos.y;
            z += pos.z;
        }

        return new Vector3(x / positions.Count, y / positions.Count, z / positions.Count);
    }

    private int ColliderArraySortComparer(Collider A, Collider B)
    {
        if (A == null && B != null)
        {
            return 1;
        }
        else if (A != null && B == null)
        {
            return -1;
        }
        else if (A == null && B == null)
        {
            return 0;
        }
        else
        {
            return Vector3.Distance(Agent.transform.position, A.transform.position).CompareTo(Vector3.Distance(Agent.transform.position, B.transform.position));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HideAfterKillServerRpc()
    {
        int hits = Physics.OverlapSphereNonAlloc(Agent.transform.position, LineOfSightChecker.Collider.radius, Colliders, HideableLayers);

        Vector3 averagePlayerPosition = GetAverageVector(LineOfSightChecker.Players.Select(p => p.position).ToList());

        int hitReduction = 0;
        for (int i = 0; i < hits; i++)
        {
            if (Vector3.Distance(Colliders[i].transform.position, averagePlayerPosition) < MinPlayerDistance)
            {
                Colliders[i] = null;
                hitReduction++;
            }
        }
        hits -= hitReduction;

        System.Array.Sort(Colliders, ColliderArraySortComparer);

        for (int i = 0; i < hits; i++)
        {
            if (NavMesh.SamplePosition(Colliders[i].transform.position, out NavMeshHit hit, ColliderTest, Agent.areaMask))
            {
                if (!NavMesh.FindClosestEdge(hit.position, out hit, Agent.areaMask))
                {
                    Debug.LogError($"Unable to find edge close to {hit.position}");
                }

                float Dot = Vector3.Dot(hit.normal, (averagePlayerPosition - hit.position).normalized);

                //if (Dot < HideSensitivity)

                if (NavMesh.SamplePosition(Colliders[i].transform.position - (averagePlayerPosition - hit.position).normalized * ColliderTest, out NavMeshHit hit2, ColliderTest, Agent.areaMask))
                {
                    if (!NavMesh.FindClosestEdge(hit2.position, out hit2, Agent.areaMask))
                    {
                        Debug.LogError($"Unable to find edge close to {hit2.position}");
                    }

                    if (Vector3.Dot(hit2.normal, (averagePlayerPosition - hit2.position).normalized) < HideSensitivity)
                    {
                        destination = hit2.position;
                        Agent.destination = hit2.position;
                        //MoveEnemyServerRpc();
                        Debug.Log("Found NAV destination!2");
                        flashFlicker.Value = true;
                        hideCounter.Value++;
                        isHiding.Value = true;
                        break;
                    }
                }
                else
                {
                    if (Vector3.Dot(hit.normal, (averagePlayerPosition - hit.position).normalized) < HideSensitivity)
                    {
                        //if (NavMesh.SamplePosition(Colliders[i].transform.position - (averagePlayerPosition - hit.position).normalized * ColliderTest, out NavMeshHit hit, ColliderTest, Agent.areaMask))
                        //{
                        Agent.SetDestination(hit.position);
                        Debug.Log("Found NAV destination!");
                        flashFlicker.Value = true;
                        //}
                        //hideCounter++;
                        isHiding.Value = true;
                        break;

                    }
                }
            }
            else
            {
                Debug.LogError($"Unable to find NavMesh near object {Colliders[i].name} at {Colliders[i].transform.position}");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveEnemyServerRpc()
    {
        Agent.SetDestination(destination);
        FaceTarget();
    }


    [ServerRpc(RequireOwnership = false)]
    private void HandleRoamServerRpc()
    {
        if(isHiding.Value == true)
        {
            lastHideTimer = 0;
        }

        lastHideTimer += Time.deltaTime;

        if(lastHideTimer >= lastHideInterval)
        {
            HideAfterKillServerRpc();
            lastHideTimer = 0;
        }
    }

    private void FaceTarget()
    {
        var turnTowardNavSteeringTarget = Agent.steeringTarget;

        Vector3 direction = (turnTowardNavSteeringTarget - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10);
    }
}
