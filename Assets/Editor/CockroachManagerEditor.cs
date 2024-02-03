using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CockroachManager))]
public class CockroachManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        CockroachManager cockroachManager = (CockroachManager)target;
        if (GUILayout.Button("Spawn"))
        {
            cockroachManager.DEBUG_SetReady();
        }
    }
}
