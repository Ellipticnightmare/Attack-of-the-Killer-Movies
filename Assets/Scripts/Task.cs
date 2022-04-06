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

    public void TaskComplete()
    {
        isActive = false;
        Debug.Log(title + " Task was completed");

    }
}
