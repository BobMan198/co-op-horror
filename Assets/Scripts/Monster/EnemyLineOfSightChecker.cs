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
    private List<GameObject> players;
    public List<GameObject> Players => players;

    public bool InSight;

    [SerializeField]
    private List<GameObject> playersWithVision;
    public List<GameObject> PlayersWithVision => playersWithVision;

    [SerializeField]
    private List<Transform> playerTransformsWithVision;
    public List<Transform> PlayerTransformsWithVision => playerTransformsWithVision;

    private GameRunner gameRunner;

    private void Awake()
    {
        gameRunner = FindObjectOfType<GameRunner>();
        Collider = GetComponent<SphereCollider>();
        // var playerScripts = FindObjectsOfType<PlayerMovement>();
        // players = playerScripts.ToList().Select(p => p.transform).ToList();
    }

    private void Update(){
        players = gameRunner.alivePlayers;
        InSight = false;
        playersWithVision = new List<GameObject>();
        playerTransformsWithVision = new List<Transform>();

        foreach (var player in players){
            bool canSeePlayer = CheckLineOfSight(player);

            if(canSeePlayer){
                playersWithVision.Add(player);
                playerTransformsWithVision.Add(player.transform);
            }
            
            InSight = InSight || canSeePlayer;
        }
    }

    private bool CheckLineOfSight(GameObject Target)
    {
        Vector3 direction = (Target.transform.position - transform.position).normalized;
        bool didHit = Physics.Raycast(transform.position, direction, out RaycastHit hit, Collider.radius, lineOfSightLayers) && hit.collider.gameObject.layer == PLAYER_LAYER;

        return didHit;
    }
}
