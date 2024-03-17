using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PrefabColliderGenerator : MonoBehaviour
{
    [ContextMenu("Generate Child Colliders")]
    public void GenerateChildColliders()
    {
        List<Transform> children = new();
        FindLeaves(gameObject.transform, children);

        foreach(Transform child in children){
            var childCollider = child.GetComponent<BoxCollider>();

            if(childCollider == null){
                childCollider = child.AddComponent<BoxCollider>();
            }
        }
    }

    [ContextMenu("Generate Parent Collider")]
    public void GenerateParentCollider()
    {
        BoxCollider myCollider = GetComponent<BoxCollider>();

        List<Transform> children = new();
        FindLeaves(gameObject.transform, children);;

        Bounds newBounds = new Bounds();
        foreach(Transform child in children){
            var childCollider = child.GetComponent<BoxCollider>();
            newBounds.Encapsulate(childCollider.bounds);
        }

        myCollider.center = newBounds.center;
        myCollider.size = newBounds.size;

        EditorUtility.SetDirty(myCollider);
    }

    [ContextMenu("Delete Child Colliders")]
    public void DeleteChildColliders()
    {
        List<Transform> children = new();
        FindLeaves(gameObject.transform, children);

        foreach(Transform child in children){
            var childCollider = child.GetComponent<BoxCollider>();

            if(childCollider != null){
                EditorUtility.SetDirty(childCollider);
                DestroyImmediate(childCollider);
            }
        }
    }

    private void FindLeaves(Transform parent, List<Transform> leafArray)
    {
        if (parent.childCount == 0)
        {
            leafArray.Add(parent);
        }
        else
        {
            foreach(Transform child in parent)
            {
                FindLeaves(child, leafArray);
            }
        }
    }

}
