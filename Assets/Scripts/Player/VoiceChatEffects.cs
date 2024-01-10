using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using Dissonance.Audio.Playback;
using UnityEngine;

public class VoiceChatEffects : MonoBehaviour
{
    public LayerMask lineOfSightLayers;

    [SerializeField]
    private VoicePlayback _playbackComponent;
    [SerializeField]
    private DissonanceComms _dissonanceComms;
    [SerializeField]
    private VoicePlayerState _playerState;

    private VoicePlayerState _local;

    [SerializeField]
    private bool canSeePlayer;

    public static readonly int PLAYER_LAYER = 8;

    private void Awake()
    {
        _dissonanceComms = FindObjectOfType<DissonanceComms>();
        _playbackComponent = GetComponent<VoicePlayback>();
    }

    private void OnEnable()
    {
        _local = _dissonanceComms.FindPlayer(_dissonanceComms.LocalPlayerName);

    }

    void Update()
    {

         _playerState = _dissonanceComms.FindPlayer(_playbackComponent.PlayerName);
   
            canSeePlayer = CheckLineOfSight();

            if (canSeePlayer)
            {
                _playbackComponent.GetComponent<AudioLowPassFilter>().cutoffFrequency = (10472);
            }
            else
            {
                _playbackComponent.GetComponent<AudioLowPassFilter>().cutoffFrequency = (1000);
            }
    }

    private bool CheckLineOfSight()
    {

        Vector3 direction = (_playerState.Tracker.Position - _local.Tracker.Position).normalized;
        bool didHit = Physics.Raycast(_local.Tracker.Position, direction, out RaycastHit hit, 50f, lineOfSightLayers) && hit.collider.gameObject.layer == PLAYER_LAYER;

        //Debug.DrawRay(_local.Tracker.Position, direction, Color.red);
        Debug.DrawRay(_local.Tracker.Position, direction * 50f, Color.green);

        return didHit;
    }
}
