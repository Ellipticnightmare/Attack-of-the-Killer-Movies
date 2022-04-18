using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskUI : MonoBehaviour
{
    public Task myTask;

    [Header("UI elements")]
    public Text taskTitle;
    public Text taskDescription;
    public Text taskNumToCompletion;
    public Text taskNumberCompleted;
    public int numberCompleted = 0;
    public PlayerObject myPlayer;
    TaskObject holder;
    public bool isReached()
    {
        return (numberCompleted >= myTask.numToCompletion);
    }

    public void GenerateTaskUI()
    {
        taskTitle.text = myTask.title;
        taskDescription.text = myTask.description;
        taskNumToCompletion.text = "";
        taskNumberCompleted.text = "";
        if (myTask.numToCompletion >= 0)
        {
            taskNumToCompletion.text = myTask.numToCompletion.ToString();
            taskNumberCompleted.text = numberCompleted.ToString();
        }
    }
    public void UpdateTaskUI()
    {
        taskTitle.text = myTask.title;
        taskDescription.text = myTask.description;
        taskNumToCompletion.text = "";
        taskNumberCompleted.text = "";
        if (myTask.numToCompletion >= 0)
        {
            numberCompleted++;
            taskNumToCompletion.text = myTask.numToCompletion.ToString();
            taskNumberCompleted.text = numberCompleted.ToString();
        }
        if (numberCompleted >= myTask.numToCompletion)
            TaskManager.RemoveFromTasks(this, holder);
    }
    public void UpdateTaskData(int inNum, TaskObject taskObj)
    {
        holder = taskObj;
        TaskManager.UpdatedTask(this);
    }
}