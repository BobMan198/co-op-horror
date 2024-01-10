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
public class EnemyMovement : MonoBehaviour
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

    public float chaseSpeed;
    public float catchDistance;

    [SerializeField]
    private List<GameObject> flashLights;

    [SerializeField]
    private List<GameObject> playerAloneColliders;

    [SerializeField]
    private List<GameObject> players;

    [SerializeField]
    private float hideCounter;

    private bool isHiding;
    private bool HideCoolDown;
    private bool Rage;
    private bool flashFlicker;

    private Transform targetPlayer;

    public AudioSource neckSnapSound = null;

    private Coroutine MovementCoroutine;
    private Collider[] Colliders = new Collider[20];


    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Rage = false;

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
        IfHiding();
        HandleSwapOffHide();
        HandleRageServerRpc();
        HandleEncounterTimer();
        HandleAlonePlayersServerRpc();

        flashlightTimer += Time.deltaTime;

        foreach (var flashlight in flashLights)
        {
            if (flashlightTimer >= flashlightInterval && flashFlicker && isHiding)
            {
                flashlight.SetActive(true);
                flashlightTimer = 0;
            }
            if(flashlightTimer < flashlightInterval && flashFlicker)
            {
                flashlightOnTimer += Time.deltaTime;
                if(flashlightOnTimer >= flashlightInterval && flashFlicker)
                {
                    flashlight.SetActive(false);
                    flashlightOnTimer = 0;
                }
            }
        }
    }

    [ServerRpc]
    private void HandleAlonePlayersServerRpc()
    {
        Vector3 currentPosition = transform.position;
        float distance = Mathf.Infinity;

        foreach (var player in playerAloneColliders)
        {
            if(player.GetComponent<AloneCollider>().playerAlone == true)
            {
                var alonePlayerTransform = player.GetComponentInParent<Transform>();

                Agent.SetDestination(alonePlayerTransform.position);

                if (Vector3.Distance(currentPosition, alonePlayerTransform.position) <= 2f)
                {
                    player.GetComponent<AloneCollider>().playerAlone = false; 
                }
            }
        }

    }

    private void HandleEncounterTimer()
    {
        resetEncountersTimer += Time.deltaTime;

        if (isHiding)
        {
            resetEncountersTimer = 0;
            return;
        }

        if (resetEncountersTimer >= resetEncountersInterval && !Rage && hideCounter >= 1)
        {
            hideCounter -= 1;
            resetEncountersTimer = 0;
        }
    }

    private void HandleSwapOffHide()
    {
        if(hideCounter >= 10)
        {
            HideCoolDown = true;
            hideCounter = 0;
            Rage = true;
        }
    }

    private void IfHiding()
    {
        if (Agent.remainingDistance < 0.5f && !Agent.pathPending)
        {
            isHiding = false;
            flashFlicker = false;
            foreach (var flashlight in flashLights)
            {
                flashlight.SetActive(true);
            }
        }

        if (!LineOfSightChecker.InSight)
        {
            flashFlicker = false;
            foreach (var flashlight in flashLights)
            {
                flashlight.SetActive(true);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleRageServerRpc()
    {
        if(Rage && HideCoolDown)
        {

            if(LineOfSightChecker.InSight)
            {
                Vector3 currentPosition = transform.position;
                float distance = Mathf.Infinity;
                var player = LineOfSightChecker.PlayersWithVision;
                Transform closest = null;
                var dest = player.Select(p => p.position);
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

                Agent.destination = closest.position;
                Agent.speed = chaseSpeed;

                if(Vector3.Distance(currentPosition, closest.position) <= 2f)
                {
                    closest.GetComponent<PlayerMovement>().playerHealth = 0;
                    closest.GetComponent<PlayerMovement>().ChangePlayerTagOnDeathServerRpc();
                    Rage = false;
                    HideCoolDown = false;
                    if (neckSnapSound.isPlaying) return;

                    neckSnapSound.Play();
                }
                //if(Agent.remainingDistance <= catchDistance)
                //{
                //   closest.GetComponent<PlayerMovement>().playerHealth = 0;
                // closest.gameObject.layer = default;
                //Rage = false;
                //HideCoolDown = false;
                //}
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HideServerRpc()
    {
        if(HideCoolDown || Rage)
        {
            return;
        }

        if (!LineOfSightChecker.InSight || isHiding)
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
                            Agent.SetDestination(hit2.position);
                            Debug.Log("Found NAV destination!2");
                            flashFlicker = true;
                            hideCounter++;
                            isHiding = true;
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
                                flashFlicker = true;
                            //}
                            //hideCounter++;
                            isHiding = true;
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
}
