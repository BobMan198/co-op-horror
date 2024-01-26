using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DungeonCreator))]
public class DungeonCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DungeonCreator dungeonCreator = (DungeonCreator)target;
        GameRunner gameRunner = dungeonCreator.gameRunner;
        if(GUILayout.Button("Create New Dungeon"))
        {
            //dungeonCreator.CreateDungeon();
            gameRunner.GenerateRoomSeedServerRpc();
        }
    }
}
