using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(NavMeshAgent))]
public class BansheeMovement : NetworkBehaviour
{
    public EnemyLineOfSightChecker los;
    private float losTimer;
    public const float bansheeLosInterval = 8;
    public Animator bansheeAnimator;
    public Transform c_closestPlayer;
    public NavMeshAgent bansheeAgent;
    public GameObject bansheeHead;
    public GameObject bansheeHeadPlaceholder;

    public AudioSource bansheeAudioSource;
    public AudioClip bansheeBreathing;
    public AudioClip bansheeChasing;
    private AudioClip currentClip;

    [SerializeField]
    private float lookRoatationSpeed;

    [SerializeField]
    private float chaseSpeed;

    public NetworkVariable<bool> chasing = new NetworkVariable<bool>();
    public enum BansheeState
    {
        idle,
        breathing,
        chase
    }

    [SerializeField]
    public NetworkVariable<BansheeState> networkBansheeState = new NetworkVariable<BansheeState>();

    private void Update()
    {
        FindClosestPlayer();
        HandleAllStates();
        HandleActiveState();
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateBansheeStateServerRpc(BansheeState newState)
    {
        networkBansheeState.Value = newState;
    }

    private void HandleAllStates()
    {
        if (los.InSight && !chasing.Value)
        {
            losTimer += Time.deltaTime;
            if (losTimer >= bansheeLosInterval)
            {
                UpdateBansheeStateServerRpc(BansheeState.chase);
                losTimer = 0;

                Quaternion headPlaceholderRotation = Quaternion.Euler(0, 180, 200);
                Quaternion headRotation = Quaternion.Euler(90, 0, 0);
                bansheeHeadPlaceholder.transform.rotation = headPlaceholderRotation;
                bansheeHead.transform.rotation = headRotation;
            }
            else
            {
                if(networkBansheeState.Value == BansheeState.chase)
                {
                    return;
                }

                UpdateBansheeStateServerRpc(BansheeState.breathing);
            }
        }
        else
        {
            if(!chasing.Value)
            {
                UpdateBansheeStateServerRpc(BansheeState.idle);
            }
        }
    }

    private void HandleActiveState()
    {
        if(networkBansheeState.Value == BansheeState.idle)
        {
            bansheeAnimator.SetFloat("Banshee", 0);
        }

        if (networkBansheeState.Value == BansheeState.breathing)
        {
            if (!bansheeAudioSource.isPlaying)
            {
                bansheeAudioSource.volume = 0.25f;
                bansheeAudioSource.clip = bansheeBreathing;
                bansheeAudioSource.Play();
            }

            // Determine which direction to rotate towards
            Vector3 targetDirection = c_closestPlayer.position - transform.position;

            // The step size is equal to speed times frame time.
            float singleStep = lookRoatationSpeed * Time.deltaTime;

            // Rotate the forward vector towards the target direction by one step
            Vector3 newDirection = Vector3.RotateTowards(bansheeHead.transform.forward, targetDirection, singleStep, 0.0f);

            // Draw a ray pointing at our target in
            Debug.DrawRay(transform.position, newDirection, Color.red);

            // Calculate a rotation a step closer to the target and applies rotation to this object
            bansheeHead.transform.rotation = Quaternion.LookRotation(newDirection);

        }

        if (networkBansheeState.Value == BansheeState.chase)
        {
            bansheeAnimator.SetFloat("Banshee", 1);
            ChasePlayerServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChasePlayerServerRpc()
    {
        bansheeAudioSource.volume = 1;
        PlayChaseAudio(bansheeChasing);
        chasing.Value = true;

        if (chasing.Value)
        {
            if (los.InSight)
            {
                bansheeAgent.speed = chaseSpeed;
                bansheeAgent.destination = c_closestPlayer.transform.position;
                FaceTarget();
                //MoveEnemyServerRpc();
            }
        }

        if (c_closestPlayer == null)
        {
            return;
        }
    }

    private void PlayChaseAudio(AudioClip clip)
    {
        if (currentClip != clip)
        {
            bansheeAudioSource.volume = 1f;
            bansheeAudioSource.clip = bansheeChasing;
            bansheeAudioSource.Play();
            currentClip = clip;
        }

        if(!bansheeAudioSource.isPlaying)
        {
            currentClip = null;
        }
    }

    private void FaceTarget()
    {
        var turnTowardNavSteeringTarget = bansheeAgent.steeringTarget;

        Vector3 direction = (turnTowardNavSteeringTarget - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10);
    }

    private Transform FindClosestPlayer()
    {
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        foreach (Transform potentialTarget in los.PlayerTransformsWithVision)
        {
            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget;
            }
        }

        c_closestPlayer = bestTarget;
        return bestTarget;


    }
}
