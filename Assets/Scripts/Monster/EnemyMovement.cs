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

    public enum ShadowMonsterState
    {
        idle,
        hiding,
        hidingafter,
        chase
    }

    [SerializeField]
    public NetworkVariable<ShadowMonsterState> networkShadowMonsterState = new NetworkVariable<ShadowMonsterState>();

    private GameRunner gameRunner;
    public LayerMask HideableLayers;
    public EnemyLineOfSightChecker LineOfSightChecker;
    public NavMeshAgent ShadowMonsterAgent;
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
    private const float resetEncountersInterval = 45f;
    private float flashlightTimer = 0f;
    private const float flashlightInterval = 0.3f;
    private float flashlightOnTimer = 0f;
    private const float flashlightOnInterval = 0.3f;
    [SerializeField]
    private float hidingTimer = 0f;
    private const float hidingInterval = 6f;
    [SerializeField]
    private float lastHidingTimer = 0f;
    private const float lastHidingInterval = 20f;

    private const float hideIntervalBeforeChase = 6;

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

    public NetworkVariable<bool> flashFlicker = new NetworkVariable<bool>();


    public NetworkObject n_closestPlayer;
    public GameObject c_closestPlayer;

    private Transform targetPlayer;

    public AudioSource shadowMonsterSoundSource;
    public AudioClip hideSound1;
    public AudioClip snapNeck;

    private Collider[] Colliders = new Collider[100];

    private Vector3 destination;

    public Animator ShadowMonsterAnimator;

    private bool hidingAfterKill;
    private bool isHiding;
    private float isHidingTimer;
    private const float isHidingTimerInterval = 4;



    private void Awake()
    {
        gameRunner = FindObjectOfType<GameRunner>();

        var playerLights = GameObject.FindGameObjectsWithTag("Flashlight");
        //var playerAloneCollidersObject = GameObject.FindGameObjectsWithTag("AloneCollider");
        var playerObjects = gameRunner.alivePlayers;


        flashLights = playerLights.ToList().Select(p => p.gameObject).ToList();
        //playerAloneColliders = playerAloneCollidersObject.ToList().Select(p => p.gameObject).ToList();
        players = playerObjects.ToList().Select(p => p.gameObject).ToList();
        

        // LineOfSightChecker.OnGainSight += HandleGainSight;
        //LineOfSightChecker.OnLoseSight += HandleLoseSight;
    }

    private void Update()
    {
        HandleActiveMood();
        HandleClosestPlayerClientRpc();

        if (IsServer)
        {
            HandleMoodServerRpc();
            HandleRoamServerRpc();
            IfHidingServerRpc();
            HandleEncounterTimerServerRpc();
            HandleFLashlightFlickerServerRpc();
        }
    }

    [ServerRpc]

    private void HandleMoodServerRpc()
    {
        if (!IsServer)
        {
            return;
        }

        if (LineOfSightChecker.InSight)
        {
            if(hideCounter.Value <= hideIntervalBeforeChase)
            {
                UpdateShadowMonsterStateServerRpc(ShadowMonsterState.hiding);
            }
            else
            {
                UpdateShadowMonsterStateServerRpc(ShadowMonsterState.chase);
            }
        }
        else
        {
            if(networkShadowMonsterState.Value != ShadowMonsterState.hidingafter)
            {
                UpdateShadowMonsterStateServerRpc(ShadowMonsterState.idle);
            }
        }

        if (networkShadowMonsterState.Value == ShadowMonsterState.hidingafter && ShadowMonsterAgent.remainingDistance <= 1.5f && ShadowMonsterAgent.remainingDistance >= 0.5)
        {
            UpdateShadowMonsterStateServerRpc(ShadowMonsterState.idle);
        }

        if (networkShadowMonsterState.Value == ShadowMonsterState.hiding)
        {
            hidingTimer += Time.deltaTime;
            if(hidingTimer >= isHidingTimerInterval)
            {
                isHiding = false;
            }
        }

        if (networkShadowMonsterState.Value == ShadowMonsterState.hiding && ShadowMonsterAgent.remainingDistance <= 1.5f && ShadowMonsterAgent.remainingDistance >= 0.5)
        {
            UpdateShadowMonsterStateServerRpc(ShadowMonsterState.idle);
        }

        if (networkShadowMonsterState.Value != ShadowMonsterState.idle)
        {
            if(LineOfSightChecker.InSight)
            {
                flashFlicker.Value = true;
            }
        }
    }

    [ServerRpc]
    public void UpdateShadowMonsterStateServerRpc(ShadowMonsterState newState)
    {
        networkShadowMonsterState.Value = newState;
    }

    private void HandleActiveMood()
    {
        if(networkShadowMonsterState.Value == ShadowMonsterState.idle)
        {
            ShadowMonsterAnimator.SetFloat("Shadow", 0);
            ShadowMonsterAgent.ResetPath();
        }

        if (networkShadowMonsterState.Value == ShadowMonsterState.hiding)
        {
            HideServerRpc();
            ShadowMonsterAnimator.SetFloat("Shadow", 1);
        }

        if (networkShadowMonsterState.Value == ShadowMonsterState.chase)
        {
            HandleRageServerRpc();
            ShadowMonsterAnimator.SetFloat("Shadow", 2);
        }

        if(networkShadowMonsterState.Value == ShadowMonsterState.hidingafter)
        {
            ShadowMonsterAnimator.SetFloat("Shadow", 1);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleFLashlightFlickerServerRpc()
    {
        flashlightTimer += Time.deltaTime;

        foreach (var flashlight in players)
        {
            var light = flashlight.GetComponent<Flashlight>();

            if(!light.isActiveAndEnabled)
            {
                return;
            }

            if (flashlightTimer >= flashlightInterval && flashFlicker.Value && networkShadowMonsterState.Value == ShadowMonsterState.hiding)
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

    [ServerRpc]
    private void HandleEncounterTimerServerRpc()
    {
        resetEncountersTimer += Time.deltaTime;

        if (networkShadowMonsterState.Value == ShadowMonsterState.hiding)
        {
            resetEncountersTimer = 0;
            return;
        }

        if (resetEncountersTimer >= resetEncountersInterval && networkShadowMonsterState.Value != ShadowMonsterState.chase && hideCounter.Value >= 1)
        {
            hideCounter.Value -= 1;
            resetEncountersTimer = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]

    private void IfHidingServerRpc()
    {
        if (!ShadowMonsterAgent.isOnNavMesh)
        {
            return;
        }

        if (ShadowMonsterAgent.remainingDistance < 0.5f && !ShadowMonsterAgent.pathPending)
        {
            isHiding = false;
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

    [ServerRpc]

    private void HandleRageServerRpc()
    {
        Vector3 currentPosition = transform.position;
        float distance = Mathf.Infinity;
        var player = LineOfSightChecker.PlayersWithVision;
        GameObject closest = null;
        var dest = player.Select(p => p.transform.position);

        

            if(LineOfSightChecker.InSight)
            {
                foreach(GameObject pl in player)
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
                ShadowMonsterAgent.speed = chaseSpeed;
                ShadowMonsterAgent.destination = closest.transform.position;
                FaceTarget();
                //MoveEnemyServerRpc();
            }
        

        if(c_closestPlayer == null || closest == null)
        {
            return;
        }

        if (Vector3.Distance(currentPosition, closest.transform.position) <= 2f)
        {
            //closestPlayer = closest;
            //if (!shadowMonsterSoundSource.isPlaying)
            //{
                shadowMonsterSoundSource.PlayOneShot(snapNeck);
            //}
            KillClosestPlayerServerRpc(closest.gameObject);
            KillClosestPlayerClientRpc();
            HideAfterKillServerRpc();
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
        GameObject closest = null;
        var dest = player.Select(p => p.transform.position);

        if (LineOfSightChecker.InSight)
        {
            foreach (GameObject pl in player)
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

    [ServerRpc]

    private void HideServerRpc()
    {
        if (!LineOfSightChecker.InSight || isHiding)
        {
            return;
        }

        isHiding = true;

        for (int i = 0; i < Colliders.Length; i++)
        {
            Colliders[i] = null;
        }
        
        int hits = Physics.OverlapSphereNonAlloc(ShadowMonsterAgent.transform.position, LineOfSightChecker.Collider.radius, Colliders, HideableLayers);

        Vector3 averagePlayerPosition = GetAverageVector(LineOfSightChecker.Players.Select(p => p.transform.position).ToList());

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
            if (NavMesh.SamplePosition(Colliders[i].transform.position, out NavMeshHit hit, ColliderTest, ShadowMonsterAgent.areaMask))
            {
                if (!NavMesh.FindClosestEdge(hit.position, out hit, ShadowMonsterAgent.areaMask))
                {
                    Debug.LogError($"Unable to find edge close to {hit.position}");
                }

                float Dot = Vector3.Dot(hit.normal, (averagePlayerPosition - hit.position).normalized);

                //if (Dot < HideSensitivity)
                
                    if (NavMesh.SamplePosition(Colliders[i].transform.position - (averagePlayerPosition - hit.position).normalized * ColliderTest, out NavMeshHit hit2, ColliderTest, ShadowMonsterAgent.areaMask))
                    {
                        if (!NavMesh.FindClosestEdge(hit2.position, out hit2, ShadowMonsterAgent.areaMask))
                        {
                            Debug.LogError($"Unable to find edge close to {hit2.position}");
                        }

                        if (Vector3.Dot(hit2.normal, (averagePlayerPosition - hit2.position).normalized) < HideSensitivity)
                        {
                            destination = hit2.position;
                            ShadowMonsterAgent.destination = hit2.position;
                            //MoveEnemyServerRpc();
                            Debug.Log("Found NAV destination!2");
                            flashFlicker.Value = true;
                            hideCounter.Value++;
                        if (!shadowMonsterSoundSource.isPlaying)
                        {
                            shadowMonsterSoundSource.PlayOneShot(hideSound1);
                        }
                        break;
                        }
                    }
                    else
                    {
                        if (Vector3.Dot(hit.normal, (averagePlayerPosition - hit.position).normalized) < HideSensitivity)
                        {
                            //if (NavMesh.SamplePosition(Colliders[i].transform.position - (averagePlayerPosition - hit.position).normalized * ColliderTest, out NavMeshHit hit, ColliderTest, Agent.areaMask))
                            //{
                                ShadowMonsterAgent.SetDestination(hit.position);
                                Debug.Log("Found NAV destination!");
                                flashFlicker.Value = true;
                        if (!shadowMonsterSoundSource.isPlaying)
                        {
                            shadowMonsterSoundSource.PlayOneShot(hideSound1);
                        }
                            //}
                            //hideCounter++;
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
            return Vector3.Distance(ShadowMonsterAgent.transform.position, A.transform.position).CompareTo(Vector3.Distance(ShadowMonsterAgent.transform.position, B.transform.position));
        }
    }

    [ServerRpc]
    private void HideAfterKillServerRpc()
    {
        int hits = Physics.OverlapSphereNonAlloc(ShadowMonsterAgent.transform.position, LineOfSightChecker.Collider.radius, Colliders, HideableLayers);

        Vector3 averagePlayerPosition = GetAverageVector(LineOfSightChecker.Players.Select(p => p.transform.position).ToList());

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
            if (NavMesh.SamplePosition(Colliders[i].transform.position, out NavMeshHit hit, ColliderTest, ShadowMonsterAgent.areaMask))
            {
                if (!NavMesh.FindClosestEdge(hit.position, out hit, ShadowMonsterAgent.areaMask))
                {
                    Debug.LogError($"Unable to find edge close to {hit.position}");
                }

                float Dot = Vector3.Dot(hit.normal, (averagePlayerPosition - hit.position).normalized);

                //if (Dot < HideSensitivity)

                if (NavMesh.SamplePosition(Colliders[i].transform.position - (averagePlayerPosition - hit.position).normalized * ColliderTest, out NavMeshHit hit2, ColliderTest, ShadowMonsterAgent.areaMask))
                {
                    if (!NavMesh.FindClosestEdge(hit2.position, out hit2, ShadowMonsterAgent.areaMask))
                    {
                        Debug.LogError($"Unable to find edge close to {hit2.position}");
                    }

                    if (Vector3.Dot(hit2.normal, (averagePlayerPosition - hit2.position).normalized) < HideSensitivity)
                    {
                        destination = hit2.position;
                        UpdateShadowMonsterStateServerRpc(ShadowMonsterState.hidingafter);
                        ShadowMonsterAgent.destination = hit2.position;
                        if (!shadowMonsterSoundSource.isPlaying)
                        {
                            shadowMonsterSoundSource.PlayOneShot(hideSound1);
                        }
                        //MoveEnemyServerRpc();
                        Debug.Log("Found NAV destination!2");
                        break;
                    }
                }
                else
                {
                    if (Vector3.Dot(hit.normal, (averagePlayerPosition - hit.position).normalized) < HideSensitivity)
                    {
                        //if (NavMesh.SamplePosition(Colliders[i].transform.position - (averagePlayerPosition - hit.position).normalized * ColliderTest, out NavMeshHit hit, ColliderTest, Agent.areaMask))
                        //{
                        UpdateShadowMonsterStateServerRpc(ShadowMonsterState.hidingafter);
                        ShadowMonsterAgent.SetDestination(hit.position);
                        if (!shadowMonsterSoundSource.isPlaying)
                        {
                            shadowMonsterSoundSource.PlayOneShot(hideSound1);
                        }
                        Debug.Log("Found NAV destination!");
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

    [ServerRpc]
    private void MoveEnemyServerRpc()
    {
        ShadowMonsterAgent.SetDestination(destination);
        FaceTarget();
    }


    [ServerRpc]
    private void HandleRoamServerRpc()
    {
        if (networkShadowMonsterState.Value == ShadowMonsterState.hiding)
        {
            hidingTimer += Time.deltaTime;
            lastHidingTimer = 0;
        }
        else
        {
            lastHidingTimer += Time.deltaTime;
            hidingTimer = 0;
        }

        if(hidingTimer >= hidingInterval)
        {
            HideAfterKillServerRpc();
            hidingTimer = 0;
        }

        if(lastHidingTimer >= lastHidingInterval)
        {
            HideAfterKillServerRpc();
            lastHidingTimer = 0;
        }
    }

    private void FaceTarget()
    {
        var turnTowardNavSteeringTarget = ShadowMonsterAgent.steeringTarget;

        Vector3 direction = (turnTowardNavSteeringTarget - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10);
    }
}
