using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newTask", menuName = "newTask", order = 0)]
public class Task : ScriptableObject
{
    public string title;
    public string description;
}