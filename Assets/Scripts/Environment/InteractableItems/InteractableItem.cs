using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Jobs;
using Unity.Netcode;
using UnityEngine;

public class InteractableItem : NetworkBehaviour
{
    public MeshRenderer meshRenderer;
    public Material defaultMaterial;
    public Material highlightMaterial;

    public virtual void Interact()
    {
        Debug.LogError("did not implement interact");
    }

    public void ToggleHighlight(bool shouldHighlight)
    {
        if (shouldHighlight)
        {
            meshRenderer.material = highlightMaterial;
        }
        else
        {
            meshRenderer.material = defaultMaterial;
        }
    }
}
