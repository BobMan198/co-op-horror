using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHide : MonoBehaviour
{
    [SerializeField]
    private LayerMask HideableLayers;
    [SerializeField]
    private NavMeshAgent Agent;
    [SerializeField]
    private EnemyLineOfSightChecker LineOfSightChecker;

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
        FindHideLocation();
        TryHide();
    }

    private void FindHideLocation()
    {
        if(!LineOfSightChecker.InSight || hasHidingSpot || isHiding){
            return;
        }

        int hideableObjectCount = Physics.OverlapSphereNonAlloc(Agent.transform.position, LineOfSightChecker.Collider.radius, HideableObjects, HideableLayers);
        
        Vector3 averagePlayerPosition = GetAverageVector(LineOfSightChecker.Players.Select(p => p.position).ToList());

        // for each of our hideable objects
        for (int i = 0; i < hideableObjectCount; i++)
        {
            // check if the object has a hidden spot from all of the players
            var direction = HideableObjects[i].transform.position - averagePlayerPosition;

            Debug.DrawRay(averagePlayerPosition, direction, Color.red, 1);
            // try and get the hideable object's position
            if(Physics.Raycast(averagePlayerPosition, direction, out RaycastHit hit, direction.magnitude, HideableLayers)){
                Debug.Log("found hideable object");
                // get the objects position on the navmesh
                NavMesh.SamplePosition(hit.transform.position, out NavMeshHit navHit, 10, Agent.areaMask);

                Debug.DrawRay(navHit.position, Vector3.up * 5, Color.green, 1);

                // find the closest edge
                NavMesh.FindClosestEdge(navHit.position, out NavMeshHit possibleSpot, Agent.areaMask);

                Debug.DrawRay(possibleSpot.position, Vector3.up * 5, Color.blue, 1);

                bool isSafe = true;
                foreach(var player in LineOfSightChecker.Players){
                    Vector3 playerDirection = player.position - possibleSpot.position;
                    Debug.DrawRay(possibleSpot.position, playerDirection, Color.yellow, 1);
                    bool didHitPlayer = Physics.Raycast(possibleSpot.position, playerDirection, out RaycastHit hitPlayer, playerDirection.magnitude, LineOfSightChecker.lineOfSightLayers) 
                    && hit.collider.gameObject.layer == EnemyLineOfSightChecker.PLAYER_LAYER;

                    if(didHitPlayer){
                        isSafe = false;
                    }
                }

                // if its safe, mark that as our location to move to
                if(isSafe){
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

    private void TryHide(){
        if(!LineOfSightChecker.InSight){
            return;
        }

        if(hasHidingSpot){
            Agent.SetDestination(hidingSpot);
            hasHidingSpot = false;
            isHiding = true;
        }

        if(Agent.pathStatus == NavMeshPathStatus.PathComplete){
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
