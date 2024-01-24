using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class FootStepManager : MonoBehaviour
{
    [SerializeField]
    private float stepCoolDown;

    private AudioSource audio;

    private const float walkStepRate = 0.45f;
    private const float sprintStepRate = 0.35f;

    public LayerMask footStepLayers;

    private PlayerMovement pm;

    public List<AudioSetting> AudioSettings;

    private void Start()
    {
        pm = GetComponentInParent<PlayerMovement>();
        audio = GetComponent<AudioSource>();
    }

    void Update()
    {
        stepCoolDown -= Time.deltaTime;
        if (pm.playerIsWalking && stepCoolDown < 0f)
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down), Color.red);

            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out RaycastHit hit, 1f, footStepLayers))
            {
                AudioSetting settings = AudioSettings.First(setting => setting.physicsMaterialName == hit.collider.material.name);
                settings.shouldAlternate = !settings.shouldAlternate;
                audio.clip = settings.shouldAlternate ? settings.alternateClip : settings.clip;
                audio.pitch = 1f + Random.Range(-0.2f, 0.2f);
                audio.volume = 0.6f + Random.Range(-0.12f, 0.12f);
                audio.panStereo = settings.shouldAlternate ? -0.25f : 0.25f;
                audio.PlayOneShot(audio.clip);
                stepCoolDown = pm.isSprinting ? sprintStepRate : walkStepRate;
            }
        }
    }
}

[Serializable]
public class AudioSetting
{
    public string physicsMaterialName;
    public bool shouldAlternate;
    public AudioClip clip;
    public AudioClip alternateClip;
    
}
