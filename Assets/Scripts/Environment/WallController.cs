using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class WallController : MonoBehaviour
{
    void Start()
    {
        var meshRenderer = GetComponent<MeshRenderer>();
        var material = meshRenderer.material;

        float width = transform.localScale.x > transform.localScale.z ? transform.localScale.x : transform.localScale.z;
        material.mainTextureScale = new Vector2(width, transform.localScale.y);
    }

    
}
