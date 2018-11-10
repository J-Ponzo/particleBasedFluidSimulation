using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DistanceField_Data))]
public class DistanceField_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DistanceField_Data data = (DistanceField_Data)target;
        if (GUILayout.Button("Bake from current scene"))
        {
            data.Bake();
        }
    }
}
