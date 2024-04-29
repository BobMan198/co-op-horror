using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class EnemyHide : MonoBehaviour
{
    [SerializeField]
    private LayerMask HideableLayers;
    [SerializeField]
    private NavMeshAgent Agent;
    [SerializeField]
    private EnemyLineOfSightChecker LineOfSightChecker;

    [Range(-1, 1)]
    [Tooltip("Lower is a better hiding spot")]
    public float HideSensitivity = 0;
    [Range(1, 75)]
    public float MinPlayerDistance = 15f;

    private Collider[] HideableObjects = new Collider[20];

    private bool hasHidingSpot;
    private Vector3 hidingSpot;
    private bool isHiding;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        //FindHideLocation();
        Hide();
        //TryHide();
    }

    private void Hide()
    {
        if (!LineOfSightChecker.InSight || hasHidingSpot || isHiding) {
            //    return;
            //}

            int hideableObjectCount = Physics.OverlapSphereNonAlloc(Agent.transform.position, LineOfSightChecker.Collider.radius, HideableObjects, HideableLayers);

            Vector3 averagePlayerPosition = GetAverageVector(LineOfSightChecker.Players.Select(p => p.transform.position).ToList());

            for (int i = 0; i < hideableObjectCount; i++)
            {
                //finding a object to hide behind
                if (NavMesh.SamplePosition(HideableObjects[i].transform.position, out NavMeshHit hit, 18f, Agent.areaMask))
                {
                    //finding the best edge
                    if (!NavMesh.FindClosestEdge(hit.position, out hit, Agent.areaMask))
                    {
                        Debug.LogError($"Unable to find edge close to {hit.position}");
                    }
                    //finding the best spot depending on the players location
                    if (Vector3.Dot(hit.normal, (averagePlayerPosition - hit.position).normalized) < HideSensitivity)
                    {
                        Agent.SetDestination(hit.position);
                        break;
                    }
                    else
                    {
                        if (NavMesh.SamplePosition(HideableObjects[i].transform.position, out NavMeshHit hit2, 18f, Agent.areaMask))
                        {
                            if (!NavMesh.FindClosestEdge(hit2.position, out hit2, Agent.areaMask))
                            {
                                Debug.LogError($"Unable to find edge close to {hit2.position}");
                            }
                        }

                        if (Vector3.Dot(hit2.normal, (averagePlayerPosition - hit2.position).normalized) < HideSensitivity)
                        {
                            Agent.SetDestination(hit2.position);
                            break;
                        }
                    }
                }
                else
                {
                    Debug.LogError($"unable to find navmesh near object {HideableObjects[i].name}");
                }
            }
        }
    }

        private void FindHideLocation()
        {
            //if(!LineOfSightChecker.InSight || hasHidingSpot || isHiding){
            //    return;
            //}

            int hideableObjectCount = Physics.OverlapSphereNonAlloc(Agent.transform.position, LineOfSightChecker.Collider.radius, HideableObjects, HideableLayers);

            Vector3 averagePlayerPosition = GetAverageVector(LineOfSightChecker.Players.Select(p => p.transform.position).ToList());

            // for each of our hideable objects
            for (int i = 0; i < hideableObjectCount; i++)
            {
                // check if the object has a hidden spot from all of the players
                var direction = HideableObjects[i].transform.position - averagePlayerPosition;

                Debug.DrawRay(averagePlayerPosition, direction, Color.red, 1);
                // try and get the hideable object's position
                if (Physics.Raycast(averagePlayerPosition, direction, out RaycastHit hit, direction.magnitude, HideableLayers))
                {
                    Debug.Log("found hideable object");
                    // get the objects position on the navmesh
                    NavMesh.SamplePosition(hit.transform.position, out NavMeshHit navHit, LineOfSightChecker.Collider.radius, Agent.areaMask);

                    Debug.DrawRay(navHit.position, Vector3.up * 5, Color.green, 1);

                    // find the closest edge
                    NavMesh.FindClosestEdge(navHit.position, out NavMeshHit possibleSpot, Agent.areaMask);

                    Debug.DrawRay(possibleSpot.position, Vector3.up * 5, Color.blue, 1);

                    bool isSafe = true;
                    //for every player check to see if the monster is in line of sight
                    foreach (var player in LineOfSightChecker.Players)
                    {
                        Vector3 playerDirection = player.transform.position - possibleSpot.position;
                        Debug.DrawRay(possibleSpot.position, playerDirection, Color.yellow, 1);
                        bool didHitPlayer = Physics.Raycast(possibleSpot.position, playerDirection, out RaycastHit hitPlayer, playerDirection.magnitude, LineOfSightChecker.lineOfSightLayers)
                        && hit.collider.gameObject.layer == EnemyLineOfSightChecker.PLAYER_LAYER;
                        //if in line of sight then the hiding spot is not safe
                        if (didHitPlayer)
                        {
                            isSafe = false;
                        }
                    }

                    // if its safe, mark that as our location to move to
                    if (isSafe)
                    {
                        hasHidingSpot = true;
                        hidingSpot = possibleSpot.position;
                        break;
                    }
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

        private void TryHide()
        {
            if (!LineOfSightChecker.InSight)
            {
                return;
            }

            if (hasHidingSpot)
            {
                Agent.SetDestination(hidingSpot);
                hasHidingSpot = false;
                isHiding = true;
            }

            if (Agent.pathStatus == NavMeshPathStatus.PathComplete)
            {
                isHiding = false;
            }
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

