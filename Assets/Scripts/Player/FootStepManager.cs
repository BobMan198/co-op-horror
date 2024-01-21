using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootStepManager : MonoBehaviour
{
    [SerializeField]
    private float stepRate = 0.45f;
    [SerializeField]
    private float stepCoolDown;

    [SerializeField]
    private AudioClip grass_footStep;
    [SerializeField]
    private AudioClip grass_footStep2;
    [SerializeField]
    private AudioClip concrete_footStep;
    [SerializeField]
    private AudioClip metal_footStep;
    [SerializeField]
    private AudioClip metal_footStep2;
    [SerializeField]
    private AudioClip wood_footStep;
    [SerializeField]
    private AudioClip wood_footStep2;

    private bool grass2 = false;
    private bool wood2 = false;
    private bool metal2 = false;

    private PlayerMovement pm;

    private void Start()
    {
        pm = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        stepCoolDown -= Time.deltaTime;
        if (pm.playerIsWalking && stepCoolDown < 0f)
        {
            AudioSource audio = GetComponent<AudioSource>();

            RaycastHit hit;
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down), Color.red);

            if(pm.isSprinting)
            {
                stepRate = 0.35f;
            }
            else
            {
                stepRate = 0.45f;
            }

            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit, 1f))
            {
                if (hit.collider.material.name == "Grass (Instance)")
                {
                    if (!grass2)
                    {
                        audio.clip = grass_footStep;
                        audio.pitch = 1f + Random.Range(-0.1f, 0.1f);
                        audio.volume = 0.6f + Random.Range(-0.12f, 0.12f);
                        audio.PlayOneShot(grass_footStep, 0.9f);
                        stepCoolDown = stepRate;
                        grass2 = true;
                        Debug.Log("WALKING ON GRASS!");
                    }
                    else
                    {
                        audio.clip = grass_footStep2;
                        audio.pitch = 1f + Random.Range(-0.1f, 0.1f);
                        audio.volume = 0.6f + Random.Range(-0.12f, 0.12f);
                        audio.PlayOneShot(grass_footStep2, 0.9f);
                        stepCoolDown = stepRate;
                        grass2 = false;
                        Debug.Log("WALKING ON GRASS!");
                    }
                }

                if (hit.collider.material.name == "Concrete (Instance)")
                {
                        audio.clip = concrete_footStep;
                        audio.pitch = 1f + Random.Range(-0.1f, 0.1f);
                        audio.volume = 0.6f + Random.Range(-0.12f, 0.12f);
                        audio.PlayOneShot(concrete_footStep, 0.9f);
                        stepCoolDown = stepRate;
                        Debug.Log("WALKING ON CONCRETE!");
                }

                if (hit.collider.material.name == "Wood (Instance)")
                {
                    if(!wood2)
                    {
                        audio.clip = wood_footStep;
                        audio.pitch = 1f + Random.Range(-0.2f, 0.2f);
                        audio.volume = 0.7f + Random.Range(-0.1f, 0.1f);
                        audio.PlayOneShot(wood_footStep, 0.9f);
                        stepCoolDown = stepRate;
                        wood2 = true;
                        Debug.Log("WALKING ON WOOD!");
                    }
                    else
                    {
                        audio.clip = wood_footStep2;
                        audio.pitch = 1f + Random.Range(-0.2f, 0.2f);
                        audio.volume = 0.7f + Random.Range(-0.1f, 0.1f);
                        audio.PlayOneShot(wood_footStep2, 0.9f);
                        stepCoolDown = stepRate;
                        wood2 = false;
                        Debug.Log("WALKING ON WOOD!");
                    }
                }

                if (hit.collider.material.name == "Metal (Instance)")
                {
                    if(!metal2)
                    {
                        audio.clip = metal_footStep;
                        audio.pitch = 1f + Random.Range(-0.2f, 0.2f);
                        audio.volume = 0.9f + Random.Range(-0.1f, 0.1f);
                        audio.PlayOneShot(metal_footStep, 0.9f);
                        stepCoolDown = stepRate;
                        metal2 = true;
                        Debug.Log("WALKING ON METAL!");
                    }
                    else
                    {
                        audio.clip = metal_footStep2;
                        audio.pitch = 1f + Random.Range(-0.2f, 0.2f);
                        audio.volume = 0.9f + Random.Range(-0.1f, 0.1f);
                        audio.PlayOneShot(metal_footStep2, 0.9f);
                        stepCoolDown = stepRate;
                        metal2 = false;
                        Debug.Log("WALKING ON METAL!");
                    }
                }

                if(hit.collider.material.name == "")
                {
                        audio.clip = concrete_footStep;
                        audio.pitch = 1f + Random.Range(-0.2f, 0.2f);
                        audio.volume = 0.9f + Random.Range(-0.1f, 0.1f);
                        audio.PlayOneShot(concrete_footStep, 0.9f);
                        stepCoolDown = stepRate;
                        Debug.LogError("NULL PHYSICS COLLIDER! MAKE SURE TO SET PHYSICS MATERIAL!");
                }
            }
        }
    }
}
