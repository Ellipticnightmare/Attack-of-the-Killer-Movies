using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newTask", menuName = "newTask", order = 0)]
public class Task : ScriptableObject
{
    public GoalType goalType = GoalType.Interact;

    public string title;
    public string description;
    public int numToCompletion = 1;

    public enum GoalType
    {
        Fetch,
        Interact
    }
}