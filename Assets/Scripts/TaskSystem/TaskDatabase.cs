using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "taskDatabase", menuName = "taskDatabase", order = 1)]
public class TaskDatabase : ScriptableObject
{
    public Task[] allTasks;
}