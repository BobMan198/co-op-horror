using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class FloorInstance : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
}
