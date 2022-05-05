using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MonsterController))]
public class FieldOfViewEditor : Editor
{
    void OnSceneGUI()
    {
        MonsterController fow = (MonsterController)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(fow.transform.position, Vector3.up, Vector3.forward, 360, fow.MyAIManager.viewConeRadius);
        Vector3 viewAngleA = fow.DirFromAngle(-fow.MyAIManager.viewConeAngle / 2, false);
        Vector3 viewAngleB = fow.DirFromAngle(fow.MyAIManager.viewConeAngle / 2, false);

        Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleA * fow.MyAIManager.viewConeRadius);
        Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleB * fow.MyAIManager.viewConeRadius);

        Handles.color = Color.red;
    }
}