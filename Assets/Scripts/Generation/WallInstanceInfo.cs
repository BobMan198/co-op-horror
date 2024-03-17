using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum WallSide { top, bottom, left, right };
public class WallInstanceInfo : MonoBehaviour
{
    public WallSide wallside;
    private MeshRenderer meshRenderer;
    public MeshRenderer MeshRenderer
    {
        get
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }
            return meshRenderer;
        }
    }
}
