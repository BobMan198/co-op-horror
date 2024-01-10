using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class EnemyLineOfSightChecker : MonoBehaviour
{
    public SphereCollider Collider;
    public LayerMask lineOfSightLayers;

    public static readonly int PLAYER_LAYER = 8;

    [SerializeField]
    private List<Transform> players;
    public List<Transform> Players => players;

    public bool InSight;

    [SerializeField]
    private List<Transform> playersWithVision;
    public List<Transform> PlayersWithVision => playersWithVision;

    private void Awake()
    {
        Collider = GetComponent<SphereCollider>();
        var playerScripts = FindObjectsOfType<PlayerMovement>();
        players = playerScripts.ToList().Select(p => p.transform).ToList();

        // var playerScripts = FindObjectsOfType<PlayerMovement>();
        // players = playerScripts.ToList().Select(p => p.transform).ToList();
    }

    private void Update(){
        InSight = false;
        playersWithVision = new List<Transform>();

        foreach(var player in players){
            bool canSeePlayer = CheckLineOfSight(player);

            if(canSeePlayer){
                playersWithVision.Add(player);
            }
            
            InSight = InSight || canSeePlayer;
        }
    }

    private bool CheckLineOfSight(Transform Target)
    {
        Vector3 direction = (Target.transform.position - transform.position).normalized;
        bool didHit = Physics.Raycast(transform.position, direction, out RaycastHit hit, Collider.radius, lineOfSightLayers) && hit.collider.gameObject.layer == PLAYER_LAYER;

        return didHit;
    }
}
