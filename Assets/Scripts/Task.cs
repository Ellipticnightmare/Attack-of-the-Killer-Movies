using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Task
{
    
    public bool isActive;

    public string title;
    public string description;
    public int numToCompletion;
    public int numberCompleted = 0;

    public TaskGoal goal;
    public SpawnType spawnType;
    public Transform spawnLocation;

    public GameObject[] spawnedTaskObjects; // task objects that need to be spawned
    public void TaskComplete()
    {
        isActive = false;
        string taskName = title;
        TaskManager taskManager = GameObject.Find("Tasklist").GetComponent<TaskManager>();
        taskManager.UpdateUI(taskName);
        Debug.Log(title + " Task was completed");

    }
}
public enum SpawnType
{
    Static,
    NonStatic
}
