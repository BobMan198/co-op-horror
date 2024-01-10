using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using UnityEngine;
using Unity.Netcode;

public class AloneCollider : NetworkBehaviour
{

    public float aloneTick;
    public const float ALONETIMERMAX = 30f;

    public bool playerAlone;

    private bool playerIsAlone;


    private void Start()
    {
       // Collider[] hitColliders = Physics.OverlapSphere(gameObject.transform.position, 30f);
    }

    void Update()
    {
        if(aloneTick >= ALONETIMERMAX)
        {
            playerAlone = true;
        }

        if (playerIsAlone == true)
        {
            Debug.Log(aloneTick);
            aloneTick += Time.deltaTime;
        }

        Collider[] hitColliders = Physics.OverlapSphere(gameObject.transform.position, 30f);

        foreach (var hitCollider in hitColliders)
        {

            if(hitCollider.tag == "Player" || hitCollider.GetComponent<PlayerMovement>().IsOwner == false)
            {
                playerIsAlone = false;
                aloneTick = 0;
            }
            else
            {
                playerIsAlone = true;
            }
        }
    }
}
